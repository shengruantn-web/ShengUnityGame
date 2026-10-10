using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VillageClash.Core;

namespace VillageClash.UI
{
    public static class ClanPanel
    {
        sealed class Clan { public string Name; public int Level; public string Join; public int Trophies; public int Members; public int Required; }

        static readonly Clan[] Clans =
        {
            new Clan { Name = "Dragon Lords", Level = 14, Join = "Invite only", Trophies = 42110, Members = 49, Required = 2600 },
            new Clan { Name = "Nordic Wolves", Level = 11, Join = "Anyone can join", Trophies = 31800, Members = 44, Required = 1200 },
            new Clan { Name = "Elixir Kings", Level = 9, Join = "Anyone can join", Trophies = 25400, Members = 37, Required = 800 },
            new Clan { Name = "Iron Gate", Level = 12, Join = "Closed", Trophies = 36950, Members = 50, Required = 2000 },
            new Clan { Name = "Night Raiders", Level = 7, Join = "Anyone can join", Trophies = 18200, Members = 29, Required = 400 },
            new Clan { Name = "Phoenix Rising", Level = 10, Join = "Invite only", Trophies = 28800, Members = 41, Required = 1400 },
            new Clan { Name = "Stone Hearts", Level = 5, Join = "Anyone can join", Trophies = 12050, Members = 22, Required = 0 },
            new Clan { Name = "Goblin Gang", Level = 3, Join = "Anyone can join", Trophies = 6400, Members = 15, Required = 0 },
            new Clan { Name = "Royal Guard", Level = 13, Join = "Invite only", Trophies = 39900, Members = 48, Required = 2400 },
            new Clan { Name = "Wild Hogs", Level = 6, Join = "Anyone can join", Trophies = 14300, Members = 26, Required = 200 },
            new Clan { Name = "Barbarian Brothers", Level = 4, Join = "Anyone can join", Trophies = 9800, Members = 19, Required = 0 },
            new Clan { Name = "Archer Academy", Level = 8, Join = "Anyone can join", Trophies = 21600, Members = 33, Required = 600 },
        };

        public static void Show(int tab = 1)
        {
            var c = GameRoot.I.Controller;
            var m = UIKit.ShowModal("Clans", new Vector2(1300, 840));
            var tabs = UIKit.Tabs(m.Content, new[] { "My Profile", "Clans", "Social" }, new Vector2(260, 60), null);
            UIKit.Place((RectTransform)tabs.Buttons[0].Rt.parent, UIKit.TC, new Vector2(0, -8), new Vector2(820, 60));
            var body = UIKit.Node("body", m.Content);
            UIKit.Stretch(body, 20, 84, 20, 20);
            tabs.OnSelect = i =>
            {
                foreach (Transform t in body) Object.Destroy(t.gameObject);
                if (i == 0) Profile(body, c); else if (i == 1) Search(body, c); else Social(body);
            };
            tabs.Select(tab);
        }

