using System;

namespace VillageClash.Core
{
    /// <summary>Occupancy grid. Each cell stores the id of its occupant or -1.</summary>
    public sealed class GridMap
    {
        public const int Empty = -1;
        public readonly int Size;
        readonly int[] _cells;

        public GridMap(int size = GameData.GridSize)
        {
            Size = size;
            _cells = new int[size * size];
            for (int i = 0; i < _cells.Length; i++) _cells[i] = Empty;
        }

        public bool InBounds(Int2 c) => c.X >= 0 && c.Y >= 0 && c.X < Size && c.Y < Size;
        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Size && y < Size;

        public int OccupantAt(Int2 c) => InBounds(c) ? _cells[c.Y * Size + c.X] : Empty;
        public int OccupantAt(int x, int y) => InBounds(x, y) ? _cells[y * Size + x] : Empty;

        public bool FootprintInBounds(Int2 origin, int size)
            => origin.X >= 0 && origin.Y >= 0 && origin.X + size <= Size && origin.Y + size <= Size;

        /// <summary>True when every cell of the footprint is in bounds and empty (or occupied by ignoreId).</summary>
        public bool CanPlace(Int2 origin, int size, int ignoreId = Empty)
        {
            if (!FootprintInBounds(origin, size)) return false;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int o = _cells[(origin.Y + y) * Size + origin.X + x];
                    if (o != Empty && o != ignoreId) return false;
                }
            return true;
        }

        public void Occupy(int id, Int2 origin, int size)
        {
            if (!FootprintInBounds(origin, size)) throw new ArgumentOutOfRangeException(nameof(origin));
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    _cells[(origin.Y + y) * Size + origin.X + x] = id;
        }

        public void Free(Int2 origin, int size)
        {
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (InBounds(origin.X + x, origin.Y + y))
                        _cells[(origin.Y + y) * Size + origin.X + x] = Empty;
        }

        public void Clear()
        {
            for (int i = 0; i < _cells.Length; i++) _cells[i] = Empty;
        }
    }
}
