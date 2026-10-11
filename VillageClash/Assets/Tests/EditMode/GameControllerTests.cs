using System.Collections.Generic;
using NUnit.Framework;
using VillageClash.Core;

namespace VillageClash.Tests
{
    public class GameControllerTests
    {
        FixedClock clock;
        string store;
        GameController gc;
        List<string> toasts;
        int changes;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAll();
            clock = new FixedClock();
            store = null;
            toasts = new List<string>();
            changes = 0;
            GameEvents.Toast += t => toasts.Add(t);
            GameEvents.StateChanged += () => changes++;
            gc = new GameController(clock, () => store, s => store = s);
            gc.Load();
        }

        [TearDown]
        public void TearDown() => GameEvents.ClearAll();

        [Test]
        public void LoadCreatesNewGameAndSaves()
        {
            Assert.IsTrue(gc.IsNewPlayer);
            Assert.NotNull(store);
            Assert.That(gc.State.Gold, Is.EqualTo(GameData.StartingGold));
            Assert.That(gc.State.Village.TownHallLevel, Is.EqualTo(1));
        }

        [Test]
        public void LoadRestoresSavedState()
        {
            gc.State.Gold = 4321;
            gc.Save();
            var gc2 = new GameController(clock, () => store, s => store = s);
            gc2.Load();
            Assert.IsFalse(gc2.IsNewPlayer);
            Assert.That(gc2.State.Gold, Is.EqualTo(4321));
        }

        [Test]
        public void PlaceFailsWithToastAndSucceedsWhenAffordable()
        {
            gc.State.Gold = 0; gc.State.Elixir = 0;
            Assert.IsFalse(gc.ConfirmPlace(BuildingType.Cannon, new Int2(2, 2)));
            Assert.That(toasts, Does.Contain(Msg.NotEnoughGold));
            gc.State.Gold = 5000;
            int before = changes;
            Assert.IsTrue(gc.ConfirmPlace(BuildingType.Cannon, new Int2(2, 2)));
            Assert.That(changes, Is.GreaterThan(before));
            Assert.That(gc.State.Village.Count(BuildingType.Cannon), Is.EqualTo(2));
        }

        [Test]
        public void TickCompletesConstructionAndFiresEvent()
        {
            gc.State.Gold = 5000;
            Assert.IsTrue(gc.ConfirmPlace(BuildingType.Cannon, new Int2(2, 2)));
            int completed = -1;
            GameEvents.BuildingCompleted += id => completed = id;
            clock.Advance(GameData.Building(BuildingType.Cannon).Stats(1).BuildSeconds + 1);
            gc.Tick();
            Assert.That(completed, Is.GreaterThan(0));
            Assert.IsFalse(gc.State.Village.Find(completed).Constructing);
        }

        [Test]
        public void CollectProducesResourcesOverTime()
        {
            var mine = gc.State.Village.FindFirst(BuildingType.GoldMine);
            Assert.IsFalse(gc.Collect(mine.Id));
            clock.Advance(3600);
            gc.Tick();
            int gold = gc.State.Gold;
            Assert.IsTrue(gc.Collect(mine.Id));
            Assert.That(gc.State.Gold, Is.GreaterThan(gold));
        }

        [Test]
        public void TrainThenAttackThenFinish()
        {
            gc.State.Elixir = 5000;
            Assert.IsNull(gc.AttackError() == Msg.EmptyArmy ? null : "expected empty army");
            for (int i = 0; i < 10; i++) Assert.IsTrue(gc.Train(TroopType.Barbarian), string.Join(",", toasts));
            clock.Advance(10000);
            gc.Tick();
            Assert.That(gc.State.Army(TroopType.Barbarian), Is.EqualTo(10));
            int goldBefore = gc.State.Gold;
            var sim = gc.StartMatch(42);
            Assert.NotNull(sim);
            Assert.That(gc.State.Gold, Is.EqualTo(goldBefore - GameData.MatchCostGold));
            sim.NearestDeployCell(new Int2(20, 2), out var cell);
            for (int i = 0; i < 6; i++) Assert.IsTrue(sim.TryDeploy(TroopType.Barbarian, cell.X + .5f, cell.Y + .5f));
            float t = 0; while (!sim.Ended && t < 300) { sim.Step(0.1f); t += 0.1f; }
            var r = gc.FinishBattle();
            Assert.NotNull(r);
            Assert.That(gc.State.Army(TroopType.Barbarian), Is.EqualTo(4), "only deployed troops are consumed");
            Assert.That(gc.State.BattlesPlayed, Is.EqualTo(1));
            Assert.IsNull(gc.ActiveBattle);
        }

