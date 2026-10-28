using UnityEngine;

namespace Gumusdere.Player
{
    /// <summary>
    /// Stardew benzeri, yukarıdan eğik bakan takip kamerası. Açı sabittir, sadece konum yumuşakça izler.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new(0f, 13f, -10.5f);
        [SerializeField] private float pitch = 52f;
        [SerializeField] private float smoothTime = 0.18f;

        private Transform target;
        private Vector3 velocity;

        public void Initialize(Transform followTarget)
        {
            target = followTarget;
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
            transform.position = Vector3.SmoothDamp(transform.position, target.position + offset, ref velocity, smoothTime);
        }
    }
}
