using System;
using System.Collections.Generic;

namespace Gumusdere.Items
{
    /// <summary>Eşya çantası sözleşmesi. Arayüz ve NPC tarafı somut sınıfı değil bunu kullanır.</summary>
    public interface IInventory
    {
        event Action<ItemType, int> Changed;

        int Count(ItemType type);
        bool Has(ItemType type, int amount = 1);
        void Add(ItemType type, int amount = 1);
        bool TryRemove(ItemType type, int amount = 1);

        /// <summary>Sıfırdan büyük olan tüm eşyalar.</summary>
        IReadOnlyDictionary<ItemType, int> Snapshot();
    }
}
