using System.Collections;
using Gumusdere.Ambience;
using Gumusdere.Core;
using Gumusdere.Spawning;
using Gumusdere.World;
using UnityEngine;

namespace Gumusdere.Fishing
{
    /// <summary>
    /// Balık tutmanın görüntüsü (oyun kuralı değil): oyuncunun elinde olta kamışı, kamıştan şamandıraya misina,
    /// atma hareketi, şamandıranın yay çizerek suya düşmesi, su halkaları ve damlalar, balık vurunca şamandıranın batması
    /// ve kamışın eğilmesi, yakalanınca balığın sudan sıçrayıp oyuncuya gelmesi, kaçınca misinanın geri sarılması.
    /// </summary>
    public sealed class FishingRig : MonoBehaviour
    {
        private const float CastDuration = 0.75f;
        private const int LinePoints = 12;

        private Vector3 waterPoint;
        private Transform bobber;
        private LineRenderer line;
        private Transform player;
        private Transform rod;
        private Transform rodTip;
        private float castStart;
        private bool casting;
        private bool landed;
        private bool reeling;
        private bool ending;
        private float slack = 1f;
        private float nextNibble;

        public void Initialize(Vector3 waterSurfacePoint)
        {
            waterPoint = waterSurfacePoint;
            bobber = new GameObject("Samandira").transform;
            bobber.SetParent(transform, false);
            Ambient.Decor(PrimitiveType.Sphere, "Samandira_Alt", bobber, Vector3.zero, Vector3.one * 0.2f, Color.white);
            Ambient.Decor(PrimitiveType.Sphere, "Samandira_Ust", bobber, new Vector3(0f, 0.07f, 0f), new Vector3(0.2f, 0.12f, 0.2f), new Color(0.9f, 0.15f, 0.1f));
            Ambient.Decor(PrimitiveType.Cylinder, "Samandira_Cubuk", bobber, new Vector3(0f, 0.17f, 0f), new Vector3(0.03f, 0.08f, 0.03f), new Color(0.9f, 0.15f, 0.1f));
            bobber.gameObject.SetActive(false);

            line = gameObject.AddComponent<LineRenderer>();
            line.positionCount = LinePoints;
            line.widthMultiplier = 0.018f;
            line.numCapVertices = 2;
            line.sharedMaterial = MaterialLibrary.Get(new Color(0.92f, 0.92f, 0.88f));
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.enabled = false;
        }

        // ------------------------------------------------------------------ Olaylar (FishingSpot çağırır)

        public void BeginCast(Transform playerRoot)
        {
            StopAllCoroutines();
            Cleanup();
            player = playerRoot;

            // Oyuncu suya dönsün
            Vector3 toWater = waterPoint - player.position;
            toWater.y = 0f;
            if (toWater.sqrMagnitude > 0.01f) player.rotation = Quaternion.LookRotation(toWater);

            // Sağ elde olta kamışı (kol pivotunun altında)
            Transform hand = player.FindDeep(HumanoidBody.RightArm) ?? player;
            rod = new GameObject("Olta_Kamisi").transform;
            rod.SetParent(hand, false);
            rod.localPosition = hand == player ? new Vector3(0.35f, 0.75f, 0.2f) : new Vector3(0f, -0.52f, 0.04f);
            Ambient.Decor(PrimitiveType.Cylinder, "Kamis", rod, new Vector3(0f, 0.85f, 0f), new Vector3(0.035f, 0.85f, 0.035f), Palette.DarkWood);
            Ambient.Decor(PrimitiveType.Cylinder, "Sap", rod, new Vector3(0f, 0.12f, 0f), new Vector3(0.06f, 0.12f, 0.06f), new Color(0.2f, 0.2f, 0.22f));
            Ambient.Decor(PrimitiveType.Cylinder, "Makara", rod, new Vector3(0f, 0.3f, 0.05f), new Vector3(0.09f, 0.03f, 0.09f), Palette.Stone, Quaternion.Euler(90f, 0f, 0f));
            rodTip = new GameObject("Uc").transform;
            rodTip.SetParent(rod, false);
            rodTip.localPosition = new Vector3(0f, 1.7f, 0f);
            SetRodAngle(-15f);

            bobber.gameObject.SetActive(true);
            bobber.position = rodTip.position;
            line.enabled = true;
            castStart = Time.time;
            casting = true;
            landed = false;
            reeling = false;
            ending = false;
            slack = 0.2f;
            nextNibble = Time.time + Random.Range(0.8f, 1.6f);
        }