        static void Profile(RectTransform body, GameController c)
        {
            var s = c.State;
            var card = UIKit.Node("card", body);
            UIKit.Place(card, UIKit.TC, new Vector2(0, 0), new Vector2(1240, 160));
            UIKit.Panel(card, Palette.CreamDark, true, false);
            var ic = UIKit.Icon(card, "xp", 100);
            UIKit.Place((RectTransform)ic.transform, UIKit.ML, new Vector2(30, 0), new Vector2(100, 100));
            var lv = UIKit.Label(card, s.Level.ToString(), 36, Color.white);
            UIKit.Place((RectTransform)lv.transform, UIKit.ML, new Vector2(30, 0), new Vector2(100, 100));
            var name = UIKit.Dark(card, s.PlayerName, 34, TextAnchor.MiddleLeft);
            UIKit.Place((RectTransform)name.transform, UIKit.ML, new Vector2(150, 30), new Vector2(600, 50));
            var league = Leagues.ForTrophies(s.Trophies);
            var sub = UIKit.Dark(card, $"{league.Name} League  -  {s.Trophies} trophies  -  Town Hall {s.TownHallLevel}  -  No clan", 24, TextAnchor.MiddleLeft, false);
            UIKit.Place((RectTransform)sub.transform, UIKit.ML, new Vector2(150, -25), new Vector2(900, 40));

            var rn = UIKit.Dark(body, "Change name:", 24, TextAnchor.MiddleLeft, false);
            UIKit.Place((RectTransform)rn.transform, UIKit.TL, new Vector2(10, -180), new Vector2(240, 50));
            InputField field = null;
            field = UIInput.Field(body, "Your name", new Vector2(420, 54), null, null, s.PlayerName);
            UIKit.Place((RectTransform)field.transform, UIKit.TL, new Vector2(230, -180), new Vector2(420, 54));
            var ok = UIKit.Btn(body, "Rename", Palette.Green, new Vector2(180, 54), () => { if (c.SetName(field.text)) UIKit.Toast($"Hello, {c.State.PlayerName}!"); }, 24);
            UIKit.Place(ok.Rt, UIKit.TL, new Vector2(670, -180), new Vector2(180, 54));

            var grid = UIKit.Node("stats", body);
            UIKit.Place(grid, UIKit.TC, new Vector2(0, -250), new Vector2(1240, 420));
            UIKit.Grid(grid, new Vector2(400, 90), 14, 6);
            Stat(grid, "trophy", "Trophies", s.Trophies.ToString());
            Stat(grid, "sword", "Battles played", s.BattlesPlayed.ToString());
            Stat(grid, "star", "Battles won", s.BattlesWon.ToString());
            Stat(grid, "star", "Total stars", s.TotalStars.ToString());
            int goldLost = 0, elixirLost = 0;
            foreach (var r in s.DefenseLog) { goldLost += r.GoldLost; elixirLost += r.ElixirLost; }
            Stat(grid, "gold", "Gold lost in defense", Fmt.Num(goldLost));
            Stat(grid, "elixir", "Elixir lost in defense", Fmt.Num(elixirLost));
            Stat(grid, "castle", "Buildings", s.Village.Buildings.Count.ToString());
            Stat(grid, "builder", "Builders", GameData.MaxBuilders >= 2 && s.Village.Count(BuildingType.BuilderHut) >= 2 ? "2" : "1");
            Stat(grid, "shield", "Shield", s.ShieldActive(c.Now) ? Fmt.Time((int)(s.ShieldUntilUnix - c.Now)) : "None");
        }

        static void Stat(RectTransform grid, string icon, string label, string value)
        {
            var row = UIKit.Node("stat", grid);
            UIKit.Panel(row, Palette.CreamDark, true, false);
            var ic = UIKit.Icon(row, icon, 44);
            UIKit.Place((RectTransform)ic.transform, UIKit.ML, new Vector2(14, 0), new Vector2(44, 44));
            var l = UIKit.Dark(row, label, 22, TextAnchor.MiddleLeft, false);
            UIKit.Place((RectTransform)l.transform, UIKit.ML, new Vector2(70, 0), new Vector2(230, 80));
            var v = UIKit.Dark(row, value, 26, TextAnchor.MiddleRight);
            UIKit.Place((RectTransform)v.transform, UIKit.MR, new Vector2(-14, 0), new Vector2(120, 80));
        }

        static void Search(RectTransform body, GameController c)
        {
            var top = UIKit.Node("top", body);
            UIKit.Place(top, UIKit.TC, new Vector2(0, 0), new Vector2(1240, 70));
            var sc = UIKit.Btn(top, "Search Clans", Palette.Blue, new Vector2(260, 60), () => Sfx.Play(SfxId.Tap), 24, "search");
            UIKit.Place(sc.Rt, UIKit.ML, new Vector2(0, 0), new Vector2(260, 60));
            var bm = UIKit.Btn(top, "Bookmarks", Palette.GreyDark, new Vector2(220, 60), () => UIKit.Toast("You have no bookmarked clans."), 24, "bookmark");
            UIKit.Place(bm.Rt, UIKit.ML, new Vector2(275, 0), new Vector2(220, 60));

            var listHolder = UIKit.Node("listHolder", body);
            UIKit.Stretch(listHolder, 0, 160, 0, 0);
            var list = UIKit.Scroll(listHolder, out _);
            UIKit.VList(list, 8, 4);
            UIKit.FitContent(list);

            string query = "";
            System.Action rebuild = () =>
            {
                foreach (Transform t in list) Object.Destroy(t.gameObject);
                int n = 0;
                foreach (var cl in Clans)
                {
                    if (query.Length > 0 && cl.Name.ToLowerInvariant().IndexOf(query, System.StringComparison.Ordinal) < 0) continue;
                    ClanRow(list, cl, c);
                    n++;
                }
                if (n == 0)
                {
                    var none = UIKit.Dark(list, "No clans found. Try another name.", 26, TextAnchor.MiddleCenter, false);
                    UIKit.Size((RectTransform)none.transform, 1200, 80);
                }
            };

            var field = UIInput.Field(body, "Clan name", new Vector2(620, 56), v => { query = v.Trim().ToLowerInvariant(); rebuild(); });
            UIKit.Place((RectTransform)field.transform, UIKit.TL, new Vector2(0, -82), new Vector2(620, 56));
            var go = UIKit.Btn(body, "Search", Palette.Green, new Vector2(200, 56), () => { query = field.text.Trim().ToLowerInvariant(); rebuild(); Sfx.Play(SfxId.Tap); }, 24, "search");
            UIKit.Place(go.Rt, UIKit.TL, new Vector2(640, -82), new Vector2(200, 56));
            var adv = UIKit.Btn(body, "Advanced Options", Palette.GreyDark, new Vector2(300, 56), () => UIKit.Toast("Advanced search filters are not available offline."), 22);
            UIKit.Place(adv.Rt, UIKit.TL, new Vector2(860, -82), new Vector2(300, 56));
            rebuild();
        }

