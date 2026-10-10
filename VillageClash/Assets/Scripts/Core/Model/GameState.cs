using System;
using System.Collections.Generic;

namespace VillageClash.Core
{
    public sealed class TrainJob
    {
        public TroopType Type;
        public int Count;
    }

    public sealed class DefenseReport
    {
        public string Attacker;
        public int Stars;
        public int Percent;
        public int GoldLost;
        public int ElixirLost;
        public int TrophyDelta;
        public long AtUnix;
        public bool Seen;
    }

    /// <summary>The whole persistent player state. Mutated only by Core systems.</summary>
    public sealed class GameState
    {
        public const int SchemaVersion = 1;

        public string PlayerName = "Chief";
        public int Gold;
        public int Elixir;
        public int Gems;
        public int Xp;
        public int Level = 1;
        public int Trophies;
        public long ShieldUntilUnix;
        public long LastSeenUnix;
        public long CreatedUnix;
        public int Seed;
        public bool MusicOn = true;
        public bool SfxOn = true;
        public int BattlesPlayed;
        public int BattlesWon;
        public int TotalStars;

        public VillageModel Village = new VillageModel();

        public readonly int[] ArmyCounts = new int[Enum.GetValues(typeof(TroopType)).Length];
        public readonly int[] TroopLevels = { 1, 1, 1, 1, 1 };
        public readonly List<TrainJob> TrainQueue = new List<TrainJob>();
        public long TrainNextFinishUnix;

        public int LabTroop = -1;          // TroopType being researched, -1 = none
        public long LabFinishUnix;

        public readonly List<DefenseReport> DefenseLog = new List<DefenseReport>();

        public int Army(TroopType t) => ArmyCounts[(int)t];
        public void SetArmy(TroopType t, int n) => ArmyCounts[(int)t] = Math.Max(0, n);
        public int TroopLevel(TroopType t) => TroopLevels[(int)t];
        public int ArmySize { get { int n = 0; foreach (var c in ArmyCounts) n += c; return n; } }
        public int TownHallLevel => Village.TownHallLevel;
        public bool ShieldActive(long now) => ShieldUntilUnix > now;
    }
}
