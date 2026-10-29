using System.Text;
using Gumusdere.Npc.Dialogue;
using Gumusdere.Npc.Llm;
using Gumusdere.Npc.Parsing;
using UnityEngine;

namespace Gumusdere.UI
{
    /// <summary>
    /// Hüsnü Dayı ile sohbet penceresi: geçmiş satırlar, serbest metin kutusu, güven çubuğu,
    /// ruh hâli, LLM durumu ve model düşünürken animasyonlu "düşünüyor..." göstergesi.
    /// Sadece görünüm; tüm mantık NpcDialogueController'da.
    /// </summary>
    public sealed class DialogueView : MonoBehaviour
    {
        private const string InputControl = "DialogueInput";
        private const float Width = 760f;
        private const float Height = 380f;

        private NpcDialogueController dialogue;
        private MessageLog messageLog;
        private string input = "";
        private Vector2 scroll;
        private int lastLineCount;
        private bool focusNextFrame;
        private bool wasOpen;

        private GUIStyle panelStyle, headerStyle, lineStyle, inputStyle, buttonStyle, smallStyle;
        private Texture2D white;

        public void Initialize(NpcDialogueController controller, MessageLog log)
        {
            dialogue = controller;
            messageLog = log;
        }

        private void OnGUI()
        {
            // Play modundayken kod yeniden derlenirse Unity sahnedeki nesneleri yarım bırakabilir; o durumda çizme.
            if (dialogue == null || dialogue.Persona == null || dialogue.Trust == null) return;

            if (dialogue.IsOpen != wasOpen)
            {
                wasOpen = dialogue.IsOpen;
                if (messageLog != null) messageLog.Visible = !wasOpen;
                if (wasOpen)
                {
                    input = "";
                    focusNextFrame = true;
                }
            }

            if (!dialogue.IsOpen) return;
            EnsureStyles();

            float scale = Mathf.Max(1f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            float h = Screen.height / scale;

            HandleKeys();

            var panel = new Rect((w - Width) * 0.5f, h - Height - 20f, Width, Height);
            GUI.Box(panel, GUIContent.none, panelStyle);

            DrawHeader(new Rect(panel.x + 16f, panel.y + 10f, panel.width - 32f, 52f));
            DrawLines(new Rect(panel.x + 16f, panel.y + 68f, panel.width - 32f, panel.height - 130f));
            DrawInput(new Rect(panel.x + 16f, panel.yMax - 52f, panel.width - 32f, 36f));
        }

        private void HandleKeys()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown) return;

            if (e.keyCode == KeyCode.Escape)
            {
                dialogue.Close();
                e.Use();
            }
            else if ((e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) && !dialogue.IsThinking)
            {
                Send();
                e.Use();
            }
        }

        private void DrawHeader(Rect r)
        {
            GUI.Label(new Rect(r.x, r.y, 300f, 26f), $"<b>{dialogue.Persona.Name}</b>   <size=13>({EmotionText(dialogue.LastEmotion)})</size>", headerStyle);

            // Güven çubuğu
            int trust = dialogue.Trust.Value;
            var bar = new Rect(r.x + 300f, r.y + 7f, 220f, 14f);
            Fill(bar, new Color(1f, 1f, 1f, 0.15f));
            Fill(new Rect(bar.x, bar.y, bar.width * trust / 100f, bar.height), TrustColor(trust));
            float mark = bar.x + bar.width * dialogue.Persona.OpenThreshold / 100f;
            Fill(new Rect(mark - 1f, bar.y - 3f, 2f, bar.height + 6f), Color.white);
            GUI.Label(new Rect(bar.xMax + 10f, r.y, 120f, 26f), $"Güven {trust}/100", smallStyle);

            GUI.Label(new Rect(r.x, r.y + 26f, r.width, 22f), StatusText(), smallStyle);
        }