        /// <summary>Balık vurdu: şamandıra batar, kamış eğilir, misina gerilir.</summary>
        public void Bite()
        {
            if (!landed || reeling) return;
            reeling = true;
            Splash(bobber.position, 0.9f, 4);
        }

        public void EndCaught(Color fishColor)
        {
            if (rod == null) return;
            StopAllCoroutines();
            StartCoroutine(CatchAnimation(fishColor));
        }

        public void EndFailed()
        {
            if (rod == null) return;
            StopAllCoroutines();
            StartCoroutine(ReelBack());
        }

        // ------------------------------------------------------------------ Kare kare

        private void LateUpdate()
        {
            if (rod == null || ending) { if (rod != null) UpdateLine(); return; }
            float t = Time.time - castStart;

            if (casting)
            {
                // Kamış geriye alınır, hızla öne savrulur; şamandıra yay çizerek suya uçar.
                float swing = Mathf.Clamp01(t / 0.25f);
                SetRodAngle(Mathf.Lerp(-15f, 72f, EaseOut(swing)));
                float k = Mathf.Clamp01((t - 0.15f) / (CastDuration - 0.15f));
                Vector3 start = rodTip.position;
                Vector3 flat = Vector3.Lerp(start, waterPoint, k);
                bobber.position = flat + Vector3.up * (4f * k * (1f - k) * 2.2f);
                slack = Mathf.Lerp(0.1f, 1f, k);
                if (k >= 1f)
                {
                    casting = false;
                    landed = true;
                    Splash(waterPoint, 1.2f, 6);
                }
            }
            else if (reeling)
            {
                // Balık çekiştiriyor: şamandıra batıp çıkar, yana kayar; kamış eğilip titrer.
                float pull = Mathf.Sin(Time.time * 26f) * 0.035f;
                bobber.position = waterPoint + new Vector3(Mathf.Sin(Time.time * 7f) * 0.12f, -0.16f + pull, Mathf.Cos(Time.time * 5f) * 0.08f);
                SetRodAngle(78f + Mathf.Sin(Time.time * 30f) * 4f);
                slack = Mathf.MoveTowards(slack, 0.05f, Time.deltaTime * 4f);
            }
            else
            {
                // Bekleme: şamandıra dalgada sallanır, ara sıra minik vuruşlar (balık yemi kokluyor).
                float bob = Mathf.Sin(Time.time * 3.2f) * 0.025f;
                float nibble = 0f;
                if (Time.time > nextNibble)
                {
                    float n = Time.time - nextNibble;
                    nibble = -Mathf.Sin(Mathf.Clamp01(n / 0.25f) * Mathf.PI) * 0.06f;
                    if (n > 0.25f) nextNibble = Time.time + Random.Range(0.8f, 1.8f);
                }
                bobber.position = waterPoint + new Vector3(0f, bob + nibble, 0f);
                SetRodAngle(Mathf.Lerp(rodAngle, 58f, Time.deltaTime * 4f));
            }

            UpdateLine();
        }

        private IEnumerator CatchAnimation(Color fishColor)
        {
            ending = true;
            Splash(bobber.position, 1.4f, 8);

            // Balık sudan sıçrayıp kıvrılarak oyuncuya gelir
            var fish = new GameObject("Yakalanan_Balik").transform;
            fish.SetParent(transform, false);
            Ambient.Decor(PrimitiveType.Capsule, "Govde", fish, Vector3.zero, new Vector3(0.18f, 0.24f, 0.12f), fishColor, Quaternion.Euler(0f, 0f, 90f));
            Ambient.Decor(PrimitiveType.Cube, "Kuyruk", fish, new Vector3(-0.3f, 0f, 0f), new Vector3(0.12f, 0.18f, 0.03f), fishColor, Quaternion.Euler(0f, 0f, 45f));

            Vector3 from = bobber.position;
            Vector3 to = player.position + Vector3.up * 1.1f + player.forward * 0.4f;
            for (float t = 0f; t < 0.75f; t += Time.deltaTime)
            {
                float k = t / 0.75f;
                Vector3 p = Vector3.Lerp(from, to, k) + Vector3.up * (4f * k * (1f - k) * 2.4f);
                fish.position = p;
                fish.rotation = Quaternion.LookRotation((to - from).normalized) * Quaternion.Euler(0f, 90f, Mathf.Sin(t * 30f) * 35f);
                bobber.position = Vector3.Lerp(from, rodTip.position, k);
                SetRodAngle(Mathf.Lerp(78f, 15f, EaseOut(Mathf.Clamp01(k * 2f))));
                slack = 0.05f;
                UpdateLine();
                yield return null;
            }

            yield return new WaitForSeconds(0.35f);
            Destroy(fish.gameObject);
            Cleanup();
        }

