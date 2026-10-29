using Gumusdere.Core;
using UnityEngine;

namespace Gumusdere.UI
{
    /// <summary>
    /// Karakterin başının üstünde kısa süre görünen konuşma balonu (ISpeaker uygulaması).
    /// Söylenen her şey ayrıca sol alttaki mesaj kutusuna da yazılır.
    /// </summary>
    public sealed class SpeechBubble : MonoBehaviour, ISpeaker
    {
        private const float MaxWidth = 320f;
        private const float PaddingX = 12f;
        private const float PaddingY = 8f;

        [SerializeField] private float headHeight = 2.3f;

        private string speakerName;
        private Color speakerColor = Color.white;
        private IMessageLog log;

        private string text;
        private float hideTime;
        private GUIStyle backgroundStyle;
        private GUIStyle textStyle;
        private GUIStyle measureStyle;

        public void Initialize(string displayName, Color nameColor, IMessageLog messageLog)
        {
            speakerName = displayName;
            speakerColor = nameColor;
            log = messageLog;
        }

        public void Say(string message, float seconds = 2.5f)
        {
            text = message;
            hideTime = Time.time + Mathf.Max(seconds, 1.5f + message.Length * 0.05f);
            log?.Add(speakerName ?? gameObject.name, message, speakerColor);
            Debug.Log($"[{gameObject.name}] \"{message}\"");
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(text) || Time.time > hideTime) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * headHeight);
            if (screen.z < 0f) return;

            EnsureStyles();
            float scale = Mathf.Max(1f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            // Metin genişliği: tek satır ölçümü, en fazla MaxWidth. Yükseklik: o genişlikte
            // alt satıra kaydırılmış metnin yüksekliği. Kutu, metnin etrafına dolgu ekler.
            var content = new GUIContent(text);
            float textWidth = Mathf.Min(measureStyle.CalcSize(content).x + 4f, MaxWidth);
            float textHeight = textStyle.CalcHeight(content, textWidth);
            float boxWidth = textWidth + PaddingX * 2f;
            float boxHeight = textHeight + PaddingY * 2f;

            float x = screen.x / scale - boxWidth * 0.5f;
            float y = (Screen.height - screen.y) / scale - boxHeight;

            // Ekran kenarından taşmasın.
            x = Mathf.Clamp(x, 8f, Screen.width / scale - boxWidth - 8f);
            y = Mathf.Max(8f, y);

            GUI.Box(new Rect(x, y, boxWidth, boxHeight), GUIContent.none, backgroundStyle);
            GUI.Label(new Rect(x + PaddingX, y + PaddingY, textWidth, textHeight), content, textStyle);
        }

        private void EnsureStyles()
        {
            if (textStyle != null) return;

            var background = new Texture2D(1, 1);
            background.SetPixel(0, 0, new Color(1f, 1f, 1f, 0.94f));
            background.Apply();
            backgroundStyle = new GUIStyle(GUI.skin.box);
            backgroundStyle.normal.background = background;

            textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };
            textStyle.normal.textColor = new Color(0.15f, 0.12f, 0.1f);

            measureStyle = new GUIStyle(textStyle) { wordWrap = false };
        }
    }
}
