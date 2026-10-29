using System.Collections.Generic;
using Gumusdere.Interaction;
using Gumusdere.Items;
using UnityEngine;

namespace Gumusdere.Farming
{
    /// <summary>
    /// Meyve çalısı: E ile toplanır (her seferinde 3 böğürtlen), sonra bir süre sonra yeniden meyve verir.
    /// </summary>
    public sealed class BerryBush : MonoBehaviour, IPlayerInteractable
    {
        private const int BerriesPerHarvest = 3;
        private const float RegrowDuration = 40f;

        private readonly List<GameObject> berries = new();
        private float readyTime;

        public bool IsReady => Time.time >= readyTime;
        public Vector3 Position => transform.position;
        public float UseRadius => 1.8f;

        public void Initialize(IEnumerable<GameObject> berryVisuals)
        {
            berries.AddRange(berryVisuals);
            readyTime = 0f;
        }

        public string GetPrompt(PlayerContext context) => IsReady ? "Böğürtlen topla" : null;

        public void Use(PlayerContext context)
        {
            if (!IsReady) return;

            readyTime = Time.time + RegrowDuration;
            SetBerriesVisible(false);
            context.Inventory.Add(ItemType.Berry, BerriesPerHarvest);
            context.Info($"+{BerriesPerHarvest} {ItemInfo.DisplayName(ItemType.Berry)}");
        }

        private void Update()
        {
            if (IsReady && berries.Count > 0 && !berries[0].activeSelf) SetBerriesVisible(true);
        }

        private void SetBerriesVisible(bool visible)
        {
            foreach (GameObject berry in berries) berry.SetActive(visible);
        }
    }
}
