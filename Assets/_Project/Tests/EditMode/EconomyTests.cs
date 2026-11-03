using Gumusdere.Economy;
using Gumusdere.Items;
using Gumusdere.Mining;
using Gumusdere.Village;
using NUnit.Framework;

namespace Gumusdere.Tests
{
    /// <summary>Bakkal, cüzdan ve madene giriş kuralı.</summary>
    public sealed class EconomyTests
    {
        private Inventory inventory;
        private Wallet wallet;
        private Shop shop;

        [SetUp]
        public void SetUp()
        {
            inventory = new Inventory();
            wallet = new Wallet(10);
            shop = new Shop(inventory, wallet);
        }

        [Test]
        public void Satis_CantadanDuserParaEklenir()
        {
            inventory.Add(ItemType.Pumpkin, 3);
            Assert.IsTrue(shop.TrySell(ItemType.Pumpkin, 2, out _));
            Assert.AreEqual(1, inventory.Count(ItemType.Pumpkin));
            Assert.AreEqual(10 + 2 * PriceList.SellPrice(ItemType.Pumpkin), wallet.Money);
        }

        [Test]
        public void Satis_CantadaYoksaReddedilir()
        {
            Assert.IsFalse(shop.TrySell(ItemType.Catfish, 1, out string message));
            Assert.AreEqual(10, wallet.Money);
            Assert.IsNotEmpty(message);
        }

        [Test]
        public void Ekipman_Satilamaz()
        {
            inventory.Add(ItemType.Helmet);
            Assert.IsFalse(shop.TrySell(ItemType.Helmet, 1, out _));
            Assert.IsTrue(inventory.Has(ItemType.Helmet));
        }

        [Test]
        public void Alis_ParaYetmezseOlmaz()
        {
            Assert.IsFalse(shop.TryBuy(ItemType.Helmet, out string message));
            Assert.IsFalse(inventory.Has(ItemType.Helmet));
            Assert.AreEqual(10, wallet.Money);
            StringAssert.Contains("lira daha lazım", message);
        }

        [Test]
        public void Alis_ParaDuserEsyaGelir_IkinciKezAlinmaz()
        {
            wallet.Add(100);
            Assert.IsTrue(shop.TryBuy(ItemType.Helmet, out _));
            Assert.IsTrue(inventory.Has(ItemType.Helmet));
            Assert.AreEqual(110 - PriceList.BuyPrice(ItemType.Helmet), wallet.Money);
            Assert.IsFalse(shop.TryBuy(ItemType.Helmet, out _));
        }

        [Test]
        public void Cuzdan_EksiyeDusmez()
        {
            Assert.IsFalse(wallet.TrySpend(11));
            Assert.IsTrue(wallet.TrySpend(10));
            Assert.AreEqual(0, wallet.Money);
            Assert.IsFalse(wallet.TrySpend(0));
        }

        [Test]
        public void Fiyatlar_ZorBalikEnPahali()
        {
            Assert.Greater(PriceList.SellPrice(ItemType.Pufferfish), PriceList.SellPrice(ItemType.Catfish));
            Assert.Greater(PriceList.SellPrice(ItemType.Catfish), PriceList.SellPrice(ItemType.Trout));
            foreach (ItemType item in ItemInfo.Giftable) Assert.Greater(PriceList.SellPrice(item), 0, item.ToString());
        }

        [Test]
        public void MadenKurali_GuvenVeEkipmanIster()
        {
            Assert.IsNotNull(MineEntryRule.Check(60, 70, false, inventory), "güven düşük");
            Assert.AreEqual(MineEntryRule.MissingGearReason, MineEntryRule.Check(90, 70, false, inventory), "ekipman yok");

            inventory.Add(ItemType.Helmet);
            Assert.AreEqual(MineEntryRule.MissingGearReason, MineEntryRule.Check(90, 70, false, inventory), "fener yok");

            inventory.Add(ItemType.Lantern);
            Assert.IsNull(MineEntryRule.Check(70, 70, false, inventory), "her şey tamam");
            Assert.IsNotNull(MineEntryRule.Check(90, 70, true, inventory), "kapı zaten açık");
            StringAssert.Contains("kiriş", MineEntryRule.Check(90, 70, false, inventory, 2), "kiriş eksik");
            Assert.IsNull(MineEntryRule.Check(90, 70, false, inventory, MineEntryRule.RequiredBeams));
        }

        [Test]
        public void IlanPanosu_TeslimEdinceEsyaDuserOdulGelir()
        {
            var book = new QuestBook(inventory, wallet, QuestBook.GumusdereQuests());
            VillageQuest trout = book.Quests[0];
            Assert.AreEqual(ItemType.Trout, trout.Item);

            inventory.Add(ItemType.Trout, 2);
            Assert.IsFalse(book.CanComplete(trout));
            Assert.IsFalse(book.TryComplete(trout, out string message));
            StringAssert.Contains("1 alabalık daha", message);

            inventory.Add(ItemType.Trout);
            int completedEvents = 0;
            book.Completed += q => completedEvents++;
            Assert.IsTrue(book.TryComplete(trout, out _));
            Assert.IsTrue(trout.Done);
            Assert.AreEqual(0, inventory.Count(ItemType.Trout));
            Assert.AreEqual(10 + trout.Reward, wallet.Money);
            Assert.AreEqual(1, book.CompletedCount);
            Assert.AreEqual(1, completedEvents);
            Assert.IsFalse(book.TryComplete(trout, out _), "ikinci kez teslim edilmez");
        }

        [Test]
        public void IlanPanosu_OdulBakkalFiyatindanYuksek()
        {
            foreach (VillageQuest q in QuestBook.GumusdereQuests())
            {
                Assert.Greater(q.Reward, q.Amount * PriceList.SellPrice(q.Item), q.Title);
            }
        }
    }
}
