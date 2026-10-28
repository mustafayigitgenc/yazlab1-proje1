using Gumusdere.Companion;
using Gumusdere.Core;
using Gumusdere.World;
using UnityEngine;

namespace Gumusdere.UI
{
    /// <summary>
    /// Sol üstte kontrol ipuçları; F1 açıkken Cemal'in durum bilgileri.
    /// Geçici, kurulum gerektirmeyen arayüz (IMGUI). Diyalog ekranı ileride ayrı yapılacak.
    /// </summary>
    public sealed class HelpOverlay : MonoBehaviour
    {
        private CompanionController companion;
        private Drawbridge bridge;
        private GUIStyle panelStyle;
        private GUIStyle textStyle;

        public void Initialize(CompanionController targetCompanion, Drawbridge targetBridge)
        {
            companion = targetCompanion;
            bridge = targetBridge;
        }

        private void OnGUI()
        {
            EnsureStyles();

            float scale = Mathf.Max(1f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            float height = DebugView.Enabled ? 210f : 120f;
            GUILayout.BeginArea(new Rect(12f, 12f, 430f, height), panelStyle);
            GUILayout.Label("<b>Gümüşdere</b>  ·  1. adım: köy ve Cemal", textStyle);
            GUILayout.Label("WASD / oklar: yürü    Shift: koş", textStyle);
            GUILayout.Label("G: Cemal bekle / takip et    F1: debug görünümü", textStyle);
            GUILayout.Label("F2: (test) köprüyü indir", textStyle);

            if (DebugView.Enabled && companion != null && companion.Agent != null)
            {
                var agent = companion.Agent;
                GUILayout.Space(6f);
                GUILayout.Label($"<b>Cemal</b>: {companion.CurrentStateName}", textStyle);
                GUILayout.Label($"Hız: {agent.velocity.magnitude:0.0} m/s   Hedefe kalan: {(agent.hasPath ? agent.remainingDistance : 0f):0.0} m", textStyle);
                GUILayout.Label($"Yol durumu: {(agent.hasPath ? agent.pathStatus.ToString() : "yol yok")}", textStyle);
                GUILayout.Label($"Köprü: {(bridge != null && bridge.IsLowered ? "indi" : "kalkık")}", textStyle);
            }

            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (panelStyle != null) return;

            var background = new Texture2D(1, 1);
            background.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.55f));
            background.Apply();

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 10, 10)
            };
            panelStyle.normal.background = background;

            textStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                fontSize = 15
            };
            textStyle.normal.textColor = Color.white;
        }
    }
}
