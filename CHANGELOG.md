# Changelog

All notable changes to Village Clash are recorded here. Dates are in the Asia/Bangkok timezone.

## [Unreleased]

## [1.0.0] - 2026-10-11

First playable release (macOS, Apple silicon and Intel).

- Village building on a 40x40 isometric grid with Town Hall levels 1 to 5, resource buildings, storages, barracks, army camps, laboratory, builder's hut, walls, cannons, archer towers, mortars and traps.
- Five troop types with laboratory research.
- Real-time raids on procedurally generated enemy bases and three practice bases, with pathfinding, defenses, traps, stars, loot and trophies.
- Leagues from Unranked to Titan, mock top players and clans, profile and settings panels.
- Offline production, simulated incoming raids and a defense log.
- Fully procedural art, UI and audio; no imported assets.
- 70 EditMode tests (including save-file recovery, interrupted-raid settlement and battle camera framing), a headless gameplay smoke runner and a screenshot capture mode.
- Save reliability: writes go to a temporary file and atomically replace `village.json`; the previous valid save is kept as `village.json.bak`; unreadable files are preserved; interrupted raids are checkpointed and settled once on the next launch.
- Battle camera frames the whole enemy base between the HUD strips at any window size (`BattleFraming` in Core; the HUD reports its top and bottom inset heights to the battle controller).
- Locked shop cards hide the build-time row so the clock icon no longer shows through the "Town Hall Required" label.
- Release identity: bundle id `com.shengruantn.villageclash`, version 1.0.0, procedural app icon.
- `scripts/package_mac.sh` strips debug symbols, signs, verifies and zips the app; optional notarization. The `.sha256` file records the zip basename so `shasum -c` works from any download folder.
- `ProjectSetup.BuildMac` applies release settings before building and removes the Burst debug folder afterwards.
- MIT license.
