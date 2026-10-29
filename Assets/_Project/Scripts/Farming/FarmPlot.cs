using Gumusdere.Interaction;
using Gumusdere.Items;
using Gumusdere.World;
using UnityEngine;

namespace Gumusdere.Farming
{
    /// <summary>
    /// Tek bir tarla karesi. Döngü: Boş → (E) tohum ek → (E) sula → büyür → (E) hasat → Boş.
    /// Hasatta çantaya bir balkabağı girer.
    /// </summary>
    public sealed class FarmPlot : MonoBehaviour, IPlayerInteractable
    {
        public enum PlotState { Empty, Planted, Growing, Ripe }

        private const float GrowDuration = 15f;

        private Renderer soil;
        private Transform sprout;
        private GameObject pumpkin;
        private float growStartTime;

        public PlotState State { get; private set; } = PlotState.Empty;
        public Vector3 Position => transform.position;
        public float UseRadius => 1.5f;

        /// <summary>Büyüme ilerlemesi (0..1).</summary>
        public float GrowProgress => State == PlotState.Growing
            ? Mathf.Clamp01((Time.time - growStartTime) / GrowDuration)
            : State == PlotState.Ripe ? 1f : 0f;

        public void Initialize(Renderer soilRenderer, Transform visualParent)
        {
            soil = soilRenderer;
            Vector3 p = transform.position;

            sprout = Shapes.Cylinder("Filiz", visualParent, p + new Vector3(0f, 0.15f, 0f),
                new Vector3(0.12f, 0.1f, 0.12f), Palette.LeafLight, false).transform;
            pumpkin = Shapes.Sphere("Balkabagi", visualParent, p + new Vector3(0f, 0.38f, 0f),
                new Vector3(0.75f, 0.6f, 0.75f), Palette.Pumpkin, false);
            Shapes.Cylinder("Balkabagi_Sap", pumpkin.transform, new Vector3(0f, 0.55f, 0f),
                new Vector3(0.12f, 0.25f, 0.12f), Palette.Leaf, false);

            sprout.gameObject.SetActive(false);
            pumpkin.SetActive(false);
        }

        public string GetPrompt(PlayerContext context) => State switch
        {
            PlotState.Empty => "Tohum ek",
            PlotState.Planted => "Sula",
            PlotState.Growing => $"Büyüyor... %{Mathf.RoundToInt(GrowProgress * 100f)}",
            PlotState.Ripe => "Balkabağını hasat et",
            _ => null
        };

        public void Use(PlayerContext context)
        {
            switch (State)
            {
                case PlotState.Empty:
                    State = PlotState.Planted;
                    sprout.gameObject.SetActive(true);
                    sprout.localScale = new Vector3(0.12f, 0.1f, 0.12f);
                    context.Info("Tohum ekildi. Şimdi sula.");
                    break;

                case PlotState.Planted:
                    State = PlotState.Growing;
                    growStartTime = Time.time;
                    soil.sharedMaterial = MaterialLibrary.Get(Palette.SoilWet);
                    context.Info($"Sulandı. Balkabağı yaklaşık {GrowDuration:0} saniyede büyür.");
                    break;

                case PlotState.Ripe:
                    State = PlotState.Empty;
                    pumpkin.SetActive(false);
                    context.Inventory.Add(ItemType.Pumpkin);
                    context.Info($"+1 {ItemInfo.DisplayName(ItemType.Pumpkin)}");
                    break;
            }
        }

        private void Update()
        {
            if (State != PlotState.Growing) return;

            float k = GrowProgress;
            sprout.localScale = Vector3.Lerp(new Vector3(0.12f, 0.1f, 0.12f), new Vector3(0.3f, 0.35f, 0.3f), k);
            sprout.position = transform.position + new Vector3(0f, 0.1f + sprout.localScale.y, 0f);

            if (k < 1f) return;

            State = PlotState.Ripe;
            sprout.gameObject.SetActive(false);
            pumpkin.SetActive(true);
            soil.sharedMaterial = MaterialLibrary.Get(Palette.Soil);
        }
    }
}
