namespace Gumusdere.Fishing
{
    /// <summary>
    /// Balık tutma zamanlama oyununun kuralları (Unity'den bağımsız, test edilebilir):
    ///  1) Bekleme: balık oltaya gelene kadar beklenir. Bu sırada çekmek balığı kaçırır.
    ///  2) Çekme: ibre çubukta gidip gelir; yeşil bölgedeyken çekilirse balık yakalanır.
    ///  Belli bir sürede çekilmezse balık kaçar.
    /// </summary>
    public sealed class FishingMinigame
    {
        public enum Phase { Idle, Waiting, Reeling }

        public const float ReelDuration = 6f;

        private float direction = 1f;
        private float speed;
        private float timer;

        public Phase Current { get; private set; } = Phase.Idle;

        /// <summary>İbre konumu (0..1).</summary>
        public float Needle { get; private set; }

        public float ZoneStart { get; private set; }
        public float ZoneWidth { get; private set; }
        public float TimeLeft => timer;

        public bool IsActive => Current != Phase.Idle;
        public bool InZone => Needle >= ZoneStart && Needle <= ZoneStart + ZoneWidth;
        public bool HasTimedOut => Current == Phase.Reeling && timer <= 0f;

        public void Begin(float biteDelay, float zoneStart, float zoneWidth, float needleSpeed)
        {
            Current = Phase.Waiting;
            timer = biteDelay;
            ZoneWidth = zoneWidth;
            ZoneStart = zoneStart < 0f ? 0f : zoneStart > 1f - zoneWidth ? 1f - zoneWidth : zoneStart;
            speed = needleSpeed;
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
                        timer = ReelDuration;
                    }
                    break;

                case Phase.Reeling:
                    timer -= deltaTime;
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
