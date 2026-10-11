using UnityEngine;
using UnityEngine.UI;
using VillageClash.Core;

namespace VillageClash.UI
{
    public static class SettingsPanel
    {
        public static void Show()
        {
            var root = GameRoot.I;
            var c = root.Controller;
            var m = UIKit.ShowModal("Settings", new Vector2(1100, 860));
            var body = m.Content;

            // Village ID banner (mock of an account service)
            var banner = UIKit.Node("banner", body);
            UIKit.Place(banner, UIKit.TC, new Vector2(0, -10), new Vector2(1040, 120));
            UIKit.Panel(banner, Palette.BlueDark, true, false);
            var ic = UIKit.Icon(banner, "globe", 80);
            UIKit.Place((RectTransform)ic.transform, UIKit.ML, new Vector2(24, 0), new Vector2(80, 80));
            var t = UIKit.Label(banner, "Village ID\nSave your village across devices", 26, Color.white, TextAnchor.MiddleLeft);
            UIKit.Place((RectTransform)t.transform, UIKit.ML, new Vector2(124, 0), new Vector2(600, 110));
            var dis = UIKit.Btn(banner, "Disconnected", Palette.Red, new Vector2(240, 60), () => UIKit.Toast("Online accounts are not available in this offline edition."), 24);
            UIKit.Place(dis.Rt, UIKit.MR, new Vector2(-24, 0), new Vector2(240, 60));

            // toggles
            float y = -150;
            Toggle(body, "music", "Music", y, c.State.MusicOn, on => { c.SetAudio(on, c.State.SfxOn); Sfx.SetMusic(on); });
            Toggle(body, "sound", "Sound Effects", y - 76, c.State.SfxOn, on => { c.SetAudio(c.State.MusicOn, on); Sfx.SetSfx(on); });
            var langL = UIKit.Dark(body, "Language", 28, TextAnchor.MiddleLeft);
            UIKit.Place((RectTransform)langL.transform, UIKit.TL, new Vector2(100, y - 152), new Vector2(400, 60));
            var lang = UIKit.Btn(body, "English", Palette.GreyDark, new Vector2(240, 60), () => UIKit.Toast("English is the only language available."), 24);
            UIKit.Place(lang.Rt, UIKit.TR, new Vector2(-40, y - 152), new Vector2(240, 60));
            var langIc = UIKit.Icon(body, "globe", 44);
            UIKit.Place((RectTransform)langIc.transform, UIKit.TL, new Vector2(40, y - 160), new Vector2(44, 44));

            // mock sign-ins
            var fb = UIKit.Dark(body, "Facebook", 28, TextAnchor.MiddleLeft);
            UIKit.Place((RectTransform)fb.transform, UIKit.TL, new Vector2(100, y - 236), new Vector2(400, 60));
            var fbb = UIKit.Btn(body, "Disconnected", Palette.Red, new Vector2(240, 60), () => UIKit.Toast("Facebook sign-in is not available offline."), 24);
            UIKit.Place(fbb.Rt, UIKit.TR, new Vector2(-40, y - 236), new Vector2(240, 60));
            var gp = UIKit.Dark(body, "Game Center", 28, TextAnchor.MiddleLeft);
            UIKit.Place((RectTransform)gp.transform, UIKit.TL, new Vector2(100, y - 312), new Vector2(400, 60));
            var gpb = UIKit.Btn(body, "Disconnected", Palette.Red, new Vector2(240, 60), () => UIKit.Toast("Game Center sign-in is not available offline."), 24);
            UIKit.Place(gpb.Rt, UIKit.TR, new Vector2(-40, y - 312), new Vector2(240, 60));
            var fbIc = UIKit.Icon(body, "chat", 44); UIKit.Place((RectTransform)fbIc.transform, UIKit.TL, new Vector2(40, y - 244), new Vector2(44, 44));
            var gpIc = UIKit.Icon(body, "fist", 44); UIKit.Place((RectTransform)gpIc.transform, UIKit.TL, new Vector2(40, y - 320), new Vector2(44, 44));

            // bottom grid
            var grid = UIKit.Node("grid", body);
            UIKit.Place(grid, UIKit.BC, new Vector2(0, 16), new Vector2(1040, 170));
            UIKit.Grid(grid, new Vector2(330, 72), 12, 4);
            GridBtn(grid, "Privacy", "info", () => UIKit.Toast("Village Clash stores your save file locally and collects no data."));
            GridBtn(grid, "Terms", "scroll", () => UIKit.Toast("Fan-made, non-commercial learning project. Not affiliated with Supercell."));
            GridBtn(grid, "Parent's Guide", "heart", () => UIKit.Toast("No purchases, no chat, no online play. Safe for all ages."));
            GridBtn(grid, "Help", "info", () => UIKit.Toast("Tap buildings to manage them. Hold to move. Attack from the bottom-left."));
            GridBtn(grid, "Credits", "crown", () => UIKit.Toast("Village Clash - built with Unity. Art, audio and code generated procedurally."));
            GridBtn(grid, "More Settings", "gear", () => MoreSettings(root));
        }

