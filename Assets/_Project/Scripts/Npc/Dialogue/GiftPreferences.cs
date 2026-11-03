using System.Collections.Generic;
using System.Text;
using Gumusdere.Items;

namespace Gumusdere.Npc.Dialogue
{
    public enum GiftTaste
    {
        Loved,
        Liked,
        Neutral,
        Disliked,
        Hated
    }

    /// <summary>
    /// NPC'nin hediye zevki. Bir hediyenin güvene etkisini MODEL değil OYUN hesaplar (tutarlı ve test edilebilir);
    /// model sadece bu zevke uygun bir tepki cümlesi kurar. Aynı hediye tekrar verildikçe etkisi azalır.
    /// </summary>
    public sealed class GiftPreferences
    {
        private readonly Dictionary<ItemType, (GiftTaste taste, string reason)> table = new();
        private readonly Dictionary<ItemType, int> timesReceived = new();

        public GiftPreferences Set(ItemType item, GiftTaste taste, string reason)
        {
            table[item] = (taste, reason);
            return this;
        }

        public GiftTaste TasteOf(ItemType item) => table.TryGetValue(item, out var entry) ? entry.taste : GiftTaste.Neutral;

        public static int BaseEffect(GiftTaste taste) => taste switch
        {
            GiftTaste.Loved => 12,
            GiftTaste.Liked => 6,
            GiftTaste.Neutral => 2,
            GiftTaste.Disliked => -8,
            GiftTaste.Hated => -15,
            _ => 0
        };

        /// <summary>
        /// Hediyeyi kaydeder ve güvene etkisini döndürür. Olumlu etki tekrarlarda yarıya iner (12, 6, 3...);
        /// olumsuz etki ise azalmaz.
        /// </summary>
        public int Receive(ItemType item)
        {
            int count = timesReceived.TryGetValue(item, out int n) ? n : 0;
            timesReceived[item] = count + 1;

            int effect = BaseEffect(TasteOf(item));
            if (effect > 0)
            {
                for (int i = 0; i < count; i++) effect /= 2;
                if (effect < 1) effect = 1;
            }
            return effect;
        }

        /// <summary>System prompt için: "SEVDİKLERİN: ..., SEVMEDİKLERİN: ..." satırları.</summary>
        public string Describe()
        {
            var sb = new StringBuilder();
            Append(sb, GiftTaste.Loved, "ÇOK SEVDİĞİN HEDİYELER");
            Append(sb, GiftTaste.Liked, "HOŞLANDIĞIN HEDİYELER");
            Append(sb, GiftTaste.Disliked, "SEVMEDİĞİN HEDİYELER");
            Append(sb, GiftTaste.Hated, "NEFRET ETTİĞİN HEDİYELER");
            return sb.ToString();
        }

        private void Append(StringBuilder sb, GiftTaste taste, string title)
        {
            bool any = false;
            foreach (KeyValuePair<ItemType, (GiftTaste taste, string reason)> pair in table)
            {
                if (pair.Value.taste != taste) continue;
                sb.Append(any ? "; " : $"- {title}: ");
                sb.Append(ItemInfo.DisplayName(pair.Key)).Append(" (").Append(pair.Value.reason).Append(')');
                any = true;
            }
            if (any) sb.AppendLine();
        }

        public static GiftPreferences HusnuDayi() => new GiftPreferences()
            .Set(ItemType.Trout, GiftTaste.Loved, "alabalık tavasına bayılırsın")
            .Set(ItemType.Catfish, GiftTaste.Loved, "gençken babanla yayın tutardın, çok nadir bulunur")
            .Set(ItemType.Pumpkin, GiftTaste.Loved, "rahmetli eşinin balkabağı tatlısını hatırlatır")
            .Set(ItemType.Carp, GiftTaste.Liked, "fena değil ama kılçıklı")
            .Set(ItemType.Berry, GiftTaste.Liked, "çocukken bu dağlarda toplardın")
            .Set(ItemType.Chanterelle, GiftTaste.Disliked, "geçen yıl mantar yüzünden zehirlendin, mantar sevmezsin")
            .Set(ItemType.FlyAgaric, GiftTaste.Hated, "zehirli mantar; sana zarar vermek istediklerini düşünürsün")
            .Set(ItemType.Pufferfish, GiftTaste.Hated, "zehirli balık; sana zehir getirildiğini düşünüp kızarsın");
    }
}
