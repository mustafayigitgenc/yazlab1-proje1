using Gumusdere.Items;

namespace Gumusdere.Mining
{
    /// <summary>
    /// Maden kapısının açılabilmesi için OYUN kuralı (model ne derse desin uygulanır):
    ///   kapı kapalı olmalı, güven eşiği aşılmış olmalı ve oyuncunun çantasında baret ile fener olmalı.
    /// Hem gerçek OpenMineAction hem de diyalog testi bu tek sınıfı kullanır.
    /// </summary>
    public static class MineEntryRule
    {
        public const string MissingGearReason =
            "Baret ve fener olmadan kimse madene giremez. İkisini de köydeki Nazmi'nin bakkalından alabilirsin.";

        /// <summary>İzin varsa null, yoksa sebebini döndürür.</summary>
        public static string Check(int trust, int requiredTrust, bool doorOpen, IInventory inventory)
        {
            if (doorOpen) return "Kapı zaten açık.";
            if (trust < requiredTrust) return $"Güven yetersiz ({trust} < {requiredTrust}).";
            if (inventory == null || !inventory.Has(ItemType.Helmet) || !inventory.Has(ItemType.Lantern)) return MissingGearReason;
            return null;
        }
    }
}
