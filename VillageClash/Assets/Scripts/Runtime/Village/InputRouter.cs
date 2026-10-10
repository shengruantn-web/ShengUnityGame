using System;
using UnityEngine;
using UnityEngine.EventSystems;
using VillageClash.Core;

namespace VillageClash
{
    /// <summary>Translates pointer gestures into taps, captured drags, holds and camera movement.</summary>
    public sealed class InputRouter : MonoBehaviour
    {
        public event Action<Int2, Vector2> Tap;
        public event Action<Int2, Vector2> PressBegan;
        public event Action<Int2, Vector2> PressMoved;
        public event Action<Int2, Vector2> PressEnded;
        public event Action<Int2, Vector2, float> Held;
        public event Action Escape;

        /// <summary>A consumer can capture an initial press or a later long-press.</summary>
        public bool CaptureDrag;
        public bool Enabled = true;

        bool down, overUi, dragging;
        Vector2 downScreen, lastScreen;
        float downTime;
        const float DragThreshold = 12f;
        const float HoldTime = 0.35f;
        CameraRig Cam => GameRoot.I.Cam;

        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape)) Escape?.Invoke();
            if (!Enabled) { CancelPointer(); return; }
            var screen = (Vector2)UnityEngine.Input.mousePosition;
            bool uiHit = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            float scroll = UnityEngine.Input.mouseScrollDelta.y;
            if (scroll != 0 && !uiHit && !Cam.Locked) Cam.ZoomAt(scroll, screen);

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                downTime = Time.unscaledTime;
                BeginPointer(screen, uiHit);
            }
            else if (down && UnityEngine.Input.GetMouseButton(0)) MovePointer(screen, Time.unscaledTime - downTime);
            else if (down && UnityEngine.Input.GetMouseButtonUp(0)) EndPointer(screen);
        }

        // These same gesture entry points are exercised by the standalone verification runner.
        public void BeginPointer(Vector2 screen, bool isOverUi)
        {
            if (!Enabled) return;
            down = true; overUi = isOverUi; dragging = false; CaptureDrag = false;
            downScreen = lastScreen = screen;
            if (!overUi)
            {
                var w = Cam.ScreenToWorld(screen);
                PressBegan?.Invoke(IsoMath.WorldToCellInt(w.x, w.y), w);
            }
        }

        public void MovePointer(Vector2 screen, float heldSeconds)
        {
            if (!Enabled) { CancelPointer(); return; }
            if (!down || overUi) return;
            var w = Cam.ScreenToWorld(screen);
            var cell = IsoMath.WorldToCellInt(w.x, w.y);
            if (CaptureDrag)
            {
                if ((screen - lastScreen).sqrMagnitude > 0.5f) PressMoved?.Invoke(cell, w);
            }
            else
            {
                if (!dragging && (screen - downScreen).magnitude > DragThreshold) dragging = true;
                if (dragging && !Cam.Locked)
                {
                    var prev = Cam.ScreenToWorld(lastScreen);
                    Cam.DragBy(w - prev);
                }
                if (!dragging && heldSeconds >= HoldTime) Held?.Invoke(cell, w, heldSeconds);
            }
            lastScreen = screen;
        }

        public void EndPointer(Vector2 screen)
        {
            if (!Enabled || !down || overUi) { CancelPointer(); return; }
            var w = Cam.ScreenToWorld(screen);
            var cell = IsoMath.WorldToCellInt(w.x, w.y);
            if (CaptureDrag) PressEnded?.Invoke(cell, w);
            else if (!dragging) Tap?.Invoke(cell, w);
            CancelPointer();
        }

        void CancelPointer() { down = false; dragging = false; CaptureDrag = false; }
    }
}
