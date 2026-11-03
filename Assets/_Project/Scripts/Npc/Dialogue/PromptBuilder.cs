using System.Collections.Generic;
using System.Text;
using Gumusdere.Items;
using Gumusdere.Npc.Llm;

namespace Gumusdere.Npc.Dialogue
{
    /// <summary>
    /// Modele gidecek mesaj listesini kurar:
    ///   [system]    kişilik, konuşma tarzı, karar kuralları, örnekler (her istekte aynı)
    ///   [geçmiş]    son birkaç tur: oyuncunun sözü + NPC'nin JSON cevabı
    ///   [user]      [DURUM] güven / envanter / hediyeler  +  [OYUNCU] yeni söz  +  kısa hatırlatma
    /// Güncel durum ve hatırlatma en son mesaja konur; küçük modeller en son okudukları talimata
    /// en çok uyar. Durum her istekte oyundan okunur, böylece model kararını oyunun gerçek durumuna göre verir.
    /// </summary>
    public sealed class PromptBuilder
    {
        private const string Reminder =
            "(Hüsnü Dayı olarak, sade ve düzgün Türkçeyle, 1 veya 2 kısa cümleyle cevap ver. " +
            "Önce oyuncunun söylediğine cevap ver. Sadece JSON döndür.)";

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
            IReadOnlyList<ItemType> giftsReceived,
            bool mineOpen,
            string extraState = null)
        {
            var messages = new List<ChatMessage>(history.Count + 2)
            {
                ChatMessage.FromSystem(persona.SystemPrompt)
            };
            messages.AddRange(history.Recent());

            string turn = DescribeState(trust, inventory, giftsReceived, mineOpen, extraState) + "\n" +
                          FormatPlayer(playerMessage) + "\n" +
                          Reminder;
            messages.Add(ChatMessage.FromUser(turn));
            return messages;
        }

        /// <summary>Oyuncunun sözü; geçmişte de aynı biçimde saklanır ki model kimin konuştuğunu karıştırmasın.</summary>
        public static string FormatPlayer(string playerMessage) =>
            "[OYUNCU] \"" + (playerMessage ?? "").Replace("\"", "'") + "\"";

        /// <summary>Tek satırlık güncel durum, örn: [DURUM] Güven: 35/100 | Maden kapısı: kapalı | Envanter: alabalik x1 | Aldığın hediyeler: yok</summary>
        public static string DescribeState(int trust, IReadOnlyDictionary<ItemType, int> inventory,
            IReadOnlyList<ItemType> giftsReceived, bool mineOpen, string extraState = null)
        {
            var sb = new StringBuilder();
            sb.Append("[DURUM] Güven: ").Append(trust).Append("/100");
            sb.Append(" | Maden kapısı: ").Append(mineOpen ? "AÇIK (anahtarı verdin)" : "kapalı");
            sb.Append(" | Envanter: ");

            if (inventory == null || inventory.Count == 0)
            {
                sb.Append("boş");
            }
            else
            {
                bool first = true;
                foreach (KeyValuePair<ItemType, int> pair in inventory)
                {
                    if (!first) sb.Append(", ");
                    sb.Append(ItemInfo.Key(pair.Key)).Append(" x").Append(pair.Value);
                    first = false;
                }
            }

            sb.Append(" | Aldığın hediyeler: ");
            if (giftsReceived == null || giftsReceived.Count == 0)
            {
                sb.Append("yok");
            }
            else
            {
                for (int i = 0; i < giftsReceived.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(ItemInfo.Key(giftsReceived[i]));
                }
            }

            // Oyunun eklediği başka durum bilgisi (örn. "Destek kirişi: 2/3")
            if (!string.IsNullOrEmpty(extraState)) sb.Append(" | ").Append(extraState);

            return sb.ToString();
        }
    }
}
