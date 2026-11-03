using Gumusdere.Companion;
using Gumusdere.Companion.Sensing;
using Gumusdere.Core;
using Gumusdere.Player;
using Gumusdere.UI;
using Gumusdere.Village;
using Gumusdere.World;
using UnityEngine;
using UnityEngine.AI;

namespace Gumusdere.Spawning
{
    /// <summary>
    /// Karakterleri (oyuncu, Cemal, inekler) basit şekillerden kurar ve gerekli bileşenleri ekler.
    /// Görsel parçaların çarpışması kaldırılır; çarpışmayı kök nesnedeki bileşen yönetir.
    /// </summary>
    public static class CharacterFactory
    {
        public static PlayerController CreatePlayer(Transform parent, Vector3 position)
        {
            var root = new GameObject("Oyuncu");
            root.transform.SetParent(parent, false);
            root.transform.position = position;

            var body = root.AddComponent<CharacterController>();
            body.height = 1.8f;
            body.radius = 0.35f;
            body.center = new Vector3(0f, 0.9f, 0f);
            body.stepOffset = 0.35f;

            Transform t = root.transform;
            Shapes.Capsule("Govde", t, new Vector3(0f, 0.7f, 0f), new Vector3(0.7f, 0.55f, 0.6f), Palette.Shirt, false);
            Shapes.Box("Pantolon", t, new Vector3(0f, 0.3f, 0f), new Vector3(0.55f, 0.6f, 0.45f), Palette.Trousers, false);
            Shapes.Sphere("Kafa", t, new Vector3(0f, 1.5f, 0f), Vector3.one * 0.5f, Palette.Skin, false);
            Shapes.Box("Burun", t, new Vector3(0f, 1.5f, 0.25f), new Vector3(0.1f, 0.1f, 0.1f), Palette.Skin, false);
            var hat = new GameObject(PlayerEquipmentView.HatName).transform;
            hat.SetParent(t, false);
            Shapes.Cylinder("Sapka_Kenar", hat, new Vector3(0f, 1.72f, 0f), new Vector3(0.75f, 0.03f, 0.75f), Palette.Hay, false);
            Shapes.Cylinder("Sapka_Tepe", hat, new Vector3(0f, 1.85f, 0f), new Vector3(0.38f, 0.12f, 0.38f), Palette.Hay, false);

            // Bakkaldan alınınca görünen ekipman (PlayerEquipmentView açıp kapatır).
            var helmet = new GameObject(PlayerEquipmentView.HelmetName).transform;
            helmet.SetParent(t, false);
            Shapes.Sphere("Baret", helmet, new Vector3(0f, 1.66f, 0f), new Vector3(0.56f, 0.36f, 0.56f), Palette.Helmet, false);
            Shapes.Box("Baret_Siper", helmet, new Vector3(0f, 1.62f, 0.22f), new Vector3(0.45f, 0.04f, 0.2f), Palette.Helmet, false);
            helmet.gameObject.SetActive(false);

            var lantern = new GameObject(PlayerEquipmentView.LanternName).transform;
            lantern.SetParent(t, false);
            lantern.localPosition = new Vector3(-0.45f, 0.75f, 0.15f);
            Shapes.Box("Fener_Govde", lantern, Vector3.zero, new Vector3(0.16f, 0.22f, 0.16f), Palette.DarkWood, false);
            Shapes.Sphere("Fener_Isik", lantern, new Vector3(0f, 0.01f, 0f), Vector3.one * 0.13f, Palette.Hay, false);
            var glow = new GameObject("Fener_Lamba").AddComponent<Light>();
            glow.transform.SetParent(lantern, false);
            glow.type = LightType.Point;
            glow.range = 7f;
            glow.intensity = 2.2f;
            glow.color = new Color(1f, 0.82f, 0.5f);
            glow.shadows = LightShadows.None;
            lantern.gameObject.SetActive(false);

            // Cemal ve inekler oyuncunun içinden geçmesin diye: hareketli, delik açmayan NavMesh engeli.
            var obstacle = root.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Capsule;
            obstacle.center = new Vector3(0f, 0.9f, 0f);
            obstacle.radius = 0.4f;
            obstacle.height = 1.8f;
            obstacle.carving = false;

            return root.AddComponent<PlayerController>();
        }

