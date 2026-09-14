using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using DeadCells.Combat;
using DeadCells.Data;
using DeadCells.Player;

namespace DeadCells.Core
{
    /// <summary>
    /// 背包面板 —— 挂在 Canvas 下的空物体上（如 Canvas/InventoryPanel）。
    /// 按 B 开关；上半区武器列表（点击切换，当前武器高亮），下半区消耗品列表（点击使用）。
    /// 全部走事件订阅刷新（WeaponChange / InventoryChange），不直接引用战斗模块。
    ///
    /// ★ 为什么要分出一个 Content 子容器：
    ///   旧实现 Awake 里直接 gameObject.SetActive(false) ——
    ///   物体一旦未激活，Unity 就不再调它的 Update，于是"按 B 打开"这段代码永远跑不到，
    ///   面板再也打不开（而且不报任何错）。
    ///   正确做法：自己保持激活（只负责听按键），把 UI 内容放子容器里，隐藏的是子容器。
    /// </summary>
    public class InventoryPanel : MonoBehaviour
    {
        private PlayerCombat playerCombat;
        private PlayerInventory playerInventory;
        private Transform content;     // 所有可见 UI 的父物体；显示/隐藏的是它

        private void Awake()
        {
            // 建内容容器：自己始终激活，才有 Update 能收到 B 键
            var go = new GameObject("Content", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            content = go.transform;
            content.gameObject.SetActive(false);

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerCombat = player.GetComponent<PlayerCombat>();
                playerInventory = player.GetComponent<PlayerInventory>();
            }
        }

        private void OnEnable()
        {
            // 订阅事件（OnEnable 订阅，OnDisable 退订 —— 规范防泄漏）
            EventCenter.Instance?.AddListener<int>(EventCenter.EventType.WeaponChange.ToString(), OnWeaponChange);
            EventCenter.Instance?.AddListener(EventCenter.EventType.InventoryChange.ToString(), OnInventoryChange);
        }

        private void OnDisable()
        {
            EventCenter.Instance?.RemoveListener<int>(EventCenter.EventType.WeaponChange.ToString(), OnWeaponChange);
            EventCenter.Instance?.RemoveListener(EventCenter.EventType.InventoryChange.ToString(), OnInventoryChange);
        }

        private void OnWeaponChange(int _) { if (IsOpen) Refresh(); }
        private void OnInventoryChange() { if (IsOpen) Refresh(); }

        /// <summary>面板是否打开（外部查询用；也用来避免关着的时候白刷 UI）</summary>
        public bool IsOpen => content != null && content.gameObject.activeSelf;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.B)) Toggle();
        }

        public void Toggle()
        {
            if (content == null) return;
            bool open = !content.gameObject.activeSelf;
            content.gameObject.SetActive(open);
            if (open) Refresh();
        }

        /// <summary>重建面板内容（数据驱动：武器/道具都在组件数据里，UI 只负责展示）</summary>
        private void Refresh()
        {
            if (content == null) return;

            // 清掉上一批动态 UI
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);

            float y = 0f;
            UIFactory.CreateText(content, "Title", "INVENTORY", 28)
                .rectTransform.anchoredPosition = new Vector2(0f, -20f);

            // ===== 武器区 =====
            y += 50f;
            UIFactory.CreateText(content, "WeaponHeader", "-- WEAPONS (Q / click) --", 18)
                .rectTransform.anchoredPosition = new Vector2(0f, -y);

            if (playerCombat != null && playerCombat.weaponInventory != null)
            {
                for (int i = 0; i < playerCombat.weaponInventory.Count; i++)
                {
                    y += 45f;
                    WeaponData w = playerCombat.weaponInventory[i];
                    bool isCurrent = i == playerCombat.CurrentIndex;

                    string mark = isCurrent ? "▶ " : "   ";
                    TextMeshProUGUI wText = UIFactory.CreateText(
                        content, "Weapon" + i, $"{mark}{w.weaponName}", 18);
                    wText.rectTransform.anchoredPosition = new Vector2(-120f, -y);
                    if (isCurrent) wText.color = new Color(1f, 0.85f, 0.3f);

                    int capturedIndex = i;
                    Button btn = UIFactory.CreateButton(content, "SwitchBtn", "Equip", () =>
                    {
                        playerCombat?.SelectWeapon(capturedIndex);
                    });
                    RectTransform bRt = btn.GetComponent<RectTransform>();
                    bRt.anchoredPosition = new Vector2(150f, -y);
                    bRt.sizeDelta = new Vector2(70f, 32f);
                }
            }

            // ===== 消耗品区 =====
            y += 50f;
            UIFactory.CreateText(content, "ItemHeader", "-- CONSUMABLES --", 18)
                .rectTransform.anchoredPosition = new Vector2(0f, -y);

            if (playerInventory != null && playerInventory.slots.Count > 0)
            {
                foreach (InventorySlot slot in playerInventory.slots)
                {
                    if (slot == null || slot.item == null) continue;
                    y += 45f;

                    UIFactory.CreateText(content, "ItemName", $"{slot.item.itemName} x{slot.count}", 18)
                        .rectTransform.anchoredPosition = new Vector2(-120f, -y);

                    ShopItemData captured = slot.item;
                    Button btn = UIFactory.CreateButton(content, "UseBtn", "Use", () =>
                    {
                        playerInventory?.UseItem(captured);
                    });
                    RectTransform bRt = btn.GetComponent<RectTransform>();
                    bRt.anchoredPosition = new Vector2(150f, -y);
                    bRt.sizeDelta = new Vector2(70f, 32f);
                }
            }
            else
            {
                UIFactory.CreateText(content, "Empty", "(empty - buy something at the shop)", 16)
                    .rectTransform.anchoredPosition = new Vector2(0f, -(y + 30f));
            }
        }
    }
}
