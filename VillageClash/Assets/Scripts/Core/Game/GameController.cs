using System;
using System.Collections.Generic;

namespace VillageClash.Core
{
    /// <summary>Facade over all Core systems. Owns the GameState, persistence, and the tick loop. All mutations go through here.</summary>
    public sealed class GameController
    {
        readonly IClock clock;
        readonly Func<string> load;
        readonly Action<string> save;
        public GameState State { get; private set; }
        public DefenseReport PendingDefenseReport { get; private set; }
        public BattleSim ActiveBattle { get; private set; }
        public bool IsNewPlayer { get; private set; }
        long lastSaveUnix;
        const int AutoSaveSeconds = 15;

        public long Now => clock.NowUnix;

        public GameController(IClock clock, Func<string> load, Action<string> save)
        {
            this.clock = clock;
            this.load = load;
            this.save = save;
        }

        // ---- lifecycle --------------------------------------------------------
        public void Load()
        {
            string json = null;
            try { json = load?.Invoke(); } catch { json = null; }
            if (string.IsNullOrEmpty(json) || !SaveSystem.TryDeserialize(json, out var s))
            {
                s = SaveSystem.NewGame(Environment.TickCount, Now);
                IsNewPlayer = true;
            }
            State = s;
            long elapsed = State.LastSeenUnix > 0 ? Now - State.LastSeenUnix : 0;
            if (elapsed > 0)
            {
                PendingDefenseReport = DefenseLog.SimulateOffline(State, elapsed, Now, new Random(State.Seed ^ (int)Now));
                if (PendingDefenseReport != null) State.DefenseLog.Insert(0, PendingDefenseReport);
                while (State.DefenseLog.Count > DefenseLog.MaxEntries) State.DefenseLog.RemoveAt(State.DefenseLog.Count - 1);
            }
            Tick();
            Save();
        }

        public void Reset()
        {
            State = SaveSystem.NewGame(Environment.TickCount, Now);
            IsNewPlayer = true;
            PendingDefenseReport = null;
            ActiveBattle = null;
            Save();
            GameEvents.RaiseStateChanged();
        }

        public void ClearPendingDefenseReport()
        {
            if (PendingDefenseReport != null) PendingDefenseReport.Seen = true;
            PendingDefenseReport = null;
        }

        public void Save()
        {
            if (State == null) return;
            State.LastSeenUnix = Now;
            lastSaveUnix = Now;
            try { save?.Invoke(SaveSystem.Serialize(State)); } catch { }
        }

        /// <summary>Advance time-driven systems. Call every frame or every second; cheap.</summary>
        public void Tick()
        {
            if (State == null) return;
            long now = Now;
            bool changed = false;
            int before = State.Level;
            Economy.Tick(State, now);
            var wasBuilding = new List<int>();
            foreach (var b in State.Village.Buildings) if (b.Constructing) wasBuilding.Add(b.Id);
            if (Builders.Tick(State, now) > 0)
            {
                changed = true;
                foreach (var id in wasBuilding)
                {
                    var b = State.Village.Find(id);
                    if (b == null || b.Constructing) continue;
                    GameEvents.RaiseBuildingCompleted(id);
                    GameEvents.RaiseToast(b.Level > 1 ? $"{b.Def.Name} upgraded to level {b.Level}!" : $"{b.Def.Name} complete!");
                }
            }
            int trained = Training.Tick(State, now);
            if (trained > 0) changed = true;
            if (Laboratory.Tick(State, now)) { changed = true; GameEvents.RaiseToast("Research complete!"); }
            if (State.Level > before) GameEvents.RaiseLevelUp(State.Level);
            if (changed) GameEvents.RaiseStateChanged();
            if (now - lastSaveUnix >= AutoSaveSeconds) Save();
        }

        // ---- village actions ----------------------------------------------------
        public bool Collect(int id)
        {
            var b = State.Village.Find(id);
            if (b == null) return Fail(Msg.NotFound);
            int amount = Economy.Collect(State, id);
            if (amount <= 0) return Fail(b.Stored >= 1 ? Msg.StorageFull : Msg.NothingToCollect);
            bool gold = b.Def.ProducesKind == ResourceKind.Gold;
            GameEvents.RaiseResourcesCollected(id, gold ? amount : 0, gold ? 0 : amount);
            return Ok();
        }

        public int CollectAll()
        {
            int total = 0;
            foreach (var b in State.Village.Buildings.ToArray())
            {
                if (!Economy.IsProducer(b.Type) || b.Stored < 1) continue;
                int amt = Economy.Collect(State, b.Id);
                if (amt > 0)
                {
                    total += amt;
                    bool gold = b.Def.ProducesKind == ResourceKind.Gold;
                    GameEvents.RaiseResourcesCollected(b.Id, gold ? amt : 0, gold ? 0 : amt);
                }
            }
            if (total > 0) Ok();
            return total;
        }

        public ShopStatus Shop(BuildingType type) => ShopRules.Status(State, type);

        public bool ConfirmPlace(BuildingType type, Int2 origin)
        {
            var err = Builders.StartBuild(State, type, origin, Now, out _);
            return err == null ? Ok() : Fail(err);
        }

        public bool CanPlace(BuildingType type, Int2 origin, int ignoreId = -1) => State.Village.CanPlace(type, origin, ignoreId);

        public bool MoveBuilding(int id, Int2 origin)
        {
            var b = State.Village.Find(id);
            if (b == null) return Fail(Msg.NotFound);
            if (!State.Village.TryMove(id, origin)) return Fail(Msg.CantPlace);
            return Ok();
        }

        public bool Upgrade(int id)
        {
            var err = Builders.StartUpgrade(State, id, Now);
            return err == null ? Ok() : Fail(err);
        }

