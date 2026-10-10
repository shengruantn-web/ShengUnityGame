# Village Clash — Design Spec

Date: 2026-10-10
Status: approved by default (autonomous `/goal` run; assumptions listed in §1.3)

## 1. Intent

### 1.1 What the user asked for
Ten screenshots of Clash of Clans (village, build-placement mode, shop tabs, attack battle,
tutorial battles, leagues, clans search, settings) were supplied with the instruction:
"brainstorm and plan a game using Unity on this local to generate a similar game till it
complete."

### 1.2 Outcome
A playable, single-player, offline Clash-of-Clans-style base builder and raid game that runs
locally in Unity 6 LTS on this Mac. "Similar" means the same screens, layout, flow and core
loop as the references, not Supercell's art. Success = every reference screen exists and works
end to end: build a village, collect resources, train troops, attack a generated enemy base,
earn loot and trophies, see league and clan panels, change settings, and have progress persist
across launches.

### 1.3 Assumptions (no user available to confirm)
- Engine: Unity 6000.3 LTS (latest LTS available to the CLI), 2D core template, uGUI with the
  built-in legacy UI Text (no TextMeshPro resource import needed, no asset-pipeline dependency).
- Platform: macOS desktop (mouse + scroll wheel); layout targets a 16:9 landscape viewport like
  the phone screenshots. No mobile build.
- Art: fully procedural sprites generated in C# at startup (isometric blocks, roofs, icons).
  No external art, no copyrighted assets. Names avoid Supercell trademarks ("Village Clash").
- Online features (clans, Supercell ID, Facebook, Google Play, IAP, gems purchase) are
  presented as local mock UI only. Gems exist as a currency earned in-game, never bought.
- Multiplayer attacks are replaced by procedurally generated enemy villages with simulated
  defenses; the battle itself is real-time simulation, not a replay.
- Audio: a few procedurally synthesized sound effects, toggleable in Settings. No music track
  (the toggle exists and controls a soft procedural drone).

## 2. Core loop

1. **Build**: spend Gold/Elixir in the Shop to place buildings on a 40×40 isometric grid.
   Construction takes time and occupies a Builder. Upgrades likewise.
2. **Produce**: Gold Mines and Elixir Collectors accumulate while playing and while offline.
   Tap to collect into Storages (capped). Full collectors show a "Full!" bubble.
3. **Train**: Barracks train troops for Elixir into Army Camp capacity.
4. **Attack**: "Attack!" → "Find a Match" generates an enemy village scaled to the player's
   Town Hall level. Deploy troops by tapping outside the enemy base; troops path, attack,
   destroy; defenses shoot back. Destruction % → stars → loot and trophies.
5. **Progress**: XP → player level; trophies → league tier; Town Hall level gates the shop.

## 3. Screens (one per reference image)

| Screen | Reference | Content |
|---|---|---|
| Village HUD | img 3, 10 | Top-left: level badge + XP bar + name. Trophies + shield icon. Mail button. Top-center: Builders `1/2` [+], Shield time [+]. Top-right: Gold bar, Elixir bar, Gems [+]. Bottom-left: Attack!, Army (troop icon), Clan Castle icon. Bottom-right: Settings gear, News/scroll button, Shop. Left edge: collapsed orange side-tab (chat) that opens the Clans panel. |
| Build mode | img 3 | Selected building ghost on grid with green (valid) / red (invalid) footprint, green arrows, ✓ confirm and ✗ cancel buttons floating above it; drag to move. Also used when moving an existing building. |
| Shop | img 2, 6 | Modal: top icon tab row (Buildings, Army, Treasure/gems, Shield, League, Special offers) — only Buildings is populated; others show a "coming soon" mock. Sub-tabs Army / Resources / Defenses / Traps with red count badges (number affordable+unlocked). Cards: name, (i), picture, build time, Built x/y, price with currency icon. Grey cards for "Level N Town Hall Required". Bottom resource strip. |
| Building info | — (tap on building) | Small popup: name, level, HP, production/capacity/damage, buttons: Info, Upgrade (cost + time), Collect for producers, Train for Barracks, Move. |
| Army / Train | implied by img 1,7,8 | Modal: tabs Army (camp contents) / Train. Troop cards with cost, train time, counts, queue bar. |
| Attack menu | img 3 → img 1 | Modal: "Find a Match" (cost in gold scales with TH), "Practice" (fixed tutorial-style maps "Giant Smash", "Barbarian Rush" like img 7–8). |
| Battle | img 1, 7, 8 | Top-left: enemy name, Available Loot (gold, elixir), trophies at stake. Top-center: "Battle ends in: 2m 41s" (after first deployment, 3:00 otherwise "Scout"). Top-right: loot bars. Bottom: troop deployment bar with cards xN and dashed empty slots. Bottom-right: Overall Damage panel with 3 stars + %. Bottom-left: Surrender (red) / End Battle, and "Exit Tutorial" in practice. Deploy cursor ring. Health bars over units under attack. Loot coins/elixir pop out of destroyed storages. Hold to deploy repeatedly. |
| Battle result | — | Modal: stars, destruction %, loot gained, trophies ±, Return Home. |
| League | img 4 | Modal tabs My League / Top Clans / Top Players / Clan War League. "Collect 400 trophies to join a League" banner with hostess silhouette; league ladder Bronze 400+ … Titan 4100+ with tier icons and Divisions III II I. Top Players lists mock names with player on it. |
| Clans | img 9 | Modal tabs My Profile / Clans / Social. Clans: Search Clans / Bookmarks buttons, search field + Search + Advanced Options, list rows (badge with level, name, join type, trophies, members x/50, three icon chips). Mock data; search filters by name. My Profile shows player stats. |
| Settings | img 5 | Modal: blue "Village ID" banner (mock of Supercell ID) with Disconnected button; Music/Sound Effects On/Off toggles, Language button (English only); Facebook / Google Play Sign-In red "Disconnected" mock buttons; bottom grid: Privacy, Terms, Parent's Guide, Help, Credits, More Settings (More Settings has "Reset Village" with confirm). |

