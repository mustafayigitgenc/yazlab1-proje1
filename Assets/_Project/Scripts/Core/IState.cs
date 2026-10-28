namespace Gumusdere.Core
{
    /// <summary>
    /// Durum makinesindeki tek bir durum (State deseni).
    /// Her durum kendi davranışından sorumludur (Single Responsibility);
    /// yeni bir davranış eklemek için mevcut kodu değiştirmek yerine
    /// yeni bir IState sınıfı yazılır (Open/Closed).
    /// </summary>
    public interface IState
    {
        /// <summary>Ekranda ve loglarda görünen Türkçe durum adı.</summary>
        string Name { get; }

        /// <summary>Duruma girildiğinde bir kez çağrılır.</summary>
        void Enter();

        /// <summary>Durum aktifken her karede çağrılır.</summary>
        void Tick(float deltaTime);

        /// <summary>Durumdan çıkılırken bir kez çağrılır.</summary>
        void Exit();
    }
}
