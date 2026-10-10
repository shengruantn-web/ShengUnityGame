using UnityEngine;

namespace VillageClash
{
    public static class Palette
    {
        public static readonly Color Grass1 = Hex("7cbf3f");
        public static readonly Color Grass2 = Hex("6aa832");
        public static readonly Color GrassDark = Hex("4f8a28");
        public static readonly Color GrassEdge = Hex("3f6f20");
        public static readonly Color Cream = Hex("efe9dc");
        public static readonly Color CreamDark = Hex("d9d0bd");
        public static readonly Color Border = Hex("3a2a1a");
        public static readonly Color Header = Hex("2b2b2b");
        public static readonly Color Green = Hex("8dd13c");
        public static readonly Color GreenDark = Hex("5e9a22");
        public static readonly Color Red = Hex("e2402f");
        public static readonly Color Blue = Hex("5fb9e6");
        public static readonly Color BlueDark = Hex("2f78b3");
        public static readonly Color Grey = Hex("9a9a9a");
        public static readonly Color GreyDark = Hex("5c5c5c");
        public static readonly Color Gold = Hex("f4c531");
        public static readonly Color GoldDark = Hex("c4931a");
        public static readonly Color Elixir = Hex("d84cd0");
        public static readonly Color ElixirDark = Hex("9a2f93");
        public static readonly Color Gem = Hex("8bd04a");
        public static readonly Color Orange = Hex("f0932b");
        public static readonly Color Wood = Hex("8a5a2b");
        public static readonly Color WoodDark = Hex("5e3a17");
        public static readonly Color Stone = Hex("a7a9ad");
        public static readonly Color StoneDark = Hex("6f7276");
        public static readonly Color Roof = Hex("c0392b");
        public static readonly Color Skin = Hex("f1c27d");
        public static readonly Color White = Color.white;
        public static readonly Color Black = Hex("1a1a1a");
        public static readonly Color Dim = new Color(0, 0, 0, 0.55f);
        public static readonly Color Transparent = new Color(0, 0, 0, 0);

        public static Color Hex(string h)
        {
            if (h.StartsWith("#")) h = h.Substring(1);
            byte r = byte.Parse(h.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(h.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(h.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color32(r, g, b, 255);
        }

        public static Color Shade(Color c, float f) => new Color(Mathf.Clamp01(c.r * f), Mathf.Clamp01(c.g * f), Mathf.Clamp01(c.b * f), c.a);
        public static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
