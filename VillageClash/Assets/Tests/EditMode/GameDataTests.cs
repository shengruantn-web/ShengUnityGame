using System;
using NUnit.Framework;
using VillageClash.Core;

namespace VillageClash.Tests
{
    public class GameDataTests
    {
        [Test]
        public void EveryBuildingTypeHasDefWithStatsPerLevel()
        {
            foreach (BuildingType t in Enum.GetValues(typeof(BuildingType)))
            {
                var d = GameData.Building(t);
                Assert.That(d.MaxLevel, Is.GreaterThanOrEqualTo(1), t.ToString());
                Assert.That(d.Levels.Length, Is.EqualTo(d.MaxLevel));
                Assert.That(d.MaxCountByTh.Length, Is.EqualTo(GameData.MaxTownHall), t.ToString());
                Assert.That(d.Size, Is.GreaterThan(0));
                for (int l = 1; l <= d.MaxLevel; l++) Assert.That(d.Stats(l).Hp, Is.GreaterThan(0));
            }
        }

        [Test]
        public void TownHall1AllowsTwoCannonsButNoMortar()
        {
            Assert.That(GameData.MaxCount(BuildingType.Cannon, 1), Is.EqualTo(2));
            Assert.That(GameData.MaxCount(BuildingType.Mortar, 1), Is.EqualTo(0));
            Assert.That(GameData.MaxCount(BuildingType.Mortar, 3), Is.EqualTo(1));
        }

        [Test]
        public void EveryTroopHasThreeLevels()
        {
            foreach (TroopType t in Enum.GetValues(typeof(TroopType)))
            {
                var d = GameData.Troop(t);
                Assert.That(d.MaxLevel, Is.EqualTo(3));
                Assert.That(d.Dps.Length, Is.EqualTo(3));
                Assert.That(d.CostElixir.Length, Is.EqualTo(3));
                Assert.That(d.UpgradeCostElixir.Length, Is.EqualTo(2));
                Assert.That(d.Housing, Is.GreaterThan(0));
            }
        }

        [Test]
        public void LeagueThresholds()
        {
            Assert.That(Leagues.ForTrophies(399).Name, Is.EqualTo("Unranked"));
            Assert.That(Leagues.ForTrophies(400).Name, Is.EqualTo("Bronze"));
            Assert.That(Leagues.ForTrophies(4100).Name, Is.EqualTo("Titan"));
            Assert.That(Leagues.ForTrophies(99999).Name, Is.EqualTo("Titan"));
            Assert.That(Leagues.Next(Leagues.ForTrophies(0)).Name, Is.EqualTo("Bronze"));
            Assert.That(Leagues.Next(Leagues.ForTrophies(5000)), Is.Null);
        }
    }
}
