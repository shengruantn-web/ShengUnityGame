using NUnit.Framework;
using VillageClash.Core;

namespace VillageClash.Tests
{
    public class VillageModelTests
    {
        [Test]
        public void PlaceMoveRemove()
        {
            var v = new VillageModel();
            var th = v.TryPlace(BuildingType.TownHall, new Int2(18, 18));
            Assert.NotNull(th);
            Assert.That(th.Size, Is.EqualTo(4));
            Assert.IsNull(v.TryPlace(BuildingType.Cannon, new Int2(20, 20)), "overlap rejected");
            var c = v.TryPlace(BuildingType.Cannon, new Int2(10, 10));
            Assert.NotNull(c);
            Assert.IsTrue(v.TryMove(c.Id, new Int2(11, 11)), "move onto own footprint");
            Assert.IsFalse(v.TryMove(c.Id, new Int2(17, 17)), "move onto town hall rejected");
            Assert.That(v.BuildingAt(new Int2(12, 12)), Is.SameAs(c));
            Assert.IsTrue(v.Remove(c.Id));
            Assert.IsNull(v.BuildingAt(new Int2(12, 12)));
            Assert.That(v.Count(BuildingType.TownHall), Is.EqualTo(1));
        }

        [Test]
        public void ObstacleBlocksPlacement()
        {
            var v = new VillageModel();
            var tree = v.TryPlaceObstacle(ObstacleKind.Tree, new Int2(5, 5));
            Assert.NotNull(tree);
            Assert.IsNull(v.TryPlace(BuildingType.Cannon, new Int2(4, 4)));
            Assert.That(v.ObstacleAt(new Int2(6, 6)), Is.SameAs(tree));
            Assert.IsTrue(v.RemoveObstacle(tree.Id));
            Assert.NotNull(v.TryPlace(BuildingType.Cannon, new Int2(4, 4)));
        }

        [Test]
        public void RebuildGridRestoresOccupancy()
        {
            var v = new VillageModel();
            var a = v.TryPlace(BuildingType.GoldMine, new Int2(0, 0));
            v.Grid.Clear();
            Assert.IsTrue(v.Grid.CanPlace(new Int2(0, 0), 3));
            v.RebuildGrid();
            Assert.IsFalse(v.Grid.CanPlace(new Int2(0, 0), 3));
            Assert.That(v.NextId, Is.GreaterThan(a.Id));
        }
    }
}
