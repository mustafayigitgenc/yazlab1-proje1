using UnityEngine;

namespace Gumusdere.Interaction
{
    /// <summary>
    /// Bir karakterin gidip kullanabileceği nesne (vinç, şalter, sandık...).
    /// Cemal hangi nesneyle uğraştığını bilmez; sadece bu arayüz üzerinden konuşur
    /// (Liskov / Interface Segregation: her yeni görev nesnesi bu küçük sözleşmeyi uygular).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Ekranda görünen ad (örn. "Köprü vinci").</summary>
        string DisplayName { get; }

        /// <summary>Oyuncuya gösterilecek emir metni (örn. "Cemal'e köprüyü indirt").</summary>
        string CommandText { get; }

        /// <summary>Kullanmak için durulması gereken nokta.</summary>
        Vector3 InteractionPoint { get; }

        /// <summary>Sensörün "hedefi görüyor muyum?" kontrolü için nesnenin kökü ve bakılacak nokta.</summary>
        Transform Root { get; }
        Vector3 LookPoint { get; }

        /// <summary>Şu an kullanılabilir mi (örn. köprü zaten indiyse hayır).</summary>
        bool IsAvailable { get; }

        /// <summary>Oyuncu bu konumdayken bu görevi emredebilir mi.</summary>
        bool CanBeRequestedFrom(Vector3 playerPosition);

        void Interact(GameObject actor);
    }
}
