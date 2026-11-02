using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Gumusdere.Core
{
    /// <summary>
    /// Oyunun genel akışı, bir durum makinesiyle:
    ///   Başlangıç → Oynanıyor ⇄ Duraklatıldı,   Oynanıyor → Kazanıldı → (Yeniden başla)
    /// Ekranları GameScreensView çizer; bu sınıf sadece durumu ve geçişleri yönetir.
    /// </summary>
    public sealed class GameFlow : MonoBehaviour
    {
        private Action<bool> setControls;
        private Func<bool> canPause;

        public StateMachine Machine { get; } = new();
        public GameStats Stats { get; private set; }

        public TitleState Title { get; private set; }
        public PlayingState Playing { get; private set; }
        public PausedState Paused { get; private set; }
        public VictoryState Victory { get; private set; }

        public bool IsPlaying => Machine.Current == Playing;

        public event Action Won;

        public void Initialize(GameStats stats, Action<bool> controlsSetter, Func<bool> pauseAllowed)
        {
            Stats = stats;
            setControls = controlsSetter;
            canPause = pauseAllowed ?? (() => true);

            Title = new TitleState(this);
            Playing = new PlayingState(this);
            Paused = new PausedState(this);
            Victory = new VictoryState(this);

            Machine.StateChanged += (previous, next) =>
                Debug.Log($"[Oyun] {(previous == null ? "başlangıç" : previous.Name)} → {next.Name}");
            Machine.ChangeState(Title);
        }

        public void SetControls(bool enabled) => setControls?.Invoke(enabled);

        public void StartGame()
        {
            if (Machine.Current != Title) return;
            Stats.StartClock();
            Machine.ChangeState(Playing);
        }

        public void Pause()
        {
            if (Machine.Current == Playing && canPause()) Machine.ChangeState(Paused);
        }

        public void Resume()
        {
            if (Machine.Current == Paused) Machine.ChangeState(Playing);
        }

        public void Win()
        {
            if (Machine.Current == Victory) return;
            Machine.ChangeState(Victory);
            Won?.Invoke();
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void Quit()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (Machine.Current == Title)
            {
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) StartGame();
            }
            else if (kb.escapeKey.wasPressedThisFrame && !InputGate.IsEscapeConsumed)
            {
                if (Machine.Current == Playing) Pause();
                else if (Machine.Current == Paused) Resume();
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
