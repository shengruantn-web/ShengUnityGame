using System;
using System.Collections.Generic;

namespace VillageClash.Core
{
    public static class SaveSystem
    {
        public static string Serialize(GameState s)
        {
            var o = new Dictionary<string, object>
            {
                ["version"] = GameState.SchemaVersion,
                ["playerName"] = s.PlayerName,
                ["gold"] = s.Gold, ["elixir"] = s.Elixir, ["gems"] = s.Gems,
                ["xp"] = s.Xp, ["level"] = s.Level, ["trophies"] = s.Trophies,
                ["shieldUntil"] = s.ShieldUntilUnix, ["lastSeen"] = s.LastSeenUnix, ["created"] = s.CreatedUnix,
                ["seed"] = s.Seed, ["musicOn"] = s.MusicOn, ["sfxOn"] = s.SfxOn,
                ["battlesPlayed"] = s.BattlesPlayed, ["battlesWon"] = s.BattlesWon, ["totalStars"] = s.TotalStars,
                ["nextId"] = s.Village.NextId,
                ["army"] = new List<object>(Array.ConvertAll(s.ArmyCounts, x => (object)x)),
                ["troopLevels"] = new List<object>(Array.ConvertAll(s.TroopLevels, x => (object)x)),
                ["trainNextFinish"] = s.TrainNextFinishUnix,
                ["labTroop"] = s.LabTroop, ["labFinish"] = s.LabFinishUnix,
            };
            var buildings = new List<object>();
            foreach (var b in s.Village.Buildings)
                buildings.Add(new Dictionary<string, object>
                {
                    ["id"] = b.Id, ["type"] = b.Type.ToString(), ["level"] = b.Level,
                    ["x"] = b.Origin.X, ["y"] = b.Origin.Y,
                    ["constructing"] = b.Constructing, ["newBuild"] = b.IsNewBuild,
                    ["finishAt"] = b.FinishAtUnix, ["startedAt"] = b.StartedAtUnix,
                    ["stored"] = b.Stored, ["triggered"] = b.Triggered,
                });
            o["buildings"] = buildings;
            var obstacles = new List<object>();
            foreach (var ob in s.Village.Obstacles)
                obstacles.Add(new Dictionary<string, object>
                {
                    ["id"] = ob.Id, ["kind"] = ob.Kind.ToString(), ["x"] = ob.Origin.X, ["y"] = ob.Origin.Y,
                    ["removing"] = ob.Removing, ["finishAt"] = ob.FinishAtUnix,
                });
            o["obstacles"] = obstacles;
            var queue = new List<object>();
            foreach (var j in s.TrainQueue) queue.Add(new Dictionary<string, object> { ["type"] = j.Type.ToString(), ["count"] = j.Count });
            o["trainQueue"] = queue;
            var log = new List<object>();
            foreach (var r in s.DefenseLog)
                log.Add(new Dictionary<string, object>
                {
                    ["attacker"] = r.Attacker, ["stars"] = r.Stars, ["percent"] = r.Percent, ["gold"] = r.GoldLost,
                    ["elixir"] = r.ElixirLost, ["trophies"] = r.TrophyDelta, ["at"] = r.AtUnix, ["seen"] = r.Seen,
                });
            o["defenseLog"] = log;
            return MiniJson.Write(o);
        }

