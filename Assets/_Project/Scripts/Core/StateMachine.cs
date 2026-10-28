using System;

namespace Gumusdere.Core
{
    /// <summary>
    /// Genel amaçlı sonlu durum makinesi (FSM).
    /// Hem Cemal'in karar mantığı hem de ileride oyunun genel akışı bunu kullanır.
    /// </summary>
    public sealed class StateMachine
    {
        /// <summary>Şu anki aktif durum.</summary>
        public IState Current { get; private set; }

        /// <summary>Durum değiştiğinde (önceki, yeni) bilgisiyle tetiklenir.</summary>
        public event Action<IState, IState> StateChanged;

        public void ChangeState(IState next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            if (ReferenceEquals(next, Current)) return;

            IState previous = Current;
            previous?.Exit();
            Current = next;
            Current.Enter();
            StateChanged?.Invoke(previous, Current);
        }

        public void Tick(float deltaTime) => Current?.Tick(deltaTime);
    }
}
