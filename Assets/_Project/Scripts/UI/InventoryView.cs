using System.Collections.Generic;
using Gumusdere.Items;
using UnityEngine;

namespace Gumusdere.UI
{
    /// <summary>Sağ altta çantadaki eşyalar.</summary>
    public sealed class InventoryView : MonoBehaviour
    {
        private const float Width = 220f;

        private IInventory inventory;
        private GUIStyle textStyle;
        private Texture2D white;

        public void Initialize(IInventory playerInventory)
        {
            inventory = playerInventory;
        }

        private void OnGUI()
        {
            if (inventory == null) return;
            EnsureStyles();

            float scale = Mathf.Max(1f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            float h = Screen.height / scale;

            IReadOnlyDictionary<ItemType, int> items = inventory.Snapshot();
            int rows = Mathf.Max(1, items.Count);
            float height = 40f + rows * 26f;
            var panel = new Rect(w - Width - 12f, h - height - 12f, Width, height);

            Fill(panel, new Color(0f, 0f, 0f, 0.55f));
            GUI.Label(new Rect(panel.x + 12f, panel.y + 6f, Width - 24f, 26f), "<b>Çanta</b>", textStyle);

            float y = panel.y + 34f;
            if (items.Count == 0)
            {
                GUI.Label(new Rect(panel.x + 12f, y, Width - 24f, 24f), "<i>boş</i>", textStyle);
                return;
            }

            foreach (KeyValuePair<ItemType, int> pair in items)
            {
                Fill(new Rect(panel.x + 12f, y + 6f, 12f, 12f), ItemInfo.Tint(pair.Key));
                GUI.Label(new Rect(panel.x + 32f, y, Width - 44f, 24f), $"{ItemInfo.DisplayName(pair.Key)}  x{pair.Value}", textStyle);
                y += 26f;
            }
        }

        private void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, white);
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (textStyle != null) return;
            white = Texture2D.whiteTexture;
            textStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 15 };
            textStyle.normal.textColor = Color.white;
        }
    }
}
