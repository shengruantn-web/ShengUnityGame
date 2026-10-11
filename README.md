# Village Clash

A single-player, offline, Clash-of-Clans-style base builder and raid game built in Unity 6 (6000.3 LTS).
Everything (sprites, UI, sound effects, the scene) is generated from code at startup; there are no
imported art assets. The game was designed from ten reference screenshots of Clash of Clans and
re-implemented from scratch as a learning project. It is not affiliated with Supercell.

## What you can do

- Build and upgrade a village on a 40x40 isometric grid: Town Hall (levels 1-5), Gold Mines, Elixir
  Collectors, storages, Barracks, Army Camps, Laboratory, Builder's Hut, Walls, Cannons, Archer
  Towers, Mortars, and traps. Buildings take real time to finish; gems can speed them up.
- Collect resources, buy a second builder, buy a shield, clear obstacles.
- Train Barbarians, Archers, Giants, Goblins and Wall Breakers, and research them in the Laboratory.
- Attack procedurally generated enemy bases (or three practice bases): deploy troops by tapping or
  holding, watch the real-time battle (pathfinding through walls, defenses shooting back, traps,
  loot pops), and earn stars, loot and trophies.
- Climb leagues from Unranked through Titan; browse mock Top Players / Top Clans, a mock clan search,
  a profile panel with rename, and a Settings panel (audio toggles, Save, Reset Village).
- Progress persists to a JSON save file. While you are away, simulated raids may hit your village
  and appear in the mail/defense log at next login.

## Layout

```
docs/superpowers/specs/   design spec
docs/superpowers/plans/   implementation plan (14 tasks)
VillageClash/             Unity project
  Assets/Scripts/Core     engine-free game logic (data tables, grid, economy, training, battle sim, save)
  Assets/Scripts/Runtime  rendering, camera, input, audio, battle presentation (depends on UnityEngine)
  Assets/Scripts/UI       uGUI built from code: HUD, shop, popups, panels, battle HUD
  Assets/Editor           ProjectSetup: creates Main scene, builds the macOS player
  Assets/Tests/EditMode   NUnit tests for Core and save-file recovery
```

The scene `Assets/Scenes/Main.unity` contains only a camera. `Bootstrap` runs on scene load and
constructs the whole game.

## Requirements

- Unity 6000.3.x LTS (tested with 6000.3.26f1, Apple silicon)
- The Unity CLI (`unity`) for the headless commands below, or the Unity Hub/Editor.

## Open and play in the Editor

```bash
unity open VillageClash          # or open VillageClash/ from Unity Hub
```

Open `Assets/Scenes/Main.unity` and press Play. The window is designed for a 16:9 landscape view.

Controls: drag to pan, scroll/pinch to zoom, tap a building to open its popup, long-press a building
to move it, Escape closes panels. In battle, tap or hold on green ground to deploy the selected troop.

## Run the tests

```bash
unity test VillageClash --mode EditMode
```

## Build the macOS app

```bash
cd VillageClash
unity run . -- -executeMethod VillageClash.Editor.ProjectSetup.CreateMainScene   # once
unity run . -- -executeMethod VillageClash.Editor.ProjectSetup.BuildMac
open Builds/VillageClash.app
```

Both menu items are also available in the Editor under **Village Clash**.

### Automated gameplay and screenshot checks

Both verification modes start with a fresh, seeded village in a unique temporary
directory, using a controlled clock. They never load or write your normal player
save. The isolated save path is printed in the log and retained for inspection.

Run gameplay checks without a display (from the repository root):

```bash
"VillageClash/Builds/VillageClash.app/Contents/MacOS/Village Clash" \
  -batchmode -nographics -smoke-test -logFile /tmp/vc-smoke.log
```

The runner asserts building placement and construction completion, resource
collection, paid training and completion, a normal raid with loot/trophies and
troop consumption, return home, and complete save/reload through a fresh controller.
Timers and the battle simulation are accelerated so the checks finish quickly.
It prints `[Smoke] OK/FAIL` lines and returns exit code **0** only when all checks
pass; failed checks, runtime errors, or invalid options return **1**.

