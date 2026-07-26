using System.Collections;
using UnityEngine;

namespace DeadCells.Player
{
    /// <summary>
    /// 玩家移动组件 —— 处理水平移动、跳跃、重力、贴墙、单向平台、冲刺物理。
    /// </summary>
    public class PlayerMovement : MonoBehaviour
    {
        [Header("References")]
        public Transform groundCheckPos;
        public Transform wallCheckPos;

        [Header("Movement")]
        public float moveSpeed = 5f;
        public float jumpForce = 12f;
        public float risingGravityScale = 1f;
        public float fallingGravityScale = 2.5f;

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
