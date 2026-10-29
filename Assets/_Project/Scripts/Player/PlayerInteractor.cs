using System.Collections.Generic;
using Gumusdere.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gumusdere.Player
{
    /// <summary>
    /// Oyuncunun E tuşu: yakındaki en uygun nesneyi bulur, ekranda ne yapılacağını gösterir ve kullanır.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        private readonly List<IPlayerInteractable> interactables = new();
        private PlayerContext context;

        /// <summary>Şu an E ile yapılabilecek eylem metni (yoksa null).</summary>
        public string CurrentPrompt { get; private set; }

        /// <summary>Balık tutma, diyalog gibi durumlarda kapatılır.</summary>
        public bool InteractionEnabled { get; set; } = true;

        public void Initialize(PlayerContext playerContext, IEnumerable<IPlayerInteractable> targets)
        {
            context = playerContext;
            interactables.AddRange(targets);
        }

        public void Register(IPlayerInteractable target)
        {
            if (target != null && !interactables.Contains(target)) interactables.Add(target);
        }

        private void Update()
        {
            CurrentPrompt = null;
            if (context == null || !InteractionEnabled) return;

            IPlayerInteractable best = FindBest(out string prompt);
            CurrentPrompt = prompt;

            Keyboard kb = Keyboard.current;
            if (best != null && kb != null && kb.eKey.wasPressedThisFrame)
            {
                best.Use(context);
            }
        }

        private IPlayerInteractable FindBest(out string prompt)
        {
            prompt = null;
            IPlayerInteractable best = null;
            float bestScore = float.MaxValue;
            Vector3 position = transform.position;

            foreach (IPlayerInteractable target in interactables)
            {
                Vector3 delta = target.Position - position;
                delta.y = 0f;
                float distance = delta.magnitude;
                if (distance > target.UseRadius) continue;

                string candidatePrompt = target.GetPrompt(context);
                if (candidatePrompt == null) continue;

                // Önündekini arkasındakine tercih et.
                float facing = distance > 0.01f ? Vector3.Dot(transform.forward, delta / distance) : 1f;
                float score = distance - facing * 0.6f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = target;
                    prompt = candidatePrompt;
                }
            }

            return best;
        }
    }
}