The watchdog runs independently of Unity frames and forcibly terminates a stalled
player with a nonzero exit code, including when rendering stops advancing.
Override its deadline with `-smoke-timeout <seconds>` (1–600; default 180).

To also capture all 13 screens, use an unlocked desktop with a working graphics
session. This includes the village, building popup, shop, build mode, army, attack
menu, raid, result, league, clans, settings, and mail:

```bash
"VillageClash/Builds/VillageClash.app/Contents/MacOS/Village Clash" \
  -shots /tmp/vc-shots -logFile /tmp/vc-shots.log
open /tmp/vc-shots
```

Screenshots must contain nonuniform rendered pixels and decode successfully at
the captured resolution. Existing PNGs with the same names are replaced.
`-shots` rejects batch mode or a null graphics device; use `-smoke-test` for CI.
Successful capture validates the artifacts, but layout and input still need visual
and interactive review.

Verify repeatability, failure exit codes, and preservation of your normal save:

```bash
python3 scripts/verify_smoke.py
```

This runs two independent gameplay checks, two invalid-invocation checks, and a
one-second watchdog check. It compares the resulting game states and checks that
your normal save's contents and modification time are unchanged. Logs are retained
in a temporary directory.

The runner also exercises the same input-router gesture methods used by mouse
input: UI click suppression, camera drag and zoom, modal input blocking,
long-press building movement, invalid placement, and cancellation. These checks
complement desktop mouse/keyboard testing; they do not synthesize OS input.

For manual testing with an isolated starter village and normal real-time timers:

```bash
"VillageClash/Builds/VillageClash.app/Contents/MacOS/Village Clash" -qa-session
```

This mode stays open until you quit and prints its temporary save path in the
player log. Use it separately from `-smoke-test` and `-shots`.

## Package for release

`ProjectSetup.BuildMac` now applies the release identity before building (bundle id
`com.shengruantn.villageclash`, version from `ProjectSetup.Version`, the procedural icon in
`Assets/Icon/AppIcon.png`) and removes the Burst debug folder afterwards. To produce a
distributable zip:

```bash
scripts/package_mac.sh                                   # ad-hoc signed
CODESIGN_IDENTITY="Developer ID Application: ..." NOTARY_PROFILE=myprofile scripts/package_mac.sh
```

The script strips debug symbols, signs and verifies the bundle, writes
`VillageClash/Builds/VillageClash-<version>-macOS.zip` with a `.sha256` next to it, and
notarizes when a Developer ID identity and notary profile are given. An ad-hoc signed build runs
on other Macs only after **right-click > Open** on the first launch, or after
`xattr -d com.apple.quarantine VillageClash.app`.

To regenerate the icon: `python3 scripts/make_icon.py VillageClash/Assets/Icon/AppIcon.png`.

## Save file

`~/Library/Application Support/VillageClash/Village Clash/village.json` (Unity's
`persistentDataPath`). Use Settings > More Settings > Reset Village to start over.

Saves are flushed to `village.json.tmp` and atomically replace the primary file.
`village.json.bak` keeps the previous valid save. If the primary is missing or
invalid, the game loads the backup and displays a recovery notice. Invalid files
are copied to `.unreadable-<id>` files before replacement. An IO error while loading
disables saving for that session to protect existing progress; fix file access and
restart. Save Now reports whether the write succeeded. To reset manually, remove
both the primary and backup while the game is closed.

Raids save a result checkpoint at entry, on each troop deployment, every five seconds
of combat, and at battle end. Quit/pause also saves the current checkpoint. After an
interruption, the next launch settles the last committed checkpoint once and returns
home: deployed troops are consumed and earned loot, trophies, and XP are applied.
Unfinished raids use the same result rules as ending the battle at that point;
leaving during scouting keeps the army but counts as a zero-star surrender.
Practice still leaves trophies and normal battle statistics unchanged. Combat does
not resume; a crash can lose combat progress since the last successful checkpoint.
If the initial raid save fails, the raid does not start or charge its entry cost.
Existing version-1 saves remain compatible.

## License

MIT, see `LICENSE`. Village Clash is a fan-made learning project and is not affiliated with or
endorsed by Supercell.
