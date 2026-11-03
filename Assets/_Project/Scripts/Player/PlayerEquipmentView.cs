using Gumusdere.Items;
using UnityEngine;

namespace Gumusdere.Player
{
    /// <summary>
    /// Çantadaki ekipmanı oyuncunun üstünde gösterir: baret alınınca hasır şapkanın yerine sarı baret,
    /// fener alınınca elde yanan bir fener (madenin karanlığında etrafı aydınlatır).
    /// </summary>
    public sealed class PlayerEquipmentView : MonoBehaviour
    {
        public const string HelmetName = "Ekipman_Baret";
        public const string LanternName = "Ekipman_Fener";
        public const string HatName = "Sapka";

        private IInventory inventory;
        private GameObject helmet;
        private GameObject lantern;
        private GameObject hat;

        public void Initialize(IInventory playerInventory)
        {
            inventory = playerInventory;
            helmet = transform.Find(HelmetName)?.gameObject;
            lantern = transform.Find(LanternName)?.gameObject;
            hat = transform.Find(HatName)?.gameObject;
            inventory.Changed += (type, count) => Refresh();
            Refresh();
        }

        private void Refresh()
        {
            bool hasHelmet = inventory.Has(ItemType.Helmet);
            if (helmet != null) helmet.SetActive(hasHelmet);
            if (hat != null) hat.SetActive(!hasHelmet);
            if (lantern != null) lantern.SetActive(inventory.Has(ItemType.Lantern));
        }
    }
}
