using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VillageClash.UI
{
    public sealed class BtnView
    {
        public Button Button; public Image Image; public Text Label; public RectTransform Rt; public Color BaseColor;
        public GameObject Go => Rt.gameObject;
        public void SetEnabled(bool on)
        {
            Button.interactable = on;
            Image.color = on ? BaseColor : Palette.Grey;
            if (Label != null) Label.color = on ? Color.white : new Color(0.85f, 0.85f, 0.85f);
        }
        public void SetColor(Color c) { BaseColor = c; if (Button.interactable) Image.color = c; }
        public void SetText(string t) { if (Label != null) Label.text = t; }
    }

    public sealed class BarView
    {
        public RectTransform Rt; public Image Fill; public Text Label;
        public void Set(float fraction, string text = null)
        {
            var frt = (RectTransform)Fill.transform;
            frt.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1);
            if (text != null && Label != null) Label.text = text;
        }
    }

    public sealed class TabsView
    {
        public List<BtnView> Buttons = new List<BtnView>();
        public int Selected = -1;
        public Action<int> OnSelect;
        public Color On = Palette.Green, Off = Palette.GreyDark;
        public void Select(int i)
        {
            Selected = i;
            for (int k = 0; k < Buttons.Count; k++) Buttons[k].SetColor(k == i ? On : Off);
            OnSelect?.Invoke(i);
        }
    }

    public sealed class Modal
    {
        public RectTransform Root, Window, Content; public Text Title; public Action OnClosed; public bool Closed;
        public void Close()
        {
            if (Closed) return;
            Closed = true;
            UIKit.CloseModal(this);
        }
    }

    /// <summary>Code-built uGUI toolkit: canvas, primitives, modals, toasts.</summary>
    public static class UIKit
    {
        /// <summary>Canvas design resolution; the scaler keeps all of it visible (Expand match mode).</summary>
        public const float ReferenceWidth = 1920f, ReferenceHeight = 1080f;
        public static Canvas Canvas { get; private set; }
        public static RectTransform Root { get; private set; }
        public static RectTransform HudLayer { get; private set; }
        public static RectTransform PanelLayer { get; private set; }
        public static RectTransform ModalLayer { get; private set; }
        public static RectTransform ToastLayer { get; private set; }
        public static Font Font { get; private set; }
        public static event Action<bool> ModalStateChanged;
        static readonly List<Modal> modals = new List<Modal>();
        static Runner runner;
        public static bool AnyModalOpen => modals.Count > 0;
        public static Modal TopModal => modals.Count > 0 ? modals[modals.Count - 1] : null;

        public static void Init()
        {
            if (Canvas != null) return;
            Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas = go.GetComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 10;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            sc.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            Root = (RectTransform)go.transform;
            runner = go.AddComponent<Runner>();
            HudLayer = Layer("Hud"); PanelLayer = Layer("Panels"); ModalLayer = Layer("Modals"); ToastLayer = Layer("Toasts");
            ToastLayer.GetComponent<GraphicRaycaster>().enabled = false;
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                UnityEngine.Object.DontDestroyOnLoad(es);
            }
        }

        static RectTransform Layer(string name)
        {
            // GraphicRaycaster requires a Canvas, so create both up front (adding Canvas afterwards fails).
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            var rt = (RectTransform)go.transform;
            rt.SetParent(Root, false);
            Stretch(rt);
            go.GetComponent<Canvas>().overrideSorting = false;
            return rt;
        }

        // ----- geometry -----
        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        /// <summary>Anchor + pivot at the same normalized point, with position offset and size.</summary>
        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static readonly Vector2 TL = new Vector2(0, 1), TC = new Vector2(0.5f, 1), TR = new Vector2(1, 1),
            ML = new Vector2(0, 0.5f), MC = new Vector2(0.5f, 0.5f), MR = new Vector2(1, 0.5f),
            BL = new Vector2(0, 0), BC = new Vector2(0.5f, 0), BR = new Vector2(1, 0);

        // ----- primitives -----
        public static Image Panel(Transform parent, Color color, bool rounded = true, bool raycast = true)
        {
            var go = new GameObject("panel", typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = rounded ? SpriteFactory.RoundRect : SpriteFactory.Square;
            img.type = rounded ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color; img.raycastTarget = raycast;
            Stretch((RectTransform)go.transform);
            return img;
        }

        /// <summary>Cream window with dark border, like the game's panels.</summary>
        public static RectTransform Frame(Transform parent, Color fill, float border = 4)
        {
            var outer = Panel(parent, Palette.Border);
            var inner = Panel(outer.transform, fill);
            Stretch((RectTransform)inner.transform, border, border, border, border);
            inner.raycastTarget = false;
            return (RectTransform)outer.transform;
        }

        public static Text Label(Transform parent, string text, int size = 28, Color? color = null, TextAnchor anchor = TextAnchor.MiddleCenter, bool outline = true, bool bold = true)
        {
            var go = new GameObject("label", typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Font; t.fontSize = size; t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; t.alignment = anchor;
            t.color = color ?? Color.white; t.text = text; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            if (outline)
            {
                var o = go.AddComponent<Outline>(); o.effectColor = new Color(0, 0, 0, 0.85f); o.effectDistance = new Vector2(1.5f, -1.5f);
                var s = go.AddComponent<Shadow>(); s.effectColor = new Color(0, 0, 0, 0.5f); s.effectDistance = new Vector2(0, -2);
            }
            Stretch((RectTransform)go.transform);
            return t;
        }

        /// <summary>Dark text without outline for cream backgrounds.</summary>
        public static Text Dark(Transform parent, string text, int size = 26, TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = true)
            => Label(parent, text, size, Palette.Border, anchor, false, bold);

        public static Image Icon(Transform parent, string icon, float size = 48)
        {
            var go = new GameObject("icon:" + icon, typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = SpriteFactory.Icon(icon); img.preserveAspect = true; img.raycastTarget = false;
            Place((RectTransform)go.transform, MC, Vector2.zero, new Vector2(size, size));
            return img;
        }

        public static Image Sprite(Transform parent, Sprite sprite, float w, float h)
        {
            var go = new GameObject("sprite", typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite; img.preserveAspect = true; img.raycastTarget = false;
            Place((RectTransform)go.transform, MC, Vector2.zero, new Vector2(w, h));
            return img;
        }

        public static BtnView Btn(Transform parent, string text, Color color, Vector2 size, Action onClick, int fontSize = 28, string icon = null)
        {
            var go = new GameObject("btn:" + text, typeof(Image), typeof(Button), typeof(PressScale));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = SpriteFactory.Bevel; img.type = Image.Type.Sliced; img.color = color;
            var b = go.GetComponent<Button>();
            b.transition = Selectable.Transition.None;
            var v = new BtnView { Button = b, Image = img, Rt = rt, BaseColor = color };
            if (icon != null)
            {
                float iconSize = Mathf.Min(size.y - 16, 56);
                var ic = Icon(go.transform, icon, iconSize);
                if (!string.IsNullOrEmpty(text)) Place((RectTransform)ic.transform, ML, new Vector2(10, 0), new Vector2(iconSize, iconSize));
            }
            if (!string.IsNullOrEmpty(text))
            {
                v.Label = Label(go.transform, text, fontSize);
                if (icon != null) Stretch((RectTransform)v.Label.transform, Mathf.Min(size.y - 16, 56) + 18, 0, 8, 0);
            }
            b.onClick.AddListener(() => { Sfx.Play(SfxId.Tap); onClick?.Invoke(); });
            return v;
        }

        public static BtnView IconBtn(Transform parent, string icon, Color color, float size, Action onClick, bool round = false)
        {
            var v = Btn(parent, "", color, new Vector2(size, size), onClick, 28, icon);
            if (round) { v.Image.sprite = SpriteFactory.Circle; v.Image.type = Image.Type.Simple; }
            return v;
        }

        public static BarView Bar(Transform parent, Vector2 size, Color fill, Color? bg = null, int fontSize = 22)
        {
            var rt = Node("bar", parent);
            rt.sizeDelta = size;
            var bgImg = Panel(rt, bg ?? new Color(0.12f, 0.12f, 0.12f, 0.9f), true, false);
            var fillGo = new GameObject("fill", typeof(Image));
            fillGo.transform.SetParent(rt, false);
            var f = fillGo.GetComponent<Image>();
            f.sprite = SpriteFactory.RoundRect; f.type = Image.Type.Sliced; f.color = fill; f.raycastTarget = false;
            f.pixelsPerUnitMultiplier = 1.5f;
            var frt = (RectTransform)fillGo.transform;
            frt.anchorMin = Vector2.zero; frt.anchorMax = new Vector2(1, 1); frt.pivot = new Vector2(0, 0.5f);
            frt.offsetMin = new Vector2(3, 3); frt.offsetMax = new Vector2(-3, -3);
            var label = Label(rt, "", fontSize);
            return new BarView { Rt = rt, Fill = f, Label = label };
        }

        public static Text Badge(Transform parent, Vector2 anchor, Vector2 pos, float size = 30)
        {
            var rt = Node("badge", parent);
            Place(rt, anchor, pos, new Vector2(size, size));
            var bg = new GameObject("bg", typeof(Image)).GetComponent<Image>();
            bg.transform.SetParent(rt, false); bg.sprite = SpriteFactory.Circle; bg.color = Palette.Red; bg.raycastTarget = false;
            Stretch((RectTransform)bg.transform);
            var t = Label(rt, "1", (int)(size * 0.65f));
            return t;
        }

        public static TabsView Tabs(Transform parent, string[] names, Vector2 btnSize, Action<int> onSelect, int fontSize = 26)
        {
            var row = Node("tabs", parent);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 6; h.childForceExpandWidth = false; h.childForceExpandHeight = false; h.childAlignment = TextAnchor.MiddleLeft;
            var fit = row.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var tv = new TabsView { OnSelect = onSelect };
            for (int i = 0; i < names.Length; i++)
            {
                int idx = i;
                var b = Btn(row, names[i], Palette.GreyDark, btnSize, () => tv.Select(idx), fontSize);
                var le = b.Go.AddComponent<LayoutElement>(); le.preferredWidth = btnSize.x; le.preferredHeight = btnSize.y;
                tv.Buttons.Add(b);
            }
            return tv;
        }

        // ----- layout -----
        public static VerticalLayoutGroup VList(RectTransform rt, float spacing = 8, int pad = 0, TextAnchor align = TextAnchor.UpperCenter)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.padding = new RectOffset(pad, pad, pad, pad); v.childAlignment = align;
            v.childForceExpandWidth = false; v.childForceExpandHeight = false; v.childControlWidth = false; v.childControlHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup HList(RectTransform rt, float spacing = 8, int pad = 0, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing; h.padding = new RectOffset(pad, pad, pad, pad); h.childAlignment = align;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false; h.childControlWidth = false; h.childControlHeight = false;
            return h;
        }

        public static GridLayoutGroup Grid(RectTransform rt, Vector2 cell, float spacing = 12, int pad = 10)
        {
            var g = rt.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = cell; g.spacing = new Vector2(spacing, spacing); g.padding = new RectOffset(pad, pad, pad, pad);
            g.childAlignment = TextAnchor.UpperLeft;
            return g;
        }

        public static void FitContent(RectTransform rt, bool horizontal = false, bool vertical = true)
        {
            var f = rt.gameObject.AddComponent<ContentSizeFitter>();
            f.horizontalFit = horizontal ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            f.verticalFit = vertical ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
        }

        public static LayoutElement Size(RectTransform rt, float w, float h)
        {
            rt.sizeDelta = new Vector2(w, h);
            var le = rt.gameObject.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = w; le.preferredHeight = h; le.minHeight = h;
            return le;
        }

        /// <summary>Vertical scroll area filling the parent; returns the content container (top-anchored, auto height).</summary>
        public static RectTransform Scroll(Transform parent, out ScrollRect scroll, bool horizontal = false)
        {
            var view = Node("scroll", parent);
            Stretch(view);
            var img = view.gameObject.AddComponent<Image>(); img.color = new Color(0, 0, 0, 0.001f);
            view.gameObject.AddComponent<RectMask2D>();
            scroll = view.gameObject.AddComponent<ScrollRect>();
            var content = Node("content", view);
            if (horizontal)
            {
                content.anchorMin = new Vector2(0, 0); content.anchorMax = new Vector2(0, 1); content.pivot = new Vector2(0, 0.5f);
                content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            }
            else
            {
                content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
                content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            }
            scroll.content = content; scroll.viewport = view; scroll.horizontal = horizontal; scroll.vertical = !horizontal;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;
            return content;
        }

        // ----- modals -----
        public static Modal ShowModal(string title, Vector2 size, Action onClose = null, bool closeOnBackdrop = true, bool showClose = true)
        {
            var m = new Modal { OnClosed = onClose };
            m.Root = Node("modal:" + title, ModalLayer);
            Stretch(m.Root);
            var dim = Panel(m.Root, Palette.Dim, false);
            if (closeOnBackdrop)
            {
                var db = dim.gameObject.AddComponent<Button>(); db.transition = Selectable.Transition.None;
                db.onClick.AddListener(() => m.Close());
            }
            m.Window = Frame(m.Root, Palette.Cream);
            Place(m.Window, MC, Vector2.zero, size);
            var header = Node("header", m.Window);
            header.anchorMin = new Vector2(0, 1); header.anchorMax = new Vector2(1, 1); header.pivot = new Vector2(0.5f, 1);
            header.offsetMin = new Vector2(4, -64); header.offsetMax = new Vector2(-4, -4);
            var hbg = Panel(header, Palette.Header, true, false);
            m.Title = Label(header, title, 34);
            if (showClose)
            {
                var x = IconBtn(header, "close", Palette.Red, 52, () => m.Close(), true);
                Place(x.Rt, MR, new Vector2(-6, 0), new Vector2(52, 52));
            }
            m.Content = Node("content", m.Window);
            Stretch(m.Content, 10, 72, 10, 10);
            modals.Add(m);
            Sfx.Play(SfxId.Open);
            m.Root.localScale = Vector3.one * 0.9f;
            runner.StartCoroutine(Pop(m.Window));
            ModalStateChanged?.Invoke(true);
            return m;
        }

        static IEnumerator Pop(RectTransform rt)
        {
            float t = 0;
            while (t < 0.15f && rt != null)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.SmoothStep(0.85f, 1f, t / 0.15f);
                rt.localScale = new Vector3(s, s, 1);
                rt.parent.localScale = Vector3.one;
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }

        internal static void CloseModal(Modal m)
        {
            if (!modals.Remove(m)) return;
            if (m.Root != null) UnityEngine.Object.Destroy(m.Root.gameObject);
            Sfx.Play(SfxId.Close);
            m.OnClosed?.Invoke();
            ModalStateChanged?.Invoke(modals.Count > 0);
        }

        public static void CloseAllModals()
        {
            while (modals.Count > 0) modals[modals.Count - 1].Close();
        }

        // ----- toasts -----
        static readonly List<RectTransform> toasts = new List<RectTransform>();
        public static void Toast(string message, Color? color = null)
        {
            if (runner == null) return;
            var rt = Node("toast", ToastLayer);
            Place(rt, BC, new Vector2(0, 230), new Vector2(Mathf.Max(360, message.Length * 15 + 60), 60));
            var bg = Panel(rt, color ?? new Color(0.1f, 0.1f, 0.1f, 0.85f), true, false);
            Label(rt, message, 28);
            toasts.Add(rt);
            for (int i = 0; i < toasts.Count; i++)
            {
                int fromTop = toasts.Count - 1 - i;
                toasts[i].anchoredPosition = new Vector2(0, 230 + fromTop * 70);
            }
            runner.StartCoroutine(FadeToast(rt));
        }

        static IEnumerator FadeToast(RectTransform rt)
        {
            yield return new WaitForSecondsRealtime(2.2f);
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            float t = 0;
            while (t < 0.4f) { t += Time.unscaledDeltaTime; cg.alpha = 1 - t / 0.4f; yield return null; }
            toasts.Remove(rt);
            UnityEngine.Object.Destroy(rt.gameObject);
        }

        public static Coroutine Run(IEnumerator routine) => runner.StartCoroutine(routine);
        public static void Delay(float seconds, Action a) => runner.StartCoroutine(DelayCo(seconds, a));
        static IEnumerator DelayCo(float s, Action a) { yield return new WaitForSecondsRealtime(s); a?.Invoke(); }

        sealed class Runner : MonoBehaviour { }
    }

    public sealed class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public void OnPointerDown(PointerEventData e) { transform.localScale = new Vector3(0.95f, 0.95f, 1); }
        public void OnPointerUp(PointerEventData e) { transform.localScale = Vector3.one; }
        public void OnPointerExit(PointerEventData e) { transform.localScale = Vector3.one; }
    }
}
