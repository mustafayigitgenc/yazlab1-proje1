using UnityEngine;

namespace Gumusdere.Ambience
{
    /// <summary>Su yüzeyinde rastgele yerlerde beliren ve kaybolan parıltılar.</summary>
    public sealed class WaterGlints : MonoBehaviour
    {
        private Transform[] glints;
        private float[] start;
        private float[] life;
        private System.Func<Vector3> pickPoint;

        public void Initialize(int count, System.Func<Vector3> randomSurfacePoint)
        {
            pickPoint = randomSurfacePoint;
            glints = new Transform[count];
            start = new float[count];
            life = new float[count];
            Color white = Ambient.Hdr(new Color(1f, 1f, 0.95f), 2.6f);
            for (int i = 0; i < count; i++)
            {
                glints[i] = Ambient.Decor(PrimitiveType.Cube, "Parilti", transform, pickPoint(), Vector3.zero, white).transform;
                Respawn(i, Time.time - Random.value * 2f);
            }
        }

        private void Respawn(int i, float now)
        {
            glints[i].position = pickPoint();
            glints[i].rotation = Quaternion.Euler(0f, Random.value * 180f, 0f);
            start[i] = now;
            life[i] = 0.8f + Random.value * 1.6f;
        }

        private void Update()
        {
            float now = Time.time;
            for (int i = 0; i < glints.Length; i++)
            {
                float k = (now - start[i]) / life[i];
                if (k >= 1f)
                {
                    Respawn(i, now);
                    k = 0f;
                }
                float s = Mathf.Sin(k * Mathf.PI);
                glints[i].localScale = new Vector3(0.45f * s, 0.01f, 0.07f * s);
            }
        }
    }
}
