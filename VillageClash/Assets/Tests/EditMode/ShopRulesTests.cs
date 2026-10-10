using NUnit.Framework;
using VillageClash.Core;

namespace VillageClash.Tests
{
    public class ShopRulesTests
    {
        [Test]
        public void TownHall1Shop()
        {
            var s = SaveSystem.NewGame(1, 1000);
            var mortar = ShopRules.Status(s, BuildingType.Mortar);
            Assert.IsFalse(mortar.Unlocked);
            Assert.That(mortar.LockText, Is.EqualTo("Level 3 Town Hall Required"));
            var cannon = ShopRules.Status(s, BuildingType.Cannon);
            Assert.IsTrue(cannon.Unlocked);
            Assert.That(cannon.Built, Is.EqualTo(1));
            Assert.That(cannon.Max, Is.EqualTo(2));
            Assert.IsTrue(cannon.CanBuildMore);
            Assert.IsTrue(cannon.Affordable);
            var hut = ShopRules.Status(s, BuildingType.BuilderHut);
            Assert.That(hut.CostGems, Is.EqualTo(GameData.SecondBuilderGemCost));
        }

        [Test]
        public void OfflineRaidIsDeterministicAndBounded()
        {
            var s = SaveSystem.NewGame(1, 1000);
            s.Village.TownHall.Level = 2;
            s.Gold = 1000; s.Elixir = 1000; s.Trophies = 100;
            s.ShieldUntilUnix = 0;
            DefenseReport r = null;
            for (int seed = 0; seed < 20 && r == null; seed++)
                r = DefenseLog.SimulateOffline(s, DefenseLog.RaidIntervalSeconds, 1000 + DefenseLog.RaidIntervalSeconds, new System.Random(seed));
            Assert.NotNull(r, "some seed should produce a raid");
            Assert.That(r.GoldLost, Is.LessThanOrEqualTo(130));
            Assert.That(s.Gold, Is.EqualTo(1000 - r.GoldLost));
            Assert.That(s.DefenseLog.Count, Is.EqualTo(1));
            Assert.IsNull(DefenseLog.SimulateOffline(s, 100, 2000, new System.Random(1)), "too soon");
        }
    }
}
