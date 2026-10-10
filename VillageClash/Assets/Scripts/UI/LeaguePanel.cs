using UnityEngine;
using UnityEngine.UI;
using VillageClash.Core;

namespace VillageClash.UI
{
    public static class LeaguePanel
    {
        static readonly string[] TopPlayers = { "Jorge Yao", "Wolfric", "Ash", "LiLaWe", "NightOwl", "Kaneki", "Panda", "Roxi", "Hakone", "Brutus", "Zephyr", "Sora" };
        static readonly string[] TopClans = { "Dragon Lords", "Nordic Wolves", "Elixir Kings", "Iron Gate", "Night Raiders", "Phoenix Rising", "Stone Hearts", "Goblin Gang", "Royal Guard", "Wild Hogs" };

        public static void Show(int tab = 0)
        {
            var c = GameRoot.I.Controller;
            var m = UIKit.ShowModal("Leagues", new Vector2(1300, 820));
            var tabs = UIKit.Tabs(m.Content, new[] { "My League", "Top Clans", "Top Players", "Clan War League" }, new Vector2(260, 60), null);
            UIKit.Place((RectTransform)tabs.Buttons[0].Rt.parent, UIKit.TC, new Vector2(0, -8), new Vector2(1100, 60));
            var body = UIKit.Node("body", m.Content);
            UIKit.Stretch(body, 20, 84, 20, 20);
            tabs.OnSelect = i => { foreach (Transform t in body) Object.Destroy(t.gameObject); Build(i, body, c); };
            tabs.Select(tab);
        }

        static void Build(int tab, RectTransform body, GameController c)
        {
            var s = c.State;
            switch (tab)
            {
                case 0: MyLeague(body, s); break;
                case 1: Ranking(body, TopClans, i => $"{(TopClans.Length - i) * 1700 + 9000} trophies  -  {38 + (i * 7) % 12}/50 members", null, "badge"); break;
                case 2:
                {
                    int rank = 0; int myT = s.Trophies;
                    var names = new System.Collections.Generic.List<string>(TopPlayers);
                    var scores = new System.Collections.Generic.List<int>();
                    for (int i = 0; i < names.Count; i++) scores.Add(5200 - i * 310);
                    for (rank = 0; rank < scores.Count && scores[rank] > myT; rank++) { }
                    names.Insert(rank, s.PlayerName + "  (you)"); scores.Insert(rank, myT);
                    Ranking(body, names.ToArray(), i => $"{scores[i]} trophies  -  {Leagues.ForTrophies(scores[i]).Name} League", rank, "trophy");
                    break;
                }
                default:
                {
                    var l = UIKit.Dark(body, "Clan War Leagues run once a month between clans of similar strength.\n\nJoin a clan with at least 15 members to take part.\n(Clan Wars are not available in this offline edition.)", 26, TextAnchor.MiddleCenter, false);
                    UIKit.Stretch((RectTransform)l.transform, 60, 40, 60, 40);
                    break;
                }
            }
        }

