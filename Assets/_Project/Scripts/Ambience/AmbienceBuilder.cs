using Gumusdere.Navigation;
using Gumusdere.World;
using UnityEngine;
using UnityEngine.AI;

namespace Gumusdere.Ambience
{
    /// <summary>
    /// Oyuna etkisi olmayan "canlılık" katmanı: çiçekler, çimen öbekleri, kelebekler, ateş böcekleri ve su parıltıları.
    /// NavMesh kurulduktan SONRA çağrılır; yerleşim için NavMesh'in yürünebilir alanı kullanılır ve nesnelerin çarpışması yoktur,
    /// yani ne yol bulmayı ne de testleri etkiler. Aynı tohumla her açılışta aynı yerlere konur.
    /// </summary>
    public static class AmbienceBuilder
    {
        private const int Seed = 1903;

        private static readonly Color[] FlowerColors =
        {
            new(0.95f, 0.30f, 0.30f), new(0.98f, 0.85f, 0.25f), new(0.97f, 0.97f, 0.95f),
            new(0.70f, 0.45f, 0.90f), new(0.98f, 0.60f, 0.75f), new(0.40f, 0.65f, 0.98f)
        };

        private static readonly Color[] WingColors =
        {
            new(0.98f, 0.85f, 0.25f), new(0.97f, 0.97f, 0.95f), new(0.98f, 0.55f, 0.20f), new(0.55f, 0.75f, 1f)
        };

        public static void Populate(Transform parent, WorldLayout layout)
        {
            var root = new GameObject("Canli_Dunya").transform;
            root.SetParent(parent, false);
            Random.State saved = Random.state;
            Random.InitState(Seed);

            ScatterPlants(root);
            SpawnButterflies(root);
            SpawnFireflies(root);
            SpawnWaterGlints(root);

            Random.state = saved;
        }

        // ------------------------------------------------------------------ Çiçek ve çimen

