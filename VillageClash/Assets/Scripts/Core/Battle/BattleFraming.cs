using System.Collections.Generic;

namespace VillageClash.Core
{
    /// <summary>
    /// Picks an orthographic camera center and half-height (zoom) that shows every enemy building,
    /// keeping the base out from under HUD strips at the top and bottom of the screen.
    /// Pure math so it can be unit-tested; the caller clamps the result through the camera rig.
    /// </summary>
    public static class BattleFraming
    {
        /// <summary>World-unit padding around the footprint box so edge buildings do not touch the screen edge.</summary>
        public const float Margin = 1.0f;
        /// <summary>Extra room above the box: building sprites rise above their footprint diamond.</summary>
        public const float TopExtra = 1.0f;
        public const float FallbackZoom = 9f;

        /// <param name="buildings">Enemy buildings; traps are ignored (they are hidden during a raid).</param>
        /// <param name="aspect">Screen width / height.</param>
        /// <param name="topFrac">Fraction of screen height covered by HUD at the top.</param>
        /// <param name="bottomFrac">Fraction of screen height covered by HUD at the bottom.</param>
        public static void Fit(IReadOnlyList<BuildingInstance> buildings, float aspect, float topFrac, float bottomFrac,
            float minZoom, float maxZoom, out float cx, out float cy, out float zoom)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            bool any = false;
            if (buildings != null)
            {
                foreach (var b in buildings)
                {
                    if (b == null || b.Def.IsTrap) continue;
                    any = true;
                    // Footprint diamond extremes in iso: left = (x, y+s), right = (x+s, y), bottom = (x, y), top = (x+s, y+s).
                    IsoMath.CellToWorld(b.Origin.X, b.Origin.Y + b.Size, out var lx, out _);
                    IsoMath.CellToWorld(b.Origin.X + b.Size, b.Origin.Y, out var rx, out _);
                    IsoMath.CellToWorld(b.Origin.X, b.Origin.Y, out _, out var by);
                    IsoMath.CellToWorld(b.Origin.X + b.Size, b.Origin.Y + b.Size, out _, out var ty);
                    if (lx < minX) minX = lx;
                    if (rx > maxX) maxX = rx;
                    if (by < minY) minY = by;
                    if (ty > maxY) maxY = ty;
                }
            }
            if (!any)
            {
                cx = 0f; cy = GameData.GridSize * 0.25f;
                zoom = Clamp(FallbackZoom, minZoom, maxZoom);
                return;
            }
            minX -= Margin; maxX += Margin; minY -= Margin; maxY += Margin + TopExtra;

            float usable = 1f - topFrac - bottomFrac;
            if (usable < 0.1f) usable = 0.1f;
            if (aspect < 0.1f) aspect = 0.1f;
            float needH = (maxY - minY) / (2f * usable);
            float needW = (maxX - minX) / (2f * aspect);
            zoom = Clamp(needH > needW ? needH : needW, minZoom, maxZoom);

            cx = (minX + maxX) * 0.5f;
            // The unobstructed band is centred at screen fraction (bottomFrac + 1 - topFrac) / 2 from the bottom;
            // move the camera so the box centre lands there.
            cy = (minY + maxY) * 0.5f - zoom * (bottomFrac - topFrac);
        }

        static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;
    }
}
