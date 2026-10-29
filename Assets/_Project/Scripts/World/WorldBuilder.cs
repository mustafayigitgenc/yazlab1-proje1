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

            layout.NavBounds = new Bounds(new Vector3(0f, 0f, 27f), new Vector3(90f, 16f, 120f));
            layout.PlayerOnlyWalls = Zone("PlayerOnlyWalls");

            BuildGround();
            BuildFarm();
            BuildVillageHouses();
            BuildRiver();
            BuildMeadow();
            BuildLakeAndForest();
            BuildMineEntrance();
            BuildBorders();

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
                }
            }

            // Tarlanın çiti; kuzey tarafında x=8..10 arası giriş boşluğu var.
            Fence(zone, new Vector3(4f, 0f, -14f), new Vector3(14f, 0f, -14f));
            Fence(zone, new Vector3(4f, 0f, -14f), new Vector3(4f, 0f, -6f));
            Fence(zone, new Vector3(14f, 0f, -14f), new Vector3(14f, 0f, -6f));
            Fence(zone, new Vector3(4f, 0f, -6f), new Vector3(8f, 0f, -6f));
            Fence(zone, new Vector3(10f, 0f, -6f), new Vector3(14f, 0f, -6f));

            // Kuyu
            Shapes.Cylinder("Kuyu", zone, new Vector3(-4f, 0.5f, -14f), new Vector3(1.6f, 0.5f, 1.6f), Palette.Stone);
            Shapes.Cylinder("Kuyu_Su", zone, new Vector3(-4f, 1.01f, -14f), new Vector3(1.2f, 0.01f, 1.2f), Palette.Water, false);

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

        // ------------------------------------------------------------------ Maden girişi

        private void BuildMineEntrance()
        {
            Transform zone = Zone("Maden_Girisi");

            Shapes.Box("Dag_Bati", zone, new Vector3(-22f, 4f, 76f), new Vector3(36f, 8f, 8f), Palette.Rock);
            Shapes.Box("Dag_Dogu", zone, new Vector3(22f, 4f, 76f), new Vector3(36f, 8f, 8f), Palette.Rock);
            Shapes.Box("Dag_Sol", zone, new Vector3(-3f, 2f, 76f), new Vector3(2f, 4f, 8f), Palette.Rock);
            Shapes.Box("Dag_Sag", zone, new Vector3(3f, 2f, 76f), new Vector3(2f, 4f, 8f), Palette.Rock);
            Shapes.Box("Dag_Ust", zone, new Vector3(0f, 6f, 76f), new Vector3(8f, 4f, 8f), Palette.Rock);
            Shapes.Sphere("Kaya", zone, new Vector3(-12f, 8f, 75f), new Vector3(9f, 5f, 7f), Palette.Stone, false);
            Shapes.Sphere("Kaya", zone, new Vector3(14f, 8.5f, 76f), new Vector3(11f, 6f, 7f), Palette.Stone, false);
            Shapes.Sphere("Kaya", zone, new Vector3(0f, 8.2f, 76f), new Vector3(7f, 4f, 6f), Palette.Rock, false);

            // Tünelin karanlığı
            Shapes.Box("Tunel_Karanlik", zone, new Vector3(0f, 1.9f, 76.2f), new Vector3(3.9f, 3.8f, 6.6f), Palette.Darkness, false);

            // Kapı çerçevesi ve kilitli kapı
            Shapes.Box("Kapi_Direk", zone, new Vector3(-1.85f, 1.75f, 72.1f), new Vector3(0.4f, 3.5f, 0.5f), Palette.DarkWood);
            Shapes.Box("Kapi_Direk", zone, new Vector3(1.85f, 1.75f, 72.1f), new Vector3(0.4f, 3.5f, 0.5f), Palette.DarkWood);
            Shapes.Box("Kapi_Ust", zone, new Vector3(0f, 3.7f, 72.1f), new Vector3(4.1f, 0.5f, 0.6f), Palette.DarkWood);
            GameObject door = Shapes.Box("Maden_Kapisi", zone, new Vector3(0f, 1.6f, 72.3f), new Vector3(3.3f, 3.2f, 0.25f), Palette.Wood);
            Shapes.Box("Kapi_Kusak", door.transform, new Vector3(0f, 0.25f, -0.6f), new Vector3(1.02f, 0.06f, 0.4f), Palette.DarkWood, false);
            Shapes.Box("Kapi_Kusak", door.transform, new Vector3(0f, -0.25f, -0.6f), new Vector3(1.02f, 0.06f, 0.4f), Palette.DarkWood, false);
            layout.MineDoor = door.transform;

            // Fener
            Shapes.Cylinder("Fener_Direk", zone, new Vector3(-3.2f, 1.1f, 70.8f), new Vector3(0.15f, 1.1f, 0.15f), Palette.DarkWood);
            Shapes.Sphere("Fener", zone, new Vector3(-3.2f, 2.35f, 70.8f), new Vector3(0.45f, 0.45f, 0.45f), Palette.Hay, false);
            var lanternLight = new GameObject("Fener_Isik").AddComponent<Light>();
            lanternLight.transform.SetParent(zone, false);
            lanternLight.transform.localPosition = new Vector3(-3.2f, 2.4f, 70.4f);
            lanternLight.type = LightType.Point;
            lanternLight.color = new Color(1f, 0.8f, 0.5f);
            lanternLight.range = 7f;
            lanternLight.intensity = 2f;

            layout.GuardSpot = new Vector3(3.5f, 0f, 69.5f);
        }

        // ------------------------------------------------------------------ Sınırlar

        private void BuildBorders()
        {
            Transform walls = layout.PlayerOnlyWalls;
            Shapes.InvisibleBox("Sinir_Bati", walls, new Vector3(-40.25f, 1.5f, 27f), new Vector3(0.5f, 3f, 110f));
            Shapes.InvisibleBox("Sinir_Dogu", walls, new Vector3(40.25f, 1.5f, 27f), new Vector3(0.5f, 3f, 110f));
            Shapes.InvisibleBox("Sinir_Guney", walls, new Vector3(0f, 1.5f, -26.25f), new Vector3(81f, 3f, 0.5f));
            Shapes.InvisibleBox("Sinir_Kuzey", walls, new Vector3(0f, 1.5f, 80.25f), new Vector3(81f, 3f, 0.5f));
        }

        // ------------------------------------------------------------------ Yardımcılar

        private static void House(Transform parent, Vector3 basePos, Vector3 size, Color roof)
        {
            Shapes.Box("Ev", parent, basePos + new Vector3(0f, size.y * 0.5f, 0f), size, Palette.Wall);
            Shapes.Box("Cati", parent, basePos + new Vector3(0f, size.y + 0.25f, 0f), new Vector3(size.x + 0.6f, 0.5f, size.z + 0.6f), roof, false);
            Shapes.Box("Cati_Ust", parent, basePos + new Vector3(0f, size.y + 0.75f, 0f), new Vector3(size.x * 0.7f, 0.5f, size.z * 0.6f), roof, false);
            Shapes.Box("Ev_Kapi", parent, basePos + new Vector3(0f, 0.9f, -size.z * 0.5f - 0.03f), new Vector3(1.2f, 1.8f, 0.06f), Palette.DarkWood, false);
            Shapes.Box("Ev_Pencere", parent, basePos + new Vector3(size.x * 0.28f, size.y * 0.6f, -size.z * 0.5f - 0.03f), new Vector3(0.9f, 0.7f, 0.06f), Palette.ShallowWater, false);
        }

        private static void Tree(Transform parent, Vector3 basePos, float size, bool lightLeaves = false)
        {
            Shapes.Cylinder("Agac_Govde", parent, basePos + new Vector3(0f, size, 0f), new Vector3(0.35f, 1f, 0.35f) * size, Palette.DarkWood);
            Color leaves = lightLeaves ? Palette.LeafLight : Palette.Leaf;
            Shapes.Sphere("Agac_Yaprak", parent, basePos + new Vector3(0f, 2.7f * size, 0f), new Vector3(2.3f, 2.2f, 2.3f) * size, leaves, false);
            Shapes.Sphere("Agac_Yaprak", parent, basePos + new Vector3(0.5f, 3.4f * size, 0.3f), new Vector3(1.5f, 1.4f, 1.5f) * size, leaves, false);
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
