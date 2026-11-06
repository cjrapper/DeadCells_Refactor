using System.Collections.Generic;
using UnityEngine;

namespace DeadCells.Data
{
    /// <summary>
    /// 商店配置 —— 一张表配出整个商店的商品列表（数据驱动）。
    /// </summary>
    [CreateAssetMenu(fileName = "NewShop", menuName = "Data/Shop Data")]
    public class ShopData : ScriptableObject
    {
        [Header("商店名称")]
        public string shopName = "SHOP";

        [Header("商品列表")]
        public List<ShopItemData> items = new();//货架
    }
}
