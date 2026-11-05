using System;
using UnityEngine;

namespace Gumusdere.Audio
{
    /// <summary>
    /// Bütün ses efektleri kodla (sentezle) üretilir; projede hiç ses dosyası yoktur.
    /// Her ses, zamanın bir fonksiyonu olarak örnek örnek hesaplanır (sinüs, gürültü, zarf, frekans kayması).
    /// </summary>
    public sealed class SoundBank
    {
        private const int Rate = 44100;
        private readonly System.Random rng = new(42);

        public AudioClip[] Footsteps { get; }
        public AudioClip Coin { get; }
        public AudioClip Pop { get; }
        public AudioClip Splash { get; }
        public AudioClip Bite { get; }
        public AudioClip Thud { get; }
        public AudioClip Creak { get; }
        public AudioClip Tink { get; }
        public AudioClip Crumble { get; }
        public AudioClip Jingle { get; }
        public AudioClip Blip { get; }
        public AudioClip Fail { get; }
        public AudioClip Wind { get; }
        public AudioClip[] Birds { get; }

        public SoundBank()
        {
            Footsteps = new[] { Footstep("Adim1", 1f), Footstep("Adim2", 0.9f), Footstep("Adim3", 1.12f) };
            Coin = Make("Para", 0.42f, t =>
            {
                float f = t < 0.07f ? 1318.5f : 1975.5f;
                float env = t < 0.07f ? 1f : Mathf.Exp(-(t - 0.07f) * 9f);
                return (Sine(f, t) + 0.3f * Sine(f * 2f, t)) * 0.45f * env;
            });
            Pop = Make("Toplama", 0.12f, t => Sine(Lerp(520f, 980f, t / 0.12f), t, true) * 0.5f * Decay(t, 0.12f));
            Splash = Make("Su", 0.6f, t => (Noise() * 0.55f * Mathf.Exp(-t * 7f) + Sine(Lerp(300f, 120f, t / 0.6f), t, true) * 0.2f * Mathf.Exp(-t * 10f)) * Attack(t, 0.01f));
            Bite = Make("Balik_Vurdu", 0.22f, t => Sine(Lerp(780f, 240f, t / 0.22f), t, true) * 0.55f * Decay(t, 0.22f));
            Thud = Make("Gurultu", 0.7f, t => (Sine(58f, t) * 0.8f + LowNoise(0.08f) * 0.6f) * Mathf.Exp(-t * 6f) * Attack(t, 0.005f));
            Creak = Make("Kapi", 0.9f, t =>
            {
                float vibrato = 1f + 0.06f * Mathf.Sin(t * 38f);
                float body = Saw(130f * vibrato * Lerp(1f, 1.25f, t / 0.9f), t) * 0.18f;
                return (body + LowNoise(0.25f) * 0.12f) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.9f));
            });
            Tink = Make("Kazma", 0.35f, t => (Sine(2093f, t) * 0.5f + Sine(3140f, t) * 0.25f + Noise() * 0.25f * Mathf.Exp(-t * 60f)) * Mathf.Exp(-t * 14f));
            Crumble = Make("Gocuk", 1.3f, t =>
            {
                float grains = rng.NextDouble() < 0.02 ? 1f : 0.25f;
                return LowNoise(0.15f) * grains * 0.7f * Mathf.Exp(-t * 2.4f) * Attack(t, 0.02f);
            });
            Jingle = Make("Basari", 0.9f, t =>
            {
                float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
                int i = Mathf.Min(3, (int)(t / 0.13f));
                float local = t - i * 0.13f;
                return Triangle(notes[i], t) * 0.38f * Mathf.Exp(-local * (i == 3 ? 3.5f : 9f));
            });
            Blip = Make("Cevap", 0.09f, t => Sine(880f, t) * 0.25f * Decay(t, 0.09f));
            Fail = Make("Kacti", 0.4f, t => Triangle(Lerp(330f, 196f, t / 0.4f), t) * 0.3f * Decay(t, 0.4f));
            Wind = WindLoop();
            Birds = new[] { BirdCall("Kus1", 2, 2600f), BirdCall("Kus2", 3, 3200f), BirdCall("Kus3", 1, 2200f) };
        }

        // ------------------------------------------------------------------ Sesler

        /// <summary>Yumuşak çimen hışırtısı: tiz kısmı süzülmüş gürültü, yavaş giriş, kısa sönüm; "tok" vuruş yok.</summary>
        private AudioClip Footstep(string name, float pitch)
        {
            return Make(name, 0.18f, t => Rustle() * 0.32f * Attack(t, 0.02f) * Mathf.Exp(-t * 20f * pitch));
        }

        private float rustleLow;
        private float rustleSoft;

        private float Rustle()
        {
            float n = Noise();
            rustleLow += (n - rustleLow) * 0.2f;       // alçak frekansları çıkar (yüksek geçiren)
            float high = n - rustleLow;
            rustleSoft += (high - rustleSoft) * 0.35f; // en sert tizleri yumuşat
            return rustleSoft * 1.6f;
        }

        private AudioClip BirdCall(string name, int chirps, float baseFreq)
        {
            float chirp = 0.13f, gap = 0.07f;
            float length = chirps * (chirp + gap) + 0.05f;
            return Make(name, length, t =>
            {
                int i = (int)(t / (chirp + gap));
                float local = t - i * (chirp + gap);
                if (i >= chirps || local > chirp) return 0f;
                float k = local / chirp;
                float f = baseFreq * (1f + 0.45f * Mathf.Sin(k * Mathf.PI)) * (1f + 0.06f * i);
                return Sine(f, t, true) * 0.22f * Mathf.Sin(k * Mathf.PI);
            });
        }

        /// <summary>Kesintisiz dönen rüzgâr: süzülmüş gürültü, başı ve sonu çapraz geçişle birleştirilir.</summary>
        private AudioClip WindLoop()
        {
            const float seconds = 8f;
            int n = (int)(Rate * seconds);
            int fade = Rate / 2;
            var raw = new float[n + fade];
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < raw.Length; i++)
            {
                float t = i / (float)Rate;
                float cutoff = 0.012f + 0.01f * Mathf.Sin(t * 0.8f) + 0.006f * Mathf.Sin(t * 2.3f);
                lp += (Noise() - lp) * cutoff;
                lp2 += (lp - lp2) * 0.05f;
                raw[i] = lp2 * 6f;
            }

            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = raw[i];
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                data[i] = raw[i] * k + raw[n + i] * (1f - k);
            }

            AudioClip clip = AudioClip.Create("Ruzgar", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ------------------------------------------------------------------ Sentez yardımcıları

        private float phaseAcc;
        private float lowNoise;

        private AudioClip Make(string name, float seconds, Func<float, float> sample)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            phaseAcc = 0f;
            lowNoise = 0f;
            rustleLow = 0f;
            rustleSoft = 0f;
            for (int i = 0; i < n; i++)
            {
                data[i] = Mathf.Clamp(sample(i / (float)Rate), -1f, 1f);
            }
            // Tık sesi olmasın diye son 5 ms'yi sıfıra indir
            int tail = Mathf.Min(n, Rate / 200);
            for (int i = 0; i < tail; i++) data[n - 1 - i] *= i / (float)tail;

            AudioClip clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Sine(float freq, float t) => Mathf.Sin(2f * Mathf.PI * freq * t);

        /// <summary>Frekansı zamanla değişen (kayan) sinüs: fazı biriktirerek hesaplanır.</summary>
        private float Sine(float freq, float t, bool sweeping)
        {
            phaseAcc += 2f * Mathf.PI * freq / Rate;
            return Mathf.Sin(phaseAcc);
        }

        private static float Saw(float freq, float t) => 2f * (t * freq - Mathf.Floor(t * freq + 0.5f));
        private static float Triangle(float freq, float t) => 2f * Mathf.Abs(Saw(freq, t)) - 1f;
        private float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);

        private float LowNoise(float amount)
        {
            lowNoise += (Noise() - lowNoise) * amount;
            return lowNoise * 2f;
        }

        private static float Decay(float t, float length) => Mathf.Pow(1f - Mathf.Clamp01(t / length), 2f);
        private static float Attack(float t, float length) => Mathf.Clamp01(t / length);
        private static float Lerp(float a, float b, float k) => Mathf.Lerp(a, b, k);
    }
}
