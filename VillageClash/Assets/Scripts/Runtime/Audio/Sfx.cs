using System.Collections.Generic;
using UnityEngine;

namespace VillageClash
{
    public enum SfxId { Tap, Open, Close, Collect, Build, Place, Error, Shot, Hit, Destroy, Deploy, Explosion, Win, Lose, Star, Coin, Upgrade }

    /// <summary>Procedurally synthesised sound effects and a soft ambient music loop.</summary>
    public static class Sfx
    {
        const int Rate = 22050;
        static AudioSource sfxSource, musicSource;
        static readonly Dictionary<SfxId, AudioClip> clips = new Dictionary<SfxId, AudioClip>();
        static bool sfxOn = true, musicOn = true;

        public static void Init(Transform parent)
        {
            if (sfxSource != null) return;
            var go = new GameObject("Audio");
            go.transform.SetParent(parent, false);
            sfxSource = go.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            musicSource = go.AddComponent<AudioSource>();
            musicSource.loop = true; musicSource.volume = 0.35f; musicSource.playOnAwake = false;
            musicSource.clip = MakeMusic();
            if (musicOn) musicSource.Play();
        }

        public static void SetMusic(bool on)
        {
            musicOn = on;
            if (musicSource == null) return;
            if (on && !musicSource.isPlaying) musicSource.Play();
            if (!on) musicSource.Stop();
        }

        public static void SetSfx(bool on) => sfxOn = on;

        public static void Play(SfxId id, float volume = 1f)
        {
            if (!sfxOn || sfxSource == null) return;
            if (!clips.TryGetValue(id, out var clip)) { clip = Make(id); clips[id] = clip; }
            sfxSource.PlayOneShot(clip, volume);
        }

        static AudioClip Make(SfxId id)
        {
            switch (id)
            {
                case SfxId.Tap: return Tone(880, 0.05f, 0.4f, Wave.Sine);
                case SfxId.Open: return Sweep(400, 900, 0.12f, 0.5f);
                case SfxId.Close: return Sweep(900, 400, 0.12f, 0.5f);
                case SfxId.Collect: return Chord(new[] { 660f, 880f, 1320f }, 0.18f, 0.5f);
                case SfxId.Coin: return Chord(new[] { 1320f, 1760f }, 0.1f, 0.4f);
                case SfxId.Build: return Noise(0.12f, 0.5f, 1800);
                case SfxId.Place: return Tone(330, 0.1f, 0.6f, Wave.Square);
                case SfxId.Upgrade: return Sweep(300, 1200, 0.35f, 0.5f);
                case SfxId.Error: return Chord(new[] { 220f, 233f }, 0.25f, 0.5f, Wave.Square);
                case SfxId.Shot: return Noise(0.08f, 0.6f, 600);
                case SfxId.Hit: return Tone(180, 0.06f, 0.5f, Wave.Square);
                case SfxId.Destroy: return Noise(0.45f, 0.8f, 300);
                case SfxId.Explosion: return Noise(0.6f, 1f, 200);
                case SfxId.Deploy: return Sweep(500, 700, 0.08f, 0.4f);
                case SfxId.Star: return Chord(new[] { 880f, 1108f, 1320f }, 0.3f, 0.6f);
                case SfxId.Win: return Melody(new[] { 523f, 659f, 784f, 1046f }, 0.14f, 0.6f);
                case SfxId.Lose: return Melody(new[] { 392f, 349f, 311f, 261f }, 0.2f, 0.6f, Wave.Square);
            }
            return Tone(440, 0.1f, 0.5f, Wave.Sine);
        }

        enum Wave { Sine, Square, Tri }

        static float Osc(Wave w, float phase)
        {
            float t = phase - Mathf.Floor(phase);
            switch (w)
            {
                case Wave.Square: return t < 0.5f ? 0.6f : -0.6f;
                case Wave.Tri: return 4f * Mathf.Abs(t - 0.5f) - 1f;
                default: return Mathf.Sin(t * Mathf.PI * 2f);
            }
        }

        static AudioClip FromSamples(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Env(float t, float dur) => Mathf.Clamp01(t / 0.005f) * Mathf.Pow(1f - t / dur, 1.5f);

        static AudioClip Tone(float freq, float dur, float vol, Wave w)
        {
            int n = (int)(Rate * dur);
            var d = new float[n];
            for (int i = 0; i < n; i++) { float t = i / (float)Rate; d[i] = Osc(w, t * freq) * Env(t, dur) * vol; }
            return FromSamples("tone", d);
        }

        static AudioClip Sweep(float f0, float f1, float dur, float vol)
        {
            int n = (int)(Rate * dur);
            var d = new float[n];
            float phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float f = Mathf.Lerp(f0, f1, t / dur);
                phase += f / Rate;
                d[i] = Osc(Wave.Sine, phase) * Env(t, dur) * vol;
            }
            return FromSamples("sweep", d);
        }

        static AudioClip Chord(float[] freqs, float dur, float vol, Wave w = Wave.Sine)
        {
            int n = (int)(Rate * dur);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate, s = 0;
                foreach (var f in freqs) s += Osc(w, t * f);
                d[i] = s / freqs.Length * Env(t, dur) * vol;
            }
            return FromSamples("chord", d);
        }

        static AudioClip Melody(float[] notes, float noteDur, float vol, Wave w = Wave.Sine)
        {
            int per = (int)(Rate * noteDur), n = per * notes.Length;
            var d = new float[n];
            for (int k = 0; k < notes.Length; k++)
                for (int i = 0; i < per; i++)
                {
                    float t = i / (float)Rate;
                    d[k * per + i] = Osc(w, t * notes[k]) * Env(t, noteDur) * vol;
                }
            return FromSamples("melody", d);
        }

        static AudioClip Noise(float dur, float vol, float cutoffHz)
        {
            int n = (int)(Rate * dur);
            var d = new float[n];
            var rng = new System.Random(7);
            float lp = 0, alpha = Mathf.Clamp01(cutoffHz / Rate * 6f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float white = (float)(rng.NextDouble() * 2 - 1);
                lp += (white - lp) * alpha;
                d[i] = lp * Env(t, dur) * vol * 1.8f;
            }
            return FromSamples("noise", d);
        }

        static AudioClip MakeMusic()
        {
            float dur = 16f;
            int n = (int)(Rate * dur);
            var d = new float[n];
            float[][] chords = { new[] { 196f, 246.9f, 293.7f }, new[] { 220f, 261.6f, 329.6f }, new[] { 174.6f, 220f, 261.6f }, new[] { 196f, 246.9f, 293.7f } };
            float[] lead = { 587.3f, 659.3f, 587.3f, 493.9f, 440f, 493.9f, 587.3f, 659.3f, 783.9f, 659.3f, 587.3f, 493.9f, 440f, 392f, 440f, 493.9f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                int bar = Mathf.Min(3, (int)(t / 4f));
                float s = 0;
                foreach (var f in chords[bar]) s += Mathf.Sin(t * f * Mathf.PI * 2f) * 0.12f;
                int beat = (int)(t / 1f) % lead.Length;
                float bt = t - Mathf.Floor(t);
                s += Osc(Wave.Tri, t * lead[beat]) * 0.10f * Mathf.Pow(1f - bt, 2f);
                // slow swell
                s *= 0.8f + 0.2f * Mathf.Sin(t * 0.5f);
                d[i] = s;
            }
            // fade ends for seamless loop
            int fade = Rate / 4;
            for (int i = 0; i < fade; i++) { float f = i / (float)fade; d[i] *= f; d[n - 1 - i] *= f; }
            return FromSamples("music", d);
        }
    }
}
