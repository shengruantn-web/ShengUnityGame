using System;
using UnityEngine;
using UnityEngine.UI;

namespace VillageClash.UI
{
    /// <summary>Text input field built from code, styled like the rest of the UI.</summary>
    public static class UIInput
    {
        public static InputField Field(Transform parent, string placeholder, Vector2 size, Action<string> onChanged = null, Action<string> onSubmit = null, string initial = "")
        {
            var rt = UIKit.Node("input", parent);
            UIKit.Size(rt, size.x, size.y);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.sprite = SpriteFactory.RoundRect; bg.type = Image.Type.Sliced; bg.color = Color.white;
            var f = rt.gameObject.AddComponent<InputField>();
            var txt = UIKit.Dark(rt, "", 24, TextAnchor.MiddleLeft, false);
            UIKit.Stretch((RectTransform)txt.transform, 16, 4, 16, 4);
            txt.supportRichText = false;
            var ph = UIKit.Dark(rt, placeholder, 24, TextAnchor.MiddleLeft, false);
            ph.color = new Color(0.45f, 0.42f, 0.38f);
            ph.fontStyle = FontStyle.Italic;
            UIKit.Stretch((RectTransform)ph.transform, 16, 4, 16, 4);
            f.textComponent = txt;
            f.placeholder = ph;
            f.targetGraphic = bg;
            f.characterLimit = 24;
            f.text = initial;
            if (onChanged != null) f.onValueChanged.AddListener(v => onChanged(v));
            if (onSubmit != null) f.onEndEdit.AddListener(v => onSubmit(v));
            return f;
        }
    }
}
