using System;
using System.Collections.Generic;

namespace VillageClash.Core
{
    /// <summary>Static balance tables. All timers are short so the game is playable in one sitting.</summary>
    public static class GameData
    {
        public const int GridSize = 40;
        public const int MaxTownHall = 5;
        public const int MaxBuilders = 2;
        public const int SecondBuilderGemCost = 250;
        public const int ShieldGemCost = 100;
        public const int ShieldSeconds = 12 * 3600;
        public const int MatchCostGold = 50;
        public const int BattleSeconds = 180;
        public const int StartingGold = 750;
        public const int StartingElixir = 750;
        public const int StartingGems = 500;

        static readonly Dictionary<BuildingType, BuildingDef> _buildings = new Dictionary<BuildingType, BuildingDef>();
        static readonly Dictionary<TroopType, TroopDef> _troops = new Dictionary<TroopType, TroopDef>();

        public static IEnumerable<BuildingDef> AllBuildings => _buildings.Values;
        public static IEnumerable<TroopDef> AllTroops => _troops.Values;

        public static BuildingDef Building(BuildingType t) => _buildings[t];
        public static TroopDef Troop(TroopType t) => _troops[t];

        public static int MaxCount(BuildingType t, int thLevel)
        {
            var arr = Building(t).MaxCountByTh;
            int i = Math.Max(1, Math.Min(thLevel, arr.Length)) - 1;
            return arr[i];
        }

        public static int ObstacleRemoveCostGold(ObstacleKind k) => k == ObstacleKind.Tree ? 0 : (k == ObstacleKind.Bush ? 25 : k == ObstacleKind.Rock ? 100 : 50);
        public static int ObstacleRemoveCostElixir(ObstacleKind k) => k == ObstacleKind.Tree ? 50 : 0;
        public static int ObstacleRemoveSeconds(ObstacleKind k) => k == ObstacleKind.Rock ? 30 : 10;
        public static int ObstacleSize(ObstacleKind k) => k == ObstacleKind.Bush || k == ObstacleKind.Stump ? 1 : 2;

        static LevelStats L(int hp, int gold, int elixir, int secs, int th, int prod = 0, int cap = 0, int dmg = 0, float range = 0, float minRange = 0, float cd = 1f, float splash = 0, int gems = 0)
            => new LevelStats { Hp = hp, CostGold = gold, CostElixir = elixir, BuildSeconds = secs, ThRequired = th, ProductionPerHour = prod, Capacity = cap, Damage = dmg, Range = range, MinRange = minRange, AttackCooldown = cd, SplashRadius = splash, CostGems = gems };

        static void Add(BuildingDef d) => _buildings[d.Type] = d;

