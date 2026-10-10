namespace VillageClash.Core
{
    public static class Laboratory
    {
        public static BuildingInstance Lab(GameState s)
        {
            foreach (var b in s.Village.Buildings) if (b.Type == BuildingType.Laboratory && Economy.IsActive(b)) return b;
            return null;
        }

        public static bool Busy(GameState s) => s.LabTroop >= 0;

        public static string StartError(GameState s, TroopType t)
        {
            var lab = Lab(s);
            if (lab == null) return Msg.NoLab;
            if (Busy(s)) return Msg.LabBusy;
            var def = GameData.Troop(t);
            int lvl = s.TroopLevel(t);
            if (lvl >= def.MaxLevel) return Msg.MaxLevel;
            if (!Training.Unlocked(s, t)) return Msg.BarracksLevel;
            if (lab.Level < def.LabLevelRequired[lvl - 1]) return Msg.LabLevel;
            if (s.Elixir < def.UpgradeCostElixir[lvl - 1]) return Msg.NotEnoughElixir;
            return null;
        }

        public static string Start(GameState s, TroopType t, long now)
        {
            var err = StartError(s, t);
            if (err != null) return err;
            var def = GameData.Troop(t);
            int lvl = s.TroopLevel(t);
            s.Elixir -= def.UpgradeCostElixir[lvl - 1];
            s.LabTroop = (int)t;
            s.LabFinishUnix = now + def.UpgradeSeconds[lvl - 1];
            return null;
        }

        public static bool Tick(GameState s, long now)
        {
            if (s.LabTroop < 0 || s.LabFinishUnix > now) return false;
            s.TroopLevels[s.LabTroop] = System.Math.Min(3, s.TroopLevels[s.LabTroop] + 1);
            s.LabTroop = -1;
            Progression.AddXp(s, 30);
            return true;
        }

        public static string FinishWithGems(GameState s, long now)
        {
            if (!Busy(s)) return Msg.NotFound;
            int cost = Builders.GemFinishCost(System.Math.Max(0, s.LabFinishUnix - now));
            if (s.Gems < cost) return Msg.NotEnoughGems;
            s.Gems -= cost;
            s.LabFinishUnix = now;
            Tick(s, now);
            return null;
        }
    }
}
