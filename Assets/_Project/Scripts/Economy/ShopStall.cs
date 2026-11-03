using System;
using Gumusdere.Interaction;
using UnityEngine;

namespace Gumusdere.Economy
{
    /// <summary>Köydeki bakkal tezgâhı. Oyuncu yanına gelip E'ye basınca alışveriş penceresi açılır.</summary>
    public sealed class ShopStall : MonoBehaviour, IPlayerInteractable
    {
        private Vector3 customerSpot;

        public Vector3 Position => customerSpot;
        public float UseRadius => 2.4f;

        /// <summary>Oyuncu alışveriş yapmak istedi.</summary>
        public event Action Requested;

        public void Initialize(Vector3 customerStandPoint)
        {
            customerSpot = customerStandPoint;
        }

        public string GetPrompt(PlayerContext context) => "Nazmi'nin bakkalı (al / sat)";

        public void Use(PlayerContext context) => Requested?.Invoke();
    }
}
