namespace VillageClash.Core
{
    public sealed class ShopStatus
    {
        public BuildingType Type;
        public int Built;
        public int Max;
        public int RequiredTh;      // lowest TH level at which Max > 0
        public bool Unlocked;       // Max > 0 at current TH
        public int CostGold;
        public int CostElixir;
        public int CostGems;
        public int BuildSeconds;
        public bool Affordable;
        public bool CanBuildMore => Unlocked && Built < Max;
        public string LockText => Unlocked ? null : $"Level {RequiredTh} Town Hall Required";
    }

    public static class ShopRules
    {
        public static ShopStatus Status(GameState s, BuildingType type)
        {
            var def = GameData.Building(type);
            int th = s.TownHallLevel;
            var st = def.Stats(1);
            int requiredTh = GameData.MaxTownHall + 1;
            for (int t = 1; t <= GameData.MaxTownHall; t++) if (GameData.MaxCount(type, t) > 0) { requiredTh = t; break; }
            int built = s.Village.Count(type);
            int gems = st.CostGems;
            // Builder's Hut: the first is free (starter), subsequent ones cost gems only.
            if (type == BuildingType.BuilderHut) gems = built == 0 ? 0 : GameData.SecondBuilderGemCost;
            return new ShopStatus
            {
                Type = type,
                Built = built,
                Max = GameData.MaxCount(type, th),
                RequiredTh = requiredTh,
                Unlocked = GameData.MaxCount(type, th) > 0,
                CostGold = st.CostGold,
                CostElixir = st.CostElixir,
                CostGems = gems,
                BuildSeconds = st.BuildSeconds,
                Affordable = Economy.CanAfford(s, st.CostGold, st.CostElixir, gems),
            };
        }

        /// <summary>Error message for upgrading a building to its next level, or null when allowed (ignores builders/cost).</summary>
        public static string UpgradeGateError(GameState s, BuildingInstance b)
        {
            if (b.Constructing) return Msg.AlreadyUpgrading;
            if (b.Level >= b.Def.MaxLevel) return Msg.MaxLevel;
            var next = b.Def.Stats(b.Level + 1);
            if (b.Type != BuildingType.TownHall && next.ThRequired > s.TownHallLevel) return Msg.TownHallRequired;
            return null;
        }
    }
}
