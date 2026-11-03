namespace Gumusdere.Interaction
{
    /// <summary>
    /// Birden fazla duraktan oluşan görev (örn. kuyudan kovayı al → ekili kareleri tek tek sula).
    /// IInteractable'ın InteractionPoint / LookPoint değerleri her zaman ŞU ANKİ durağı gösterir.
    /// Cemal bir durağı bitirince (Interact) görev bir sonraki durağa geçer; durak kalmayınca görev biter.
    /// </summary>
    public interface IMultiStepTask : IInteractable
    {
        /// <summary>Şu anki duraktan sonra gidilecek başka durak var mı.</summary>
        bool HasNextStep { get; }

        /// <summary>Bir sonraki durağa geç.</summary>
        void AdvanceStep();

        /// <summary>
        /// Görev verilince çağrılır: durakları hazırlar (örn. o an ekili olan kareleri sıralar)
        /// ve işçinin söyleyeceği ilk cümleyi seçer.
        /// </summary>
        void Begin(UnityEngine.GameObject worker);

        /// <summary>Görev bittiğinde ya da yarıda kaldığında temizlik (örn. kovayı bırak).</summary>
        void End(UnityEngine.GameObject worker);
    }
}
