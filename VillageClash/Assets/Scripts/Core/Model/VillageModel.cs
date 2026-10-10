using System;
using System.Collections.Generic;

namespace VillageClash.Core
{
    /// <summary>A village layout: buildings + obstacles on a grid. No economy rules here.</summary>
    public sealed class VillageModel
    {
        public readonly List<BuildingInstance> Buildings = new List<BuildingInstance>();
        public readonly List<ObstacleInstance> Obstacles = new List<ObstacleInstance>();
        public readonly GridMap Grid;
        public int NextId = 1;

        public VillageModel(int gridSize = GameData.GridSize) { Grid = new GridMap(gridSize); }

        public BuildingInstance Find(int id)
        {
            foreach (var b in Buildings) if (b.Id == id) return b;
            return null;
        }

        public ObstacleInstance FindObstacle(int id)
        {
            foreach (var o in Obstacles) if (o.Id == id) return o;
            return null;
        }

        public BuildingInstance TownHall => FindFirst(BuildingType.TownHall);

        public BuildingInstance FindFirst(BuildingType t)
        {
            foreach (var b in Buildings) if (b.Type == t) return b;
            return null;
        }

        public int Count(BuildingType t)
        {
            int n = 0;
            foreach (var b in Buildings) if (b.Type == t) n++;
            return n;
        }

        public int TownHallLevel => TownHall?.Level ?? 1;

        public bool CanPlace(BuildingType type, Int2 origin, int ignoreId = GridMap.Empty)
            => Grid.CanPlace(origin, GameData.Building(type).Size, ignoreId);

        public BuildingInstance TryPlace(BuildingType type, Int2 origin, int level = 1)
        {
            int size = GameData.Building(type).Size;
            if (!Grid.CanPlace(origin, size)) return null;
            var b = new BuildingInstance { Id = NextId++, Type = type, Level = level, Origin = origin, Size = size };
            Buildings.Add(b);
            Grid.Occupy(b.Id, origin, size);
            return b;
        }

        public bool TryMove(int id, Int2 origin)
        {
            var b = Find(id);
            if (b == null) return false;
            if (!Grid.CanPlace(origin, b.Size, id)) return false;
            Grid.Free(b.Origin, b.Size);
            b.Origin = origin;
            Grid.Occupy(b.Id, origin, b.Size);
            return true;
        }

        public bool Remove(int id)
        {
            var b = Find(id);
            if (b == null) return false;
            Grid.Free(b.Origin, b.Size);
            Buildings.Remove(b);
            return true;
        }

        public ObstacleInstance TryPlaceObstacle(ObstacleKind kind, Int2 origin)
        {
            int size = GameData.ObstacleSize(kind);
            if (!Grid.CanPlace(origin, size)) return null;
            var o = new ObstacleInstance { Id = NextId++, Kind = kind, Origin = origin, Size = size };
            Obstacles.Add(o);
            Grid.Occupy(o.Id, origin, size);
            return o;
        }

        public bool RemoveObstacle(int id)
        {
            var o = FindObstacle(id);
            if (o == null) return false;
            Grid.Free(o.Origin, o.Size);
            Obstacles.Remove(o);
            return true;
        }

        /// <summary>Returns the building whose footprint contains the cell, or null.</summary>
        public BuildingInstance BuildingAt(Int2 cell)
        {
            int id = Grid.OccupantAt(cell);
            return id == GridMap.Empty ? null : Find(id);
        }

        public ObstacleInstance ObstacleAt(Int2 cell)
        {
            int id = Grid.OccupantAt(cell);
            return id == GridMap.Empty ? null : FindObstacle(id);
        }

        /// <summary>Rebuilds grid occupancy from the lists (after deserialization).</summary>
        public void RebuildGrid()
        {
            Grid.Clear();
            int max = 0;
            foreach (var b in Buildings) { Grid.Occupy(b.Id, b.Origin, b.Size); if (b.Id > max) max = b.Id; }
            foreach (var o in Obstacles) { Grid.Occupy(o.Id, o.Origin, o.Size); if (o.Id > max) max = o.Id; }
            if (NextId <= max) NextId = max + 1;
        }
    }
}