        static void Toggle(RectTransform body, string icon, string label, float y, bool on, System.Action<bool> set)
        {
            var ic = UIKit.Icon(body, icon, 44);
            UIKit.Place((RectTransform)ic.transform, UIKit.TL, new Vector2(40, y - 8), new Vector2(44, 44));
            var l = UIKit.Dark(body, label, 28, TextAnchor.MiddleLeft);
            UIKit.Place((RectTransform)l.transform, UIKit.TL, new Vector2(100, y), new Vector2(400, 60));
            BtnView b = null;
            b = UIKit.Btn(body, on ? "On" : "Off", on ? Palette.Green : Palette.GreyDark, new Vector2(240, 60), () =>
            {
                on = !on; set(on);
                b.SetText(on ? "On" : "Off"); b.SetColor(on ? Palette.Green : Palette.GreyDark);
            }, 26);
            UIKit.Place(b.Rt, UIKit.TR, new Vector2(-40, y), new Vector2(240, 60));
        }

        static void GridBtn(RectTransform grid, string text, string icon, System.Action onClick)
        {
            var b = UIKit.Btn(grid, text, Palette.Blue, new Vector2(330, 72), onClick, 24, icon);
        }

        static void MoreSettings(GameRoot root)
        {
            var c = root.Controller;
            var m = UIKit.ShowModal("More Settings", new Vector2(760, 420));
            var l = UIKit.Dark(m.Content, $"Save file:\n{root.SavePath}\n\nTown Hall {c.State.TownHallLevel}  -  Level {c.State.Level}  -  {c.State.Village.Buildings.Count} buildings", 22, TextAnchor.UpperLeft, false);
            UIKit.Place((RectTransform)l.transform, UIKit.TC, new Vector2(0, -10), new Vector2(700, 160));
            var save = UIKit.Btn(m.Content, "Save Now", Palette.Green, new Vector2(300, 70), () =>
            {
                UIKit.Toast(c.Save() ? "Village saved." : "Village could not be saved. Check disk space and file access.");
            }, 26);
            UIKit.Place(save.Rt, UIKit.BC, new Vector2(-170, 20), new Vector2(300, 70));
            var reset = UIKit.Btn(m.Content, "Reset Village", Palette.Red, new Vector2(300, 70), () =>
            {
                ConfirmDialog.Show("Reset Village?", "This deletes your village, army and progress and starts a brand new game. This cannot be undone!", "Reset", () =>
                {
                    c.Reset();
                    UIKit.CloseAllModals();
                    root.ShowHomeVillage();
                    UIKit.Toast("Welcome back, Chief! A fresh village awaits.");
                }, "Cancel", Palette.Red);
            }, 26);
            UIKit.Place(reset.Rt, UIKit.BC, new Vector2(170, 20), new Vector2(300, 70));
        }
    }
}
