using System.Collections.Generic;
using System.Text;
using Gumusdere.Items;
using Gumusdere.Npc.Llm;

namespace Gumusdere.Npc.Dialogue
{
    /// <summary>
    /// Modele gidecek mesaj listesini kurar:
    ///   [system: kişilik + kurallar + GÜNCEL DURUM]  [geçmiş turlar]  [oyuncunun yeni mesajı]
    /// Güncel durum (güven, envanter, verilen hediyeler) her istekte oyundan okunur,
    /// böylece model kararını oyunun gerçek durumuna göre verir.
    /// </summary>
    public sealed class PromptBuilder
    {
        private readonly NpcPersona persona;

        public PromptBuilder(NpcPersona persona)
        {
            this.persona = persona;
        }

        public List<ChatMessage> Build(
            ConversationHistory history,
            string playerMessage,
            int trust,
            IReadOnlyDictionary<ItemType, int> inventory,
            IReadOnlyList<ItemType> giftsReceived)
        {
            var messages = new List<ChatMessage>(history.Count + 2)
            {
                ChatMessage.FromSystem(persona.SystemPrompt + "\n\n" + DescribeState(trust, inventory, giftsReceived))
            };
            messages.AddRange(history.Recent());
            messages.Add(ChatMessage.FromUser(playerMessage));
            return messages;
        }

        public static string DescribeState(int trust, IReadOnlyDictionary<ItemType, int> inventory, IReadOnlyList<ItemType> giftsReceived)
        {
            var sb = new StringBuilder();
            sb.AppendLine("GÜNCEL DURUM:");
            sb.AppendLine($"- GÜVEN: {trust}/100");

            sb.Append("- ENVANTER (oyuncunun çantası): ");
            if (inventory == null || inventory.Count == 0)
            {
                sb.AppendLine("boş");
            }
            else
            {
                bool first = true;
                foreach (KeyValuePair<ItemType, int> pair in inventory)
                {
                    if (!first) sb.Append(", ");
                    sb.Append($"{ItemInfo.Key(pair.Key)} x{pair.Value}");
                    first = false;
                }
                sb.AppendLine();
            }

            sb.Append("- DAHA ÖNCE ALDIĞIN HEDİYELER: ");
            if (giftsReceived == null || giftsReceived.Count == 0) sb.AppendLine("yok");
            else
            {
                for (int i = 0; i < giftsReceived.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(ItemInfo.Key(giftsReceived[i]));
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
