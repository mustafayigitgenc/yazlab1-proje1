using Gumusdere.Core;
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
        public const string PickaxeName = "Ekipman_Kazma";
        public const string LanternName = "Ekipman_Fener";
        public const string HatName = "Sapka";

        private IInventory inventory;
        private GameObject helmet;
        private GameObject lantern;
        private GameObject hat;
        private GameObject pickaxe;

        public void Initialize(IInventory playerInventory)
        {
            inventory = playerInventory;
            helmet = transform.FindDeep(HelmetName)?.gameObject;
            lantern = transform.FindDeep(LanternName)?.gameObject;
            hat = transform.FindDeep(HatName)?.gameObject;
            pickaxe = transform.FindDeep(PickaxeName)?.gameObject;
            inventory.Changed += (type, count) => Refresh();
            Refresh();
        }

        private void Refresh()
        {
            bool hasHelmet = inventory.Has(ItemType.Helmet);
            if (helmet != null) helmet.SetActive(hasHelmet);
            if (hat != null) hat.SetActive(!hasHelmet);
            if (lantern != null) lantern.SetActive(inventory.Has(ItemType.Lantern));
            if (pickaxe != null) pickaxe.SetActive(inventory.Has(ItemType.Pickaxe));
        }
    }
}