        static void MyLeague(RectTransform body, GameState s)
        {
            var league = Leagues.ForTrophies(s.Trophies);
            var banner = UIKit.Node("banner", body);
            UIKit.Place(banner, UIKit.TC, new Vector2(0, 0), new Vector2(1240, 120));
            UIKit.Panel(banner, Palette.BlueDark, true, false);
            var cup = UIKit.Icon(banner, "cup", 90);
            UIKit.Place((RectTransform)cup.transform, UIKit.ML, new Vector2(30, 0), new Vector2(90, 90));
            string txt = league.MinTrophies == 0
                ? $"Collect {Leagues.All[1].MinTrophies} trophies to join a League!\nYou have {s.Trophies}."
                : $"{league.Name} League  -  {s.Trophies} trophies\nLeague bonus per victory: {Fmt.Num(league.BonusGold)} gold, {Fmt.Num(league.BonusElixir)} elixir";
            var t = UIKit.Label(banner, txt, 28, Color.white, TextAnchor.MiddleLeft);
            UIKit.Place((RectTransform)t.transform, UIKit.ML, new Vector2(140, 0), new Vector2(900, 110));
            var next = Leagues.Next(league);
            if (next != null)
            {
                var bar = UIKit.Bar(banner, new Vector2(300, 32), Palette.Gold, Palette.Black, 20);
                UIKit.Place(bar.Rt, UIKit.MR, new Vector2(-30, 0), new Vector2(300, 32));
                float frac = Mathf.InverseLerp(league.MinTrophies, next.MinTrophies, s.Trophies);
                bar.Set(frac, $"{s.Trophies} / {next.MinTrophies}");
            }

            var list = UIKit.Scroll(body, out _);
            UIKit.Stretch((RectTransform)list.parent, 0, 136, 0, 0);
            UIKit.VList(list, 10, 4);
            UIKit.FitContent(list);
            for (int i = Leagues.All.Length - 1; i >= 1; i--)
            {
                var l = Leagues.All[i];
                var row = UIKit.Node("row", list);
                UIKit.Size(row, 1200, 90);
                UIKit.Panel(row, l == league ? Palette.Hex("f5e3a0") : Palette.CreamDark, true, false);
                var ic = UIKit.Icon(row, "trophy", 60);
                ic.color = TierColor(i);
                UIKit.Place((RectTransform)ic.transform, UIKit.ML, new Vector2(20, 0), new Vector2(60, 60));
                var n = UIKit.Dark(row, $"{l.Name} League   {l.MinTrophies}+", 28, TextAnchor.MiddleLeft);
                UIKit.Place((RectTransform)n.transform, UIKit.ML, new Vector2(100, 0), new Vector2(460, 80));
                var d = UIKit.Dark(row, "Divisions:   III     II     I", 22, TextAnchor.MiddleLeft, false);
                UIKit.Place((RectTransform)d.transform, UIKit.ML, new Vector2(560, 0), new Vector2(320, 80));
                var b = UIKit.Dark(row, $"Bonus {Fmt.Num(l.BonusGold)} / {Fmt.Num(l.BonusElixir)}", 22, TextAnchor.MiddleRight, false);
                UIKit.Place((RectTransform)b.transform, UIKit.MR, new Vector2(-24, 0), new Vector2(300, 80));
                if (l == league)
                {
                    var you = UIKit.Label(row, "YOU", 20, Color.white);
                    UIKit.Place((RectTransform)you.transform, UIKit.TR, new Vector2(-10, -4), new Vector2(60, 24));
                }
            }
        }

        static Color TierColor(int i)
        {
            switch (i)
            {
                case 1: return Palette.Hex("b87333");
                case 2: return Palette.Hex("c0c0c0");
                case 3: return Palette.Gold;
                case 4: return Palette.Hex("8fd3ff");
                case 5: return Palette.Hex("c48cff");
                case 6: return Palette.Hex("ff5a5a");
                default: return Palette.Hex("ffd27a");
            }
        }

        static void Ranking(RectTransform body, string[] names, System.Func<int, string> sub, int? highlight, string icon)
        {
            var list = UIKit.Scroll(body, out _);
            UIKit.VList(list, 8, 4);
            UIKit.FitContent(list);
            for (int i = 0; i < names.Length; i++)
            {
                var row = UIKit.Node("row", list);
                UIKit.Size(row, 1200, 76);
                UIKit.Panel(row, highlight == i ? Palette.Hex("f5e3a0") : Palette.CreamDark, true, false);
                var r = UIKit.Dark(row, (i + 1).ToString(), 30, TextAnchor.MiddleCenter);
                UIKit.Place((RectTransform)r.transform, UIKit.ML, new Vector2(10, 0), new Vector2(70, 70));
                var ic = UIKit.Icon(row, icon, 50);
                UIKit.Place((RectTransform)ic.transform, UIKit.ML, new Vector2(90, 0), new Vector2(50, 50));
                var n = UIKit.Dark(row, names[i], 26, TextAnchor.MiddleLeft);
                UIKit.Place((RectTransform)n.transform, UIKit.ML, new Vector2(160, 0), new Vector2(500, 70));
                var sb = UIKit.Dark(row, sub(i), 22, TextAnchor.MiddleRight, false);
                UIKit.Place((RectTransform)sb.transform, UIKit.MR, new Vector2(-24, 0), new Vector2(520, 70));
            }
        }
    }
}
