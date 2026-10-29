using Gumusdere.Companion;
using Gumusdere.Companion.Sensing;
using Gumusdere.Core;
using Gumusdere.Player;
using Gumusdere.World;
using UnityEngine;

namespace Gumusdere.UI
{
    /// <summary>
    /// Sol üstte kontrol ipuçları, ekranın altında o an verilebilecek emir,
    /// F1 açıkken Cemal'in durum ve sensör bilgileri. Kurulum gerektirmeyen arayüz (IMGUI).
    /// </summary>
    public sealed class HelpOverlay : MonoBehaviour
    {
        private CompanionController companion;
        private PlayerCommandInput commands;
        private PlayerInteractor interactor;
        private Drawbridge bridge;
        private GUIStyle panelStyle;
        private GUIStyle textStyle;
        private GUIStyle promptStyle;

        public void Initialize(CompanionController targetCompanion, PlayerCommandInput playerCommands,
            PlayerInteractor playerInteractor, Drawbridge targetBridge)
        {
            companion = targetCompanion;
            commands = playerCommands;
            interactor = playerInteractor;
            bridge = targetBridge;
        }

        private void OnGUI()
        {
            EnsureStyles();

            float scale = Mathf.Max(1f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            DrawHelpPanel();
            DrawPrompt(width, height);
        }

        private void DrawHelpPanel()
        {
            float panelHeight = DebugView.Enabled ? 275f : 125f;
            GUILayout.BeginArea(new Rect(12f, 12f, 450f, panelHeight), panelStyle);
            GUILayout.Label("<b>Gümüşdere</b>", textStyle);
            GUILayout.Label("WASD / oklar: yürü    Shift: koş", textStyle);
            GUILayout.Label("E: kullan (tarla, çalı, iskele)    Boşluk: oltayı çek", textStyle);
            GUILayout.Label("F: Cemal'e görev ver    G: bekle / takip / geri çağır    F1: debug", textStyle);

            if (DebugView.Enabled && companion != null && companion.Agent != null)
            {
                var agent = companion.Agent;
                SensorReading reading = companion.Sensor != null ? companion.Sensor.Latest : default;
                GUILayout.Space(6f);
                GUILayout.Label($"<b>Cemal</b>: {companion.CurrentStateName}" +
                                (companion.CurrentTask != null ? $"  (görev: {companion.CurrentTask.DisplayName})" : ""), textStyle);
                GUILayout.Label($"Hız: {agent.velocity.magnitude:0.0} m/s   Hedefe kalan: {(agent.hasPath ? agent.remainingDistance : 0f):0.0} m", textStyle);
                GUILayout.Label($"Yol: {(agent.hasPath ? agent.pathStatus.ToString() : "yok")}   Yeniden yol: {companion.GoToTargetState.RepathCount}   Sığ suda: {(companion.IsInShallowWater() ? "evet" : "hayır")}", textStyle);
                if (reading.IsValid)
                {
                    string nearest = reading.AnyHit
                        ? $"{reading.NearestDistance:0.0} m, {reading.NearestAngle:+0;-0;0}°"
                        : "yok";
                    GUILayout.Label($"Sensör: en yakın engel {nearest}   Dikkatli mod: {(companion.GoToTargetState.IsCareful ? "açık" : "kapalı")}", textStyle);
                }
                GUILayout.Label($"Köprü: {(bridge != null && bridge.IsLowered ? "indi" : "kalkık")}", textStyle);
            }

            GUILayout.EndArea();
        }

        private void DrawPrompt(float width, float height)
        {
            float y = height - 90f;

            if (commands != null && commands.AvailableTask != null)
            {
                GUI.Box(new Rect(width * 0.5f - 200f, y, 400f, 40f), $"[F]  {commands.AvailableTask.CommandText}", promptStyle);
                y -= 48f;
            }

            if (interactor != null && !string.IsNullOrEmpty(interactor.CurrentPrompt))
            {
                GUI.Box(new Rect(width * 0.5f - 200f, y, 400f, 40f), $"[E]  {interactor.CurrentPrompt}", promptStyle);
            }
        }

        private void EnsureStyles()
        {
            if (panelStyle != null) return;

            panelStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(12, 12, 10, 10) };
            panelStyle.normal.background = SolidTexture(new Color(0f, 0f, 0f, 0.55f));

            textStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 15 };
            textStyle.normal.textColor = Color.white;

            promptStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            promptStyle.normal.background = SolidTexture(new Color(0.1f, 0.1f, 0.1f, 0.8f));
            promptStyle.normal.textColor = new Color(1f, 0.92f, 0.55f);
        }

        private static Texture2D SolidTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
