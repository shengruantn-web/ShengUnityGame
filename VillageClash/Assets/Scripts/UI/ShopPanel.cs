using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VillageClash.Core;

namespace VillageClash.UI
{
    /// <summary>Shop modal: icon tab row (only Buildings populated), category sub-tabs with badges, scrolling cards, resource strip.</summary>
    public static class ShopPanel
    {
        static readonly string[] MainTabs = { "hammer", "sword", "gold", "shield", "trophy", "star" };
        static readonly string[] MainNames = { "Buildings", "Army", "Treasure", "Shield", "League", "Special" };
        static readonly string[] SubNames = { "Army", "Resources", "Defenses", "Traps" };
        static readonly BuildingCategory[] SubCats = { BuildingCategory.Army, BuildingCategory.Resource, BuildingCategory.Defense, BuildingCategory.Trap };

        public static void Show(Action<BuildingType> onBuy, int subTab = 1)
        {
            var c = GameRoot.I.Controller;
            var m = UIKit.ShowModal("Shop", new Vector2(1560, 880));
            var content = m.Content;

            // main icon tabs
            var mainRow = UIKit.Node("main-tabs", content);
            UIKit.Place(mainRow, UIKit.TL, new Vector2(10, -4), new Vector2(900, 80));
            UIKit.HList(mainRow, 10);
            var mainBtns = new List<BtnView>();
            int mainSel = 0;
            var body = UIKit.Node("body", content);
            UIKit.Stretch(body, 0, 92, 0, 70);

            // sub tabs + badges
            var subRow = UIKit.Node("sub-tabs", body);
            UIKit.Place(subRow, UIKit.TL, new Vector2(10, 0), new Vector2(900, 60));
            UIKit.HList(subRow, 8);
            var subBtns = new List<BtnView>();
            var badges = new List<Text>();
            var cards = UIKit.Node("cards", body);
            UIKit.Stretch(cards, 0, 70, 0, 0);
            UIKit.Panel(cards, Palette.CreamDark, true, false);
            var other = UIKit.Dark(body, "", 32, TextAnchor.MiddleCenter);
            UIKit.Stretch((RectTransform)other.transform, 0, 70, 0, 0);
            other.gameObject.SetActive(false);

            int subSel = Mathf.Clamp(subTab, 0, 3);
            Action rebuild = null;

            for (int i = 0; i < MainTabs.Length; i++)
            {
                int idx = i;
                var b = UIKit.IconBtn(mainRow, MainTabs[i], i == 0 ? Palette.Green : Palette.GreyDark, 72, () =>
                {
                    mainSel = idx;
                    for (int k = 0; k < mainBtns.Count; k++) mainBtns[k].SetColor(k == idx ? Palette.Green : Palette.GreyDark);
                    rebuild();
                });
                UIKit.Size(b.Rt, 72, 72);
                mainBtns.Add(b);
            }
            for (int i = 0; i < SubNames.Length; i++)
            {
                int idx = i;
                var b = UIKit.Btn(subRow, SubNames[i], Palette.GreyDark, new Vector2(200, 56), () =>
                {
                    subSel = idx; rebuild();
                }, 26);
                UIKit.Size(b.Rt, 200, 56);
                var badge = UIKit.Badge(b.Rt, UIKit.TR, new Vector2(8, 8), 32);
                badges.Add(badge);
                subBtns.Add(b);
            }

            // resource strip
            var strip = UIKit.Node("strip", content);
            UIKit.Place(strip, UIKit.BC, new Vector2(0, 4), new Vector2(1500, 56));
            UIKit.Panel(strip, Palette.Header, true, false);
            var gold = UIKit.Label(strip, "", 26, Color.white, TextAnchor.MiddleLeft);
            var elixir = UIKit.Label(strip, "", 26, Color.white, TextAnchor.MiddleLeft);
            var gems = UIKit.Label(strip, "", 26, Color.white, TextAnchor.MiddleLeft);
            StripItem(strip, "gold", gold, 40);
            StripItem(strip, "elixir", elixir, 400);
            StripItem(strip, "gem", gems, 760);
            var hint = UIKit.Label(strip, $"Town Hall level {c.State.TownHallLevel}", 24, Palette.Gold, TextAnchor.MiddleRight);
            UIKit.Stretch((RectTransform)hint.transform, 0, 0, 20, 0);

            Action refreshStrip = () =>
            {
                gold.text = Fmt.Num(c.State.Gold); elixir.text = Fmt.Num(c.State.Elixir); gems.text = Fmt.Num(c.State.Gems);
            };

            rebuild = () =>
            {
                foreach (Transform t in cards) UnityEngine.Object.Destroy(t.gameObject);
                refreshStrip();
                bool buildings = mainSel == 0;
                subRow.gameObject.SetActive(buildings);
                cards.gameObject.SetActive(buildings);
                other.gameObject.SetActive(!buildings);
                if (!buildings)
                {
                    other.text = mainSel == 3
                        ? $"Shields are bought from the village screen.\nTap the + next to the shield for {GameData.ShieldGemCost} gems."
                        : $"{MainNames[mainSel]}\n\nNothing for sale here yet. Check back later!";
                    return;
                }
                for (int k = 0; k < subBtns.Count; k++)
                {
                    subBtns[k].SetColor(k == subSel ? Palette.Green : Palette.GreyDark);
                    int affordable = 0;
                    foreach (var def in GameData.AllBuildings)
                    {
                        if (def.Category != SubCats[k]) continue;
                        var st = c.Shop(def.Type);
                        if (st.CanBuildMore && st.Affordable) affordable++;
                    }
                    badges[k].transform.parent.gameObject.SetActive(affordable > 0);
                    badges[k].text = affordable.ToString();
                }
                UIKit.Panel(cards, Palette.CreamDark, true, false);
                var scrollContent = UIKit.Scroll(cards, out var sr, true);
                UIKit.HList(scrollContent, 16, 16, TextAnchor.MiddleLeft);
                UIKit.FitContent(scrollContent, true, false);
                foreach (var def in GameData.AllBuildings)
                {
                    if (def.Category != SubCats[subSel]) continue;
                    Card(scrollContent, c, def, m, onBuy);
                }
            };
            rebuild();
            Action onState = () => rebuild();
            GameEvents.StateChanged += onState;
            m.OnClosed = () => GameEvents.StateChanged -= onState;
        }

