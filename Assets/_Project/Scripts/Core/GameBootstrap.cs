using Gumusdere.Ambience;
using Gumusdere.Audio;
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
using Gumusdere.Village;
using Gumusdere.UI;
using Gumusdere.World;
using System.Collections;
using System.Collections.Generic;
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
            // Rıfat Amca'nın başıboş inekleri: her biri kendi bölgesinde dolaşır; Cemal onları ahıra toplar.
            var cows = new List<AnimalWanderer>();
            foreach (Rect area in layout.LooseCowAreas)
            {
                var cow = CharacterFactory.CreateCow(characters, new Vector3(area.center.x, 0f, area.center.y), area);
                navMesh.Ignore(cow.transform); // hareketli olduğu için NavMesh'e "duvar" olarak girmesin
                WalkBob.Attach(cow.gameObject, 0.04f, 2f, 1.1f);
                cows.Add(cow);
            }
            layout.Herd.Initialize(cows, layout.BarnGate + new Vector3(-1.1f, 0f, 0f), layout.BarnPen);

            // 4b) Canlı dünya: çiçekler, kelebekler, ateş böcekleri, su parıltısı; karakterlere yürüme animasyonu
            AmbienceBuilder.Populate(transform, layout);
            WalkBob playerWalk = WalkBob.Attach(player.gameObject);
            WalkBob.Attach(cemal.gameObject);

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
            var mineDarkness = gameObject.AddComponent<MineDarkness>();
            mineDarkness.Initialize(player.transform, cemal.transform,
                cemal.transform.FindDeep(CharacterFactory.HeadLampName)?.gameObject, layout.MineStartZ);

            var commands = player.gameObject.AddComponent<PlayerCommandInput>();
            commands.Initialize(cemal, new IInteractable[] { layout.Winch, layout.Rubble, layout.Watering, layout.Herd, layout.Beams });

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

            // 7b') Cemal'in can yeleği: bakkaldan alınınca giyer; o zamana kadar sığ geçit onun için yürünemez
            cemal.SetLifeVest(inventory.Has(ItemType.LifeVest));
            inventory.Changed += (type, count) => { if (type == ItemType.LifeVest) cemal.SetLifeVest(count > 0); };
            cemal.LifeVestNeeded += () => log.Add("", "Cemal'in sığ geçitten geçmesi için can yeleği lazım: Nazmi'nin bakkalında 30 lira.", Color.white);

            // 7b'') Çoban Rıfat Amca: inekler ahıra toplanınca destek kirişlerini verir
            GameObject rifat = CharacterFactory.CreateFarmerBody(characters, layout.FarmerSpot, 270f);
            navMesh.Ignore(rifat.transform);
            WalkBob.Attach(rifat);
            var rifatSpeech = rifat.AddComponent<SpeechBubble>();
            rifatSpeech.Initialize("Rıfat Amca", new Color(0.45f, 0.65f, 0.3f), log);
            var farmer = rifat.AddComponent<FarmerNpc>();
            farmer.Initialize(layout.Herd, rifatSpeech, layout.FarmerSpot + new Vector3(-1.3f, 0f, 0f));
            interactor.Register(farmer);
            layout.Herd.CowPenned += count => log.Add("", $"İnek ahıra girdi ({count}/{layout.Herd.Total}).", Color.white);
            layout.Herd.AllPenned += () =>
            {
                farmer.GiveReward();
                layout.Beams.Unlock();
                log.Add("", "Rıfat Amca destek kirişlerini ahırın önüne bıraktı. Yanında F ile Cemal'e madenin ağzına taşıt.", Color.white);
            };

            // 7c) Köy ilan panosu: köylülerin istekleri (bakkaldan daha iyi para)
            var quests = new QuestBook(inventory, wallet, QuestBook.GumusdereQuests());
            var questView = gameObject.AddComponent<QuestView>();
            questView.Initialize(quests, setControls, log);
            layout.QuestBoard.Requested += questView.Open;

            layout.Beams.BeamDelivered += count =>
            {
                log.Add("", $"Madenin ağzına destek kirişi bırakıldı ({count}/{Gumusdere.Mining.MineEntryRule.RequiredBeams}).", Color.white);
                if (layout.Beams.IsComplete) log.Add("", "Kirişler tamam! Hüsnü Dayı'ya bunu söyleyebilirsin.", Color.white);
            };

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
            // Ekranı kaplayan bir pencere açık mı (sohbet, balık, bakkal, ilan panosu)?
            bool UiBusy() => dialogue.IsOpen || layout.Fishing.Game.IsActive || shopView.IsOpen || questView.IsOpen;
            quests.Completed += quest => stats.QuestsCompleted++;
            flow.Initialize(stats, setControls, () => !UiBusy() && !map.IsOpen && !agentTest.IsRunning);

            // 11) M ile açılan harita
            map.Initialize(layout, cemal, () => flow.IsPlaying && !UiBusy());
            map.AddMarker("Sen", player.transform, Palette.Shirt);
            map.AddMarker("Cemal", cemal.transform, Palette.WorkerShirt);
            map.AddMarker("Hüsnü Dayı", dialogue.transform, Palette.Vest);
            map.AddMarker("Rıfat Amca", rifat.transform, new Color(0.45f, 0.65f, 0.3f));
            foreach (AnimalWanderer cow in cows) map.AddMarker("İnek", cow.transform, Palette.Cow);

            // 12) Testler: F9 ajan testi, F10 diyalog testi (sonuçlar TestResults/ klasörüne)
            agentTest.Initialize(cemal, player.transform, layout.Bridge, setControls,
                () => flow.IsPlaying && !UiBusy() && !dialogueTest.IsRunning);
            dialogueTest.Initialize(() => flow.IsPlaying && !UiBusy() && !agentTest.IsRunning);
            layout.Vein.Mined += () => StartCoroutine(WinAfter(flow, 1.5f));
            gameObject.AddComponent<GameScreensView>().Initialize(flow);
            cam.GetComponent<CameraFollow>().SetShowcase(() => flow.Machine.Current == flow.Title || flow.Machine.Current == flow.Victory);

            // 13) Sağ üstte "Hedefler" listesi: ana hikâyenin adımları, durumu oyunun kendisinden okunur
            bool doorEverOpened = false;
            layout.Door.Opened += () => doorEverOpened = true;
            int MoneyNeeded(params ItemType[] tools)
            {
                int need = 0;
                foreach (ItemType tool in tools) if (!inventory.Has(tool)) need += PriceList.BuyPrice(tool);
                return need;
            }
            gameObject.AddComponent<ObjectiveTracker>().Initialize(new[]
            {
                new ObjectiveTracker.Step("Cemal'e can yeleği al", () => cemal.HasLifeVest || layout.Bridge.IsLowered,
                    () => $"Tarlaya tohum ek, F ile Cemal'e sulat, hasat edip Nazmi'nin bakkalında sat. Can yeleği {PriceList.BuyPrice(ItemType.LifeVest)} lira (paran {wallet.Money})."),
                new ObjectiveTracker.Step("Köprüyü indir", () => layout.Bridge.IsLowered,
                    () => "Köprü başına git, F ile Cemal'e vinci çevirt."),
                new ObjectiveTracker.Step("Baret ve fener al",
                    () => doorEverOpened || (inventory.Has(ItemType.Helmet) && inventory.Has(ItemType.Lantern)),
                    () => $"Nazmi'nin bakkalında. Paran {wallet.Money}/{MoneyNeeded(ItemType.Helmet, ItemType.Lantern)} lira: ürün sat ya da ilan panosundaki istekleri yap."),
                new ObjectiveTracker.Step("Rıfat Amca'nın ineklerini ahıra topla", () => layout.Herd.AllDone,
                    () => $"Mera'nın doğusundaki ahırın yanında F: Cemal kaçan inekleri tek tek getirir ({layout.Herd.Penned}/{layout.Herd.Total})."),
                new ObjectiveTracker.Step("Destek kirişlerini madenin ağzına taşıt", () => layout.Beams.IsComplete,
                    () => $"Ahırın önündeki kirişlerin yanında F'ye bas ({layout.Beams.Delivered}/3)."),
                new ObjectiveTracker.Step("Hüsnü Dayı'nın güvenini kazan", () => doorEverOpened || dialogue.Trust.Value >= dialogue.Persona.OpenThreshold,
                    () => $"Güven {dialogue.Trust.Value}/{dialogue.Persona.OpenThreshold}. Sevdiği hediyeleri ver, madene nasıl güvenle gireceğini anlat."),
                new ObjectiveTracker.Step("Maden kapısını açtır", () => doorEverOpened,
                    () => "Hüsnü Dayı'ya E ile git ve kapıyı açmasını iste."),
                new ObjectiveTracker.Step("Göçüğü Cemal'e temizlet", () => !layout.Rubble.IsAvailable,
                    () => "Madenin içinde göçüğün yanında F'ye bas."),
                new ObjectiveTracker.Step("Gümüş damarını kaz", () => layout.Vein.IsMined,
                    () => "En içteki odada damarın yanında E'ye üç kez bas."),
            }, () => !UiBusy() && !map.IsOpen && !agentTest.IsRunning && !dialogueTest.IsRunning,
               () => $"Köy istekleri: <b>{quests.CompletedCount}/{quests.Quests.Count}</b> (ilan panosu)");

            // 14) Hikâye modu: önemli anlarda sinema şeritleri ve kısa bölüm yazıları (oyunu durdurmaz)
            var story = gameObject.AddComponent<StoryView>();
            story.Initialize(() => flow.IsPlaying && !UiBusy() && !map.IsOpen && !agentTest.IsRunning && !dialogueTest.IsRunning);
            story.Add("Bölüm 1 · Miras",
                "Dedenin köyü Gümüşdere artık senin. Madeni yeniden açarsan köy de yeniden doğacak.",
                () => flow.IsPlaying);
            story.Add("Bölüm 2 · Dere",
                "Cemal can yeleğini giydi. Dere soğuk ama artık onu korkutmuyor.",
                () => cemal.HasLifeVest);
            story.Add("Bölüm 3 · Köprü",
                "Yıllardır kalkık duran köprü gıcırdayarak indi. Madenin yolu açıldı.",
                () => layout.Bridge.IsLowered);
            story.Add("Bölüm 4 · Hazırlık",
                "Baret başında, fener elinde. Nazmi gülümsedi: \"Deden de böyle başlamıştı.\"",
                () => inventory.Has(ItemType.Helmet) && inventory.Has(ItemType.Lantern));
            story.Add("Bölüm 5 · Sürü",
                "İnekler ahırda. Rıfat Amca sözünü tuttu: destek kirişleri artık senin.",
                () => layout.Herd.AllDone);
            story.Add("Bölüm 6 · Kirişler",
                "Üç kiriş madenin ağzında. Sıra, kapının anahtarını tutan Hüsnü Dayı'da.",
                () => layout.Beams.IsComplete);
            story.Add("Bölüm 7 · Kapı",
                "Hüsnü Dayı anahtarı çevirdi. Yirmi yıllık sessizlik bozuldu.",
                () => doorEverOpened);
            story.Add("Bölüm 8 · Karanlık",
                "İçerisi zifiri karanlık. Fenerin ve Cemal'in lambası tek dostun.",
                () => mineDarkness.PlayerInside);
            story.Add("Bölüm 9 · Damar",
                "Göçük temizlendi. Taşların arasında bir şey parıldıyor...",
                () => !layout.Rubble.IsAvailable);

            // 15) Sesler (kodla üretilir) ve ortam sesi
            gameObject.AddComponent<SoundDirector>().Initialize(flow, inventory, wallet, layout, layout.Fishing.Game, dialogue, quests, playerWalk);

            log.Add("", "Gümüşdere'ye hoş geldin ağa! Köprü kalkık; Cemal sığ geçitten geçip vinci çevirebilir ama önce ona bir can yeleği lazım. Sağ üstteki hedeflere bak.", Color.white);
        }

        private NpcDialogueController SetUpGuard(WorldLayout layout, Transform parent, Transform player,
            IInventory inventory, System.Action<bool> setControls, PlayerInteractor interactor)
        {
            GameObject body = CharacterFactory.CreateGuardBody(parent, layout.GuardSpot);
            WalkBob.Attach(body);
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

            dialogue.RegisterAction(NpcAction.OpenMine, new OpenMineAction(layout.Door, persona.OpenThreshold, inventory, () => layout.Beams.Delivered));
            dialogue.RegisterAction(NpcAction.CloseMine, new CloseMineAction(layout.Door));
            dialogue.RegisterAction(NpcAction.KickOut, new KickOutAction(guard, persona.KickThreshold));
            dialogue.SetMineStateSource(() => layout.Door.IsOpen);
            dialogue.SetExtraStateSource(() => Gumusdere.Mining.MineEntryRule.DescribeBeams(layout.Beams.Delivered));
            dialogue.SetRuleHintSource(text => Gumusdere.Mining.MineEntryRule.IsAboutDoor(text)
                ? Gumusdere.Mining.MineEntryRule.HintForNpc(dialogue.Trust.Value, persona.OpenThreshold, layout.Door.IsOpen, inventory, layout.Beams.Delivered)
                : null);
            return dialogue;
        }

        private ShopView SetUpShop(WorldLayout layout, Transform parent, IInventory inventory, IWallet wallet,
            System.Action<bool> setControls, IMessageLog log)
        {
            GameObject nazmi = CharacterFactory.CreateShopkeeperBody(parent, layout.ShopkeeperSpot, 180f);
            WalkBob.Attach(nazmi);
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
