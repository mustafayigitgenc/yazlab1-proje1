using System;

namespace Gumusdere.Economy
{
    public sealed class Wallet : IWallet
    {
        public Wallet(int startMoney = 0)
        {
            Money = Math.Max(0, startMoney);
        }

        public int Money { get; private set; }

        /// <summary>Oyun boyunca kazanılan toplam para (istatistik için).</summary>
        public int TotalEarned { get; private set; }

        public event Action<int, int> Changed;

        public void Add(int amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            int old = Money;
            Money += amount;
            TotalEarned += amount;
            Changed?.Invoke(old, Money);
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0 || amount > Money) return false;
            int old = Money;
            Money -= amount;
            Changed?.Invoke(old, Money);
            return true;
        }
    }
}
