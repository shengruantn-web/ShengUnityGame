using NUnit.Framework;
using VillageClash.Core;

namespace VillageClash.Tests
{
    public class TrainingTests
    {
        static GameState Fresh(long now = 1000) => SaveSystem.NewGame(1, now);

        [Test]
        public void QueueCompletesInOrderAndRespectsCapacity()
        {
            var s = Fresh();
            Assert.That(Training.CampCapacity(s), Is.EqualTo(20));
            s.Elixir = 10000;
            for (int i = 0; i < 20; i++) Assert.IsNull(Training.Enqueue(s, TroopType.Barbarian, 1000), "troop " + i);
            Assert.That(Training.Enqueue(s, TroopType.Barbarian, 1000), Is.EqualTo(Msg.ArmyFull));
            Assert.That(s.TrainQueue.Count, Is.EqualTo(1));
            Assert.That(s.TrainQueue[0].Count, Is.EqualTo(20));
            Assert.That(Training.Tick(s, 1004), Is.EqualTo(0));
            Assert.That(Training.Tick(s, 1005), Is.EqualTo(1));
            Assert.That(s.Army(TroopType.Barbarian), Is.EqualTo(1));
            Assert.That(Training.Tick(s, 1000 + 5 * 20), Is.EqualTo(19));
            Assert.That(s.TrainQueue.Count, Is.EqualTo(0));
            Assert.That(Training.ArmyHousing(s), Is.EqualTo(20));
        }

        [Test]
        public void LockedTroopsAndCancel()
        {
            var s = Fresh();
            s.Elixir = 1000;
            Assert.That(Training.Enqueue(s, TroopType.Archer, 1000), Is.EqualTo(Msg.BarracksLevel));
            Assert.IsNull(Training.Enqueue(s, TroopType.Barbarian, 1000));
            Assert.That(s.Elixir, Is.EqualTo(975));
            Assert.IsTrue(Training.Cancel(s, 0, 1000));
            Assert.That(s.Elixir, Is.EqualTo(1000));
            Assert.That(s.TrainQueue.Count, Is.EqualTo(0));
        }

        [Test]
        public void GemFinishTrainsEverything()
        {
            var s = Fresh();
            s.Elixir = 1000;
            for (int i = 0; i < 5; i++) Training.Enqueue(s, TroopType.Barbarian, 1000);
            Assert.That(Training.TotalRemaining(s, 1000), Is.EqualTo(25));
            Assert.IsNull(Training.FinishWithGems(s, 1000));
            Assert.That(s.Army(TroopType.Barbarian), Is.EqualTo(5));
            Assert.That(s.TrainQueue.Count, Is.EqualTo(0));
        }

        [Test]
        public void LaboratoryUpgradesTroops()
        {
            var s = Fresh();
            s.Elixir = 100000; s.Gold = 100000;
            Assert.That(Laboratory.Start(s, TroopType.Barbarian, 1000), Is.EqualTo(Msg.NoLab));
            s.Village.TownHall.Level = 3;
            Assert.IsNull(Builders.StartBuild(s, BuildingType.Laboratory, new Int2(2, 2), 1000, out var lab));
            Builders.Tick(s, lab.FinishAtUnix);
            Assert.IsNull(Laboratory.Start(s, TroopType.Barbarian, 2000));
            Assert.That(Laboratory.Start(s, TroopType.Barbarian, 2000), Is.EqualTo(Msg.LabBusy));
            Assert.IsFalse(Laboratory.Tick(s, 2000 + 299));
            Assert.IsTrue(Laboratory.Tick(s, 2000 + 300));
            Assert.That(s.TroopLevel(TroopType.Barbarian), Is.EqualTo(2));
            Assert.That(Laboratory.Start(s, TroopType.Barbarian, 3000), Is.EqualTo(Msg.LabLevel));
        }
    }
}
