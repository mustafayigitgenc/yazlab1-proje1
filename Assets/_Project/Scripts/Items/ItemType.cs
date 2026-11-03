using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Gumusdere.Items
{
    /// <summary>Oyunda toplanabilen eşyalar.</summary>
    public enum ItemType
    {
        Pumpkin,
        Berry,
        Trout,
        Carp,
        Catfish,
        Pufferfish,
        Chanterelle,
        FlyAgaric,

        // Bakkaldan alınan ekipman (hediye edilmez, satılmaz; madene girmek için gerekir)
        Helmet,
        Lantern
    }

    /// <summary>Bir eşyanın sabit bilgileri: ekranda görünen ad, LLM anahtarı, renk ve cümlede tanınacağı kelimeler.</summary>
    public sealed class ItemDefinition
    {
        public ItemDefinition(string displayName, string key, Color tint, params string[] keywords)
        {
            DisplayName = displayName;
            Key = key;
            Tint = tint;
            Keywords = keywords;
        }

        public string DisplayName { get; }

        /// <summary>LLM promptunda ve JSON'da kullanılan, Türkçe karaktersiz anahtar.</summary>
        public string Key { get; }

        public Color Tint { get; }

        /// <summary>Oyuncu cümlesinde bu eşyayı anlatan kelime parçaları (küçük harf).</summary>
        public IReadOnlyList<string> Keywords { get; }
    }

    /// <summary>
    /// Tüm eşyaların tanım tablosu. Yeni bir eşya eklemek için enum'a bir değer ve buraya bir satır eklemek yeterli
    /// (JSON şeması ve prompt bu tablodan otomatik üretilir).
    /// </summary>
    public static class ItemInfo
    {
        private static readonly CultureInfo Turkish = new("tr-TR");

        private static readonly Dictionary<ItemType, ItemDefinition> Table = new()
        {
            [ItemType.Pumpkin] = new("Balkabağı", "balkabagi", new Color(0.95f, 0.55f, 0.12f), "balkabağ", "balkabag", "kabak"),
            [ItemType.Berry] = new("Böğürtlen", "bogurtlen", new Color(0.55f, 0.15f, 0.45f), "böğürtlen", "bogurtlen", "meyve"),
            [ItemType.Trout] = new("Alabalık", "alabalik", new Color(0.55f, 0.70f, 0.85f), "alabalık", "alabalik", "balık", "balik"),
            [ItemType.Carp] = new("Sazan", "sazan", new Color(0.75f, 0.62f, 0.30f), "sazan", "balık", "balik"),
            [ItemType.Catfish] = new("Yayın balığı", "yayin_baligi", new Color(0.35f, 0.38f, 0.42f), "yayın", "yayin", "balık", "balik"),
            [ItemType.Pufferfish] = new("Balon balığı", "balon_baligi", new Color(0.90f, 0.85f, 0.45f), "balon", "balık", "balik"),
            [ItemType.Chanterelle] = new("Kanlıca mantarı", "kanlica_mantari", new Color(0.95f, 0.65f, 0.25f), "kanlıca", "kanlica", "mantar"),
            [ItemType.FlyAgaric] = new("Sinek mantarı", "sinek_mantari", new Color(0.85f, 0.12f, 0.12f), "sinek", "mantar"),
            [ItemType.Helmet] = new("Baret", "baret", new Color(0.98f, 0.82f, 0.10f), "baret", "kask"),
            [ItemType.Lantern] = new("Fener", "fener", new Color(1.00f, 0.92f, 0.55f), "fener", "lamba")
        };

        public static IEnumerable<ItemType> All => Table.Keys;

        /// <summary>Ekipman mı (baret, fener)? Ekipman hediye edilemez ve satılamaz.</summary>
        public static bool IsTool(ItemType type) => type == ItemType.Helmet || type == ItemType.Lantern;

        /// <summary>NPC'ye hediye edilebilen eşyalar (JSON şemasındaki accepted_gift seçenekleri).</summary>
        public static IEnumerable<ItemType> Giftable
        {
            get
            {
                foreach (ItemType type in Table.Keys)
                {
                    if (!IsTool(type)) yield return type;
                }
            }
        }

        public static ItemDefinition Get(ItemType type) => Table[type];

        public static string DisplayName(ItemType type) => Table.TryGetValue(type, out var d) ? d.DisplayName : type.ToString();

        public static Color Tint(ItemType type) => Table.TryGetValue(type, out var d) ? d.Tint : Color.white;

        public static string Key(ItemType type) => Table.TryGetValue(type, out var d) ? d.Key : type.ToString().ToLowerInvariant();

        public static bool TryParseKey(string key, out ItemType type)
        {
            foreach (KeyValuePair<ItemType, ItemDefinition> pair in Table)
            {
                if (pair.Value.Key == key)
                {
                    type = pair.Key;
                    return true;
                }
            }

            type = default;
            return false;
        }

        /// <summary>Eşyanın oyuncunun cümlesinde geçip geçmediği (Türkçe büyük/küçük harf kurallarıyla).</summary>
        public static bool IsMentionedIn(ItemType type, string text)
        {
            if (string.IsNullOrEmpty(text) || !Table.TryGetValue(type, out ItemDefinition d)) return false;
            string t = text.ToLower(Turkish);
            foreach (string word in d.Keywords)
            {
                if (t.Contains(word)) return true;
            }
            return false;
        }
    }
}
