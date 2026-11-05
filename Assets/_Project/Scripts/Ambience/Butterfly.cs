using UnityEngine;

namespace Gumusdere.Ambience
{
    /// <summary>Çiçeklerin üstünde gelişigüzel uçan, kanat çırpan kelebek.</summary>
    public sealed class Butterfly : MonoBehaviour
    {
        private Vector3 home;
        private float radius;
        private float seed;
        private Transform leftWing;
        private Transform rightWing;

        public void Initialize(Vector3 homePoint, float wanderRadius, Color color)
        {
            home = homePoint;
            radius = wanderRadius;
            seed = Random.value * 100f;

            leftWing = Wing("Kanat_Sol", color, -1f);
            rightWing = Wing("Kanat_Sag", color, 1f);
            Ambient.Decor(PrimitiveType.Capsule, "Govde", transform, Vector3.zero, new Vector3(0.04f, 0.07f, 0.04f), new Color(0.2f, 0.15f, 0.1f),
                Quaternion.Euler(90f, 0f, 0f));
        }

        private Transform Wing(string name, Color color, float side)
        {
            var hinge = new GameObject(name).transform;
            hinge.SetParent(transform, false);
            Ambient.Decor(PrimitiveType.Cube, "Kanat", hinge, new Vector3(0.07f * side, 0f, 0f), new Vector3(0.13f, 0.01f, 0.11f), color);
            return hinge;
        }

        private void Update()
        {
            float t = Time.time * 0.35f;
            float nx = Mathf.PerlinNoise(seed, t) * 2f - 1f;
            float nz = Mathf.PerlinNoise(t, seed) * 2f - 1f;
            float ny = Mathf.PerlinNoise(seed * 0.5f, t * 1.7f);
            Vector3 target = home + new Vector3(nx * radius, 0.5f + ny * 1.1f, nz * radius);

            Vector3 old = transform.position;
            transform.position = Vector3.Lerp(old, target, 1f - Mathf.Exp(-2f * Time.deltaTime));
            Vector3 dir = transform.position - old;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.00001f) transform.rotation = Quaternion.LookRotation(dir);

            float flap = Mathf.Sin(Time.time * 22f + seed) * 60f;
            leftWing.localRotation = Quaternion.Euler(0f, 0f, flap);
            rightWing.localRotation = Quaternion.Euler(0f, 0f, -flap);
        }
    }
}
