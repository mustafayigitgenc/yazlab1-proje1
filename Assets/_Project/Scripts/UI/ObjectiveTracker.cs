using System;
using System.Collections.Generic;
using Gumusdere.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gumusdere.UI
{
    /// <summary>
    /// Sağ üstte ana hikâyenin adımlarını gösteren hedef listesi. Her adımın "tamam mı" bilgisi oyunun gerçek
    /// durumundan okunur (köprü indi mi, kirişler taşındı mı...). Sıradaki adım vurgulanır ve altında kısa bir ipucu yazar.
    /// </summary>
    public sealed class ObjectiveTracker : MonoBehaviour
    {
        public sealed class Step
        {
            public Step(string title, Func<bool> done, Func<string> hint)
            {
                Title = title;
                Done = done;
                Hint = hint;
            }

            public string Title { get; }
            public Func<bool> Done { get; }
            public Func<string> Hint { get; }
        }

        private const float Width = 340f;

        private readonly List<Step> steps = new();
        private Func<bool> visible = () => true;
        private Func<string> footer = () => null;
        private GUIStyle titleStyle, doneStyle, currentStyle, futureStyle, hintStyle, footerStyle, panelStyle;

        public void Initialize(IEnumerable<Step> storySteps, Func<bool> showWhen, Func<string> footerLine)
        {
            steps.AddRange(storySteps);
            visible = showWhen ?? (() => true);
            footer = footerLine ?? (() => null);
        }

        /// <summary>J: bütün adımları göster / sadece sıradaki hedefi göster.</summary>
        public bool Expanded { get; private set; }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.jKey.wasPressedThisFrame && Hud.Visible && visible()) Expanded = !Expanded;
        }

        private void OnGUI()
        {
            if (!Hud.Visible || steps.Count == 0 || !visible()) return;
            EnsureStyles();

            float scale = Mathf.Max(1f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;

            int current = steps.FindIndex(s => !s.Done());
            int doneCount = 0;
            foreach (Step s in steps) if (s.Done()) doneCount++;
            string extra = footer();
            string hint = current >= 0 ? steps[current].Hint?.Invoke() : null;

            // Yükseklik içeriğe göre hesaplanır (ipucu birkaç satıra sarabilir).
            float inner = Width - 24f;
            float height = 16f + 24f;
            if (Expanded) height += steps.Count * 22f;
            else height += 24f;
            if (!string.IsNullOrEmpty(hint)) height += hintStyle.CalcHeight(new GUIContent(hint), inner) + 2f;
            if (!string.IsNullOrEmpty(extra)) height += 22f;

            GUILayout.BeginArea(new Rect(w - Width - 12f, 50f, Width, height), panelStyle);
            GUILayout.Label($"Hedefler  <size=12><color=#bbbbbb>{doneCount}/{steps.Count}  ·  J: {(Expanded ? "küçült" : "tümü")}</color></size>", titleStyle);

            if (Expanded)
            {
                for (int i = 0; i < steps.Count; i++) DrawRow(i, steps[i].Done(), i == current, i == current ? hint : null);
            }
            else if (current >= 0)
            {
                DrawRow(current, false, true, hint);
            }
            else
            {
                GUILayout.Label("Bütün hedefler tamam!", currentStyle);
            }

            if (!string.IsNullOrEmpty(extra)) GUILayout.Label(extra, footerStyle);
            GUILayout.EndArea();
        }

        private void DrawRow(int index, bool done, bool isCurrent, string hint)
        {
            Rect row = GUILayoutUtility.GetRect(Width - 24f, 22f);
            DrawBox(new Rect(row.x, row.y + 5f, 12f, 12f), done, isCurrent);
            GUI.Label(new Rect(row.x + 20f, row.y, row.width - 20f, row.height), steps[index].Title,
                done ? doneStyle : isCurrent ? currentStyle : futureStyle);
            if (!string.IsNullOrEmpty(hint)) GUILayout.Label(hint, hintStyle);
        }

        private static void DrawBox(Rect r, bool done, bool current)
        {
            Color old = GUI.color;
            GUI.color = done ? new Color(0.55f, 0.82f, 0.5f) : current ? new Color(1f, 0.82f, 0.4f) : new Color(1f, 1f, 1f, 0.35f);
            if (done)
            {
                GUI.DrawTexture(r, Texture2D.whiteTexture);
            }
            else
            {
                GUI.DrawTexture(new Rect(r.x, r.y, r.width, 2f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(r.x, r.yMax - 2f, r.width, 2f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(r.x, r.y, 2f, r.height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(r.xMax - 2f, r.y, 2f, r.height), Texture2D.whiteTexture);
            }
            GUI.color = old;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            panelStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(12, 12, 8, 8) };
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, richText = true, normal = { textColor = new Color(1f, 0.86f, 0.55f) } };
            doneStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = new Color(0.62f, 0.75f, 0.6f) } };
            currentStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            futureStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = new Color(0.75f, 0.75f, 0.75f) } };
            hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true, richText = true, padding = new RectOffset(20, 0, 0, 2), normal = { textColor = new Color(1f, 0.9f, 0.7f) } };
            footerStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true, normal = { textColor = new Color(0.8f, 0.8f, 0.8f) } };
        }
    }
}
