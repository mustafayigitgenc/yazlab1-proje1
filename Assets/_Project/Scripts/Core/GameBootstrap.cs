using Gumusdere.Companion;
using Gumusdere.Navigation;
using Gumusdere.Player;
using Gumusdere.Spawning;
using Gumusdere.UI;
using Gumusdere.World;
using UnityEngine;

namespace Gumusdere.Core
{
    /// <summary>
    /// Oyunun giriş noktası (composition root). Play'e basıldığında kendiliğinden oluşur,
    /// haritayı kurar, NavMesh'i hesaplar, karakterleri yaratır ve bileşenleri birbirine bağlar.
    /// Bağımlılıkların hepsi burada elle verilir; diğer sınıflar birbirini aramaz (Dependency Inversion).
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private const int CowCount = 3;

        private NavMeshService navMesh;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (FindAnyObjectByType<GameBootstrap>() != null) return;
            new GameObject("[Gumusdere]").AddComponent<GameBootstrap>();
        }

        private void Awake()
        {
            // 1) Harita
            WorldLayout layout = new WorldBuilder().Build(transform);

            // 2) Oyuncu (NavMesh'ten önce, çünkü NavMesh hesabında yok sayılması gerekiyor)
            var characters = new GameObject("Karakterler").transform;
            characters.SetParent(transform, false);
            PlayerController player = CharacterFactory.CreatePlayer(characters, layout.PlayerSpawn);

            // 3) NavMesh
            navMesh = new NavMeshService(layout.NavBounds);
            navMesh.Ignore(layout.PlayerOnlyWalls);
            navMesh.Ignore(player.transform);
            foreach ((Transform root, int area) in layout.NavAreaOverrides)
            {
                navMesh.SetArea(root, area);
            }
            navMesh.Build();

            // 4) NavMesh'e ihtiyaç duyan ajanlar
            CompanionController cemal = CharacterFactory.CreateCompanion(characters, layout.CompanionSpawn, player.transform);
            for (int i = 0; i < CowCount; i++)
            {
                Rect p = layout.CowPasture;
                var spawn = new Vector3(p.xMin + p.width * (0.2f + 0.3f * i), 0f, p.yMin + p.height * (0.3f + 0.2f * i));
                CharacterFactory.CreateCow(characters, spawn, p);
            }

            // 5) Köprü inince NavMesh'i oyunu dondurmadan yeniden hesapla
            layout.Bridge.Lowered += () =>
            {
                Debug.Log("[Köprü] İndi, NavMesh güncelleniyor...");
                navMesh.RebuildAsync();
            };

            // 6) Kamera, girdiler ve arayüz
            SetUpCamera(player.transform);
            player.gameObject.AddComponent<PlayerCommandInput>().Initialize(cemal, layout.Bridge);
            gameObject.AddComponent<HelpOverlay>().Initialize(cemal, layout.Bridge);
        }

        private static void SetUpCamera(Transform target)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }

            CameraFollow follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.Initialize(target);
        }

        private void OnDestroy()
        {
            navMesh?.Dispose();
        }
    }
}
