# Changelog

All notable changes to Village Clash are recorded here. Dates are in the Asia/Bangkok timezone.

## [Unreleased]

### Added
- Release identity: bundle id `com.shengruantn.villageclash`, version 1.0.0, procedural app icon.
- `scripts/package_mac.sh`: strips debug symbols, signs, verifies and zips the macOS app; optional notarization.
- MIT license.

### Changed
- `ProjectSetup.BuildMac` applies release settings before building and removes the Burst debug folder afterwards.

## [1.0.0] - 2026-10-10 (candidate)

First playable release.

- Village building on a 40x40 isometric grid with Town Hall levels 1 to 5, resource buildings, storages, barracks, army camps, laboratory, builder's hut, walls, cannons, archer towers, mortars and traps.
- Five troop types with laboratory research.
- Real-time raids on procedurally generated enemy bases and three practice bases, with pathfinding, defenses, traps, stars, loot and trophies.
- Leagues from Unranked to Titan, mock top players and clans, profile and settings panels.
- Offline production, simulated incoming raids and a defense log.
- JSON save with atomic writes, backup recovery and interrupted-raid settlement.
- Fully procedural art, UI and audio; no imported assets.
- 65 EditMode tests (including save-file recovery and interrupted-raid settlement), a headless gameplay smoke runner and a screenshot capture mode.
