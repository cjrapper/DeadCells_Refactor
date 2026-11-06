using DeadCells.Core;
using DeadCells.Data;
using DeadCells.Interaction;
using DeadCells.Player;
using UnityEngine;

namespace DeadCells.Traps
{
    public class Door : MonoBehaviour
    {
        [Header("钥匙要求")] public ShopItemData requiredKey;
        public int KeyCount = 1;
        [Header("开门表现")]
        public Sprite icon;
        public Collider2D doorCollider;

        [Header("交互")]
        public bool needInteractKey = false;

        [Tooltip("交互范围（可选，推荐配）。做法：门本体挂挡路的实心碰撞体，" +
                 "另建一个子物体挂 Trigger 当交互范围，把它拖到这里。\n" +
                 "留空 = 用本物体自己的碰撞体")]
        public Collider2D interactionTrigger;

        [Tooltip("打开后打印进入/离开交互范围 —— 排查\"靠近没提示\"时打开")]
        public bool debugLogRange = false;

        [Header("头顶提示")]
        [Tooltip("玩家靠近时在门上方显示的文字（留空 = 不显示）")]
        public string promptText = "Press E";

        private SpriteRenderer sr;
        private Renderer cachedRenderer;   // 缓存：用来算"头顶"位置，不能每帧 GetComponent
        private bool isOpen;
        private bool playerNear;
        private PlayerInventory inventory;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            cachedRenderer = GetComponentInChildren<Renderer>();
            if (doorCollider == null) doorCollider = GetComponent<Collider2D>();
        }

        /// <summary>提示锚点 = 贴图顶边中点（用包围盒算，贴图多大都能顶在头上，不用逐个配高度）</summary>
        private Vector3 PromptAnchor
        {
            get
            {
                if (cachedRenderer != null)
                    return new Vector3(transform.position.x, cachedRenderer.bounds.max.y, 0f);
                return transform.position + Vector3.up * 1.2f;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // 只有"没配 interactionTrigger、用本物体碰撞体"时才走这条路。
            // 配了单独触发器的话由 UpdatePlayerNear 每帧检测 —— 原因见 UpdatePlayerNear 的注释
            if (interactionTrigger != null) return;
            if (!other.CompareTag("Player")) return;
            playerNear = true;
            CacheInventory(other);

            // needInteractKey = false → 碰到就开（和 TreasureChest.autoOpen 同一套语义）。
            // 旧代码在这种情况下门【永远不会开】：Open() 只有 Update 里的 E 键这一条调用路径。
            if (!needInteractKey) TryOpen();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (interactionTrigger != null) return;
            if (!other.CompareTag("Player")) return;
            playerNear = false;
            InteractionPrompt.Hide(this);
        }

        private static readonly Collider2D[] overlapBuffer = new Collider2D[8];

        /// <summary>
        /// 每帧检测玩家是否在交互范围内。
        ///
        /// ★ 为什么不用 OnTriggerEnter2D/Exit2D：
        ///   2D 物理的碰撞/触发回调只发给【挂着碰撞体的那个物体】，
        ///   子物体上的 Trigger 不会把回调转发给父物体上的脚本（3D 会转发给 Rigidbody 所在物体，2D 不会）。
        ///   "门本体挡路 + 子物体当交互范围"是最常见的搭法，所以这里必须自己做重叠检测，
        ///   否则表现就是：走到门前没提示、按 E 也没反应，而且一个报错都没有。
        ///
        /// 用 OverlapBoxNonAlloc（非分配版本），不会每帧产生 GC。
        /// </summary>
        private void UpdatePlayerNear()
        {
            Collider2D zone = interactionTrigger != null ? interactionTrigger : GetComponent<Collider2D>();
            if (zone == null || !zone.enabled)
            {
                playerNear = false;
                return;
            }

            Bounds b = zone.bounds;
            int count = Physics2D.OverlapBoxNonAlloc(b.center, b.size, 0f, overlapBuffer);

            bool found = false;
            for (int i = 0; i < count; i++)
            {
                Collider2D col = overlapBuffer[i];
                if (col == null || !col.CompareTag("Player")) continue;
                found = true;
                if (inventory == null) CacheInventory(col);
                break;
            }

            if (found != playerNear)
            {
                playerNear = found;
                if (debugLogRange)
                    Debug.Log($"[Door] {name}: 玩家{(found ? "进入" : "离开")}交互范围");
            }
        }

        private void Update()
        {
            if (isOpen) return;

            UpdatePlayerNear();
            if (!playerNear) return;

            // needInteractKey = false → 碰到就开
            if (!needInteractKey)
            {
                TryOpen();
                return;
            }

            // 每帧上报一次，提示自己会跟着门跑
            if (!string.IsNullOrEmpty(promptText))
                InteractionPrompt.Show(this, promptText, PromptAnchor);

            if (Input.GetKey(KeyCode.E)) TryOpen();
        }

        void TryOpen()
        {
            // ★ 原来是 if(!isOpen) return; —— isOpen 初始就是 false，所以这里永远直接 return，
            //    门根本打不开（Open() 只能由这里调用，形成死锁）。正确写法是"已经开了就别再开"
            if (isOpen) return;
            if (inventory == null) return;

            // 没配 requiredKey 就当"不需要钥匙的门"。
            // 旧代码在 requiredKey 为空时会一直卡在"缺少钥匙"：因为 GetCount(null) 返回 0，
            // 0 < KeyCount 恒成立 —— 看起来像 bug，其实只是配置漏了一个引用。
            if (requiredKey != null)
            {
                int have = inventory.GetItemCount(requiredKey);
                if (have < KeyCount)
                {
                    Debug.Log($"[Door] 缺少钥匙：需要 {KeyCount} 个「{requiredKey.itemName}」，当前 {have} 个");
                    return;
                }
                inventory.RemoveItem(requiredKey, KeyCount);
            }
            else
            {
                Debug.LogWarning($"[Door] {name} 勾了 Need Interact Key 但没配 Required Key —— " +
                                 "已按\"不需要钥匙\"处理。要锁门请在 Inspector 里拖入一个 ShopItemData 资产");
            }

            Open();
        }

        private void Open()
        {
            isOpen = true;
            if (sr != null && icon != null) sr.sprite = icon;
            if (doorCollider != null) doorCollider.enabled = false;
            EventCenter.Instance?.Broadcast(EventCenter.EventType.DoorOpened.ToString());
            InteractionPrompt.Hide(this);
        }

        void CacheInventory(Collider2D other)
        {
            if (inventory == null)
            {
                inventory = other.GetComponent<PlayerInventory>();
            }
        }
    }
}
