using UnityEngine;

namespace DeadCells.Player
{
    /// <summary>
    /// 玩家输入组件 —— 集中处理所有输入，其他组件通过它读取输入状态。
    /// </summary>
    public class PlayerInput : MonoBehaviour
    {
        // === 输出状态 ===
        public float InputX { get; private set; }
        public float VerticalInput { get; private set; }
        public bool JumpDown { get; private set; }
        public bool AttackDown { get; private set; }
        public bool DashDown { get; private set; }
        public bool SwitchWeaponDown { get; private set; }

        // === 输入缓存参数 ===
        [Header("Jump Buffer / Coyote Time")]
        public float jumpBufferDuration = 0.2f;
        public float coyoteDuration = 0.1f;

        public float JumpBufferTimer { get; private set; }
        public float CoyoteTimer { get; private set; }

        private bool grounded;

        public void Tick(bool isGrounded)
        {
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
            AttackDown = Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.K);
            DashDown = Input.GetKeyDown(KeyCode.LeftShift);
            SwitchWeaponDown = Input.GetKeyDown(KeyCode.Q);
        }
    }
}
