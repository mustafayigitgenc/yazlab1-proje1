using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gumusdere.World
{
    /// <summary>
    /// Nehrin üstündeki asma köprü. Başta kalkıktır; vinç çevrilince iner.
    /// İndiğinde oyuncuyu tutan görünmez kapı duvarlarını kapatır ve Lowered olayını tetikler
    /// (NavMesh bu olayla yeniden hesaplanır, böylece Cemal de köprüyü kullanabilir).
    /// </summary>
    public sealed class Drawbridge : MonoBehaviour
    {
        private const float RaisedAngle = 80f;

        [SerializeField] private float lowerDuration = 2.5f;

        private readonly List<GameObject> playerGates = new();

        public bool IsLowered { get; private set; }
        public bool IsMoving { get; private set; }

        public event Action Lowered;

        public void Initialize(IEnumerable<GameObject> gates)
        {
            playerGates.AddRange(gates);
            transform.localRotation = Quaternion.Euler(RaisedAngle, 0f, 0f);
        }

        public void Lower()
        {
            if (IsLowered || IsMoving) return;
            StartCoroutine(LowerRoutine());
        }

        private IEnumerator LowerRoutine()
        {
            IsMoving = true;
            float elapsed = 0f;

            while (elapsed < lowerDuration)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, elapsed / lowerDuration);
                transform.localRotation = Quaternion.Euler(Mathf.Lerp(RaisedAngle, 0f, k), 0f, 0f);
                yield return null;
            }

            transform.localRotation = Quaternion.identity;
            foreach (GameObject gate in playerGates)
            {
                if (gate != null) gate.SetActive(false);
            }

            IsMoving = false;
            IsLowered = true;
            Lowered?.Invoke();
        }
    }
}
