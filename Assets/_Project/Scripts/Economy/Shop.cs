using Gumusdere.Items;

namespace Gumusdere.Economy
{
    /// <summary>
    /// Alım-satım kuralları (arayüzden bağımsız, birim testi yazılabilir):
    ///  - Çantada olmayan eşya satılamaz; ekipman satılamaz.
    ///  - Parası yetmeyen alamaz; aynı ekipman iki kez alınmaz.
    /// </summary>
    public sealed class Shop
    {
        private readonly IInventory inventory;
        private readonly IWallet wallet;

        public Shop(IInventory inventory, IWallet wallet)
        {
            this.inventory = inventory;
            this.wallet = wallet;
        }

        public IInventory Inventory => inventory;
        public IWallet Wallet => wallet;

        public bool TrySell(ItemType type, int count, out string message)
        {
            int price = PriceList.SellPrice(type);
            if (price <= 0)
            {
                message = $"{ItemInfo.DisplayName(type)} satılmaz.";
                return false;
            }

            if (count <= 0 || !inventory.Has(type, count))
            {
                message = $"Çantanda o kadar {ItemInfo.DisplayName(type).ToLowerInvariant()} yok.";
                return false;
            }

            inventory.TryRemove(type, count);
            wallet.Add(price * count);
            message = $"{count} {ItemInfo.DisplayName(type)} sattın, +{price * count} lira.";
            return true;
        }

        public bool TryBuy(ItemType type, out string message)
        {
            int price = PriceList.BuyPrice(type);
            if (price <= 0)
            {
                message = $"Bakkalda {ItemInfo.DisplayName(type)} yok.";
                return false;
            }

            if (inventory.Has(type))
            {
                message = $"Zaten bir {ItemInfo.DisplayName(type).ToLowerInvariant()} var.";
                return false;
            }

            if (!wallet.TrySpend(price))
            {
                message = $"{ItemInfo.DisplayName(type)} için {price - wallet.Money} lira daha lazım.";
                return false;
            }

            inventory.Add(type);
            message = $"{ItemInfo.DisplayName(type)} aldın, -{price} lira.";
            return true;
        }
    }
}
