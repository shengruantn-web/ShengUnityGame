using System.Collections.Generic;
using UnityEngine;
using VillageClash.Core;

namespace VillageClash
{
    /// <summary>All art is generated at runtime from code. Sprites are cached by key.</summary>
    public static class SpriteFactory
    {
        public const int Ppu = 64;           // pixels per world unit; one grid cell is 64 px wide, 32 px tall
        public const int CellW = 64, CellH = 32;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string key)
        {
            if (cache.TryGetValue(key, out var s)) return s;
            s = Make(key);
            cache[key] = s;
            return s;
        }

        public static Sprite Building(BuildingType type, int level, bool ghost = false) => Get($"b:{type}:{level}:{(ghost ? 1 : 0)}");
        public static Sprite Rubble(int size) => Get($"rubble:{size}");
        public static Sprite Troop(TroopType type) => Get($"t:{type}");
        public static Sprite Obstacle(ObstacleKind kind) => Get($"o:{kind}");
        public static Sprite Icon(string name) => Get($"i:{name}");
        public static Sprite Ground(int size) => Get($"ground:{size}");
        public static Sprite Tile => Get("tile");
        public static Sprite RoundRect => Get("ui:rr");
        public static Sprite Bevel => Get("ui:bevel");
        public static Sprite Circle => Get("ui:circle");
        public static Sprite Square => Get("ui:square");
        public static Sprite Projectile(ProjectileKind k) => Get($"p:{k}");

        static Sprite Make(string key)
        {
            var parts = key.Split(':');
            switch (parts[0])
            {
                case "b": return MakeBuilding((BuildingType)System.Enum.Parse(typeof(BuildingType), parts[1]), int.Parse(parts[2]), parts[3] == "1");
                case "rubble": return MakeRubble(int.Parse(parts[1]));
                case "t": return MakeTroop((TroopType)System.Enum.Parse(typeof(TroopType), parts[1]));
                case "o": return MakeObstacle((ObstacleKind)System.Enum.Parse(typeof(ObstacleKind), parts[1]));
                case "i": return MakeIcon(parts[1]);
                case "ground": return MakeGround(int.Parse(parts[1]));
                case "tile": return MakeTile();
                case "ui": return MakeUi(parts[1]);
                case "p": return MakeProjectile((ProjectileKind)System.Enum.Parse(typeof(ProjectileKind), parts[1]));
            }
            return MakeUi("square");
        }

        // ---- UI primitives ---------------------------------------------------------
        static Sprite MakeUi(string kind)
        {
            switch (kind)
            {
                case "rr":
                {
                    var p = new Painter(64, 64);
                    p.RoundRect(0, 0, 64, 64, 14, Color.white);
                    return p.ToSprite(1, new Vector2(0.5f, 0.5f), new Vector4(20, 20, 20, 20));
                }
                case "bevel":
                {
                    var p = new Painter(64, 64);
                    p.RoundRect(0, 0, 64, 64, 14, new Color(0.78f, 0.78f, 0.78f));
                    p.RoundRect(0, 30, 64, 64, 14, Color.white);
                    p.Rect(4, 26, 60, 34, Color.white);
                    return p.ToSprite(1, new Vector2(0.5f, 0.5f), new Vector4(20, 20, 20, 20));
                }
                case "circle":
                {
                    var p = new Painter(64, 64);
                    p.Circle(32, 32, 31, Color.white);
                    return p.ToSprite(1, new Vector2(0.5f, 0.5f));
                }
                default:
                {
                    var p = new Painter(8, 8);
                    p.Fill(Color.white);
                    return p.ToSprite(1, new Vector2(0.5f, 0.5f));
                }
            }
        }

        static Sprite MakeTile()
        {
            var p = new Painter(CellW, CellH);
            p.Diamond(32, 16, 31, 15.5f, Color.white);
            return p.ToSprite(Ppu, new Vector2(0.5f, 0.5f));
        }

