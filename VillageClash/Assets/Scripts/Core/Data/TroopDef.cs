namespace VillageClash.Core
{
    public sealed class TroopDef
    {
        public TroopType Type;
        public string Name;
        public string Description;
        public int Housing;
        public int[] Hp;              // per troop level (1..3)
        public int[] Dps;             // per troop level
        public float Range;           // tiles; melee ~0.5
        public float Speed;           // tiles per second
        public TargetPreference Preference;
        public float PreferenceMultiplier; // damage multiplier vs preferred targets
        public int[] CostElixir;      // per troop level
        public int TrainSeconds;
        public int BarracksLevel;     // barracks level that unlocks the troop
        public int[] UpgradeCostElixir;   // cost to reach level 2, level 3
        public int[] UpgradeSeconds;      // time to reach level 2, level 3
        public int[] LabLevelRequired;    // lab level required to reach level 2, level 3
        public int MaxLevel => Hp.Length;
    }
}