        /// <summary>NavMesh hazır olduktan sonra çağrılmalıdır.</summary>
        public static CompanionController CreateCompanion(Transform parent, Vector3 position, Transform leader, IMessageLog log)
        {
            var root = new GameObject(CompanionController.DisplayName);
            root.transform.SetParent(parent, false);
            root.transform.position = position;

            Transform t = root.transform;
            Shapes.Capsule("Govde", t, new Vector3(0f, 0.7f, 0f), new Vector3(0.75f, 0.55f, 0.65f), Palette.WorkerShirt, false);
            Shapes.Box("Pantolon", t, new Vector3(0f, 0.3f, 0f), new Vector3(0.6f, 0.6f, 0.5f), Palette.Trousers, false);
            Shapes.Sphere("Kafa", t, new Vector3(0f, 1.5f, 0f), Vector3.one * 0.5f, Palette.Skin, false);
            Shapes.Box("Burun", t, new Vector3(0f, 1.5f, 0.25f), new Vector3(0.1f, 0.1f, 0.1f), Palette.Skin, false);
            Shapes.Sphere("Baret", t, new Vector3(0f, 1.66f, 0f), new Vector3(0.56f, 0.36f, 0.56f), Palette.Helmet, false);
            Shapes.Box("Baret_Siper", t, new Vector3(0f, 1.62f, 0.22f), new Vector3(0.45f, 0.04f, 0.2f), Palette.Helmet, false);

            // Sulama kovası: sağ elinde; sadece sulama görevinde görünür (WateringJob açıp kapatır).
            var can = new GameObject(Gumusdere.Farming.WateringJob.CanObjectName).transform;
            can.SetParent(t, false);
            can.localPosition = new Vector3(0.48f, 0.55f, 0.18f);
            Shapes.Cylinder("Kova_Govde", can, Vector3.zero, new Vector3(0.32f, 0.18f, 0.32f), Palette.RoofBlue, false);
            Shapes.Cylinder("Kova_Su", can, new Vector3(0f, 0.18f, 0f), new Vector3(0.27f, 0.01f, 0.27f), Palette.Water, false);
            Shapes.Cylinder("Kova_Ibrik", can, new Vector3(0f, 0.08f, 0.22f), new Vector3(0.06f, 0.16f, 0.06f), Palette.RoofBlue, false,
                Quaternion.Euler(60f, 0f, 0f));
            Shapes.Box("Kova_Sap", can, new Vector3(0f, 0.3f, 0f), new Vector3(0.04f, 0.04f, 0.3f), Palette.Stone, false);
            can.gameObject.SetActive(false);

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 1.8f;
            agent.speed = 3.5f;
            agent.acceleration = 14f;
            agent.angularSpeed = 540f;
            agent.stoppingDistance = 0.2f;
            agent.areaMask = NavMesh.AllAreas;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.avoidancePriority = 40;
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }

            // Sensör (ışınlar), sensör çizimi ve konuşma balonu
            var sensor = root.AddComponent<RaycastSensor>();
            root.AddComponent<SensorDebugView>().Initialize(sensor);
            var speech = root.AddComponent<SpeechBubble>();
            speech.Initialize(CompanionController.DisplayName, Palette.WorkerShirt, log);

            var companion = root.AddComponent<CompanionController>();
            companion.Initialize(leader, sensor, speech);

            var pathView = root.AddComponent<CompanionPathView>();
            pathView.Initialize(agent);
            return companion;
        }

        /// <summary>Maden bekçisi Hüsnü Dayı'nın görünümü (konuşma mantığı GuardNpc ve NpcDialogueController'da).</summary>
        public static GameObject CreateGuardBody(Transform parent, Vector3 position)
        {
            var root = new GameObject("Husnu_Dayi");
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, 200f, 0f);

            Transform t = root.transform;
            Shapes.Capsule("Govde", t, new Vector3(0f, 0.7f, 0f), new Vector3(0.8f, 0.55f, 0.7f), Palette.Vest, false);
            Shapes.Box("Pantolon", t, new Vector3(0f, 0.3f, 0f), new Vector3(0.6f, 0.6f, 0.5f), Palette.Trousers, false);
            Shapes.Sphere("Kafa", t, new Vector3(0f, 1.5f, 0f), Vector3.one * 0.5f, Palette.Skin, false);
            Shapes.Box("Burun", t, new Vector3(0f, 1.5f, 0.25f), new Vector3(0.1f, 0.1f, 0.1f), Palette.Skin, false);
            Shapes.Sphere("Sakal", t, new Vector3(0f, 1.33f, 0.14f), new Vector3(0.42f, 0.32f, 0.3f), Palette.Beard, false);
            Shapes.Cylinder("Kasket", t, new Vector3(0f, 1.74f, 0f), new Vector3(0.5f, 0.07f, 0.5f), Palette.DarkWood, false);
            Shapes.Box("Kasket_Siper", t, new Vector3(0f, 1.7f, 0.24f), new Vector3(0.36f, 0.03f, 0.18f), Palette.DarkWood, false);
            Shapes.Cylinder("Baston", t, new Vector3(0.45f, 0.55f, 0.15f), new Vector3(0.06f, 0.55f, 0.06f), Palette.Wood, false);

