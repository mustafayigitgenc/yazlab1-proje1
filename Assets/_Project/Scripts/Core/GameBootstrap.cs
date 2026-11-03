using Gumusdere.Companion;
using Gumusdere.Economy;
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
using Gumusdere.Testing;
using Gumusdere.UI;
using Gumusdere.World;
using System.Collections;
using Gumusdere.Mining;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            EnsureBootstrap();
            // "Yeniden başla" sahneyi tekrar yüklediğinde oyun yeniden kurulsun.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureBootstrap();

        private static void EnsureBootstrap()
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
            Camera cam = SetUpCamera(player.transform, layout.MineStartZ);
            EnvironmentSetup.Apply(cam);

            var commands = player.gameObject.AddComponent<PlayerCommandInput>();
            commands.Initialize(cemal, new IInteractable[] { layout.Winch, layout.Rubble, layout.Watering });

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
            player.gameObject.AddComponent<PlayerEquipmentView>().Initialize(inventory);

            // 7b) Para ve bakkal Nazmi: ürün sat, baret ve fener al
            var wallet = new Wallet(PriceList.StartMoney);
            ShopView shopView = SetUpShop(layout, characters, inventory, wallet, setControls, log);

            // 8) Hüsnü Dayı: yerel LLM (Ollama) ile konuşan NPC
            NpcDialogueController dialogue = SetUpGuard(layout, characters, player.transform, inventory, setControls, interactor);
            layout.Door.Opened += () =>
            {
                log.Add("", "Maden kapısı açıldı! Hüsnü Dayı anahtarı verdi.", Color.white);
                navMesh.RebuildAsync();
            };
            layout.Rubble.Cleared += () =>
            {
                log.Add("", "Göçük temizlendi, iç odaya giden yol açıldı.", Color.white);
                navMesh.RebuildAsync();
            };
            layout.Door.Closed += () =>
            {
                log.Add("", "Hüsnü Dayı maden kapısını geri kapattı.", Color.white);
                navMesh.RebuildAsync();
            };

            // 9) Arayüz
            gameObject.AddComponent<HelpOverlay>().Initialize(cemal, commands, interactor, layout.Bridge);
            gameObject.AddComponent<InventoryView>().Initialize(inventory, wallet);
            gameObject.AddComponent<FishingView>().Initialize(layout.Fishing);
            gameObject.AddComponent<DialogueView>().Initialize(dialogue, log);
            gameObject.AddComponent<LlmStatusIndicator>().Initialize(dialogue);

            // 10) Oyun akışı: başlangıç → oyun ⇄ duraklatma → bitiş
            var stats = new GameStats();
            stats.Track(inventory);
            wallet.Changed += (before, after) => { if (after > before) stats.MoneyEarned += after - before; };
            dialogue.GiftAccepted += (item, effect) => stats.GiftsGiven++;
            cemal.TaskSucceeded += (taskName, seconds) => stats.CompanionTasksDone++;
            cemal.TaskFailed += (taskName, reason) => stats.CompanionTasksFailed++;
            dialogue.DecisionReceived += (decision, latencyMs) => stats.NpcReplies++;

            var flow = gameObject.AddComponent<GameFlow>();
            var map = gameObject.AddComponent<WorldMapView>();
            var agentTest = gameObject.AddComponent<AgentTestRunner>();
            var dialogueTest = gameObject.AddComponent<DialogueTestRunner>();
            flow.Initialize(stats, setControls,
                () => !dialogue.IsOpen && !layout.Fishing.Game.IsActive && !map.IsOpen && !shopView.IsOpen && !agentTest.IsRunning);

            // 11) M ile açılan harita
            map.Initialize(layout, cemal, () => flow.IsPlaying && !dialogue.IsOpen && !layout.Fishing.Game.IsActive && !shopView.IsOpen);
            map.AddMarker("Sen", player.transform, Palette.Shirt);
            map.AddMarker("Cemal", cemal.transform, Palette.WorkerShirt);
            map.AddMarker("Hüsnü Dayı", dialogue.transform, Palette.Vest);

            // 12) Testler: F9 ajan testi, F10 diyalog testi (sonuçlar TestResults/ klasörüne)
            agentTest.Initialize(cemal, player.transform, layout.Bridge, setControls,
                () => flow.IsPlaying && !dialogue.IsOpen && !layout.Fishing.Game.IsActive && !shopView.IsOpen && !dialogueTest.IsRunning);
            dialogueTest.Initialize(() => flow.IsPlaying && !dialogue.IsOpen && !shopView.IsOpen && !agentTest.IsRunning);
            layout.Vein.Mined += () => StartCoroutine(WinAfter(flow, 1.5f));
            gameObject.AddComponent<GameScreensView>().Initialize(flow);

            log.Add("", "Gümüşdere'ye hoş geldin ağa! Köprü kalkık; köprü başına gidip F ile Cemal'e iş ver. M: harita.", Color.white);
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

            dialogue.RegisterAction(NpcAction.OpenMine, new OpenMineAction(layout.Door, persona.OpenThreshold, inventory));
            dialogue.RegisterAction(NpcAction.CloseMine, new CloseMineAction(layout.Door));
            dialogue.RegisterAction(NpcAction.KickOut, new KickOutAction(guard, persona.KickThreshold));
            dialogue.SetMineStateSource(() => layout.Door.IsOpen);
            return dialogue;
        }

        private ShopView SetUpShop(WorldLayout layout, Transform parent, IInventory inventory, IWallet wallet,
            System.Action<bool> setControls, IMessageLog log)
        {
            GameObject nazmi = CharacterFactory.CreateShopkeeperBody(parent, layout.ShopkeeperSpot, 180f);
            navMesh.Ignore(nazmi.transform);
            var speech = nazmi.AddComponent<SpeechBubble>();
            speech.Initialize("Bakkal Nazmi", Palette.RoofBlue, log);

            var view = gameObject.AddComponent<ShopView>();
            view.Initialize(new Shop(inventory, wallet), setControls, speech, log);
            layout.Shop.Requested += view.Open;
            return view;
        }

        private static IEnumerator WinAfter(GameFlow flow, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            flow.Win();
        }

        private static Camera SetUpCamera(Transform target, float mineStartZ)
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
            follow.Initialize(target, mineStartZ);
            return cam;
        }

        private void OnDestroy()
        {
            navMesh?.Dispose();
        }
    }
}