        static void StripItem(RectTransform strip, string icon, Text label, float x)
        {
            var ic = UIKit.Icon(strip, icon, 40);
            UIKit.Place((RectTransform)ic.transform, UIKit.ML, new Vector2(x, 0), new Vector2(40, 40));
            UIKit.Place((RectTransform)label.transform, UIKit.ML, new Vector2(x + 50, 0), new Vector2(250, 50));
        }

        static void Card(RectTransform parent, GameController c, BuildingDef def, Modal m, Action<BuildingType> onBuy)
        {
            var st = c.Shop(def.Type);
            var card = UIKit.Frame(parent, Palette.Cream, 3);
            UIKit.Size(card, 270, 420);
            var inner = card;

            var title = UIKit.Dark(inner, def.Name, 26, TextAnchor.MiddleCenter);
            UIKit.Place((RectTransform)title.transform, UIKit.TC, new Vector2(0, -8), new Vector2(220, 36));
            var info = UIKit.IconBtn(inner, "info", Palette.Blue, 36, () => InfoModal(def), true);
            UIKit.Place(info.Rt, UIKit.TR, new Vector2(-8, -8), new Vector2(36, 36));

            var pic = UIKit.Sprite(inner, SpriteFactory.Building(def.Type, 1), 200, 200);
            UIKit.Place((RectTransform)pic.transform, UIKit.TC, new Vector2(0, -50), new Vector2(200, 190));

            var time = UIKit.Dark(inner, Fmt.Time(st.BuildSeconds), 22, TextAnchor.MiddleCenter);
            UIKit.Place((RectTransform)time.transform, UIKit.TC, new Vector2(16, -246), new Vector2(200, 30));
            var clock = UIKit.Icon(inner, "clock", 26);
            UIKit.Place((RectTransform)clock.transform, UIKit.TC, new Vector2(-60, -246), new Vector2(26, 26));

            var built = UIKit.Dark(inner, $"Built: {st.Built} / {st.Max}", 22, TextAnchor.MiddleCenter);
            UIKit.Place((RectTransform)built.transform, UIKit.TC, new Vector2(0, -280), new Vector2(240, 30));

            string priceIcon = st.CostGems > 0 ? "gem" : st.CostElixir > 0 ? "elixir" : "gold";
            int price = st.CostGems > 0 ? st.CostGems : st.CostElixir > 0 ? st.CostElixir : st.CostGold;
            var buy = UIKit.Btn(inner, Fmt.Num(price), st.Affordable ? Palette.Green : Palette.Grey, new Vector2(230, 64), () =>
            {
                var now = c.Shop(def.Type);
                if (!now.CanBuildMore) { GameEvents.RaiseToast(Msg.MaxReached); return; }
                if (Builders.Free(c.State) == 0) { GameEvents.RaiseToast(Msg.BuildersBusy); Sfx.Play(SfxId.Error); return; }
                if (!now.Affordable)
                {
                    if (now.CostGems > 0) { GameEvents.RaiseToast(Msg.NotEnoughGems); Sfx.Play(SfxId.Error); return; }
                    ConfirmDialog.OfferGemFill(c, now.CostGold, now.CostElixir, () => { m.Close(); onBuy(def.Type); });
                    return;
                }
                m.Close();
                onBuy(def.Type);
            }, 28, priceIcon);
            UIKit.Place(buy.Rt, UIKit.BC, new Vector2(0, 12), new Vector2(230, 64));
            if (!st.CanBuildMore && st.Unlocked) { buy.SetEnabled(false); buy.SetText("Max built"); }

            if (!st.Unlocked)
            {
                var lockP = UIKit.Panel(inner, new Color(0.3f, 0.3f, 0.3f, 0.82f));
                UIKit.Stretch((RectTransform)lockP.transform, 3, 3, 3, 3);
                var li = UIKit.Icon(lockP.transform, "lock", 64);
                UIKit.Place((RectTransform)li.transform, UIKit.MC, new Vector2(0, 40), new Vector2(64, 64));
                var lt = UIKit.Label(lockP.transform, st.LockText, 24);
                UIKit.Place((RectTransform)lt.transform, UIKit.MC, new Vector2(0, -30), new Vector2(230, 80));
                buy.SetEnabled(false);
            }
        }

