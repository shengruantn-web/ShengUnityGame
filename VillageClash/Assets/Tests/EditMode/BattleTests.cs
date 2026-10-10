using System;
using NUnit.Framework;
using VillageClash.Core;

namespace VillageClash.Tests
{
    public class BattleTests
    {
        static int[] Army(params (TroopType, int)[] troops)
        {
            var a = new int[Enum.GetValues(typeof(TroopType)).Length];
            foreach (var (t, n) in troops) a[(int)t] = n;
            return a;
        }
        static readonly int[] L1 = { 1, 1, 1, 1, 1 };

        static void RunToEnd(BattleSim sim, float maxSeconds = 400)
        {
            float t = 0;
            while (!sim.Ended && t < maxSeconds) { sim.Step(0.1f); t += 0.1f; }
        }

        [Test]
        public void AStarFindsStraightAndDetouredPaths()
        {
            var cost = new float[10 * 10];
            var p = AStar.FindPath(10, cost, new Int2(0, 0), c => c == new Int2(5, 0), c => Math.Abs(c.X - 5) + Math.Abs(c.Y));
            Assert.NotNull(p);
            Assert.That(p.Count, Is.EqualTo(5));
            for (int y = 0; y < 10; y++) if (y != 9) cost[y * 10 + 3] = -1; // wall of blocks with a gap at the bottom
            p = AStar.FindPath(10, cost, new Int2(0, 0), c => c == new Int2(5, 0), c => Math.Abs(c.X - 5) + Math.Abs(c.Y));
            Assert.NotNull(p);
            Assert.That(p.Count, Is.GreaterThan(10));
            Assert.That(p[p.Count - 1], Is.EqualTo(new Int2(5, 0)));
            for (int y = 0; y < 10; y++) cost[y * 10 + 3] = AStar.WallCost; // soft walls: go through
            p = AStar.FindPath(10, cost, new Int2(0, 0), c => c == new Int2(5, 0), c => Math.Abs(c.X - 5) + Math.Abs(c.Y));
            Assert.That(p.Count, Is.EqualTo(5));
            for (int y = 0; y < 10; y++) cost[y * 10 + 3] = -1;
            Assert.IsNull(AStar.FindPath(10, cost, new Int2(0, 0), c => c == new Int2(5, 0), c => 0));
        }

        [Test]
        public void GeneratorIsDeterministicAndValid()
        {
            var a = EnemyBaseGenerator.Generate(7, 3, 500);
            var b = EnemyBaseGenerator.Generate(7, 3, 500);
            Assert.That(a.Name, Is.EqualTo(b.Name));
            Assert.That(a.Village.Buildings.Count, Is.EqualTo(b.Village.Buildings.Count));
            Assert.That(a.Village.TownHallLevel, Is.EqualTo(3));
            Assert.That(a.Village.Count(BuildingType.Wall), Is.GreaterThan(40));
            Assert.That(a.Village.Count(BuildingType.Cannon), Is.EqualTo(3));
            Assert.That(a.Village.Count(BuildingType.Mortar), Is.EqualTo(1));
            Assert.That(a.Gold, Is.GreaterThan(0));
            for (int th = 1; th <= 5; th++)
            {
                var e = EnemyBaseGenerator.Generate(th * 31, th, 0);
                Assert.NotNull(e.Village.TownHall);
                // grid consistent with list
                foreach (var bld in e.Village.Buildings)
                    Assert.That(e.Village.Grid.OccupantAt(bld.Origin), Is.EqualTo(bld.Id), $"TH{th} {bld.Type}");
            }
            var pr = EnemyBaseGenerator.Practice(0);
            Assert.IsTrue(pr.Practice);
            Assert.That(pr.Name, Is.EqualTo("Goblin Outpost"));
        }

        [Test]
        public void DeployRules()
        {
            var e = EnemyBaseGenerator.Generate(3, 1, 0);
            var sim = new BattleSim(e, Army((TroopType.Barbarian, 2)), L1, 1);
            var th = e.Village.TownHall;
            Assert.IsFalse(sim.CanDeploy(th.Origin), "on a building");
            Assert.IsFalse(sim.CanDeploy(th.Origin - new Int2(1, 1)), "adjacent to a building");
            Assert.IsFalse(sim.CanDeploy(new Int2(-1, 0)));
            Assert.IsTrue(sim.NearestDeployCell(new Int2(0, 0), out var cell));
            Assert.IsTrue(sim.TryDeploy(TroopType.Barbarian, cell.X + 0.5f, cell.Y + 0.5f));
            Assert.IsTrue(sim.TryDeploy(TroopType.Barbarian, cell.X + 0.5f, cell.Y + 0.5f));
            Assert.IsFalse(sim.TryDeploy(TroopType.Barbarian, cell.X + 0.5f, cell.Y + 0.5f), "no troops left");
            Assert.IsFalse(sim.TryDeploy(TroopType.Archer, cell.X + 0.5f, cell.Y + 0.5f), "none of that type");
            Assert.That(sim.Units.Count, Is.EqualTo(2));
            Assert.IsTrue(sim.Started);
        }