        private void DrawLines(Rect r)
        {
            var sb = new StringBuilder();
            foreach (DialogueLine line in dialogue.Lines)
            {
                switch (line.Speaker)
                {
                    case DialogueSpeaker.Player:
                        sb.Append("<color=#9fd3ff><b>Sen:</b></color> ").Append(line.Text);
                        break;
                    case DialogueSpeaker.Npc:
                        sb.Append("<color=#ffcf7a><b>").Append(dialogue.Persona.Name).Append(":</b></color> ").Append(line.Text);
                        break;
                    default:
                        sb.Append("<color=#b8b8b8><i>[Sistem] ").Append(line.Text).Append("</i></color>");
                        break;
                }
                sb.Append('\n');
            }

            if (dialogue.IsThinking)
            {
                int dots = 1 + (int)((Time.time - dialogue.ThinkingSince) * 3f) % 3;
                float seconds = Time.time - dialogue.ThinkingSince;
                sb.Append("<color=#ffcf7a><i>").Append(dialogue.Persona.Name).Append(" düşünüyor").Append('.', dots)
                  .Append($"  ({seconds:0} sn)</i></color>");
            }

            var content = new GUIContent(sb.ToString());
            float contentHeight = lineStyle.CalcHeight(content, r.width - 20f);

            if (dialogue.Lines.Count != lastLineCount || dialogue.IsThinking)
            {
                lastLineCount = dialogue.Lines.Count;
                scroll.y = float.MaxValue; // en alta kaydır
            }

            scroll = GUI.BeginScrollView(r, scroll, new Rect(0f, 0f, r.width - 20f, Mathf.Max(contentHeight, r.height)));
            GUI.Label(new Rect(0f, 0f, r.width - 20f, contentHeight), content, lineStyle);
            GUI.EndScrollView();
        }

        private void DrawInput(Rect r)
        {
            bool busy = dialogue.IsThinking;
            GUI.enabled = !busy;

            GUI.SetNextControlName(InputControl);
            input = GUI.TextField(new Rect(r.x, r.y, r.width - 220f, r.height), input ?? "", 300, inputStyle);

            if (GUI.Button(new Rect(r.xMax - 210f, r.y, 100f, r.height), busy ? "..." : "Gönder", buttonStyle)) Send();
            GUI.enabled = true;
            if (GUI.Button(new Rect(r.xMax - 100f, r.y, 100f, r.height), "Kapat (Esc)", buttonStyle)) dialogue.Close();

            if (focusNextFrame && Event.current.type == EventType.Repaint)
            {
                focusNextFrame = false;
                input = "";
                GUI.FocusControl(InputControl);
            }
        }

        private void Send()
        {
            if (dialogue.IsThinking || string.IsNullOrWhiteSpace(input)) return;
            dialogue.Submit(input);
            input = "";
            GUI.FocusControl(InputControl);
        }

        private string StatusText()
        {
            string dot = dialogue.Health switch
            {
                LlmHealth.Online => "<color=#5ee07a>●</color> Yerel model çevrimiçi",
                LlmHealth.ModelMissing => "<color=#ffcc44>●</color> Model yüklü değil",
                LlmHealth.Offline => "<color=#ff5555>●</color> Ollama'ya ulaşılamıyor",
                _ => "<color=#aaaaaa>●</color> Bağlantı kontrol ediliyor"
            };
            return $"{dot}  ({dialogue.ModelName})   ·   Enter: gönder   ·   Esc: kapat";
        }

        private static string EmotionText(NpcEmotion emotion) => emotion switch
        {
            NpcEmotion.Friendly => "dostça",
            NpcEmotion.Suspicious => "şüpheli",
            NpcEmotion.Angry => "kızgın",
            NpcEmotion.Sad => "hüzünlü",
            _ => "sakin"
        };

        private static Color TrustColor(int trust) =>
            trust >= 70 ? new Color(0.35f, 0.85f, 0.4f) : trust >= 35 ? new Color(0.95f, 0.75f, 0.25f) : new Color(0.95f, 0.35f, 0.3f);

        private void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, white);
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (panelStyle != null) return;
            white = Texture2D.whiteTexture;

            var background = new Texture2D(1, 1);
            background.SetPixel(0, 0, new Color(0.08f, 0.07f, 0.06f, 0.9f));
            background.Apply();
            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = background;

            headerStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 18 };
            headerStyle.normal.textColor = new Color(1f, 0.85f, 0.5f);

            smallStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 13 };
            smallStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

            lineStyle = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 15 };
            lineStyle.normal.textColor = Color.white;

            inputStyle = new GUIStyle(GUI.skin.textField) { fontSize = 16, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(8, 8, 4, 4) };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14 };
        }
    }
}