        private static void ScatterPlants(Transform root)
        {
            var plants = new GameObject("Cicekler").transform;
            plants.SetParent(root, false);
            Color stem = new(0.30f, 0.55f, 0.22f);
            Color tuft = new(0.33f, 0.62f, 0.25f);
            Color tuftDark = new(0.25f, 0.50f, 0.20f);

            int flowers = 0, tufts = 0;
            for (int attempt = 0; attempt < 2500 && (flowers < 240 || tufts < 200); attempt++)
            {
                var p = new Vector3(Random.Range(-38f, 38f), 0f, Random.Range(-24f, 70f));
                if (!IsFreeGrass(p)) continue;

                if (flowers < 240 && Random.value < 0.55f)
                {
                    // Çiçekler küçük kümeler halinde
                    Color c = FlowerColors[Random.Range(0, FlowerColors.Length)];
                    int n = Random.Range(1, 4);
                    for (int i = 0; i < n; i++)
                    {
                        Vector3 q = p + new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f));
                        float h = Random.Range(0.18f, 0.32f);
                        Ambient.Decor(PrimitiveType.Cylinder, "Sap", plants, q + new Vector3(0f, h * 0.5f, 0f), new Vector3(0.03f, h * 0.5f, 0.03f), stem);
                        Ambient.Decor(PrimitiveType.Sphere, "Cicek", plants, q + new Vector3(0f, h, 0f), new Vector3(0.16f, 0.09f, 0.16f), c);
                        flowers++;
                    }
                }
                else if (tufts < 200)
                {
                    Color c = Random.value < 0.5f ? tuft : tuftDark;
                    float yaw = Random.Range(0f, 180f);
                    for (int i = 0; i < 3; i++)
                    {
                        Quaternion rot = Quaternion.Euler(Random.Range(-18f, 18f), yaw + i * 60f, Random.Range(-18f, 18f));
                        Ambient.Decor(PrimitiveType.Cube, "Cimen", plants, p + new Vector3(0f, 0.14f, 0f), new Vector3(0.04f, 0.3f, 0.22f), c, rot);
                    }
                    tufts++;
                }
            }
        }

        /// <summary>Yürünebilir çimen mi? Su, bina, tarla, patika, maden ve diğer nesnelerin üstüne süs konmaz.</summary>
        private static bool IsFreeGrass(Vector3 p)
        {
            if (p.z > 16.5f && p.z < 27.5f) return false;                          // nehir ve kıyısı
            if (p.z > 70f) return false;                                          // dağ ve maden
            if (InRect(p, -0.5f, -8f, 2.5f, 18f) || InRect(p, -1.5f, 26f, 1.5f, 72f)) return false; // patikalar
            if (InRect(p, 3.5f, -14.5f, 14.5f, -5.5f)) return false;              // tarla ve çit
            if (Mathf.Pow((p.x + 20f) / 9f, 2f) + Mathf.Pow((p.z - 48f) / 7.5f, 2f) < 1f) return false; // göl ve kıyı

            int walkable = NavAreas.MaskOf(NavAreas.Walkable);
            if (!NavMesh.SamplePosition(p, out NavMeshHit hit, 0.3f, walkable)) return false;
            if (Mathf.Abs(hit.position.y) > 0.25f) return false;                  // çatılar, köprü vb.
            return !Physics.CheckSphere(p + Vector3.up * 1.1f, 0.85f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        private static bool InRect(Vector3 p, float x0, float z0, float x1, float z1) =>
            p.x >= x0 && p.x <= x1 && p.z >= z0 && p.z <= z1;

        // ------------------------------------------------------------------ Kelebek, ateş böceği, su

        private static void SpawnButterflies(Transform root)
        {
            Vector3[] homes =
            {
                new(-4f, 0f, 34f), new(4f, 0f, 36f), new(-6f, 0f, 2f), new(9f, 0f, -2f), new(-18f, 0f, -4f),
                new(24f, 0f, -8f), new(12f, 0f, 52f), new(-2f, 0f, 58f), new(28f, 0f, 30f), new(-26f, 0f, 32f)
            };
            foreach (Vector3 home in homes)
            {
                var go = new GameObject("Kelebek");
                go.transform.SetParent(root, false);
                go.transform.position = home + Vector3.up;
                go.AddComponent<Butterfly>().Initialize(home, 3.5f, WingColors[Random.Range(0, WingColors.Length)]);
            }
        }

        private static void SpawnFireflies(Transform root)
        {
            // Ormanın gölgeli kısımları ve göl kenarı
            Vector3[] homes =
            {
                new(-34f, 0f, 40f), new(-31f, 0f, 55f), new(-29f, 0f, 63f), new(-14f, 0f, 63f), new(-9f, 0f, 58f),
                new(27f, 0f, 48f), new(33f, 0f, 57f), new(24f, 0f, 63f), new(-13f, 0f, 42f), new(-27f, 0f, 46f),
                new(30f, 0f, 37f), new(-35f, 0f, 30f)
            };
            foreach (Vector3 home in homes)
            {
                for (int i = 0; i < 2; i++)
                {
                    var go = new GameObject("Ates_Bocegi");
                    go.transform.SetParent(root, false);
                    go.AddComponent<Firefly>().Initialize(home + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f)));
                }
            }
        }

        private static void SpawnWaterGlints(Transform root)
        {
            var river = new GameObject("Nehir_Parilti");
            river.transform.SetParent(root, false);
            river.AddComponent<WaterGlints>().Initialize(28, () =>
                new Vector3(Random.Range(-38f, 38f), -1.48f, Random.Range(18.6f, 25.4f)));

            var lake = new GameObject("Gol_Parilti");
            lake.transform.SetParent(root, false);
            lake.AddComponent<WaterGlints>().Initialize(14, () =>
            {
                Vector2 d = Random.insideUnitCircle;
                return new Vector3(-20f + d.x * 6f, 0.075f, 48f + d.y * 4.6f);
            });
        }
    }
}
