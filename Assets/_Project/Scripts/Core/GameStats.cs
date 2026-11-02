using System.Collections.Generic;
using Gumusdere.Items;
using UnityEngine;

namespace Gumusdere.Core
{
    /// <summary>Bitiş ekranında gösterilen oyun istatistikleri.</summary>
    public sealed class GameStats
    {
        private readonly Dictionary<ItemType, int> lastCounts = new();
        private float startTime;
        private float endTime;
        private bool running;

        public int CompanionTasksDone { get; set; }
        public int CompanionTasksFailed { get; set; }
        public int ItemsCollected { get; private set; }
        public int GiftsGiven { get; private set; }
        public int NpcReplies { get; set; }

        public float ElapsedSeconds => running ? Time.time - startTime : endTime - startTime;

        public void StartClock()
        {
            startTime = Time.time;
            running = true;
        }

        public void StopClock()
        {
            if (!running) return;
            endTime = Time.time;
            running = false;
        }

        /// <summary>Çantadaki artışları "toplandı", azalışları "hediye edildi" olarak sayar.</summary>
        public void Track(IInventory inventory)
        {
            inventory.Changed += (type, newCount) =>
            {
                int old = lastCounts.TryGetValue(type, out int n) ? n : 0;
                int delta = newCount - old;
                lastCounts[type] = newCount;

                if (delta > 0) ItemsCollected += delta;
                else if (delta < 0) GiftsGiven += -delta;
            };
        }
    }
}
