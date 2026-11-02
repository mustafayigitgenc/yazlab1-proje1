using System;
using System.Collections;
using Gumusdere.Interaction;
using UnityEngine;

namespace Gumusdere.Mining
{
    /// <summary>
    /// Madenin en dibindeki gümüş damarı. Oyuncu E ile kazmayla 3 kez vurur; damar açılınca oyun kazanılır.
    /// </summary>
    public sealed class SilverVein : MonoBehaviour, IPlayerInteractable
    {
        private const int HitsNeeded = 3;
        private const float HitCooldown = 0.5f;

        private Transform shell;
        private GameObject treasure;
        private int hits;
        private float nextHitTime;

        public bool IsMined => hits >= HitsNeeded;
        public Vector3 Position => transform.position;
        public float UseRadius => 2.4f;

        /// <summary>Damar tamamen açıldığında.</summary>
        public event Action Mined;

        public void Initialize(Transform rockShell, GameObject silverInside)
        {
            shell = rockShell;
            treasure = silverInside;
            treasure.SetActive(false);
        }

        public string GetPrompt(PlayerContext context)
        {
            if (IsMined) return null;
            return $"Kazmayla vur ({hits}/{HitsNeeded})";
        }

        public void Use(PlayerContext context)
        {
            if (IsMined || Time.time < nextHitTime) return;
            nextHitTime = Time.time + HitCooldown;
            hits++;
            StartCoroutine(Shake());

            if (hits < HitsNeeded)
            {
                context.Info(hits == 1 ? "Kaya çatladı..." : "Bir vuruş daha!");
                return;
            }

            shell.gameObject.SetActive(false);
            treasure.SetActive(true);
            context.Info("Gümüş damarı ortaya çıktı!");
            Mined?.Invoke();
        }

        private IEnumerator Shake()
        {
            if (shell == null) yield break;
            Vector3 origin = shell.localPosition;
            float t = 0f;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                shell.localPosition = origin + UnityEngine.Random.insideUnitSphere * 0.06f;
                yield return null;
            }
            shell.localPosition = origin;
        }
    }
}
