using System;
using System.Collections;
using Gumusdere.Interaction;
using UnityEngine;

namespace Gumusdere.Mining
{
    /// <summary>
    /// Madenin iç odasına giden yolu kapatan göçük taşları. Oyuncu kaldıramaz;
    /// Cemal'e emredilir (F), Cemal NavMesh ile yanına gidip temizler.
    /// Temizlenince çarpışma kalkar ve Cleared olayı NavMesh'i günceller.
    /// </summary>
    public sealed class RubblePile : MonoBehaviour, IInteractable
    {
        private const float RequestRadius = 9f;

        private Vector3 standPoint;
        private bool cleared;

        public string DisplayName => "Göçük taşları";
        public string CommandText => "Cemal'e göçüğü temizlet";
        public Vector3 InteractionPoint => standPoint;
        public Transform Root => transform;
        public Vector3 LookPoint => transform.position + Vector3.up * 0.6f;
        public bool IsAvailable => !cleared;

        public event Action Cleared;

        public void Initialize(Vector3 workerStandPoint)
        {
            standPoint = workerStandPoint;
        }

        public bool CanBeRequestedFrom(Vector3 playerPosition)
        {
            Vector3 d = playerPosition - transform.position;
            d.y = 0f;
            return d.magnitude <= RequestRadius;
        }

        public void Interact(GameObject actor)
        {
            if (cleared) return;
            cleared = true;
            Debug.Log($"[Maden] {actor.name} göçüğü temizledi.");
            StartCoroutine(ClearRoutine());
        }

        private IEnumerator ClearRoutine()
        {
            foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;

            Vector3 start = transform.localScale;
            float t = 0f;
            while (t < 1.2f)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(start, new Vector3(start.x, 0.05f, start.z), t / 1.2f);
                yield return null;
            }

            gameObject.SetActive(false);
            Cleared?.Invoke();
        }
    }
}
