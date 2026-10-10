namespace VillageClash.Core
{
    public sealed class League
    {
        public string Name;
        public int MinTrophies;
        public int BonusGold;
        public int BonusElixir;
        public League(string n, int min, int bg, int be) { Name = n; MinTrophies = min; BonusGold = bg; BonusElixir = be; }
    }

    public static class Leagues
    {
        public static readonly League[] All =
        {
            new League("Unranked", 0, 0, 0),
            new League("Bronze", 400, 100, 100),
            new League("Silver", 800, 250, 250),
            new League("Gold", 1400, 500, 500),
            new League("Crystal", 2000, 1000, 1000),
            new League("Master", 2600, 2000, 2000),
            new League("Champion", 3200, 4000, 4000),
            new League("Titan", 4100, 8000, 8000),
        };

        public static League ForTrophies(int trophies)
        {
            League best = All[0];
            foreach (var l in All) if (trophies >= l.MinTrophies) best = l;
            return best;
        }

        public static League Next(League current)
        {
            for (int i = 0; i < All.Length - 1; i++) if (All[i] == current) return All[i + 1];
            return null;
        }
    }
}
