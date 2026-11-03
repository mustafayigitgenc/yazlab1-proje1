using System;
using System.Collections.Generic;
using Gumusdere.Economy;
using Gumusdere.Items;

namespace Gumusdere.Village
{
    /// <summary>
    /// Köy ilan panosundaki istekler ve teslim kuralı (arayüzden bağımsız, birim testi yazılabilir).
    /// Ödüller bakkal fiyatından yüksektir; böylece oyuncu ürünü satmak, panoya vermek ya da Hüsnü Dayı'ya
    /// hediye etmek arasında seçim yapar.
    /// </summary>
    public sealed class QuestBook
    {
        private readonly List<VillageQuest> quests = new();
        private readonly IInventory inventory;
        private readonly IWallet wallet;

        public QuestBook(IInventory inventory, IWallet wallet, IEnumerable<VillageQuest> list)
        {
            this.inventory = inventory;
            this.wallet = wallet;
            quests.AddRange(list);
        }

        public IReadOnlyList<VillageQuest> Quests => quests;
        public IInventory Inventory => inventory;

        public int CompletedCount
        {
            get
            {
                int n = 0;
                foreach (VillageQuest q in quests) if (q.Done) n++;
                return n;
            }
        }

        /// <summary>Bir istek teslim edildi.</summary>
        public event Action<VillageQuest> Completed;

        public bool CanComplete(VillageQuest quest) => quest != null && !quest.Done && inventory.Has(quest.Item, quest.Amount);

        public bool TryComplete(VillageQuest quest, out string message)
        {
            if (quest == null || quest.Done)
            {
                message = "Bu istek zaten tamamlandı.";
                return false;
            }

            int have = inventory.Count(quest.Item);
            if (have < quest.Amount)
            {
                message = $"{quest.Giver} için {quest.Amount - have} {ItemInfo.DisplayName(quest.Item).ToLowerInvariant()} daha lazım.";
                return false;
            }

            inventory.TryRemove(quest.Item, quest.Amount);
            wallet.Add(quest.Reward);
            quest.Done = true;
            message = $"{quest.Giver} çok sevindi! +{quest.Reward} lira.";
            Completed?.Invoke(quest);
            return true;
        }

        /// <summary>Gümüşdere'nin köylü istekleri.</summary>
        public static IEnumerable<VillageQuest> GumusdereQuests() => new[]
        {
            new VillageQuest("Muhtar Ayşe", "Köy sofrası için 3 alabalık", ItemType.Trout, 3, 45),
            new VillageQuest("Fırıncı Hasan", "Kış hazırlığı: 8 balkabağı", ItemType.Pumpkin, 8, 70),
            new VillageQuest("Öğretmen Zeynep", "Okul için reçel: 6 böğürtlen", ItemType.Berry, 6, 25),
            new VillageQuest("Aşçı Kemal", "Mantar sote: 4 kanlıca mantarı", ItemType.Chanterelle, 4, 30),
            new VillageQuest("Eczacı Saadet", "İlaç için 1 balon balığı", ItemType.Pufferfish, 1, 60)
        };
    }
}
