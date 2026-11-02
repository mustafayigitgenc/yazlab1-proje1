using System;
using System.Collections;
using UnityEngine;

namespace Gumusdere.World
{
    /// <summary>
    /// Maden kapısı. Açılınca yukarı kayar ve çarpışması kalkar; kapanınca geri iner.
    /// Opened / Closed olayları NavMesh'i ve mesaj kutusunu günceller.
    /// </summary>
    public sealed class MineDoor : MonoBehaviour
    {
        [SerializeField] private float moveDuration = 2f;
        [SerializeField] private float liftHeight = 3.4f;

        private Vector3 closedPosition;
        private Collider body;
        private Coroutine moving;

        public bool IsOpen { get; private set; }

        public event Action Opened;
        public event Action Closed;

        private void Awake()
        {
            closedPosition = transform.position;
            body = GetComponent<Collider>();
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            Restart(MoveRoutine(closedPosition + Vector3.up * liftHeight, true));
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (body != null) body.enabled = true; // iner inmez yol kapansın
            Restart(MoveRoutine(closedPosition, false));
        }

        private void Restart(IEnumerator routine)
        {
            if (moving != null) StopCoroutine(moving);
            moving = StartCoroutine(routine);
        }

        private IEnumerator MoveRoutine(Vector3 target, bool opening)
        {
            Vector3 start = transform.position;
            float t = 0f;

            while (t < moveDuration)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / moveDuration));
                yield return null;
            }

            transform.position = target;
            moving = null;

            if (opening)
            {
                if (body != null) body.enabled = false;
                Opened?.Invoke();
            }
            else
            {
                Closed?.Invoke();
            }
        }
    }
}
