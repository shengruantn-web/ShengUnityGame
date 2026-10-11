using System;
using UnityEngine;
using VillageClash.Core;

namespace VillageClash
{
    /// <summary>Runs a BattleSim in real time: steps it, routes deploy input, drives the renderer and reports the result.</summary>
    public sealed class BattleController : MonoBehaviour
    {
        public BattleSim Sim { get; private set; }
        public BattleRenderer Renderer { get; private set; }
        public TroopType SelectedTroop { get; private set; }
        public bool Active => Sim != null;
        public bool Pressing { get; private set; }

        /// <summary>Raised once the sim has ended and the controller has applied the result to the game state.</summary>
        public event Action<BattleResult> Finished;
        public event Action Changed; // selection or counts changed (HUD refresh)

        GameRoot R => GameRoot.I;
        // HUD layout the battle camera keeps the base clear of; the HUD (UI assembly) sets these when it is created.
        float uiRefW = 1920f, uiRefH = 1080f, hudTop = 0f, hudBottom = 0f;

        /// <summary>Canvas reference size and the canvas-unit heights of the top and bottom HUD strips.</summary>
        public void SetHudInsets(float referenceWidth, float referenceHeight, float top, float bottom)
        {
            uiRefW = Mathf.Max(1, referenceWidth); uiRefH = Mathf.Max(1, referenceHeight);
            hudTop = Mathf.Max(0, top); hudBottom = Mathf.Max(0, bottom);
        }
        float endTimer; bool reported; float holdDeployCd;

        void Start()
        {
            R.Router.Tap += OnTap;
            R.Router.Held += OnHeld;
            R.Router.PressBegan += (c, w) => { if (Active) Pressing = true; };
            R.Router.Escape += () => { if (Active && !Sim.Started) Leave(); };
        }

        public void Begin(BattleSim sim)
        {
            if (Active) return;
            Sim = sim;
            reported = false; endTimer = 0;
            R.Labels.Clear();
            R.Village.ShowTraps = false;
            R.Village.Show(sim.Enemy.Village, false);
            FrameEnemyBase(sim);
            Renderer = new GameObject("BattleRenderer").AddComponent<BattleRenderer>();
            Renderer.transform.SetParent(transform, false);
            Renderer.Init(this);
            SelectedTroop = FirstAvailable();
            R.SetMode(GameMode.Battle);
            Sfx.Play(SfxId.Open);
            Changed?.Invoke();
        }

        /// <summary>Zoom and center so the whole enemy base is visible between the top HUD panels and the troop bar.</summary>
        void FrameEnemyBase(BattleSim sim)
        {
            float w = Mathf.Max(1, Screen.width), h = Mathf.Max(1, Screen.height);
            float uiScale = Mathf.Min(w / uiRefW, h / uiRefH); // CanvasScaler Expand: smaller ratio wins
            float topFrac = hudTop * uiScale / h, bottomFrac = hudBottom * uiScale / h;
            BattleFraming.Fit(sim.Enemy.Village.Buildings, w / h, topFrac, bottomFrac, CameraRig.MinZoom, CameraRig.MaxZoom,
                out var cx, out var cy, out var zoom);
            R.Cam.Focus(new Vector2(cx, cy), zoom);
        }

        TroopType FirstAvailable()
        {
            for (int i = 0; i < Sim.Remaining.Length; i++) if (Sim.Remaining[i] > 0) return (TroopType)i;
            return SelectedTroop;
        }

        public void Select(TroopType t)
        {
            if (!Active) return;
            SelectedTroop = t;
            Sfx.Play(SfxId.Tap);
            Changed?.Invoke();
        }

        void OnTap(Int2 cell, Vector2 world) { if (Active) Deploy(world); }

        void OnHeld(Int2 cell, Vector2 world, float seconds)
        {
            if (!Active) return;
            holdDeployCd -= Time.unscaledDeltaTime;
            if (holdDeployCd <= 0) { Deploy(world, true); holdDeployCd = 0.18f; }
        }

        void Deploy(Vector2 world, bool quiet = false)
        {
            if (Sim.Ended) return;
            IsoMath.WorldToCell(world.x, world.y, out float cx, out float cy);
            var cell = new Int2((int)Mathf.Floor(cx), (int)Mathf.Floor(cy));
            if (Sim.Remaining[(int)SelectedTroop] <= 0)
            {
                var next = FirstAvailable();
                if (Sim.Remaining[(int)next] <= 0) { if (!quiet) { GameEvents.RaiseToast("No troops left to deploy!"); Sfx.Play(SfxId.Error); } return; }
                SelectedTroop = next;
            }
            if (!Sim.CanDeploy(cell))
            {
                if (!quiet) { Renderer.FlashNoDeploy(); Sfx.Play(SfxId.Error); }
                return;
            }
            if (Sim.TryDeploy(SelectedTroop, cx, cy))
            {
                Sfx.Play(SfxId.Deploy, 0.8f);
                if (Sim.Remaining[(int)SelectedTroop] == 0) SelectedTroop = FirstAvailable();
                Changed?.Invoke();
            }
        }

        /// <summary>End the battle early. Before the first deploy this just leaves; afterwards it surrenders.</summary>
        public void EndBattle()
        {
            if (!Active) return;
            if (!Sim.Started) { Leave(); return; }
            if (!Sim.Ended) Sim.Surrender();
        }

        /// <summary>Leave without fighting: battle counts as surrendered with 0% (match cost already paid).</summary>
        void Leave()
        {
            if (!Active) return;
            Sim.Surrender();
            Report();
        }

        void Update()
        {
            if (!Active) return;
            if (!UnityEngine.Input.GetMouseButton(0)) Pressing = false;
            if (!Sim.Ended) Sim.Step(Mathf.Min(Time.unscaledDeltaTime, 0.25f));
            // consume events
            var ev = Sim.Events;
            for (int i = 0; i < ev.Count; i++) Renderer.OnEvent(ev[i]);
            ev.Clear();
            if (Sim.Ended && !reported)
            {
                endTimer += Time.unscaledDeltaTime;
                if (endTimer >= 1.2f) Report();
            }
        }

        void Report()
        {
            if (reported) return;
            reported = true;
            var result = R.Controller.FinishBattle();
            if (result != null) Sfx.Play(result.Won ? SfxId.Win : SfxId.Lose);
            Finished?.Invoke(result);
        }

        /// <summary>Tear down the battle scene and return to the home village.</summary>
        public void ReturnHome()
        {
            if (!Active) return;
            if (!reported) Report();
            Sim = null;
            if (Renderer != null) Destroy(Renderer.gameObject);
            Renderer = null;
            R.Labels.Clear();
            R.Village.ShowTraps = true;
            R.ShowHomeVillage();
            R.SetMode(GameMode.Village);
            Sfx.Play(SfxId.Close);
        }
    }
}
