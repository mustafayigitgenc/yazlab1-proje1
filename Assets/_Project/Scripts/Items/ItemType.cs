using UnityEngine;

namespace Gumusdere.Items
{
    /// <summary>Oyunda toplanabilen eşyalar.</summary>
    public enum ItemType
    {
        Pumpkin,
        Berry,
        Trout
    }

    /// <summary>Eşyaların ekranda görünen adları ve renkleri.</summary>
    public static class ItemInfo
    {
        public static string DisplayName(ItemType type) => type switch
        {
            ItemType.Pumpkin => "Balkabağı",
            ItemType.Berry => "Böğürtlen",
            ItemType.Trout => "Alabalık",
            _ => type.ToString()
        };

        public static Color Tint(ItemType type) => type switch
        {
            ItemType.Pumpkin => new Color(0.95f, 0.55f, 0.12f),
            ItemType.Berry => new Color(0.55f, 0.15f, 0.45f),
            ItemType.Trout => new Color(0.55f, 0.7f, 0.85f),
            _ => Color.white
        };

        /// <summary>LLM promptunda kullanılan Türkçe karaktersiz anahtar (örn. "alabalik").</summary>
        public static string Key(ItemType type) => type switch
        {
            ItemType.Pumpkin => "balkabagi",
            ItemType.Berry => "bogurtlen",
            ItemType.Trout => "alabalik",
            _ => type.ToString().ToLowerInvariant()
        };
    }
}
