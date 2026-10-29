using Gumusdere.Fishing;
using UnityEngine;

namespace Gumusdere.UI
{
    /// <summary>Balık tutarken ekranın ortasında ibre çubuğunu çizer.</summary>
    public sealed class FishingView : MonoBehaviour
    {
        private const float BarWidth = 460f;
        private const float BarHeight = 26f;

        private FishingSpot spot;
        private Texture2D white;
        private GUIStyle labelStyle;

        public void Initialize(FishingSpot fishingSpot)
        {
            spot = fishingSpot;
        }

        private void OnGUI()
        {
            if (spot == null || !spot.Game.IsActive) return;
            EnsureStyles();

            float scale = Mathf.Max(1f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            float h = Screen.height / scale;

            FishingMinigame game = spot.Game;
            var bar = new Rect((w - BarWidth) * 0.5f, h * 0.62f, BarWidth, BarHeight);
            var panel = new Rect(bar.x - 16f, bar.y - 44f, bar.width + 32f, bar.height + 76f);
            Fill(panel, new Color(0f, 0f, 0f, 0.6f));

            if (game.Current == FishingMinigame.Phase.Waiting)
            {
                GUI.Label(new Rect(panel.x, bar.y - 36f, panel.width, 28f), "Bekle... şamandıra batınca çekeceksin", labelStyle);
                Fill(bar, new Color(0.3f, 0.3f, 0.3f, 0.9f));
            }
            else
            {
                GUI.Label(new Rect(panel.x, bar.y - 36f, panel.width, 28f),
                    $"ÇEK!  İbre yeşildeyken BOŞLUK  ({game.TimeLeft:0.0} sn)", labelStyle);
                Fill(bar, new Color(0.25f, 0.25f, 0.28f, 0.95f));
                Fill(new Rect(bar.x + bar.width * game.ZoneStart, bar.y, bar.width * game.ZoneWidth, bar.height),
                    new Color(0.3f, 0.85f, 0.35f, 1f));
                float nx = bar.x + bar.width * game.Needle;
                Fill(new Rect(nx - 3f, bar.y - 6f, 6f, bar.height + 12f), game.InZone ? Color.white : new Color(1f, 0.85f, 0.3f));
            }

            GUI.Label(new Rect(panel.x, bar.yMax + 4f, panel.width, 24f), "Esc: vazgeç", labelStyle);
        }

        private void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, white);
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (labelStyle != null) return;
            white = Texture2D.whiteTexture;
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            labelStyle.normal.textColor = Color.white;
        }
    }
}