            var body = root.AddComponent<CapsuleCollider>();
            body.center = new Vector3(0f, 0.9f, 0f);
            body.radius = 0.4f;
            body.height = 1.8f;
            return root;
        }

        /// <summary>Bakkal Nazmi'nin görünümü: önlüklü, bıyıklı, kasketli.</summary>
        public static GameObject CreateShopkeeperBody(Transform parent, Vector3 position, float facingY)
        {
            var root = new GameObject("Bakkal_Nazmi");
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, facingY, 0f);

            Transform t = root.transform;
            Shapes.Capsule("Govde", t, new Vector3(0f, 0.7f, 0f), new Vector3(0.85f, 0.55f, 0.75f), Palette.RoofBlue, false);
            Shapes.Box("Onluk", t, new Vector3(0f, 0.65f, 0.3f), new Vector3(0.55f, 0.75f, 0.06f), Palette.Wall, false);
            Shapes.Box("Pantolon", t, new Vector3(0f, 0.3f, 0f), new Vector3(0.6f, 0.6f, 0.5f), Palette.Trousers, false);
            Shapes.Sphere("Kafa", t, new Vector3(0f, 1.5f, 0f), Vector3.one * 0.5f, Palette.Skin, false);
            Shapes.Box("Burun", t, new Vector3(0f, 1.5f, 0.25f), new Vector3(0.1f, 0.1f, 0.1f), Palette.Skin, false);
            Shapes.Box("Biyik", t, new Vector3(0f, 1.42f, 0.25f), new Vector3(0.26f, 0.05f, 0.06f), Palette.DarkWood, false);
            Shapes.Cylinder("Kasket", t, new Vector3(0f, 1.74f, 0f), new Vector3(0.5f, 0.07f, 0.5f), Palette.Roof, false);
            Shapes.Box("Kasket_Siper", t, new Vector3(0f, 1.7f, 0.24f), new Vector3(0.36f, 0.03f, 0.18f), Palette.Roof, false);
            return root;
        }

        /// <summary>NavMesh hazır olduktan sonra çağrılmalıdır.</summary>
        public static AnimalWanderer CreateCow(Transform parent, Vector3 position, Rect pasture)
        {
            var root = new GameObject("Inek");
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            Transform t = root.transform;
            Shapes.Box("Govde", t, new Vector3(0f, 0.85f, 0f), new Vector3(0.9f, 0.8f, 1.6f), Palette.Cow, false);
            Shapes.Box("Kafa", t, new Vector3(0f, 1.1f, 0.95f), new Vector3(0.55f, 0.5f, 0.55f), Palette.Cow, false);
            Shapes.Box("Burun", t, new Vector3(0f, 0.98f, 1.24f), new Vector3(0.4f, 0.22f, 0.1f), Palette.Skin, false);
            Shapes.Box("Benek", t, new Vector3(0.46f, 0.95f, 0.2f), new Vector3(0.02f, 0.35f, 0.5f), Palette.CowSpot, false);
            Shapes.Box("Benek", t, new Vector3(-0.46f, 0.8f, -0.3f), new Vector3(0.02f, 0.3f, 0.45f), Palette.CowSpot, false);
            Shapes.Box("Benek", t, new Vector3(0.1f, 1.26f, -0.4f), new Vector3(0.4f, 0.02f, 0.35f), Palette.CowSpot, false);
            float[] legX = { -0.3f, 0.3f };
            float[] legZ = { -0.6f, 0.6f };
            foreach (float lx in legX)
            {
                foreach (float lz in legZ)
                {
                    Shapes.Box("Bacak", t, new Vector3(lx, 0.22f, lz), new Vector3(0.18f, 0.45f, 0.18f), Palette.Cow, false);
                }
            }

            // Cemal'in sensörünün inekleri görebilmesi için çarpışma kutusu (NavMesh hesabında yok sayılır).
            var body = root.AddComponent<BoxCollider>();
            body.center = new Vector3(0f, 0.85f, 0.15f);
            body.size = new Vector3(0.9f, 1.1f, 1.9f);

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.7f;
            agent.height = 1.4f;
            agent.speed = 1.1f;
            agent.acceleration = 4f;
            agent.angularSpeed = 120f;
            agent.avoidancePriority = 60;
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }

            var wanderer = root.AddComponent<AnimalWanderer>();
            wanderer.Initialize(pasture);
            return wanderer;
        }
    }
}
