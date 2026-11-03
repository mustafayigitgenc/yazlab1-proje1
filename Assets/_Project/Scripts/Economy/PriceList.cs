using System.Collections.Generic;
using Gumusdere.Items;

namespace Gumusdere.Economy
{
    /// <summary>
    /// Bakkal Nazmi'nin fiyatları (lira). Zor bulunan şey pahalıdır: balon balığı en zor yakalanan balık,
    /// en yüksek fiyat onda. Baret + fener toplam 90 lira; bir tarlanın tamamı (12 balkabağı) 72 lira eder,
    /// yani oyuncu en az bir hasat ve biraz balık ya da mantarla ekipmanı alabilir.
    /// </summary>
    public static class PriceList
    {
        public const int StartMoney = 10;

        private static readonly Dictionary<ItemType, int> SellPrices = new()
        {
            [ItemType.Pumpkin] = 6,
            [ItemType.Berry] = 2,
            [ItemType.Carp] = 5,
            [ItemType.Trout] = 8,
            [ItemType.Catfish] = 15,
            [ItemType.Pufferfish] = 30,
            [ItemType.Chanterelle] = 4,
            [ItemType.FlyAgaric] = 1
        };

        private static readonly Dictionary<ItemType, int> BuyPrices = new()
        {
            [ItemType.Helmet] = 50,
            [ItemType.Lantern] = 40
        };

        /// <summary>Bakkalda satılan ekipman (sırayla).</summary>
        public static readonly IReadOnlyList<ItemType> ForSale = new[] { ItemType.Helmet, ItemType.Lantern };

        /// <summary>Satış fiyatı; satılamıyorsa 0.</summary>
        public static int SellPrice(ItemType type) => SellPrices.TryGetValue(type, out int p) ? p : 0;

        /// <summary>Alış fiyatı; bakkalda yoksa 0.</summary>
        public static int BuyPrice(ItemType type) => BuyPrices.TryGetValue(type, out int p) ? p : 0;
    }
}
