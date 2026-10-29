using Gumusdere.Interaction;

namespace Gumusdere.Companion
{
    /// <summary>
    /// Oyuncunun emir verebildiği karakter. Oyuncu tarafı Cemal'in iç yapısını bilmez,
    /// sadece bu arayüzü kullanır (Dependency Inversion).
    /// </summary>
    public interface ICommandable
    {
        bool IsBusy { get; }

        /// <summary>Görevi kabul ederse true döner.</summary>
        bool TryAssignTask(IInteractable target);

        /// <summary>Görevi bırakıp oyuncuya döner.</summary>
        void Recall();

        /// <summary>Boştayken: bekle / takip et.</summary>
        void ToggleWait();
    }
}
