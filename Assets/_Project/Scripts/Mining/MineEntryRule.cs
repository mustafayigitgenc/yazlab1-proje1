using System.Collections.Generic;
using System.Globalization;
using Gumusdere.Items;

namespace Gumusdere.Mining
{
    /// <summary>
    /// Maden kapısının açılabilmesi için OYUN kuralı (model ne derse desin uygulanır):
    ///   kapı kapalı olmalı, güven eşiği aşılmış olmalı, oyuncunun çantasında baret ile fener olmalı
    ///   ve madenin ağzına 3 destek kirişi taşınmış olmalı.
    /// Hem gerçek OpenMineAction hem de diyalog testi bu tek sınıfı kullanır.
    /// Ayrıca küçük dil modeline her mesajda "[KAPI]" satırıyla neyin eksik olduğunu hazır söyler;
    /// model durumu kendisi yorumlamak zorunda kalmaz.
    /// </summary>
    public static class MineEntryRule
    {
        public const int RequiredBeams = 3;

        public const string MissingGearReason =
            "Baret ve fener olmadan kimse madene giremez. İkisini de köydeki Nazmi'nin bakkalından alabilirsin.";

        public static string MissingBeamsReason(int placed) =>
            $"Tünele önce {RequiredBeams} destek kirişi lazım (şu an {placed}/{RequiredBeams}). " +
            "Kirişleri çoban Rıfat Amca verir (kaçan ineklerini ahıra toplarsan), Cemal de madenin ağzına taşır.";

        private static readonly CultureInfo Turkish = new("tr-TR");
        private static readonly string[] DoorWords = { "kapı", "kapi", "maden", "aç", "ac ", "gir", "içeri", "iceri", "anahtar", "kilit", "tünel", "tunel" };

        /// <summary>
        /// Oyuncu kapıdan, madenden ya da içeri girmekten bahsediyor mu? [KAPI] satırı sadece o zaman gönderilir;
        /// yoksa model selama bile "baretin yok" diye cevap vermeye başlar. (Model kendiliğinden open_mine derse
        /// NpcDialogueController'daki tutarlılık kontrolü yine yakalar.)
        /// </summary>
        public static bool IsAboutDoor(string playerText)
        {
            if (string.IsNullOrEmpty(playerText)) return false;
            string t = playerText.ToLower(Turkish);
            foreach (string word in DoorWords)
            {
                if (t.Contains(word)) return true;
            }
            return false;
        }

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

        /// <summary>Eksik şartlar: (modele anlatım, oyuncuya Hüsnü Dayı'nın ağzından anlatım).</summary>
        public static List<(string forNpc, string forPlayer)> Missing(int trust, int requiredTrust, IInventory inventory, int beamsPlaced)
        {
            var list = new List<(string, string)>();
            if (trust < requiredTrust)
                list.Add(($"oyuncuya henüz yeterince güvenmiyorsun (Güven {trust}/{requiredTrust})", "sana henüz tam güvenmiyorum"));
            if (inventory == null || !inventory.Has(ItemType.Helmet))
                list.Add(("oyuncunun bareti yok (bakkal Nazmi'den alabilir)", "baretin yok, Nazmi'nin bakkalından al"));
            if (inventory == null || !inventory.Has(ItemType.Lantern))
                list.Add(("oyuncunun feneri yok (bakkal Nazmi'den alabilir)", "fenerin yok, onu da Nazmi'den al"));
            if (beamsPlaced < RequiredBeams)
                list.Add(($"madenin ağzında destek kirişi eksik ({beamsPlaced}/{RequiredBeams}; kirişleri Rıfat Amca verir, Cemal taşır)",
                          $"madenin ağzında daha {beamsPlaced}/{RequiredBeams} destek kirişi var; Rıfat Amca'nın ineklerini topla, kirişleri o verir"));
            return list;
        }

        /// <summary>Modelin son mesajına eklenen kesin talimat (örn. "[KAPI] Kapıyı şu an AÇAMAZSIN... Eksikler: ...").</summary>
        public static string HintForNpc(int trust, int requiredTrust, bool doorOpen, IInventory inventory, int beamsPlaced)
        {
            if (doorOpen)
                return "[KAPI] Kapı şu an açık. Oyuncu kapatmanı isterse action close_mine.";

            List<(string forNpc, string forPlayer)> missing = Missing(trust, requiredTrust, inventory, beamsPlaced);
            if (missing.Count == 0)
                return "[KAPI] Bütün şartlar tamam (güven, baret, fener, kirişler). Oyuncu kapıyı açmanı isterse action open_mine yaz ve anahtarı verdiğini söyle.";

            var parts = new List<string>();
            foreach (var (forNpc, _) in missing) parts.Add(forNpc);
            return "[KAPI] Kapıyı şu an AÇAMAZSIN: action open_mine OLAMAZ ve kapıyı açtığını ya da girilebileceğini söyleme. " +
                   "Eksikler: " + string.Join("; ", parts) + ". Oyuncu kapıdan veya madene girmekten bahsederse bu eksikleri kısaca söyle.";
        }

        /// <summary>Kural reddedince Hüsnü Dayı'nın söyleyeceği hazır cümle.</summary>
        public static string ExplainToPlayer(int trust, int requiredTrust, bool doorOpen, IInventory inventory, int beamsPlaced)
        {
            if (doorOpen) return "Kapı zaten açık evlat.";
            List<(string forNpc, string forPlayer)> missing = Missing(trust, requiredTrust, inventory, beamsPlaced);
            if (missing.Count == 0) return "Peki evlat, biraz daha düşüneyim.";

            var parts = new List<string>();
            foreach (var (_, forPlayer) in missing) parts.Add(forPlayer);
            return "Kusura bakma evlat, kapıyı henüz açamam: " + string.Join("; ", parts) + ".";
        }
    }
}