        static void ClanRow(RectTransform list, Clan cl, GameController c)
        {
            var row = UIKit.Node("clan", list);
            UIKit.Size(row, 1200, 96);
            UIKit.Panel(row, Palette.CreamDark, true, false);
            var badge = UIKit.Icon(row, "badge", 72);
            badge.color = Color.HSVToRGB((cl.Name.GetHashCode() & 0xffff) / 65535f, 0.55f, 0.9f);
            UIKit.Place((RectTransform)badge.transform, UIKit.ML, new Vector2(14, 0), new Vector2(72, 72));
            var lvl = UIKit.Label(row, cl.Level.ToString(), 24, Color.white);
            UIKit.Place((RectTransform)lvl.transform, UIKit.ML, new Vector2(14, -2), new Vector2(72, 72));
            var n = UIKit.Dark(row, cl.Name, 28, TextAnchor.MiddleLeft);
            UIKit.Place((RectTransform)n.transform, UIKit.ML, new Vector2(104, 22), new Vector2(500, 40));
            var j = UIKit.Dark(row, $"{cl.Join}   -   Required trophies: {cl.Required}", 20, TextAnchor.MiddleLeft, false);
            UIKit.Place((RectTransform)j.transform, UIKit.ML, new Vector2(104, -18), new Vector2(560, 34));
            var tr = UIKit.Icon(row, "trophy", 34);
            UIKit.Place((RectTransform)tr.transform, UIKit.ML, new Vector2(680, 0), new Vector2(34, 34));
            var tt = UIKit.Dark(row, Fmt.Num(cl.Trophies), 24, TextAnchor.MiddleLeft);
            UIKit.Place((RectTransform)tt.transform, UIKit.ML, new Vector2(720, 0), new Vector2(140, 40));
            var mem = UIKit.Dark(row, $"{cl.Members}/50", 24, TextAnchor.MiddleLeft);
            UIKit.Place((RectTransform)mem.transform, UIKit.ML, new Vector2(880, 0), new Vector2(100, 40));
            string[] chips = { "flag", "sword", "globe" };
            for (int i = 0; i < 3; i++)
            {
                var ch = UIKit.Icon(row, chips[i], 30);
                UIKit.Place((RectTransform)ch.transform, UIKit.MR, new Vector2(-190 + i * 36, 0), new Vector2(30, 30));
            }
            bool canJoin = cl.Join == "Anyone can join" && c.State.Trophies >= cl.Required;
            var btn = UIKit.Btn(row, cl.Join == "Closed" ? "Closed" : cl.Join == "Invite only" ? "Request" : "Join", canJoin ? Palette.Green : Palette.GreyDark, new Vector2(130, 56),
                () => UIKit.Toast(canJoin ? "Clans are offline in this edition. Your village stands alone, Chief!" : cl.Join == "Closed" ? "This clan is closed." : cl.Join == "Invite only" ? "Join request sent (mock)." : $"You need {cl.Required} trophies to join."), 22);
            UIKit.Place(btn.Rt, UIKit.MR, new Vector2(-12, 0), new Vector2(130, 56));
        }

        static void Social(RectTransform body)
        {
            var l = UIKit.Dark(body, "Connect with friends to see their villages and compare progress.\n\nThis offline edition has no social features.\nRaid the practice bases and climb the leagues instead!", 26, TextAnchor.MiddleCenter, false);
            UIKit.Stretch((RectTransform)l.transform, 60, 40, 60, 120);
            var b = UIKit.Btn(body, "Invite Friends", Palette.Blue, new Vector2(300, 70), () => UIKit.Toast("Invites are not available offline."), 26, "chat");
            UIKit.Place(b.Rt, UIKit.BC, new Vector2(0, 20), new Vector2(300, 70));
        }
    }
}