        public static void InfoModal(BuildingDef def)
        {
            var m = UIKit.ShowModal(def.Name, new Vector2(900, 620));
            var pic = UIKit.Sprite(m.Content, SpriteFactory.Building(def.Type, def.MaxLevel), 220, 220);
            UIKit.Place((RectTransform)pic.transform, UIKit.TL, new Vector2(20, -10), new Vector2(220, 220));
            var desc = UIKit.Dark(m.Content, def.Description, 24, TextAnchor.UpperLeft, false);
            UIKit.Place((RectTransform)desc.transform, UIKit.TL, new Vector2(260, -10), new Vector2(600, 220));
            var table = UIKit.Node("table", m.Content);
            UIKit.Stretch(table, 20, 240, 20, 10);
            UIKit.VList(table, 4);
            string header = def.IsDefense ? "Lvl   HP   Damage   Range   Cost" : Economy.IsProducer(def.Type) ? "Lvl   HP   Per hour   Capacity   Cost" : def.Category == BuildingCategory.Army && def.Type == BuildingType.ArmyCamp ? "Lvl   HP   Capacity   Cost" : "Lvl   HP   Cost";
            var h = UIKit.Dark(table, header, 22, TextAnchor.MiddleLeft);
            UIKit.Size((RectTransform)h.transform, 840, 30);
            for (int l = 1; l <= def.MaxLevel; l++)
            {
                var s = def.Stats(l);
                string cost = s.CostGems > 0 ? $"{Fmt.Num(s.CostGems)} gems" : s.CostElixir > 0 ? $"{Fmt.Num(s.CostElixir)} elixir" : $"{Fmt.Num(s.CostGold)} gold";
                string row = def.IsDefense ? $"{l}      {s.Hp}      {s.Damage}      {s.Range}      {cost}"
                    : Economy.IsProducer(def.Type) ? $"{l}      {s.Hp}      {s.ProductionPerHour}      {Fmt.Num(s.Capacity)}      {cost}"
                    : def.Type == BuildingType.ArmyCamp ? $"{l}      {s.Hp}      {s.Capacity}      {cost}"
                    : $"{l}      {s.Hp}      {cost}";
                var r = UIKit.Dark(table, row + $"   (TH {s.ThRequired})", 22, TextAnchor.MiddleLeft, false);
                UIKit.Size((RectTransform)r.transform, 840, 30);
            }
        }
    }
}
