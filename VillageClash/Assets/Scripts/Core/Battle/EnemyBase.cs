using System;
using System.Collections.Generic;

namespace VillageClash.Core
{
    public sealed class EnemyBase
    {
        public string Name = "Enemy";
        public string ClanName = "";
        public int Trophies;
        public int TownHallLevel = 1;
        public int Seed;
        public bool Practice;
        public int PracticeId = -1;
        public int Gold;
        public int Elixir;
        public VillageModel Village = new VillageModel();
    }

    public static class EnemyBaseGenerator
    {
        static readonly string[] Names = { "Grimjaw", "Lady Ashfall", "Barbarossa", "Tinker", "Nyx", "Sir Cuddles", "Boulder", "Vex", "Mossbeard", "Red Hilda", "Kael", "Thornback", "Mira", "Dunstan", "Olga the Grim", "Pip" };
        static readonly string[] Clans = { "Iron Wolves", "Night Owls", "Goblin Grinders", "Stone Keepers", "Elixir Elite", "Red Banners", "Misty Peaks", "", "", "Lone Raiders" };
        public static readonly string[] PracticeNames = { "Goblin Outpost", "Goblin Forest", "Goblin Picnic" };

        public static EnemyBase Practice(int id)
        {
            id = Math.Max(0, Math.Min(PracticeNames.Length - 1, id));
            var e = Generate(1000 + id * 17, id + 1, 0);
            e.Practice = true;
            e.PracticeId = id;
            e.Name = PracticeNames[id];
            e.ClanName = "Goblins";
            e.Trophies = 0;
            return e;
        }

        public static EnemyBase Generate(int seed, int thLevel, int playerTrophies)
        {
            thLevel = Math.Max(1, Math.Min(GameData.MaxTownHall, thLevel));
            var rng = new Random(seed);
            var e = new EnemyBase { Seed = seed, TownHallLevel = thLevel };
            var v = e.Village;
            e.Name = Names[rng.Next(Names.Length)];
            e.ClanName = Clans[rng.Next(Clans.Length)];
            e.Trophies = Math.Max(0, playerTrophies + rng.Next(-60, 61));

            // Town Hall in the middle.
            v.TryPlace(BuildingType.TownHall, new Int2(18, 18), thLevel);

            // Wall ring sized by wall budget, centered on the TH center (20,20).
            int walls = GameData.MaxCount(BuildingType.Wall, thLevel);
            int side = Math.Min(19, (walls + 4) / 4);
            if (side % 2 == 0) side--;
            int lo = 20 - side / 2, hi = lo + side - 1;
            int ilo = lo + 1, ihi = hi - 1; // interior inclusive

            // Inside: storages and defenses.
            var inside = new List<BuildingType>();
            AddN(inside, BuildingType.GoldStorage, GameData.MaxCount(BuildingType.GoldStorage, thLevel));
            AddN(inside, BuildingType.ElixirStorage, GameData.MaxCount(BuildingType.ElixirStorage, thLevel));
            AddN(inside, BuildingType.Mortar, GameData.MaxCount(BuildingType.Mortar, thLevel));
            AddN(inside, BuildingType.ArcherTower, GameData.MaxCount(BuildingType.ArcherTower, thLevel));
            AddN(inside, BuildingType.Cannon, GameData.MaxCount(BuildingType.Cannon, thLevel));
            var spill = new List<BuildingType>();
            foreach (var t in inside)
                if (!PlaceInside(v, rng, t, Level(rng, t, thLevel), ilo, ihi)) spill.Add(t);

            // Outside: economy, army, anything that didn't fit inside.
            var outside = new List<BuildingType>(spill);
            AddN(outside, BuildingType.GoldMine, GameData.MaxCount(BuildingType.GoldMine, thLevel));
            AddN(outside, BuildingType.ElixirCollector, GameData.MaxCount(BuildingType.ElixirCollector, thLevel));
            AddN(outside, BuildingType.Barracks, Math.Min(1, GameData.MaxCount(BuildingType.Barracks, thLevel)));
            AddN(outside, BuildingType.ArmyCamp, GameData.MaxCount(BuildingType.ArmyCamp, thLevel));
            AddN(outside, BuildingType.Laboratory, GameData.MaxCount(BuildingType.Laboratory, thLevel));
            AddN(outside, BuildingType.BuilderHut, thLevel >= 3 ? 2 : 1);
            foreach (var t in outside) PlaceOutside(v, rng, t, Level(rng, t, thLevel), lo, hi);

            // Walls.
            int placed = 0;
            for (int x = lo; x <= hi && placed < walls; x++)
            {
                if (v.TryPlace(BuildingType.Wall, new Int2(x, lo), WallLevel(thLevel)) != null) placed++;
                if (v.TryPlace(BuildingType.Wall, new Int2(x, hi), WallLevel(thLevel)) != null) placed++;
            }
            for (int y = lo + 1; y < hi && placed < walls; y++)
            {
                if (v.TryPlace(BuildingType.Wall, new Int2(lo, y), WallLevel(thLevel)) != null) placed++;
                if (v.TryPlace(BuildingType.Wall, new Int2(hi, y), WallLevel(thLevel)) != null) placed++;
            }

            // Traps just outside the wall ring.
            int bombs = GameData.MaxCount(BuildingType.Bomb, thLevel), springs = GameData.MaxCount(BuildingType.SpringTrap, thLevel);
            for (int i = 0; i < bombs; i++) PlaceTrap(v, rng, BuildingType.Bomb, lo, hi);
            for (int i = 0; i < springs; i++) PlaceTrap(v, rng, BuildingType.SpringTrap, lo, hi);

            // Stored resources.
            int goldCap = 0, elixirCap = 0;
            foreach (var b in v.Buildings)
            {
                if (b.Type == BuildingType.TownHall) { goldCap += b.Stats.Capacity; elixirCap += b.Stats.Capacity; }
                else if (b.Type == BuildingType.GoldStorage) goldCap += b.Stats.Capacity;
                else if (b.Type == BuildingType.ElixirStorage) elixirCap += b.Stats.Capacity;
            }
            e.Gold = goldCap * rng.Next(35, 90) / 100;
            e.Elixir = elixirCap * rng.Next(35, 90) / 100;
            return e;
        }

