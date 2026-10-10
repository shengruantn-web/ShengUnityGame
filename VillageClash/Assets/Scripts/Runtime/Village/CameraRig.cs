using UnityEngine;
using VillageClash.Core;

namespace VillageClash
{
    /// <summary>Orthographic camera with pan, zoom and clamping to the village plateau.</summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public const float MinZoom = 4f, MaxZoom = 13f;
        public Camera Cam { get; private set; }
        float targetZoom = 7f;
        public bool Locked; // disables user pan/zoom (e.g. while a modal is open)

        public static CameraRig Create()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.backgroundColor = Palette.GrassDark;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0, GameData.GridSize * 0.25f, -10f);
            var rig = cam.gameObject.GetComponent<CameraRig>() ?? cam.gameObject.AddComponent<CameraRig>();
            rig.Cam = cam;
            rig.targetZoom = cam.orthographicSize;
            return rig;
        }

        public float Zoom => Cam.orthographicSize;

        public Vector2 ScreenToWorld(Vector2 screen)
        {
            var w = Cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0));
            return new Vector2(w.x, w.y);
        }

        public Vector2 WorldToScreen(Vector3 world) => Cam.WorldToScreenPoint(world);

        public void Focus(Vector2 world, float zoom = -1)
        {
            if (zoom > 0) { targetZoom = Mathf.Clamp(zoom, MinZoom, MaxZoom); Cam.orthographicSize = targetZoom; }
            SetCenter(world);
        }

        public void DragBy(Vector2 worldDelta) => SetCenter((Vector2)Cam.transform.position - worldDelta);

        public void ZoomAt(float steps, Vector2 screenPivot)
        {
            if (Mathf.Approximately(steps, 0)) return;
            var before = ScreenToWorld(screenPivot);
            targetZoom = Mathf.Clamp(targetZoom * Mathf.Pow(0.88f, steps), MinZoom, MaxZoom);
            Cam.orthographicSize = targetZoom;
            var after = ScreenToWorld(screenPivot);
            DragBy(after - before);
        }

        void SetCenter(Vector2 c)
        {
            float z = Cam.orthographicSize;
            float t = Mathf.InverseLerp(MinZoom, MaxZoom, z);
            float hx = Mathf.Lerp(16f, 0f, t), hy = Mathf.Lerp(9f, 0f, t);
            float cy = GameData.GridSize * 0.25f;
            c.x = Mathf.Clamp(c.x, -hx, hx);
            c.y = Mathf.Clamp(c.y, cy - hy, cy + hy);
            Cam.transform.position = new Vector3(c.x, c.y, -10f);
        }

        void Update()
        {
            if (Locked) return;
            // keyboard pan for desktop
            float dx = (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
            float dy = (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow) ? 1 : 0);
            if (dx != 0 || dy != 0) DragBy(-new Vector2(dx, dy) * Time.unscaledDeltaTime * Zoom * 1.2f);
        }
    }
}
