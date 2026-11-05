using System.Collections.Generic;
using Gumusdere.Navigation;
using UnityEngine;

namespace Gumusdere.World
{
    /// <summary>
    /// Gümüşdere haritasını koddan kurar. Sahneye elle hiçbir şey koymak gerekmez.
    ///
    /// Harita güneyden kuzeye (z ekseni) şöyle dizilir:
    ///   z -26..18 : Çiftlik ve köy evleri (başlangıç)
    ///   z  18..26 : Nehir (kalkık asma köprü x=0'da, sığ geçit x=-19'da)
    ///   z  26..42 : Mera, inekler ve köprü vinci
    ///   z  42..70 : Göl (iskele), orman, meyve çalıları
    ///   z  70..80 : Dağ ve kilitli maden kapısı
    /// </summary>
    public sealed class WorldBuilder
    {
        private const float RiverSouth = 18f;
        private const float RiverNorth = 26f;
        private const int IgnoreRaycastLayer = 2;

        private readonly WorldLayout layout = new();
        private Transform world;

        public WorldLayout Build(Transform parent)
        {
            world = new GameObject("World").transform;
            world.SetParent(parent, false);

            layout.NavBounds = new Bounds(new Vector3(0f, 0f, 37f), new Vector3(90f, 16f, 140f));
            layout.PlayerOnlyWalls = Zone("PlayerOnlyWalls");

            BuildGround();
            BuildFarm();
            BuildVillageHouses();
            BuildRiver();
            BuildMeadow();
            BuildLakeAndForest();
            BuildMineEntrance();
            BuildMineInterior();
            BuildBorders();
            BuildMapFeatures();

            // Görünmez oyuncu duvarları "Ignore Raycast" katmanında: Cemal'in sensörü onları engel sanmasın.
            foreach (Transform t in layout.PlayerOnlyWalls.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = IgnoreRaycastLayer;
            }

            layout.PlayerSpawn = new Vector3(0f, 0.1f, -8f);
            layout.CompanionSpawn = new Vector3(2f, 0f, -10f);
            return layout;
        }

        private Transform Zone(string name)
        {
            var zone = new GameObject(name).transform;
            zone.SetParent(world, false);
            return zone;
        }

        // ------------------------------------------------------------------ Zemin

        private void BuildGround()
        {
            Transform zone = Zone("Zemin");
            Shapes.Box("Zemin_Guney", zone, new Vector3(0f, -0.5f, -4f), new Vector3(80f, 1f, 44f), Palette.Grass);
            Shapes.Box("Zemin_Kuzey", zone, new Vector3(0f, -0.5f, 53f), new Vector3(80f, 1f, 54f), Palette.Grass);

            // Patika: çiftlikten köprüye, köprüden madene.
            Shapes.Box("Patika_Guney", zone, new Vector3(1f, 0.01f, 5f), new Vector3(2.4f, 0.02f, 25f), Palette.Dirt, false);
            Shapes.Box("Patika_Kuzey", zone, new Vector3(0f, 0.01f, 49f), new Vector3(2.4f, 0.02f, 46f), Palette.Dirt, false);
        }

        // ------------------------------------------------------------------ Çiftlik

        private void BuildFarm()
        {
            Transform zone = Zone("Ciftlik");

            House(zone, new Vector3(-14f, 0f, -12f), new Vector3(8f, 3f, 6f), Palette.Roof);

            // 4 x 3 tarla karesi. İleride ekim/sulama bu noktalarda yapılacak.
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    var p = new Vector3(6f + i * 2f, 0.05f, -12f + j * 2f);
                    GameObject tile = Shapes.Box($"Tarla_{i}_{j}", zone, p, new Vector3(1.7f, 0.1f, 1.7f), Palette.Soil, false);
                    layout.FarmTiles.Add(p);

                    var plot = tile.AddComponent<Gumusdere.Farming.FarmPlot>();
                    plot.Initialize(tile.GetComponent<Renderer>(), zone);
                    layout.PlayerInteractables.Add(plot);
                    layout.Plots.Add(plot);
                }
            }

            // Tarlanın çiti; kuzey tarafında x=8..10 arası giriş boşluğu var.
            Fence(zone, new Vector3(4f, 0f, -14f), new Vector3(14f, 0f, -14f));
            Fence(zone, new Vector3(4f, 0f, -14f), new Vector3(4f, 0f, -6f));
            Fence(zone, new Vector3(14f, 0f, -14f), new Vector3(14f, 0f, -6f));
            Fence(zone, new Vector3(4f, 0f, -6f), new Vector3(8f, 0f, -6f));
            Fence(zone, new Vector3(10f, 0f, -6f), new Vector3(14f, 0f, -6f));

            // Kuyu: Cemal sulama görevinde kovayı buradan alır (kuyunun kuzey tarafında durur).
            GameObject well = Shapes.Cylinder("Kuyu", zone, new Vector3(-4f, 0.5f, -14f), new Vector3(1.6f, 0.5f, 1.6f), Palette.Stone);
            Shapes.Cylinder("Kuyu_Su", zone, new Vector3(-4f, 1.01f, -14f), new Vector3(1.2f, 0.01f, 1.2f), Palette.Water, false);
            layout.Watering = well.AddComponent<Gumusdere.Farming.WateringJob>();
            layout.Watering.Initialize(layout.Plots, new Vector3(-4f, 0f, -12.5f), new Vector3(9f, 0f, -10f));

