using System;

namespace Gumusdere.Economy
{
    /// <summary>Oyuncunun parası (lira).</summary>
    public interface IWallet
    {
        int Money { get; }

        /// <summary>(eski, yeni)</summary>
        event Action<int, int> Changed;

        void Add(int amount);

        /// <summary>Yeterli para varsa düşer ve true döner; yoksa hiçbir şey değişmez.</summary>
        bool TrySpend(int amount);
    }
}
