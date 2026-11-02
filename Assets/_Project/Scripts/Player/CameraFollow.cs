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

        private void LateUpdate()
        {
            if (target == null) return;

            float goal = target.position.z > mineStartZ ? 1f : 0f;
            mineBlend = Mathf.MoveTowards(mineBlend, goal, blendSpeed * Time.deltaTime);

            Vector3 currentOffset = Vector3.Lerp(offset, mineOffset, mineBlend);
            transform.rotation = Quaternion.Euler(Mathf.Lerp(pitch, minePitch, mineBlend), 0f, 0f);
            transform.position = Vector3.SmoothDamp(transform.position, target.position + currentOffset, ref velocity, smoothTime);
        }
    }
}
