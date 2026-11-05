using System;
using System.Collections.Generic;
using Gumusdere.Core;
using Gumusdere.Interaction;
using UnityEngine;
using UnityEngine.AI;

namespace Gumusdere.Village
{
    /// <summary>
    /// Cemal'in çok duraklı görevi: Rıfat Amca'nın kaçan ineklerini tek tek bulup ahıra götürmek.
    /// Her inek için iki durak vardır: ineğin yanı (inek durur, sonra Cemal'in peşine takılır) ve ahırın kapısı
    /// (inek içeri girer). Bütün inekler ahıra girince AllPenned olayı tetiklenir; Rıfat Amca karşılığında destek kirişlerini verir.
    /// </summary>
    public sealed class HerdCowsJob : MonoBehaviour, IMultiStepTask
    {
        private const float RequestRadiusAtBarn = 18f;
        private const float RequestRadiusAtCow = 9f;

        private readonly List<AnimalWanderer> cows = new();
        private readonly List<AnimalWanderer> route = new(); // her inek iki kez: ineğin yanı, sonra ahır
        private readonly List<bool> atBarn = new();
        private Vector3 gateStand;
        private Rect pen;
        private Vector3 cowStand;
        private int stepIndex;
        private bool running;

        public int Penned { get; private set; }
        public int Total => cows.Count;
        public bool AllDone => Total > 0 && Penned >= Total;

        public event Action<int> CowPenned;
        public event Action AllPenned;

        public string DisplayName => "İnek toplama";
        public string CommandText => $"Cemal'e inekleri ahıra topla ({Penned}/{Total})";

        private bool AtBarnStep => route.Count > 0 && atBarn[stepIndex];
        private AnimalWanderer CurrentCow => route.Count > 0 ? route[stepIndex] : null;

        public Vector3 InteractionPoint => AtBarnStep || CurrentCow == null ? gateStand : cowStand;
        public Transform Root => AtBarnStep || CurrentCow == null ? transform : CurrentCow.transform;
        public Vector3 LookPoint => AtBarnStep || CurrentCow == null ? transform.position + Vector3.up * 0.8f : CurrentCow.transform.position + Vector3.up * 0.9f;
        public bool IsAvailable => running || !AllDone;
        public bool HasNextStep => stepIndex + 1 < route.Count;

        public void Initialize(IEnumerable<AnimalWanderer> looseCows, Vector3 gateStandPoint, Rect barnPen)
        {
            cows.AddRange(looseCows);
            gateStand = gateStandPoint;
            pen = barnPen;
        }

        public bool CanBeRequestedFrom(Vector3 playerPosition)
        {
            if (Flat(playerPosition - gateStand) <= RequestRadiusAtBarn) return true;
            foreach (AnimalWanderer cow in cows)
            {
                if (!cow.IsPenned && Flat(playerPosition - cow.transform.position) <= RequestRadiusAtCow) return true;
            }
            return false;
        }

        public void Begin(GameObject worker)
        {
            running = true;
            stepIndex = 0;
            route.Clear();
            atBarn.Clear();

            // En yakın inekten başla; sonraki inek ahıra en yakın olan (ahırdan çıkıp sıradakine gider).
            var loose = new List<AnimalWanderer>();
            foreach (AnimalWanderer cow in cows) if (!cow.IsPenned) loose.Add(cow);
            Vector3 from = worker.transform.position;
            while (loose.Count > 0)
            {
                int best = 0;
                for (int i = 1; i < loose.Count; i++)
                {
                    if ((loose[i].transform.position - from).sqrMagnitude < (loose[best].transform.position - from).sqrMagnitude) best = i;
                }
                route.Add(loose[best]); atBarn.Add(false);
                route.Add(loose[best]); atBarn.Add(true);
                loose.RemoveAt(best);
                from = gateStand;
            }

            PrepareCowStep();
            worker.GetComponent<ISpeaker>()?.Say($"Tamam ağam, Rıfat Amca'nın {route.Count / 2} ineğini ahıra toplayacağım!");
        }

        public void Interact(GameObject worker)
        {
            ISpeaker speaker = worker.GetComponent<ISpeaker>();
            AnimalWanderer cow = CurrentCow;
            if (cow == null) return;

            if (!AtBarnStep)
            {
                cow.Follow(worker.transform);
                speaker?.Say("Hadi bakalım sarıkız, düş peşime, ahıra gidiyoruz!");
                return;
            }

            cow.Pen(pen);
            Penned++;
            CowPenned?.Invoke(Penned);
            if (AllDone)
            {
                speaker?.Say("Hepsi ahırda ağam! Rıfat Amca kirişleri verecek.");
                AllPenned?.Invoke();
            }
            else
            {
                speaker?.Say($"Bu da ahırda ({Penned}/{Total}), sıradakini bulayım.");
            }
        }

        public void AdvanceStep()
        {
            stepIndex = Mathf.Min(stepIndex + 1, route.Count - 1);
            PrepareCowStep();
        }

        public void End(GameObject worker)
        {
            running = false;
            foreach (AnimalWanderer cow in cows) cow.Release(); // ahırdakiler etkilenmez
            route.Clear();
            atBarn.Clear();
            stepIndex = 0;
        }

        /// <summary>Sıradaki durak bir inekse: inek beklesin, Cemal ineğin ahır tarafındaki yanına gitsin.</summary>
        private void PrepareCowStep()
        {
            if (route.Count == 0 || AtBarnStep) return;
            AnimalWanderer cow = CurrentCow;
            cow.Hold();
            Vector3 toGate = gateStand - cow.transform.position;
            toGate.y = 0f;
            Vector3 side = cow.transform.position + (toGate.sqrMagnitude > 0.01f ? toGate.normalized : Vector3.forward) * 1.9f;
            cowStand = NavMesh.SamplePosition(side, out NavMeshHit hit, 2f, NavMesh.AllAreas) ? hit.position : side;
        }

        private static float Flat(Vector3 v)
        {
            v.y = 0f;
            return v.magnitude;
        }
    }
}
