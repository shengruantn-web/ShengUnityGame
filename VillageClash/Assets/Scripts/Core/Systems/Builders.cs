using System;

namespace VillageClash.Core
{
    public static class Builders
    {
        public static int Total(GameState s)
        {
            int n = 0;
            foreach (var b in s.Village.Buildings) if (b.Type == BuildingType.BuilderHut && Economy.IsActive(b)) n++;
            return Math.Max(1, n);
        }

        public static int Busy(GameState s)
        {
            int n = 0;
            foreach (var b in s.Village.Buildings) if (b.Constructing) n++;
            foreach (var o in s.Village.Obstacles) if (o.Removing) n++;
            return n;
        }

        public static int Free(GameState s) => Math.Max(0, Total(s) - Busy(s));

        static bool NeedsBuilder(int seconds) => seconds > 0;

        /// <summary>Places and starts building. Returns null on success or a message.</summary>
        public static string StartBuild(GameState s, BuildingType type, Int2 origin, long now, out BuildingInstance placed)
        {
            placed = null;
            var status = ShopRules.Status(s, type);
            if (!status.Unlocked) return Msg.TownHallRequired;
            if (!status.CanBuildMore) return Msg.MaxReached;
            var afford = Economy.AffordError(s, status.CostGold, status.CostElixir, status.CostGems);
            if (afford != null) return afford;
            if (NeedsBuilder(status.BuildSeconds) && Free(s) <= 0) return Msg.BuildersBusy;
            if (!s.Village.CanPlace(type, origin)) return Msg.CantPlace;

            Economy.Spend(s, status.CostGold, status.CostElixir, status.CostGems);
            placed = s.Village.TryPlace(type, origin, 1);
            if (status.BuildSeconds > 0)
            {
                placed.Constructing = true;
                placed.IsNewBuild = true;
                placed.StartedAtUnix = now;
                placed.FinishAtUnix = now + status.BuildSeconds;
            }
            else
            {
                Progression.AddXp(s, placed.Stats.XpReward);
            }
            return null;
        }

        public static string StartUpgrade(GameState s, int id, long now)
        {
            var b = s.Village.Find(id);
            if (b == null) return Msg.NotFound;
            var gate = ShopRules.UpgradeGateError(s, b);
            if (gate != null) return gate;
            var next = b.Def.Stats(b.Level + 1);
            var afford = Economy.AffordError(s, next.CostGold, next.CostElixir, next.CostGems);
            if (afford != null) return afford;
            if (NeedsBuilder(next.BuildSeconds) && Free(s) <= 0) return Msg.BuildersBusy;
            Economy.Spend(s, next.CostGold, next.CostElixir, next.CostGems);
            if (next.BuildSeconds > 0)
            {
                b.Constructing = true;
                b.IsNewBuild = false;
                b.StartedAtUnix = now;
                b.FinishAtUnix = now + next.BuildSeconds;
            }
            else
            {
                b.Level++;
                Progression.AddXp(s, next.XpReward);
            }
            return null;
        }

        public static string StartRemoveObstacle(GameState s, int id, long now)
        {
            var o = s.Village.FindObstacle(id);
            if (o == null) return Msg.NotFound;
            if (o.Removing) return Msg.AlreadyUpgrading;
            int gold = GameData.ObstacleRemoveCostGold(o.Kind), elixir = GameData.ObstacleRemoveCostElixir(o.Kind);
            var afford = Economy.AffordError(s, gold, elixir);
            if (afford != null) return afford;
            if (Free(s) <= 0) return Msg.BuildersBusy;
            Economy.Spend(s, gold, elixir);
            o.Removing = true;
            o.FinishAtUnix = now + GameData.ObstacleRemoveSeconds(o.Kind);
            return null;
        }

        public static int GemFinishCost(long remainingSeconds) => remainingSeconds <= 0 ? 0 : (int)Math.Max(1, (remainingSeconds + 59) / 60);

        public static long Remaining(BuildingInstance b, long now) => Math.Max(0, b.FinishAtUnix - now);

        public static string FinishWithGems(GameState s, int id, long now)
        {
            var b = s.Village.Find(id);
            if (b == null || !b.Constructing) return Msg.NotFound;
            int cost = GemFinishCost(Remaining(b, now));
            if (s.Gems < cost) return Msg.NotEnoughGems;
            s.Gems -= cost;
            b.FinishAtUnix = now;
            Tick(s, now);
            return null;
        }

        /// <summary>Completes any finished construction/removal jobs. Returns number of completions.</summary>
        public static int Tick(GameState s, long now)
        {
            int done = 0;
            foreach (var b in s.Village.Buildings)
            {
                if (!b.Constructing || b.FinishAtUnix > now) continue;
                if (!b.IsNewBuild) b.Level++;
                b.Constructing = false;
                b.IsNewBuild = false;
                Progression.AddXp(s, b.Stats.XpReward);
                done++;
            }
            for (int i = s.Village.Obstacles.Count - 1; i >= 0; i--)
            {
                var o = s.Village.Obstacles[i];
                if (!o.Removing || o.FinishAtUnix > now) continue;
                s.Village.RemoveObstacle(o.Id);
                Progression.AddXp(s, 5);
                done++;
            }
            return done;
        }
    }
}
