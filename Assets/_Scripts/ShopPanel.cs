using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using DeadCells.Data;
using DeadCells.Player;

namespace DeadCells.Core
{
    /// <summary>
    /// 商店面板 —— 挂在 Canvas 下的空物体上（如 Canvas/ShopPanel）。
    /// 打开时根据 ShopData 动态生成商品行（数据驱动），购买走 PlayerWallet + 事件解耦。
    /// </summary>
    public class ShopPanel : MonoBehaviour
    {
        public static ShopPanel Instance { get; private set; }

        public bool IsOpen { get; private set; }

        private ShopData currentShop;
        private PlayerWallet wallet;          // 钱（缓存，打开时刷新）
        private PlayerCombat playerCombat;    // 武器入库
        private PlayerInventory playerInventory; // 道具入库
        private readonly List<Button> buyButtons = new();  // 用于钱不够时禁用

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            gameObject.SetActive(false);   // 默认关闭
        }

        public void Open(ShopData shop)
        {
            if (shop == null) return;

            currentShop = shop;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                wallet = player.GetComponent<PlayerWallet>();
                playerCombat = player.GetComponent<PlayerCombat>();
                playerInventory = player.GetComponent<PlayerInventory>();
            }

            gameObject.SetActive(true);
            IsOpen = true;
            Refresh();
        }

        public void Close()
        {
            IsOpen = false;
            gameObject.SetActive(false);
        }

        /// <summary>按配置重建商品列表（数据驱动：加商品只改 ShopData 资产）</summary>
        private void Refresh()
        {
            // 清掉上一批动态生成的 UI
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
            buyButtons.Clear();

            if (currentShop == null) return;

            float y = 0f;
            UIFactory.CreateText(transform, "Title", currentShop.shopName, 28)
                .rectTransform.anchoredPosition = new Vector2(0f, -20f);

            foreach (ShopItemData item in currentShop.items)
            {
                if (item == null) continue;
                y += 55f;

                // 商品行：名字 xN —— 价格 —— 购买按钮
                string label = $"{item.itemName}";
                if (item.itemType == ShopItemType.Consumable)
                    label += $" x{(playerInventory != null ? playerInventory.GetCount(item) : 0)}";
                TextMeshProUGUI nameText = UIFactory.CreateText(transform, "ItemName", label, 20);
                nameText.rectTransform.anchoredPosition = new Vector2(-150f, -y);

                TextMeshProUGUI priceText = UIFactory.CreateText(transform, "Price", $"{item.price} G", 18);
                priceText.rectTransform.anchoredPosition = new Vector2(60f, -y);
                priceText.color = wallet != null && wallet.Coins >= item.price
                    ? Color.white : new Color(1f, 0.4f, 0.4f);

                ShopItemData captured = item;
                Button buyBtn = UIFactory.CreateButton(transform, "BuyBtn", "Buy", () => TryBuy(captured));
                buyBtn.transform.SetSiblingIndex(transform.childCount - 1);
                RectTransform btnRt = buyBtn.GetComponent<RectTransform>();
                btnRt.anchoredPosition = new Vector2(180f, -y);
                btnRt.sizeDelta = new Vector2(90f, 36f);

                // 钱不够置灰
                if (wallet == null || wallet.Coins < item.price)
                    buyBtn.interactable = false;
                buyButtons.Add(buyBtn);
            }
        }

        private void TryBuy(ShopItemData item)
        {
            if (wallet == null) return;

            if (!wallet.TrySpendCoins(item.price))
            {
                Debug.Log("[Shop] 金币不足");
                return;
            }

            // 发奖：武器进武器栏（Q 切换），消耗品进背包
            if (item.itemType == ShopItemType.Weapon)
            {
                if (item.weapon != null && playerCombat != null)
                    playerCombat.AddWeapon(item.weapon);
            }
            else if (playerInventory != null)
            {
                playerInventory.AddItem(item, 1);
            }

            Refresh();   // 刷新价格颜色 / 数量显示 / 按钮置灰
        }
    }
}
