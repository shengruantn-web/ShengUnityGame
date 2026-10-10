using System;

namespace VillageClash.Core
{
    /// <summary>Integer grid coordinate (engine-free).</summary>
    [Serializable]
    public struct Int2 : IEquatable<Int2>
    {
        public int X;
        public int Y;
        public Int2(int x, int y) { X = x; Y = y; }
        public static readonly Int2 Zero = new Int2(0, 0);
        public static Int2 operator +(Int2 a, Int2 b) => new Int2(a.X + b.X, a.Y + b.Y);
        public static Int2 operator -(Int2 a, Int2 b) => new Int2(a.X - b.X, a.Y - b.Y);
        public static bool operator ==(Int2 a, Int2 b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(Int2 a, Int2 b) => !(a == b);
        public bool Equals(Int2 o) => X == o.X && Y == o.Y;
        public override bool Equals(object obj) => obj is Int2 o && Equals(o);
        public override int GetHashCode() => X * 73856093 ^ Y * 19349663;
        public override string ToString() => $"({X},{Y})";
        public int ManhattanTo(Int2 o) => Math.Abs(X - o.X) + Math.Abs(Y - o.Y);
        public static int Chebyshev(Int2 a, Int2 b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    }
}
