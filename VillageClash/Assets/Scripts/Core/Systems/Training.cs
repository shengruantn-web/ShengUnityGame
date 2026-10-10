using System;

namespace VillageClash.Core
{
    public static class Training
    {
        public static int CampCapacity(GameState s)
        {
            int cap = 0;
            foreach (var b in s.Village.Buildings)
                if (b.Type == BuildingType.ArmyCamp && Economy.IsActive(b)) cap += b.Stats.Capacity;
            return cap;
        }

        public static int ArmyHousing(GameState s)
        {
            int h = 0;
            foreach (TroopType t in Enum.GetValues(typeof(TroopType))) h += s.Army(t) * GameData.Troop(t).Housing;
            return h;
        }

        public static int QueuedHousing(GameState s)
        {
            int h = 0;
            foreach (var j in s.TrainQueue) h += j.Count * GameData.Troop(j.Type).Housing;
            return h;
        }

        public static int QueuedCount(GameState s)
        {
            int n = 0;
            foreach (var j in s.TrainQueue) n += j.Count;
            return n;
        }

        public static int BarracksLevel(GameState s)
        {
            int best = 0;
            foreach (var b in s.Village.Buildings)
                if (b.Type == BuildingType.Barracks && Economy.IsActive(b) && b.Level > best) best = b.Level;
            return best;
        }

        public static bool Unlocked(GameState s, TroopType t) => BarracksLevel(s) >= GameData.Troop(t).BarracksLevel;

        public static int Cost(GameState s, TroopType t) => GameData.Troop(t).CostElixir[s.TroopLevel(t) - 1];

        public static string EnqueueError(GameState s, TroopType t)
        {
            var def = GameData.Troop(t);
            int bl = BarracksLevel(s);
            if (bl == 0) return Msg.NoBarracks;
            if (bl < def.BarracksLevel) return Msg.BarracksLevel;
            if (ArmyHousing(s) + QueuedHousing(s) + def.Housing > CampCapacity(s)) return Msg.ArmyFull;
            if (s.Elixir < Cost(s, t)) return Msg.NotEnoughElixir;
            return null;
        }

        public static string Enqueue(GameState s, TroopType t, long now)
        {
            var err = EnqueueError(s, t);
            if (err != null) return err;
            s.Elixir -= Cost(s, t);
            bool wasEmpty = s.TrainQueue.Count == 0;
            var last = s.TrainQueue.Count > 0 ? s.TrainQueue[s.TrainQueue.Count - 1] : null;
            if (last != null && last.Type == t) last.Count++;
            else s.TrainQueue.Add(new TrainJob { Type = t, Count = 1 });
            if (wasEmpty) s.TrainNextFinishUnix = now + GameData.Troop(t).TrainSeconds;
            return null;
        }

        /// <summary>Cancels one troop from the queue entry at index, refunding elixir.</summary>
        public static bool Cancel(GameState s, int index, long now)
        {
            if (index < 0 || index >= s.TrainQueue.Count) return false;
            var job = s.TrainQueue[index];
            s.Elixir = Math.Min(Economy.ElixirCap(s), s.Elixir + Cost(s, job.Type));
            job.Count--;
            if (job.Count <= 0) s.TrainQueue.RemoveAt(index);
            if (index == 0 && s.TrainQueue.Count > 0)
                s.TrainNextFinishUnix = now + GameData.Troop(s.TrainQueue[0].Type).TrainSeconds;
            return true;
        }

        public static long NextFinishIn(GameState s, long now) => s.TrainQueue.Count == 0 ? 0 : Math.Max(0, s.TrainNextFinishUnix - now);

        public static long TotalRemaining(GameState s, long now)
        {
            if (s.TrainQueue.Count == 0) return 0;
            long t = NextFinishIn(s, now);
            bool first = true;
            foreach (var j in s.TrainQueue)
            {
                int n = first ? j.Count - 1 : j.Count;
                first = false;
                t += (long)n * GameData.Troop(j.Type).TrainSeconds;
            }
            return t;
        }

        /// <summary>Completes queued troops whose time has come. Returns number trained.</summary>
        public static int Tick(GameState s, long now)
        {
            int trained = 0;
            while (s.TrainQueue.Count > 0 && s.TrainNextFinishUnix <= now)
            {
                var job = s.TrainQueue[0];
                // Camps full (e.g. camp destroyed)? Hold the queue.
                if (ArmyHousing(s) + GameData.Troop(job.Type).Housing > CampCapacity(s)) break;
                s.SetArmy(job.Type, s.Army(job.Type) + 1);
                trained++;
                job.Count--;
                if (job.Count <= 0) s.TrainQueue.RemoveAt(0);
                if (s.TrainQueue.Count > 0)
                    s.TrainNextFinishUnix += GameData.Troop(s.TrainQueue[0].Type).TrainSeconds;
            }
            return trained;
        }

        public static int GemFinishCost(GameState s, long now) => Builders.GemFinishCost(TotalRemaining(s, now));

        public static string FinishWithGems(GameState s, long now)
        {
            int cost = GemFinishCost(s, now);
            if (s.Gems < cost) return Msg.NotEnoughGems;
            s.Gems -= cost;
            s.TrainNextFinishUnix = now;
            while (s.TrainQueue.Count > 0)
            {
                int before = s.TrainQueue.Count;
                s.TrainNextFinishUnix = now;
                if (Tick(s, now) == 0) break;
            }
            return null;
        }
    }
}
