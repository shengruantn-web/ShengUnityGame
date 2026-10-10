using System;
using UnityEngine;
using UnityEngine.UI;
using VillageClash.Core;

namespace VillageClash.UI
{
    /// <summary>Village heads-up display laid out like the reference: profile top-left, builders/shield top-center,
    /// resources top-right, Attack/Army/Castle bottom-left, Settings/News/Shop bottom-right, chat tab on the left edge.</summary>
    public sealed class Hud : MonoBehaviour
    {
        public Action OnAttack, OnArmy, OnCastle, OnShop, OnSettings, OnNews, OnClans, OnLeague, OnBuyBuilder, OnBuyShield, OnGems, OnMail, OnProfile;

        Text nameText, levelText, trophiesText, buildersText, shieldText, goldText, elixirText, gemsText, mailBadge, armyBadge;
        BarView xpBar, goldBar, elixirBar;
        BtnView builderPlus, shieldPlus, attackBtn;
        RectTransform root;
        float timer;

        public static Hud Create()
        {
            var rt = UIKit.Node("Hud", UIKit.HudLayer);
            UIKit.Stretch(rt);
            var hud = rt.gameObject.AddComponent<Hud>();
            hud.root = rt;
            hud.Build();
            GameEvents.StateChanged += hud.Refresh;
            hud.Refresh();
            return hud;
        }