## 4. Game content

### 4.1 Resources
Gold, Elixir (stored in Town Hall + Storages, capped), Gems (uncapped; earned from removing
obstacles and achievements, spent to finish timers instantly: 1 gem per 60s remaining, min 1).

### 4.2 Buildings (all TH-gated; `Built x/y` max counts per TH level)
| Building | Size | Category | Role |
|---|---|---|---|
| Town Hall | 4×4 | — | Levels 1–5. Stores resources. Destroyed in battle = 1 star. |
| Builder's Hut | 2×2 | Resources | Each = 1 builder. Max 2 (gems for 2nd: 250). |
| Gold Mine | 3×3 | Resources | Produces gold/hour by level, internal cap. |
| Elixir Collector | 3×3 | Resources | Produces elixir/hour. |
| Gold Storage | 3×3 | Resources | Raises gold cap. |
| Elixir Storage | 3×3 | Resources | Raises elixir cap. |
| Barracks | 3×3 | Army | Trains troops; level unlocks troop types. |
| Army Camp | 4×4 | Army | Troop housing capacity. |
| Laboratory | 3×3 | Army | Upgrades troop levels (+hp/dmg) for elixir + time. |
| Cannon | 3×3 | Defenses | Single-target ground. |
| Archer Tower | 3×3 | Defenses | Longer range, single target. |
| Mortar | 3×3 | Defenses | Splash, min range, slow. (TH3) |
| Wall | 1×1 | Defenses | Blocks pathing; many per TH. |
| Bomb | 1×1 | Traps | Hidden; splash when troop steps near. (TH3) |
| Spring Trap | 1×1 | Traps | Hidden; removes up to 3 non-giant troops. (TH4) |

Obstacles (trees, bushes, rocks, gravestones) spawn around the village; removing one costs a
little gold/elixir, takes a builder, and may yield gems.

### 4.3 Troops
| Troop | Housing | Target | Notes |
|---|---|---|---|
| Barbarian | 1 | Any | Melee. |
| Archer | 1 | Any | Ranged, shoots over walls. |
| Giant | 5 | Defenses | Slow, tanky. |
| Goblin | 1 | Resources | Fast, 2× damage to resource buildings. |
| Wall Breaker | 2 | Walls | Suicide splash on walls. |

Each has 3 upgrade levels via Laboratory.

### 4.4 Progression
- XP from building/upgrading (= build seconds / 60, min 1) and from attacks. Level formula
  XP(n→n+1) = 50·n·(n+1)/2 ... simplified: `50 * n * n`.
- Trophies: win = +(10..30) based on stars, loss = −(10..20). Leagues: Bronze 400, Silver 800,
  Gold 1400, Crystal 2000, Master 2600, Champion 3200, Titan 4100.
- Loot available in an enemy base = fraction of its storages (gold 25%, elixir 25%) scaled by
  TH level; collectors contribute 50% of their stored amount.

### 4.5 Battle rules
- 3:00 battle timer starting at the first deployment; before that, free scouting.
- Troops can only be deployed on cells outside the enemy base's "no-deploy" zone (cells within
  1 tile of any building, like CoC's red outline).
- Troop AI: pick nearest valid target by its preference (walk distance via A* over the grid
  where walls are passable at high cost; if the chosen path crosses a wall the troop attacks
  that wall first). Attack when within range; re-target when target dies.
- Defense AI: target nearest troop in range; fire with cooldown; mortar deals radius splash
  with min range.
