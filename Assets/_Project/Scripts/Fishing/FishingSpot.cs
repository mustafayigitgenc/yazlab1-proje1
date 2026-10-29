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
        private Transform bobber;
        private Vector3 bobberRest;
        private float readyTime;

        public FishingMinigame Game => game;
        public Vector3 Position => transform.position;
        public float UseRadius => 1.8f;

        public void Initialize(Vector3 waterPoint)
        {
            bobberRest = waterPoint;
            bobber = Shapes.Sphere("Samandira", transform, Vector3.zero, Vector3.one * 0.22f, Palette.Lever, false).transform;
            bobber.position = bobberRest;
            bobber.gameObject.SetActive(false);
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
            game.Begin(
                biteDelay: Random.Range(1.2f, 3f),
                zoneStart: Random.Range(0.15f, 0.7f),
                zoneWidth: 0.18f,
                needleSpeed: Random.Range(0.9f, 1.4f));

            bobber.gameObject.SetActive(true);
            context.Info("Olta atıldı... Şamandıra batınca BOŞLUK ile çek!");
        }

        private void Update()
        {
            if (!game.IsActive) return;

            game.Tick(Time.deltaTime);
            AnimateBobber();

            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
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

        private void AnimateBobber()
        {
            float bob = Mathf.Sin(Time.time * 4f) * 0.03f;
            float dip = game.Current == FishingMinigame.Phase.Reeling ? -0.12f : 0f;
            bobber.position = bobberRest + new Vector3(0f, bob + dip, 0f);
        }

        private void Finish(bool caught, string failMessage)
        {
            game.End();
            bobber.gameObject.SetActive(false);
            readyTime = Time.time + Cooldown;

            if (caught)
            {
                activeContext.Inventory.Add(ItemType.Trout);
                activeContext.Info($"Yakaladın! +1 {ItemInfo.DisplayName(ItemType.Trout)}");
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
