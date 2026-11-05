using UnityEngine;

namespace Gumusdere.Ambience
{
    /// <summary>Ağaç yapraklarının rüzgârda hafifçe salınması. Her ağaç farklı fazda sallanır.</summary>
    public sealed class WindSway : MonoBehaviour
    {
        private float seed;
        private float strength = 1f;

        public void Initialize(float swayStrength)
        {
            strength = swayStrength;
        }

        private void Start()
        {
            seed = (transform.position.x * 0.37f) + (transform.position.z * 0.21f);
        }

        private void Update()
        {
            float t = Time.time;
            float gust = 0.6f + 0.4f * Mathf.PerlinNoise(t * 0.15f, seed);
            float x = Mathf.Sin(t * 0.9f + seed) * 1.8f * gust * strength;
            float z = Mathf.Sin(t * 0.7f + seed * 1.3f) * 2.4f * gust * strength;
            transform.localRotation = Quaternion.Euler(x, 0f, z);
        }
    }
}
