using System;

namespace Gumusdere.Npc.Dialogue
{
    /// <summary>NPC'nin oyuncuya güveni (0..100). Değeri oyun tutar, model sadece değişim önerir.</summary>
    public sealed class TrustMeter
    {
        public const int Min = 0;
        public const int Max = 100;

        public TrustMeter(int start)
        {
            Value = Clamp(start);
        }

        public int Value { get; private set; }

        /// <summary>(eski değer, yeni değer)</summary>
        public event Action<int, int> Changed;

        public void Apply(int delta)
        {
            int previous = Value;
            Value = Clamp(Value + delta);
            if (Value != previous) Changed?.Invoke(previous, Value);
        }

        private static int Clamp(int v) => v < Min ? Min : v > Max ? Max : v;
    }
}
