using UnityEngine;
using UnityEngine.Rendering;

namespace Gumusdere.Ambience
{
    /// <summary>
    /// Madenin karanlığı: oyuncu madene girince ortam ışığı yavaşça kararır, güneş zayıflar (çatı zaten gölge düşürür);
    /// içerisi oyuncunun feneri, Cemal'in baret lambası ve birkaç loş lambayla aydınlanır. Çıkınca gün ışığı geri gelir.
    /// </summary>
    public sealed class MineDarkness : MonoBehaviour
    {
        private static readonly Color CaveSky = new(0.035f, 0.035f, 0.05f);
        private static readonly Color CaveEquator = new(0.03f, 0.026f, 0.022f);
        private static readonly Color CaveGround = new(0.015f, 0.012f, 0.01f);

        private Transform player;
        private Transform companion;
        private GameObject companionLamp;
        private float mineStartZ;
        private Light sun;
        private float daySun;
        private Color daySky, dayEquator, dayGround;
        private SphericalHarmonicsL2 dayProbe;
        private SphericalHarmonicsL2 caveProbe;
        private float blend;
        private float lastApplied = -1f;

        public bool PlayerInside { get; private set; }

        public void Initialize(Transform playerTransform, Transform cemal, GameObject cemalLamp, float mineBeginsAtZ)
        {
            player = playerTransform;
            companion = cemal;
            companionLamp = cemalLamp;
            mineStartZ = mineBeginsAtZ;
            sun = RenderSettings.sun;
            if (sun == null)
            {
                GameObject found = GameObject.Find("Directional Light");
                if (found != null) sun = found.GetComponent<Light>();
            }
            daySun = sun != null ? sun.intensity : 1f;
            daySky = RenderSettings.ambientSkyColor;
            dayEquator = RenderSettings.ambientEquatorColor;
            dayGround = RenderSettings.ambientGroundColor;

            // Ortam ışığı hem renklerle hem de doğrudan ortam ışığı sondasıyla (SH) değiştirilir;
            // böylece çalışma anında hangi yol kullanılırsa kullanılsın içerisi gerçekten kararır.
            dayProbe = RenderSettings.ambientProbe;
            caveProbe = new SphericalHarmonicsL2();
            caveProbe.AddAmbientLight(CaveEquator);
            caveProbe.AddDirectionalLight(Vector3.down, CaveSky, 0.5f);
        }

        private void Update()
        {
            if (player == null) return;

            PlayerInside = player.position.z > mineStartZ + 1.5f;

            if (companion != null && companionLamp != null)
            {
                bool cemalInside = companion.position.z > mineStartZ + 1.5f;
                if (companionLamp.activeSelf != cemalInside) companionLamp.SetActive(cemalInside);
            }

            // Tamamen dışarıdayken gündüz ayarlarını güncel tut (motor ortam sondasını açılışta bir kare sonra hesaplayabilir).
            if (!PlayerInside && blend <= 0f)
            {
                dayProbe = RenderSettings.ambientProbe;
                return;
            }
            blend = Mathf.MoveTowards(blend, PlayerInside ? 1f : 0f, Time.deltaTime * 0.8f);
            if (!Mathf.Approximately(blend, lastApplied))
            {
                lastApplied = blend;
                float k = Mathf.SmoothStep(0f, 1f, blend);
                RenderSettings.ambientSkyColor = Color.Lerp(daySky, CaveSky, k);
                RenderSettings.ambientEquatorColor = Color.Lerp(dayEquator, CaveEquator, k);
                RenderSettings.ambientGroundColor = Color.Lerp(dayGround, CaveGround, k);
                RenderSettings.ambientProbe = dayProbe * (1f - k) + caveProbe * k;
                if (sun != null) sun.intensity = Mathf.Lerp(daySun, daySun * 0.25f, k);
            }

        }

        private void OnDestroy()
        {
            // Sahne yeniden başlarken gün ışığı ayarları geri dönsün.
            if (lastApplied < 0f) return;
            RenderSettings.ambientSkyColor = daySky;
            RenderSettings.ambientEquatorColor = dayEquator;
            RenderSettings.ambientGroundColor = dayGround;
            RenderSettings.ambientProbe = dayProbe;
        }
    }
}
