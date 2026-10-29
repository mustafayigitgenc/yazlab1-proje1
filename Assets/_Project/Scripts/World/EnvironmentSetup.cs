using UnityEngine;
using UnityEngine.Rendering;

namespace Gumusdere.World
{
    /// <summary>
    /// Işık, ortam rengi ve sis: sahneye sıcak, "köy" havası verir.
    /// Sahnedeki mevcut Directional Light'ı kullanır, yoksa bir tane oluşturur.
    /// </summary>
    public static class EnvironmentSetup
    {
        private static readonly Color SkyColor = new(0.70f, 0.83f, 0.95f);

        public static void Apply(Camera camera)
        {
            Light sun = FindSun();
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            sun.color = new Color(1f, 0.95f, 0.86f);
            sun.intensity = 1.7f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.7f;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.86f);
            RenderSettings.ambientEquatorColor = new Color(0.56f, 0.62f, 0.56f);
            RenderSettings.ambientGroundColor = new Color(0.36f, 0.33f, 0.28f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = SkyColor;
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 120f;

            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = SkyColor;
                camera.allowMSAA = true;
            }
        }

        private static Light FindSun()
        {
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional) return light;
            }

            var go = new GameObject("Directional Light");
            Light created = go.AddComponent<Light>();
            created.type = LightType.Directional;
            return created;
        }
    }
}
