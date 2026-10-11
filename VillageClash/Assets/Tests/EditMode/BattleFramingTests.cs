using System.Collections.Generic;
using NUnit.Framework;
using VillageClash.Core;

namespace VillageClash.Tests
{
    public class BattleFramingTests
    {
        static BuildingInstance B(BuildingType t, int x, int y, int size)
            => new BuildingInstance { Type = t, Origin = new Int2(x, y), Size = size, Level = 1 };

        static List<BuildingInstance> Base() => new List<BuildingInstance>
        {
            B(BuildingType.TownHall, 18, 18, 4),
            B(BuildingType.Cannon, 10, 10, 3),   // bottom corner in iso (low x+y)
            B(BuildingType.Cannon, 28, 28, 3),   // top corner
            B(BuildingType.Cannon, 8, 30, 3),    // left-most (low x, high y)
            B(BuildingType.Cannon, 30, 8, 3),    // right-most
        };

        static void AssertFootprintsVisible(List<BuildingInstance> bs, float aspect, float topFrac, float bottomFrac, float cx, float cy, float zoom)
        {
            float halfW = zoom * aspect;
            float yMin = cy - zoom + 2 * zoom * bottomFrac, yMax = cy + zoom - 2 * zoom * topFrac;
            foreach (var b in bs)
            {
                int[] dx = { 0, b.Size, 0, b.Size }, dy = { 0, 0, b.Size, b.Size };
                for (int i = 0; i < 4; i++)
                {
                    IsoMath.CellToWorld(b.Origin.X + dx[i], b.Origin.Y + dy[i], out var wx, out var wy);
                    Assert.That(wx, Is.InRange(cx - halfW, cx + halfW), $"{b.Type} corner {i} x");
                    Assert.That(wy, Is.InRange(yMin, yMax), $"{b.Type} corner {i} y");
                }
            }
        }

        [Test]
        public void FitKeepsEveryFootprintInsideTheUnobstructedBand()
        {
            var bs = Base();
            BattleFraming.Fit(bs, 16f / 9f, 0.2f, 0.2f, 4f, 13f, out var cx, out var cy, out var zoom);
            Assert.That(zoom, Is.InRange(4f, 13f));
            AssertFootprintsVisible(bs, 16f / 9f, 0.2f, 0.2f, cx, cy, zoom);

            BattleFraming.Fit(bs, 4f / 3f, 0.15f, 0.15f, 4f, 13f, out cx, out cy, out zoom);
            AssertFootprintsVisible(bs, 4f / 3f, 0.15f, 0.15f, cx, cy, zoom);
        }

        [Test]
        public void NarrowerAspectZoomsOutWhenWidthIsTheLimit()
        {
            var bs = new List<BuildingInstance> { B(BuildingType.Cannon, 2, 36, 2), B(BuildingType.Cannon, 36, 2, 2) }; // very wide, flat
            BattleFraming.Fit(bs, 16f / 9f, 0f, 0f, 1f, 100f, out _, out _, out var wide);
            BattleFraming.Fit(bs, 4f / 3f, 0f, 0f, 1f, 100f, out _, out _, out var narrow);
            Assert.That(narrow, Is.GreaterThan(wide));
        }

        [Test]
        public void HudInsetsShiftTheCenterSoTheBaseSitsBetweenThem()
        {
            var bs = Base();
            BattleFraming.Fit(bs, 16f / 9f, 0f, 0f, 4f, 13f, out _, out var cyNone, out _);
            BattleFraming.Fit(bs, 16f / 9f, 0f, 0.3f, 4f, 13f, out _, out var cyBottom, out _);
            // a bottom strip hides the lower part of the view, so the camera must look lower to keep the base in the band
            Assert.That(cyBottom, Is.LessThan(cyNone));
        }

        [Test]
        public void ZoomIsClampedAndEmptyListsFallBackToPlateauCenter()
        {
            var bs = new List<BuildingInstance> { B(BuildingType.Cannon, 0, 39, 1), B(BuildingType.Cannon, 39, 0, 1), B(BuildingType.Cannon, 0, 0, 1), B(BuildingType.Cannon, 39, 39, 1) };
            BattleFraming.Fit(bs, 4f / 3f, 0.2f, 0.2f, 4f, 13f, out _, out _, out var zoom);
            Assert.That(zoom, Is.EqualTo(13f).Within(1e-4f));

            BattleFraming.Fit(new List<BuildingInstance>(), 16f / 9f, 0.2f, 0.2f, 4f, 13f, out var cx, out var cy, out zoom);
            Assert.That(cx, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(cy, Is.EqualTo(GameData.GridSize * 0.25f).Within(1e-4f));
            Assert.That(zoom, Is.InRange(4f, 13f));
        }

        [Test]
        public void TrapsDoNotAffectFraming()
        {
            var bs = Base();
            BattleFraming.Fit(bs, 16f / 9f, 0.2f, 0.2f, 4f, 13f, out var cx0, out var cy0, out var z0);
            bs.Add(B(BuildingType.Bomb, 1, 1, 1));
            bs.Add(B(BuildingType.SpringTrap, 38, 38, 1));
            BattleFraming.Fit(bs, 16f / 9f, 0.2f, 0.2f, 4f, 13f, out var cx1, out var cy1, out var z1);
            Assert.That(cx1, Is.EqualTo(cx0).Within(1e-4f));
            Assert.That(cy1, Is.EqualTo(cy0).Within(1e-4f));
            Assert.That(z1, Is.EqualTo(z0).Within(1e-4f));
        }
    }
}