        [Test]
        public void PracticeDoesNotChangeTrophies()
        {
            gc.State.SetArmy(TroopType.Barbarian, 5);
            gc.State.Trophies = 100;
            var sim = gc.StartPractice(0);
            Assert.NotNull(sim);
            sim.Surrender();
            var r = gc.FinishBattle();
            Assert.That(r.TrophyDelta, Is.EqualTo(0));
            Assert.That(gc.State.Trophies, Is.EqualTo(100));
            Assert.That(gc.State.BattlesPlayed, Is.EqualTo(0));
        }

        [Test]
        public void BuyShieldSpendsGems()
        {
            int gems = gc.State.Gems;
            Assert.IsTrue(gc.BuyShield());
            Assert.That(gc.State.Gems, Is.EqualTo(gems - GameData.ShieldGemCost));
            Assert.IsTrue(gc.State.ShieldActive(clock.NowUnix));
            gc.State.Gems = 0;
            Assert.IsFalse(gc.BuyShield());
            Assert.That(toasts[toasts.Count - 1], Is.EqualTo(Msg.NotEnoughGems));
        }

        [Test]
        public void ResetStartsOver()
        {
            gc.State.Gold = 1;
            gc.Reset();
            Assert.That(gc.State.Gold, Is.EqualTo(GameData.StartingGold));
            Assert.IsTrue(SaveSystem.TryDeserialize(store, out var s));
            Assert.That(s.Gold, Is.EqualTo(GameData.StartingGold));
        }

        [Test]
        public void SaveFailureIsReportedAndCanBeRetried()
        {
            bool fail = true;
            var controller = new GameController(clock, () => store, json =>
            {
                if (fail) throw new System.IO.IOException("Disk full");
                store = json;
            });
            controller.Load();
            Assert.IsFalse(controller.Save());
            Assert.That(controller.LastSaveError, Is.EqualTo("Disk full"));
            Assert.That(toasts.Exists(t => t.Contains("Could not save")), Is.True);
            fail = false;
            Assert.IsTrue(controller.Save());
            Assert.IsNull(controller.LastSaveError);
        }

