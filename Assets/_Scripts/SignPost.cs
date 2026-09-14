using UnityEngine;

using DeadCells.Interaction;

namespace DeadCells.Core
{
    /// <summary>
    /// 告示牌 —— 玩家靠近按 E 弹出说明面板（操作按键/剧情）。
    /// 面板文字写在场景的 Canvas/SignPanel/Body（TMP 组件）里，本脚本不存内容。
    /// </summary>
    public class SignPost : MonoBehaviour
    {
        [Header("头顶提示")]
        [Tooltip("玩家靠近时在物体上方显示的文字（留空 = 不显示）")]
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
            SignPanel.Instance?.Hide();
        }

        private void Update()
        {
            if (!playerNear) return;

            // 每帧上报一次，提示自己会跟着物体跑（物体销毁/禁用也会自动消失）
            if (!string.IsNullOrEmpty(promptText))
                InteractionPrompt.Show(this, promptText, PromptAnchor);

            if (Input.GetKeyDown(KeyCode.E))
                SignPanel.Instance?.Toggle();
        }
    }
}
