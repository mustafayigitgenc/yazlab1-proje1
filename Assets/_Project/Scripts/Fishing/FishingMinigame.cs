using System;

namespace Gumusdere.Fishing
{
    /// <summary>
    /// Balık tutma zamanlama oyununun kuralları (Unity'den bağımsız, test edilebilir):
    ///  1) Bekleme: balık oltaya gelene kadar beklenir. Bu sırada çekmek balığı kaçırır.
    ///  2) Çekme: ibre çubukta gidip gelir; yeşil bölgedeyken çekilirse balık yakalanır.
    ///  Belli bir sürede çekilmezse balık kaçar. Zor balıklarda bölge dar, ibre hızlı ve
    ///  "düzensiz" türlerde ibre ara sıra aniden yön değiştirir.
    /// </summary>
    public sealed class FishingMinigame
    {
        public enum Phase { Idle, Waiting, Reeling }

        private const float ErraticFlipChancePerSecond = 1.6f;

        private readonly Random random;
        private float direction = 1f;
        private float speed;
        private float reelDuration;
        private bool erratic;
        private float timer;

        public FishingMinigame(int seed = 0)
        {
            random = seed == 0 ? new Random() : new Random(seed);
        }

        public Phase Current { get; private set; } = Phase.Idle;

        /// <summary>İbre konumu (0..1).</summary>
        public float Needle { get; private set; }

        public float ZoneStart { get; private set; }
        public float ZoneWidth { get; private set; }
        public float TimeLeft => timer;

        /// <summary>Şu an oltadaki balık (bekleme sırasında da seçilmiştir ama oyuncuya söylenmez).</summary>
        public FishSpecies Fish { get; private set; }

        public bool IsActive => Current != Phase.Idle;
        public bool InZone => Needle >= ZoneStart && Needle <= ZoneStart + ZoneWidth;
        public bool HasTimedOut => Current == Phase.Reeling && timer <= 0f;

        public void Begin(FishSpecies fish, float biteDelay, float zoneStart)
        {
            Fish = fish;
            Current = Phase.Waiting;
            timer = biteDelay;
            ZoneWidth = fish.ZoneWidth;
            ZoneStart = zoneStart < 0f ? 0f : zoneStart > 1f - ZoneWidth ? 1f - ZoneWidth : zoneStart;
            speed = fish.NeedleSpeed;
            reelDuration = fish.ReelDuration;
            erratic = fish.Erratic;
            Needle = 0f;
            direction = 1f;
        }

        public void Tick(float deltaTime)
        {
            switch (Current)
            {
                case Phase.Waiting:
                    timer -= deltaTime;
                    if (timer <= 0f)
                    {
                        Current = Phase.Reeling;
                        timer = reelDuration;
                    }
                    break;

                case Phase.Reeling:
                    timer -= deltaTime;
                    if (erratic && random.NextDouble() < ErraticFlipChancePerSecond * deltaTime) direction = -direction;
                    Needle += direction * speed * deltaTime;
                    if (Needle >= 1f) { Needle = 1f; direction = -1f; }
                    else if (Needle <= 0f) { Needle = 0f; direction = 1f; }
                    break;
            }
        }

        public void End()
        {
            Current = Phase.Idle;
        }
    }
}
