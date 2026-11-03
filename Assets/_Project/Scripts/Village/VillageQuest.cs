using Gumusdere.Items;

namespace Gumusdere.Village
{
    /// <summary>İlan panosundaki bir köylü isteği: belli sayıda eşya getir, karşılığında para al.</summary>
    public sealed class VillageQuest
    {
        public VillageQuest(string giver, string title, ItemType item, int amount, int reward)
        {
            Giver = giver;
            Title = title;
            Item = item;
            Amount = amount;
            Reward = reward;
        }

        public string Giver { get; }
        public string Title { get; }
        public ItemType Item { get; }
        public int Amount { get; }
        public int Reward { get; }
        public bool Done { get; internal set; }
    }
}