        public bool FinishWithGems(int id)
        {
            var err = Builders.FinishWithGems(State, id, Now);
            if (err != null) return Fail(err);
            GameEvents.RaiseBuildingCompleted(id);
            return Ok();
        }

        public bool RemoveObstacle(int id)
        {
            var err = Builders.StartRemoveObstacle(State, id, Now);
            return err == null ? Ok() : Fail(err);
        }

        public bool Train(TroopType type)
        {
            var err = Training.Enqueue(State, type, Now);
            return err == null ? Ok() : Fail(err);
        }

        public bool CancelTrain(int index)
        {
            if (!Training.Cancel(State, index, Now)) return Fail(Msg.NotFound);
            return Ok();
        }

        public bool FinishTrainingWithGems()
        {
            var err = Training.FinishWithGems(State, Now);
            return err == null ? Ok() : Fail(err);
        }

        public bool Research(TroopType type)
        {
            var err = Laboratory.Start(State, type, Now);
            return err == null ? Ok("Research started!") : Fail(err);
        }

        public bool FinishResearchWithGems()
        {
            var err = Laboratory.FinishWithGems(State, Now);
            return err == null ? Ok("Research complete!") : Fail(err);
        }

        public bool BuyShield()
        {
            if (!Economy.Spend(State, 0, 0, GameData.ShieldGemCost)) return Fail(Msg.NotEnoughGems);
            long from = Math.Max(Now, State.ShieldUntilUnix);
            State.ShieldUntilUnix = from + GameData.ShieldSeconds;
            return Ok("Shield activated!");
        }

        /// <summary>Gems needed to top up resources to afford a gold/elixir price (1 gem per 10 missing, min 1).</summary>
        public int GemFillCost(int gold, int elixir)
        {
            int missing = Math.Max(0, gold - State.Gold) + Math.Max(0, elixir - State.Elixir);
            return missing <= 0 ? 0 : Math.Max(1, (missing + 9) / 10);
        }

        /// <summary>Spend gems to raise gold/elixir up to the given price, ignoring storage caps.</summary>
        public bool FillWithGems(int gold, int elixir)
        {
            int cost = GemFillCost(gold, elixir);
            if (cost == 0) return Ok();
            if (!Economy.Spend(State, 0, 0, cost)) return Fail(Msg.NotEnoughGems);
            State.Gold = Math.Max(State.Gold, gold);
            State.Elixir = Math.Max(State.Elixir, elixir);
            return Ok();
        }

        public bool BuyBuilder(Int2 origin) => ConfirmPlace(BuildingType.BuilderHut, origin);

        public bool SetName(string name)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0) return Fail("Name can't be empty!");
            if (name.Length > 16) name = name.Substring(0, 16);
            State.PlayerName = name;
            return Ok();
        }

        public void SetAudio(bool music, bool sfx)
        {
            State.MusicOn = music; State.SfxOn = sfx;
            Ok();
        }

        // ---- battles ------------------------------------------------------------
        public string AttackError()
        {
            if (State.ArmySize == 0) return Msg.EmptyArmy;
            if (State.Gold < GameData.MatchCostGold) return Msg.NotEnoughGold;
            return null;
        }

        public BattleSim StartMatch(int seed)
        {
            var err = AttackError();
            if (err != null) { Fail(err); return null; }
            Economy.Spend(State, GameData.MatchCostGold, 0);
            State.ShieldUntilUnix = 0; // attacking drops your shield
            var enemy = EnemyBaseGenerator.Generate(seed, Math.Max(1, Math.Min(GameData.MaxTownHall, State.TownHallLevel + (State.Trophies > 400 ? 1 : 0))), State.Trophies);
            return Begin(enemy, seed);
        }

        public BattleSim StartPractice(int mapId)
        {
            if (State.ArmySize == 0) { Fail(Msg.EmptyArmy); return null; }
            return Begin(EnemyBaseGenerator.Practice(mapId), 1234 + mapId);
        }

        BattleSim Begin(EnemyBase enemy, int seed)
        {
            ActiveBattle = new BattleSim(enemy, State.ArmyCounts, State.TroopLevels, seed);
            Save();
            GameEvents.RaiseStateChanged();
            return ActiveBattle;
        }

        public BattleResult FinishBattle()
        {
            if (ActiveBattle == null) return null;
            if (!ActiveBattle.Ended) ActiveBattle.Surrender();
            var r = ActiveBattle.Result();
            ActiveBattle = null;
            // consume deployed troops
            for (int i = 0; i < r.TroopsUsed.Length; i++)
                State.SetArmy((TroopType)i, Math.Max(0, State.Army((TroopType)i) - r.TroopsUsed[i]));
            Economy.Add(State, r.Gold, r.Elixir, out int ag, out int ae);
            r.Gold = ag; r.Elixir = ae;
            int before = State.Level;
            if (!r.Practice)
            {
                Progression.AddTrophies(State, r.TrophyDelta);
                State.BattlesPlayed++;
                if (r.Won) State.BattlesWon++;
                State.TotalStars += r.Stars;
                Progression.AddXp(State, 10 + r.Stars * 15 + r.Percent / 5);
            }
            else Progression.AddXp(State, 5 + r.Stars * 5);
            if (State.Level > before) GameEvents.RaiseLevelUp(State.Level);
            Save();
            GameEvents.RaiseStateChanged();
            return r;
        }

        // ---- helpers ------------------------------------------------------------
        bool Ok(string toast = null)
        {
            if (toast != null) GameEvents.RaiseToast(toast);
            GameEvents.RaiseStateChanged();
            return true;
        }

        bool Fail(string msg)
        {
            GameEvents.RaiseToast(msg);
            return false;
        }
    }
}
