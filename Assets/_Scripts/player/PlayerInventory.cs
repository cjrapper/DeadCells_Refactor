using System;
using System.Collections.Generic;
using UnityEngine;

using DeadCells.Core;
using DeadCells.Data;

namespace DeadCells.Player
{
    /// <summary>背包槽位：道具 + 数量</summary>
    [Serializable]
    public class InventorySlot
    {
        public ShopItemData item;
        public int count = 1;
    }

    /// <summary>
    /// 玩家背包 —— 消耗品管理（武器走 PlayerCombat.weaponInventory）。
    /// 加道具/使用道具都广播 InventoryChange 事件，UI 订阅刷新，互不引用。
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        public List<InventorySlot> slots = new();

        public void AddItem(ShopItemData item, int count)
        {
            if (item == null || count <= 0) return;

            // 已有同类 → 叠数量；没有 → 新建槽位
            InventorySlot slot = slots.Find(s => s.item == item);
            if (slot != null) slot.count += count;
            else slots.Add(new InventorySlot { item = item, count = count });

            EventCenter.Instance?.Broadcast(EventCenter.EventType.InventoryChange.ToString());
        }

        /// <summary>使用一个消耗品，成功返回 true</summary>
        public bool UseItem(ShopItemData item)
        {
            if (item == null) return false;
            InventorySlot slot = slots.Find(s => s.item == item);
            if (slot == null || slot.count <= 0) return false;

            if (!ApplyEffect(item)) return false;

            slot.count--;
            if (slot.count <= 0) slots.Remove(slot);
            EventCenter.Instance?.Broadcast(EventCenter.EventType.InventoryChange.ToString());
            return true;
        }

        public int GetCount(ShopItemData item)
        {
            InventorySlot slot = slots.Find(s => s.item == item);
            return slot != null ? slot.count : 0;
        }

        /// <summary>执行消耗品效果（可扩展：Heal → Buff → 弹药...）</summary>
        private bool ApplyEffect(ShopItemData item)
        {
            switch (item.consumableType)
            {
                case ConsumableType.Heal:
                    PlayerHealth health = GetComponent<PlayerHealth>();
                    if (health == null) return false;
                    if (health.CurrentHealth >= health.maxHealth) return false;  // 满血不让用
                    health.Heal(item.healAmount);
                    return true;

                default:
                    return false;
            }
        }

        public bool RemoveItem(ShopItemData item, int count)
        {
            if (item == null || count <= 0) return false;
            InventorySlot slot =slots.Find(s=>s.item == item);
            if(slot==null||slot.count<count) return false;
            slot.count-=count;
            if(slot.count<=0)slots.Remove(slot);
            EventCenter.Instance?.Broadcast(EventCenter.EventType.InventoryChange.ToString());
            return true;
        }
    }
}
