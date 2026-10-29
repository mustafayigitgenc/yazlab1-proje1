using System.Collections.Generic;
using UnityEngine;

namespace Gumusdere.World
{
    /// <summary>
    /// WorldBuilder'ın ürettiği haritanın önemli noktaları ve nesneleri.
    /// Diğer sistemler haritayı bu sınıf üzerinden tanır; koordinatlar tek yerde kalır.
    /// </summary>
    public sealed class WorldLayout
    {
        /// <summary>NavMesh'in hesaplanacağı bölge.</summary>
        public Bounds NavBounds;

        public Vector3 PlayerSpawn;
        public Vector3 CompanionSpawn;

        /// <summary>Sadece oyuncuyu durduran görünmez duvarlar. NavMesh hesabına katılmaz.</summary>
        public Transform PlayerOnlyWalls;

        /// <summary>NavMesh'te alan türü değiştirilecek nesneler (örn. geçit = sığ su, göl = yürünmez).</summary>
        public readonly List<(Transform root, int area)> NavAreaOverrides = new();

        /// <summary>Sığ geçit: sadece Cemal'in yürüyebildiği "ShallowWater" alanı.</summary>
        public Transform Ford;

        public Drawbridge Bridge;

        /// <summary>Köprüyü indiren vinç (Cemal'in görev hedefi).</summary>
        public Gumusdere.Interaction.Winch Winch;

        /// <summary>İneklerin dolaştığı mera (x/z düzleminde).</summary>
        public Rect CowPasture;

        public readonly List<Vector3> FarmTiles = new();
        public readonly List<Vector3> BerryBushes = new();

        public Vector3 FishingSpot;
        public Transform MineDoor;
        public Vector3 GuardSpot;
    }
}
