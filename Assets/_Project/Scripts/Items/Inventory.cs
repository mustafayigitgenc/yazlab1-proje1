using System;
using System.Collections.Generic;

namespace Gumusdere.Items
{
    /// <summary>
    /// Oyuncunun çantası. Unity'den bağımsız düz bir C# sınıfıdır, bu yüzden birim testi kolaydır.
    /// </summary>
    public sealed class Inventory : IInventory
    {
        private readonly Dictionary<ItemType, int> counts = new();

        public event Action<ItemType, int> Changed;

        public int Count(ItemType type) => counts.TryGetValue(type, out int n) ? n : 0;

        public bool Has(ItemType type, int amount = 1) => Count(type) >= amount;

        public void Add(ItemType type, int amount = 1)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            counts[type] = Count(type) + amount;
            Changed?.Invoke(type, counts[type]);
        }

        public bool TryRemove(ItemType type, int amount = 1)
        {
            if (amount <= 0 || !Has(type, amount)) return false;

            int left = Count(type) - amount;
            if (left == 0) counts.Remove(type);
            else counts[type] = left;

            Changed?.Invoke(type, left);
            return true;
        }

        public IReadOnlyDictionary<ItemType, int> Snapshot() => new Dictionary<ItemType, int>(counts);
    }
}
