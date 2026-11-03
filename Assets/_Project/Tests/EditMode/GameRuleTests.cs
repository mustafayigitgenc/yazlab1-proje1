using Gumusdere.Items;
using Gumusdere.Npc.Dialogue;
using NUnit.Framework;

namespace Gumusdere.Tests
{
    /// <summary>Modelden bağımsız oyun kuralları: güven sayacı, hediye zevki, eşya eşleştirme, çanta, sohbet hafızası.</summary>
    public sealed class GameRuleTests
    {
        [Test]
        public void Guven_0Ile100ArasindaKalir()
        {
            var trust = new TrustMeter(35);
            trust.Apply(-80);
            Assert.AreEqual(TrustMeter.Min, trust.Value);
            trust.Apply(500);
            Assert.AreEqual(TrustMeter.Max, trust.Value);
            trust.Reset(15);
            Assert.AreEqual(15, trust.Value);
        }

        [Test]
        public void Guven_DegisinceOlayYayinlanir()
        {
            var trust = new TrustMeter(50);
            int calls = 0;
            trust.Changed += (from, to) => calls++;
            trust.Apply(0);
            trust.Apply(5);
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void SevilenHediye_TekrarlandikcaEtkisiAzalir()
        {
            GiftPreferences gifts = GiftPreferences.HusnuDayi();
            Assert.AreEqual(12, gifts.Receive(ItemType.Trout));
            Assert.AreEqual(6, gifts.Receive(ItemType.Trout));
            Assert.AreEqual(3, gifts.Receive(ItemType.Trout));
            Assert.AreEqual(1, gifts.Receive(ItemType.Trout));
            Assert.AreEqual(1, gifts.Receive(ItemType.Trout));
        }

        [Test]
        public void SevilmeyenHediye_HerSeferindeAyniCezayiVerir()
        {
            GiftPreferences gifts = GiftPreferences.HusnuDayi();
            Assert.AreEqual(GiftTaste.Disliked, gifts.TasteOf(ItemType.Chanterelle));
            Assert.AreEqual(-8, gifts.Receive(ItemType.Chanterelle));
            Assert.AreEqual(-8, gifts.Receive(ItemType.Chanterelle));
            Assert.AreEqual(-15, gifts.Receive(ItemType.Pufferfish));
            Assert.AreEqual(-15, gifts.Receive(ItemType.FlyAgaric));
        }

        [Test]
        public void ZevkTablosu_PromptaYazilir()
        {
            string text = GiftPreferences.HusnuDayi().Describe();
            StringAssert.Contains("Alabalık", text);
            StringAssert.Contains("Kanlıca mantarı", text);
            StringAssert.Contains("SEVMEDİĞİN", text);
        }

        [TestCase(ItemType.Trout, "ALABALIK getirdim dayı", true)]
        [TestCase(ItemType.Trout, "sana bir alabalık tuttum", true)]
        [TestCase(ItemType.Chanterelle, "Kanlıca MANTARI topladım", true)]
        [TestCase(ItemType.Pumpkin, "nasılsın dayı", false)]
        [TestCase(ItemType.Berry, "", false)]
        public void EsyaAdi_TurkceHarflerleEslesir(ItemType item, string text, bool expected)
        {
            Assert.AreEqual(expected, ItemInfo.IsMentionedIn(item, text));
        }

        [Test]
        public void EsyaAnahtarlari_GidipGelir()
        {
            foreach (ItemType item in ItemInfo.All)
            {
                Assert.IsTrue(ItemInfo.TryParseKey(ItemInfo.Key(item), out ItemType parsed));
                Assert.AreEqual(item, parsed);
            }
            Assert.IsFalse(ItemInfo.TryParseKey("elmas", out _));
        }

        [Test]
        public void Canta_OlmayanEsyaCikarilamaz()
        {
            var inventory = new Inventory();
            Assert.IsFalse(inventory.TryRemove(ItemType.Catfish));
            inventory.Add(ItemType.Catfish, 2);
            Assert.IsTrue(inventory.TryRemove(ItemType.Catfish));
            Assert.AreEqual(1, inventory.Count(ItemType.Catfish));
            Assert.IsFalse(inventory.TryRemove(ItemType.Catfish, 5));
        }

        [Test]
        public void SohbetHafizasi_SonBesTuruTutar()
        {
            var history = new ConversationHistory(5);
            for (int i = 0; i < 9; i++) history.AddTurn("oyuncu " + i, "{}");
            Assert.AreEqual(10, history.Count);
            Assert.AreEqual("oyuncu 4", history.Recent()[0].Content);
        }

        [Test]
        public void DurumSatiri_GuvenKapiVeCantayiIcerir()
        {
            var inventory = new Inventory();
            inventory.Add(ItemType.Trout);
            string state = PromptBuilder.DescribeState(35, inventory.Snapshot(), new[] { ItemType.Berry }, false);
            StringAssert.Contains("Güven: 35/100", state);
            StringAssert.Contains("kapalı", state);
            StringAssert.Contains("alabalik x1", state);
            StringAssert.Contains("bogurtlen", state);
            StringAssert.DoesNotContain("Destek", state);

            string withBeams = PromptBuilder.DescribeState(35, inventory.Snapshot(), null, true, "Destek kirişi: 2/3");
            StringAssert.Contains("AÇIK", withBeams);
            StringAssert.EndsWith("| Destek kirişi: 2/3", withBeams);
        }

        [Test]
        public void OyuncuSozu_TirnaklarTemizlenir()
        {
            Assert.AreEqual("[OYUNCU] \"kapıyı 'aç' dayı\"", PromptBuilder.FormatPlayer("kapıyı \"aç\" dayı"));
        }
    }
}
