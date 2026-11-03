using Gumusdere.Core;
using Gumusdere.Items;
using Gumusdere.Village;
using UnityEngine;

namespace Gumusdere.UI
{
    /// <summary>İlan panosu penceresi: köylülerin istekleri, ilerleme ve "Teslim et" butonu.</summary>
    public sealed class QuestView : ModalWindow
    {
        private QuestBook book;
        private IMessageLog log;
        private string lastMessage;

        protected override Vector2 PanelSize => new(720f, 450f);

        public void Initialize(QuestBook questBook, System.Action<bool> controlsSetter, IMessageLog messageLog)
        {
            book = questBook;
            log = messageLog;
            SetControlsCallback(controlsSetter);
        }

        protected override void OnOpened() => lastMessage = null;

        protected override void DrawContent()
        {
            if (book == null) return;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Köy İlan Panosu", TitleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Tamamlanan: <b>{book.CompletedCount}/{book.Quests.Count}</b>", TextStyle);
            GUILayout.EndHorizontal();
            GUILayout.Label("Köylüler bakkaldan daha iyi para veriyor. İstenen eşyayı çantanda getir, teslim et.", SmallStyle);
            GUILayout.Space(8f);

            foreach (VillageQuest quest in book.Quests)
            {
                int have = Mathf.Min(book.Inventory.Count(quest.Item), quest.Amount);
                GUILayout.BeginHorizontal();
                string status = quest.Done
                    ? "<color=#8fd18f>tamamlandı</color>"
                    : $"{have}/{quest.Amount} {ItemInfo.DisplayName(quest.Item).ToLowerInvariant()}";
                GUILayout.Label($"<b>{quest.Giver}</b>: {quest.Title}\n<color=#d8c37a>Ödül: {quest.Reward} lira</color>   {status}", TextStyle, GUILayout.Width(540f));
                GUI.enabled = book.CanComplete(quest);
                if (!quest.Done && GUILayout.Button("Teslim et", ButtonStyle, GUILayout.Width(110f), GUILayout.Height(40f))) Complete(quest);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                GUILayout.Space(4f);
            }

            if (!string.IsNullOrEmpty(lastMessage)) GUILayout.Label(lastMessage, TextStyle);
        }

        private void Complete(VillageQuest quest)
        {
            if (book.TryComplete(quest, out lastMessage)) log?.Add("", lastMessage, Color.white);
        }
    }
}
