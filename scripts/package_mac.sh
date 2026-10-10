#!/usr/bin/env bash
# Package the built macOS app for distribution: strip debug folders, sign, verify, zip.
#
#   scripts/package_mac.sh                      # ad-hoc signature (default)
#   CODESIGN_IDENTITY="Developer ID Application: Name (TEAMID)" scripts/package_mac.sh
#   CODESIGN_IDENTITY="Developer ID Application: Name (TEAMID)" NOTARY_PROFILE=myprofile scripts/package_mac.sh
#
# NOTARY_PROFILE is a keychain profile created with:
#   xcrun notarytool store-credentials myprofile --apple-id you@example.com --team-id TEAMID
# Without a Developer ID identity the zip still works, but Gatekeeper requires
# right-click > Open (or `xattr -d com.apple.quarantine`) on the first launch.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
BUILDS="$ROOT/VillageClash/Builds"
APP="$BUILDS/VillageClash.app"
IDENTITY="${CODESIGN_IDENTITY:--}"

[ -d "$APP" ] || { echo "error: $APP not found. Build first: cd VillageClash && unity run . -- -executeMethod VillageClash.Editor.ProjectSetup.BuildMac" >&2; exit 1; }

VERSION="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' "$APP/Contents/Info.plist")"
BUNDLE_ID="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$APP/Contents/Info.plist")"
ZIP="$BUILDS/VillageClash-$VERSION-macOS.zip"

echo "App:       $APP"
echo "Bundle id: $BUNDLE_ID"
echo "Version:   $VERSION"
echo "Identity:  $IDENTITY"

case "$BUNDLE_ID" in
  com.DefaultCompany.*) echo "error: bundle id is still the Unity template default; rebuild with ProjectSetup.BuildMac" >&2; exit 1 ;;
esac

# Debug symbols must never ship.
find "$BUILDS" -maxdepth 1 -type d -name '*_BurstDebugInformation_DoNotShip' -exec rm -rf {} +
find "$APP" -name '.DS_Store' -delete

# Sign inner code first, then the bundle (Unity's player already carries an ad-hoc seal; we re-seal everything consistently).
if [ "$IDENTITY" = "-" ]; then
  codesign --force --deep --sign - --timestamp=none "$APP"
else
  codesign --force --deep --options runtime --timestamp --sign "$IDENTITY" "$APP"
fi
codesign --verify --deep --strict --verbose=2 "$APP"

rm -f "$ZIP"
ditto -c -k --keepParent "$APP" "$ZIP"

if [ -n "${NOTARY_PROFILE:-}" ]; then
  [ "$IDENTITY" != "-" ] || { echo "error: notarization needs a Developer ID identity, not ad-hoc" >&2; exit 1; }
  xcrun notarytool submit "$ZIP" --keychain-profile "$NOTARY_PROFILE" --wait
  xcrun stapler staple "$APP"
  rm -f "$ZIP"
  ditto -c -k --keepParent "$APP" "$ZIP"
fi

if [ "$IDENTITY" = "-" ]; then
  echo "note: ad-hoc signed. Gatekeeper assessment is expected to fail; users must right-click > Open once."
  spctl --assess --type execute "$APP" 2>&1 | sed 's/^/spctl: /' || true
else
  spctl --assess --type execute --verbose "$APP"
fi

shasum -a 256 "$ZIP" | tee "$ZIP.sha256"
echo "Packaged: $ZIP ($(du -h "$ZIP" | cut -f1))"