- Destruction % = destroyed non-wall buildings / total non-wall buildings. Stars: 50% = 1,
  Town Hall = 1, 100% = 3 (i.e. 100% gives all three).
- Battle ends: all troops dead, 100%, timer out, or Surrender/End Battle. Loot is granted for
  every destroyed resource building and Town Hall proportionally.
- Shield: after losing ≥ 30% in a (simulated) defense you get a 12h shield. Since there is no
  real PvP, the game simulates one incoming raid per 8h offline at most, with results shown in
  a "Defense log" toast at login. Shield is also purchasable for gems from the HUD [+].

### 4.6 Persistence
JSON save (`Application.persistentDataPath/save.json`) via `JsonUtility`, written on every
state change (debounced 1s) and on quit. Timestamps allow offline production and timer
completion. "Reset Village" in Settings deletes it.

## 5. Architecture

Strict split between **pure C# simulation** (testable, no UnityEngine except math structs)
and **Unity presentation**.

```
Assets/
  Scripts/
    Core/            pure C# — no MonoBehaviours
      Data/          GameData (static tables for buildings, troops, TH caps, leagues)
      Model/         GameState, VillageModel, BuildingInstance, TroopStack, Timers
      Systems/       Economy, Builders, Training, Progression, OfflineSim, SaveSystem
      Grid/          GridMap (40×40 occupancy), IsoMath, AStar
      Battle/        BattleSim (ticks at fixed dt), Units, Defenses, Projectiles,
                     EnemyBaseGenerator, LootCalculator
    Runtime/         MonoBehaviours
      Bootstrap      [RuntimeInitializeOnLoadMethod] builds camera, root, UI; loads save
      Village/       VillageRenderer (sprite per building/obstacle), BuildModeController,
                     CameraRig (pan/zoom), InputRouter
      Battle/        BattleRenderer (units, projectiles, health bars, loot pops), DeployInput
      Art/           SpriteFactory (procedural textures), Palette, IconFactory
      Audio/         Sfx (procedural AudioClips)
    UI/              uGUI built in code via UIKit helpers
      UIKit          panels, buttons, bars, cards, tabs, modal, toast — the CoC visual language
      Hud, ShopPanel, BuildingPopup, ArmyPanel, AttackPanel, BattleHud, ResultPanel,
      LeaguePanel, ClanPanel, SettingsPanel
  Tests/EditMode/    NUnit tests over Core
  Editor/            ProjectSetup (create empty Main scene, build settings, standalone build)
```

Data flow: UI → `GameController` (facade over Core systems) → `GameState` mutations →
`GameEvents` (C# events) → renderers/UI refresh. Battle runs `BattleSim.Tick(dt)` each frame
from `BattleController` and renderers read sim state.

Scene: a single empty `Main.unity`; everything is constructed at runtime so no YAML is
hand-edited. Camera is orthographic; world units = grid cells projected with 2:1 isometric
math (`IsoMath.CellToWorld`).

## 6. Visual language (UIKit)
- Palette: grass greens (#7cbf3f / #6aa832 checker), UI panel cream (#efe9dc) with dark brown
  (#3a2a1a) border, dark header strip (#2b2b2b), green buttons (#8dd13c, bevel), red buttons
  (#e2402f), blue info cards (#5fb9e6), grey locked cards (#9a9a9a), gold (#f4c531), elixir
  magenta (#d84cd0), gem green (#8bd04a).
- Text: bold, white with dark outline + shadow (Outline + Shadow components on legacy Text).
- Buttons: rounded rectangle sprite with lighter top half (bevel), 2px dark border, press
  scales 0.95.
- Cards: rounded, colored header area, large icon, footer price strip.
- All sprites are 9-sliced procedurally generated textures.

## 7. Error handling
- Save corrupt/unreadable → start new village and show a toast.
- Cannot place (overlap/out of bounds) → red footprint, confirm disabled.
- Insufficient resources → button greyed; tapping shows "Not enough Gold!" toast and a
  "Fill with gems?" prompt (gems cost = missing/10, min 1).
- No builder free → "All builders are busy" toast with time-to-free.
- Army empty when attacking → "Train troops first" toast.

## 8. Testing
- EditMode NUnit tests (run with `unity test --mode EditMode`): grid placement rules, economy
  caps, offline production, builder timers, gem-finish pricing, training queue, XP/level,
  trophies/league, A* path through walls, battle sim determinism (seeded enemy base, scripted
  deployment reaches ≥1 star), save round-trip.
- Smoke: headless build succeeds; standalone app launches and a screenshot is captured.

## 9. Out of scope
Real networking, real clans/chat, Clan Castle troops, spells, heroes, air units, Builder Base,
replays, achievements list, localization beyond English, IAP, ads.
