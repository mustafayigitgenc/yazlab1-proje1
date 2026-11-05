using UnityEngine;

namespace Gumusdere.Ambience
{
    /// <summary>Ormanın gölgesinde yanıp sönen ateş böceği (bloom ile parlar).</summary>
    public sealed class Firefly : MonoBehaviour
    {
        private Vector3 home;
        private float seed;
        private Transform glow;

        public void Initialize(Vector3 homePoint)
        {
            home = homePoint;
            seed = Random.value * 100f;
            glow = Ambient.Decor(PrimitiveType.Sphere, "Isik", transform, Vector3.zero, Vector3.one * 0.08f,
                Ambient.Hdr(new Color(0.85f, 1f, 0.45f), 4f)).transform;
        }

        private void Update()
        {
            float t = Time.time * 0.25f;
            transform.position = home + new Vector3(
                (Mathf.PerlinNoise(seed, t) - 0.5f) * 3f,
                0.6f + Mathf.PerlinNoise(t, seed) * 1.6f,
                (Mathf.PerlinNoise(seed + 7f, t) - 0.5f) * 3f);
            float pulse = Mathf.Clamp01(Mathf.Sin(Time.time * 1.7f + seed) * 1.4f);
            glow.localScale = Vector3.one * (0.02f + 0.08f * pulse);
        }
    }
}