        void OnDestroy() { GameEvents.StateChanged -= Refresh; }

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer >= 1f) { timer = 0; Refresh(); }
        }

        GameState S => GameRoot.I.Controller.State;

        void Build()
        {
            // ---------- top-left: profile ----------
            var prof = UIKit.Node("profile", root);
            UIKit.Place(prof, UIKit.TL, new Vector2(16, -14), new Vector2(420, 120));
            var profBtn = prof.gameObject.AddComponent<Button>(); profBtn.transition = Selectable.Transition.None;
            prof.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.001f);
            profBtn.onClick.AddListener(() => OnProfile?.Invoke());
            // level badge
            var badge = UIKit.Node("level", prof);
            UIKit.Place(badge, UIKit.TL, Vector2.zero, new Vector2(72, 72));
            var badgeImg = UIKit.Sprite(badge, SpriteFactory.Icon("badge"), 72, 72);
            levelText = UIKit.Label(badge, "1", 30);
            // xp bar
            xpBar = UIKit.Bar(prof, new Vector2(230, 26), Palette.Blue, null, 18);
            UIKit.Place(xpBar.Rt, UIKit.TL, new Vector2(70, -6), new Vector2(230, 26));
            // name
            nameText = UIKit.Label(prof, "Chief", 30, Color.white, TextAnchor.MiddleLeft);
            UIKit.Place((RectTransform)nameText.transform, UIKit.TL, new Vector2(80, -34), new Vector2(300, 36));
            // trophies + shield
            var troph = UIKit.Node("trophies", prof);
            UIKit.Place(troph, UIKit.TL, new Vector2(0, -78), new Vector2(170, 38));
            var tb = troph.gameObject.AddComponent<Button>(); tb.transition = Selectable.Transition.None;
            UIKit.Panel(troph, new Color(0.1f, 0.1f, 0.1f, 0.75f));
            tb.onClick.AddListener(() => { Sfx.Play(SfxId.Tap); OnLeague?.Invoke(); });
            var ti = UIKit.Icon(troph, "trophy", 34); UIKit.Place((RectTransform)ti.transform, UIKit.ML, new Vector2(4, 0), new Vector2(34, 34));
            trophiesText = UIKit.Label(troph, "0", 24, Color.white, TextAnchor.MiddleLeft);
            UIKit.Stretch((RectTransform)trophiesText.transform, 44, 0, 6, 0);
            // mail
            var mail = UIKit.IconBtn(prof, "mail", Palette.Blue, 56, () => OnMail?.Invoke());
            UIKit.Place(mail.Rt, UIKit.TL, new Vector2(184, -70), new Vector2(56, 56));
            mailBadge = UIKit.Badge(mail.Rt, UIKit.TR, new Vector2(6, 6), 28);
            mailBadge.transform.parent.gameObject.SetActive(false);

            // ---------- top-center: builders + shield ----------
            var center = UIKit.Node("center", root);
            UIKit.Place(center, UIKit.TC, new Vector2(0, -14), new Vector2(520, 50));
            var builders = Pill(center, "builder", out buildersText, UIKit.ML, new Vector2(0, 0), 250, out builderPlus, () => OnBuyBuilder?.Invoke());
            var shield = Pill(center, "shield", out shieldText, UIKit.MR, new Vector2(0, 0), 250, out shieldPlus, () => OnBuyShield?.Invoke());

            // ---------- top-right: resources ----------
            var res = UIKit.Node("resources", root);
            UIKit.Place(res, UIKit.TR, new Vector2(-16, -14), new Vector2(380, 160));
            goldBar = ResBar(res, "gold", Palette.Gold, 0, out goldText);
            elixirBar = ResBar(res, "elixir", Palette.Elixir, -50, out elixirText);
            var gems = UIKit.Node("gems", res);
            UIKit.Place(gems, UIKit.TR, new Vector2(0, -100), new Vector2(380, 44));
            UIKit.Panel(gems, new Color(0.1f, 0.1f, 0.1f, 0.8f), true, false);
            var gi = UIKit.Icon(gems, "gem", 40); UIKit.Place((RectTransform)gi.transform, UIKit.ML, new Vector2(4, 0), new Vector2(40, 40));
            gemsText = UIKit.Label(gems, "0", 26, Color.white, TextAnchor.MiddleRight);
            UIKit.Stretch((RectTransform)gemsText.transform, 50, 0, 60, 0);
            var gplus = UIKit.IconBtn(gems, "plus", Palette.Green, 38, () => OnGems?.Invoke(), true);
            UIKit.Place(gplus.Rt, UIKit.MR, new Vector2(-4, 0), new Vector2(38, 38));

            // ---------- bottom-left: attack / army / castle ----------
            var bl = UIKit.Node("bottom-left", root);
            UIKit.Place(bl, UIKit.BL, new Vector2(16, 16), new Vector2(520, 130));
            attackBtn = UIKit.Btn(bl, "Attack!", Palette.Red, new Vector2(200, 92), () => OnAttack?.Invoke(), 32, "sword");
            UIKit.Place(attackBtn.Rt, UIKit.BL, Vector2.zero, new Vector2(200, 92));
            var army = UIKit.Btn(bl, "Army", Palette.Blue, new Vector2(130, 92), () => OnArmy?.Invoke(), 26, "fist");
            UIKit.Place(army.Rt, UIKit.BL, new Vector2(212, 0), new Vector2(130, 92));
            // stack label under icon instead of beside it
            UIKit.Place((RectTransform)army.Label.transform, UIKit.BC, new Vector2(0, 4), new Vector2(130, 30));
            UIKit.Place((RectTransform)army.Go.transform.Find("icon:fist"), UIKit.TC, new Vector2(0, -4), new Vector2(50, 50));
            armyBadge = UIKit.Badge(army.Rt, UIKit.TR, new Vector2(6, 6), 30);
            armyBadge.transform.parent.gameObject.SetActive(false);
            var castle = UIKit.Btn(bl, "Clan Castle", Palette.Blue, new Vector2(150, 92), () => OnCastle?.Invoke(), 22, "castle");
            UIKit.Place(castle.Rt, UIKit.BL, new Vector2(354, 0), new Vector2(150, 92));
            UIKit.Place((RectTransform)castle.Label.transform, UIKit.BC, new Vector2(0, 4), new Vector2(150, 30));
            UIKit.Place((RectTransform)castle.Go.transform.Find("icon:castle"), UIKit.TC, new Vector2(0, -4), new Vector2(50, 50));

            // ---------- bottom-right: settings / news / shop ----------
            var br = UIKit.Node("bottom-right", root);
            UIKit.Place(br, UIKit.BR, new Vector2(-16, 16), new Vector2(420, 130));
            var shop = UIKit.Btn(br, "Shop", Palette.Green, new Vector2(150, 92), () => OnShop?.Invoke(), 26, "shop");
            UIKit.Place(shop.Rt, UIKit.BR, Vector2.zero, new Vector2(150, 92));
            UIKit.Place((RectTransform)shop.Label.transform, UIKit.BC, new Vector2(0, 4), new Vector2(150, 30));
            UIKit.Place((RectTransform)shop.Go.transform.Find("icon:shop"), UIKit.TC, new Vector2(0, -4), new Vector2(50, 50));
            var news = UIKit.Btn(br, "News", Palette.Blue, new Vector2(120, 92), () => OnNews?.Invoke(), 24, "scroll");
            UIKit.Place(news.Rt, UIKit.BR, new Vector2(-162, 0), new Vector2(120, 92));
            UIKit.Place((RectTransform)news.Label.transform, UIKit.BC, new Vector2(0, 4), new Vector2(120, 30));
            UIKit.Place((RectTransform)news.Go.transform.Find("icon:scroll"), UIKit.TC, new Vector2(0, -4), new Vector2(50, 50));
            var settings = UIKit.Btn(br, "Settings", Palette.GreyDark, new Vector2(120, 92), () => OnSettings?.Invoke(), 22, "gear");
            UIKit.Place(settings.Rt, UIKit.BR, new Vector2(-294, 0), new Vector2(120, 92));
            UIKit.Place((RectTransform)settings.Label.transform, UIKit.BC, new Vector2(0, 4), new Vector2(120, 30));
            UIKit.Place((RectTransform)settings.Go.transform.Find("icon:gear"), UIKit.TC, new Vector2(0, -4), new Vector2(50, 50));

            // ---------- left edge: chat/clan tab ----------
            var chat = UIKit.IconBtn(root, "chat", Palette.Orange, 64, () => OnClans?.Invoke());
            UIKit.Place(chat.Rt, UIKit.ML, new Vector2(0, 0), new Vector2(54, 150));
            var chatIcon = (RectTransform)chat.Go.transform.Find("icon:chat");
            UIKit.Place(chatIcon, UIKit.MC, Vector2.zero, new Vector2(40, 40));
        }

        static RectTransform Pill(Transform parent, string icon, out Text text, Vector2 anchor, Vector2 pos, float width, out BtnView plus, Action onPlus)
        {
            var rt = UIKit.Node("pill:" + icon, parent);
            UIKit.Place(rt, anchor, pos, new Vector2(width, 46));
            UIKit.Panel(rt, new Color(0.1f, 0.1f, 0.1f, 0.8f), true, false);
            var ic = UIKit.Icon(rt, icon, 38); UIKit.Place((RectTransform)ic.transform, UIKit.ML, new Vector2(6, 0), new Vector2(38, 38));
            text = UIKit.Label(rt, "", 24, Color.white, TextAnchor.MiddleLeft);
            UIKit.Stretch((RectTransform)text.transform, 52, 0, 50, 0);
            plus = UIKit.IconBtn(rt, "plus", Palette.Green, 38, onPlus, true);
            UIKit.Place(plus.Rt, UIKit.MR, new Vector2(-4, 0), new Vector2(38, 38));
            return rt;
        }

        static BarView ResBar(Transform parent, string icon, Color color, float y, out Text text)
        {
            var bar = UIKit.Bar(parent, new Vector2(330, 42), color, new Color(0.1f, 0.1f, 0.1f, 0.8f), 24);
            UIKit.Place(bar.Rt, UIKit.TR, new Vector2(0, y), new Vector2(330, 42));
            bar.Label.alignment = TextAnchor.MiddleRight;
            UIKit.Stretch((RectTransform)bar.Label.transform, 8, 0, 12, 0);
            text = bar.Label;
            var ic = UIKit.Icon(parent, icon, 48);
            UIKit.Place((RectTransform)ic.transform, UIKit.TR, new Vector2(-330 + 14, y + 4), new Vector2(50, 50));
            return bar;
        }

        public void Refresh()
        {
            if (GameRoot.I == null) return;
            var s = S;
            long now = GameRoot.I.Controller.Now;
            nameText.text = s.PlayerName;
            levelText.text = s.Level.ToString();
            int need = Progression.XpToNext(s.Level);
            xpBar.Set(need > 0 ? s.Xp / (float)need : 1f, $"{s.Xp} / {need}");
            trophiesText.text = Fmt.Num(s.Trophies);

            int total = Builders.Total(s), free = Builders.Free(s);
            buildersText.text = $"{free}/{total}";
            builderPlus.Go.SetActive(total < GameData.MaxBuilders);
            bool shielded = s.ShieldActive(now);
            shieldText.text = shielded ? Fmt.Time(s.ShieldUntilUnix - now) : "No shield";
            shieldPlus.Go.SetActive(!shielded);

            int gcap = Economy.GoldCap(s), ecap = Economy.ElixirCap(s);
            goldBar.Set(gcap > 0 ? s.Gold / (float)gcap : 0, Fmt.Num(s.Gold));
            elixirBar.Set(ecap > 0 ? s.Elixir / (float)ecap : 0, Fmt.Num(s.Elixir));
            gemsText.text = Fmt.Num(s.Gems);

            int unseen = 0;
            foreach (var r in s.DefenseLog) if (!r.Seen) unseen++;
            mailBadge.transform.parent.gameObject.SetActive(unseen > 0);
            mailBadge.text = unseen.ToString();

            int armySize = s.ArmySize;
            armyBadge.transform.parent.gameObject.SetActive(armySize > 0);
            armyBadge.text = armySize.ToString();
        }
    }
}
