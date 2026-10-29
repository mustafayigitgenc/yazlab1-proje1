using UnityEngine;

namespace Gumusdere.Companion.Sensing
{
    /// <summary>
    /// Cemal'in "gözleri": göz hizasından önüne doğru yelpaze şeklinde ışınlar (Raycast) atar.
    /// Her ışın için engelin mesafesini ve açısını ölçer. Ayrıca hedefe tek bir ışınla
    /// bakıp arada engel olup olmadığını kontrol eder.
    /// </summary>
    public sealed class RaycastSensor : MonoBehaviour, IObstacleSensor
    {
        [SerializeField] private int rayCount = 7;
        [SerializeField] private float fieldOfView = 120f;
        [SerializeField] private float range = 6f;
        [SerializeField] private float eyeHeight = 0.8f;

        private float[] angles;
        private float[] distances;

        public SensorReading Latest { get; private set; }
        public float Range => range;
        public float EyeHeight => eyeHeight;

        private void Awake()
        {
            rayCount = Mathf.Max(3, rayCount);
            angles = new float[rayCount];
            for (int i = 0; i < rayCount; i++)
            {
                angles[i] = -fieldOfView * 0.5f + fieldOfView * i / (rayCount - 1);
            }
            distances = new float[rayCount];
            Scan();
        }

        public Vector3 Eye => transform.position + Vector3.up * eyeHeight;

        public Vector3 RayDirection(int index) =>
            Quaternion.Euler(0f, angles[index], 0f) * transform.forward;

        public SensorReading Scan()
        {
            Vector3 eye = Eye;
            for (int i = 0; i < rayCount; i++)
            {
                distances[i] = Physics.Raycast(eye, RayDirection(i), out RaycastHit hit, range,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    ? hit.distance
                    : range;
            }

            Latest = new SensorReading(angles, (float[])distances.Clone(), range);
            return Latest;
        }

        public bool CanSee(Transform target, Vector3 point)
        {
            Vector3 eye = Eye;
            Vector3 toTarget = point - eye;
            float distance = toTarget.magnitude;
            if (distance < 0.01f) return true;

            if (!Physics.Raycast(eye, toTarget / distance, out RaycastHit hit, distance + 0.5f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            return hit.transform == target || hit.transform.IsChildOf(target);
        }
    }
}
