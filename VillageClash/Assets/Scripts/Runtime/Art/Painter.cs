using System;
using UnityEngine;

namespace VillageClash
{
    /// <summary>Tiny software rasterizer into a Color32 buffer. All coordinates are pixels, origin bottom-left.</summary>
    public sealed class Painter
    {
        public readonly int W, H;
        public readonly Color32[] Px;

        public Painter(int w, int h)
        {
            W = w; H = h; Px = new Color32[w * h];
        }

        public void Set(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            if (c.a >= 0.999f) { Px[y * W + x] = c; return; }
            var d = (Color)Px[y * W + x];
            float a = c.a + d.a * (1 - c.a);
            if (a <= 0) return;
            var o = new Color((c.r * c.a + d.r * d.a * (1 - c.a)) / a, (c.g * c.a + d.g * d.a * (1 - c.a)) / a, (c.b * c.a + d.b * d.a * (1 - c.a)) / a, a);
            Px[y * W + x] = o;
        }

        public void Fill(Color c) { for (int i = 0; i < Px.Length; i++) Px[i] = c; }

        public void Rect(float x0, float y0, float x1, float y1, Color c)
        {
            int ax = Mathf.RoundToInt(Mathf.Min(x0, x1)), bx = Mathf.RoundToInt(Mathf.Max(x0, x1));
            int ay = Mathf.RoundToInt(Mathf.Min(y0, y1)), by = Mathf.RoundToInt(Mathf.Max(y0, y1));
            for (int y = ay; y < by; y++) for (int x = ax; x < bx; x++) Set(x, y, c);
        }

        public void RectOutline(float x0, float y0, float x1, float y1, float t, Color c)
        {
            Rect(x0, y0, x1, y0 + t, c); Rect(x0, y1 - t, x1, y1, c);
            Rect(x0, y0, x0 + t, y1, c); Rect(x1 - t, y0, x1, y1, c);
        }

        public void Circle(float cx, float cy, float r, Color c) => Ellipse(cx, cy, r, r, c);

        public void Ellipse(float cx, float cy, float rx, float ry, Color c)
        {
            int x0 = Mathf.FloorToInt(cx - rx) - 1, x1 = Mathf.CeilToInt(cx + rx) + 1;
            int y0 = Mathf.FloorToInt(cy - ry) - 1, y1 = Mathf.CeilToInt(cy + ry) + 1;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    float d = dx * dx + dy * dy;
                    if (d <= 1f) Set(x, y, c);
                    else if (d < 1.25f) Set(x, y, Palette.Alpha(c, c.a * Mathf.Clamp01((1.25f - d) / 0.25f) * 0.8f));
                }
        }

        public void Ring(float cx, float cy, float r, float t, Color c)
        {
            int x0 = Mathf.FloorToInt(cx - r) - 1, x1 = Mathf.CeilToInt(cx + r) + 1;
            int y0 = Mathf.FloorToInt(cy - r) - 1, y1 = Mathf.CeilToInt(cy + r) + 1;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                    if (d <= r && d >= r - t) Set(x, y, c);
                }
        }

        /// <summary>Isometric diamond centred at (cx,cy) with half width hw and half height hh.</summary>
        public void Diamond(float cx, float cy, float hw, float hh, Color c)
        {
            int y0 = Mathf.FloorToInt(cy - hh), y1 = Mathf.CeilToInt(cy + hh);
            for (int y = y0; y <= y1; y++)
            {
                float fy = Mathf.Abs((y + 0.5f - cy) / hh);
                if (fy > 1) continue;
                float half = hw * (1 - fy);
                int xa = Mathf.RoundToInt(cx - half), xb = Mathf.RoundToInt(cx + half);
                for (int x = xa; x < xb; x++) Set(x, y, c);
            }
        }

        public void Poly(Color c, params Vector2[] pts)
        {
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in pts) { minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y); }
            int y0 = Mathf.FloorToInt(minY), y1 = Mathf.CeilToInt(maxY);
            var xs = new float[pts.Length];
            for (int y = y0; y <= y1; y++)
            {
                float sy = y + 0.5f;
                int n = 0;
                for (int i = 0; i < pts.Length; i++)
                {
                    var a = pts[i]; var b = pts[(i + 1) % pts.Length];
                    if ((sy >= a.y && sy < b.y) || (sy >= b.y && sy < a.y))
                        xs[n++] = a.x + (sy - a.y) / (b.y - a.y) * (b.x - a.x);
                }
                Array.Sort(xs, 0, n);
                for (int i = 0; i + 1 < n; i += 2)
                {
                    int xa = Mathf.RoundToInt(xs[i]), xb = Mathf.RoundToInt(xs[i + 1]);
                    for (int x = xa; x < xb; x++) Set(x, y, c);
                }
            }
        }

        public void Line(float x0, float y0, float x1, float y1, float t, Color c)
        {
            float len = Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1));
            int steps = Mathf.CeilToInt(len * 2) + 1;
            for (int i = 0; i <= steps; i++)
            {
                float f = i / (float)steps;
                Circle(Mathf.Lerp(x0, x1, f), Mathf.Lerp(y0, y1, f), t * 0.5f, c);
            }
        }

        public void RoundRect(float x0, float y0, float x1, float y1, float r, Color c)
        {
            r = Mathf.Min(r, (x1 - x0) / 2f, (y1 - y0) / 2f);
            Rect(x0 + r, y0, x1 - r, y1, c);
            Rect(x0, y0 + r, x0 + r, y1 - r, c);
            Rect(x1 - r, y0 + r, x1, y1 - r, c);
            Circle(x0 + r, y0 + r, r, c); Circle(x1 - r, y0 + r, r, c);
            Circle(x0 + r, y1 - r, r, c); Circle(x1 - r, y1 - r, r, c);
        }

        /// <summary>Iso block: footprint diamond centre (cx, baseY), half sizes hw/hh, extruded up by h px.</summary>
        public void Block(float cx, float baseY, float hw, float hh, float h, Color top, Color left, Color right)
        {
            // left face
            Poly(left, new Vector2(cx - hw, baseY), new Vector2(cx, baseY - hh), new Vector2(cx, baseY - hh + h), new Vector2(cx - hw, baseY + h));
            // right face
            Poly(right, new Vector2(cx, baseY - hh), new Vector2(cx + hw, baseY), new Vector2(cx + hw, baseY + h), new Vector2(cx, baseY - hh + h));
            Diamond(cx, baseY + h, hw, hh, top);
        }

        public void Shadow(float cx, float cy, float hw, float hh) => Diamond(cx, cy, hw, hh, new Color(0, 0, 0, 0.25f));

        public Texture2D ToTexture(FilterMode filter = FilterMode.Bilinear)
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = filter, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(Px);
            tex.Apply(false, false);
            return tex;
        }

        public Sprite ToSprite(float ppu, Vector2 pivot, Vector4 border = default, FilterMode filter = FilterMode.Bilinear)
        {
            var tex = ToTexture(filter);
            return Sprite.Create(tex, new Rect(0, 0, W, H), pivot, ppu, 0, SpriteMeshType.FullRect, border);
        }
    }
}
