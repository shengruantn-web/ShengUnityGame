using System;
using System.Collections.Generic;
using UnityEngine;
using VillageClash.Core;

namespace VillageClash
{
    /// <summary>Placing a new building or moving an existing one: ghost sprite, green/red footprint, drag, confirm/cancel bubbles.</summary>
    public sealed class BuildModeController : MonoBehaviour
    {
        public bool Active { get; private set; }
        public event Action Ended;

        BuildingType type;
        int level = 1;
        int moveId = -1;
        Int2 origin;
        int size;
        bool valid;
        SpriteRenderer ghost;
        readonly List<SpriteRenderer> tiles = new List<SpriteRenderer>();
        Transform holder;
        bool dragging; Int2 grabOffset;

        GameRoot R => GameRoot.I;

        void Start()
        {
            R.Router.PressBegan += OnPressBegan;
            R.Router.PressMoved += OnPressMoved;
            R.Router.PressEnded += OnPressEnded;
            R.Router.Tap += OnTap;
            R.Router.Escape += () => { if (Active) Cancel(); };
        }

        public void BeginPlace(BuildingType t)
        {
            if (Active) Cancel();
            type = t; level = 1; moveId = -1;
            size = GameData.Building(t).Size;
            Begin(CenterCell());
        }

        public void BeginMove(int id)
        {
            var b = R.Controller.State.Village.Find(id);
            if (b == null) return;
            if (Active) Cancel();
            type = b.Type; level = b.Level; moveId = id; size = b.Size;
            R.Village.HiddenId = id;
            Begin(b.Origin);
        }

        Int2 CenterCell()
        {
            var c = R.Cam.ScreenToWorld(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            var cell = IsoMath.WorldToCellInt(c.x, c.y);
            return Clamp(new Int2(cell.X - size / 2, cell.Y - size / 2));
        }

        Int2 Clamp(Int2 o)
        {
            int max = GameData.GridSize - size;
            return new Int2(Mathf.Clamp(o.X, 0, max), Mathf.Clamp(o.Y, 0, max));
        }

        void Begin(Int2 at)
        {
            Active = true;
            holder = new GameObject("BuildMode").transform;
            holder.SetParent(transform, false);
            ghost = new GameObject("ghost").AddComponent<SpriteRenderer>();
            ghost.transform.SetParent(holder, false);
            ghost.sprite = SpriteFactory.Building(type, level, true);
            for (int i = 0; i < size * size; i++)
            {
                var t = new GameObject("tile").AddComponent<SpriteRenderer>();
                t.transform.SetParent(holder, false);
                t.sprite = SpriteFactory.Tile;
                t.sortingOrder = -90000;
                tiles.Add(t);
            }
            R.SetMode(GameMode.Build);
            SetOrigin(at);
            Sfx.Play(SfxId.Open);
        }

        void SetOrigin(Int2 o)
        {
            origin = Clamp(o);
            valid = R.Controller.CanPlace(type, origin, moveId);
            ghost.transform.position = VillageRenderer.FootprintWorld(origin, size);
            ghost.sortingOrder = VillageRenderer.SortOrder(origin.X + size * 0.5f, origin.Y + size * 0.5f) + 1;
            ghost.color = valid ? Color.white : new Color(1f, 0.6f, 0.6f, 1f);
            var c = valid ? Palette.Alpha(Palette.Green, 0.55f) : Palette.Alpha(Palette.Red, 0.55f);
            int k = 0;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var t = tiles[k++];
                    t.transform.position = VillageRenderer.CellWorld(origin.X + x + 0.5f, origin.Y + y + 0.5f) + new Vector3(0, 0, 0.01f);
                    t.color = c;
                }
        }

        bool Inside(Int2 cell) => cell.X >= origin.X && cell.Y >= origin.Y && cell.X < origin.X + size && cell.Y < origin.Y + size;

        void OnPressBegan(Int2 cell, Vector2 world)
        {
            if (!Active) return;
            if (Inside(cell)) { dragging = true; grabOffset = new Int2(cell.X - origin.X, cell.Y - origin.Y); R.Router.CaptureDrag = true; }
        }

        void OnPressMoved(Int2 cell, Vector2 world)
        {
            if (!Active || !dragging) return;
            SetOrigin(new Int2(cell.X - grabOffset.X, cell.Y - grabOffset.Y));
        }

        void OnPressEnded(Int2 cell, Vector2 world) { dragging = false; }

        void OnTap(Int2 cell, Vector2 world)
        {
            if (!Active) return;
            if (!Inside(cell)) SetOrigin(new Int2(cell.X - size / 2, cell.Y - size / 2));
        }

        void Update()
        {
            if (!Active) return;
            var top = VillageRenderer.FootprintWorld(origin, size) + new Vector3(0, size * 0.25f + 0.9f, 0);
            R.Labels.Bubble("bm_ok", top + new Vector3(-0.55f, 0, 0), SpriteFactory.Icon("check"), Confirm, 64);
            R.Labels.Bubble("bm_no", top + new Vector3(0.55f, 0, 0), SpriteFactory.Icon("close"), Cancel, 64);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter)) Confirm();
            if (UnityEngine.Input.GetKey(KeyCode.LeftArrow) && UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow)) SetOrigin(new Int2(origin.X - 1, origin.Y));
        }

        public void Confirm()
        {
            if (!Active) return;
            if (!valid) { GameEvents.RaiseToast(Msg.CantPlace); Sfx.Play(SfxId.Error); return; }
            bool ok = moveId >= 0 ? R.Controller.MoveBuilding(moveId, origin) : R.Controller.ConfirmPlace(type, origin);
            if (!ok) { Sfx.Play(SfxId.Error); return; }
            Sfx.Play(SfxId.Place);
            End();
        }

        public void Cancel()
        {
            if (!Active) return;
            Sfx.Play(SfxId.Close);
            End();
        }

        void End()
        {
            Active = false; dragging = false;
            R.Village.HiddenId = -1;
            if (holder != null) Destroy(holder.gameObject);
            tiles.Clear(); ghost = null; holder = null;
            R.SetMode(GameMode.Village);
            Ended?.Invoke();
        }
    }
}