        static GameData()
        {
            Add(new BuildingDef
            {
                Type = BuildingType.TownHall, Name = "Town Hall", Category = BuildingCategory.Resource, Size = 4,
                Description = "The heart of your village. Upgrade it to unlock new buildings and higher levels.",
                MaxCountByTh = new[] { 1, 1, 1, 1, 1 }, ProducesKind = ResourceKind.Gold,
                Levels = new[]
                {
                    L(450, 0, 0, 0, 1, cap: 1000),
                    L(1600, 1000, 0, 60, 1, cap: 1500),
                    L(1850, 4000, 0, 300, 2, cap: 2500),
                    L(2100, 25000, 0, 900, 3, cap: 5000),
                    L(2400, 150000, 0, 1800, 4, cap: 10000),
                }
            });
            Add(new BuildingDef
            {
                Type = BuildingType.BuilderHut, Name = "Builder's Hut", Category = BuildingCategory.Resource, Size = 2,
                Description = "Houses a builder. Each builder can work on one project at a time.",
                MaxCountByTh = new[] { 2, 2, 2, 2, 2 },
                Levels = new[] { L(250, 0, 0, 0, 1, gems: SecondBuilderGemCost) }
            });
            Add(new BuildingDef
            {
                Type = BuildingType.GoldMine, Name = "Gold Mine", Category = BuildingCategory.Resource, Size = 3,
                Description = "Mines gold. Collect it before the mine fills up!", ProducesKind = ResourceKind.Gold,
                MaxCountByTh = new[] { 2, 3, 4, 5, 6 },
                Levels = new[]
                {
                    L(400, 0, 150, 10, 1, prod: 200, cap: 500),
                    L(440, 0, 300, 60, 1, prod: 400, cap: 1000),
                    L(480, 0, 700, 180, 2, prod: 600, cap: 1500),
                    L(520, 0, 1400, 600, 2, prod: 800, cap: 2500),
                    L(560, 0, 3500, 1800, 3, prod: 1000, cap: 5000),
                }
            });
            Add(new BuildingDef
            {
                Type = BuildingType.ElixirCollector, Name = "Elixir Collector", Category = BuildingCategory.Resource, Size = 3,
                Description = "Pumps elixir from the ground. Collect it before it fills up!", ProducesKind = ResourceKind.Elixir,
                MaxCountByTh = new[] { 2, 3, 4, 5, 6 },
                Levels = new[]
                {
                    L(400, 150, 0, 10, 1, prod: 200, cap: 500),
                    L(440, 300, 0, 60, 1, prod: 400, cap: 1000),
                    L(480, 700, 0, 180, 2, prod: 600, cap: 1500),
                    L(520, 1400, 0, 600, 2, prod: 800, cap: 2500),
                    L(560, 3500, 0, 1800, 3, prod: 1000, cap: 5000),
                }
            });
            Add(new BuildingDef
            {
                Type = BuildingType.GoldStorage, Name = "Gold Storage", Category = BuildingCategory.Resource, Size = 3,
                Description = "Stores your gold. Upgrade to hold more.", ProducesKind = ResourceKind.Gold,
                MaxCountByTh = new[] { 1, 1, 2, 2, 2 },
                Levels = new[]
                {
                    L(400, 0, 300, 30, 1, cap: 1500),
                    L(600, 0, 750, 120, 2, cap: 3000),
                    L(800, 0, 1500, 300, 2, cap: 6000),
                    L(1000, 0, 3000, 900, 3, cap: 12000),
                    L(1200, 0, 6000, 1800, 4, cap: 25000),
                }
            });
            Add(new BuildingDef
            {
                Type = BuildingType.ElixirStorage, Name = "Elixir Storage", Category = BuildingCategory.Resource, Size = 3,
                Description = "Stores your elixir. Upgrade to hold more.", ProducesKind = ResourceKind.Elixir,
                MaxCountByTh = new[] { 1, 1, 2, 2, 2 },
                Levels = new[]
                {
                    L(400, 300, 0, 30, 1, cap: 1500),
                    L(600, 750, 0, 120, 2, cap: 3000),
                    L(800, 1500, 0, 300, 2, cap: 6000),
                    L(1000, 3000, 0, 900, 3, cap: 12000),
                    L(1200, 6000, 0, 1800, 4, cap: 25000),
                }
            });
            Add(new BuildingDef
            {
                Type = BuildingType.Barracks, Name = "Barracks", Category = BuildingCategory.Army, Size = 3,
                Description = "Trains troops. Upgrade to unlock new troop types.",
                MaxCountByTh = new[] { 1, 1, 2, 2, 2 },
                Levels = new[]
                {
                    L(250, 0, 200, 10, 1),
                    L(290, 0, 1000, 60, 1),
                    L(330, 0, 2500, 300, 2),
                    L(370, 0, 5000, 900, 3),
                    L(420, 0, 10000, 1800, 4),
                }
            });
            Add(new BuildingDef
            {
                Type = BuildingType.ArmyCamp, Name = "Army Camp", Category = BuildingCategory.Army, Size = 4,
                Description = "Houses your trained troops. Upgrade to hold a bigger army.",
                MaxCountByTh = new[] { 1, 1, 2, 2, 3 },
                Levels = new[]
                {
                    L(250, 0, 250, 10, 1, cap: 20),
                    L(270, 0, 1500, 300, 2, cap: 30),
                    L(290, 0, 5000, 900, 3, cap: 35),
                    L(310, 0, 15000, 1800, 4, cap: 40),
                }
            });
            Add(new BuildingDef
            {
                Type = BuildingType.Laboratory, Name = "Laboratory", Category = BuildingCategory.Army, Size = 3,
                Description = "Research upgrades to make your troops stronger.",
                MaxCountByTh = new[] { 0, 0, 1, 1, 1 },
                Levels = new[]
                {
                    L(500, 0, 500, 60, 3),
                    L(550, 0, 2500, 600, 3),
                    L(600, 0, 10000, 1800, 4),
                }
            });
            Add(new BuildingDef
            {
                Type = BuildingType.Cannon, Name = "Cannon", Category = BuildingCategory.Defense, Size = 3,
                Description = "Reliable single-target defense that fires at ground troops.",
                MaxCountByTh = new[] { 2, 2, 3, 4, 5 },
                Levels = new[]
                {
                    L(420, 250, 0, 10, 1, dmg: 7, range: 9, cd: 0.8f),
                    L(470, 1000, 0, 60, 1, dmg: 9, range: 9, cd: 0.8f),
                    L(520, 4000, 0, 300, 2, dmg: 12, range: 9, cd: 0.8f),
                    L(570, 16000, 0, 900, 3, dmg: 15, range: 9, cd: 0.8f),
                    L(620, 50000, 0, 1800, 4, dmg: 20, range: 9, cd: 0.8f),
                }
            });
            Add(new BuildingDef
            {
                Type = BuildingType.ArcherTower, Name = "Archer Tower", Category = BuildingCategory.Defense, Size = 3,
                Description = "Long-range tower with fast arrows.",
                MaxCountByTh = new[] { 0, 1, 1, 2, 3 },
                Levels = new[]
                {
                    L(380, 1000, 0, 60, 2, dmg: 6, range: 10, cd: 0.5f),
                    L(420, 2000, 0, 300, 2, dmg: 8, range: 10, cd: 0.5f),
                    L(460, 5000, 0, 900, 3, dmg: 10, range: 10, cd: 0.5f),
                    L(500, 20000, 0, 1800, 4, dmg: 13, range: 10, cd: 0.5f),
                    L(540, 80000, 0, 3600, 5, dmg: 15, range: 10, cd: 0.5f),
                }
            });
            Add(new BuildingDef
            {
                Type = BuildingType.Mortar, Name = "Mortar", Category = BuildingCategory.Defense, Size = 3,
                Description = "Lobs explosive shells that splash groups of troops. Cannot hit nearby targets.",
                MaxCountByTh = new[] { 0, 0, 1, 1, 2 },
                Levels = new[]
                {
                    L(400, 5000, 0, 300, 3, dmg: 20, range: 11, minRange: 4, cd: 5f, splash: 1.5f),
                    L(450, 15000, 0, 1800, 4, dmg: 25, range: 11, minRange: 4, cd: 5f, splash: 1.5f),
                    L(500, 40000, 0, 3600, 5, dmg: 30, range: 11, minRange: 4, cd: 5f, splash: 1.5f),
                }
            });
            Add(new BuildingDef
            {
                Type = BuildingType.Wall, Name = "Wall", Category = BuildingCategory.Defense, Size = 1,
                Description = "Walls slow attackers down. Build them around your defenses.",
                MaxCountByTh = new[] { 25, 50, 75, 100, 125 },
                Levels = new[]
                {
                    L(300, 50, 0, 0, 1),
                    L(500, 200, 0, 0, 2),
                    L(700, 800, 0, 0, 3),
                    L(900, 2000, 0, 0, 4),
                    L(1400, 5000, 0, 0, 5),
                }
            });
            Add(new BuildingDef
            {
                Type = BuildingType.Bomb, Name = "Bomb", Category = BuildingCategory.Trap, Size = 1,
                Description = "Hidden explosive. Blows up when attackers step near it.",
                MaxCountByTh = new[] { 0, 0, 2, 2, 4 },
                Levels = new[]
                {
                    L(1, 400, 0, 0, 3, dmg: 20, splash: 1.5f),
                    L(1, 1000, 0, 0, 4, dmg: 24, splash: 1.5f),
                    L(1, 2500, 0, 0, 5, dmg: 29, splash: 1.5f),
                }
            });
            Add(new BuildingDef
            {
                Type = BuildingType.SpringTrap, Name = "Spring Trap", Category = BuildingCategory.Trap, Size = 1,
                Description = "Flings small troops right off the battlefield.",
                MaxCountByTh = new[] { 0, 0, 0, 2, 4 },
                Levels = new[] { L(1, 2000, 0, 0, 4, dmg: 0, splash: 0.8f) }
            });

            _troops[TroopType.Barbarian] = new TroopDef
            {
                Type = TroopType.Barbarian, Name = "Barbarian", Housing = 1,
                Description = "A fearless melee fighter. Attacks the nearest building.",
                Hp = new[] { 45, 54, 65 }, Dps = new[] { 8, 11, 14 }, Range = 0.5f, Speed = 2f,
                Preference = TargetPreference.Any, PreferenceMultiplier = 1f,
                CostElixir = new[] { 25, 40, 60 }, TrainSeconds = 5, BarracksLevel = 1,
                UpgradeCostElixir = new[] { 1000, 2500 }, UpgradeSeconds = new[] { 300, 900 }, LabLevelRequired = new[] { 1, 2 },
            };
            _troops[TroopType.Archer] = new TroopDef
            {
                Type = TroopType.Archer, Name = "Archer", Housing = 1,
                Description = "Shoots over walls from a distance.",
                Hp = new[] { 20, 23, 28 }, Dps = new[] { 7, 9, 12 }, Range = 3.5f, Speed = 2.4f,
                Preference = TargetPreference.Any, PreferenceMultiplier = 1f,
                CostElixir = new[] { 50, 80, 120 }, TrainSeconds = 8, BarracksLevel = 2,
                UpgradeCostElixir = new[] { 1500, 4000 }, UpgradeSeconds = new[] { 300, 900 }, LabLevelRequired = new[] { 1, 2 },
            };
            _troops[TroopType.Giant] = new TroopDef
            {
                Type = TroopType.Giant, Name = "Giant", Housing = 5,
                Description = "Slow but tough. Targets defenses first.",
                Hp = new[] { 300, 360, 430 }, Dps = new[] { 11, 14, 19 }, Range = 0.5f, Speed = 1.2f,
                Preference = TargetPreference.Defenses, PreferenceMultiplier = 1f,
                CostElixir = new[] { 250, 750, 1250 }, TrainSeconds = 30, BarracksLevel = 3,
                UpgradeCostElixir = new[] { 2500, 6000 }, UpgradeSeconds = new[] { 600, 1800 }, LabLevelRequired = new[] { 2, 3 },
            };
            _troops[TroopType.Goblin] = new TroopDef
            {
                Type = TroopType.Goblin, Name = "Goblin", Housing = 1,
                Description = "Fast and greedy. Deals double damage to resource buildings.",
                Hp = new[] { 25, 30, 36 }, Dps = new[] { 11, 14, 19 }, Range = 0.5f, Speed = 3.2f,
                Preference = TargetPreference.Resources, PreferenceMultiplier = 2f,
                CostElixir = new[] { 25, 40, 60 }, TrainSeconds = 10, BarracksLevel = 4,
                UpgradeCostElixir = new[] { 1500, 4000 }, UpgradeSeconds = new[] { 300, 900 }, LabLevelRequired = new[] { 1, 2 },
            };
            _troops[TroopType.WallBreaker] = new TroopDef
            {
                Type = TroopType.WallBreaker, Name = "Wall Breaker", Housing = 2,
                Description = "Runs at walls and blows them up. Massive damage to walls.",
                Hp = new[] { 20, 24, 29 }, Dps = new[] { 6, 10, 15 }, Range = 0.5f, Speed = 2.4f,
                Preference = TargetPreference.Walls, PreferenceMultiplier = 40f,
                CostElixir = new[] { 250, 400, 600 }, TrainSeconds = 20, BarracksLevel = 5,
                UpgradeCostElixir = new[] { 3000, 7000 }, UpgradeSeconds = new[] { 600, 1800 }, LabLevelRequired = new[] { 2, 3 },
            };
        }
    }
}
