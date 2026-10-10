namespace VillageClash.Core
{
    /// <summary>2:1 isometric projection. One grid cell is 1.0 world units wide and 0.5 tall.</summary>
    public static class IsoMath
    {
        public const float HalfW = 0.5f;
        public const float HalfH = 0.25f;

        /// <summary>Cell-space (continuous) to world. Cell (0,0) corner is at world origin.</summary>
        public static void CellToWorld(float cx, float cy, out float wx, out float wy)
        {
            wx = (cx - cy) * HalfW;
            wy = (cx + cy) * HalfH;
        }

        public static void WorldToCell(float wx, float wy, out float cx, out float cy)
        {
            float a = wx / HalfW;  // cx - cy
            float b = wy / HalfH;  // cx + cy
            cx = (a + b) * 0.5f;
            cy = (b - a) * 0.5f;
        }

        public static Int2 WorldToCellInt(float wx, float wy)
        {
            WorldToCell(wx, wy, out var cx, out var cy);
            return new Int2((int)System.Math.Floor(cx), (int)System.Math.Floor(cy));
        }

        /// <summary>Center of a square footprint in world space.</summary>
        public static void FootprintCenter(Int2 origin, int size, out float wx, out float wy)
            => CellToWorld(origin.X + size * 0.5f, origin.Y + size * 0.5f, out wx, out wy);
    }
}
