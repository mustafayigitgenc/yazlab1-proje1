using System;
using System.Collections.Generic;
using Gumusdere.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gumusdere.UI
{
    /// <summary>
    /// Hikâye modu: önemli anlarda ekranın üstüne ve altına sinema şeritleri iner; üst şeritte bölüm adı,
    /// alt şeritte 1–2 cümlelik anlatım daktilo gibi yazılır. Oyun durmaz; Enter ile geçilir.
    /// Her bölüm bir koşula bağlıdır (örn. "köprü indi") ve bir kez gösterilir. Numara gösterilme sırasıyla verilir
    /// (oyuncu adımları farklı sırayla yapsa da Bölüm 1, 2, 3... diye gider). Başlık ekranından kapatılabilir.
    /// </summary>
    public sealed class StoryView : MonoBehaviour
    {
        public static bool Enabled { get; set; } = true;

        private const float BarHeight = 92f;
        private const float CharsPerSecond = 48f;
        private const float BarSpeed = 2.6f;

        private sealed class Beat
        {
            public string Chapter;
            public string Text;
            public Func<bool> Condition;
            public bool Fired;
        }

        private readonly List<Beat> beats = new();
        private readonly Queue<Beat> queue = new();
        private Func<bool> canShow = () => true;
        private Beat current;
        private int chapterNumber;
        private string currentTitle;
        private float shownFor;
        private float bars; // 0 = kapalı, 1 = tam açık
        private GUIStyle chapterStyle, textStyle, hintStyle;

        public bool IsShowing => current != null;

        public void Initialize(Func<bool> showAllowed)
        {
            canShow = showAllowed ?? (() => true);
        }

        /// <summary>Koşul ilk kez doğru olduğunda bu bölüm gösterilir. chapter: numarasız bölüm adı.</summary>
        public void Add(string chapter, string text, Func<bool> condition)
        {
            beats.Add(new Beat { Chapter = chapter, Text = text, Condition = condition });
        }

        private void Update()
        {
            foreach (Beat beat in beats)
            {
                if (beat.Fired) continue;
                bool ready;
                try { ready = beat.Condition(); } catch { ready = false; }
                if (!ready) continue;
                beat.Fired = true;
                if (Enabled) queue.Enqueue(beat);
            }

            bool allowed = canShow();
            if (current == null && queue.Count > 0 && allowed && bars <= 0.01f)
            {
                current = queue.Dequeue();
                shownFor = 0f;
                chapterNumber++;
                currentTitle = $"Bölüm {chapterNumber} · {current.Chapter}";
            }

            bars = Mathf.MoveTowards(bars, current != null && allowed ? 1f : 0f, Time.unscaledDeltaTime * BarSpeed);
            if (current == null || !allowed) return;

            shownFor += Time.deltaTime; // duraklatınca anlatım da bekler
            float typed = shownFor * CharsPerSecond;
            float total = current.Text.Length / CharsPerSecond + 3.6f;

            Keyboard kb = Keyboard.current;
            bool skip = kb != null && shownFor > 0.35f && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
            if (skip)
            {
                if (typed < current.Text.Length) shownFor = current.Text.Length / CharsPerSecond; // önce yazıyı tamamla
                else current = null;
            }
            else if (shownFor >= total)
            {
                current = null;
            }
        }

        private void OnGUI()
        {
            if (bars <= 0f) return;
            EnsureStyles();
            GUI.depth = -50;

            float scale = Mathf.Max(1f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            float h = Screen.height / scale;
            float k = Mathf.SmoothStep(0f, 1f, bars);
            float bar = BarHeight * k;

            Fill(new Rect(0f, 0f, w, bar), new Color(0f, 0f, 0f, 0.92f));
            Fill(new Rect(0f, h - bar, w, bar), new Color(0f, 0f, 0f, 0.92f));
            if (current == null || k < 0.95f) return;

            Fill(new Rect(w * 0.5f - 160f, bar - 20f, 320f, 1f), new Color(1f, 0.8f, 0.45f, 0.6f));
            GUI.Label(new Rect(0f, 8f, w, bar - 30f), currentTitle, chapterStyle);

            int visible = Mathf.Clamp(Mathf.FloorToInt(shownFor * CharsPerSecond), 0, current.Text.Length);
            GUI.Label(new Rect(w * 0.12f, h - bar + 8f, w * 0.76f, bar - 26f), current.Text.Substring(0, visible), textStyle);
            GUI.Label(new Rect(w - 220f, h - 24f, 200f, 20f), "Enter: geç", hintStyle);
        }

        private static void Fill(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        private void EnsureStyles()
        {
            if (chapterStyle != null) return;
            chapterStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerCenter };
            chapterStyle.normal.textColor = new Color(1f, 0.84f, 0.5f);
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            textStyle.normal.textColor = new Color(0.95f, 0.93f, 0.88f);
            hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleRight };
            hintStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
        }
    }
}
