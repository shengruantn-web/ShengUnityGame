# Village Clash Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A playable single-player Clash-of-Clans-style village builder + raid game in Unity 6 LTS, runnable on this Mac, reproducing the ten reference screens.

**Architecture:** Pure-C# simulation core (`Assets/Scripts/Core`, no MonoBehaviours, NUnit-tested headlessly) drives a thin Unity presentation layer (`Runtime`, `UI`) that builds everything at startup from code: procedural sprites, uGUI panels, an orthographic iso camera. One empty scene; no hand-edited YAML.

**Tech Stack:** Unity 6000.3.26f1 (2D template), uGUI + legacy `UnityEngine.UI.Text`, Unity Test Framework (NUnit, EditMode), `unity` CLI for create/test/build, git.

**Spec:** `docs/superpowers/specs/2026-10-10-village-clash-design.md`

## Global Constraints
- Unity 6000.3.26f1 only; no extra UPM packages beyond the 2D template + test framework.
- No external art or audio assets; everything procedural in C#.
- Core (`Assets/Scripts/Core/**`) must not reference `UnityEngine` except `Mathf`/`Vector2Int`-free math: use `System` only (own `Int2` struct) so tests stay pure.
- Names: project/product "Village Clash"; no "Clash of Clans", "Supercell" strings in UI.
- Every state mutation goes through `GameController` and raises `GameEvents.StateChanged`.
- Save file: `Application.persistentDataPath/save.json`, JsonUtility, schema versioned (`version:1`).

## Review Focus
1. Offline time far in the future/past (clock changed): production must clamp to cap and never go negative; timers finishing in the past complete once. Test in Task 4.
2. Placing a building partially off-grid or over an obstacle must be rejected, including the wall's 1×1 case at the grid edge. Test in Task 3.
3. Deploying a troop on a no-deploy cell or with zero troops left must be ignored, not crash. Test in Task 7.
4. Battle with an enemy base fully enclosed by walls must still end (troops break walls, or timer). Test in Task 7.
5. Save file from an unknown version or garbage must start a fresh village without throwing. Test in Task 4.

---

### Task 1: Project bootstrap
**Files:** Unity project `VillageClash/` created by CLI; `.gitignore`; `Assets/Editor/ProjectSetup.cs`; `Assets/Scripts/Core/VillageClash.Core.asmdef`; `Assets/Scripts/VillageClash.Runtime.asmdef`; `Assets/Tests/EditMode/VillageClash.Tests.asmdef`; `Assets/Editor/VillageClash.Editor.asmdef`.
- [ ] `unity projects create "VillageClash" --path /Users/Sheng/2026VibeCode/Oct/UnityGame --editor-version 6000.3.26f1 --template com.unity.template.2d` (confirm id via `unity templates list`).
- [ ] `git init -b main` at `UnityGame/`, Unity gitignore adapted to subfolder (`**/[Ll]ibrary/` etc.).
- [ ] asmdefs: Core (no refs, `noEngineReferences: true`), Runtime (refs Core), Tests (Editor-only, refs Core + nunit), Editor (refs Runtime).
- [ ] `ProjectSetup.CreateMainScene()` makes empty `Assets/Scenes/Main.unity` with only a Camera, sets build scenes; `ProjectSetup.BuildMac()` builds `Builds/VillageClash.app`.
- [ ] Smoke test `CoreSmokeTests.AsmdefCompiles` (`Assert.Pass()`), run `unity test VillageClash --mode EditMode` → exit 0. Commit.

### Task 2: Core data tables
**Files:** `Core/Data/Int2.cs`, `Core/Data/BuildingType.cs` (enum), `Core/Data/TroopType.cs`, `Core/Data/BuildingDef.cs`, `Core/Data/TroopDef.cs`, `Core/Data/GameData.cs`, `Core/Data/Leagues.cs`. Test: `Tests/EditMode/GameDataTests.cs`.
**Produces:** `GameData.Building(BuildingType)` → `BuildingDef { Type, Name, Category, Size, MaxLevel, LevelStats[] }` where `LevelStats { Hp, CostGold, CostElixir, CostGems, BuildSeconds, ThRequired, Production/hr, Capacity, Damage, Range, MinRange, AttackCooldown, Splash }`; `GameData.MaxCount(BuildingType, thLevel)`; `GameData.Troop(TroopType)` → `TroopDef { Housing, Hp[], Dps[], Range, Speed, Preference, CostElixir[], TrainSeconds, BarracksLevel }`; `Leagues.ForTrophies(int)` → `(name, minTrophies)`; `Leagues.All`.
- [ ] Tests: every BuildingType has a def with MaxLevel ≥ 1 and stats for each level; TH1 allows Cannon ×2 but Mortar ×0; `Leagues.ForTrophies(399)` is "Unranked", `(400)` Bronze, `(4100)` Titan. Implement tables. Commit.