            Tree(zone, new Vector3(-30f, 0f, -20f), 1.1f);
            Tree(zone, new Vector3(-34f, 0f, -8f), 1.0f, true);
            Tree(zone, new Vector3(-28f, 0f, 6f), 1.2f);
            Tree(zone, new Vector3(-10f, 0f, 13f), 0.9f, true);
            Tree(zone, new Vector3(12f, 0f, 13f), 1.0f);
            Tree(zone, new Vector3(34f, 0f, 12f), 1.2f, true);
            Tree(zone, new Vector3(36f, 0f, -20f), 1.0f);
            Tree(zone, new Vector3(-20f, 0f, -22f), 0.9f, true);
        }

        private void BuildVillageHouses()
        {
            Transform zone = Zone("Koy_Evleri");
            House(zone, new Vector3(22f, 0f, 2f), new Vector3(5f, 2.5f, 4f), Palette.RoofBlue);
            House(zone, new Vector3(29f, 0f, -6f), new Vector3(5f, 2.5f, 4f), Palette.Roof);
            House(zone, new Vector3(21f, 0f, -14f), new Vector3(6f, 2.8f, 4.5f), Palette.RoofBlue);
            BuildShop(zone, new Vector3(17f, 0f, -3f));
            BuildQuestBoard(zone, new Vector3(4.5f, 0f, 2f));
        }

        /// <summary>Köy meydanındaki ilan panosu (patikanın doğusunda, oyuncu güneyinden okur).</summary>
        private void BuildQuestBoard(Transform parent, Vector3 c)
        {
            var root = new GameObject("Ilan_Panosu").transform;
            root.SetParent(parent, false);
            root.position = c;
            var body = root.gameObject.AddComponent<BoxCollider>();
            body.center = new Vector3(0f, 1f, 0f);
            body.size = new Vector3(2.4f, 2f, 0.3f);

            Shapes.Box("Pano_Direk", root, new Vector3(-1.1f, 1f, 0f), new Vector3(0.15f, 2f, 0.15f), Palette.DarkWood, false);
            Shapes.Box("Pano_Direk", root, new Vector3(1.1f, 1f, 0f), new Vector3(0.15f, 2f, 0.15f), Palette.DarkWood, false);
            Shapes.Box("Pano", root, new Vector3(0f, 1.35f, 0f), new Vector3(2.2f, 1.1f, 0.1f), Palette.Wood, false);
            Shapes.Box("Pano_Cati", root, new Vector3(0f, 2.05f, 0f), new Vector3(2.6f, 0.12f, 0.5f), Palette.Roof, false);
            float[] xs = { -0.7f, -0.1f, 0.55f, -0.4f, 0.3f };
            float[] ys = { 1.55f, 1.6f, 1.5f, 1.12f, 1.1f };
            for (int i = 0; i < xs.Length; i++)
            {
                Shapes.Box("Ilan_Kagidi", root, new Vector3(xs[i], ys[i], -0.06f), new Vector3(0.42f, 0.32f, 0.02f), Palette.Wall, false);
            }

            layout.QuestBoard = root.gameObject.AddComponent<Gumusdere.Village.QuestBoard>();
            layout.QuestBoard.Initialize(c + new Vector3(0f, 0f, -1.3f));
            layout.PlayerInteractables.Add(layout.QuestBoard);
        }

        /// <summary>Bakkal Nazmi'nin tezgâhı: tezgâh, direkler, tente ve üstünde birkaç ürün. Müşteri güney tarafında durur.</summary>
        private void BuildShop(Transform parent, Vector3 c)
        {
            var root = new GameObject("Bakkal").transform;
            root.SetParent(parent, false);
            root.position = c;

            Shapes.Box("Tezgah", root, new Vector3(0f, 0.5f, -0.9f), new Vector3(3.2f, 1f, 0.7f), Palette.Wood);
            Shapes.Box("Tezgah_Ust", root, new Vector3(0f, 1.03f, -0.9f), new Vector3(3.3f, 0.06f, 0.8f), Palette.DarkWood, false);
            foreach (float x in new[] { -1.5f, 1.5f })
            {
                Shapes.Box("Direk", root, new Vector3(x, 1.3f, -1.2f), new Vector3(0.15f, 2.6f, 0.15f), Palette.DarkWood);
                Shapes.Box("Direk", root, new Vector3(x, 1.3f, 1.0f), new Vector3(0.15f, 2.6f, 0.15f), Palette.DarkWood);
            }
            Shapes.Box("Arka_Raf", root, new Vector3(0f, 0.9f, 1.0f), new Vector3(3.0f, 1.8f, 0.4f), Palette.Wood);

            // Çizgili tente (kırmızı-beyaz)
            for (int i = 0; i < 6; i++)
            {
                Color stripe = i % 2 == 0 ? Palette.Roof : Palette.Wall;
                Shapes.Box("Tente", root, new Vector3(-1.5f + i * 0.6f, 2.65f, -0.1f), new Vector3(0.6f, 0.1f, 2.6f), stripe, false);
            }
            Shapes.Box("Tente_Sacak", root, new Vector3(0f, 2.5f, -1.4f), new Vector3(3.6f, 0.25f, 0.05f), Palette.Roof, false);

            // Tezgâhtaki ürünler
            Shapes.Sphere("Urun_Balkabagi", root, new Vector3(-1.0f, 1.25f, -0.9f), new Vector3(0.45f, 0.38f, 0.45f), Palette.Pumpkin, false);
            Shapes.Sphere("Urun_Bogurtlen", root, new Vector3(-0.45f, 1.12f, -0.95f), Vector3.one * 0.18f, Palette.Berry, false);
            Shapes.Sphere("Urun_Baret", root, new Vector3(0.45f, 1.18f, -0.9f), new Vector3(0.42f, 0.26f, 0.42f), Palette.Helmet, false);
            Shapes.Box("Urun_Fener", root, new Vector3(1.05f, 1.2f, -0.9f), new Vector3(0.18f, 0.28f, 0.18f), Palette.Hay, false);

            layout.ShopkeeperSpot = c + new Vector3(0f, 0f, 0.1f);
            layout.Shop = root.gameObject.AddComponent<Gumusdere.Economy.ShopStall>();
            layout.Shop.Initialize(c + new Vector3(0f, 0f, -1.9f));
            layout.PlayerInteractables.Add(layout.Shop);
        }

        // ------------------------------------------------------------------ Nehir ve köprü

        private void BuildRiver()
        {
            Transform zone = Zone("Nehir");
            float riverCenter = (RiverSouth + RiverNorth) * 0.5f;
            float riverWidth = RiverNorth - RiverSouth;

            // Derin su: sadece görüntü. Altında zemin yok, yani NavMesh de yok.
            Shapes.Box("Nehir_Su", zone, new Vector3(0f, -1.6f, riverCenter), new Vector3(80f, 0.2f, riverWidth), Palette.Water, false);

            // Sığ geçit: zemin seviyesinde bir şerit. NavMesh'te "ShallowWater" alanı olur.
            GameObject ford = Shapes.Box("Sig_Gecit", zone, new Vector3(-19f, -0.5f, riverCenter), new Vector3(4f, 1f, riverWidth), Palette.ShallowWater);
            layout.Ford = ford.transform;
            layout.NavAreaOverrides.Add((ford.transform, NavAreas.ShallowWater));
            Shapes.Cylinder("Gecit_Tasi", zone, new Vector3(-19.8f, 0.03f, 19.6f), new Vector3(0.9f, 0.03f, 0.9f), Palette.Stone, false);
            Shapes.Cylinder("Gecit_Tasi", zone, new Vector3(-18.4f, 0.03f, 21.4f), new Vector3(1.0f, 0.03f, 1.0f), Palette.Stone, false);
            Shapes.Cylinder("Gecit_Tasi", zone, new Vector3(-19.6f, 0.03f, 23.0f), new Vector3(0.8f, 0.03f, 0.8f), Palette.Stone, false);
            Shapes.Cylinder("Gecit_Tasi", zone, new Vector3(-18.6f, 0.03f, 24.6f), new Vector3(0.9f, 0.03f, 0.9f), Palette.Stone, false);

            // Asma köprü: menteşe kuzey kıyıda, köprü başta kalkık.
            var hinge = new GameObject("Kopru_Mentese").transform;
            hinge.SetParent(zone, false);
            hinge.localPosition = new Vector3(0f, 0f, RiverNorth);
            Shapes.Box("Kopru", hinge, new Vector3(0f, -0.15f, -riverWidth * 0.5f), new Vector3(4f, 0.3f, riverWidth + 0.2f), Palette.Wood);
            for (int k = 0; k < 4; k++)
            {
                Shapes.Box("Kopru_Tahta", hinge, new Vector3(0f, 0.01f, -1f - k * 2f), new Vector3(4.05f, 0.05f, 0.15f), Palette.DarkWood, false);
            }

            Shapes.Box("Kopru_Direk", zone, new Vector3(-2.4f, 1f, RiverSouth - 0.6f), new Vector3(0.4f, 2f, 0.4f), Palette.DarkWood);
            Shapes.Box("Kopru_Direk", zone, new Vector3(2.4f, 1f, RiverSouth - 0.6f), new Vector3(0.4f, 2f, 0.4f), Palette.DarkWood);
            Shapes.Box("Kopru_Direk", zone, new Vector3(-2.4f, 1.5f, RiverNorth + 0.6f), new Vector3(0.4f, 3f, 0.4f), Palette.DarkWood);
            Shapes.Box("Kopru_Direk", zone, new Vector3(2.4f, 1.5f, RiverNorth + 0.6f), new Vector3(0.4f, 3f, 0.4f), Palette.DarkWood);

            // Sadece oyuncuyu durduran kıyı duvarları (NavMesh bunları görmez).
            Transform walls = layout.PlayerOnlyWalls;
            Shapes.InvisibleBox("Kiyi_Guney_Bati", walls, new Vector3(-21f, 1f, RiverSouth - 0.25f), new Vector3(38f, 2f, 0.5f));
            Shapes.InvisibleBox("Kiyi_Guney_Dogu", walls, new Vector3(21f, 1f, RiverSouth - 0.25f), new Vector3(38f, 2f, 0.5f));
            Shapes.InvisibleBox("Kiyi_Kuzey_Bati", walls, new Vector3(-21f, 1f, RiverNorth + 0.25f), new Vector3(38f, 2f, 0.5f));
            Shapes.InvisibleBox("Kiyi_Kuzey_Dogu", walls, new Vector3(21f, 1f, RiverNorth + 0.25f), new Vector3(38f, 2f, 0.5f));
            GameObject southGate = Shapes.InvisibleBox("Kopru_Kapisi_Guney", walls, new Vector3(0f, 1f, RiverSouth - 0.25f), new Vector3(4f, 2f, 0.5f));
            GameObject northGate = Shapes.InvisibleBox("Kopru_Kapisi_Kuzey", walls, new Vector3(0f, 1f, RiverNorth + 0.25f), new Vector3(4f, 2f, 0.5f));

            layout.Bridge = hinge.gameObject.AddComponent<Drawbridge>();
            layout.Bridge.Initialize(new[] { southGate, northGate });

            // Vinç: köprüyü indiren kol, karşı (kuzey) kıyıda. Cemal'in görev hedefi.
            var winchRoot = new GameObject("Vinc").transform;
            winchRoot.SetParent(zone, false);
            winchRoot.localPosition = new Vector3(6f, 0f, 29.5f);
            Shapes.Box("Vinc_Govde", winchRoot, new Vector3(0f, 0.4f, 0f), new Vector3(1.2f, 0.8f, 1.2f), Palette.Stone);
            Shapes.Cylinder("Vinc_Teker", winchRoot, new Vector3(0.7f, 0.9f, 0f), new Vector3(0.9f, 0.06f, 0.9f), Palette.DarkWood, false, Quaternion.Euler(0f, 0f, 90f));
            Transform lever = Shapes.Cylinder("Vinc_Kol", winchRoot, new Vector3(0f, 1.25f, 0f), new Vector3(0.12f, 0.45f, 0.12f), Palette.Lever, false, Quaternion.Euler(0f, 0f, 25f)).transform;

            layout.Winch = winchRoot.gameObject.AddComponent<Gumusdere.Interaction.Winch>();
            layout.Winch.Initialize(layout.Bridge, lever,
                standPoint: winchRoot.position + new Vector3(0f, 0f, -1.3f),
                bridgeSouthEnd: new Vector3(0f, 0f, RiverSouth - 1.5f));
        }

        // ------------------------------------------------------------------ Mera

        private void BuildMeadow()
        {
            Transform zone = Zone("Mera");
            layout.CowPasture = new Rect(-8f, 28f, 22f, 12f);

            Shapes.Cylinder("Saman", zone, new Vector3(-10f, 0.6f, 38f), new Vector3(1.2f, 0.6f, 1.2f), Palette.Hay, true, Quaternion.Euler(0f, 0f, 90f));
            Shapes.Cylinder("Saman", zone, new Vector3(16f, 0.6f, 33f), new Vector3(1.2f, 0.6f, 1.2f), Palette.Hay, true, Quaternion.Euler(90f, 0f, 0f));
            Shapes.Box("Yemlik", zone, new Vector3(-4f, 0.35f, 41f), new Vector3(2.4f, 0.7f, 0.8f), Palette.Wood);
            BuildBarn(zone);
        }

        /// <summary>
        /// Çoban Rıfat Amca'nın ahırı: çitle çevrili ağıl + arkada damlı bir ahır binası, batı tarafında kapı.
        /// İnekler başıboş gezer; Cemal onları buraya toplar. Karşılığında kirişler ağılın kuzeyinde belirir.
        /// </summary>
        private void BuildBarn(Transform zone)
        {
            var root = new GameObject("Ahir").transform;
            root.SetParent(zone, false);
            const float x0 = 21f, x1 = 29f, z0 = 28f, z1 = 34f;

            // Ahır binası (doğu kenarı)
            Shapes.Box("Ahir_Bina", root, new Vector3(28f, 1.4f, 31f), new Vector3(2.2f, 2.8f, 6f), Palette.Roof);
            Shapes.Box("Ahir_Cati", root, new Vector3(28f, 3.0f, 31f), new Vector3(2.8f, 0.4f, 6.6f), Palette.DarkWood, false);
            Shapes.Box("Ahir_Kapisi", root, new Vector3(26.88f, 1.0f, 31f), new Vector3(0.06f, 2f, 1.8f), Palette.DarkWood, false);

            // Ağıl çiti; batıda z 30..32.4 arası kapı boşluğu
            Fence(root, new Vector3(x0, 0f, z0), new Vector3(26.9f, 0f, z0));
            Fence(root, new Vector3(x0, 0f, z1), new Vector3(26.9f, 0f, z1));
            Fence(root, new Vector3(x0, 0f, z0), new Vector3(x0, 0f, 30f));
            Fence(root, new Vector3(x0, 0f, 32.4f), new Vector3(x0, 0f, z1));
            Shapes.Box("Yemlik", root, new Vector3(25.5f, 0.35f, 33.2f), new Vector3(2f, 0.7f, 0.6f), Palette.Wood);
            Shapes.Cylinder("Saman", root, new Vector3(23f, 0.5f, 28.9f), new Vector3(1f, 0.5f, 1f), Palette.Hay, true, Quaternion.Euler(0f, 0f, 90f));

            layout.BarnPen = Rect.MinMaxRect(x0 + 1f, z0 + 1f, 26f, z1 - 1.2f);
            layout.BarnGate = new Vector3(x0, 0f, 31.2f);
            layout.FarmerSpot = new Vector3(19.6f, 0f, 29.4f);

            var gate = new GameObject("Ahir_Kapisi_Gorev").transform;
            gate.SetParent(root, false);
            gate.position = layout.BarnGate;
            layout.Herd = gate.gameObject.AddComponent<Gumusdere.Village.HerdCowsJob>();

            // Ödül: destek kirişleri (inekler toplanınca görünür)
            var pile = new GameObject("Kiris_Yigini").transform;
            pile.SetParent(root, false);
            pile.position = new Vector3(24f, 0f, 36.4f);
            var pileBody = pile.gameObject.AddComponent<BoxCollider>();
            pileBody.center = new Vector3(0f, 0.4f, 0f);
            pileBody.size = new Vector3(2.8f, 0.8f, 1.2f);
            for (int i = 0; i < 3; i++)
            {
                Shapes.Box("Kiris", pile, new Vector3(0f, 0.14f + (i == 2 ? 0.27f : 0f), (i == 2 ? 0f : (i - 0.5f) * 0.4f)),
                    new Vector3(2.6f, 0.26f, 0.26f), Palette.Wood, false);
            }
            pile.gameObject.SetActive(false);
            layout.BeamPile = pile;
            layout.BeamPileStand = pile.position + new Vector3(0f, 0f, -1.4f);
        }

        // ------------------------------------------------------------------ Göl ve orman

        private void BuildLakeAndForest()
        {
            Transform zone = Zone("Gol_ve_Orman");
            var lakeCenter = new Vector3(-20f, 0f, 48f);

            Shapes.Cylinder("Gol_Kiyi", zone, lakeCenter + new Vector3(0f, 0.015f, 0f), new Vector3(15f, 0.015f, 12f), Palette.Dirt, false);
            Shapes.Cylinder("Gol_Su", zone, lakeCenter + new Vector3(0f, 0.03f, 0f), new Vector3(14f, 0.03f, 11f), Palette.Water, false);

            // Gölün görünmez engeli herkesi durdurur; üstü NavMesh'te "yürünmez" sayılır.
            GameObject lakeBlocker = Shapes.InvisibleCylinder("Gol_Engel", zone, lakeCenter, new Vector3(12.4f, 1.5f, 9.4f));
            layout.NavAreaOverrides.Add((lakeBlocker.transform, NavAreas.NotWalkable));

            // İskele: gölün doğu kıyısından içeri uzanır. Balık burada tutulacak.
            Shapes.Box("Iskele", zone, new Vector3(-12.2f, 0.1f, 48f), new Vector3(3.6f, 0.2f, 1.6f), Palette.Wood);
            Shapes.Cylinder("Iskele_Ayak", zone, new Vector3(-13.6f, 0.1f, 47.3f), new Vector3(0.2f, 0.3f, 0.2f), Palette.DarkWood, false);
            Shapes.Cylinder("Iskele_Ayak", zone, new Vector3(-13.6f, 0.1f, 48.7f), new Vector3(0.2f, 0.3f, 0.2f), Palette.DarkWood, false);
            layout.FishingSpot = new Vector3(-13.2f, 0.2f, 48f);
            var fishingRoot = new GameObject("Balik_Noktasi");
            fishingRoot.transform.SetParent(zone, false);
            fishingRoot.transform.position = layout.FishingSpot;
            layout.Fishing = fishingRoot.AddComponent<Gumusdere.Fishing.FishingSpot>();
            layout.Fishing.Initialize(new Vector3(-16.5f, 0.05f, 48f));
            layout.PlayerInteractables.Add(layout.Fishing);

            // Meyve çalıları
            layout.PlayerInteractables.Add(Bush(zone, new Vector3(14f, 0f, 52f)));
            layout.PlayerInteractables.Add(Bush(zone, new Vector3(18f, 0f, 57f)));
            layout.PlayerInteractables.Add(Bush(zone, new Vector3(10f, 0f, 60f)));

            // Mantar yerleri (orman içinde, ağaç gövdelerinden uzak)
            float[,] mushrooms =
            {
                { -34f, 33f }, { -30f, 41f }, { -35f, 52f }, { -29f, 63f }, { -14f, 64f },
                { -10f, 58f }, { -24f, 57f }, { 27f, 49f }, { 31f, 61f }, { 22f, 64f }
            };
            for (int i = 0; i < mushrooms.GetLength(0); i++)
            {
                var spot = new GameObject("Mantar_Yeri");
                spot.transform.SetParent(zone, false);
                spot.transform.localPosition = new Vector3(mushrooms[i, 0], 0f, mushrooms[i, 1]);
                var mushroom = spot.AddComponent<Gumusdere.Farming.MushroomSpot>();
                mushroom.Initialize();
                layout.PlayerInteractables.Add(mushroom);
            }

            // Orman
            float[,] trees =
            {
                { -36f, 30f }, { -33f, 36f }, { -37f, 43f }, { -34f, 58f }, { -37f, 64f }, { -31f, 68f },
                { -28f, 38f }, { -27f, 60f }, { -12f, 62f }, { -16f, 66f }, { -8f, 56f },
                { 26f, 46f }, { 32f, 50f }, { 36f, 44f }, { 29f, 58f }, { 35f, 63f }, { 24f, 66f },
                { 20f, 36f }, { 33f, 34f }, { 12f, 66f }
            };
            for (int i = 0; i < trees.GetLength(0); i++)
            {
                Tree(zone, new Vector3(trees[i, 0], 0f, trees[i, 1]), 0.9f + (i % 3) * 0.15f, i % 2 == 0);
            }
        }

        // ------------------------------------------------------------------ Harita (M)

        /// <summary>M ile açılan haritanın bölgeleri. Koordinatlar yukarıdaki yapım kodlarıyla aynıdır.</summary>
        private void BuildMapFeatures()
        {
            layout.MapBounds = new Rect(-40f, -26f, 80f, 131f);
            List<MapFeature> f = layout.MapFeatures;
            Color none = new(0f, 0f, 0f, 0f);

            // Su ve geçişler
            f.Add(MapFeature.At("Nehir", 26f, 22f, 28f, 8f, Palette.Water));
            f.Add(MapFeature.At("", -2f, 22f, 76f, 8f, Palette.Water));
            f.Add(MapFeature.At("Sığ geçit", -19f, 22f, 4f, 8f, Palette.ShallowWater));
            f.Add(MapFeature.At("Köprü", 0f, 22f, 4f, 8f, Palette.Wood));
            f.Add(MapFeature.At("Göl", -20f, 48f, 15f, 12f, Palette.Water, true));
            f.Add(MapFeature.At("İskele", -11.5f, 48f, 3.6f, 1.6f, Palette.Wood));

            // Patikalar
            f.Add(MapFeature.At("", 1f, 5f, 2.4f, 25f, Palette.Dirt));
            f.Add(MapFeature.At("", 0f, 49f, 2.4f, 46f, Palette.Dirt));

            // Çiftlik ve köy
            f.Add(MapFeature.At("Tarla", 9f, -10f, 10f, 8f, Palette.Soil));
            f.Add(MapFeature.At("Evin", -14f, -12f, 8f, 6f, Palette.Roof));
            f.Add(MapFeature.At("Kuyu", -4f, -14f, 2.2f, 2.2f, Palette.Stone, true));
            f.Add(MapFeature.At("", 22f, 2f, 5f, 4f, Palette.RoofBlue));
            f.Add(MapFeature.At("", 29f, -6f, 5f, 4f, Palette.Roof));
            f.Add(MapFeature.At("", 21f, -14f, 6f, 4.5f, Palette.RoofBlue));
            f.Add(MapFeature.At("Köy", 25f, -9f, 0f, 0f, none));
            f.Add(MapFeature.At("Bakkal", 17f, -3f, 3.6f, 2.6f, Palette.Roof));
            f.Add(MapFeature.At("İlan panosu", 4.5f, 2f, 2.4f, 0.8f, Palette.Wall));
            f.Add(MapFeature.At("Ahır", 25f, 31f, 8f, 6f, Palette.Dirt));
            f.Add(MapFeature.At("", 28f, 31f, 2.2f, 6f, Palette.Roof));
            f.Add(MapFeature.At("Kiriş yeri", -6.5f, 69.6f, 3f, 1.2f, Palette.Dirt));

            // Mera
            Rect pasture = layout.CowPasture;
            f.Add(new MapFeature("Mera", pasture, Palette.GrassDark));
            f.Add(MapFeature.At("Vinç", 6f, 29.5f, 1.6f, 1.6f, Palette.Lever));

            // Orman, çalılar
            f.Add(MapFeature.At("Orman", -32f, 50f, 0f, 0f, none));
            f.Add(MapFeature.At("Orman", 30f, 55f, 0f, 0f, none));
            float[,] trees =
            {
                { -36f, 30f }, { -33f, 36f }, { -37f, 43f }, { -34f, 58f }, { -37f, 64f }, { -31f, 68f },
                { -28f, 38f }, { -27f, 60f }, { -12f, 62f }, { -16f, 66f }, { -8f, 56f },
                { 26f, 46f }, { 32f, 50f }, { 36f, 44f }, { 29f, 58f }, { 35f, 63f }, { 24f, 66f },
                { 20f, 36f }, { 33f, 34f }, { 12f, 66f }
            };
            for (int i = 0; i < trees.GetLength(0); i++)
            {
                f.Add(MapFeature.At("", trees[i, 0], trees[i, 1], 4f, 4f, Palette.Leaf, true));
            }
            f.Add(MapFeature.At("Böğürtlen", 14f, 52f, 1.8f, 1.8f, Palette.Berry, true));
            f.Add(MapFeature.At("", 18f, 57f, 1.8f, 1.8f, Palette.Berry, true));
            f.Add(MapFeature.At("", 10f, 60f, 1.8f, 1.8f, Palette.Berry, true));

            // Dağ ve maden
            f.Add(MapFeature.At("", 0f, 92.5f, 80f, 25f, Palette.Rock));
            f.Add(MapFeature.At("Dağ", -21f, 76f, 38f, 8f, Palette.Rock));
            f.Add(MapFeature.At("", 21f, 76f, 38f, 8f, Palette.Rock));
            f.Add(MapFeature.At("", 0f, 76f, 4f, 8f, Palette.CaveFloor));
            f.Add(MapFeature.At("Maden kapısı", 0f, 72.3f, 4f, 0.8f, Palette.DarkWood));
            f.Add(MapFeature.At("Maden", 0f, 87f, 24f, 14f, Palette.CaveFloor));
            f.Add(MapFeature.At("", 0f, 99.5f, 24f, 9f, Palette.CaveFloor));
            f.Add(MapFeature.At("", -7f, 95f, 11f, 1f, Palette.CaveWall));
            f.Add(MapFeature.At("", 7f, 95f, 11f, 1f, Palette.CaveWall));
            f.Add(MapFeature.At("Göçük", 0f, 95f, 3f, 1.6f, Palette.Stone));
            f.Add(MapFeature.At("Gümüş", 0f, 101.5f, 2.2f, 1.8f, Palette.Silver, true));
        }

        // ------------------------------------------------------------------ Maden girişi

        private void BuildMineEntrance()
        {
            Transform zone = Zone("Maden_Girisi");

            Shapes.Box("Dag_Bati", zone, new Vector3(-22f, 4f, 76f), new Vector3(36f, 8f, 8f), Palette.Rock);
            Shapes.Box("Dag_Dogu", zone, new Vector3(22f, 4f, 76f), new Vector3(36f, 8f, 8f), Palette.Rock);
            Shapes.Box("Dag_Sol", zone, new Vector3(-3f, 2f, 76f), new Vector3(2f, 4f, 8f), Palette.Rock);
            Shapes.Box("Dag_Sag", zone, new Vector3(3f, 2f, 76f), new Vector3(2f, 4f, 8f), Palette.Rock);
            Shapes.Sphere("Kaya", zone, new Vector3(-12f, 8f, 75f), new Vector3(9f, 5f, 7f), Palette.Stone, false);
            Shapes.Sphere("Kaya", zone, new Vector3(14f, 8.5f, 76f), new Vector3(11f, 6f, 7f), Palette.Stone, false);

            // Tünelin zemini (üstü açık bir yarık; yukarıdan bakan kamera içeriyi görebilsin)
            Shapes.Box("Tunel_Zemin", zone, new Vector3(0f, 0.01f, 76f), new Vector3(4f, 0.02f, 8f), Palette.CaveFloor, false);

            // Kapı çerçevesi ve kilitli kapı
            Shapes.Box("Kapi_Direk", zone, new Vector3(-1.85f, 1.75f, 72.1f), new Vector3(0.4f, 3.5f, 0.5f), Palette.DarkWood);
            Shapes.Box("Kapi_Direk", zone, new Vector3(1.85f, 1.75f, 72.1f), new Vector3(0.4f, 3.5f, 0.5f), Palette.DarkWood);
            Shapes.Box("Kapi_Ust", zone, new Vector3(0f, 3.7f, 72.1f), new Vector3(4.1f, 0.5f, 0.6f), Palette.DarkWood);
            GameObject door = Shapes.Box("Maden_Kapisi", zone, new Vector3(0f, 1.6f, 72.3f), new Vector3(3.3f, 3.2f, 0.25f), Palette.Wood);
            Shapes.Box("Kapi_Kusak", door.transform, new Vector3(0f, 0.25f, -0.6f), new Vector3(1.02f, 0.06f, 0.4f), Palette.DarkWood, false);
            Shapes.Box("Kapi_Kusak", door.transform, new Vector3(0f, -0.25f, -0.6f), new Vector3(1.02f, 0.06f, 0.4f), Palette.DarkWood, false);
            layout.MineDoor = door.transform;
            layout.Door = door.AddComponent<MineDoor>();

            // Fener
            Shapes.Cylinder("Fener_Direk", zone, new Vector3(-3.2f, 1.1f, 70.8f), new Vector3(0.15f, 1.1f, 0.15f), Palette.DarkWood);
            Shapes.Sphere("Fener", zone, new Vector3(-3.2f, 2.35f, 70.8f), new Vector3(0.45f, 0.45f, 0.45f),
                Gumusdere.Ambience.Ambient.Hdr(new Color(1f, 0.8f, 0.45f), 3.5f), false);
            var lanternLight = new GameObject("Fener_Isik").AddComponent<Light>();
            lanternLight.transform.SetParent(zone, false);
            lanternLight.transform.localPosition = new Vector3(-3.2f, 2.4f, 70.4f);
            lanternLight.type = LightType.Point;
            lanternLight.color = new Color(1f, 0.8f, 0.5f);
            lanternLight.range = 7f;
            lanternLight.intensity = 2f;

            layout.GuardSpot = new Vector3(3.5f, 0f, 69.5f);
            BuildBeamJob(zone);
        }

        /// <summary>
        /// Destek kirişi görevi: Rıfat Amca'nın ahırının önündeki kirişler ve madenin ağzında kirişlerin
        /// bırakılacağı yer. Cemal ikisi arasında gidip gelir.
        /// </summary>
        private void BuildBeamJob(Transform mineZone)
        {
            // Kirişler Rıfat Amca'nın ahırından gelir (BuildBarn).
            Transform pile = layout.BeamPile;

            // Madenin ağzındaki kiriş yeri (kapının batısı)
            var stock = new GameObject("Kiris_Yeri").transform;
            stock.SetParent(mineZone, false);
            stock.position = new Vector3(-6.5f, 0f, 69.6f);
            Shapes.Box("Kiris_Yeri_Isaret", stock, new Vector3(0f, 0.015f, 0f), new Vector3(3f, 0.03f, 1.2f), Palette.Dirt, false);
            var placed = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < Gumusdere.Mining.MineEntryRule.RequiredBeams; i++)
            {
                placed.Add(Shapes.Box("Destek_Kirisi", stock, new Vector3(0f, 0.14f + i * 0.27f, (i - 1) * 0.1f),
                    new Vector3(2.6f, 0.26f, 0.26f), Palette.Wood, false));
            }

            layout.Beams = stock.gameObject.AddComponent<Gumusdere.Mining.BeamHaulJob>();
            layout.Beams.Initialize(pile, layout.BeamPileStand, stock.position + new Vector3(0f, 0f, -1.4f), placed);
        }

        // ------------------------------------------------------------------ Maden içi (final)

        /// <summary>
        /// Tünelin arkasındaki mağara (z 80..104). Ortadaki duvarın geçidini göçük taşları kapatır;
        /// Cemal temizleyince iç odadaki gümüş damarına ulaşılır.
        /// </summary>
        private void BuildMineInterior()
        {
            Transform zone = Zone("Maden_Ici");
            layout.MineStartZ = 71f;

            Shapes.Box("Magara_Zemin", zone, new Vector3(0f, -0.5f, 92f), new Vector3(24f, 1f, 24f), Palette.CaveFloor);

            // Dış duvarlar (girişte x=-2..2 arası boşluk)
            Shapes.Box("Magara_Duvar_Bati", zone, new Vector3(-12.5f, 2f, 92f), new Vector3(1f, 4f, 24f), Palette.CaveWall);
            Shapes.Box("Magara_Duvar_Dogu", zone, new Vector3(12.5f, 2f, 92f), new Vector3(1f, 4f, 24f), Palette.CaveWall);
            Shapes.Box("Magara_Duvar_Kuzey", zone, new Vector3(0f, 2f, 104.5f), new Vector3(26f, 4f, 1f), Palette.CaveWall);
            Shapes.Box("Magara_Duvar_Guney", zone, new Vector3(-7.5f, 2f, 80.5f), new Vector3(11f, 4f, 1f), Palette.CaveWall);
            Shapes.Box("Magara_Duvar_Guney", zone, new Vector3(7.5f, 2f, 80.5f), new Vector3(11f, 4f, 1f), Palette.CaveWall);

            // İç oda ile dış oda arasındaki duvar; ortada 3 m'lik geçit
            Shapes.Box("Ara_Duvar", zone, new Vector3(-7f, 2f, 95f), new Vector3(11f, 4f, 1f), Palette.CaveWall);
            Shapes.Box("Ara_Duvar", zone, new Vector3(7f, 2f, 95f), new Vector3(11f, 4f, 1f), Palette.CaveWall);

            // Eski maden araçları (süs)
            Shapes.Box("Vagon", zone, new Vector3(-8f, 0.5f, 86f), new Vector3(1.4f, 1f, 2.2f), Palette.DarkWood);
            Shapes.Box("Ray", zone, new Vector3(-8f, 0.02f, 88f), new Vector3(1.2f, 0.04f, 8f), Palette.Rock, false);
            Shapes.Box("Destek_Kirisi", zone, new Vector3(-2.5f, 1.8f, 82f), new Vector3(0.3f, 3.6f, 0.3f), Palette.Wood);
            Shapes.Box("Destek_Kirisi", zone, new Vector3(2.5f, 1.8f, 82f), new Vector3(0.3f, 3.6f, 0.3f), Palette.Wood);
            Shapes.Sphere("Kaya_Yigini", zone, new Vector3(8f, 0.5f, 90f), new Vector3(2.2f, 1.2f, 1.8f), Palette.Rock);

            // Göçük: geçidi tıkayan taş yığını (tek bir çarpışma kutusu + görsel taşlar)
            var rubbleRoot = new GameObject("Gocuk_Taslari").transform;
            rubbleRoot.SetParent(zone, false);
            rubbleRoot.localPosition = new Vector3(0f, 0f, 95f);
            var rubbleBody = rubbleRoot.gameObject.AddComponent<BoxCollider>();
            rubbleBody.center = new Vector3(0f, 1f, 0f);
            rubbleBody.size = new Vector3(3.2f, 2f, 1.6f);
            Shapes.Sphere("Tas", rubbleRoot, new Vector3(-0.8f, 0.5f, 0f), new Vector3(1.3f, 1f, 1.2f), Palette.Stone, false);
            Shapes.Sphere("Tas", rubbleRoot, new Vector3(0.7f, 0.55f, 0.1f), new Vector3(1.4f, 1.1f, 1.2f), Palette.Rock, false);
            Shapes.Sphere("Tas", rubbleRoot, new Vector3(0f, 1.3f, 0f), new Vector3(1.2f, 0.9f, 1.1f), Palette.Stone, false);
            layout.Rubble = rubbleRoot.gameObject.AddComponent<Gumusdere.Mining.RubblePile>();
            layout.Rubble.Initialize(new Vector3(0f, 0f, 93.3f));

            // Gümüş damarı: iç odanın dibinde, kaya kabuğunun içinde
            var veinRoot = new GameObject("Gumus_Damari").transform;
            veinRoot.SetParent(zone, false);
            veinRoot.localPosition = new Vector3(0f, 0f, 101.5f);
            Transform shell = Shapes.Sphere("Kaya_Kabuk", veinRoot, new Vector3(0f, 0.9f, 0f), new Vector3(2.2f, 1.8f, 1.6f), Palette.Rock).transform;
            Shapes.Sphere("Parilti", shell, new Vector3(0.3f, 0.2f, -0.45f), Vector3.one * 0.15f, Palette.Silver, false);
            Shapes.Sphere("Parilti", shell, new Vector3(-0.25f, -0.1f, -0.47f), Vector3.one * 0.12f, Palette.Silver, false);
            var treasure = new GameObject("Gumus");
            treasure.transform.SetParent(veinRoot, false);
            Shapes.Sphere("Gumus_Kulce", treasure.transform, new Vector3(0f, 0.6f, 0f), new Vector3(1.6f, 1.1f, 1.2f), Palette.Silver, false);
            Shapes.Sphere("Gumus_Kulce", treasure.transform, new Vector3(0.7f, 0.35f, 0.3f), new Vector3(0.7f, 0.6f, 0.6f), Palette.Silver, false);
            Shapes.Sphere("Gumus_Kulce", treasure.transform, new Vector3(-0.6f, 0.3f, -0.2f), new Vector3(0.6f, 0.5f, 0.5f), Palette.Silver, false);
            layout.Vein = veinRoot.gameObject.AddComponent<Gumusdere.Mining.SilverVein>();
            layout.Vein.Initialize(shell, treasure);
            layout.PlayerInteractables.Add(layout.Vein);

            // Çatı: madenin üstü kapalı. Kamerada görünmez (sadece gölge atar), ama güneşi keser;
            // içerisi oyuncunun feneri, Cemal'in baret lambası ve iki loş maden lambasıyla aydınlanır (MineDarkness).
            Roof(zone, new Vector3(0f, 4.3f, 92.5f), new Vector3(28f, 0.6f, 27f));
            Roof(zone, new Vector3(0f, 4.3f, 76f), new Vector3(6f, 0.6f, 9f));

            // Loş maden lambaları: biri girişte, biri gümüş damarının üstünde (soğuk mavi parıltı)
            AddLantern(zone, new Vector3(-10f, 3f, 84f), new Color(1f, 0.7f, 0.4f), 6f, 1.3f);
            AddLantern(zone, new Vector3(0f, 2.6f, 100f), new Color(0.7f, 0.82f, 1f), 5f, 1.4f);
        }

        private static void Roof(Transform parent, Vector3 center, Vector3 size)
        {
            GameObject roof = Shapes.Box("Maden_Cati", parent, center, size, Palette.CaveWall, false);
            var r = roof.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }

        private static void AddLantern(Transform parent, Vector3 position, Color color, float range, float intensity = 2.2f)
        {
            var light = new GameObject("Maden_Feneri").AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.localPosition = position;
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            Shapes.Sphere("Maden_Feneri_Cam", parent, position, Vector3.one * 0.35f, Gumusdere.Ambience.Ambient.Hdr(color, 3.5f), false);
            Shapes.Box("Maden_Feneri_Kapak", parent, position + new Vector3(0f, 0.25f, 0f), new Vector3(0.3f, 0.08f, 0.3f), Palette.DarkWood, false);
        }

        // ------------------------------------------------------------------ Sınırlar

        private void BuildBorders()
        {
            Transform walls = layout.PlayerOnlyWalls;
            Shapes.InvisibleBox("Sinir_Bati", walls, new Vector3(-40.25f, 1.5f, 27f), new Vector3(0.5f, 3f, 110f));
            Shapes.InvisibleBox("Sinir_Dogu", walls, new Vector3(40.25f, 1.5f, 27f), new Vector3(0.5f, 3f, 110f));
            Shapes.InvisibleBox("Sinir_Guney", walls, new Vector3(0f, 1.5f, -26.25f), new Vector3(81f, 3f, 0.5f));
            Shapes.InvisibleBox("Sinir_Kuzey_Bati", walls, new Vector3(-21.25f, 1.5f, 104.75f), new Vector3(38.5f, 3f, 0.5f));
            Shapes.InvisibleBox("Sinir_Kuzey_Dogu", walls, new Vector3(21.25f, 1.5f, 104.75f), new Vector3(38.5f, 3f, 0.5f));
            Shapes.InvisibleBox("Sinir_Maden", walls, new Vector3(0f, 1.5f, 104.75f), new Vector3(4f, 3f, 0.5f));
        }

        // ------------------------------------------------------------------ Yardımcılar

        private static void House(Transform parent, Vector3 basePos, Vector3 size, Color roof)
        {
            Shapes.Box("Ev", parent, basePos + new Vector3(0f, size.y * 0.5f, 0f), size, Palette.Wall);
            Shapes.Box("Cati", parent, basePos + new Vector3(0f, size.y + 0.25f, 0f), new Vector3(size.x + 0.6f, 0.5f, size.z + 0.6f), roof, false);
            Shapes.Box("Cati_Ust", parent, basePos + new Vector3(0f, size.y + 0.75f, 0f), new Vector3(size.x * 0.7f, 0.5f, size.z * 0.6f), roof, false);
            Shapes.Box("Ev_Kapi", parent, basePos + new Vector3(0f, 0.9f, -size.z * 0.5f - 0.03f), new Vector3(1.2f, 1.8f, 0.06f), Palette.DarkWood, false);
            // Akşamüstü içeride lamba yanıyor: pencere hafifçe parlar (bloom).
            Shapes.Box("Ev_Pencere", parent, basePos + new Vector3(size.x * 0.28f, size.y * 0.6f, -size.z * 0.5f - 0.03f), new Vector3(0.9f, 0.7f, 0.06f),
                Gumusdere.Ambience.Ambient.Hdr(new Color(1f, 0.78f, 0.42f), 1.8f), false);
            Shapes.Box("Ev_Pencere_Cerceve", parent, basePos + new Vector3(size.x * 0.28f, size.y * 0.6f, -size.z * 0.5f - 0.02f), new Vector3(1.05f, 0.85f, 0.04f), Palette.DarkWood, false);
        }

        private static void Tree(Transform parent, Vector3 basePos, float size, bool lightLeaves = false)
        {
            Shapes.Cylinder("Agac_Govde", parent, basePos + new Vector3(0f, size, 0f), new Vector3(0.35f, 1f, 0.35f) * size, Palette.DarkWood);
            Color leaves = lightLeaves ? Palette.LeafLight : Palette.Leaf;

            // Yapraklar gövdenin tepesindeki bir pivotun altında: rüzgârda o noktanın etrafında salınır.
            var crown = new GameObject("Agac_Tepe").transform;
            crown.SetParent(parent, false);
            crown.localPosition = basePos + new Vector3(0f, 2f * size, 0f);
            Shapes.Sphere("Agac_Yaprak", crown, new Vector3(0f, 0.7f * size, 0f), new Vector3(2.3f, 2.2f, 2.3f) * size, leaves, false);
            Shapes.Sphere("Agac_Yaprak", crown, new Vector3(0.5f, 1.4f * size, 0.3f), new Vector3(1.5f, 1.4f, 1.5f) * size, leaves, false);
            crown.gameObject.AddComponent<Gumusdere.Ambience.WindSway>().Initialize(1f / Mathf.Max(0.6f, size));
        }

        private static Gumusdere.Farming.BerryBush Bush(Transform parent, Vector3 basePos)
        {
            GameObject bush = Shapes.Sphere("Cali", parent, basePos + new Vector3(0f, 0.55f, 0f), new Vector3(1.4f, 1.1f, 1.4f), Palette.GrassDark);
            GameObject[] berries =
            {
                Shapes.Sphere("Meyve", parent, basePos + new Vector3(0.45f, 0.8f, -0.4f), Vector3.one * 0.22f, Palette.Berry, false),
                Shapes.Sphere("Meyve", parent, basePos + new Vector3(-0.35f, 0.95f, -0.45f), Vector3.one * 0.22f, Palette.Berry, false),
                Shapes.Sphere("Meyve", parent, basePos + new Vector3(0.1f, 1.05f, -0.55f), Vector3.one * 0.22f, Palette.Berry, false)
            };

            var berryBush = bush.AddComponent<Gumusdere.Farming.BerryBush>();
            berryBush.Initialize(berries);
            return berryBush;
        }

        private static void Fence(Transform parent, Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            float length = delta.magnitude;
            Vector3 mid = (from + to) * 0.5f;
            Quaternion rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);

            Shapes.Box("Cit_Ust", parent, mid + Vector3.up * 0.75f, new Vector3(0.1f, 0.12f, length), Palette.Wood, false, rotation);
            Shapes.Box("Cit_Alt", parent, mid + Vector3.up * 0.4f, new Vector3(0.1f, 0.12f, length), Palette.Wood, false, rotation);

            int posts = Mathf.Max(2, Mathf.CeilToInt(length / 2f) + 1);
            for (int i = 0; i < posts; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, i / (float)(posts - 1));
                Shapes.Box("Cit_Direk", parent, p + Vector3.up * 0.5f, new Vector3(0.18f, 1f, 0.18f), Palette.DarkWood, false);
            }

            // Görsel parçalar yerine tek bir görünmez engel: hem oyuncu hem NavMesh için.
            GameObject blocker = Shapes.InvisibleBox("Cit_Engel", parent, mid + Vector3.up * 0.5f, new Vector3(0.2f, 1f, length));
            blocker.transform.localRotation = rotation;
        }
    }
}
