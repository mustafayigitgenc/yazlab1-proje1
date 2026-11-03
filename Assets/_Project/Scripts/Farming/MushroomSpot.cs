using Gumusdere.Interaction;
using Gumusdere.Items;
using Gumusdere.World;
using UnityEngine;

namespace Gumusdere.Farming
{
    /// <summary>
    /// Ormandaki mantar yeri. Her çıkışta rastgele bir tür belirir: turuncu Kanlıca mantarı ya da
    /// kırmızı benekli Sinek mantarı (zehirli). Toplanınca bir süre sonra yeniden çıkar.
    /// </summary>
    public sealed class MushroomSpot : MonoBehaviour, IPlayerInteractable
    {
        private const float MinRegrow = 45f;
        private const float MaxRegrow = 80f;
        private const float FlyAgaricChance = 0.4f;

        private GameObject chanterelle;
        private GameObject flyAgaric;
        private ItemType current;
        private bool ready;
        private float readyTime;

        public Vector3 Position => transform.position;
        public float UseRadius => 1.4f;

        public void Initialize()
        {
            chanterelle = BuildMushroom("Kanlica", ItemInfo.Tint(ItemType.Chanterelle), false);
            flyAgaric = BuildMushroom("Sinek_Mantari", ItemInfo.Tint(ItemType.FlyAgaric), true);
            Spawn();
        }

        public string GetPrompt(PlayerContext context) => ready ? $"{ItemInfo.DisplayName(current)} topla" : null;

        public void Use(PlayerContext context)
        {
            if (!ready) return;

            ready = false;
            chanterelle.SetActive(false);
            flyAgaric.SetActive(false);
            readyTime = Time.time + Random.Range(MinRegrow, MaxRegrow);

            context.Inventory.Add(current);
            context.Info(current == ItemType.FlyAgaric
                ? $"+1 {ItemInfo.DisplayName(current)} (kırmızı benekli... yenmez gibi duruyor)"
                : $"+1 {ItemInfo.DisplayName(current)}");
        }

        private void Update()
        {
            if (!ready && Time.time >= readyTime) Spawn();
        }

        private void Spawn()
        {
            current = Random.value < FlyAgaricChance ? ItemType.FlyAgaric : ItemType.Chanterelle;
            chanterelle.SetActive(current == ItemType.Chanterelle);
            flyAgaric.SetActive(current == ItemType.FlyAgaric);
            ready = true;
        }

        private GameObject BuildMushroom(string name, Color cap, bool dotted)
        {
            var root = new GameObject(name);
            root.transform.SetParent(transform, false);
            Transform t = root.transform;

            Shapes.Cylinder("Sap", t, new Vector3(0f, 0.12f, 0f), new Vector3(0.1f, 0.12f, 0.1f), Palette.Wall, false);
            Shapes.Sphere("Sapka", t, new Vector3(0f, 0.27f, 0f), new Vector3(0.38f, 0.16f, 0.38f), cap, false);
            Shapes.Cylinder("Sap", t, new Vector3(0.22f, 0.09f, 0.12f), new Vector3(0.07f, 0.09f, 0.07f), Palette.Wall, false);
            Shapes.Sphere("Sapka", t, new Vector3(0.22f, 0.2f, 0.12f), new Vector3(0.24f, 0.11f, 0.24f), cap, false);

            if (dotted)
            {
                Shapes.Sphere("Benek", t, new Vector3(0.08f, 0.34f, -0.05f), Vector3.one * 0.05f, Palette.Wall, false);
                Shapes.Sphere("Benek", t, new Vector3(-0.09f, 0.33f, 0.06f), Vector3.one * 0.05f, Palette.Wall, false);
                Shapes.Sphere("Benek", t, new Vector3(0.02f, 0.35f, 0.1f), Vector3.one * 0.04f, Palette.Wall, false);
            }

            return root;
        }
    }
}