        [Test]
        public void TwentyBarbariansBeatTownHall1()
        {
            var e = EnemyBaseGenerator.Generate(11, 1, 0);
            var sim = new BattleSim(e, Army((TroopType.Barbarian, 20)), L1, 5);
            sim.NearestDeployCell(new Int2(20, 2), out var cell);
            for (int i = 0; i < 20; i++) sim.TryDeploy(TroopType.Barbarian, cell.X + 0.5f, cell.Y + 0.5f);
            RunToEnd(sim);
            Assert.IsTrue(sim.Ended);
            Assert.That(sim.Percent, Is.GreaterThanOrEqualTo(50), $"percent={sim.Percent} destroyed={sim.DestroyedCount}/{sim.TotalDestructible} time={sim.Time}");
            Assert.That(sim.Stars, Is.GreaterThanOrEqualTo(1));
            var r = sim.Result();
            Assert.That(r.TrophyDelta, Is.GreaterThan(0));
            Assert.That(r.TroopsUsed[(int)TroopType.Barbarian], Is.EqualTo(20));
            Assert.That(r.Gold + r.Elixir, Is.GreaterThan(0));
        }

        [Test]
        public void WalledBaseWithWallBreakersEndsInTime()
        {
            var e = EnemyBaseGenerator.Generate(23, 3, 0);
            var sim = new BattleSim(e, Army((TroopType.Giant, 4), (TroopType.WallBreaker, 4), (TroopType.Barbarian, 10), (TroopType.Archer, 10)), new[] { 2, 2, 2, 1, 2 }, 9);
            sim.NearestDeployCell(new Int2(20, 1), out var cell);
            for (int i = 0; i < 4; i++) sim.TryDeploy(TroopType.Giant, cell.X + 0.5f, cell.Y + 0.5f);
            for (int i = 0; i < 4; i++) sim.TryDeploy(TroopType.WallBreaker, cell.X + 0.5f, cell.Y + 0.5f);
            for (int i = 0; i < 10; i++) sim.TryDeploy(TroopType.Barbarian, cell.X + 0.5f, cell.Y + 0.5f);
            for (int i = 0; i < 10; i++) sim.TryDeploy(TroopType.Archer, cell.X + 0.5f, cell.Y + 0.5f);
            RunToEnd(sim);
            Assert.IsTrue(sim.Ended);
            Assert.That(sim.Time, Is.LessThanOrEqualTo(GameData.BattleSeconds + 1f));
            Assert.That(sim.Percent, Is.GreaterThan(0));
            int wallsDown = 0;
            foreach (var b in sim.Buildings) if (b.IsWall && b.Destroyed) wallsDown++;
            Assert.That(wallsDown, Is.GreaterThan(0), "wall breakers should break walls");
        }

        [Test]
        public void DestructionExcludesWallsAndTraps()
        {
            var e = EnemyBaseGenerator.Generate(5, 3, 0);
            var sim = new BattleSim(e, Army(), L1, 1);
            int nonWall = 0;
            foreach (var b in e.Village.Buildings) if (!b.Def.IsWall && !b.Def.IsTrap) nonWall++;
            Assert.That(sim.TotalDestructible, Is.EqualTo(nonWall));
            Assert.That(sim.Percent, Is.EqualTo(0));
        }

        [Test]
        public void EmptyArmyBattleEndsOnSurrenderWithZeroStars()
        {
            var e = EnemyBaseGenerator.Generate(5, 2, 300);
            var sim = new BattleSim(e, Army(), L1, 1);
            sim.Step(1f);
            Assert.IsFalse(sim.Started);
            sim.Surrender();
            Assert.IsTrue(sim.Ended);
            var r = sim.Result();
            Assert.That(r.Stars, Is.EqualTo(0));
            Assert.That(r.TrophyDelta, Is.LessThan(0));
            Assert.IsTrue(r.Surrendered);
        }

        [Test]
        public void StarsAndTrophies()
        {
            Assert.That(Progression.Stars(49, false), Is.EqualTo(0));
            Assert.That(Progression.Stars(50, false), Is.EqualTo(1));
            Assert.That(Progression.Stars(50, true), Is.EqualTo(2));
            Assert.That(Progression.Stars(100, true), Is.EqualTo(3));
            Assert.That(Progression.TrophyDelta(0), Is.LessThan(0));
            Assert.That(Progression.TrophyDelta(3), Is.GreaterThan(Progression.TrophyDelta(1)));
        }
    }
}
