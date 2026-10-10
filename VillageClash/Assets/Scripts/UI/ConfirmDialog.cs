using System;
using UnityEngine;
using VillageClash.Core;

namespace VillageClash.UI
{
    public static class ConfirmDialog
    {
        public static Modal Show(string title, string message, string yesText, Action onYes, string noText = "Cancel", Color? yesColor = null, string yesIcon = null)
        {
            var m = UIKit.ShowModal(title, new Vector2(620, 340), null, false, false);
            var msg = UIKit.Dark(m.Content, message, 26, TextAnchor.MiddleCenter);
            UIKit.Stretch((RectTransform)msg.transform, 20, 10, 20, 100);
            var no = UIKit.Btn(m.Content, noText, Palette.GreyDark, new Vector2(220, 70), () => m.Close(), 28);
            UIKit.Place(no.Rt, UIKit.BL, new Vector2(40, 14), new Vector2(220, 70));
            var yes = UIKit.Btn(m.Content, yesText, yesColor ?? Palette.Green, new Vector2(260, 70), () => { m.Close(); onYes?.Invoke(); }, 28, yesIcon);
            UIKit.Place(yes.Rt, UIKit.BR, new Vector2(-40, 14), new Vector2(260, 70));
            return m;
        }

        /// <summary>Spec §7: offer to fill missing resources with gems.</summary>
        public static void OfferGemFill(GameController c, int gold, int elixir, Action after)
        {
            int gems = c.GemFillCost(gold, elixir);
            if (gems <= 0) { after?.Invoke(); return; }
            Show("Not enough resources", $"Fill the missing resources for {gems} gems?", $"{gems}", () =>
            {
                if (c.FillWithGems(gold, elixir)) { Sfx.Play(SfxId.Coin); after?.Invoke(); }
            }, "Cancel", Palette.Green, "gem");
        }
    }
}
