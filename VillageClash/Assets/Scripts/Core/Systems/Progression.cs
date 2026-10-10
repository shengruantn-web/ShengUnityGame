using System;

namespace VillageClash.Core
{
    public static class Progression
    {
        public static int XpToNext(int level) => 50 * level * level;

        public static int AddXp(GameState s, int amount)
        {
            if (amount <= 0) return 0;
            s.Xp += amount;
            int ups = 0;
            while (s.Xp >= XpToNext(s.Level)) { s.Xp -= XpToNext(s.Level); s.Level++; ups++; }
            return ups;
        }

        public static int TrophyDelta(int stars)
        {
            switch (stars)
            {
                case 0: return -12;
                case 1: return 12;
                case 2: return 20;
                default: return 30;
            }
        }

        public static void AddTrophies(GameState s, int delta) => s.Trophies = Math.Max(0, s.Trophies + delta);

        public static int Stars(int percent, bool townHallDown)
        {
            int stars = 0;
            if (percent >= 50) stars++;
            if (townHallDown) stars++;
            if (percent >= 100) stars++;
            return stars;
        }
    }
}
