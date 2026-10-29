namespace Gumusdere.Npc.Actions
{
    /// <summary>
    /// NPC'nin kararını oyun dünyasına uygulayan eylem (Strategy deseni).
    /// Model bir eylem önerir; eylemin gerçekten yapılıp yapılmayacağına oyun kuralı (CanExecute) karar verir.
    /// Yeni bir eylem eklemek için mevcut kodu değiştirmeden yeni bir sınıf yazılır (Open/Closed).
    /// </summary>
    public interface IGameAction
    {
        string Name { get; }

        /// <summary>Oyun kuralı izin veriyor mu? İzin vermiyorsa nedenini döndürür.</summary>
        bool CanExecute(int trust, out string reason);

        void Execute();
    }
}
