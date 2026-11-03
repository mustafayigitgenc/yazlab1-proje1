using Gumusdere.Items;
using Gumusdere.Npc.Parsing;
using NUnit.Framework;

namespace Gumusdere.Tests
{
    /// <summary>LLM çıktısını ayrıştıran sınıfın birim testleri: geçerli, bozuk ve şema dışı cevaplar.</summary>
    public sealed class NpcResponseParserTests
    {
        private NpcResponseParser parser;

        [SetUp]
        public void SetUp() => parser = new NpcResponseParser();

        private static string Json(string reply = "Hoş geldin evlat.", string emotion = "friendly", string trust = "3",
            string gift = "none", string action = "none") =>
            "{\"reply\":\"" + reply + "\",\"emotion\":\"" + emotion + "\",\"trust_change\":" + trust +
            ",\"accepted_gift\":\"" + gift + "\",\"action\":\"" + action + "\"}";

        [Test]
        public void GecerliJson_TumAlanlarOkunur()
        {
            Assert.IsTrue(parser.TryParse(Json(gift: "alabalik", action: "open_mine", trust: "7"), out NpcDecision d, out string error), error);
            Assert.AreEqual("Hoş geldin evlat.", d.Reply);
            Assert.AreEqual(NpcEmotion.Friendly, d.Emotion);
            Assert.AreEqual(7, d.TrustChange);
            Assert.AreEqual(ItemType.Trout, d.AcceptedGift);
            Assert.AreEqual(NpcAction.OpenMine, d.Action);
            Assert.IsEmpty(d.Warnings);
        }

        [Test]
        public void MetninIcindekiJson_Ayiklanir()
        {
            string raw = "Tabii! İşte cevabım:\n```json\n" + Json() + "\n```\nBaşka bir şey?";
            Assert.IsTrue(parser.TryParse(raw, out NpcDecision d, out string error), error);
            Assert.AreEqual("Hoş geldin evlat.", d.Reply);
        }

        [Test]
        public void TirnakIcindekiSuslu_ParantezSayilmaz()
        {
            Assert.IsTrue(parser.TryParse(Json(reply: "Şu {kapı} meselesi var."), out NpcDecision d, out _));
            Assert.AreEqual("Şu {kapı} meselesi var.", d.Reply);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("Merhaba evlat, nasılsın?")]
        [TestCase("{reply: 'yarım kalmış")]
        [TestCase("```yine bozuk```")]
        public void BozukCevap_IstisnaFirlatmadanReddedilir(string raw)
        {
            bool ok = true;
            string error = null;
            Assert.DoesNotThrow(() => ok = parser.TryParse(raw, out _, out error));
            Assert.IsFalse(ok);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void ReplyEksik_Reddedilir()
        {
            Assert.IsFalse(parser.TryParse("{\"emotion\":\"angry\",\"trust_change\":-3,\"action\":\"none\"}", out _, out string error));
            StringAssert.Contains("reply", error);
        }

        [TestCase("kick_out")]
        [TestCase("Kick out")]
        [TestCase("open mine")]
        [TestCase("none")]
        [TestCase("friendly")]
        public void KonusmaOlmayanReply_Reddedilir(string reply)
        {
            Assert.IsFalse(parser.TryParse(Json(reply: reply), out _, out _));
        }

        [TestCase("999", 20)]
        [TestCase("-75", -20)]
        [TestCase("20", 20)]
        [TestCase("-4", -4)]
        public void GuvenDegisimi_Kirpilir(string raw, int expected)
        {
            Assert.IsTrue(parser.TryParse(Json(trust: raw), out NpcDecision d, out _));
            Assert.AreEqual(expected, d.TrustChange);
        }

        [Test]
        public void GuvenDegisimiEksik_SifirVeUyari()
        {
            Assert.IsTrue(parser.TryParse("{\"reply\":\"Peki.\",\"emotion\":\"neutral\",\"action\":\"none\"}", out NpcDecision d, out _));
            Assert.AreEqual(0, d.TrustChange);
            Assert.IsNotEmpty(d.Warnings);
        }

        [Test]
        public void SemaDisiDegerler_VarsayilanaDoner()
        {
            Assert.IsTrue(parser.TryParse(Json(emotion: "mutlu", gift: "elmas", action: "fly"), out NpcDecision d, out _));
            Assert.AreEqual(NpcEmotion.Neutral, d.Emotion);
            Assert.AreEqual(NpcAction.None, d.Action);
            Assert.IsNull(d.AcceptedGift);
            Assert.AreEqual(3, d.Warnings.Count);
        }

        [TestCase("baret")]
        [TestCase("fener")]
        public void Ekipman_HediyeOlarakKabulEdilmez(string key)
        {
            Assert.IsTrue(parser.TryParse(Json(gift: key), out NpcDecision d, out _));
            Assert.IsNull(d.AcceptedGift);
            Assert.IsNotEmpty(d.Warnings);
        }

        [TestCase("OPEN_MINE", NpcAction.OpenMine)]
        [TestCase(" close_mine ", NpcAction.CloseMine)]
        [TestCase("kick_out", NpcAction.KickOut)]
        public void Eylem_BuyukKucukHarfeDuyarsiz(string raw, NpcAction expected)
        {
            Assert.IsTrue(parser.TryParse(Json(action: raw), out NpcDecision d, out _));
            Assert.AreEqual(expected, d.Action);
        }

        [Test]
        public void UzunCevap_UcCumleyeVe240KaraktereKisaltilir()
        {
            string longReply = "Bir. İki cümle burada. Üç cümle de var. Dört olmamalı. Beş hiç olmamalı.";
            Assert.IsTrue(parser.TryParse(Json(reply: longReply), out NpcDecision d, out _));
            Assert.AreEqual("Bir. İki cümle burada. Üç cümle de var.", d.Reply);

            string huge = new string('a', 400);
            Assert.IsTrue(parser.TryParse(Json(reply: huge), out d, out _));
            Assert.LessOrEqual(d.Reply.Length, NpcResponseParser.MaxReplyLength);
        }

        [Test]
        public void JsonSemasi_TumEsyaAnahtarlariniIcerir()
        {
            foreach (ItemType item in ItemInfo.Giftable)
            {
                StringAssert.Contains("\"" + ItemInfo.Key(item) + "\"", NpcResponseParser.JsonSchema);
            }
            StringAssert.DoesNotContain("\"baret\"", NpcResponseParser.JsonSchema);
            StringAssert.DoesNotContain("\"fener\"", NpcResponseParser.JsonSchema);
            StringAssert.Contains("open_mine", NpcResponseParser.JsonSchema);
            StringAssert.Contains("close_mine", NpcResponseParser.JsonSchema);
            StringAssert.Contains("kick_out", NpcResponseParser.JsonSchema);
        }
    }
}
