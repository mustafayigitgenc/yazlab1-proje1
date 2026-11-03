using System;
using System.Collections.Generic;
using Gumusdere.Core;
using Gumusdere.Economy;
using Gumusdere.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gumusdere.UI
{
    /// <summary>
    /// Nazmi'nin bakkalı penceresi: solda çantadaki ürünleri satma, sağda baret ve fener alma.
    /// Pencere açıkken oyuncu yürüyemez; Esc veya E ile kapanır. Kurallar Shop sınıfındadır, burası sadece çizer.
    /// </summary>
    public sealed class ShopView : MonoBehaviour
    {
        private const float PanelWidth = 720f;
        private const float PanelHeight = 470f;

        private Shop shop;
        private Action<bool> setControls;
        private ISpeaker shopkeeper;
        private IMessageLog log;
        private string lastMessage;
        private int openedFrame = -1;

        private GUIStyle titleStyle;
        private GUIStyle textStyle;
        private GUIStyle smallStyle;
        private GUIStyle buttonStyle;
        private GUIStyle panelStyle;

        public bool IsOpen { get; private set; }

        public void Initialize(Shop targetShop, Action<bool> controlsSetter, ISpeaker shopkeeperSpeaker, IMessageLog messageLog)
        {
            shop = targetShop;
            setControls = controlsSetter;
            shopkeeper = shopkeeperSpeaker;
            log = messageLog;
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            openedFrame = Time.frameCount;
            lastMessage = null;
            setControls?.Invoke(false);
            shopkeeper?.Say("Hoş geldin ağa! Ne satıyorsun, ne lazım?");
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            setControls?.Invoke(true);
            shopkeeper?.Say("Yine beklerim ağa!");
        }

        // LateUpdate: GameFlow aynı karede Esc'yi okurken pencere hâlâ açık görünür, oyun duraklamaz.
        private void LateUpdate()
        {
            if (!IsOpen || Time.frameCount == openedFrame) return; // açan E tuşu pencereyi hemen kapatmasın
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                InputGate.ConsumeEscape();
                Close();
            }
            else if (kb.eKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        private void OnGUI()
        {
            if (!IsOpen || shop == null) return;
            EnsureStyles();

            float scale = Mathf.Max(1f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            float h = Screen.height / scale;

            var panel = new Rect((w - PanelWidth) * 0.5f, (h - PanelHeight) * 0.5f, PanelWidth, PanelHeight);
            GUI.Box(panel, GUIContent.none, panelStyle);
            GUILayout.BeginArea(new Rect(panel.x + 20f, panel.y + 14f, panel.width - 40f, panel.height - 28f));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Nazmi'nin Bakkalı", titleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Paran: <b>{shop.Wallet.Money} lira</b>", textStyle);
            GUILayout.EndHorizontal();
            GUILayout.Space(10f);

            GUILayout.BeginHorizontal();
            DrawSellColumn();
            GUILayout.Space(24f);
            DrawBuyColumn();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            if (!string.IsNullOrEmpty(lastMessage)) GUILayout.Label(lastMessage, textStyle);
            GUILayout.Label("Esc / E: kapat", smallStyle);
            GUILayout.EndArea();
        }

        private void DrawSellColumn()
        {
            GUILayout.BeginVertical(GUILayout.Width(380f));
            GUILayout.Label("<b>Sat</b>", textStyle);

            bool any = false;
            foreach (KeyValuePair<ItemType, int> pair in shop.Inventory.Snapshot())
            {
                int price = PriceList.SellPrice(pair.Key);
                if (price <= 0) continue;
                any = true;

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{ItemInfo.DisplayName(pair.Key)} x{pair.Value}  <color=#d8c37a>({price} lira)</color>", textStyle, GUILayout.Width(220f));
                if (GUILayout.Button("1 sat", buttonStyle, GUILayout.Width(64f))) Sell(pair.Key, 1);
                if (pair.Value > 1 && GUILayout.Button("Hepsi", buttonStyle, GUILayout.Width(64f))) Sell(pair.Key, pair.Value);
                GUILayout.EndHorizontal();
            }

            if (!any) GUILayout.Label("<i>Satacak bir şeyin yok. Hasat et, balık tut ya da mantar topla.</i>", smallStyle);
            GUILayout.EndVertical();
        }

        private void DrawBuyColumn()
        {
            GUILayout.BeginVertical(GUILayout.Width(270f));
            GUILayout.Label("<b>Al</b>", textStyle);

            foreach (ItemType item in PriceList.ForSale)
            {
                int price = PriceList.BuyPrice(item);
                bool owned = shop.Inventory.Has(item);

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{ItemInfo.DisplayName(item)}  <color=#d8c37a>({price} lira)</color>", textStyle, GUILayout.Width(150f));
                GUI.enabled = !owned && shop.Wallet.Money >= price;
                if (GUILayout.Button(owned ? "Var" : "Al", buttonStyle, GUILayout.Width(80f))) Buy(item);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8f);
            GUILayout.Label("Hüsnü Dayı, baret ve fener olmadan kimseyi madene sokmaz.", smallStyle);
            GUILayout.EndVertical();
        }

        private void Sell(ItemType type, int count)
        {
            bool ok = shop.TrySell(type, count, out lastMessage);
            if (ok) log?.Add("", lastMessage, Color.white);
        }

        private void Buy(ItemType type)
        {
            if (!shop.TryBuy(type, out lastMessage)) return;
            log?.Add("", lastMessage, Color.white);
            shopkeeper?.Say(type == ItemType.Helmet ? "Sağlam baret, başına taş düşse bir şey olmaz!" : "Bu fener madenin en dibini aydınlatır.");
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, richText = true, normal = { textColor = Color.white } };
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true, wordWrap = true, normal = { textColor = Color.white } };
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true, wordWrap = true, normal = { textColor = new Color(0.85f, 0.85f, 0.85f) } };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 15 };
            panelStyle = new GUIStyle(GUI.skin.box);
            var bg = new Texture2D(1, 1) { hideFlags = HideFlags.DontSave };
            bg.SetPixel(0, 0, new Color(0.10f, 0.08f, 0.06f, 0.94f));
            bg.Apply();
            panelStyle.normal.background = bg;
        }
    }
}