        [Test]
        public void LoadIoFailureNeverOverwritesExistingSave()
        {
            int writes = 0;
            var controller = new GameController(clock, () => throw new System.IO.IOException("Access denied"), _ => writes++);
            controller.Load();
            controller.Reset();
            clock.Advance(60);
            controller.Tick();
            Assert.IsFalse(controller.Save());
            Assert.That(writes, Is.Zero);
            Assert.That(controller.LoadNotice, Does.Contain("disabled"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InterruptedRaidSettlesCheckpointOnce(bool practice)
        {
            gc.State.SetArmy(TroopType.Barbarian, 10);
            gc.State.Trophies = 100;
            gc.State.Gold = 100;
            gc.State.Elixir = 100;
            var battle = practice ? gc.StartPractice(0) : gc.StartMatch(42);
            battle.NearestDeployCell(new Int2(20, 2), out var cell);
            for (int i = 0; i < 6; i++) Assert.IsTrue(battle.TryDeploy(TroopType.Barbarian, cell.X + .5f, cell.Y + .5f));
            // Deployment itself is durable, with no explicit controller Save call.
            Assert.IsTrue(SaveSystem.TryDeserialize(store, out var deployed));
            Assert.That(deployed.PendingBattle.TroopsUsed[0], Is.EqualTo(6));
            for (int i = 0; i < 180 && !battle.Ended; i++) battle.Step(.5f);
            Assert.IsTrue(SaveSystem.TryDeserialize(store, out var checkpoint));
            Assert.That(checkpoint.PendingBattle.Duration, Is.GreaterThan(0));
            var result = checkpoint.PendingBattle;

            var reopened = new GameController(clock, () => store, json => store = json);
            reopened.Load();
            Assert.That(reopened.State.Army(TroopType.Barbarian), Is.EqualTo(4));
            Assert.That(reopened.State.Gold, Is.EqualTo(System.Math.Min(Economy.GoldCap(checkpoint), checkpoint.Gold + result.Gold)));
            Assert.That(reopened.State.Elixir, Is.EqualTo(System.Math.Min(Economy.ElixirCap(checkpoint), checkpoint.Elixir + result.Elixir)));
            Assert.That(reopened.State.Trophies, Is.EqualTo(100 + result.TrophyDelta));
            Assert.That(reopened.State.BattlesPlayed, Is.EqualTo(practice ? 0 : 1));
            Assert.IsNull(reopened.State.PendingBattle);
            Assert.IsNull(reopened.FinishBattle());
            string settled = store;
            reopened.Load();
            Assert.That(store, Is.EqualTo(settled), "reopening cannot award loot or consume troops again");
        }

        [Test]
        public void InterruptedScoutingKeepsArmyButSettlesSurrender()
        {
            gc.State.SetArmy(TroopType.Barbarian, 5);
            gc.State.Trophies = 100;
            Assert.NotNull(gc.StartMatch(42));
            int paidGold = gc.State.Gold;
            var reopened = new GameController(clock, () => store, json => store = json);
            reopened.Load();
            Assert.That(reopened.State.Army(TroopType.Barbarian), Is.EqualTo(5));
            Assert.That(reopened.State.Gold, Is.EqualTo(paidGold));
            Assert.That(reopened.State.Trophies, Is.EqualTo(100 + Progression.TrophyDelta(0)));
            Assert.That(reopened.State.BattlesPlayed, Is.EqualTo(1));
        }

        [Test]
        public void FailedSettlementWriteCanRecoverWithoutDuplicatingRewards()
        {
            bool fail = false;
            var controller = new GameController(clock, () => store, json =>
            {
                if (fail) throw new System.IO.IOException("Disk full");
                store = json;
            });
            controller.Load();
            controller.State.SetArmy(TroopType.Barbarian, 5);
            var battle = controller.StartMatch(42);
            battle.NearestDeployCell(new Int2(20, 2), out var cell);
            battle.TryDeploy(TroopType.Barbarian, cell.X + .5f, cell.Y + .5f);
            battle.Surrender();
            fail = true;
            controller.FinishBattle();
            Assert.IsNull(controller.FinishBattle());
            fail = false;
            controller.Load();
            Assert.That(controller.State.Army(TroopType.Barbarian), Is.EqualTo(4));
            Assert.That(controller.State.BattlesPlayed, Is.EqualTo(1));
        }

        [Test]
        public void RaidDoesNotStartWhenInitialSaveFails()
        {
            var controller = new GameController(clock, () => store, _ => throw new System.IO.IOException("Disk full"));
            controller.Load();
            controller.State.SetArmy(TroopType.Barbarian, 5);
            int gold = controller.State.Gold;
            long shield = controller.State.ShieldUntilUnix;
            Assert.IsNull(controller.StartMatch(42));
            Assert.That(controller.State.Gold, Is.EqualTo(gold));
            Assert.That(controller.State.ShieldUntilUnix, Is.EqualTo(shield));
            Assert.IsNull(controller.ActiveBattle);
            Assert.IsNull(controller.State.PendingBattle);
        }

        [Test]
        public void SecondRaidCannotReplaceAnActiveBattle()
        {
            gc.State.SetArmy(TroopType.Barbarian, 5);
            var battle = gc.StartMatch(42);
            int gold = gc.State.Gold;
            Assert.IsNull(gc.StartMatch(43));
            Assert.IsNull(gc.StartPractice(0));
            Assert.That(gc.ActiveBattle, Is.SameAs(battle));
            Assert.That(gc.State.Gold, Is.EqualTo(gold));
        }
    
        [Test]
        public void FillWithGems_TopsUpMissingResourcesAtOneGemPerTen()
        {
            gc.State.Gold = 100; gc.State.Elixir = 0; gc.State.Gems = 50;
            Assert.AreEqual(25, gc.GemFillCost(300, 50));          // missing 200 + 50 = 250 -> 25 gems
            Assert.IsTrue(gc.FillWithGems(300, 50));
            Assert.AreEqual(300, gc.State.Gold);
            Assert.AreEqual(50, gc.State.Elixir);
            Assert.AreEqual(25, gc.State.Gems);
            Assert.AreEqual(0, gc.GemFillCost(300, 50));
            gc.State.Gems = 0;
            Assert.IsFalse(gc.FillWithGems(10000, 0));
            Assert.AreEqual(Msg.NotEnoughGems, toasts[toasts.Count - 1]);
        }
}
}
