using DeadCells.Data;
using UnityEngine;
using DeadCells.Shop;
using System.Collections.Generic;
using DeadCells.Core;
namespace DeadCells.Player
{
    public class PlayerInventory:MonoBehaviour
{
    [System.Serializable]
    public class InventorySlot
    {
        public ShopItemData itemData;
        public int count;
    }
    public List<InventorySlot>slots = new();//背包栏位
    public bool AddItem(ShopItemData item,int amount)
    {
        if(item==null||amount<=0)return false;
        //检查是否已有该物品
        var slot=slots.Find(s=>s.itemData==item);
        if(slot!=null)
        {
            slot.count+=amount;
        }
        else
        {
            slots.Add(new InventorySlot{itemData=item,count=amount});
        }
        EventCenter.Instance.Broadcast(EventCenter.EventType.InventoryChange.ToString());
        return true;
    }
    public bool TrySpendItem(ShopItemData item,int amount)
    {
        var slot=slots.Find(s=>s.itemData==item);
        if(item==null||amount<=0||slot==null||slot.count<amount)return false;
        UseItem(item);
        slot.count-=amount;
        if(slot.count<=0)
        {
            slots.Remove(slot);
        }
        EventCenter.Instance.Broadcast(EventCenter.EventType.InventoryChange.ToString());
        return true;
    
    }
    public int GetItemCount(ShopItemData item)
    {
        var slot=slots.Find(s=>s.itemData==item);
        return slot!=null?slot.count:0;
    }
    public bool RemoveItem(ShopItemData item,int amount)
    {
        var slot=slots.Find(s=>s.itemData==item);
        if(item==null||amount<=0||slot==null||slot.count<amount)return false;
        slot.count-=amount;
        if(slot.count<=0)
        {
            slots.Remove(slot);
        }
        EventCenter.Instance.Broadcast(EventCenter.EventType.InventoryChange.ToString());
        return true;
    }
    public void UseItem(ShopItemData item)
    {
        if(item==null)return;
        switch(item.consumableType)
        {
            case ConsumableType.Heal:
                var playerHealth=GetComponent<PlayerHealth>();
                if(playerHealth!=null)
                {
                    playerHealth.Heal(item.healAmount);
                }
                break;
            default:
                Debug.LogWarning("未实现的消耗品类型："+item.consumableType);
                break;
        }
    }
}
}

