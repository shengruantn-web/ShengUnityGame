using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VillageClash
{
    /// <summary>Screen-space overlay for progress bars, tap bubbles and floating text pinned to world positions.
    /// Items are keyed; an item not touched for two frames is hidden.</summary>
    public sealed class WorldLabels : MonoBehaviour
    {
        sealed class Item
        {
            public GameObject Go; public RectTransform Rt; public Image Fill; public Text Text; public Image Icon;
            public Action OnClick; public int Frame; public Vector3 World; public Vector2 Offset;
        }

        readonly Dictionary<string, Item> items = new Dictionary<string, Item>();
        RectTransform canvasRt;
        Font font;

        public static WorldLabels Create(Transform parent)
        {
            var go = new GameObject("WorldLabels", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 0.5f;
            var wl = go.AddComponent<WorldLabels>();
            wl.canvasRt = (RectTransform)go.transform;
            wl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return wl;
        }

        public void Clear()
        {
            foreach (var it in items.Values) Destroy(it.Go);
            items.Clear();
        }

        Item Get(string key, Func<Item> make)
        {
            if (!items.TryGetValue(key, out var it)) { it = make(); items[key] = it; }
            it.Frame = Time.frameCount;
            if (!it.Go.activeSelf) it.Go.SetActive(true);
            return it;
        }

        /// <summary>Horizontal bar with text above a world point.</summary>
        public void Bar(string key, Vector3 world, float fraction, Color color, string text, float width = 110)
        {
            var it = Get(key, () =>
            {
                var i = new Item();
                i.Go = new GameObject("bar", typeof(RectTransform));
                i.Rt = (RectTransform)i.Go.transform;
                i.Rt.SetParent(canvasRt, false);
                i.Rt.sizeDelta = new Vector2(width, 18);
                var bg = new GameObject("bg", typeof(Image)).GetComponent<Image>();
                bg.transform.SetParent(i.Rt, false);
                bg.sprite = SpriteFactory.RoundRect; bg.type = Image.Type.Sliced; bg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f); bg.raycastTarget = false;
                Stretch((RectTransform)bg.transform, 0, 0, 0, 0);
                var fillGo = new GameObject("fill", typeof(Image));
                i.Fill = fillGo.GetComponent<Image>();
                i.Fill.transform.SetParent(i.Rt, false);
                i.Fill.sprite = SpriteFactory.RoundRect; i.Fill.type = Image.Type.Sliced; i.Fill.raycastTarget = false;
                i.Fill.pixelsPerUnitMultiplier = 2f;
                var frt = (RectTransform)i.Fill.transform;
                frt.anchorMin = new Vector2(0, 0); frt.anchorMax = new Vector2(1, 1); frt.pivot = new Vector2(0, 0.5f);
                frt.offsetMin = new Vector2(3, 3); frt.offsetMax = new Vector2(-3, -3);
                i.Text = MakeText(i.Rt, 20);
                var trt = (RectTransform)i.Text.transform;
                trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1); trt.pivot = new Vector2(0.5f, 0);
                trt.anchoredPosition = new Vector2(0, 2); trt.sizeDelta = new Vector2(0, 26);
                return i;
            });
            it.World = world; it.Offset = Vector2.zero;
            it.Fill.color = color;
            var frt2 = (RectTransform)it.Fill.transform;
            frt2.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1);
            it.Text.text = text;
        }

        /// <summary>Tappable icon bubble above a world point.</summary>
        public void Bubble(string key, Vector3 world, Sprite icon, Action onClick, float size = 56)
        {
            var it = Get(key, () =>
            {
                var i = new Item();
                i.Go = new GameObject("bubble", typeof(RectTransform));
                i.Rt = (RectTransform)i.Go.transform;
                i.Rt.SetParent(canvasRt, false);
                i.Rt.sizeDelta = new Vector2(size, size);
                var bg = new GameObject("bg", typeof(Image), typeof(Button)).GetComponent<Image>();
                bg.transform.SetParent(i.Rt, false);
                bg.sprite = SpriteFactory.Circle; bg.color = new Color(1f, 1f, 1f, 0.92f);
                Stretch((RectTransform)bg.transform, 0, 0, 0, 0);
                var captured = i;
                bg.GetComponent<Button>().onClick.AddListener(() => captured.OnClick?.Invoke());
                var ic = new GameObject("icon", typeof(Image)).GetComponent<Image>();
                ic.transform.SetParent(i.Rt, false); ic.raycastTarget = false;
                Stretch((RectTransform)ic.transform, 8, 8, 8, 8);
                i.Icon = ic;
                i.Go.AddComponent<Bob>();
                return i;
            });
            it.World = world; it.Offset = new Vector2(0, 10);
            it.Icon.sprite = icon;
            it.OnClick = onClick;
        }

        /// <summary>Plain floating text (e.g. names, damage).</summary>
        public void Label(string key, Vector3 world, string text, Color color, int size = 22, Vector2 offset = default)
        {
            var it = Get(key, () =>
            {
                var i = new Item();
                i.Go = new GameObject("label", typeof(RectTransform));
                i.Rt = (RectTransform)i.Go.transform;
                i.Rt.SetParent(canvasRt, false);
                i.Rt.sizeDelta = new Vector2(300, 30);
                i.Text = MakeText(i.Rt, size);
                Stretch((RectTransform)i.Text.transform, 0, 0, 0, 0);
                return i;
            });
            it.World = world; it.Offset = offset;
            it.Text.text = text; it.Text.color = color; it.Text.fontSize = size;
        }

        Text MakeText(RectTransform parent, int size)
        {
            var go = new GameObject("text", typeof(Text), typeof(Outline), typeof(Shadow));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font; t.fontSize = size; t.fontStyle = FontStyle.Bold; t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white; t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            var o = go.GetComponent<Outline>(); o.effectColor = new Color(0, 0, 0, 0.9f); o.effectDistance = new Vector2(1.5f, -1.5f);
            var s = go.GetComponent<Shadow>(); s.effectColor = new Color(0, 0, 0, 0.6f); s.effectDistance = new Vector2(0, -2);
            return t;
        }

        static void Stretch(RectTransform rt, float l, float t, float r, float b)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        void LateUpdate()
        {
            var cam = GameRoot.I != null ? GameRoot.I.Cam : null;
            if (cam == null) return;
            foreach (var it in items.Values)
            {
                if (it.Frame < Time.frameCount - 1) { if (it.Go.activeSelf) it.Go.SetActive(false); continue; }
                var screen = cam.WorldToScreen(it.World);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out var local);
                it.Rt.anchoredPosition = local + it.Offset;
            }
        }

        sealed class Bob : MonoBehaviour
        {
            RectTransform rt; float phase;
            void Awake() { rt = (RectTransform)transform; phase = UnityEngine.Random.value * 6f; }
            void Update() { float s = 1f + Mathf.Sin(Time.unscaledTime * 3f + phase) * 0.06f; rt.localScale = new Vector3(s, s, 1); }
        }
    }
}
