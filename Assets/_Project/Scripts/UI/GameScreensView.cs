using Gumusdere.Core;
using UnityEngine;

namespace Gumusdere.UI
{
    /// <summary>
    /// Başlangıç, duraklatma ve bitiş ekranları. Sadece görünüm; geçişleri GameFlow yönetir.
    /// </summary>
    public sealed class GameScreensView : MonoBehaviour
    {
        private const float PanelWidth = 640f;

        private GameFlow flow;
        private Texture2D white;
        private GUIStyle titleStyle, textStyle, buttonStyle, smallStyle;

        public void Initialize(GameFlow gameFlow)
        {
            flow = gameFlow;
        }

        private void OnGUI()
        {
            if (flow == null || flow.Machine.Current == null || flow.IsPlaying) return;

            GUI.depth = -100;
            EnsureStyles();

            float scale = Mathf.Max(1f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            float h = Screen.height / scale;

            if (flow.Machine.Current == flow.Title) DrawTitle(w, h);
            else if (flow.Machine.Current == flow.Paused) DrawPause(w, h);
            else if (flow.Machine.Current == flow.Victory) DrawVictory(w, h);
        }

        private void DrawTitle(float w, float h)
        {
            Fill(new Rect(0f, 0f, w, h), new Color(0.05f, 0.04f, 0.03f, 0.72f));
            Rect panel = Centered(w, h, PanelWidth, 470f);

            GUILayout.BeginArea(panel);
            GUILayout.Label("Gümüşdere", titleStyle);
            GUILayout.Space(8f);
            GUILayout.Label(
                "Dedenden kalan dağ köyü Gümüşdere'yi devraldın. Köy eskiden gümüş madeniyle geçinirdi, " +
                "ama yirmi yıl önceki göçükten beri maden kapalı. Madeni yeniden açıp köyü canlandırmak istiyorsun.\n\n" +
                "Yanında işçin Cemal var. Nehrin karşısına geç, tarlanla ve oltanla hazırlan, " +
                "sonra maden kapısının anahtarını tutan Hüsnü Dayı'yı ikna et.", textStyle);
            GUILayout.Space(14f);
            GUILayout.Label(
                "<b>WASD</b> yürü   <b>Shift</b> koş   <b>E</b> kullan / konuş   <b>F</b> Cemal'e görev ver\n" +
                "<b>G</b> Cemal bekle / takip   <b>Boşluk</b> oltayı çek   <b>Esc</b> menü   <b>F1</b> debug", smallStyle);
            GUILayout.Space(22f);
            if (GUILayout.Button("Başla  (Enter)", buttonStyle, GUILayout.Height(46f))) flow.StartGame();
            GUILayout.Space(6f);
            if (GUILayout.Button("Çık", buttonStyle, GUILayout.Height(38f))) flow.Quit();
            GUILayout.EndArea();
        }

        private void DrawPause(float w, float h)
        {
            Fill(new Rect(0f, 0f, w, h), new Color(0f, 0f, 0f, 0.55f));
            Rect panel = Centered(w, h, 360f, 260f);

            GUILayout.BeginArea(panel);
            GUILayout.Label("Duraklatıldı", titleStyle);
            GUILayout.Space(16f);
            if (GUILayout.Button("Devam et  (Esc)", buttonStyle, GUILayout.Height(42f))) flow.Resume();
            GUILayout.Space(6f);
            if (GUILayout.Button("Yeniden başla", buttonStyle, GUILayout.Height(42f))) flow.Restart();
            GUILayout.Space(6f);
            if (GUILayout.Button("Çık", buttonStyle, GUILayout.Height(42f))) flow.Quit();
            GUILayout.EndArea();
        }

        private void DrawVictory(float w, float h)
        {
            Fill(new Rect(0f, 0f, w, h), new Color(0.06f, 0.05f, 0.02f, 0.7f));
            Rect panel = Centered(w, h, PanelWidth, 440f);
            GameStats s = flow.Stats;
            int minutes = (int)(s.ElapsedSeconds / 60f);
            int seconds = (int)(s.ElapsedSeconds % 60f);

            GUILayout.BeginArea(panel);
            GUILayout.Label("Gümüşdere yeniden canlanıyor!", titleStyle);
            GUILayout.Space(8f);
            GUILayout.Label(
                "Gümüş damarını buldun. Hüsnü Dayı'nın güveni ve Cemal'in emeğiyle maden yeniden açıldı; " +
                "köyün gençleri artık şehre gitmek zorunda değil.", textStyle);
            GUILayout.Space(14f);
            GUILayout.Label(
                $"Süre: <b>{minutes} dk {seconds:00} sn</b>\n" +
                $"Cemal'in tamamladığı görevler: <b>{s.CompanionTasksDone}</b>" +
                (s.CompanionTasksFailed > 0 ? $"  (yarım kalan: {s.CompanionTasksFailed})" : "") + "\n" +
                $"Toplanan eşyalar: <b>{s.ItemsCollected}</b>\n" +
                $"Hüsnü Dayı'ya verilen hediyeler: <b>{s.GiftsGiven}</b>\n" +
                $"Hüsnü Dayı ile konuşma sayısı: <b>{s.NpcReplies}</b>", textStyle);
            GUILayout.Space(20f);
            if (GUILayout.Button("Tekrar oyna", buttonStyle, GUILayout.Height(44f))) flow.Restart();
            GUILayout.Space(6f);
            if (GUILayout.Button("Çık", buttonStyle, GUILayout.Height(38f))) flow.Quit();
            GUILayout.EndArea();
        }

        private static Rect Centered(float w, float h, float width, float height) =>
            new((w - width) * 0.5f, (h - height) * 0.5f, width, height);

        private void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, white);
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            white = Texture2D.whiteTexture;

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            titleStyle.normal.textColor = new Color(1f, 0.86f, 0.5f);

            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true, richText = true };
            textStyle.normal.textColor = Color.white;

            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true, richText = true, alignment = TextAnchor.MiddleCenter };
            smallStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18 };
        }
    }
}
