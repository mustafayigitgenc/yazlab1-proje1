using System.Collections;
using Gumusdere.World;
using UnityEngine;

namespace Gumusdere.Interaction
{
    /// <summary>
    /// Karşı kıyıdaki köprü vinci. Cemal çevirince asma köprü iner.
    /// Oyuncu bu görevi ancak nehrin güney kıyısındaki köprü başına yakınken emredebilir.
    /// </summary>
    public sealed class Winch : MonoBehaviour, IInteractable
    {
        private const float RequestRadius = 11f;

        private Drawbridge bridge;
        private Transform lever;
        private Vector3 interactionPoint;
        private Vector3 requestCenter;
        private bool used;

        public string DisplayName => "Köprü vinci";
        public string CommandText => "Cemal'e köprüyü indirt";
        public Vector3 InteractionPoint => interactionPoint;
        public Transform Root => transform;
        public Vector3 LookPoint => transform.position + Vector3.up * 0.5f;
        public bool IsAvailable => !used && bridge != null && !bridge.IsLowered && !bridge.IsMoving;

        public void Initialize(Drawbridge targetBridge, Transform leverPart, Vector3 standPoint, Vector3 bridgeSouthEnd)
        {
            bridge = targetBridge;
            lever = leverPart;
            interactionPoint = standPoint;
            requestCenter = bridgeSouthEnd;
        }

        public bool CanBeRequestedFrom(Vector3 playerPosition)
        {
            Vector3 d = playerPosition - requestCenter;
            d.y = 0f;
            return d.magnitude <= RequestRadius;
        }

        public void Interact(GameObject actor)
        {
            if (!IsAvailable) return;
            used = true;
            Debug.Log($"[Vinç] {actor.name} vinci çevirdi.");
            StartCoroutine(SpinLever());
            bridge.Lower();
        }

        private IEnumerator SpinLever()
        {
            if (lever == null) yield break;
            float t = 0f;
            Quaternion start = lever.localRotation;
            while (t < 2.5f)
            {
                t += Time.deltaTime;
                lever.localRotation = start * Quaternion.Euler(0f, t * 360f, 0f);
                yield return null;
            }
        }
    }
}
