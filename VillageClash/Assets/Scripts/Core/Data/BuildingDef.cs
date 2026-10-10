namespace VillageClash.Core
{
    /// <summary>Per-level stats for a building. Costs are for *reaching* this level (level 1 = build cost).</summary>
    public sealed class LevelStats
    {
        public int Hp;
        public int CostGold;
        public int CostElixir;
        public int CostGems;
        public int BuildSeconds;
        public int ThRequired;
        public int ProductionPerHour;
        public int Capacity;
        public int Damage;
        public float Range;
        public float MinRange;
        public float AttackCooldown;
        public float SplashRadius;

        public int XpReward => System.Math.Max(1, (int)System.Math.Sqrt(BuildSeconds));
    }

    public sealed class BuildingDef
    {
        public BuildingType Type;
        public string Name;
        public string Description;
        public BuildingCategory Category;
        public int Size;
        public LevelStats[] Levels;
        /// <summary>Max count indexed by Town Hall level (index 0 = TH1 ... index 4 = TH5).</summary>
        public int[] MaxCountByTh;
        public ResourceKind ProducesKind;
        public bool IsDefense => Category == BuildingCategory.Defense && Type != BuildingType.Wall;
        public bool IsTrap => Category == BuildingCategory.Trap;
        public bool IsWall => Type == BuildingType.Wall;
        public int MaxLevel => Levels.Length;
        public LevelStats Stats(int level) => Levels[System.Math.Max(1, System.Math.Min(level, MaxLevel)) - 1];
    }
}
