using Gumusdere.Npc.Dialogue;
using Gumusdere.Npc.Llm;
using UnityEngine;

namespace Gumusdere.UI
{
    /// <summary>
    /// Sağ üstte her zaman görünen LLM durum ışığı (yeşil: çevrimiçi, sarı: model yok, kırmızı: sunucu kapalı).
    /// Sunumda Ollama kapatılıp açıldığında ışığın değiştiği gösterilir.
    /// </summary>
    public sealed class LlmStatusIndicator : MonoBehaviour
    {
        private NpcDialogueController dialogue;
        private GUIStyle style;
        private Texture2D background;

        public void Initialize(NpcDialogueController controller)
        {
            dialogue = controller;
        }

        private void OnGUI()
        {
            if (dialogue == null) return;
            EnsureStyle();

            float scale = Mathf.Max(1f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;

            string text = dialogue.Health switch
            {
                LlmHealth.Online => $"<color=#5ee07a>●</color> LLM çevrimiçi ({dialogue.ModelName})",
                LlmHealth.ModelMissing => $"<color=#ffcc44>●</color> Model yok: {dialogue.ModelName}",
                LlmHealth.Offline => "<color=#ff5555>●</color> Ollama kapalı",
                _ => "<color=#aaaaaa>●</color> LLM kontrol ediliyor"
            };

            var rect = new Rect(w - 300f, 12f, 288f, 30f);
            GUI.DrawTexture(rect, background);
            GUI.Label(rect, text, style);
        }

        private void EnsureStyle()
        {
            if (style != null) return;
            background = new Texture2D(1, 1);
            background.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.55f));
            background.Apply();
            style = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = Color.white;
        }
    }
}
