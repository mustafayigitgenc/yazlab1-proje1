using System.Collections.Generic;
using Gumusdere.Core;
using Gumusdere.Interaction;
using UnityEngine;

namespace Gumusdere.Farming
{
    /// <summary>
    /// Cemal'in çok adımlı görevi: kuyuya gidip sulama kovasını alır, sonra ekili ama sulanmamış
    /// kareleri en yakından başlayarak (en yakın komşu sırası) tek tek sular.
    /// Duraklar görev başlarken o anki tarla durumuna göre hesaplanır; yani rota her seferinde farklıdır.
    /// </summary>
    public sealed class WateringJob : MonoBehaviour, IMultiStepTask
    {
        public const string CanObjectName = "Sulama_Kovasi";

        private const float RequestRadius = 15f;

        private readonly List<FarmPlot> plots = new();
        private readonly List<FarmPlot> route = new();
        private Vector3 wellStandPoint;
        private Vector3 fieldCenter;
        private int stepIndex = -1; // -1: kuyu, 0..n-1: tarla kareleri
        private bool running;
        private int watered;

        public string DisplayName => "Tarla sulama";
        public string CommandText => "Cemal'e tarlayı sulat";

        // Cemal karenin üstüne basmaz; güney kenarında (iki sıranın arasındaki yolda) durup sular.
        public Vector3 InteractionPoint => stepIndex < 0 ? wellStandPoint : route[stepIndex].transform.position + new Vector3(0f, 0f, -1f);
        public Transform Root => stepIndex < 0 ? transform : route[stepIndex].transform;
        public Vector3 LookPoint => stepIndex < 0 ? transform.position + Vector3.up * 0.6f : route[stepIndex].transform.position + Vector3.up * 0.3f;

        public bool IsAvailable => running || CountThirsty() > 0;
        public bool HasNextStep => NextThirstyIndex() >= 0;

        public void Initialize(IEnumerable<FarmPlot> farmPlots, Vector3 wellStand, Vector3 center)
        {
            plots.AddRange(farmPlots);
            wellStandPoint = wellStand;
            fieldCenter = center;
        }

        public bool CanBeRequestedFrom(Vector3 playerPosition)
        {
            Vector3 d = playerPosition - fieldCenter;
            d.y = 0f;
            return d.magnitude <= RequestRadius;
        }

        public void Begin(GameObject worker)
        {
            running = true;
            watered = 0;
            stepIndex = -1;
            BuildRoute(wellStandPoint);
            worker.GetComponent<ISpeaker>()?.Say("Sulama kovasını alıp geliyorum ağam, kuyuya gidiyorum!");
        }

        /// <summary>Kuyudan başlayarak her seferinde en yakın susuz kareyi seç (en yakın komşu sıralaması).</summary>
        private void BuildRoute(Vector3 start)
        {
            route.Clear();
            var remaining = new List<FarmPlot>();
            foreach (FarmPlot p in plots) if (p.NeedsWater) remaining.Add(p);

            Vector3 from = start;
            while (remaining.Count > 0)
            {
                int best = 0;
                float bestDistance = float.MaxValue;
                for (int i = 0; i < remaining.Count; i++)
                {
                    float d = (remaining[i].transform.position - from).sqrMagnitude;
                    if (d < bestDistance) { bestDistance = d; best = i; }
                }
                from = remaining[best].transform.position;
                route.Add(remaining[best]);
                remaining.RemoveAt(best);
            }
        }

        public void Interact(GameObject worker)
        {
            ISpeaker speaker = worker.GetComponent<ISpeaker>();

            if (stepIndex < 0)
            {
                SetCan(worker, true);
                int left = 0;
                foreach (FarmPlot p in route) if (p.NeedsWater) left++;
                speaker?.Say(left > 0 ? $"Kovayı doldurdum, {left} kare sulayacağım." : "Sulanacak kare kalmamış ağam.");
                return;
            }

            if (route[stepIndex].Water())
            {
                watered++;
                speaker?.Say(HasNextStep
                    ? $"Sulandı ({watered}/{route.Count}), sıradakine geçiyorum."
                    : $"Hepsi sulandı ağam ({watered} kare), kovayı kuyuya bırakırım!");
            }
        }

        public void AdvanceStep()
        {
            int next = NextThirstyIndex();
            if (next >= 0) stepIndex = next;
        }

        /// <summary>Sıradaki hâlâ susuz kare. Bu arada oyuncu bir kareyi kendisi suladıysa o durak atlanır.</summary>
        private int NextThirstyIndex()
        {
            for (int i = stepIndex + 1; i < route.Count; i++)
            {
                if (route[i].NeedsWater) return i;
            }
            return -1;
        }

        public void End(GameObject worker)
        {
            running = false;
            stepIndex = -1;
            SetCan(worker, false);
        }

        private int CountThirsty()
        {
            int n = 0;
            foreach (FarmPlot p in plots) if (p.NeedsWater) n++;
            return n;
        }

        private static void SetCan(GameObject worker, bool visible)
        {
            Transform can = worker.transform.Find(CanObjectName);
            if (can != null) can.gameObject.SetActive(visible);
        }
    }
}
