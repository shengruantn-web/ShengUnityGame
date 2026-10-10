using System;
using UnityEngine;
using UnityEngine.EventSystems;
using VillageClash.Core;

namespace VillageClash
{
    /// <summary>Translates mouse/touch into world taps, drags and holds. Camera pan happens here unless a consumer captures the drag.</summary>
    public sealed class InputRouter : MonoBehaviour
    {
        public event Action<Int2, Vector2> Tap;          // short press without drag (cell, world)
        public event Action<Int2, Vector2> PressBegan;   // pointer down on world (cell, world)
        public event Action<Int2, Vector2> PressMoved;   // while captured: pointer moved (cell, world)
        public event Action<Int2, Vector2> PressEnded;   // while captured: pointer up (cell, world)
        public event Action<Int2, Vector2, float> Held;  // pointer held still (cell, world, seconds)
        public event Action Escape;

        /// <summary>Set by a consumer during PressBegan to receive PressMoved/PressEnded instead of panning the camera.</summary>
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
            if (!Enabled) { down = false; return; }
            var screen = (Vector2)UnityEngine.Input.mousePosition;
            bool uiHit = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            float scroll = UnityEngine.Input.mouseScrollDelta.y;
            if (scroll != 0 && !uiHit && !Cam.Locked) Cam.ZoomAt(scroll, screen);

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                down = true; overUi = uiHit; dragging = false; CaptureDrag = false;
                downScreen = lastScreen = screen; downTime = Time.unscaledTime;
                if (!overUi)
                {
                    var w = Cam.ScreenToWorld(screen);
                    PressBegan?.Invoke(IsoMath.WorldToCellInt(w.x, w.y), w);
                }
            }
            else if (down && UnityEngine.Input.GetMouseButton(0))
            {
                if (overUi) return;
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
                        Cam.DragBy(Cam.ScreenToWorld(screen) - prev);
                    }
                    if (!dragging && Time.unscaledTime - downTime >= HoldTime) Held?.Invoke(cell, w, Time.unscaledTime - downTime);
                }
                lastScreen = screen;
            }
            else if (down && UnityEngine.Input.GetMouseButtonUp(0))
            {
                down = false;
                if (overUi) return;
                var w = Cam.ScreenToWorld(screen);
                var cell = IsoMath.WorldToCellInt(w.x, w.y);
                if (CaptureDrag) PressEnded?.Invoke(cell, w);
                else if (!dragging && Time.unscaledTime - downTime < HoldTime) Tap?.Invoke(cell, w);
                else if (!dragging) Tap?.Invoke(cell, w); // long still press also counts as tap when nobody consumed Held
                CaptureDrag = false;
            }
        }
    }
}
