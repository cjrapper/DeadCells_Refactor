using UnityEngine;

using DeadCells.Combat;

namespace DeadCells.Data
{
    /// <summary>商品/道具类型</summary>
    public enum ShopItemType
    {
        Weapon,      // 武器：进武器栏（Q 切换）
        Consumable,  // 消耗品：进背包（使用生效）
    }

    /// <summary>消耗品效果类型</summary>
    public enum ConsumableType
    {
        None,
        Heal,        // 回血
    }

    /// <summary>
    /// 商品/道具数据 —— 数据驱动：商店卖什么、道具什么效果，全部配置化，不改代码。
    /// </summary>
    [CreateAssetMenu(fileName = "NewShopItem", menuName = "Data/Shop Item")]
    public class ShopItemData : ScriptableObject
    {
        [Header("基础（名字用英文：项目字体只有 ASCII，中文会显示成方框）")]
        public string itemName = "UNKNOWN";
        public Sprite icon;                 // 图标（可空）
        public int price = 10;              // 售价

        [Header("类型")]
        public ShopItemType itemType = ShopItemType.Consumable;

        [Header("武器类配置（itemType = Weapon）")]
        public WeaponData weapon;           // 购买的武器资产

        [Header("消耗品类配置（itemType = Consumable）")]
        public ConsumableType consumableType = ConsumableType.None;
        public int healAmount = 20;         // Heal 回血量
    }
}
