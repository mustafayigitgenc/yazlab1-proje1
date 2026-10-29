using Gumusdere.Core;
using Gumusdere.Items;
using UnityEngine;

namespace Gumusdere.Interaction
{
    /// <summary>
    /// Oyuncunun E tuşuyla kullandığı şeylere verilen bilgiler (çanta, mesaj kutusu, hareket kilidi).
    /// </summary>
    public sealed class PlayerContext
    {
        public PlayerContext(GameObject player, IInventory inventory, IMessageLog log, System.Action<bool> setMovementEnabled)
        {
            Player = player;
            Inventory = inventory;
            Log = log;
            SetMovementEnabled = setMovementEnabled;
        }

        public GameObject Player { get; }
        public IInventory Inventory { get; }
        public IMessageLog Log { get; }
        public System.Action<bool> SetMovementEnabled { get; }

        public void Info(string text) => Log?.Add("", text, Color.white);
    }

    /// <summary>
    /// Oyuncunun yanına gidip E ile kullanabildiği nesne (tarla, çalı, iskele, NPC...).
    /// Cemal'in görev nesneleri (IInteractable) bundan ayrıdır; her arayüz tek bir işe hizmet eder.
    /// </summary>
    public interface IPlayerInteractable
    {
        /// <summary>Oyuncunun yaklaşması gereken nokta.</summary>
        Vector3 Position { get; }

        /// <summary>Ne kadar yakından kullanılabileceği (metre).</summary>
        float UseRadius { get; }

        /// <summary>Ekranda gösterilecek eylem (örn. "Tohum ek"). Kullanılamıyorsa null.</summary>
        string GetPrompt(PlayerContext context);

        void Use(PlayerContext context);
    }
}
