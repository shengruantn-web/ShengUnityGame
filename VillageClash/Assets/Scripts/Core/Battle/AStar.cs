using System;
using System.Collections.Generic;

namespace VillageClash.Core
{
    /// <summary>8-neighbour A* over a square grid. Cost cells: &lt;0 blocked, otherwise extra cost added when entering.</summary>
    public static class AStar
    {
        public const float WallCost = 50f;
        const float Diag = 1.41421356f;

        /// <summary>Returns the path (excluding start, including goal) or null if unreachable. Empty list if start is a goal.</summary>
        public static List<Int2> FindPath(int size, float[] cost, Int2 start, Func<Int2, bool> isGoal, Func<Int2, float> heuristic, int maxExpansions = 40000)
        {
            int n = size * size;
            if (!InBounds(size, start)) return null;
            if (isGoal(start)) return new List<Int2>();
            var g = new float[n];
            var parent = new int[n];
            var closed = new bool[n];
            for (int i = 0; i < n; i++) { g[i] = float.PositiveInfinity; parent[i] = -1; }
            var open = new MinHeap();
            int s = start.Y * size + start.X;
            g[s] = 0;
            open.Push(heuristic(start), s);
            int expansions = 0;
            while (open.Count > 0)
            {
                open.Pop(out _, out int cur);
                if (closed[cur]) continue;
                closed[cur] = true;
                var cc = new Int2(cur % size, cur / size);
                if (isGoal(cc)) return Reconstruct(size, parent, cur, s);
                if (++expansions > maxExpansions) return null;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = cc.X + dx, ny = cc.Y + dy;
                        if (nx < 0 || ny < 0 || nx >= size || ny >= size) continue;
                        int ni = ny * size + nx;
                        if (closed[ni]) continue;
                        float extra = cost[ni];
                        if (extra < 0) continue;
                        bool diag = dx != 0 && dy != 0;
                        if (diag)
                        {
                            // no corner cutting past anything solid (walls included)
                            if (cost[cc.Y * size + nx] != 0 || cost[ny * size + cc.X] != 0) continue;
                        }
                        float ng = g[cur] + (diag ? Diag : 1f) + extra;
                        if (ng < g[ni])
                        {
                            g[ni] = ng;
                            parent[ni] = cur;
                            open.Push(ng + heuristic(new Int2(nx, ny)), ni);
                        }
                    }
            }
            return null;
        }

        static bool InBounds(int size, Int2 p) => p.X >= 0 && p.Y >= 0 && p.X < size && p.Y < size;

        static List<Int2> Reconstruct(int size, int[] parent, int cur, int start)
        {
            var path = new List<Int2>();
            while (cur != start && cur >= 0)
            {
                path.Add(new Int2(cur % size, cur / size));
                cur = parent[cur];
            }
            path.Reverse();
            return path;
        }

        sealed class MinHeap
        {
            readonly List<float> keys = new List<float>();
            readonly List<int> vals = new List<int>();
            public int Count => keys.Count;

            public void Push(float k, int v)
            {
                keys.Add(k); vals.Add(v);
                int i = keys.Count - 1;
                while (i > 0)
                {
                    int p = (i - 1) / 2;
                    if (keys[p] <= keys[i]) break;
                    Swap(i, p); i = p;
                }
            }

            public void Pop(out float k, out int v)
            {
                k = keys[0]; v = vals[0];
                int last = keys.Count - 1;
                keys[0] = keys[last]; vals[0] = vals[last];
                keys.RemoveAt(last); vals.RemoveAt(last);
                int i = 0;
                while (true)
                {
                    int l = 2 * i + 1, r = l + 1, m = i;
                    if (l < keys.Count && keys[l] < keys[m]) m = l;
                    if (r < keys.Count && keys[r] < keys[m]) m = r;
                    if (m == i) break;
                    Swap(i, m); i = m;
                }
            }

            void Swap(int a, int b)
            {
                var tk = keys[a]; keys[a] = keys[b]; keys[b] = tk;
                var tv = vals[a]; vals[a] = vals[b]; vals[b] = tv;
            }
        }
    }
}
