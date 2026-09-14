using UnityEngine;

using DeadCells.Data;
using DeadCells.Interaction;

namespace DeadCells.Core
{
    /// <summary>
    /// 商店 NPC —— 交互载体（不做场景切换，商业项目常规做法）。
    /// 玩家进入 Trigger 范围按 E 打开/关闭商店面板。
    /// </summary>
    public class ShopNPC : MonoBehaviour
    {
        [Header("商店配置")]
        public ShopData shopData;

        [Header("头顶提示")]
        [Tooltip("玩家靠近时在 NPC 上方显示的文字（留空 = 不显示）")]
        public string promptText = "Press E";

        private bool playerNear;
        private Renderer cachedRenderer;   // 缓存：用来算"头顶"位置，不能每帧 GetComponent

        private void Awake()
        {
            cachedRenderer = GetComponentInChildren<Renderer>();
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
            if (!other.CompareTag("Player")) return;
            playerNear = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            playerNear = false;
            InteractionPrompt.Hide(this);

            // 离开范围自动关商店（防止面板悬空）
            ShopPanel.Instance?.Close();
        }

        private void Update()
        {
            if (!playerNear) return;

            // 每帧上报一次，提示自己会跟着 NPC 跑（NPC 销毁/禁用也会自动消失）
            if (!string.IsNullOrEmpty(promptText))
                InteractionPrompt.Show(this, promptText, PromptAnchor);

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (ShopPanel.Instance != null && ShopPanel.Instance.IsOpen)
                    ShopPanel.Instance.Close();
                else
                    ShopPanel.Instance?.Open(shopData);
            }
        }
    }
}
