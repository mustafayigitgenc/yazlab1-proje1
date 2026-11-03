using System.Collections.Generic;
using Gumusdere.Items;

namespace Gumusdere.Fishing
{
    /// <summary>
    /// Bir balık türünün oltadaki davranışı. Zorluk: yeşil bölge genişliği, ibre hızı, çekme süresi
    /// ve ibrenin düzensiz yön değiştirip değiştirmediği. Weight nadirliği belirler (büyük = sık).
    /// </summary>
    public sealed class FishSpecies
    {
        public FishSpecies(ItemType item, int weight, float zoneWidth, float needleSpeed, float reelDuration,
            bool erratic, string biteHint)
        {
            Item = item;
            Weight = weight;
            ZoneWidth = zoneWidth;
            NeedleSpeed = needleSpeed;
            ReelDuration = reelDuration;
            Erratic = erratic;
            BiteHint = biteHint;
        }

        public ItemType Item { get; }
        public int Weight { get; }
        public float ZoneWidth { get; }
        public float NeedleSpeed { get; }
        public float ReelDuration { get; }
        public bool Erratic { get; }

        /// <summary>Balık oltaya gelince gösterilen ipucu (türü söylemez, zorluğu sezdirir).</summary>
        public string BiteHint { get; }

        /// <summary>Gölün balıkları: sazan bol ve kolay, balon balığı nadir ve çok zor.</summary>
        public static IReadOnlyList<FishSpecies> Lake { get; } = new[]
        {
            new FishSpecies(ItemType.Carp, 40, 0.26f, 0.80f, 6.0f, false, "Oltaya bir şey takıldı."),
            new FishSpecies(ItemType.Trout, 35, 0.18f, 1.10f, 5.5f, false, "Hızlı bir balık, dikkat!"),
            new FishSpecies(ItemType.Catfish, 15, 0.12f, 1.50f, 5.0f, false, "Olta fena çekiyor, büyük bir şey bu!"),
            new FishSpecies(ItemType.Pufferfish, 10, 0.08f, 1.90f, 4.5f, true, "Şamandıra tuhaf tuhaf sallanıyor...")
        };

        /// <summary>Ağırlıklara göre rastgele bir tür seçer. roll: 0..1 arası sayı.</summary>
        public static FishSpecies Pick(IReadOnlyList<FishSpecies> pool, float roll)
        {
            int total = 0;
            foreach (FishSpecies s in pool) total += s.Weight;

            float target = roll * total;
            int running = 0;
            foreach (FishSpecies s in pool)
            {
                running += s.Weight;
                if (target < running) return s;
            }
            return pool[pool.Count - 1];
        }
    }
}
