using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using DeadCells.Data;
using DeadCells.Core;
using DeadCells.Player;
using System;
namespace DeadCells.Shop
{
    public class ShopPanel:MonoBehaviour
    {
        private ShopData currentShop;
        private PlayerCombat playerCombat;
        private PlayerInventory inventory;
        private PlayerInput playerInput;
        private readonly List<Button> buyButtons = new();
        public static ShopPanel Instance{get;private set;}
        private PlayerWallet wallet;
        public bool IsOpen = false;
        [SerializeField]
        private ShopData testShop;
        private Transform content;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // 创建内容容器
            var go = new GameObject("Content",typeof(RectTransform),typeof(Image));
            go.transform.SetParent(transform,false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image img = go.GetComponent<Image>();
            img.color = new Color(0.1f,0.1f,0.15f,0.95f);
            content = go.transform;
            content.gameObject.SetActive(false);
        }

        public void Open(ShopData shop)
        {
            //获取playerWallet和playerInventory
            if(playerInput == null)
            {
                playerInput = FindObjectOfType<PlayerInput>();
                if(playerInput == null)
                {
                    Debug.LogError("PlayerInput not found in the scene.");
                    return;
                }
            }
            if(wallet == null)
            {
                wallet = FindObjectOfType<PlayerWallet>();
                if(wallet == null)
                {
                    Debug.LogError("PlayerWallet not found in the scene.");
                    return;
                }
            }
            if(inventory == null)
            {
                inventory = FindObjectOfType<PlayerInventory>();
                if(inventory == null)
                {
                    Debug.LogError("PlayerInventory not found in the scene.");
                    return;
                }
            }
            if(shop == null) return;
            playerInput.InPutLocked = true;
            currentShop = shop;
            content.gameObject.SetActive(true);
            IsOpen = true;
            Refresh();
        }
        public void Close()
        {
            IsOpen = false;
            content.gameObject.SetActive(false);
            playerInput.InPutLocked = false;
        }
        private void Refresh()
        {
            // 清空现有内容
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Destroy(content.GetChild(i).gameObject);
            }
            buyButtons.Clear();
            if(currentShop == null) return;
            // 创建标题
            float y = 40f;
            TextMeshProUGUI Title = UIFactory.CreateText(content,"Title",currentShop.shopName,22);
            RectTransform titleRT = Title.rectTransform;
            titleRT.anchorMin = new Vector2(0.5f,1f);
            titleRT.anchorMax = new Vector2(0.5f,1f);
            titleRT.pivot = new Vector2(0.5f,1f);
            titleRT.sizeDelta = new Vector2(200f,40f);
            titleRT.anchoredPosition = new Vector2(0f,-20f);
            foreach(var item in currentShop.items)
            {
                if(item ==null)continue;
                CreateRow(item,y);
                y += 55f;
            }
        }
        private void CreateRow(ShopItemData item,float y)
        {
            GameObject row = new GameObject("Row",typeof(RectTransform));
            row.transform.SetParent(content,false);
            RectTransform rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f,1f);
            rt.anchorMax = new Vector2(1f,1f);
            rt.pivot = new Vector2(0.5f,1f);
            rt.sizeDelta = new Vector2(0f,55f);
            rt.anchoredPosition = new Vector2(0f,-y);
            // 左侧文本
            TextMeshProUGUI left = UIFactory.CreateText(row.transform,"Left",item.itemName,20);
            RectTransform leftRT = left.rectTransform;
            leftRT.anchorMin = new Vector2(0f,0.5f);
            leftRT.anchorMax = new Vector2(0f,0.5f);
            leftRT.pivot = new Vector2(0f,0.5f);
            leftRT.sizeDelta = new Vector2(320f,40f);
            leftRT.anchoredPosition = new Vector2(16f,0f);
            // 右侧文本
            TextMeshProUGUI right = UIFactory.CreateText(row.transform,"Right", $"{item.price} G",20);
            RectTransform rightRT = right.rectTransform;
            rightRT.anchorMin = new Vector2(0.5f,0.5f);
            rightRT.anchorMax = new Vector2(1f,0.5f);
            rightRT.pivot = new Vector2(1f,0.5f);
            rightRT.sizeDelta = new Vector2(140f,40f);
            rightRT.anchoredPosition = new Vector2(45f,0f);//行左右上下移动
            // 购买按钮
            Button button =UIFactory.CreateButton(row.transform,"BuyButton","Buy",()=>TryBuyItem(item));
            RectTransform buttonRt = button.GetComponent<RectTransform>();
            buttonRt.anchorMin=new Vector2(1f,0.5f);
            buttonRt.anchorMax=new Vector2(1f,0.5f);
            buttonRt.pivot=new Vector2(1f,0.5f);
            buttonRt.sizeDelta=new Vector2(100f,40f);
            buttonRt.anchoredPosition=new Vector2(-16f,0f);
        }
        public void TryBuyItem(ShopItemData item)
        {
            if(wallet == null)
            {
                
                Debug.LogError("PlayerWallet not found in the scene.");
            }
            if(wallet.TrySpendCoins(item.price))
            {
                // 购买成功
                if(item.itemType == ShopItemType.Weapon && item.weapon != null)
                {
                    playerCombat.AddWeapon(item.weapon);
                }
                else if(item.itemType == ShopItemType.Consumable)
                {
                    inventory.AddItem(item,1);
                }
                else
                {
                    Debug.LogWarning("Unknown item type or missing weapon data for " + item.itemName);
                }
                inventory.AddItem(item,1);
                Refresh();
            }
            else
            {
                Debug.Log("Not enough coins to buy " + item.itemName);
            }
        }
        void Update()
       {
           if(Input.GetKeyDown(KeyCode.F))
           {
              Open(testShop);
           }
           if(Input.GetKeyDown(KeyCode.G))
           {
               Close();
           }
       }
    }

}