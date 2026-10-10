using System;

namespace VillageClash.Core
{
    /// <summary>Simulated offline raids against the player's village.</summary>
    public static class DefenseLog
    {
        public const int RaidIntervalSeconds = 8 * 3600;
        public const int MaxEntries = 5;

        static readonly string[] Names = { "Grimjaw", "Lady Ashfall", "Barbarossa", "Tinker", "Nyx", "Sir Cuddles", "Boulder", "Vex", "Mossbeard", "Red Hilda" };

        public static int DefenseStrength(GameState s)
        {
            int str = 0;
            foreach (var b in s.Village.Buildings)
            {
                if (!Economy.IsActive(b)) continue;
                if (b.Def.IsDefense) str += 10 * b.Level;
                else if (b.Def.IsWall) str += 1;
            }
            return str;
        }

        /// <summary>Call once on resume with the time elapsed since the player was last seen. Returns the report if a raid happened.</summary>
        public static DefenseReport SimulateOffline(GameState s, long elapsedSeconds, long now, Random rng)
        {
            if (elapsedSeconds < RaidIntervalSeconds) return null;
            if (s.ShieldActive(now - elapsedSeconds)) return null;
            if (s.TownHallLevel < 2 && s.BattlesPlayed == 0) return null; // new players are left alone
            if (rng.NextDouble() > 0.6) return null;

            int str = DefenseStrength(s);
            // stronger defense -> fewer stars
            double p = Math.Max(0.1, 0.9 - str / 200.0);
            int stars = 0;
            for (int i = 0; i < 3; i++) if (rng.NextDouble() < p) stars++;
            int percent = stars == 0 ? 10 + rng.Next(35) : stars == 1 ? 50 + rng.Next(20) : stars == 2 ? 70 + rng.Next(29) : 100;
            double lootFrac = 0.04 + 0.03 * stars;
            int gold = (int)(s.Gold * lootFrac);
            int elixir = (int)(s.Elixir * lootFrac);
            s.Gold -= gold; s.Elixir -= elixir;
            int trophies = stars == 0 ? 8 : -(6 + 6 * stars);
            Progression.AddTrophies(s, trophies);
            if (percent >= 30) s.ShieldUntilUnix = Math.Max(s.ShieldUntilUnix, now + GameData.ShieldSeconds);
            var r = new DefenseReport
            {
                Attacker = Names[rng.Next(Names.Length)], Stars = stars, Percent = percent,
                GoldLost = gold, ElixirLost = elixir, TrophyDelta = trophies, AtUnix = now - rng.Next((int)Math.Min(int.MaxValue, elapsedSeconds)),
            };
            s.DefenseLog.Insert(0, r);
            while (s.DefenseLog.Count > MaxEntries) s.DefenseLog.RemoveAt(s.DefenseLog.Count - 1);
            return r;
        }
    }
}
