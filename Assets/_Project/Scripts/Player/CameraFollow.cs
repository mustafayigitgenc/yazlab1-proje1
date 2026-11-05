using UnityEngine;

namespace Gumusdere.Player
{
    /// <summary>
    /// Stardew benzeri, yukarıdan eğik bakan takip kamerası. Konum yumuşakça izler.
    /// Oyuncu madene girince kamera dikleşir; böylece dağın kayaları görüşü kapatmaz.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new(0f, 13f, -10.5f);
        [SerializeField] private float pitch = 52f;
        [SerializeField] private Vector3 mineOffset = new(0f, 16f, -4.5f);
        [SerializeField] private float minePitch = 74f;
        [SerializeField] private float smoothTime = 0.18f;
        [SerializeField] private float blendSpeed = 3f;

        private Transform target;
        private System.Func<bool> showcase = () => false;
        private float orbitAngle = 200f;
        private static readonly Vector3 ShowcaseCenter = new(-2f, 0f, 14f);
        private Vector3 velocity;
        private float mineStartZ = float.MaxValue;
        private float mineBlend;

        public void Initialize(Transform followTarget, float mineBeginsAtZ)
        {
            target = followTarget;
            mineStartZ = mineBeginsAtZ;
            transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
            transform.position = target.position + offset;

            Camera cam = GetComponent<Camera>();
            cam.fieldOfView = 42f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 200f;
        }

        /// <summary>Başlık ekranında kamera köyün üstünde yavaşça döner; koşul false olunca oyuncuya yumuşakça döner.</summary>
        public void SetShowcase(System.Func<bool> when) => showcase = when ?? (() => false);

        private void LateUpdate()
        {
            if (target == null) return;

            if (showcase())
            {
                orbitAngle += 5f * Time.unscaledDeltaTime;
                float rad = orbitAngle * Mathf.Deg2Rad;
                Vector3 orbit = ShowcaseCenter + new Vector3(Mathf.Sin(rad) * 44f, 30f, -Mathf.Cos(rad) * 44f);
                transform.position = Vector3.SmoothDamp(transform.position, orbit, ref velocity, 0.6f, Mathf.Infinity, Time.unscaledDeltaTime);
                Quaternion look = Quaternion.LookRotation(ShowcaseCenter + Vector3.up * 2f - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime));
                return;
            }

            float goal = target.position.z > mineStartZ ? 1f : 0f;
            mineBlend = Mathf.MoveTowards(mineBlend, goal, blendSpeed * Time.deltaTime);

            Vector3 currentOffset = Vector3.Lerp(offset, mineOffset, mineBlend);
            Quaternion goalRotation = Quaternion.Euler(Mathf.Lerp(pitch, minePitch, mineBlend), 0f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, goalRotation, 1f - Mathf.Exp(-8f * Time.deltaTime));
            transform.position = Vector3.SmoothDamp(transform.position, target.position + currentOffset, ref velocity, smoothTime);
        }
    }
}
