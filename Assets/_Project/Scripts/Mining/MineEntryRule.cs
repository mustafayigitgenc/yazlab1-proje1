using Gumusdere.Items;

namespace Gumusdere.Mining
{
    /// <summary>
    /// Maden kapısının açılabilmesi için OYUN kuralı (model ne derse desin uygulanır):
    ///   kapı kapalı olmalı, güven eşiği aşılmış olmalı, oyuncunun çantasında baret ile fener olmalı
    ///   ve madenin ağzına 3 destek kirişi taşınmış olmalı.
    /// Hem gerçek OpenMineAction hem de diyalog testi bu tek sınıfı kullanır.
    /// </summary>
    public static class MineEntryRule
    {
        public const int RequiredBeams = 3;

        public const string MissingGearReason =
            "Baret ve fener olmadan kimse madene giremez. İkisini de köydeki Nazmi'nin bakkalından alabilirsin.";

        public static string MissingBeamsReason(int placed) =>
            $"Tünele önce {RequiredBeams} destek kirişi lazım (şu an {placed}/{RequiredBeams}). " +
            "Cemal kirişleri ormandaki tomruk yığınından madenin ağzına taşıyabilir.";

        /// <summary>Durum satırı için: "Destek kirişi: 2/3"</summary>
        public static string DescribeBeams(int placed) => $"Destek kirişi: {placed}/{RequiredBeams}";

        /// <summary>İzin varsa null, yoksa sebebini döndürür.</summary>
        public static string Check(int trust, int requiredTrust, bool doorOpen, IInventory inventory, int beamsPlaced = RequiredBeams)
        {
            if (doorOpen) return "Kapı zaten açık.";
            if (trust < requiredTrust) return $"Güven yetersiz ({trust} < {requiredTrust}).";
            if (inventory == null || !inventory.Has(ItemType.Helmet) || !inventory.Has(ItemType.Lantern)) return MissingGearReason;
            if (beamsPlaced < RequiredBeams) return MissingBeamsReason(beamsPlaced);
            return null;
        }
    }
}
