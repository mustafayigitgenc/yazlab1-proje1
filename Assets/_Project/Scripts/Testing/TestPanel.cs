using UnityEngine;

namespace Gumusdere.Testing
{
    /// <summary>Test koşucularının ekranın sağ üstünde gösterdiği küçük bilgi kutusu (IMGUI).</summary>
    public static class TestPanel
    {
        private static GUIStyle style;

        public static void Draw(string title, string body)
        {
            if (string.IsNullOrEmpty(body)) return;

            style ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 14,
                richText = true,
                wordWrap = true,
                padding = new RectOffset(12, 12, 10, 10),
                normal = { textColor = Color.white }
            };

            float scale = Mathf.Max(1f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;

            const float boxWidth = 430f;
            string text = $"<b>{title}</b>\n{body}";
            float height = style.CalcHeight(new GUIContent(text), boxWidth) + 4f;
            GUI.Box(new Rect(width - boxWidth - 12f, 52f, boxWidth, height), text, style); // LLM durum kutusunun altı
        }
    }
}