        static Sprite MakeGround(int size)
        {
            int margin = 48;
            int w = size * CellW + margin * 2, h = size * CellH + margin * 2;
            var p = new Painter(w, h);
            float cx = w / 2f, cy = h / 2f;
            // dirt rim under the plateau
            p.Diamond(cx, cy - 10, size * CellW / 2f + 40, size * CellH / 2f + 20, Palette.Hex("5b4426"));
            p.Diamond(cx, cy, size * CellW / 2f + 36, size * CellH / 2f + 18, Palette.GrassEdge);
            p.Diamond(cx, cy, size * CellW / 2f + 20, size * CellH / 2f + 10, Palette.GrassDark);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float px = cx + (x - y) * CellW / 2f;
                    float py = cy + (x + y + 1 - size) * CellH / 2f;
                    var c = ((x + y) & 1) == 0 ? Palette.Grass1 : Palette.Grass2;
                    p.Diamond(px, py, CellW / 2f, CellH / 2f, c);
                }
            // a few lighter tufts
            var rng = new System.Random(3);
            for (int i = 0; i < size * 6; i++)
            {
                int x = rng.Next(size), y = rng.Next(size);
                float px = cx + (x - y) * CellW / 2f + rng.Next(-20, 20);
                float py = cy + (x + y + 1 - size) * CellH / 2f + rng.Next(-8, 8);
                p.Ellipse(px, py, 5, 2.5f, Palette.Alpha(Palette.Hex("9ad65a"), 0.6f));
            }
            return p.ToSprite(Ppu, new Vector2(0.5f, 0.5f));
        }

        // ---- buildings ------------------------------------------------------------
        static Sprite MakeBuilding(BuildingType type, int level, bool ghost)
        {
            var def = GameData.Building(type);
            int s = def.Size;
            int w = s * CellW + 16;
            int extra = 140;
            int h = s * CellH + extra;
            var p = new Painter(w, h);
            float cx = w / 2f;
            float hh = s * CellH / 2f, hw = s * CellW / 2f;
            float baseY = hh + 8;
            float inset = 5f;
            float fhw = hw - inset, fhh = hh - inset / 2f;
            p.Shadow(cx, baseY - 2, hw, hh);
            int lvl = Mathf.Clamp(level, 1, 5);
            float hscale = 1f + (lvl - 1) * 0.06f;
            switch (type)
            {
                case BuildingType.TownHall: DrawTownHall(p, cx, baseY, fhw, fhh, lvl); break;
                case BuildingType.BuilderHut: DrawHut(p, cx, baseY, fhw, fhh, Palette.Wood, Palette.Hex("c9a227")); break;
                case BuildingType.GoldMine: DrawMine(p, cx, baseY, fhw, fhh, Palette.Gold, lvl); break;
                case BuildingType.ElixirCollector: DrawCollector(p, cx, baseY, fhw, fhh, lvl); break;
                case BuildingType.GoldStorage: DrawStorage(p, cx, baseY, fhw, fhh, Palette.Gold, Palette.GoldDark, lvl); break;
                case BuildingType.ElixirStorage: DrawStorage(p, cx, baseY, fhw, fhh, Palette.Elixir, Palette.ElixirDark, lvl); break;
                case BuildingType.Barracks: DrawBarracks(p, cx, baseY, fhw, fhh, lvl); break;
                case BuildingType.ArmyCamp: DrawCamp(p, cx, baseY, fhw, fhh, lvl); break;
                case BuildingType.Laboratory: DrawLab(p, cx, baseY, fhw, fhh, lvl); break;
                case BuildingType.Cannon: DrawCannon(p, cx, baseY, fhw, fhh, lvl); break;
                case BuildingType.ArcherTower: DrawArcherTower(p, cx, baseY, fhw, fhh, lvl); break;
                case BuildingType.Mortar: DrawMortar(p, cx, baseY, fhw, fhh, lvl); break;
                case BuildingType.Wall: DrawWall(p, cx, baseY, hw - 2, hh - 1, lvl); break;
                case BuildingType.Bomb: DrawBomb(p, cx, baseY); break;
                case BuildingType.SpringTrap: DrawSpring(p, cx, baseY, hw - 6, hh - 3); break;
            }
            if (ghost) for (int i = 0; i < p.Px.Length; i++) { var c = p.Px[i]; c.a = (byte)(c.a * 0.6f); p.Px[i] = c; }
            return p.ToSprite(Ppu, new Vector2(0.5f, baseY / h));
        }

        static void Faces(Color c, out Color top, out Color left, out Color right)
        {
            top = Palette.Shade(c, 1.15f); left = Palette.Shade(c, 0.92f); right = Palette.Shade(c, 0.72f);
        }

        static void StoneBlock(Painter p, float cx, float baseY, float hw, float hh, float h, Color c)
        {
            Faces(c, out var t, out var l, out var r);
            p.Block(cx, baseY, hw, hh, h, t, l, r);
            // dark bottom edge
            p.Poly(new Color(0, 0, 0, 0.18f), new Vector2(cx - hw, baseY), new Vector2(cx, baseY - hh), new Vector2(cx, baseY - hh + 3), new Vector2(cx - hw, baseY + 3));
            p.Poly(new Color(0, 0, 0, 0.3f), new Vector2(cx, baseY - hh), new Vector2(cx + hw, baseY), new Vector2(cx + hw, baseY + 3), new Vector2(cx, baseY - hh + 3));
        }

        static void Roof(Painter p, float cx, float y, float hw, float hh, float peak, Color c)
        {
            // pyramid roof over a diamond footprint: four triangular faces; draw back faces first
            var top = new Vector2(cx, y + peak);
            p.Poly(Palette.Shade(c, 1.1f), new Vector2(cx - hw, y), new Vector2(cx, y + hh), top);
            p.Poly(Palette.Shade(c, 1.2f), new Vector2(cx, y + hh), new Vector2(cx + hw, y), top);
            p.Poly(Palette.Shade(c, 0.95f), new Vector2(cx - hw, y), new Vector2(cx, y - hh), top);
            p.Poly(Palette.Shade(c, 0.75f), new Vector2(cx, y - hh), new Vector2(cx + hw, y), top);
        }

        static void DrawTownHall(Painter p, float cx, float baseY, float hw, float hh, int lvl)
        {
            StoneBlock(p, cx, baseY, hw, hh, 10, Palette.Stone);
            float h = 34 + lvl * 4;
            StoneBlock(p, cx, baseY + 10, hw * 0.78f, hh * 0.78f, h, Palette.Hex("c89a6b"));
            // door
            p.Rect(cx - 7, baseY + 4, cx + 7, baseY + 20, Palette.WoodDark);
            p.Circle(cx, baseY + 20, 7, Palette.WoodDark);
            // windows
            p.Rect(cx - hw * 0.5f, baseY + h - 8, cx - hw * 0.5f + 8, baseY + h + 2, Palette.Hex("3b5f8a"));
            p.Rect(cx + hw * 0.3f, baseY + h - 14, cx + hw * 0.3f + 8, baseY + h - 4, Palette.Hex("3b5f8a"));
            Roof(p, cx, baseY + 10 + h, hw * 0.82f, hh * 0.82f, 24 + lvl * 3, lvl >= 3 ? Palette.Hex("c9a227") : Palette.Roof);
            // flag
            float fy = baseY + 10 + h + 24 + lvl * 3;
            p.Rect(cx - 1, fy, cx + 1, fy + 22, Palette.WoodDark);
            p.Poly(Palette.Hex("e74c3c"), new Vector2(cx + 1, fy + 22), new Vector2(cx + 16, fy + 17), new Vector2(cx + 1, fy + 11));
            if (lvl >= 2) { p.Rect(cx - hw * 0.9f, baseY + 8, cx - hw * 0.9f + 6, baseY + 30, Palette.StoneDark); p.Rect(cx + hw * 0.9f - 6, baseY + 8, cx + hw * 0.9f, baseY + 30, Palette.StoneDark); }
        }

        static void DrawHut(Painter p, float cx, float baseY, float hw, float hh, Color wall, Color roof)
        {
            StoneBlock(p, cx, baseY, hw, hh, 18, wall);
            p.Rect(cx - 5, baseY + 2, cx + 5, baseY + 14, Palette.WoodDark);
            Roof(p, cx, baseY + 18, hw * 1.05f, hh * 1.05f, 18, roof);
        }

        static void DrawMine(Painter p, float cx, float baseY, float hw, float hh, Color gold, int lvl)
        {
            StoneBlock(p, cx, baseY, hw, hh, 8, Palette.Hex("8d7355"));
            // mine shaft arch
            StoneBlock(p, cx - hw * 0.25f, baseY + 8, hw * 0.5f, hh * 0.5f, 16, Palette.StoneDark);
            p.Rect(cx - hw * 0.25f - 7, baseY + 10, cx - hw * 0.25f + 7, baseY + 26, Palette.Black);
            p.Circle(cx - hw * 0.25f, baseY + 26, 7, Palette.Black);
            // gold nuggets
            for (int i = 0; i < 2 + lvl; i++)
            {
                float ox = cx + hw * 0.35f + (i % 3) * 9 - 9, oy = baseY + 12 + (i / 3) * 8;
                p.Circle(ox, oy, 5, gold); p.Circle(ox - 1.5f, oy + 1.5f, 2, Palette.Shade(gold, 1.3f));
            }
            // wooden wheel/pick
            p.Line(cx + hw * 0.1f, baseY + 6, cx + hw * 0.6f, baseY + 30, 3, Palette.Wood);
            p.Rect(cx + hw * 0.6f - 8, baseY + 28, cx + hw * 0.6f + 8, baseY + 33, Palette.StoneDark);
        }

        static void DrawCollector(Painter p, float cx, float baseY, float hw, float hh, int lvl)
        {
            StoneBlock(p, cx, baseY, hw, hh, 8, Palette.Hex("7b6b8d"));
            p.Ellipse(cx, baseY + 14, hw * 0.55f, hh * 0.55f, Palette.Hex("3d2a47"));
            p.Ellipse(cx, baseY + 15, hw * 0.45f, hh * 0.45f, Palette.Elixir);
            p.Ellipse(cx - 4, baseY + 17, hw * 0.12f, hh * 0.12f, Palette.Shade(Palette.Elixir, 1.4f));
            // pump
            p.Rect(cx + hw * 0.45f, baseY + 10, cx + hw * 0.45f + 7, baseY + 36 + lvl * 2, Palette.StoneDark);
            p.Rect(cx + hw * 0.45f - 10, baseY + 32 + lvl * 2, cx + hw * 0.45f + 10, baseY + 38 + lvl * 2, Palette.Stone);
            p.Circle(cx + hw * 0.45f + 3, baseY + 42 + lvl * 2, 6, Palette.Elixir);
        }

        static void DrawStorage(Painter p, float cx, float baseY, float hw, float hh, Color c, Color dark, int lvl)
        {
            StoneBlock(p, cx, baseY, hw, hh, 6, Palette.StoneDark);
            float h = 26 + lvl * 4;
            // round tank: body ellipse column
            p.Rect(cx - hw * 0.6f, baseY + 6, cx + hw * 0.6f, baseY + 6 + h, dark);
            p.Rect(cx - hw * 0.6f, baseY + 6, cx - hw * 0.1f, baseY + 6 + h, Palette.Shade(dark, 1.25f));
            p.Ellipse(cx, baseY + 6, hw * 0.6f, hh * 0.6f, dark);
            p.Ellipse(cx, baseY + 6 + h, hw * 0.6f, hh * 0.6f, c);
            p.Ellipse(cx - 4, baseY + 8 + h, hw * 0.3f, hh * 0.3f, Palette.Shade(c, 1.3f));
            // bands
            for (int i = 1; i <= lvl; i++) p.Rect(cx - hw * 0.62f, baseY + 6 + i * h / (lvl + 1) - 2, cx + hw * 0.62f, baseY + 6 + i * h / (lvl + 1) + 1, Palette.Hex("4a4a4a"));
        }

        static void DrawBarracks(Painter p, float cx, float baseY, float hw, float hh, int lvl)
        {
            StoneBlock(p, cx, baseY, hw, hh, 22 + lvl * 2, Palette.Hex("b86f3a"));
            p.Rect(cx - 6, baseY + 2, cx + 6, baseY + 16, Palette.WoodDark);
            Roof(p, cx, baseY + 22 + lvl * 2, hw * 1.05f, hh * 1.05f, 20, Palette.Hex("8e3b2f"));
            // sword sign
            p.Line(cx - hw * 0.55f, baseY + 30, cx - hw * 0.55f + 14, baseY + 44, 3, Palette.Stone);
            p.Line(cx - hw * 0.55f + 14, baseY + 30, cx - hw * 0.55f, baseY + 44, 3, Palette.Stone);
        }

        static void DrawCamp(Painter p, float cx, float baseY, float hw, float hh, int lvl)
        {
            p.Diamond(cx, baseY, hw, hh, Palette.Hex("8aa64a"));
            // campfire
            p.Circle(cx, baseY, 7, Palette.StoneDark);
            p.Poly(Palette.Orange, new Vector2(cx - 5, baseY), new Vector2(cx + 5, baseY), new Vector2(cx, baseY + 14));
            p.Poly(Palette.Gold, new Vector2(cx - 2.5f, baseY), new Vector2(cx + 2.5f, baseY), new Vector2(cx, baseY + 9));
            // tents
            Tent(p, cx - hw * 0.5f, baseY + hh * 0.2f, 18, Palette.Hex("c94c3b"));
            Tent(p, cx + hw * 0.45f, baseY + hh * 0.35f, 16, Palette.Hex("d0a040"));
            if (lvl >= 2) Tent(p, cx, baseY + hh * 0.75f, 15, Palette.Hex("4f86c6"));
        }

        static void Tent(Painter p, float x, float y, float s, Color c)
        {
            p.Poly(Palette.Shade(c, 0.8f), new Vector2(x - s, y), new Vector2(x, y + s * 1.3f), new Vector2(x, y - s * 0.3f));
            p.Poly(c, new Vector2(x, y - s * 0.3f), new Vector2(x, y + s * 1.3f), new Vector2(x + s, y));
            p.Poly(Palette.Black, new Vector2(x - 4, y - s * 0.3f + 2), new Vector2(x + 4, y - s * 0.3f + 2), new Vector2(x, y + s * 0.5f));
        }

        static void DrawLab(Painter p, float cx, float baseY, float hw, float hh, int lvl)
        {
            StoneBlock(p, cx, baseY, hw, hh, 20 + lvl * 2, Palette.Hex("6f6f7f"));
            // dome
            p.Ellipse(cx, baseY + 22 + lvl * 2, hw * 0.6f, hh * 0.6f, Palette.Hex("3f6fa8"));
            p.Ellipse(cx, baseY + 22 + lvl * 2, hw * 0.55f, hw * 0.55f * 0.75f, Palette.Hex("4f86c6"));
            p.Ellipse(cx - hw * 0.15f, baseY + 30 + lvl * 2, hw * 0.18f, hw * 0.12f, Palette.Hex("7fb3e6"));
            // flask
            p.Rect(cx + hw * 0.55f, baseY + 20, cx + hw * 0.55f + 6, baseY + 40, Palette.Hex("c9ecff"));
            p.Circle(cx + hw * 0.55f + 3, baseY + 20, 8, Palette.Gem);
        }

        static void DrawCannon(Painter p, float cx, float baseY, float hw, float hh, int lvl)
        {
            StoneBlock(p, cx, baseY, hw, hh, 8, Palette.Hex("7d7d7d"));
            p.Ellipse(cx, baseY + 10, hw * 0.55f, hh * 0.55f, Palette.WoodDark);
            p.Ellipse(cx, baseY + 12, hw * 0.45f, hh * 0.45f, Palette.Wood);
            // barrel pointing down-right
            p.Line(cx - 4, baseY + 22, cx + hw * 0.55f, baseY + 8, 11, Palette.Hex("3a3a3a"));
            p.Line(cx - 4, baseY + 22, cx + hw * 0.55f, baseY + 8, 7, Palette.Hex("555555"));
            p.Circle(cx + hw * 0.55f, baseY + 8, 6, Palette.Black);
            p.Circle(cx - 4, baseY + 22, 7, Palette.Hex("3a3a3a"));
            if (lvl >= 3) p.Ring(cx - 4, baseY + 22, 9, 2, Palette.Gold);
        }

        static void DrawArcherTower(Painter p, float cx, float baseY, float hw, float hh, int lvl)
        {
            StoneBlock(p, cx, baseY, hw, hh, 6, Palette.Hex("7d7d7d"));
            float h = 40 + lvl * 5;
            StoneBlock(p, cx, baseY + 6, hw * 0.55f, hh * 0.55f, h, Palette.Hex("9b7b4f"));
            // platform
            StoneBlock(p, cx, baseY + 6 + h, hw * 0.75f, hh * 0.75f, 6, Palette.Wood);
            // crenellations / posts
            for (int i = -1; i <= 1; i++) p.Rect(cx + i * hw * 0.5f - 2, baseY + 12 + h, cx + i * hw * 0.5f + 2, baseY + 22 + h, Palette.WoodDark);
            // archer
            p.Circle(cx, baseY + 20 + h, 5, Palette.Skin);
            p.Rect(cx - 4, baseY + 8 + h, cx + 4, baseY + 17 + h, Palette.Hex("c0392b"));
        }

        static void DrawMortar(Painter p, float cx, float baseY, float hw, float hh, int lvl)
        {
            StoneBlock(p, cx, baseY, hw, hh, 8, Palette.Hex("6f7276"));
            p.Ellipse(cx, baseY + 12, hw * 0.6f, hh * 0.6f, Palette.StoneDark);
            p.Ellipse(cx, baseY + 14, hw * 0.45f, hw * 0.45f * 0.7f, Palette.Hex("4a4a4a"));
            p.Ellipse(cx, baseY + 20, hw * 0.3f, hw * 0.3f * 0.6f, Palette.Black);
            p.Ellipse(cx, baseY + 22, hw * 0.22f, hw * 0.22f * 0.6f, Palette.Hex("2a2a2a"));
            if (lvl >= 2) p.Ring(cx, baseY + 20, hw * 0.3f + 2, 2, Palette.Gold);
        }

        static void DrawWall(Painter p, float cx, float baseY, float hw, float hh, int lvl)
        {
            Color c = lvl <= 1 ? Palette.Wood : lvl == 2 ? Palette.Stone : lvl == 3 ? Palette.Hex("b9b9b9") : lvl == 4 ? Palette.Hex("c9a227") : Palette.Hex("8bd0e6");
            StoneBlock(p, cx, baseY, hw, hh, 20 + lvl * 2, c);
            if (lvl <= 1) { p.Rect(cx - hw + 2, baseY + 4, cx - hw + 5, baseY + 20, Palette.WoodDark); p.Rect(cx + 2, baseY - hh + 8, cx + 5, baseY + 14, Palette.WoodDark); }
        }

        static void DrawBomb(Painter p, float cx, float baseY)
        {
            p.Circle(cx, baseY + 8, 11, Palette.Black);
            p.Circle(cx - 3, baseY + 11, 4, Palette.Hex("4a4a4a"));
            p.Line(cx + 4, baseY + 18, cx + 10, baseY + 26, 2.5f, Palette.Wood);
            p.Circle(cx + 10, baseY + 27, 3.5f, Palette.Orange);
        }

        static void DrawSpring(Painter p, float cx, float baseY, float hw, float hh)
        {
            p.Diamond(cx, baseY, hw, hh, Palette.Hex("6f6f6f"));
            p.Diamond(cx, baseY + 2, hw * 0.7f, hh * 0.7f, Palette.Hex("9a9a9a"));
            p.Rect(cx - 2, baseY, cx + 2, baseY + 10, Palette.StoneDark);
        }

        static Sprite MakeRubble(int size)
        {
            int w = size * CellW + 16, h = size * CellH + 60;
            var p = new Painter(w, h);
            float cx = w / 2f, hh = size * CellH / 2f, hw = size * CellW / 2f, baseY = hh + 8;
            p.Diamond(cx, baseY, hw - 4, hh - 2, Palette.Hex("5b4a3a"));
            var rng = new System.Random(size);
            for (int i = 0; i < size * 5; i++)
            {
                float ox = cx + (float)(rng.NextDouble() - 0.5) * hw * 1.2f, oy = baseY + (float)(rng.NextDouble() - 0.5) * hh * 1.2f;
                var c = rng.Next(2) == 0 ? Palette.StoneDark : Palette.Hex("8a8a8a");
                p.Block(ox, oy, 6 + rng.Next(6), 3 + rng.Next(3), 4 + rng.Next(6), Palette.Shade(c, 1.1f), c, Palette.Shade(c, 0.8f));
            }
            return p.ToSprite(Ppu, new Vector2(0.5f, baseY / h));
        }

        // ---- troops --------------------------------------------------------------
        static Sprite MakeTroop(TroopType type)
        {
            var p = new Painter(48, 60);
            float cx = 24, y0 = 6;
            p.Ellipse(cx, y0, 12, 5, new Color(0, 0, 0, 0.3f));
            switch (type)
            {
                case TroopType.Barbarian:
                    p.Rect(cx - 7, y0, cx + 7, y0 + 16, Palette.Hex("c9a227")); // kilt
                    p.Rect(cx - 8, y0 + 14, cx + 8, y0 + 30, Palette.Skin);
                    p.Circle(cx, y0 + 36, 8, Palette.Skin);
                    p.Poly(Palette.Gold, new Vector2(cx - 6, y0 + 40), new Vector2(cx + 6, y0 + 40), new Vector2(cx + 2, y0 + 52), new Vector2(cx - 2, y0 + 52)); // hair
                    p.Line(cx + 10, y0 + 14, cx + 18, y0 + 44, 3, Palette.Stone); // sword
                    p.Rect(cx + 7, y0 + 16, cx + 15, y0 + 20, Palette.Wood);
                    break;
                case TroopType.Archer:
                    p.Rect(cx - 6, y0, cx + 6, y0 + 18, Palette.Hex("3f6fa8"));
                    p.Rect(cx - 7, y0 + 16, cx + 7, y0 + 30, Palette.Hex("2e8b57"));
                    p.Circle(cx, y0 + 36, 7, Palette.Skin);
                    p.Poly(Palette.Hex("e05fb0"), new Vector2(cx - 7, y0 + 36), new Vector2(cx + 7, y0 + 36), new Vector2(cx, y0 + 50)); // hood
                    p.Line(cx - 12, y0 + 12, cx - 12, y0 + 44, 2, Palette.Wood); // bow
                    p.Line(cx - 12, y0 + 12, cx - 9, y0 + 44, 1, Palette.White);
                    break;
                case TroopType.Giant:
                    p.Rect(cx - 12, y0, cx + 12, y0 + 18, Palette.Hex("8a5a2b"));
                    p.Rect(cx - 15, y0 + 16, cx + 15, y0 + 38, Palette.Skin);
                    p.Circle(cx, y0 + 44, 10, Palette.Skin);
                    p.Rect(cx - 10, y0 + 50, cx + 10, y0 + 55, Palette.Hex("e0a060"));
                    p.Circle(cx - 18, y0 + 24, 6, Palette.Skin); p.Circle(cx + 18, y0 + 24, 6, Palette.Skin);
                    break;
                case TroopType.Goblin:
                    p.Rect(cx - 5, y0, cx + 5, y0 + 14, Palette.Hex("5e3a17"));
                    p.Rect(cx - 6, y0 + 12, cx + 6, y0 + 26, Palette.Gem);
                    p.Circle(cx, y0 + 32, 7, Palette.Gem);
                    p.Poly(Palette.Gem, new Vector2(cx - 7, y0 + 32), new Vector2(cx - 16, y0 + 40), new Vector2(cx - 6, y0 + 37)); // ears
                    p.Poly(Palette.Gem, new Vector2(cx + 7, y0 + 32), new Vector2(cx + 16, y0 + 40), new Vector2(cx + 6, y0 + 37));
                    p.Circle(cx - 3, y0 + 33, 1.5f, Palette.Red); p.Circle(cx + 3, y0 + 33, 1.5f, Palette.Red);
                    p.Rect(cx + 8, y0 + 8, cx + 18, y0 + 20, Palette.Wood); // sack
                    break;
                case TroopType.WallBreaker:
                    p.Rect(cx - 5, y0, cx + 5, y0 + 14, Palette.Hex("5e3a17"));
                    p.Rect(cx - 6, y0 + 12, cx + 6, y0 + 24, Palette.Hex("b0b0b0"));
                    p.Circle(cx, y0 + 30, 7, Palette.White); // skull mask
                    p.Circle(cx - 3, y0 + 31, 2, Palette.Black); p.Circle(cx + 3, y0 + 31, 2, Palette.Black);
                    p.Circle(cx + 14, y0 + 26, 9, Palette.Black); // bomb
                    p.Circle(cx + 17, y0 + 36, 2.5f, Palette.Orange);
                    break;
            }
            return p.ToSprite(Ppu, new Vector2(0.5f, 0.1f));
        }

        static Sprite MakeProjectile(ProjectileKind k)
        {
            var p = new Painter(16, 16);
            switch (k)
            {
                case ProjectileKind.Cannonball: p.Circle(8, 8, 5, Palette.Black); p.Circle(6, 10, 1.5f, Palette.GreyDark); break;
                case ProjectileKind.Arrow: p.Line(2, 8, 14, 8, 2, Palette.Wood); p.Poly(Palette.Stone, new Vector2(10, 5), new Vector2(16, 8), new Vector2(10, 11)); break;
                case ProjectileKind.Shell: p.Circle(8, 8, 6, Palette.Hex("3a3a3a")); p.Circle(8, 8, 3, Palette.Orange); break;
            }
            return p.ToSprite(Ppu, new Vector2(0.5f, 0.5f));
        }

        // ---- obstacles --------------------------------------------------------------
        static Sprite MakeObstacle(ObstacleKind kind)
        {
            int s = GameData.ObstacleSize(kind);
            int w = s * CellW + 8, h = s * CellH + 80;
            var p = new Painter(w, h);
            float cx = w / 2f, hh = s * CellH / 2f, hw = s * CellW / 2f, baseY = hh + 4;
            p.Shadow(cx, baseY, hw * 0.8f, hh * 0.8f);
            switch (kind)
            {
                case ObstacleKind.Tree:
                    p.Rect(cx - 6, baseY, cx + 6, baseY + 26, Palette.WoodDark);
                    p.Circle(cx - 14, baseY + 34, 18, Palette.Hex("3f8a2e"));
                    p.Circle(cx + 14, baseY + 36, 18, Palette.Hex("4a9a36"));
                    p.Circle(cx, baseY + 50, 20, Palette.Hex("5aad3e"));
                    p.Circle(cx - 6, baseY + 56, 8, Palette.Hex("7cc45a"));
                    break;
                case ObstacleKind.Bush:
                    p.Ellipse(cx, baseY + 8, 22, 14, Palette.Hex("3f8a2e"));
                    p.Ellipse(cx - 6, baseY + 14, 14, 10, Palette.Hex("5aad3e"));
                    p.Circle(cx + 8, baseY + 12, 3, Palette.Red); p.Circle(cx - 10, baseY + 8, 3, Palette.Red);
                    break;
                case ObstacleKind.Rock:
                    p.Block(cx, baseY, hw * 0.8f, hh * 0.8f, 22, Palette.Hex("b0b0b0"), Palette.Hex("8a8a8a"), Palette.Hex("6a6a6a"));
                    p.Block(cx - 10, baseY + 12, hw * 0.4f, hh * 0.4f, 14, Palette.Hex("c0c0c0"), Palette.Hex("9a9a9a"), Palette.Hex("7a7a7a"));
                    break;
                case ObstacleKind.Stump:
                    p.Block(cx, baseY, 14, 7, 14, Palette.Hex("c9a06a"), Palette.Wood, Palette.WoodDark);
                    p.Ring(cx, baseY + 14, 8, 1.5f, Palette.Wood);
                    break;
            }
            return p.ToSprite(Ppu, new Vector2(0.5f, baseY / h));
        }

        // ---- icons -----------------------------------------------------------------
        static Sprite MakeIcon(string name)
        {
            var p = new Painter(64, 64);
            float c = 32;
            switch (name)
            {
                case "gold":
                    p.Circle(c, c, 26, Palette.GoldDark); p.Circle(c, c, 22, Palette.Gold); p.Ring(c, c, 16, 3, Palette.GoldDark);
                    p.Rect(c - 3, c - 10, c + 3, c + 10, Palette.GoldDark); p.Circle(c - 10, c + 10, 4, Palette.Hex("fff1a8"));
                    break;
                case "elixir":
                    p.Poly(Palette.ElixirDark, new Vector2(c, 60), new Vector2(c - 20, 28), new Vector2(c + 20, 28));
                    p.Circle(c, 26, 22, Palette.ElixirDark);
                    p.Poly(Palette.Elixir, new Vector2(c, 55), new Vector2(c - 16, 28), new Vector2(c + 16, 28));
                    p.Circle(c, 26, 18, Palette.Elixir); p.Circle(c - 7, 20, 5, Palette.Hex("f3a6ef"));
                    break;
                case "gem":
                    p.Poly(Palette.Hex("4f8a2e"), new Vector2(10, 40), new Vector2(22, 56), new Vector2(42, 56), new Vector2(54, 40), new Vector2(32, 8));
                    p.Poly(Palette.Gem, new Vector2(14, 40), new Vector2(24, 52), new Vector2(40, 52), new Vector2(50, 40), new Vector2(32, 13));
                    p.Poly(Palette.Hex("c7f29a"), new Vector2(24, 52), new Vector2(40, 52), new Vector2(44, 42), new Vector2(20, 42));
                    break;
                case "trophy":
                    p.Rect(20, 6, 44, 12, Palette.GoldDark); p.Rect(28, 12, 36, 20, Palette.GoldDark);
                    p.Poly(Palette.Gold, new Vector2(14, 56), new Vector2(50, 56), new Vector2(44, 28), new Vector2(32, 20), new Vector2(20, 28));
                    p.Ring(14, 44, 8, 3, Palette.Gold); p.Ring(50, 44, 8, 3, Palette.Gold);
                    p.Rect(14, 52, 50, 58, Palette.Hex("ffe27a"));
                    break;
                case "shield":
                    p.Poly(Palette.Hex("2f78b3"), new Vector2(10, 54), new Vector2(54, 54), new Vector2(54, 30), new Vector2(32, 6), new Vector2(10, 30));
                    p.Poly(Palette.Blue, new Vector2(15, 50), new Vector2(49, 50), new Vector2(49, 31), new Vector2(32, 12), new Vector2(15, 31));
                    p.Rect(30, 18, 34, 46, Palette.White); p.Rect(20, 34, 44, 38, Palette.White);
                    break;
                case "hammer":
                    p.Line(18, 10, 44, 40, 7, Palette.Wood);
                    p.Poly(Palette.StoneDark, new Vector2(34, 56), new Vector2(58, 34), new Vector2(46, 22), new Vector2(22, 44));
                    p.Poly(Palette.Stone, new Vector2(36, 52), new Vector2(54, 34), new Vector2(46, 26), new Vector2(28, 44));
                    break;
                case "sword":
                    p.Poly(Palette.Stone, new Vector2(26, 26), new Vector2(38, 26), new Vector2(34, 60), new Vector2(30, 60));
                    p.Poly(Palette.White, new Vector2(30, 28), new Vector2(34, 28), new Vector2(32, 58));
                    p.Rect(16, 22, 48, 28, Palette.GoldDark); p.Rect(28, 6, 36, 22, Palette.WoodDark); p.Circle(32, 6, 5, Palette.Gold);
                    break;
                case "gear":
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.PI / 4;
                        p.Line(c, c, c + Mathf.Cos(a) * 26, c + Mathf.Sin(a) * 26, 10, Palette.GreyDark);
                    }
                    p.Circle(c, c, 20, Palette.GreyDark); p.Circle(c, c, 15, Palette.Grey); p.Circle(c, c, 7, Palette.Header);
                    break;
                case "scroll":
                    p.RoundRect(12, 10, 52, 54, 4, Palette.Hex("e8d9b0"));
                    p.Rect(8, 48, 56, 56, Palette.Hex("c9b27a")); p.Rect(8, 8, 56, 16, Palette.Hex("c9b27a"));
                    for (int i = 0; i < 4; i++) p.Rect(18, 22 + i * 7, 46, 24 + i * 7, Palette.Hex("8a7a50"));
                    break;
                case "star":
                    Star(p, c, c, 28, 12, Palette.GoldDark); Star(p, c, c, 24, 10, Palette.Gold);
                    break;
                case "star_empty":
                    Star(p, c, c, 28, 12, Palette.Hex("4a4a4a")); Star(p, c, c, 24, 10, Palette.Hex("6a6a6a"));
                    break;
                case "builder":
                    p.Circle(c, 28, 18, Palette.Skin);
                    p.Ellipse(c, 42, 20, 12, Palette.Gold); p.Rect(10, 40, 54, 46, Palette.GoldDark);
                    p.Circle(25, 28, 2.5f, Palette.Black); p.Circle(39, 28, 2.5f, Palette.Black);
                    p.Rect(20, 16, 44, 20, Palette.Hex("6b4b2b"));
                    break;
                case "mail":
                    p.RoundRect(6, 14, 58, 50, 4, Palette.Hex("f4f4f4"));
                    p.Poly(Palette.Hex("d0d0d0"), new Vector2(6, 50), new Vector2(58, 50), new Vector2(32, 26));
                    p.Line(6, 50, 32, 26, 2, Palette.Grey); p.Line(58, 50, 32, 26, 2, Palette.Grey);
                    break;
                case "chat":
                    p.Ellipse(c, 36, 26, 18, Palette.White);
                    p.Poly(Palette.White, new Vector2(16, 24), new Vector2(10, 8), new Vector2(30, 20));
                    p.Circle(20, 36, 3, Palette.Grey); p.Circle(32, 36, 3, Palette.Grey); p.Circle(44, 36, 3, Palette.Grey);
                    break;
                case "info":
                    p.Circle(c, c, 28, Palette.BlueDark); p.Circle(c, c, 24, Palette.Blue);
                    p.Rect(28, 14, 36, 36, Palette.White); p.Circle(32, 44, 5, Palette.White);
                    break;
                case "close":
                    p.Circle(c, c, 28, Palette.Hex("a52a1a")); p.Circle(c, c, 24, Palette.Red);
                    p.Line(20, 20, 44, 44, 7, Palette.White); p.Line(44, 20, 20, 44, 7, Palette.White);
                    break;
                case "check":
                    p.Circle(c, c, 28, Palette.GreenDark); p.Circle(c, c, 24, Palette.Green);
                    p.Line(16, 32, 28, 20, 7, Palette.White); p.Line(28, 20, 50, 44, 7, Palette.White);
                    break;
                case "plus":
                    p.Circle(c, c, 28, Palette.GreenDark); p.Circle(c, c, 24, Palette.Green);
                    p.Rect(28, 14, 36, 50, Palette.White); p.Rect(14, 28, 50, 36, Palette.White);
                    break;
                case "arrow":
                    p.Poly(Palette.Green, new Vector2(8, 24), new Vector2(36, 24), new Vector2(36, 12), new Vector2(60, 32), new Vector2(36, 52), new Vector2(36, 40), new Vector2(8, 40));
                    break;
                case "castle":
                    p.Rect(10, 8, 54, 40, Palette.Stone);
                    for (int i = 0; i < 4; i++) p.Rect(10 + i * 12, 40, 18 + i * 12, 50, Palette.Stone);
                    p.Rect(26, 8, 38, 26, Palette.WoodDark); p.Circle(32, 26, 6, Palette.WoodDark);
                    p.Rect(10, 8, 54, 12, Palette.StoneDark);
                    break;
                case "lock":
                    p.RoundRect(14, 6, 50, 34, 4, Palette.GreyDark); p.Ring(32, 38, 12, 5, Palette.GreyDark); p.Circle(32, 20, 4, Palette.Header);
                    break;
                case "clock":
                    p.Circle(c, c, 26, Palette.GreyDark); p.Circle(c, c, 22, Palette.White);
                    p.Line(c, c, c, c + 14, 3, Palette.Header); p.Line(c, c, c + 10, c, 3, Palette.Header);
                    break;
                case "xp":
                    p.Circle(c, c, 28, Palette.Hex("1f5f9f")); p.Circle(c, c, 24, Palette.Hex("3a86d0"));
                    Star(p, c, c, 16, 7, Palette.White);
                    break;
                case "skull":
                    p.Circle(c, 36, 20, Palette.White); p.Rect(22, 10, 42, 24, Palette.White);
                    p.Circle(24, 36, 5, Palette.Black); p.Circle(40, 36, 5, Palette.Black); p.Rect(30, 24, 34, 30, Palette.Black);
                    break;
                case "goblin":
                    p.Circle(c, 30, 18, Palette.Gem);
                    p.Poly(Palette.Gem, new Vector2(16, 34), new Vector2(2, 50), new Vector2(20, 44));
                    p.Poly(Palette.Gem, new Vector2(48, 34), new Vector2(62, 50), new Vector2(44, 44));
                    p.Circle(25, 32, 3.5f, Palette.Red); p.Circle(39, 32, 3.5f, Palette.Red);
                    p.Poly(Palette.Black, new Vector2(22, 22), new Vector2(42, 22), new Vector2(32, 16));
                    break;
                case "badge":
                    p.Poly(Palette.Hex("6a3a9a"), new Vector2(12, 56), new Vector2(52, 56), new Vector2(52, 26), new Vector2(32, 6), new Vector2(12, 26));
                    p.Poly(Palette.Hex("9a5ad0"), new Vector2(16, 52), new Vector2(48, 52), new Vector2(48, 28), new Vector2(32, 12), new Vector2(16, 28));
                    Star(p, 32, 34, 10, 4, Palette.Gold);
                    break;
                case "flag":
                    p.Rect(14, 6, 18, 58, Palette.WoodDark);
                    p.Poly(Palette.Red, new Vector2(18, 58), new Vector2(54, 48), new Vector2(18, 38));
                    break;
                case "fist":
                    p.RoundRect(14, 14, 50, 44, 8, Palette.Skin); p.Rect(14, 36, 50, 40, Palette.Hex("d9a96a"));
                    p.Rect(10, 24, 18, 40, Palette.Skin);
                    break;
                case "cup":
                    p.Rect(22, 8, 42, 14, Palette.GoldDark);
                    p.Poly(Palette.Gold, new Vector2(16, 56), new Vector2(48, 56), new Vector2(42, 20), new Vector2(22, 20));
                    break;
                case "music":
                    p.Rect(26, 16, 31, 52, Palette.White); p.Rect(46, 24, 51, 56, Palette.White);
                    p.Ellipse(22, 16, 9, 7, Palette.White); p.Ellipse(42, 24, 9, 7, Palette.White); p.Rect(26, 48, 51, 56, Palette.White);
                    break;
                case "sound":
                    p.Poly(Palette.White, new Vector2(10, 26), new Vector2(22, 26), new Vector2(36, 12), new Vector2(36, 52), new Vector2(22, 38), new Vector2(10, 38));
                    p.Ring(36, 32, 14, 3, Palette.White); p.Ring(36, 32, 22, 3, Palette.White);
                    break;
                case "globe":
                    p.Circle(c, c, 26, Palette.BlueDark); p.Circle(c, c, 22, Palette.Blue);
                    p.Ellipse(c, c, 10, 22, Palette.Hex("8fd0f5")); p.Rect(10, 30, 54, 34, Palette.Hex("8fd0f5"));
                    break;
                case "search":
                    p.Ring(38, 38, 16, 5, Palette.White); p.Line(26, 26, 10, 10, 7, Palette.White);
                    break;
                case "bookmark":
                    p.Poly(Palette.White, new Vector2(18, 58), new Vector2(46, 58), new Vector2(46, 8), new Vector2(32, 22), new Vector2(18, 8));
                    break;
                case "ring":
                    p.Ring(c, c, 30, 4, Palette.White);
                    break;
                case "dot":
                    p.Circle(c, c, 30, Palette.White);
                    break;
                case "coin_small":
                    p.Circle(c, c, 14, Palette.GoldDark); p.Circle(c, c, 11, Palette.Gold);
                    break;
                case "drop_small":
                    p.Circle(c, 26, 11, Palette.Elixir); p.Poly(Palette.Elixir, new Vector2(c, 44), new Vector2(c - 10, 28), new Vector2(c + 10, 28));
                    break;
                case "shop":
                    p.Rect(12, 8, 52, 36, Palette.Hex("c9a06a"));
                    p.Rect(26, 8, 38, 28, Palette.WoodDark);
                    for (int i = 0; i < 5; i++) p.Poly(i % 2 == 0 ? Palette.Red : Palette.White, new Vector2(8 + i * 10, 34), new Vector2(18 + i * 10, 34), new Vector2(18 + i * 10, 48), new Vector2(8 + i * 10, 48));
                    p.Poly(Palette.Hex("8e3b2f"), new Vector2(6, 48), new Vector2(58, 48), new Vector2(32, 60));
                    break;
                case "heart":
                    p.Circle(22, 38, 12, Palette.Red); p.Circle(42, 38, 12, Palette.Red);
                    p.Poly(Palette.Red, new Vector2(10, 34), new Vector2(54, 34), new Vector2(32, 8));
                    break;
                case "crown":
                    p.Poly(Palette.Gold, new Vector2(10, 14), new Vector2(54, 14), new Vector2(56, 44), new Vector2(44, 30), new Vector2(32, 52), new Vector2(20, 30), new Vector2(8, 44));
                    p.Rect(10, 14, 54, 20, Palette.GoldDark);
                    break;
                default:
                    p.RoundRect(8, 8, 56, 56, 8, Palette.Grey);
                    break;
            }
            return p.ToSprite(1, new Vector2(0.5f, 0.5f));
        }

        static void Star(Painter p, float cx, float cy, float r, float r2, Color c)
        {
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2 + i * Mathf.PI / 5;
                float rr = i % 2 == 0 ? r : r2;
                pts[i] = new Vector2(cx + Mathf.Cos(a) * rr, cy + Mathf.Sin(a) * rr);
            }
            p.Poly(c, pts);
        }
    }
}
