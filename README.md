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
  Assets/Tests/EditMode   NUnit tests for Core (50 tests)
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

### Automated screenshot smoke test

The player accepts a `-shots <dir>` argument. It then walks through every screen
(village, shop, build mode, army, attack menu, a full practice battle, result,
league, clans, settings, mail), saves one PNG per screen into `<dir>`, saves the
game and quits. Useful for a headless visual check after a build:

```bash
"Builds/VillageClash.app/Contents/MacOS/Village Clash" -shots /tmp/vc-shots
open /tmp/vc-shots
```

The same run also logs `[Smoke] OK/FAIL` lines for placing a building, training a troop,
the battle result and a save/reload round-trip, ending with `ALL CHECKS PASSED`. To run
it without a display (CI, or a locked screen) add `-batchmode -nographics`; the state
checks still run and are printed to stdout, but the PNGs are then blank.

## Save file

`~/Library/Application Support/VillageClash/Village Clash/village.json` (Unity's
`persistentDataPath`). Delete it, or use Settings > More Settings > Reset Village, to start over.
