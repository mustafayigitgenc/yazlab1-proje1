using System;
using System.Collections;
using UnityEngine;

namespace Gumusdere.World
{
    /// <summary>Maden kapısı. Açılınca yukarı kayar ve çarpışması kalkar; Opened olayı NavMesh'i günceller.</summary>
    public sealed class MineDoor : MonoBehaviour
    {
        [SerializeField] private float openDuration = 2f;
        [SerializeField] private float liftHeight = 3.4f;

        public bool IsOpen { get; private set; }

        public event Action Opened;

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            StartCoroutine(OpenRoutine());
        }

        private IEnumerator OpenRoutine()
        {
            Vector3 start = transform.position;
            Vector3 end = start + Vector3.up * liftHeight;
            float t = 0f;

            while (t < openDuration)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t / openDuration));
                yield return null;
            }

            transform.position = end;
            Collider body = GetComponent<Collider>();
            if (body != null) body.enabled = false;
            Opened?.Invoke();
        }
    }
}
