using NUnit.Framework;
using VillageClash.Core;

namespace VillageClash.Tests
{
    public class SaveTests
    {
        [Test]
        public void RoundTrip()
        {
            var s = SaveSystem.NewGame(42, 5000);
            s.Gold = 123; s.Elixir = 456; s.Gems = 7; s.Trophies = 321; s.Xp = 10; s.Level = 3;
            s.SetArmy(TroopType.Barbarian, 4);
            s.TroopLevels[(int)TroopType.Archer] = 2;
            s.TrainQueue.Add(new TrainJob { Type = TroopType.Barbarian, Count = 3 });
            s.TrainNextFinishUnix = 5005;
            s.LabTroop = (int)TroopType.Giant; s.LabFinishUnix = 9999;
            s.PlayerName = "Thắng \"the\" Chief";
            var mine = s.Village.FindFirst(BuildingType.GoldMine);
            mine.Stored = 123.5;
            s.Gold = 2000;
            Assert.IsNull(Builders.StartUpgrade(s, s.Village.FindFirst(BuildingType.Cannon).Id, 5000));
            s.Gold = 123;
            s.DefenseLog.Add(new DefenseReport { Attacker = "Grimjaw", Stars = 2, Percent = 77, GoldLost = 50, ElixirLost = 60, TrophyDelta = -18, AtUnix = 4000 });

            var json = SaveSystem.Serialize(s);
            Assert.IsTrue(SaveSystem.TryDeserialize(json, out var r));
            Assert.That(r.Gold, Is.EqualTo(123));
            Assert.That(r.Elixir, Is.EqualTo(456));
            Assert.That(r.Gems, Is.EqualTo(7));
            Assert.That(r.Trophies, Is.EqualTo(321));
            Assert.That(r.Level, Is.EqualTo(3));
            Assert.That(r.PlayerName, Is.EqualTo("Thắng \"the\" Chief"));
            Assert.That(r.Army(TroopType.Barbarian), Is.EqualTo(4));
            Assert.That(r.TroopLevel(TroopType.Archer), Is.EqualTo(2));
            Assert.That(r.TrainQueue.Count, Is.EqualTo(1));
            Assert.That(r.TrainQueue[0].Count, Is.EqualTo(3));
            Assert.That(r.TrainNextFinishUnix, Is.EqualTo(5005));
            Assert.That(r.LabTroop, Is.EqualTo((int)TroopType.Giant));
            Assert.That(r.Village.Buildings.Count, Is.EqualTo(s.Village.Buildings.Count));
            Assert.That(r.Village.Obstacles.Count, Is.EqualTo(s.Village.Obstacles.Count));
            Assert.That(r.Village.FindFirst(BuildingType.GoldMine).Stored, Is.EqualTo(123.5).Within(1e-9));
            var cannon = r.Village.FindFirst(BuildingType.Cannon);
            Assert.IsTrue(cannon.Constructing);
            Assert.IsFalse(cannon.IsNewBuild);
            Assert.That(cannon.FinishAtUnix, Is.EqualTo(5060));
            Assert.That(r.DefenseLog.Count, Is.EqualTo(1));
            Assert.That(r.DefenseLog[0].Attacker, Is.EqualTo("Grimjaw"));
            Assert.That(r.Village.NextId, Is.EqualTo(s.Village.NextId));
            Assert.IsFalse(r.Village.Grid.CanPlace(new Int2(18, 18), 1), "grid rebuilt");
        }

        [Test]
        public void GarbageAndWrongVersionAreRejected()
        {
            Assert.IsFalse(SaveSystem.TryDeserialize("not json at all", out var a));
            Assert.IsNull(a);
            Assert.IsFalse(SaveSystem.TryDeserialize("", out _));
            Assert.IsFalse(SaveSystem.TryDeserialize(null, out _));
            Assert.IsFalse(SaveSystem.TryDeserialize("{\"version\":99}", out _));
            Assert.IsFalse(SaveSystem.TryDeserialize("{\"version\":1}", out _), "no town hall");
            Assert.IsFalse(SaveSystem.TryDeserialize("[1,2,3]", out _));
        }

        [Test]
        public void MiniJsonHandlesEdgeCases()
        {
            var v = MiniJson.Parse("{\"a\":[1,2.5,-3,\"x\\n\\u0041\",true,null],\"b\":{}}") as System.Collections.Generic.Dictionary<string, object>;
            Assert.NotNull(v);
            var a = v["a"] as System.Collections.Generic.List<object>;
            Assert.That(a[0], Is.EqualTo(1L));
            Assert.That(a[1], Is.EqualTo(2.5));
            Assert.That(a[2], Is.EqualTo(-3L));
            Assert.That(a[3], Is.EqualTo("x\nA"));
            Assert.That(a[4], Is.EqualTo(true));
            Assert.IsNull(a[5]);
            Assert.Throws<System.FormatException>(() => MiniJson.Parse("{\"a\":}"));
            Assert.Throws<System.FormatException>(() => MiniJson.Parse("{} x"));
        }

        [Test]
        public void NewGameHasStarterLayout()
        {
            var s = SaveSystem.NewGame(3, 100);
            Assert.NotNull(s.Village.TownHall);
            Assert.That(s.Village.Count(BuildingType.Cannon), Is.EqualTo(1));
            Assert.That(s.Village.Count(BuildingType.ArmyCamp), Is.EqualTo(1));
            Assert.That(s.Village.Obstacles.Count, Is.GreaterThan(5));
            Assert.That(s.Gold, Is.EqualTo(GameData.StartingGold));
            Assert.IsTrue(s.ShieldActive(100));
        }
    }
}
