using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gumusdere.World
{
    /// <summary>
    /// Işık, ortam rengi, sis ve görüntü sonrası efektler: sahneye sıcak bir "akşamüstü köy" havası verir.
    /// Sahnedeki mevcut Directional Light'ı kullanır, yoksa bir tane oluşturur.
    /// Efektler (bloom, renk düzeltme, beyaz dengesi, vinyet, tonlama) kodla kurulan bir global Volume ile uygulanır;
    /// şablon sahnedeki Volume'dan daha yüksek öncelikli olduğu için onun ayarlarını ezer.
    /// </summary>
    public static class EnvironmentSetup
    {
        private static readonly Color HazeColor = new(0.86f, 0.83f, 0.76f);

        public static void Apply(Camera camera)
        {
            Light sun = FindSun();
            sun.transform.rotation = Quaternion.Euler(40f, -40f, 0f); // biraz daha alçak güneş: uzun, yumuşak gölgeler
            sun.color = new Color(1f, 0.89f, 0.74f);
            sun.intensity = 1.65f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.60f, 0.68f, 0.84f);
            RenderSettings.ambientEquatorColor = new Color(0.64f, 0.60f, 0.50f);
            RenderSettings.ambientGroundColor = new Color(0.38f, 0.32f, 0.26f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = HazeColor;
            RenderSettings.fogStartDistance = 40f;
            RenderSettings.fogEndDistance = 115f;

            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = HazeColor;
                camera.allowMSAA = true;
                camera.allowHDR = true;
                UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
                if (data != null) data.renderPostProcessing = true;
            }

            CreatePostProcessing();
        }

        private static void CreatePostProcessing()
        {
            var go = new GameObject("Gorsel_Efektler");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Gumusdere_Profil";

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.75f);
            bloom.scatter.Override(0.72f);
            bloom.tint.Override(new Color(1f, 0.9f, 0.78f));

            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.1f);
            color.contrast.Override(12f);
            color.saturation.Override(14f);
            color.colorFilter.Override(new Color(1f, 0.97f, 0.93f));

            WhiteBalance white = profile.Add<WhiteBalance>(true);
            white.temperature.Override(9f);
            white.tint.Override(2f);

            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.27f);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(new Color(0.12f, 0.07f, 0.04f));

            Tonemapping tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);

            volume.sharedProfile = profile;
        }

        private static Light FindSun()
        {
            // Önce sahnenin ışık ayarındaki güneş, sonra şablon sahnedeki "Directional Light" nesnesi.
            if (RenderSettings.sun != null) return RenderSettings.sun;

            GameObject existing = GameObject.Find("Directional Light");
            if (existing != null && existing.TryGetComponent(out Light found) && found.type == LightType.Directional)
            {
                return found;
            }

            var go = new GameObject("Directional Light");
            Light created = go.AddComponent<Light>();
            created.type = LightType.Directional;
            return created;
        }
    }
}
