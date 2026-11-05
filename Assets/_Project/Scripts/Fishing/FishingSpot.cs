using Gumusdere.Interaction;
using Gumusdere.Items;
using Gumusdere.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gumusdere.Fishing
{
    /// <summary>
    /// İskelenin ucu: E ile olta atılır, Boşluk (Space) ile çekilir, Esc ile vazgeçilir.
    /// Balık tutarken oyuncu yürüyemez. Kurallar FishingMinigame sınıfındadır; bu sınıf
    /// sadece girdiyi, şamandırayı ve sonucu yönetir.
    /// </summary>
    public sealed class FishingSpot : MonoBehaviour, IPlayerInteractable
    {
        private const float Cooldown = 1.5f;

        private readonly FishingMinigame game = new();
        private PlayerContext activeContext;
        private FishingRig rig;
        private bool biteShown;
        private float readyTime;

        public FishingMinigame Game => game;
        public Vector3 Position => transform.position;
        public float UseRadius => 1.8f;

        public void Initialize(Vector3 waterPoint)
        {
            rig = gameObject.AddComponent<FishingRig>();
            rig.Initialize(waterPoint);
        }

        public string GetPrompt(PlayerContext context)
        {
            if (game.IsActive || Time.time < readyTime) return null;
            return "Olta at (balık tut)";
        }

        public void Use(PlayerContext context)
        {
            if (game.IsActive || Time.time < readyTime) return;

            activeContext = context;
            context.SetMovementEnabled(false);
            FishSpecies fish = FishSpecies.Pick(FishSpecies.Lake, Random.value);
            game.Begin(fish, biteDelay: Random.Range(1.5f, 3.5f), zoneStart: Random.Range(0.1f, 0.8f));

            biteShown = false;
            rig.BeginCast(context.Player.transform);
            context.Info("Olta atıldı... Şamandıra batınca BOŞLUK ile çek!");
        }

        private void Update()
        {
            if (!game.IsActive) return;

            game.Tick(Time.deltaTime);
            if (!biteShown && game.Current == FishingMinigame.Phase.Reeling)
            {
                biteShown = true;
                rig.Bite();
            }

            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                Gumusdere.Core.InputGate.ConsumeEscape();
                Finish(false, "Oltayı topladın.");
            }
            else if (kb.spaceKey.wasPressedThisFrame)
            {
                if (game.Current == FishingMinigame.Phase.Waiting) Finish(false, "Çok erken çektin, balık kaçtı!");
                else Finish(game.InZone, game.InZone ? null : "Kaçırdın! İbre yeşil bölgedeyken çekmelisin.");
            }
            else if (game.HasTimedOut)
            {
                Finish(false, "Balık yemi alıp kaçtı.");
            }
        }

        private void Finish(bool caught, string failMessage)
        {
            ItemType fishItem = game.Fish.Item;
            game.End();
            if (caught) rig.EndCaught(ItemInfo.Tint(fishItem));
            else rig.EndFailed();
            readyTime = Time.time + Cooldown;

            if (caught)
            {
                ItemType caughtItem = game.Fish.Item;
                activeContext.Inventory.Add(caughtItem);
                string rare = game.Fish.Weight <= 15 ? " Nadir bir balık!" : "";
                activeContext.Info($"Yakaladın! +1 {ItemInfo.DisplayName(caughtItem)}.{rare}");
            }
            else
            {
                activeContext.Info(failMessage);
            }

            activeContext.SetMovementEnabled(true);
            activeContext = null;
        }
    }
}
