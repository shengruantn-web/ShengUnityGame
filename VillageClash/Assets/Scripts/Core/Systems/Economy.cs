using System;

namespace VillageClash.Core
{
    public static class Economy
    {
        /// <summary>Buildings under initial construction don't count for anything.</summary>
        public static bool IsActive(BuildingInstance b) => !(b.Constructing && b.IsNewBuild);

        public static int GoldCap(GameState s) => Cap(s, ResourceKind.Gold);
        public static int ElixirCap(GameState s) => Cap(s, ResourceKind.Elixir);

        static int Cap(GameState s, ResourceKind kind)
        {
            int cap = 0;
            foreach (var b in s.Village.Buildings)
            {
                if (!IsActive(b)) continue;
                if (b.Type == BuildingType.TownHall) cap += b.Stats.Capacity;
                else if (kind == ResourceKind.Gold && b.Type == BuildingType.GoldStorage) cap += b.Stats.Capacity;
                else if (kind == ResourceKind.Elixir && b.Type == BuildingType.ElixirStorage) cap += b.Stats.Capacity;
            }
            return cap;
        }

        /// <summary>Accrue production between LastSeenUnix and now. Clamps to capacity; never negative.</summary>
        public static void Tick(GameState s, long now)
        {
            long dt = now - s.LastSeenUnix;
            if (dt < 0) dt = 0;
            if (dt > 0)
            {
                foreach (var b in s.Village.Buildings)
                {
                    if (!IsActive(b)) continue;
                    if (b.Type != BuildingType.GoldMine && b.Type != BuildingType.ElixirCollector) continue;
                    var st = b.Stats;
                    b.Stored = Math.Min(st.Capacity, b.Stored + st.ProductionPerHour / 3600.0 * dt);
                }
            }
            s.LastSeenUnix = now;
        }

        public static bool IsProducer(BuildingType t) => t == BuildingType.GoldMine || t == BuildingType.ElixirCollector;

        /// <summary>Moves a collector's stored resources into the player's storages. Returns the amount collected.</summary>
        public static int Collect(GameState s, int buildingId)
        {
            var b = s.Village.Find(buildingId);
            if (b == null || !IsProducer(b.Type) || !IsActive(b)) return 0;
            int amount = (int)Math.Floor(b.Stored);
            if (amount <= 0) return 0;
            int taken;
            if (b.Type == BuildingType.GoldMine)
            {
                taken = Math.Min(amount, Math.Max(0, GoldCap(s) - s.Gold));
                s.Gold += taken;
            }
            else
            {
                taken = Math.Min(amount, Math.Max(0, ElixirCap(s) - s.Elixir));
                s.Elixir += taken;
            }
            b.Stored -= taken;
            return taken;
        }

        public static string AffordError(GameState s, int gold, int elixir, int gems = 0)
        {
            if (s.Gold < gold) return Msg.NotEnoughGold;
            if (s.Elixir < elixir) return Msg.NotEnoughElixir;
            if (s.Gems < gems) return Msg.NotEnoughGems;
            return null;
        }

        public static bool CanAfford(GameState s, int gold, int elixir, int gems = 0) => AffordError(s, gold, elixir, gems) == null;

        public static bool Spend(GameState s, int gold, int elixir, int gems = 0)
        {
            if (!CanAfford(s, gold, elixir, gems)) return false;
            s.Gold -= gold; s.Elixir -= elixir; s.Gems -= gems;
            return true;
        }

        /// <summary>Adds resources, capped by storage. Returns what was actually added.</summary>
        public static void Add(GameState s, int gold, int elixir, out int addedGold, out int addedElixir)
        {
            addedGold = Math.Max(0, Math.Min(gold, GoldCap(s) - s.Gold));
            addedElixir = Math.Max(0, Math.Min(elixir, ElixirCap(s) - s.Elixir));
            s.Gold += addedGold;
            s.Elixir += addedElixir;
        }

        public static void AddGems(GameState s, int gems) => s.Gems = Math.Max(0, s.Gems + gems);
    }
}
