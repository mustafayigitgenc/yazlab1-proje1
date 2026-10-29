using System.Collections.Generic;
using Gumusdere.Core;
using UnityEngine;

namespace Gumusdere.UI
{
    /// <summary>
    /// Sol altta sohbet kutusu: son mesajlar, konuşanın adı renkli olarak gösterilir.
    /// Eski mesajlar bir süre sonra soluklaşır.
    /// </summary>
    public sealed class MessageLog : MonoBehaviour, IMessageLog
    {
        private const int MaxMessages = 7;
        private const float FadeAfter = 12f;
        private const float BoxWidth = 520f;

        private readonly struct Entry
        {
            public readonly string Speaker;
            public readonly string Text;
            public readonly Color Color;
            public readonly float Time;

            public Entry(string speaker, string text, Color color, float time)
            {
                Speaker = speaker;
                Text = text;
                Color = color;
                Time = time;
            }
        }

        private readonly List<Entry> entries = new();
        private GUIStyle lineStyle;
        private GUIStyle panelStyle;

        /// <summary>Diyalog penceresi açıkken gizlenir.</summary>
        public bool Visible { get; set; } = true;

        public void Add(string speaker, string text, Color color)
        {
            entries.Add(new Entry(speaker, text, color, Time.time));
            if (entries.Count > MaxMessages) entries.RemoveAt(0);
        }

        private void OnGUI()
        {
            if (entries.Count == 0 || !Visible) return;
            EnsureStyles();

            float scale = Mathf.Max(1f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float screenHeight = Screen.height / scale;

            // Satır yüksekliklerini hesapla (uzun mesajlar alt satıra kayar).
            float contentWidth = BoxWidth - 24f;
            float total = 0f;
            var heights = new float[entries.Count];
            var texts = new string[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                string hex = ColorUtility.ToHtmlStringRGB(e.Color);
                texts[i] = string.IsNullOrEmpty(e.Speaker)
                    ? $"<i>{e.Text}</i>"
                    : $"<b><color=#{hex}>{e.Speaker}:</color></b> {e.Text}";
                heights[i] = lineStyle.CalcHeight(new GUIContent(texts[i]), contentWidth);
                total += heights[i];
            }

            var panel = new Rect(12f, screenHeight - total - 36f, BoxWidth, total + 20f);
            GUI.Box(panel, GUIContent.none, panelStyle);

            float y = panel.y + 10f;
            Color previous = GUI.color;
            for (int i = 0; i < entries.Count; i++)
            {
                float age = Time.time - entries[i].Time;
                GUI.color = new Color(1f, 1f, 1f, age > FadeAfter ? 0.55f : 1f);
                GUI.Label(new Rect(panel.x + 12f, y, contentWidth, heights[i]), texts[i], lineStyle);
                y += heights[i];
            }
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (lineStyle != null) return;

            lineStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                wordWrap = true,
                fontSize = 15,
                padding = new RectOffset(0, 0, 2, 2)
            };
            lineStyle.normal.textColor = Color.white;

            var background = new Texture2D(1, 1);
            background.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.5f));
            background.Apply();
            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = background;
        }
    }
}
