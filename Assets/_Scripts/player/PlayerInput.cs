using UnityEngine;
using XLua;

namespace DeadCells.Player
{
    /// <summary>
    /// 玩家输入组件 —— 集中处理所有输入，其他组件通过它读取输入状态。
    /// </summary>
    [LuaCallCSharp]
    public class PlayerInput : MonoBehaviour
    {
        // === 输出状态 ===
        public float InputX { get; private set; }
        public float VerticalInput { get; private set; }
        public bool JumpDown { get; private set; }
        public bool JumpHeld { get; private set; }   // 跳键按住状态（可变跳高：松开=跳矮）
        public bool AttackDown { get; private set; }
        public bool AttackHeld { get; private set; }   // 攻击键按住（持续攻击 channel 用）
        public bool DashDown { get; private set; }
        public bool SwitchWeaponDown { get; private set; }
        public bool InPutLocked { get; set; }// 输入锁定（眩晕、UI 等状态下锁定输入）

        // === 输入缓存参数 ===
        [Header("Jump Buffer / Coyote Time")]
        public float jumpBufferDuration = 0.2f;
        public float coyoteDuration = 0.1f;

        public float JumpBufferTimer { get; private set; }
        public float CoyoteTimer { get; private set; }

        private bool grounded;

        public void Tick(bool isGrounded)
        {
            if (InPutLocked)
            {
                ClearOutputs();
                return;
            }
            grounded = isGrounded;

            // 方向
            InputX = Input.GetAxisRaw("Horizontal");
            VerticalInput = Input.GetAxisRaw("Vertical");

            // 先读取一次，避免重复消耗 GetButtonDown
            bool jumpPressed = Input.GetButtonDown("Jump");

            // 跳跃 Buffer
            if (jumpPressed) JumpBufferTimer = jumpBufferDuration;
            else JumpBufferTimer -= Time.deltaTime;

            // Coyote Time
            if (grounded) CoyoteTimer = coyoteDuration;
            else CoyoteTimer -= Time.deltaTime;

            // 单帧按钮
            JumpDown = jumpPressed;
            JumpHeld = Input.GetButton("Jump");   // 按住状态（可变跳高用）
            AttackDown = Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.K);
            AttackHeld = Input.GetButton("Fire1") || Input.GetKey(KeyCode.K);   // 持续攻击判定
            DashDown = Input.GetKeyDown(KeyCode.LeftShift);
            SwitchWeaponDown = Input.GetKeyDown(KeyCode.Q);
        }

        /// <summary>
        /// 清零所有输出（眩晕用）—— 状态机读到的输入全是 0，玩家就无法操作。
        /// 比"给状态机加个眩晕状态"简单得多，而且对所有状态同时生效。
        /// </summary>
        public void ClearOutputs()
        {
            InputX = 0f;
            VerticalInput = 0f;
            JumpDown = false;
            JumpHeld = false;
            AttackDown = false;
            AttackHeld = false;
            DashDown = false;
            SwitchWeaponDown = false;
            JumpBufferTimer = 0f;
        }

        /// <summary>
        /// 消费跳跃缓冲："下+跳"穿平台时调用——下落不算跳跃，
        /// 必须清空缓冲，否则 0.2s 缓冲窗口内会自动起跳（Bug 回归源）。
        /// </summary>
        public void ConsumeJumpBuffer()
        {
            JumpBufferTimer = 0f;
        }
    }
}