        static void AddN(List<BuildingType> list, BuildingType t, int n) { for (int i = 0; i < n; i++) list.Add(t); }

        static int WallLevel(int th) => Math.Max(1, Math.Min(5, th));

        static int Level(Random rng, BuildingType t, int th)
        {
            var def = GameData.Building(t);
            int lvl = Math.Max(1, Math.Min(def.MaxLevel, th - rng.Next(0, 2)));
            while (lvl > 1 && def.Stats(lvl).ThRequired > th) lvl--;
            return lvl;
        }

        static bool PlaceInside(VillageModel v, Random rng, BuildingType t, int level, int ilo, int ihi)
        {
            int size = GameData.Building(t).Size;
            for (int tries = 0; tries < 300; tries++)
            {
                int max = ihi - size + 1;
                if (max < ilo) return false;
                var p = new Int2(rng.Next(ilo, max + 1), rng.Next(ilo, max + 1));
                if (v.TryPlace(t, p, level) != null) return true;
            }
            return false;
        }

        static bool PlaceOutside(VillageModel v, Random rng, BuildingType t, int level, int lo, int hi)
        {
            int size = GameData.Building(t).Size;
            for (int tries = 0; tries < 400; tries++)
            {
                var p = new Int2(rng.Next(2, GameData.GridSize - size - 1), rng.Next(2, GameData.GridSize - size - 1));
                // keep a 1-cell corridor around the wall ring
                bool overlapsRing = p.X + size > lo - 1 && p.X < hi + 2 && p.Y + size > lo - 1 && p.Y < hi + 2;
                if (overlapsRing) continue;
                if (v.TryPlace(t, p, level) != null) return true;
            }
            return false;
        }

        static void PlaceTrap(VillageModel v, Random rng, BuildingType t, int lo, int hi)
        {
            for (int tries = 0; tries < 200; tries++)
            {
                int ring = rng.Next(2, 4);
                int side = rng.Next(4);
                int along = rng.Next(lo - ring, hi + ring + 1);
                Int2 p = side == 0 ? new Int2(along, lo - ring) : side == 1 ? new Int2(along, hi + ring) : side == 2 ? new Int2(lo - ring, along) : new Int2(hi + ring, along);
                if (v.TryPlace(t, p, 1) != null) return;
            }
        }
    }
}
