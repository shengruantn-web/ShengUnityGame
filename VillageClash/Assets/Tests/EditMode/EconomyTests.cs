using NUnit.Framework;
using VillageClash.Core;

namespace VillageClash.Tests
{
    public class EconomyTests
    {
        static GameState Fresh(long now = 1000) => SaveSystem.NewGame(1, now);

        [Test]
        public void ProductionAccruesAndCaps()
        {
            var s = Fresh(1000);
            var mine = s.Village.FindFirst(BuildingType.GoldMine);
            Economy.Tick(s, 1000 + 3600);
            Assert.That(mine.Stored, Is.EqualTo(200).Within(0.01));
            Economy.Tick(s, 1000 + 3600 * 11);
            Assert.That(mine.Stored, Is.EqualTo(500).Within(0.01), "capped at capacity");
        }

        [Test]
        public void ClockGoingBackwardsIsHarmless()
        {
            var s = Fresh(1000);
            var mine = s.Village.FindFirst(BuildingType.GoldMine);
            Economy.Tick(s, 500);
            Assert.That(mine.Stored, Is.EqualTo(0));
            Assert.That(s.LastSeenUnix, Is.EqualTo(500));
            Economy.Tick(s, 500 + 3600);
            Assert.That(mine.Stored, Is.EqualTo(200).Within(0.01));
        }

        [Test]
        public void CollectRespectsStorageCap()
        {
            var s = Fresh(1000);
            var mine = s.Village.FindFirst(BuildingType.GoldMine);
            mine.Stored = 500;
            s.Gold = Economy.GoldCap(s) - 100;
            int got = Economy.Collect(s, mine.Id);
            Assert.That(got, Is.EqualTo(100));
            Assert.That(s.Gold, Is.EqualTo(Economy.GoldCap(s)));
            Assert.That(mine.Stored, Is.EqualTo(400).Within(0.01));
        }

        [Test]
        public void SpendRejectsOverdraft()
        {
            var s = Fresh();
            s.Gold = 100;
            Assert.IsFalse(Economy.Spend(s, 101, 0));
            Assert.That(s.Gold, Is.EqualTo(100));
            Assert.IsTrue(Economy.Spend(s, 100, 0));
            Assert.That(s.Gold, Is.EqualTo(0));
            Assert.That(Economy.AffordError(s, 1, 0), Is.EqualTo(Msg.NotEnoughGold));
        }

        [Test]
        public void TownHallContributesToCap()
        {
            var s = Fresh();
            Assert.That(Economy.GoldCap(s), Is.EqualTo(1000));
            Builders.StartBuild(s, BuildingType.GoldStorage, new Int2(2, 2), 1000, out var st);
            Assert.NotNull(st);
            Assert.That(Economy.GoldCap(s), Is.EqualTo(1000), "under construction does not count");
            Builders.Tick(s, st.FinishAtUnix);
            Assert.That(Economy.GoldCap(s), Is.EqualTo(2500));
        }
    }
}
