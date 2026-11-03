using System;
using Gumusdere.Interaction;
using UnityEngine;

namespace Gumusdere.Village
{
    /// <summary>Köy meydanındaki ilan panosu. E ile köylülerin isteklerini gösteren pencere açılır.</summary>
    public sealed class QuestBoard : MonoBehaviour, IPlayerInteractable
    {
        private Vector3 readSpot;

        public Vector3 Position => readSpot;
        public float UseRadius => 2.4f;

        public event Action Requested;

        public void Initialize(Vector3 readerStandPoint)
        {
            readSpot = readerStandPoint;
        }

        public string GetPrompt(PlayerContext context) => "İlan panosu (köylülerin istekleri)";

        public void Use(PlayerContext context) => Requested?.Invoke();
    }
}
