using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

namespace BreachPoint
{
    /// <summary>Controller rumble and DualSense/DualShock light bars.</summary>
    public sealed class Haptics : MonoBehaviour
    {
        static Haptics _instance;
        /// <summary>Main-menu option. Light bars still follow team colours when rumble is off.</summary>
        public static bool Enabled = true;
        readonly Dictionary<Gamepad, float> _until = new Dictionary<Gamepad, float>();
        readonly List<Gamepad> _expired = new List<Gamepad>();

        static Haptics Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("Haptics");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<Haptics>();
                }
                return _instance;
            }
        }

        public static void Pulse(PlayerControls c, float low, float high, float seconds)
        {
            if (!Enabled || c == null || c.Pad == null || !c.Pad.added) return;
            c.Pad.SetMotorSpeeds(low, high);
            Instance._until[c.Pad] = Time.unscaledTime + seconds;
        }

        public static void LightBar(PlayerControls c, Color color)
        {
            if (c != null && c.Pad is DualShockGamepad ds && ds.added) ds.SetLightBarColor(color);
        }

        public static void StopAll()
        {
            foreach (var g in Gamepad.all) g.ResetHaptics();
            if (_instance != null) _instance._until.Clear();
        }

        void Update()
        {
            _expired.Clear();
            foreach (var kv in _until) if (Time.unscaledTime >= kv.Value) _expired.Add(kv.Key);
            foreach (var g in _expired)
            {
                _until.Remove(g);
                if (g.added) g.SetMotorSpeeds(0f, 0f);
            }
        }

        void OnDisable() => StopAll();
    }

    /// <summary>Synthesised sound effects: no audio files needed for the first test stage.</summary>
    public static class Sfx
    {
        public static bool Muted;
        static AudioSource _src;
        static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();
        const int Rate = 44100;

        static AudioSource Source
        {
            get
            {
                if (_src == null)
                {
                    var go = new GameObject("Sfx");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    _src = go.AddComponent<AudioSource>();
                    _src.playOnAwake = false;
                }
                return _src;
            }
        }

        public static void Play(string id, float volume = 1f)
        {
            if (Muted) return;
            var clip = Get(id);
            if (clip != null) Source.PlayOneShot(clip, volume);
        }

        static AudioClip Get(string id)
        {
            if (Clips.TryGetValue(id, out var c)) return c;
            c = id switch
            {
                "fire0" => Tone(id, 0.22f, (600, 950, 0)),
                "fire1" => Tone(id, 0.14f, (1100, 320, 1)),
                "fire2" => Tone(id, 0.2f, (500, 260, 3), noise: 0.45f),
                "block" => Tone(id, 0.22f, (520, 1040, 2), (1040, 1560, 0)),
                "wrong" => Tone(id, 0.3f, (190, 150, 1)),
                "hit" => Tone(id, 0.45f, (140, 60, 3), noise: 0.5f),
                "stun" => Tone(id, 0.35f, (900, 200, 2)),
                "boost" => Tone(id, 0.25f, (200, 600, 3), noise: 0.3f),
                "switch" => Tone(id, 0.06f, (1400, 1400, 0)),
                "confirm" => Seq(id, 0.09f, 660, 990),
                "join" => Seq(id, 0.08f, 520, 780, 1040),
                "count" => Tone(id, 0.12f, (880, 880, 2)),
                "go" => Tone(id, 0.4f, (1320, 1320, 2), (660, 660, 0)),
                "whistle" => Tone(id, 0.5f, (1800, 1500, 0)),
                "swap" => Tone(id, 0.6f, (250, 1200, 3), noise: 0.15f),
                "win" => Seq(id, 0.14f, 523, 659, 784, 1047),
                "pause" => Tone(id, 0.12f, (700, 500, 2)),
                _ => null,
            };
            Clips[id] = c;
            return c;
        }

        // wave: 0 sine, 1 square, 2 triangle, 3 saw
        static float Wave(int wave, double phase)
        {
            double x = phase / (2 * Math.PI);
            x -= Math.Floor(x);
            return wave switch
            {
                0 => (float)Math.Sin(phase),
                1 => x < 0.5 ? 0.6f : -0.6f,
                2 => (float)(4 * Math.Abs(x - 0.5) - 1),
                _ => (float)(2 * x - 1) * 0.7f,
            };
        }

        static AudioClip Tone(string id, float dur, (float f0, float f1, int wave) a, (float f0, float f1, int wave)? b = null, float noise = 0f)
        {
            int n = Mathf.Max(1, (int)(dur * Rate));
            var data = new float[n];
            var rnd = new System.Random(id.GetHashCode());
            double pa = 0, pb = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                pa += 2 * Math.PI * Mathf.Lerp(a.f0, a.f1, t) / Rate;
                float s = Wave(a.wave, pa);
                if (b.HasValue)
                {
                    pb += 2 * Math.PI * Mathf.Lerp(b.Value.f0, b.Value.f1, t) / Rate;
                    s = 0.6f * s + 0.4f * Wave(b.Value.wave, pb);
                }
                if (noise > 0) s = s * (1 - noise) + noise * (float)(rnd.NextDouble() * 2 - 1);
                float env = Mathf.Min(1f, i / (Rate * 0.004f)) * (1 - t) * (1 - t);
                data[i] = s * env * 0.45f;
            }
            var clip = AudioClip.Create(id, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Seq(string id, float step, params float[] notes)
        {
            int per = (int)(step * Rate), tail = (int)(0.25f * Rate);
            int n = per * notes.Length + tail;
            var data = new float[n];
            for (int k = 0; k < notes.Length; k++)
            {
                double ph = 0;
                int start = k * per, len = per + tail;
                for (int i = 0; i < len && start + i < n; i++)
                {
                    ph += 2 * Math.PI * notes[k] / Rate;
                    float t = i / (float)len;
                    float env = Mathf.Min(1f, i / (Rate * 0.004f)) * (1 - t) * (1 - t);
                    data[start + i] += Wave(2, ph) * env * 0.35f;
                }
            }
            var clip = AudioClip.Create(id, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
