using System;
using System.Collections;
using Gumusdere.Core;
using Gumusdere.Interaction;
using Gumusdere.Items;
using Gumusdere.World;
using UnityEngine;

namespace Gumusdere.Mining
{
    /// <summary>
    /// Madenin en dibindeki gümüş damarı. Oyuncu bakkaldan aldığı kazmayla (E) 3 kez vurur; damar açılınca oyun kazanılır.
    /// Kazma yoksa vurulamaz: oyuncuya kazmanın bakkalda olduğu söylenir.
    /// </summary>
    public sealed class SilverVein : MonoBehaviour, IPlayerInteractable
    {
        private const int HitsNeeded = 3;
        private const float HitCooldown = 0.5f;

        private Transform shell;
        private GameObject treasure;
        private int hits;
        private float nextHitTime;

        public bool IsMined => hits >= HitsNeeded;
        public Vector3 Position => transform.position;
        public float UseRadius => 2.4f;

        /// <summary>Damar tamamen açıldığında.</summary>
        public event Action Mined;

        /// <summary>Her kazma vuruşunda (ses ve efekt için).</summary>
        public event Action Hit;

        public void Initialize(Transform rockShell, GameObject silverInside)
        {
            shell = rockShell;
            treasure = silverInside;
            treasure.SetActive(false);
        }

        public string GetPrompt(PlayerContext context)
        {
            if (IsMined) return null;
            if (!HasPickaxe(context)) return "Kazma lazım (Nazmi'nin bakkalı)";
            return $"Kazmayla vur ({hits}/{HitsNeeded})";
        }

        public void Use(PlayerContext context)
        {
            if (IsMined || Time.time < nextHitTime) return;
            nextHitTime = Time.time + HitCooldown;
            if (!HasPickaxe(context))
            {
                context.Info($"Kaya çok sert, elle olmaz. Nazmi'nin bakkalından kazma al ({Economy.PriceList.BuyPrice(ItemType.Pickaxe)} lira).");
                return;
            }
            StartCoroutine(Swing(context.Player.transform));
            hits++;
            Hit?.Invoke();
            StartCoroutine(Shake());

            if (hits < HitsNeeded)
            {
                context.Info(hits == 1 ? "Kaya çatladı..." : "Bir vuruş daha!");
                return;
            }

            shell.gameObject.SetActive(false);
            treasure.SetActive(true);
            context.Info("Gümüş damarı ortaya çıktı!");
            Mined?.Invoke();
        }

        private static bool HasPickaxe(PlayerContext context) => context?.Inventory != null && context.Inventory.Has(ItemType.Pickaxe);

        /// <summary>Sırttaki kazma bir anlığına gizlenir; oyuncunun önünde kalkıp kayaya inen bir kazma çizilir.</summary>
        private IEnumerator Swing(Transform player)
        {
            Vector3 toVein = transform.position - player.position;
            toVein.y = 0f;
            if (toVein.sqrMagnitude > 0.01f) player.rotation = Quaternion.LookRotation(toVein);

            Transform back = player.FindDeep(Player.PlayerEquipmentView.PickaxeName);
            if (back != null) back.gameObject.SetActive(false);

            var pivot = new GameObject("Kazma_Vurus").transform;
            pivot.position = player.position + player.up * 1.25f + player.forward * 0.35f + player.right * 0.15f;
            Shapes.Cylinder("Kazma_Sap", pivot, new Vector3(0f, 0.45f, 0f), new Vector3(0.06f, 0.45f, 0.06f), Palette.Wood, false);
            Shapes.Box("Kazma_Bas", pivot, new Vector3(0f, 0.9f, 0.05f), new Vector3(0.09f, 0.09f, 0.62f), new Color(0.55f, 0.57f, 0.6f), false);

            const float duration = 0.32f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                // Önce geriye kalkar (-30°), sonra hızla öne iner (110°)
                float angle = k < 0.45f ? Mathf.Lerp(0f, -30f, k / 0.45f) : Mathf.Lerp(-30f, 110f, (k - 0.45f) / 0.55f);
                pivot.rotation = player.rotation * Quaternion.Euler(angle, 0f, 0f);
                yield return null;
            }
            yield return new WaitForSeconds(0.12f);
            Destroy(pivot.gameObject);
            if (back != null) back.gameObject.SetActive(true);
        }

        private IEnumerator Shake()
        {
            if (shell == null) yield break;
            Vector3 origin = shell.localPosition;
            float t = 0f;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                shell.localPosition = origin + UnityEngine.Random.insideUnitSphere * 0.06f;
                yield return null;
            }
            shell.localPosition = origin;
        }
    }
}