### Task 3: Grid + village model
**Files:** `Core/Grid/GridMap.cs`, `Core/Grid/IsoMath.cs`, `Core/Model/BuildingInstance.cs`, `Core/Model/ObstacleInstance.cs`, `Core/Model/VillageModel.cs`. Test: `GridTests.cs`, `VillageModelTests.cs`.
**Produces:** `GridMap(int size=40)`: `bool CanPlace(Int2 origin, int size, int ignoreId=-1)`, `Occupy/Free(id, origin, size)`, `int OccupantAt(Int2)`; `VillageModel { List<BuildingInstance> Buildings; List<ObstacleInstance> Obstacles; GridMap Grid; int NextId }` with `TryPlace(BuildingType, Int2) → BuildingInstance?`, `TryMove(id, Int2)`, `Remove(id)`; `BuildingInstance { Id, Type, Level, Origin, Size, bool Constructing, long FinishAtUnix, double Stored }`; `IsoMath.CellToWorld(float cx, float cy) → (x,y)` with 2:1 iso (tile w=1, h=0.5) and `WorldToCell`.
- [ ] Tests: place 3×3 at (0,0) ok, overlapping rejected, off-grid (38,38 size 3) rejected, wall at (39,39) ok, move onto own footprint ok, obstacle blocks placement; iso round-trip. Commit.

### Task 4: GameState, economy, builders, timers, save
**Files:** `Core/Model/GameState.cs`, `Core/Systems/Economy.cs`, `Core/Systems/Builders.cs`, `Core/Systems/Progression.cs`, `Core/Systems/SaveSystem.cs` (serializable DTO `SaveData` + `ToDto/FromDto`), `Core/Systems/Clock.cs` (`IClock { long NowUnix }`). Test: `EconomyTests.cs`, `BuildersTests.cs`, `SaveTests.cs`, `ProgressionTests.cs`.
**Produces:** `GameState { PlayerName, Gold, Elixir, Gems, Xp, Level, Trophies, ShieldUntilUnix, LastSeenUnix, Village, Army (Dictionary<TroopType,int>), TrainQueue (List<TrainJob>), TroopLevels, LabJob }`; `Economy.Tick(state, now)` accrues production into each producer's `Stored` (capped at its capacity), `Economy.Collect(state, buildingId)` moves into Gold/Elixir capped by `Economy.GoldCap(state)`; `Economy.CanAfford/Spend`; `Builders.Total(state)`, `Builders.Busy(state)`, `Builders.Tick(state, now)` completes constructions (level++ / Constructing=false, awards XP); `Builders.StartBuild/StartUpgrade/StartRemoveObstacle`; `Builders.GemFinishCost(seconds) = max(1, ceil(seconds/60))`; `Progression.AddXp`, `Progression.XpToNext(level) = 50*level*level`; `SaveSystem.Serialize(state) → string`, `Deserialize(string) → GameState` (throws on bad version), `NewGame(seed)` creates TH1 village with starter layout (TH, 1 gold mine, 1 elixir collector, 1 cannon, 1 barracks, 1 army camp, builder hut, obstacles).
- [ ] Tests: 1h offline on level-1 gold mine (200/hr, cap 500) gives 200; 10h gives 500 (cap); clock going backwards gives 0 and no exception; collect respects storage cap; spending below zero rejected; build consumes builder, completes at FinishAt, second build with 1 builder rejected; gem finish cost 61s → 2; XP 50→level 2; save round-trips all fields incl. army; garbage string → `null` from `SaveSystem.TryDeserialize`. Commit.

