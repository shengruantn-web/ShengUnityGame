using NUnit.Framework;
using VillageClash.Core;

namespace VillageClash.Tests
{
    public class BuildersTests
    {
        static GameState Fresh(long now = 1000) => SaveSystem.NewGame(1, now);

        [Test]
        public void BuildConsumesBuilderAndCompletes()
        {
            var s = Fresh();
            Assert.That(Builders.Total(s), Is.EqualTo(1));
            int gold = s.Gold;
            var err = Builders.StartBuild(s, BuildingType.Cannon, new Int2(2, 2), 1000, out var c);
            Assert.IsNull(err);
            Assert.That(s.Gold, Is.EqualTo(gold - 250));
            Assert.IsTrue(c.Constructing && c.IsNewBuild);
            Assert.That(Builders.Free(s), Is.EqualTo(0));
            var err2 = Builders.StartBuild(s, BuildingType.GoldStorage, new Int2(6, 2), 1000, out _);
            Assert.That(err2, Is.EqualTo(Msg.BuildersBusy));
            Assert.That(Builders.Tick(s, 1005), Is.EqualTo(0));
            int xp = s.Xp;
            Assert.That(Builders.Tick(s, 1010), Is.EqualTo(1));
            Assert.IsFalse(c.Constructing);
            Assert.That(s.Xp, Is.GreaterThan(xp));
            Assert.That(Builders.Free(s), Is.EqualTo(1));
        }

        [Test]
        public void WallsAreInstantAndNeedNoBuilder()
        {
            var s = Fresh();
            Builders.StartBuild(s, BuildingType.Cannon, new Int2(2, 2), 1000, out _);
            Assert.That(Builders.Free(s), Is.EqualTo(0));
            var err = Builders.StartBuild(s, BuildingType.Wall, new Int2(0, 0), 1000, out var w);
            Assert.IsNull(err);
            Assert.IsFalse(w.Constructing);
        }

        [Test]
        public void MaxCountAndThGate()
        {
            var s = Fresh();
            Assert.That(Builders.StartBuild(s, BuildingType.Mortar, new Int2(2, 2), 1000, out _), Is.EqualTo(Msg.TownHallRequired));
            s.Gold = 100000;
            Assert.IsNull(Builders.StartBuild(s, BuildingType.Cannon, new Int2(2, 2), 1000, out _));
            Assert.That(Builders.StartBuild(s, BuildingType.Cannon, new Int2(6, 2), 1000, out _), Is.EqualTo(Msg.MaxReached));
        }

        [Test]
        public void UpgradeAndGemFinish()
        {
            var s = Fresh();
            var cannon = s.Village.FindFirst(BuildingType.Cannon);
            s.Gold = 5000;
            Assert.IsNull(Builders.StartUpgrade(s, cannon.Id, 1000));
            Assert.That(cannon.Level, Is.EqualTo(1));
            Assert.That(cannon.FinishAtUnix, Is.EqualTo(1060));
            Assert.That(Builders.StartUpgrade(s, cannon.Id, 1000), Is.EqualTo(Msg.AlreadyUpgrading));
            Assert.That(Builders.GemFinishCost(61), Is.EqualTo(2));
            Assert.That(Builders.GemFinishCost(60), Is.EqualTo(1));
            Assert.That(Builders.GemFinishCost(0), Is.EqualTo(0));
            int gems = s.Gems;
            Assert.IsNull(Builders.FinishWithGems(s, cannon.Id, 1001));
            Assert.That(s.Gems, Is.EqualTo(gems - 1));
            Assert.That(cannon.Level, Is.EqualTo(2));
            Assert.IsFalse(cannon.Constructing);
        }

        [Test]
        public void UpgradeBlockedByTownHallLevel()
        {
            var s = Fresh();
            var cannon = s.Village.FindFirst(BuildingType.Cannon);
            cannon.Level = 2;
            s.Gold = 100000;
            Assert.That(Builders.StartUpgrade(s, cannon.Id, 1000), Is.EqualTo(Msg.TownHallRequired));
        }

        [Test]
        public void ObstacleRemoval()
        {
            var s = Fresh();
            var tree = s.Village.Obstacles[0];
            Assert.IsNull(Builders.StartRemoveObstacle(s, tree.Id, 1000));
            Assert.That(Builders.Free(s), Is.EqualTo(0));
            Builders.Tick(s, tree.FinishAtUnix);
            Assert.IsNull(s.Village.FindObstacle(tree.Id));
        }

        [Test]
        public void XpLevels()
        {
            var s = new GameState();
            Progression.AddXp(s, 49);
            Assert.That(s.Level, Is.EqualTo(1));
            Progression.AddXp(s, 1);
            Assert.That(s.Level, Is.EqualTo(2));
            Assert.That(s.Xp, Is.EqualTo(0));
            Assert.That(Progression.XpToNext(2), Is.EqualTo(200));
        }
    }
}
