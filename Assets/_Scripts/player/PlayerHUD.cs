using UnityEngine;
using UnityEngine.UI;
using DeadCells.Core;
using TMPro;
namespace DeadCells.Player
{
    public class PlayerHUD:MonoBehaviour
    {
        
       public Transform content;
       public PlayerInventory inventory;
       public PlayerCombat combat;
       public PlayerKeys keys;

       void Awake()
        {
            //获取playerInventory和playerCombat
            if(inventory == null)
            {
                inventory = FindObjectOfType<PlayerInventory>();
                if(inventory == null)
                {
                    Debug.LogError("PlayerInventory not found in the scene.");
                    return;
                }
            }
            if(combat == null)
            {
                combat = FindObjectOfType<PlayerCombat>();
                if(combat == null)
                {
                    Debug.LogError("PlayerCombat not found in the scene.");
                    return;
                }
            }
            if(keys == null)
            {
                keys = FindObjectOfType<PlayerKeys>();
                if(keys == null)
                {
                    Debug.LogError("PlayerKeys not found in the scene.");
                    return;
                }
            }
            // 创建内容容器
            var go = new GameObject("Content",typeof(RectTransform),typeof(Image));
            go.transform.SetParent(transform,false);
            RectTransform rt = go.GetComponent<RectTransform>();
            //屏幕左上角
            rt.anchorMin = new Vector2(0,0.75f);
            rt.anchorMax = new Vector2(0,0.75f);
            rt.pivot = new Vector2(0,1);
            rt.anchoredPosition = new Vector2(10,-10);
            rt.sizeDelta = new Vector2(200,100);
            Image img = go.GetComponent<Image>();
            img.color = new Color(0.1f,0.1f,0.15f,0.95f);
            content = go.transform;
            Refresh();

        }
        public void OnEnable()
        {
            EventCenter.Instance.AddListener<int>(EventCenter.EventType.InventoryChange.ToString(),UpdateItemUI);
            EventCenter.Instance.AddListener<int>(EventCenter.EventType.KeyChange.ToString(),UpdateKeyUI);
            EventCenter.Instance.AddListener<int>(EventCenter.EventType.WeaponChange.ToString(),UpdateWeaponUI);
        }
        public void OnDisable()
        {
            EventCenter.Instance.RemoveListener<int>(EventCenter.EventType.InventoryChange.ToString(),UpdateItemUI);
            EventCenter.Instance.RemoveListener<int>(EventCenter.EventType.KeyChange.ToString(),UpdateKeyUI);
            EventCenter.Instance.RemoveListener<int>(EventCenter.EventType.WeaponChange.ToString(),UpdateWeaponUI);
        }
        void UpdateItemUI(int itemCount)
        {
            //更新物品UI
            Refresh();
            Debug.Log($"[PlayerHUD] Item count updated: {itemCount}");
        }
        void UpdateKeyUI(int keyCount)
        {
            //更新钥匙UI
            Refresh();
        }
        void UpdateWeaponUI(int currentWeaponIndex)
        {
            //更新武器UI
            Refresh();
            Debug.Log($"[PlayerHUD] Current weapon index updated: {currentWeaponIndex}");
        }
        public void Refresh()
        {
            for(int i=content.childCount-1;i>=0;i--)
            {
                Destroy(content.GetChild(i).gameObject);
            }
            CreateRow("POTION x3", 0);
            CreateRow("SWORD ▶", 26);
            CreateRow("KEY x2", 52);
        }
        RectTransform CreateRow(string text,float y)
        {
            TextMeshProUGUI txt= UIFactory.CreateText(content,text,text,18);
            RectTransform rt=txt.rectTransform;
            rt.anchorMin=new Vector2(0,1);
            rt.anchorMax=new Vector2(0,1);
            rt.pivot=new Vector2(0,1);
            rt.sizeDelta=new Vector2(200,30);
            rt.anchoredPosition=new Vector2(10,-y);
            return rt;
        }
    }
}