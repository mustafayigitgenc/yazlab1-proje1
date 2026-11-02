using UnityEngine;

namespace Gumusdere.Core
{
    /// <summary>Başlangıç ekranı: dünya arka planda yaşar, oyuncu hareket edemez.</summary>
    public sealed class TitleState : IState
    {
        private readonly GameFlow flow;
        public TitleState(GameFlow flow) => this.flow = flow;

        public string Name => "Başlangıç";
        public void Enter() { Time.timeScale = 1f; flow.SetControls(false); }
        public void Tick(float deltaTime) { }
        public void Exit() { }
    }

    /// <summary>Normal oyun.</summary>
    public sealed class PlayingState : IState
    {
        private readonly GameFlow flow;
        public PlayingState(GameFlow flow) => this.flow = flow;

        public string Name => "Oynanıyor";
        public void Enter() { Time.timeScale = 1f; flow.SetControls(true); }
        public void Tick(float deltaTime) { }
        public void Exit() { }
    }

    /// <summary>Duraklatma menüsü: zaman durur, girdiler kapanır.</summary>
    public sealed class PausedState : IState
    {
        private readonly GameFlow flow;
        public PausedState(GameFlow flow) => this.flow = flow;

        public string Name => "Duraklatıldı";
        public void Enter() { Time.timeScale = 0f; flow.SetControls(false); }
        public void Tick(float deltaTime) { }
        public void Exit() { Time.timeScale = 1f; }
    }

    /// <summary>Gümüş damarı bulundu: bitiş ekranı.</summary>
    public sealed class VictoryState : IState
    {
        private readonly GameFlow flow;
        public VictoryState(GameFlow flow) => this.flow = flow;

        public string Name => "Kazanıldı";
        public void Enter() { Time.timeScale = 1f; flow.SetControls(false); flow.Stats.StopClock(); }
        public void Tick(float deltaTime) { }
        public void Exit() { }
    }
}