        private IEnumerator ReelBack()
        {
            ending = true;
            Vector3 from = bobber.position;
            for (float t = 0f; t < 0.45f; t += Time.deltaTime)
            {
                float k = t / 0.45f;
                bobber.position = Vector3.Lerp(from, rodTip.position, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 0.6f);
                SetRodAngle(Mathf.Lerp(rodAngle, 20f, k));
                slack = Mathf.Lerp(slack, 0.1f, k);
                UpdateLine();
                yield return null;
            }
            Cleanup();
        }

        // ------------------------------------------------------------------ Yardımcılar

        private float rodAngle;

        /// <summary>0° kamış dik yukarı, 90° tam öne (yatay).</summary>
        private void SetRodAngle(float degrees)
        {
            rodAngle = degrees;
            if (rod == null) return;
            // Kamış, kol pivotunun altında: karakterin önüne doğru (yerel +Z) eğilir.
            Quaternion parentInverse = Quaternion.Inverse(rod.parent.rotation);
            Quaternion world = player.rotation * Quaternion.Euler(degrees, 0f, 0f);
            rod.localRotation = parentInverse * world;
        }

        private void UpdateLine()
        {
            if (rodTip == null || !line.enabled) return;
            Vector3 a = rodTip.position;
            Vector3 b = bobber.position + Vector3.up * 0.2f;
            float sag = Vector3.Distance(a, b) * 0.18f * slack;
            for (int i = 0; i < LinePoints; i++)
            {
                float k = i / (float)(LinePoints - 1);
                line.SetPosition(i, Vector3.Lerp(a, b, k) + Vector3.down * (sag * 4f * k * (1f - k)));
            }
        }

        private void Splash(Vector3 at, float size, int drops)
        {
            StartCoroutine(Ring(at, size));
            for (int i = 0; i < drops; i++) StartCoroutine(Drop(at));
        }

        private IEnumerator Ring(Vector3 at, float size)
        {
            Transform ring = Ambient.Decor(PrimitiveType.Cylinder, "Su_Halkasi", transform, Vector3.zero, Vector3.zero,
                Ambient.Hdr(new Color(0.85f, 0.95f, 1f), 1.3f)).transform;
            Transform hole = Ambient.Decor(PrimitiveType.Cylinder, "Su_Halkasi_Ic", ring, new Vector3(0f, 0.4f, 0f), new Vector3(0.82f, 1f, 0.82f), Palette.Water).transform;
            ring.position = new Vector3(at.x, waterPoint.y + 0.02f, at.z);
            for (float t = 0f; t < 0.7f; t += Time.deltaTime)
            {
                float k = t / 0.7f;
                float s = Mathf.Lerp(0.15f, size, EaseOut(k));
                ring.localScale = new Vector3(s, 0.004f * (1f - k) + 0.001f, s);
                yield return null;
            }
            Destroy(ring.gameObject);
            _ = hole;
        }

        private IEnumerator Drop(Vector3 at)
        {
            Transform drop = Ambient.Decor(PrimitiveType.Sphere, "Damla", transform, Vector3.zero, Vector3.one * 0.06f,
                Ambient.Hdr(new Color(0.8f, 0.92f, 1f), 1.6f)).transform;
            Vector3 velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(2f, 3.5f), Random.Range(-1f, 1f));
            drop.position = at;
            for (float t = 0f; t < 0.6f && drop.position.y >= waterPoint.y - 0.05f; t += Time.deltaTime)
            {
                velocity += Vector3.down * 9.8f * Time.deltaTime;
                drop.position += velocity * Time.deltaTime;
                yield return null;
            }
            Destroy(drop.gameObject);
        }

        private void Cleanup()
        {
            if (rod != null) Destroy(rod.gameObject);
            rod = null;
            rodTip = null;
            if (bobber != null) bobber.gameObject.SetActive(false);
            if (line != null) line.enabled = false;
            casting = landed = reeling = ending = false;
        }

        private static float EaseOut(float k) => 1f - (1f - k) * (1f - k);
    }
}
