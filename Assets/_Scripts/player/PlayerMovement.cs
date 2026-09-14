using System.Collections;
using UnityEngine;
using XLua;

namespace DeadCells.Player
{
    /// <summary>
    /// 玩家移动组件 —— 处理水平移动、跳跃、重力、贴墙、单向平台、冲刺物理。
    /// </summary>
    [LuaCallCSharp]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("References")]
        public Transform groundCheckPos;
        public Transform wallCheckPos;

        [Header("Movement")]
        public float moveSpeed = 5f;
        public float jumpForce = 10f;            // 12 → 10（配合上升重力 2，最高点约 2.5 格）
        public float risingGravityScale = 2f;    // 1 → 2（弧线聚拢，不飘）
        public float fallingGravityScale = 3.5f; // 2.5 → 3.5（下落更扎实）

        [Header("Jump Feel（可变跳高 / 空中惯性）")]
        [Tooltip("松开跳键后允许的最大上升速度：轻点跳 = 跳矮，按住跳 = 跳高（蔚蓝同款）")]
        public float jumpCutSpeed = 5f;          // ≈ jumpForce * 0.5
        [Tooltip("空中水平加速度：越大转向越跟手，越小惯性越强")]
        public float airAccel = 60f;

        [Header("Ladder")]
        [Tooltip("爬梯速度")]
        public float climbSpeed = 3f;

        [Header("Physics")]
        public PhysicsMaterial2D noFrictionMaterial;
        public float checkRadius = 0.3f;
        public LayerMask groundLayer;
        public LayerMask wallLayer;
        public LayerMask oneWayPlatformLayer;

        [Header("Wall Mechanics")]
        public float wallSlideSpeed = 2f;
        public Vector2 wallJumpForce = new Vector2(10f, 12f);
        public float wallJumpTime = 0.2f;

        [Header("Dash")]
        public float dashSpeed = 15f;
        public float dashTime = 0.2f;
        public float dashCooldown = 1f;

        [Header("Step Climb（小台阶自动走上去，不用起跳）")]
        public bool enableStepClimb = true;
        [Tooltip("能自动走上去的最大台阶高度（世界单位）")]
        public float stepHeight = 0.45f;
        [Tooltip("前方探测距离")]
        public float stepProbeForward = 0.35f;

        // 运行时状态
        public Rigidbody2D Rb { get; private set; }
        public Collider2D BodyCollider { get; private set; }
        public float DashCooldownTimer { get; set; }

        private Vector2 workVector;
        private Collider2D currentPlatformCollider;
        private Coroutine platformDropCoroutine;
        private bool isFallingThroughPlatform;

        private void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
            BodyCollider = GetComponent<Collider2D>();
            if (noFrictionMaterial != null && BodyCollider != null)
                BodyCollider.sharedMaterial = noFrictionMaterial;
        }

        // ============== 台阶自动攀爬 ==============

        /// <summary>
        /// 小台阶自动走上去（塔科夫式"step offset"的 2D 版）：
        /// 贴地移动时，若前方有"低矮台阶"（高度 ≤ stepHeight）且上方有空间 → 把玩家抬到台阶面上，
        /// 避免"很小的楼梯也要小跳"的别扭感。高台不会爬（stepHeight 限制）。
        /// </summary>
        private void FixedUpdate()
        {
            if (!enableStepClimb) return;
            if (Rb == null || groundCheckPos == null) return;
            if (isFallingThroughPlatform) return;
            if (Mathf.Abs(Rb.velocity.x) < 0.15f) return;   // 没在水平移动
            if (!CheckGrounded()) return;                    // 只在地面时爬

            float dir = Mathf.Sign(Rb.velocity.x);
            Vector2 foot = groundCheckPos.position;
            LayerMask blockMask = groundLayer | oneWayPlatformLayer;

            // ① 前方（脚底高度）有阻挡才算遇到台阶
            RaycastHit2D wall = Physics2D.Raycast(foot, new Vector2(dir, 0f),
                stepProbeForward, blockMask);
            if (wall.collider == null) return;

            // ② 抬到 stepHeight 高度后，前方必须是空的（否则是墙，不能爬）
            Vector2 highPivot = new Vector2(foot.x, foot.y + stepHeight + 0.05f);
            RaycastHit2D highBlock = Physics2D.Raycast(highPivot, new Vector2(dir, 0f),
                stepProbeForward, blockMask);
            if (highBlock.collider != null) return;

            // ③ 从高处向下找台阶顶面
            Vector2 downFrom = new Vector2(foot.x + dir * stepProbeForward, foot.y + stepHeight + 0.05f);
            RaycastHit2D stepTop = Physics2D.Raycast(downFrom, Vector2.down,
                stepHeight + 0.1f, blockMask);
            if (stepTop.collider == null) return;

            float rise = stepTop.point.y - foot.y;
            if (rise <= 0.01f || rise > stepHeight) return;

            // ④ 抬升玩家到台阶面（+0.02 余量保证落地检测成立）
            transform.position = new Vector3(
                transform.position.x,
                transform.position.y + rise + 0.02f,
                transform.position.z);
        }

        // ============== 检测方法 ==============

        public bool CheckGrounded()
    {
        if (groundCheckPos == null) return false;
        return !isFallingThroughPlatform &&
            (Physics2D.OverlapCircle(groundCheckPos.position, checkRadius, groundLayer) ||
             Physics2D.OverlapCircle(groundCheckPos.position, checkRadius, oneWayPlatformLayer));
    }
    
        public bool CheckTouchingWall()
        {
            if (wallCheckPos == null) return false;
            return !isFallingThroughPlatform &&
                Physics2D.OverlapCircle(wallCheckPos.position, checkRadius, wallLayer);
        }

        // ============== 速度设置 ==============

        public void SetVelocityX(float x)
        {
            workVector.Set(x, Rb.velocity.y);
            Rb.velocity = workVector;
        }

        public void SetVelocityY(float y)
        {
            workVector.Set(Rb.velocity.x, y);
            Rb.velocity = workVector;
        }

        public void SetVelocity(float x, float y)
        {
            workVector.Set(x, y);
            Rb.velocity = workVector;
        }

        // ============== 翻转 ==============

        public void CheckFlip(float inputX)
        {
            if (inputX > 0 && transform.localScale.x < 0) Flip();
            else if (inputX < 0 && transform.localScale.x > 0) Flip();
        }

        public void Flip()
        {
            Vector3 s = transform.localScale;
            s.x *= -1;
            transform.localScale = s;
        }

        // ============== 单向平台 ==============

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("OneWayPlatform"))
                currentPlatformCollider = collision.collider;
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("OneWayPlatform"))
                currentPlatformCollider = collision.collider;
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("OneWayPlatform") &&
                collision.collider == currentPlatformCollider)
                currentPlatformCollider = null;
        }

        public void StartPlatformDrop()
        {
            if (currentPlatformCollider == null) return;
            if (platformDropCoroutine != null) StopCoroutine(platformDropCoroutine);
            platformDropCoroutine = StartCoroutine(PlatformDropRoutine());
        }

        private IEnumerator PlatformDropRoutine()
        {
            isFallingThroughPlatform = true;
            Collider2D platform = currentPlatformCollider;
            platform.enabled = false;
            yield return new WaitForSeconds(0.3f);
            if (platform != null) platform.enabled = true;
            isFallingThroughPlatform = false;
            currentPlatformCollider = null;
        }
    }
}