        /// <summary>Returns false (and null state) for malformed or unknown-version data. Never throws.</summary>
        public static bool TryDeserialize(string json, out GameState state)
        {
            state = null;
            try
            {
                if (!(MiniJson.Parse(json) is Dictionary<string, object> o)) return false;
                if (MiniJson.GetInt(o, "version", -1) != GameState.SchemaVersion) return false;
                var s = new GameState
                {
                    PlayerName = MiniJson.GetString(o, "playerName", "Chief"),
                    Gold = MiniJson.GetInt(o, "gold"), Elixir = MiniJson.GetInt(o, "elixir"), Gems = MiniJson.GetInt(o, "gems"),
                    Xp = MiniJson.GetInt(o, "xp"), Level = Math.Max(1, MiniJson.GetInt(o, "level", 1)), Trophies = MiniJson.GetInt(o, "trophies"),
                    ShieldUntilUnix = MiniJson.GetLong(o, "shieldUntil"), LastSeenUnix = MiniJson.GetLong(o, "lastSeen"), CreatedUnix = MiniJson.GetLong(o, "created"),
                    Seed = MiniJson.GetInt(o, "seed"), MusicOn = MiniJson.GetBool(o, "musicOn", true), SfxOn = MiniJson.GetBool(o, "sfxOn", true),
                    BattlesPlayed = MiniJson.GetInt(o, "battlesPlayed"), BattlesWon = MiniJson.GetInt(o, "battlesWon"), TotalStars = MiniJson.GetInt(o, "totalStars"),
                    TrainNextFinishUnix = MiniJson.GetLong(o, "trainNextFinish"),
                    LabTroop = MiniJson.GetInt(o, "labTroop", -1), LabFinishUnix = MiniJson.GetLong(o, "labFinish"),
                };
                var army = MiniJson.GetList(o, "army");
                for (int i = 0; i < Math.Min(army.Count, s.ArmyCounts.Length); i++) s.ArmyCounts[i] = (int)(army[i] is long l ? l : 0);
                var tl = MiniJson.GetList(o, "troopLevels");
                for (int i = 0; i < Math.Min(tl.Count, s.TroopLevels.Length); i++) s.TroopLevels[i] = Math.Max(1, Math.Min(3, (int)(tl[i] is long l ? l : 1)));
                foreach (var item in MiniJson.GetList(o, "buildings"))
                {
                    if (!(item is Dictionary<string, object> bo)) continue;
                    if (!Enum.TryParse(MiniJson.GetString(bo, "type", ""), out BuildingType type)) continue;
                    var def = GameData.Building(type);
                    var b = new BuildingInstance
                    {
                        Id = MiniJson.GetInt(bo, "id"), Type = type, Level = Math.Max(1, Math.Min(def.MaxLevel, MiniJson.GetInt(bo, "level", 1))),
                        Origin = new Int2(MiniJson.GetInt(bo, "x"), MiniJson.GetInt(bo, "y")), Size = def.Size,
                        Constructing = MiniJson.GetBool(bo, "constructing"), IsNewBuild = MiniJson.GetBool(bo, "newBuild"),
                        FinishAtUnix = MiniJson.GetLong(bo, "finishAt"), StartedAtUnix = MiniJson.GetLong(bo, "startedAt"),
                        Stored = MiniJson.GetDouble(bo, "stored"), Triggered = MiniJson.GetBool(bo, "triggered"),
                    };
                    if (!s.Village.Grid.FootprintInBounds(b.Origin, b.Size)) continue;
                    s.Village.Buildings.Add(b);
                }
                foreach (var item in MiniJson.GetList(o, "obstacles"))
                {
                    if (!(item is Dictionary<string, object> oo)) continue;
                    if (!Enum.TryParse(MiniJson.GetString(oo, "kind", ""), out ObstacleKind kind)) continue;
                    var ob = new ObstacleInstance
                    {
                        Id = MiniJson.GetInt(oo, "id"), Kind = kind, Origin = new Int2(MiniJson.GetInt(oo, "x"), MiniJson.GetInt(oo, "y")),
                        Size = GameData.ObstacleSize(kind), Removing = MiniJson.GetBool(oo, "removing"), FinishAtUnix = MiniJson.GetLong(oo, "finishAt"),
                    };
                    if (!s.Village.Grid.FootprintInBounds(ob.Origin, ob.Size)) continue;
                    s.Village.Obstacles.Add(ob);
                }
                foreach (var item in MiniJson.GetList(o, "trainQueue"))
                {
                    if (!(item is Dictionary<string, object> jo)) continue;
                    if (!Enum.TryParse(MiniJson.GetString(jo, "type", ""), out TroopType tt)) continue;
                    int count = MiniJson.GetInt(jo, "count");
                    if (count > 0) s.TrainQueue.Add(new TrainJob { Type = tt, Count = count });
                }
                foreach (var item in MiniJson.GetList(o, "defenseLog"))
                {
                    if (!(item is Dictionary<string, object> ro)) continue;
                    s.DefenseLog.Add(new DefenseReport
                    {
                        Attacker = MiniJson.GetString(ro, "attacker", "?"), Stars = MiniJson.GetInt(ro, "stars"), Percent = MiniJson.GetInt(ro, "percent"),
                        GoldLost = MiniJson.GetInt(ro, "gold"), ElixirLost = MiniJson.GetInt(ro, "elixir"), TrophyDelta = MiniJson.GetInt(ro, "trophies"),
                        AtUnix = MiniJson.GetLong(ro, "at"), Seen = MiniJson.GetBool(ro, "seen"),
                    });
                }
                s.Village.NextId = MiniJson.GetInt(o, "nextId", 1);
                s.Village.RebuildGrid();
                if (s.Village.TownHall == null) return false;
                state = s;
                return true;
            }
            catch (Exception)
            {
                state = null;
                return false;
            }
        }

        /// <summary>Creates the starter village.</summary>
        public static GameState NewGame(int seed, long now)
        {
            var s = new GameState { Seed = seed, CreatedUnix = now, LastSeenUnix = now, Gold = GameData.StartingGold, Elixir = GameData.StartingElixir, Gems = GameData.StartingGems };
            var v = s.Village;
            v.TryPlace(BuildingType.TownHall, new Int2(18, 18));
            v.TryPlace(BuildingType.BuilderHut, new Int2(23, 17));
            v.TryPlace(BuildingType.GoldMine, new Int2(13, 20));
            v.TryPlace(BuildingType.ElixirCollector, new Int2(20, 13));
            v.TryPlace(BuildingType.Cannon, new Int2(14, 15));
            v.TryPlace(BuildingType.Barracks, new Int2(23, 21));
            v.TryPlace(BuildingType.ArmyCamp, new Int2(16, 24));
            var rng = new Random(seed);
            var kinds = new[] { ObstacleKind.Tree, ObstacleKind.Tree, ObstacleKind.Bush, ObstacleKind.Rock, ObstacleKind.Stump, ObstacleKind.Tree };
            int placed = 0, tries = 0;
            while (placed < 14 && tries < 400)
            {
                tries++;
                var k = kinds[rng.Next(kinds.Length)];
                var p = new Int2(rng.Next(2, 37), rng.Next(2, 37));
                // keep obstacles away from the starting cluster
                if (p.X >= 10 && p.X <= 28 && p.Y >= 10 && p.Y <= 29) continue;
                if (v.TryPlaceObstacle(k, p) != null) placed++;
            }
            s.ShieldUntilUnix = now + 3 * 24 * 3600; // starter shield
            return s;
        }
    }
}
