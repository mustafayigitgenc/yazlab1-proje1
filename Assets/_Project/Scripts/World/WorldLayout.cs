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
        public readonly List<Gumusdere.Farming.FarmPlot> Plots = new();

        /// <summary>Kuyudan kova alıp tarlayı sulama görevi (Cemal'in çok duraklı görevi).</summary>
        public Gumusdere.Farming.WateringJob Watering;

        /// <summary>M ile açılan haritada çizilecek bölgeler (x/z düzleminde).</summary>
        public readonly List<MapFeature> MapFeatures = new();

        /// <summary>Haritanın kapsadığı dünya alanı (x/z).</summary>
        public Rect MapBounds;
        public readonly List<Vector3> BerryBushes = new();

        /// <summary>Bakkal tezgâhı ve bakkal Nazmi'nin durduğu yer.</summary>
        public Gumusdere.Economy.ShopStall Shop;
        public Vector3 ShopkeeperSpot;

        /// <summary>Köy ilan panosu (köylülerin istekleri).</summary>
        public Gumusdere.Village.QuestBoard QuestBoard;

        /// <summary>Cemal'in kiriş taşıma görevi; kirişler Rıfat Amca'nın ahırının önünde durur.</summary>
        public Gumusdere.Mining.BeamHaulJob Beams;
        public Transform BeamPile;
        public Vector3 BeamPileStand;

        /// <summary>Rıfat Amca'nın ahırı: inek toplama görevi, ağıl alanı, kapı ve çobanın yeri.</summary>
        public Gumusdere.Village.HerdCowsJob Herd;
        public Rect BarnPen;
        public Vector3 BarnGate;
        public Vector3 FarmerSpot;

        /// <summary>Başıboş ineklerin dolaştığı bölgeler (her inek için bir tane).</summary>
        public readonly List<Rect> LooseCowAreas = new()
        {
            new Rect(-17f, 33f, 7f, 5f),   // göl kıyısının güneydoğusu
            new Rect(4f, 54f, 6f, 6f),     // böğürtlen çalılarının batısı
            new Rect(-8f, 63f, 5f, 5f)     // maden yolunun batısı
        };

        public Vector3 FishingSpot;
        public Gumusdere.Fishing.FishingSpot Fishing;

        /// <summary>Oyuncunun E ile kullanabildiği her şey (tarlalar, çalılar, iskele...).</summary>
        public readonly List<Gumusdere.Interaction.IPlayerInteractable> PlayerInteractables = new();
        public Transform MineDoor;
        public Gumusdere.World.MineDoor Door;
        public Vector3 GuardSpot;

        /// <summary>Bu z değerinden sonrası maden (tünel ve mağara); kamera dikleşir.</summary>
        public float MineStartZ;
        public Gumusdere.Mining.RubblePile Rubble;
        public Gumusdere.Mining.SilverVein Vein;
    }
}
