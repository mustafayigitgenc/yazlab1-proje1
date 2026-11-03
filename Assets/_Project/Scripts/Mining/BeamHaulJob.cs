using System;
using System.Collections.Generic;
using Gumusdere.Core;
using Gumusdere.Interaction;
using UnityEngine;

namespace Gumusdere.Mining
{
    /// <summary>
    /// Cemal'in çok duraklı taşıma görevi: ormandaki tomruk yığınından bir kiriş omuzlar, madenin ağzına götürür,
    /// eksik kiriş kalmayana kadar gidip gelir (her kiriş 2 durak: yığın → maden). Köprü kalkıksa yol sığ geçitten geçer.
    /// Hüsnü Dayı 3 kiriş yerine konmadan maden kapısını açmaz (MineEntryRule).
    /// </summary>
    public sealed class BeamHaulJob : MonoBehaviour, IMultiStepTask
    {
        public const string CarriedBeamName = "Tasinan_Kiris";
        private const float RequestRadius = 14f;

        private readonly List<bool> route = new(); // true: yığından al, false: madene bırak
        private readonly List<GameObject> placedVisuals = new();
        private Transform pile;
        private Vector3 pileStand;
        private Vector3 stockStand;
        private Vector3 stockLook;
        private int stepIndex;
        private bool running;

        public int Delivered { get; private set; }
        public bool IsComplete => Delivered >= MineEntryRule.RequiredBeams;

        /// <summary>Bir kiriş madenin ağzına bırakıldı (toplam).</summary>
        public event Action<int> BeamDelivered;

        public string DisplayName => "Kiriş taşıma";
        public string CommandText => $"Cemal'e destek kirişlerini taşıt ({Delivered}/{MineEntryRule.RequiredBeams})";

        private bool AtPile => route.Count > 0 && route[stepIndex];
        public Vector3 InteractionPoint => AtPile ? pileStand : stockStand;
        public Transform Root => AtPile ? pile : transform;
        public Vector3 LookPoint => AtPile ? pile.position + Vector3.up * 0.5f : stockLook;
        public bool IsAvailable => running || !IsComplete;
        public bool HasNextStep => stepIndex + 1 < route.Count;

        /// <param name="woodpile">Tomruk yığını (çarpışmalı; sensör onu görmeli).</param>
        /// <param name="placedBeams">Madenin ağzında kiriş bırakıldıkça görünecek kiriş görselleri.</param>
        public void Initialize(Transform woodpile, Vector3 woodpileStand, Vector3 stockpileStand, IEnumerable<GameObject> placedBeams)
        {
            pile = woodpile;
            pileStand = woodpileStand;
            stockStand = stockpileStand;
            stockLook = transform.position + Vector3.up * 0.3f;
            placedVisuals.AddRange(placedBeams);
            foreach (GameObject beam in placedVisuals) beam.SetActive(false);
        }

        public bool CanBeRequestedFrom(Vector3 playerPosition) =>
            Flat(playerPosition - stockStand) <= RequestRadius || Flat(playerPosition - pileStand) <= RequestRadius;

        public void Begin(GameObject worker)
        {
            running = true;
            stepIndex = 0;
            route.Clear();
            for (int i = Delivered; i < MineEntryRule.RequiredBeams; i++)
            {
                route.Add(true);
                route.Add(false);
            }
            worker.GetComponent<ISpeaker>()?.Say("Tomruk yığınına gidiyorum ağam, kirişleri madenin ağzına taşıyacağım!");
        }

        public void Interact(GameObject worker)
        {
            ISpeaker speaker = worker.GetComponent<ISpeaker>();
            if (AtPile)
            {
                SetCarried(worker, true);
                speaker?.Say($"Kirişi omuzladım, madene götürüyorum ({Delivered + 1}/{MineEntryRule.RequiredBeams}).");
                return;
            }

            SetCarried(worker, false);
            if (Delivered < placedVisuals.Count) placedVisuals[Delivered].SetActive(true);
            Delivered++;
            BeamDelivered?.Invoke(Delivered);
            speaker?.Say(IsComplete
                ? "Üç kiriş de madenin ağzında ağam! Hüsnü Dayı'ya söyleyebilirsin."
                : $"Kiriş yerinde ({Delivered}/{MineEntryRule.RequiredBeams}), bir sonrakine gidiyorum.");
        }

        public void AdvanceStep() => stepIndex = Mathf.Min(stepIndex + 1, route.Count - 1);

        public void End(GameObject worker)
        {
            running = false;
            stepIndex = 0;
            route.Clear();
            SetCarried(worker, false);
        }

        private static void SetCarried(GameObject worker, bool visible)
        {
            Transform beam = worker.transform.Find(CarriedBeamName);
            if (beam != null) beam.gameObject.SetActive(visible);
        }

        private static float Flat(Vector3 v)
        {
            v.y = 0f;
            return v.magnitude;
        }
    }
}
