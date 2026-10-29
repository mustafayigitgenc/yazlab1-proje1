using System.Collections.Generic;
using Gumusdere.Npc.Llm;

namespace Gumusdere.Npc.Dialogue
{
    /// <summary>
    /// Sohbet hafızası. Küçük modellerin bağlam penceresi dar olduğu için sadece son birkaç tur gönderilir.
    /// </summary>
    public sealed class ConversationHistory
    {
        private readonly List<ChatMessage> messages = new();
        private readonly int maxMessages;

        public ConversationHistory(int maxTurns = 6)
        {
            maxMessages = maxTurns * 2;
        }

        public int Count => messages.Count;

        public void AddTurn(string playerText, string npcJson)
        {
            messages.Add(ChatMessage.FromUser(playerText));
            messages.Add(ChatMessage.FromAssistant(npcJson));
            while (messages.Count > maxMessages) messages.RemoveAt(0);
        }

        public IReadOnlyList<ChatMessage> Recent() => messages;

        public void Clear() => messages.Clear();
    }
}
