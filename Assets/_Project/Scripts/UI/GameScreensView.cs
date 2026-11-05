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
        private GUIStyle titleStyle, textStyle, buttonStyle, smallStyle, logoStyle, subtitleStyle, hintStyle, footerStyle;

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
            // Sol tarafta yarı saydam panel; sağda kamera köyün üstünde döner (CameraFollow.SetShowcase).
            const float panelWidth = 600f;
            for (int i = 0; i < 24; i++)
            {
                float a = Mathf.Lerp(0.82f, 0f, i / 23f);
                Fill(new Rect(i * (panelWidth + 260f) / 24f, 0f, (panelWidth + 260f) / 24f + 1f, h), new Color(0.05f, 0.035f, 0.02f, a));
            }

            var area = new Rect(56f, Mathf.Max(30f, (h - 700f) * 0.5f), panelWidth - 40f, 700f);
            GUILayout.BeginArea(area);

            // Logo: gölgeli büyük başlık + ince gümüş çizgi + alt başlık
            Rect logo = GUILayoutUtility.GetRect(area.width, 86f);
            Shadowed(logo, "GÜMÜŞDERE", logoStyle, new Color(0f, 0f, 0f, 0.65f), 3f);
            Rect line = GUILayoutUtility.GetRect(area.width, 10f);
            Fill(new Rect(line.x, line.y + 4f, 330f, 2f), new Color(0.85f, 0.88f, 0.92f, 0.9f));
            Fill(new Rect(line.x + 335f, line.y + 3f, 4f, 4f), new Color(1f, 0.8f, 0.45f, 1f));
            GUILayout.Label("Bir dağ köyünün yeniden doğuşu", subtitleStyle);
            GUILayout.Space(18f);

            GUILayout.Label(
                "Dedenden kalan dağ köyü Gümüşdere'yi devraldın. Köy eskiden gümüş madeniyle geçinirdi, " +
                "ama yirmi yıl önceki göçükten beri maden kapalı.\n\n" +
                "Yanında işçin <b>Cemal</b> var: köprüyü indirir, tarlayı sular, kiriş taşır, göçüğü temizler. " +
                "Tarlanla, oltanla ve köylülerin istekleriyle para kazan, bakkal <b>Nazmi</b>'den baret ve fener al, " +
                "sonra maden kapısının anahtarını tutan <b>Hüsnü Dayı</b>'yı ikna et.", textStyle);
            GUILayout.Space(16f);
            GUILayout.Label(
                "<b>WASD</b> yürü   <b>Shift</b> koş   <b>E</b> kullan / konuş   <b>F</b> Cemal'e görev ver\n" +
                "<b>G</b> bekle / takip   <b>M</b> harita   <b>Boşluk</b> oltayı çek   <b>Esc</b> menü", hintStyle);
            GUILayout.Space(24f);
            if (GUILayout.Button("Başla  (Enter)", buttonStyle, GUILayout.Height(48f), GUILayout.Width(330f))) flow.StartGame();
            GUILayout.Space(6f);
            if (GUILayout.Button("Çık", buttonStyle, GUILayout.Height(38f), GUILayout.Width(330f))) flow.Quit();
            GUILayout.EndArea();

            GUI.Label(new Rect(56f, h - 40f, panelWidth, 24f),
                "Kocaeli Üniversitesi · YazLab I · Proje 1   —   Mustafa Yiğit Genç · İbrahim Akkuş", footerStyle);
        }

        private static void Shadowed(Rect r, string text, GUIStyle style, Color shadow, float offset)
        {
            Color old = style.normal.textColor;
            style.normal.textColor = shadow;
            GUI.Label(new Rect(r.x + offset, r.y + offset, r.width, r.height), text, style);
            style.normal.textColor = old;
            GUI.Label(r, text, style);
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
            Rect panel = Centered(w, h, PanelWidth, 470f);
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
                $"Kazanılan para: <b>{s.MoneyEarned} lira</b>   Köy istekleri: <b>{s.QuestsCompleted}/5</b>\n" +
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

            logoStyle = new GUIStyle(GUI.skin.label) { fontSize = 68, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            logoStyle.normal.textColor = new Color(0.93f, 0.95f, 0.98f);
            subtitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Italic };
            subtitleStyle.normal.textColor = new Color(1f, 0.84f, 0.55f);
            hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true, richText = true };
            hintStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            footerStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            footerStyle.normal.textColor = new Color(0.8f, 0.78f, 0.72f);
        }
    }
}
