using NUnit.Framework;
using VillageClash.Core;

namespace VillageClash.Tests
{
    public class GridTests
    {
        [Test]
        public void PlaceOverlapAndBounds()
        {
            var g = new GridMap(40);
            Assert.IsTrue(g.CanPlace(new Int2(0, 0), 3));
            g.Occupy(7, new Int2(0, 0), 3);
            Assert.IsFalse(g.CanPlace(new Int2(2, 2), 3));
            Assert.IsTrue(g.CanPlace(new Int2(2, 2), 3, ignoreId: 7));
            Assert.IsTrue(g.CanPlace(new Int2(3, 0), 3));
            Assert.IsFalse(g.CanPlace(new Int2(38, 38), 3), "partially off grid");
            Assert.IsTrue(g.CanPlace(new Int2(39, 39), 1), "wall in last cell");
            Assert.IsFalse(g.CanPlace(new Int2(-1, 0), 1));
            Assert.That(g.OccupantAt(new Int2(1, 1)), Is.EqualTo(7));
            Assert.That(g.OccupantAt(new Int2(40, 40)), Is.EqualTo(GridMap.Empty));
            g.Free(new Int2(0, 0), 3);
            Assert.IsTrue(g.CanPlace(new Int2(0, 0), 3));
        }

        [Test]
        public void IsoRoundTrip()
        {
            for (int x = 0; x < 40; x += 7)
                for (int y = 0; y < 40; y += 5)
                {
                    IsoMath.CellToWorld(x + 0.5f, y + 0.5f, out var wx, out var wy);
                    var c = IsoMath.WorldToCellInt(wx, wy);
                    Assert.That(c, Is.EqualTo(new Int2(x, y)));
                }
            IsoMath.CellToWorld(1, 0, out var ax, out var ay);
            IsoMath.CellToWorld(0, 1, out var bx, out var by);
            Assert.That(ax, Is.EqualTo(-bx).Within(1e-5));
            Assert.That(ay, Is.EqualTo(by).Within(1e-5));
        }
    }
}