### Task 5: Training, lab, offline raid sim, shop rules
**Files:** `Core/Systems/Training.cs`, `Core/Systems/Laboratory.cs`, `Core/Systems/ShopRules.cs`, `Core/Systems/DefenseLog.cs`. Tests: `TrainingTests.cs`, `ShopRulesTests.cs`.
**Produces:** `Training.CampCapacity(state)`, `Training.ArmyHousing(state)`, `Training.Enqueue(state, TroopType, now) → bool` (elixir, housing incl. queue, barracks level), `Training.Tick(state, now)` (sequential queue; jobs complete into Army), `Training.Cancel(state, index)`; `ShopRules.Status(state, BuildingType) → ShopStatus { Built, Max, ThRequired, Unlocked, CostGold, CostElixir, BuildSeconds, Affordable }`; `Laboratory.Start/Tick`; `DefenseLog.SimulateOffline(state, elapsedSeconds, rng) → DefenseReport?` (≤1 raid per 8h, steals ≤10% of each stored resource, −trophies, sets shield 12h if ≥30% "destroyed").
- [ ] Tests: cannot enqueue beyond camp capacity; queue completes in order; TH1 shop shows Mortar locked "Level 3 Town Hall Required" and Cannon 1/2 built. Commit.

### Task 6: Enemy base generator + A*
**Files:** `Core/Battle/EnemyBaseGenerator.cs`, `Core/Grid/AStar.cs`. Tests: `EnemyBaseTests.cs`, `AStarTests.cs`.
**Produces:** `EnemyBaseGenerator.Generate(int thLevel, int seed) → VillageModel` (TH centered, walls ring, defenses inside, storages/collectors around, obstacles sparse) with every building `Level` scaled and `Stored` loot; `AStar.FindPath(GridMap, Int2 from, Int2 to, Func<Int2,float> cellCost, int maxNodes) → List<Int2>` (8-neighbour, wall cells cost 50, building cells impassable except the goal's footprint ring).
- [ ] Tests: generated base is valid (no overlaps, inside grid, has TH), deterministic for same seed; A* through a wall ring finds a path whose cells include a wall cell; no path when goal unreachable → empty list. Commit.

### Task 7: BattleSim
**Files:** `Core/Battle/BattleSim.cs`, `Core/Battle/Unit.cs`, `Core/Battle/DefenseState.cs`, `Core/Battle/Projectile.cs`, `Core/Battle/LootCalculator.cs`, `Core/Battle/BattleResult.cs`. Tests: `BattleSimTests.cs`.
**Produces:** `BattleSim(VillageModel enemy, Dictionary<TroopType,int> army, int[] troopLevels, int seed)`; `bool CanDeploy(Int2 cell)`; `bool Deploy(TroopType, float x, float y)`; `Tick(float dt)`; properties `Units`, `Buildings` (hp), `Projectiles`, `DestructionPercent`, `Stars`, `TimeLeft`, `Started`, `Ended`, `LootGold/LootElixir` (earned so far), `Events` queue (`UnitSpawned, UnitDied, BuildingDestroyed, Shot(from,to), LootPop(cell,kind,amount)`); `End(surrender)` → `BattleResult { Stars, Percent, Gold, Elixir, TrophyDelta, Won }`; `LootCalculator.Available(enemy, thLevel)`.
- [ ] Tests: deploy on no-deploy cell returns false; deploy with 0 left returns false; 20 level-1 barbarians vs seeded TH1 base, ticked at 0.05 for 180s, reaches ≥ 50% and ≥1 star; fully walled TH1 base with 5 wall breakers + 10 barbarians: battle ends (Ended true) before 200s; destruction excludes walls; result TrophyDelta positive on 1+ star, negative on 0. Commit.

### Task 8: GameController facade + GameEvents + Clock
**Files:** `Core/GameController.cs`, `Core/GameEvents.cs`. Test: `GameControllerTests.cs`.
**Produces:** `GameController(IClock clock, Func<string> load, Action<string> save)`: `State`, `Load()`, `Tick()` (economy, builders, training, lab, debounced save), `Collect(id)`, `BeginPlace(type)`/`ConfirmPlace(type, origin)`, `MoveBuilding(id, origin)`, `Upgrade(id)`, `FinishWithGems(id)`, `RemoveObstacle(id)`, `Train(type)`, `CancelTrain(i)`, `BuyShield()`, `BuyBuilder()`, `StartMatch(seed) → BattleSim` (deducts match cost), `StartPractice(mapId) → BattleSim`, `FinishBattle(BattleResult)` (apply loot/trophies/xp, consume deployed troops), `Reset()`. Events: `StateChanged`, `Toast(string)`.
- [ ] Tests: ConfirmPlace spends & occupies builder; Upgrade with no builder → Toast "All builders are busy"; FinishBattle adds loot capped by storage and trophies. Commit.

### Task 9: Procedural art + audio
**Files:** `Runtime/Art/Palette.cs`, `Runtime/Art/SpriteFactory.cs` (rounded rect 9-slice, circle, iso diamond tile, iso block building composites per BuildingType/level, troop blobs, icons: gold coin, elixir drop, gem, trophy, shield, hammer, sword, gear, scroll, star, builder face), `Runtime/Audio/Sfx.cs` (AudioClip.Create tones: tap, collect, build, shot, hit, destroy, win, lose; music drone).
**Produces:** `SpriteFactory.Get(string key)` cached; `SpriteFactory.Building(BuildingType, int level, bool ghost)`; `SpriteFactory.Troop(TroopType)`; `SpriteFactory.Obstacle(kind)`; `Sfx.Play(SfxId)`, `Sfx.MusicOn/SfxOn`.
- [ ] No unit tests (visual); verify by running the app in Task 13. Commit.

### Task 10: UIKit + HUD + camera + village rendering
**Files:** `UI/UIKit.cs` (Canvas root 1920×1080 scaler, `Panel`, `Button(text, color, size, onClick)`, `IconButton`, `Label`, `ResourceBar`, `Modal(title)` with X, `Tabs`, `Card`, `Toast`, `ProgressBar`, `Badge`), `UI/Hud.cs`, `Runtime/Village/CameraRig.cs` (drag pan, scroll zoom, clamp), `Runtime/Village/VillageRenderer.cs` (SpriteRenderer per building/obstacle, y-sorting, construction progress bars + timer text, "Full!"/resource bubbles, tap detection), `Runtime/Village/InputRouter.cs` (click vs drag, hit-test cells), `Runtime/Bootstrap.cs` (`[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`, builds camera, controller with `SystemClock` + file IO, HUD, renderer; `Application.quitting` save).
- [ ] Visual acceptance: village renders with HUD matching reference layout. Commit.

### Task 11: Build mode, shop, building popup, army panel, attack menu
**Files:** `Runtime/Village/BuildModeController.cs` (ghost, green/red footprint, ✓/✗ floating, drag), `UI/ShopPanel.cs` (icon tabs, sub-tabs w/ badges, cards, locked grey, resource strip), `UI/BuildingPopup.cs`, `UI/ArmyPanel.cs` (Army/Train tabs, troop cards, queue), `UI/AttackPanel.cs` (Find a Match, Practice list), `UI/ConfirmDialog.cs`.
- [ ] Commit after each panel.

### Task 12: Battle presentation + result
**Files:** `Runtime/Battle/BattleController.cs` (owns BattleSim, ticks, deploy input incl. hold-to-deploy, camera on enemy base), `Runtime/Battle/BattleRenderer.cs` (units, hp bars, projectiles, loot pops, destroyed rubble, no-deploy tint), `UI/BattleHud.cs` (loot, timer, troop bar with dashed empty slots, Overall Damage stars/%, Surrender/End Battle, Exit Tutorial), `UI/ResultPanel.cs`.
- [x] Commit.

### Task 13: League, Clans, Settings, defense log, polish
**Files:** `UI/LeaguePanel.cs`, `UI/ClanPanel.cs` (mock data, search), `UI/SettingsPanel.cs` (toggles, mock connect buttons, More Settings → Reset Village), `UI/DefenseLogToast.cs`; `Editor/ProjectSetup.BuildMac()`.
- [x] Headless build via `unity build … --execute-method ProjectSetup.BuildMac --target StandaloneOSX`; launch app, capture screenshot with `screencapture`, fix visual issues. Commit.

### Task 14: Verification
- [x] `unity test --mode EditMode` all green; build succeeds; app launches; smoke a full loop (place, collect, train, attack, result, save reload). Write `README.md` with how to open/run/test/build. Final commit.
