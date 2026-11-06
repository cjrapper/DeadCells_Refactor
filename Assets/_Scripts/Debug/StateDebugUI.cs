using UnityEngine;
using TMPro;

using DeadCells.Player;

namespace DeadCells.Core
{
    /// <summary>
    /// 状态机状态显示（调试 HUD）—— 挂在头像下方的 TMP 文本物体上。
    /// 显示单层当前状态名（默认 BASE 层；想看攻击/冲刺切 layer=2，受击切 layer=3）。
    /// 面试演示 + 开发调试两用：给面试官看"状态机在实时工作"比讲十分钟都直观。
    /// </summary>
    public class StateDebugUI : MonoBehaviour
    {
        [Tooltip("1=BASE（地面/空中） 2=ACTIVE（攻击/冲刺） 3=ABNORMAL（受击）")]
        public int layer = 1;

        private TextMeshProUGUI text;
        private PlayerLuaBridge bridge;

        private void Awake()
        {
            // 挂在文本物体上直接拿组件（GetComponent 必须有空判断）
            text = GetComponent<TextMeshProUGUI>();
            if (text == null)
                Debug.LogError("[StateDebugUI] 请把脚本挂在 TMP 文本物体上");
        }

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                bridge = player.GetComponent<PlayerLuaBridge>();
        }

        private void Update()
        {
            if (text == null || bridge == null) return;

            // GetCurrentStateName 是 PlayerLuaBridge 的公开方法（查询 Lua HFSM 当前状态）
            text.text = bridge.GetCurrentStateName(layer);
        }
    }
}
