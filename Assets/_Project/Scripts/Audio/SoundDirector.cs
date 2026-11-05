using System.Collections.Generic;
using Gumusdere.Ambience;
using Gumusdere.Core;
using Gumusdere.Economy;
using Gumusdere.Fishing;
using Gumusdere.Items;
using Gumusdere.Npc.Dialogue;
using Gumusdere.Village;
using Gumusdere.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gumusdere.Audio
{
    /// <summary>
    /// Oyundaki olayları dinleyip uygun sesi çalar (Observer): eşya toplama, para, kapı, köprü, kazma, göçük,
    /// balık, Hüsnü Dayı'nın cevabı, başarı. Ayrıca arka planda rüzgâr ve ara sıra kuş sesi.
    /// Sesler SoundBank'ta kodla üretilir. N tuşu sesi açıp kapatır.
    /// </summary>
    public sealed class SoundDirector : MonoBehaviour
    {
        private const float EffectsVolume = 0.7f;
        private const float WindVolume = 0.12f;

        private const int Voices = 6;

        private readonly Dictionary<ItemType, int> lastCounts = new();
        private SoundBank bank;
        private AudioSource[] voices;
        private int nextVoice;
        private AudioSource ambience;
        private FishingMinigame fishing;
        private FishingMinigame.Phase lastFishingPhase;
        private float nextBird;
        private int footstepIndex;

        public bool Muted { get; private set; }

        public void Initialize(GameFlow flow, IInventory inventory, IWallet wallet, WorldLayout layout, FishingMinigame fishingGame,
            NpcDialogueController dialogue, QuestBook quests, WalkBob playerWalk)
        {
            bank = new SoundBank();
            fishing = fishingGame;

            // Aynı anda birkaç ses çalabilsin ve her biri kendi perdesinde (pitch) olsun diye küçük bir kaynak havuzu.
            voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0f;
                voices[i].volume = EffectsVolume;
            }

            ambience = gameObject.AddComponent<AudioSource>();
            ambience.clip = bank.Wind;
            ambience.loop = true;
            ambience.volume = WindVolume;
            ambience.spatialBlend = 0f;
            ambience.Play();
            nextBird = Time.time + 3f;

            foreach (KeyValuePair<ItemType, int> pair in inventory.Snapshot()) lastCounts[pair.Key] = pair.Value;
            inventory.Changed += (type, count) =>
            {
                int before = lastCounts.TryGetValue(type, out int c) ? c : 0;
                lastCounts[type] = count;
                if (count > before && !ItemInfo.IsTool(type)) Play(bank.Pop, 0.8f); // eşya toplandı
            };
            wallet.Changed += (before, after) => Play(bank.Coin, after > before ? 0.9f : 0.6f);
            quests.Completed += _ => Play(bank.Jingle, 0.8f);
            layout.Door.Opened += () => Play(bank.Creak, 1f);
            layout.Door.Closed += () => Play(bank.Creak, 0.8f, 0.9f);
            layout.Bridge.Lowered += () => Play(bank.Thud, 1f);
            layout.Rubble.Cleared += () => Play(bank.Crumble, 1f);
            layout.Vein.Hit += () => Play(bank.Tink, 0.9f, Random.Range(0.95f, 1.08f));
            layout.Beams.BeamDelivered += _ => Play(bank.Thud, 0.45f, 1.6f);
            dialogue.DecisionReceived += (decision, ms) => Play(bank.Blip, 0.7f);
            flow.Won += () => Play(bank.Jingle, 1f, 0.9f);
            if (playerWalk != null) playerWalk.Stepped += OnStep;
        }

        private void OnStep()
        {
            footstepIndex = (footstepIndex + 1) % bank.Footsteps.Length;
            Play(bank.Footsteps[footstepIndex], 0.16f, Random.Range(0.85f, 1.15f));
        }

        private void Update()
        {
            if (bank == null) return;

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.nKey.wasPressedThisFrame && Hud.Visible)
            {
                Muted = !Muted;
                AudioListener.volume = Muted ? 0f : 1f;
            }

            // Balık tutma: oltayı atınca su sesi, balık vurunca "blup", kaçarsa düşen ton.
            if (fishing != null && fishing.Current != lastFishingPhase)
            {
                if (fishing.Current == FishingMinigame.Phase.Waiting) Play(bank.Splash, 0.7f);
                else if (fishing.Current == FishingMinigame.Phase.Reeling) Play(bank.Bite, 1f);
                else if (lastFishingPhase == FishingMinigame.Phase.Reeling) Play(bank.Splash, 0.5f, 1.3f);
                lastFishingPhase = fishing.Current;
            }

            if (Time.time >= nextBird)
            {
                nextBird = Time.time + Random.Range(6f, 14f);
                AudioClip bird = bank.Birds[Random.Range(0, bank.Birds.Length)];
                Play(bird, Random.Range(0.25f, 0.45f), Random.Range(0.9f, 1.15f));
            }
        }

        private void Play(AudioClip clip, float volume, float pitch = 1f)
        {
            if (clip == null || voices == null) return;
            AudioSource source = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            source.pitch = pitch;
            source.PlayOneShot(clip, volume);
        }
    }
}
