using System.Collections.Generic;
using Gumusdere.Core;
using Gumusdere.Economy;
using Gumusdere.Items;
using UnityEngine;

namespace Gumusdere.UI
{
    /// <summary>
    /// Nazmi'nin bakkalı penceresi: solda çantadaki ürünleri satma, sağda ekipman (can yeleği, baret, fener, kazma) alma.
    /// Kurallar Shop sınıfındadır, burası sadece çizer.
    /// </summary>
    public sealed class ShopView : ModalWindow
    {
        private Shop shop;
        private ISpeaker shopkeeper;
        private IMessageLog log;
        private string lastMessage;

        protected override Vector2 PanelSize => new(720f, 500f);

        public void Initialize(Shop targetShop, System.Action<bool> controlsSetter, ISpeaker shopkeeperSpeaker, IMessageLog messageLog)
        {
            shop = targetShop;
            shopkeeper = shopkeeperSpeaker;
            log = messageLog;
            SetControlsCallback(controlsSetter);
        }

        protected override void OnOpened()
        {
            lastMessage = null;
            shopkeeper?.Say("Hoş geldin ağa! Ne satıyorsun, ne lazım?");
        }

        protected override void OnClosed() => shopkeeper?.Say("Yine beklerim ağa!");

        protected override void DrawContent()
        {
            if (shop == null) return;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Nazmi'nin Bakkalı", TitleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Paran: <b>{shop.Wallet.Money} lira</b>", TextStyle);
            GUILayout.EndHorizontal();
            GUILayout.Space(10f);

            GUILayout.BeginHorizontal();
            DrawSellColumn();
            GUILayout.Space(24f);
            DrawBuyColumn();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            if (!string.IsNullOrEmpty(lastMessage)) GUILayout.Label(lastMessage, TextStyle);
        }

        private void DrawSellColumn()
        {
            GUILayout.BeginVertical(GUILayout.Width(380f));
            GUILayout.Label("<b>Sat</b>", TextStyle);

            bool any = false;
            foreach (KeyValuePair<ItemType, int> pair in shop.Inventory.Snapshot())
            {
                int price = PriceList.SellPrice(pair.Key);
                if (price <= 0) continue;
                any = true;

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{ItemInfo.DisplayName(pair.Key)} x{pair.Value}  <color=#d8c37a>({price} lira)</color>", TextStyle, GUILayout.Width(220f));
                if (GUILayout.Button("1 sat", ButtonStyle, GUILayout.Width(64f))) Sell(pair.Key, 1);
                if (pair.Value > 1 && GUILayout.Button("Hepsi", ButtonStyle, GUILayout.Width(64f))) Sell(pair.Key, pair.Value);
                GUILayout.EndHorizontal();
            }

            if (!any) GUILayout.Label("<i>Satacak bir şeyin yok. Hasat et, balık tut ya da mantar topla.</i>", SmallStyle);
            GUILayout.EndVertical();
        }

        private void DrawBuyColumn()
        {
            GUILayout.BeginVertical(GUILayout.Width(270f));
            GUILayout.Label("<b>Al</b>", TextStyle);

            foreach (ItemType item in PriceList.ForSale)
            {
                int price = PriceList.BuyPrice(item);
                bool owned = shop.Inventory.Has(item);

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{ItemInfo.DisplayName(item)}  <color=#d8c37a>({price} lira)</color>", TextStyle, GUILayout.Width(150f));
                GUI.enabled = !owned && shop.Wallet.Money >= price;
                if (GUILayout.Button(owned ? "Var" : "Al", ButtonStyle, GUILayout.Width(80f))) Buy(item);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8f);
            GUILayout.Label("Can yeleği Cemal için: onsuz sığ geçide girmez.", SmallStyle);
            GUILayout.Label("Hüsnü Dayı, baret ve fener olmadan kimseyi madene sokmaz.", SmallStyle);
            GUILayout.Label("Gümüş damarı ancak kazmayla açılır.", SmallStyle);
            GUILayout.Label("Daha çok para için köy meydanındaki ilan panosuna bak.", SmallStyle);
            GUILayout.EndVertical();
        }

        private void Sell(ItemType type, int count)
        {
            if (shop.TrySell(type, count, out lastMessage)) log?.Add("", lastMessage, Color.white);
        }

        private void Buy(ItemType type)
        {
            if (!shop.TryBuy(type, out lastMessage)) return;
            log?.Add("", lastMessage, Color.white);
            shopkeeper?.Say(type switch
            {
                ItemType.Helmet => "Sağlam baret, başına taş düşse bir şey olmaz!",
                ItemType.Lantern => "Bu fener madenin en dibini aydınlatır.",
                ItemType.LifeVest => "Cemal'e yakışır! Artık akıntıdan korkmaz.",
                ItemType.Pickaxe => "Dükkânın en iyisi! Deden de böyle bir kazmayla çalışırdı.",
                _ => "Güle güle kullan ağa!"
            });
        }
    }
}
