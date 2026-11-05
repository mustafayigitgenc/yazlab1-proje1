using Gumusdere.Core;
using Gumusdere.Interaction;
using UnityEngine;

namespace Gumusdere.Village
{
    /// <summary>
    /// Çoban Rıfat Amca: ahırın kapısında durur. İnekleri kaçmıştır; Cemal inekleri ahıra toplarsa
    /// karşılığında madenin destek kirişlerini verir. Konuşması sabit cümlelerdir (LLM sadece Hüsnü Dayı'da).
    /// </summary>
    public sealed class FarmerNpc : MonoBehaviour, IPlayerInteractable
    {
        private HerdCowsJob herd;
        private ISpeaker speaker;
        private Vector3 talkSpot;
        private bool rewarded;

        public Vector3 Position => talkSpot;
        public float UseRadius => 2.6f;

        public void Initialize(HerdCowsJob cowsJob, ISpeaker voice, Vector3 talkPoint)
        {
            herd = cowsJob;
            speaker = voice;
            talkSpot = talkPoint;
            herd.CowPenned += count => speaker?.Say(count < herd.Total ? $"Aferin Cemal! {count}/{herd.Total} oldu." : "Hepsi geldi!");
        }

        /// <summary>Bütün inekler ahırda: kirişleri ver.</summary>
        public void GiveReward()
        {
            rewarded = true;
            speaker?.Say("Allah razı olsun ağa! Söz verdiğim destek kirişleri ahırın önünde; Cemal madenin ağzına taşısın.");
        }

        public string GetPrompt(PlayerContext context) => "Rıfat Amca ile konuş";

        public void Use(PlayerContext context)
        {
            if (rewarded)
            {
                speaker?.Say("Kirişler ahırın önünde ağa, Cemal'e F ile taşıt. Madende dikkatli olun!");
            }
            else if (herd.Penned == 0)
            {
                speaker?.Say($"Ağa, köprü kalkınca ineklerim karşıya kaçtı, {herd.Total} tanesi ortalıkta geziyor! " +
                             "Cemal'e söyle de ahıra toplasın; karşılığında madenin destek kirişlerini veririm.");
            }
            else
            {
                speaker?.Say($"{herd.Penned}/{herd.Total} inek ahırda, gerisini de Cemal getirsin ağa.");
            }
        }
    }
}
