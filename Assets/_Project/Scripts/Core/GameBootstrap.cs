using Gumusdere.Companion;
using Gumusdere.Interaction;
using Gumusdere.Items;
using Gumusdere.Navigation;
using Gumusdere.Npc;
using Gumusdere.Npc.Actions;
using Gumusdere.Npc.Dialogue;
using Gumusdere.Npc.Llm;
using Gumusdere.Npc.Parsing;
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
            // 0) Mesaj kutusu (sol alt)
            MessageLog log = gameObject.AddComponent<MessageLog>();

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
            CompanionController cemal = CharacterFactory.CreateCompanion(characters, layout.CompanionSpawn, player.transform, log);
            for (int i = 0; i < CowCount; i++)
            {
                Rect p = layout.CowPasture;
                var spawn = new Vector3(p.xMin + p.width * (0.2f + 0.3f * i), 0f, p.yMin + p.height * (0.3f + 0.2f * i));
                var cow = CharacterFactory.CreateCow(characters, spawn, p);
                navMesh.Ignore(cow.transform); // hareketli olduğu için NavMesh'e "duvar" olarak girmesin
            }

            // 5) Köprü inince NavMesh'i oyunu dondurmadan yeniden hesapla
            layout.Bridge.Lowered += () =>
            {
                Debug.Log("[Köprü] İndi, NavMesh güncelleniyor...");
                log.Add("", "Köprü indi. Artık nehrin karşısına geçebilirsin.", Color.white);
                navMesh.RebuildAsync();
            };

            // 6) Kamera, ışık, girdiler ve arayüz
            Camera cam = SetUpCamera(player.transform);
            EnvironmentSetup.Apply(cam);

            var commands = player.gameObject.AddComponent<PlayerCommandInput>();
            commands.Initialize(cemal, new IInteractable[] { layout.Winch });

            // 7) Çanta ve E ile etkileşim (tarla, çalı, iskele)
            IInventory inventory = new Inventory();
            var interactor = player.gameObject.AddComponent<PlayerInteractor>();
            System.Action<bool> setControls = controlsOn =>
            {
                player.InputEnabled = controlsOn;
                interactor.InteractionEnabled = controlsOn;
                commands.enabled = controlsOn;
            };
            var context = new PlayerContext(player.gameObject, inventory, log, setControls);
            interactor.Initialize(context, layout.PlayerInteractables);

            // 8) Hüsnü Dayı: yerel LLM (Ollama) ile konuşan NPC
            NpcDialogueController dialogue = SetUpGuard(layout, characters, player.transform, inventory, setControls, interactor);
            layout.Door.Opened += () =>
            {
                log.Add("", "Maden kapısı açıldı! Hüsnü Dayı anahtarı verdi.", Color.white);
                navMesh.RebuildAsync();
            };

            // 9) Arayüz
            gameObject.AddComponent<HelpOverlay>().Initialize(cemal, commands, interactor, layout.Bridge);
            gameObject.AddComponent<InventoryView>().Initialize(inventory);
            gameObject.AddComponent<FishingView>().Initialize(layout.Fishing);
            gameObject.AddComponent<DialogueView>().Initialize(dialogue, log);
            gameObject.AddComponent<LlmStatusIndicator>().Initialize(dialogue);

            log.Add("", "Gümüşdere'ye hoş geldin ağa! Köprü kalkık; köprü başına gidip F ile Cemal'e iş ver.", Color.white);
        }

        private NpcDialogueController SetUpGuard(WorldLayout layout, Transform parent, Transform player,
            IInventory inventory, System.Action<bool> setControls, PlayerInteractor interactor)
        {
            GameObject body = CharacterFactory.CreateGuardBody(parent, layout.GuardSpot);
            navMesh.Ignore(body.transform);

            var speech = body.AddComponent<SpeechBubble>();
            NpcPersona persona = NpcPersona.HusnuDayi();
            speech.Initialize(persona.Name, Palette.Vest, null);

            var dialogue = body.AddComponent<NpcDialogueController>();
            ILLMClient client = new OllamaClient(OllamaConfig.LoadOrDefault());
            dialogue.Initialize(client, new NpcResponseParser(), persona, inventory, speech, setControls);

            var guard = body.AddComponent<GuardNpc>();
            guard.Initialize(dialogue, player);
            interactor.Register(guard);

            dialogue.RegisterAction(NpcAction.OpenMine, new OpenMineAction(layout.Door, persona.OpenThreshold));
            dialogue.RegisterAction(NpcAction.KickOut, new KickOutAction(guard, persona.KickThreshold));
            return dialogue;
        }

        private static Camera SetUpCamera(Transform target)
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
            return cam;
        }

        private void OnDestroy()
        {
            navMesh?.Dispose();
        }
    }
}
