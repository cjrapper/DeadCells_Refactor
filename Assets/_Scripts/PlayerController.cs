using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System.IO;

public class PlayerController : MonoBehaviour, IDamageable
{ 
    #region Settings
    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;
    private SpriteRenderer sr;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("Jump Feel (Coyote & Buffer)")]
    // 核心手感优化：土狼时间与预输入缓冲
    public float jumpBufferTime = 0.2f; // 预输入：落地前按下跳跃也能触发
    public float coyoteTime = 0.1f;     // 土狼时间：离开平台后短时间内仍可跳跃

    [Header("Ground Detection")]
    public Transform feetPos;
    public float checkRadius = 0.3f;
    public LayerMask ground;

    [Header("Combat System")]
    public Transform attackPoint;
    public WeaponData currentWeapon;
    public Animator weaponAnimator;
    private float nextAttackTime = 0f;
    // 性能优化：使用 Hash ID 代替字符串，提升 SetTrigger 性能
    private static readonly int AttackAnimID = Animator.StringToHash("Attack");

    [Header("Dash Settings")]
    public float dashSpeed = 15f;
    public float dashTime = 0.2f;
    public float dashCooldown = 1f;
    private bool isDashing;
    private bool canDash = true;
    private TrailRenderer tr;

    [Header("Effects")]
    public GameObject hitEffectPrefab;
    public CinemachineImpulseSource impulseSource;

    [Header("One Way Platform Settings")]
    public float platformFallForce = -10f; // 下落时的向下初始速度，增强手感
    public float platformFallTime = 0.5f;  // 忽略碰撞的时间
    private Collider2D playerCollider;     // 缓存玩家碰撞体
    private Collider2D currentPlatformCollider; // 缓存当前平台的碰撞体

    [Header("Wall Jump Settings")]
    public Transform frontCheck;// Check for wall in front
    public LayerMask whatIsWall;
    public float wallSlideSpeed;
    public Vector2 wallJumpForce;// Force applied when wall jumping
    public float wallJumpTime = 0.2f; // Time to wall jump
    public bool isTouchingWall;
    public bool isWallSliding;
    public bool isWallJumping;
    private bool isFallingThroughPlatform = false; // 标记是否正在穿过平台
    #endregion

    // Internal Variables
    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private bool isHurting; // 受击状态锁：受击时暂时锁定控制
    private float jumpBufferCounter;
    private float coyoteTimeCounter;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        tr = GetComponent<TrailRenderer>(); // Ensure TrailRenderer is attached for Dash
        playerCollider = GetComponent<Collider2D>(); // 缓存 Player Collider

        // Fix Wall Stick: Create zero-friction material dynamically
        // 解决粘墙问题：动态创建一个摩擦力为0的材质
        PhysicsMaterial2D noFriction = new PhysicsMaterial2D("NoFriction");
        noFriction.friction = 0f;
        GetComponent<Collider2D>().sharedMaterial = noFriction;

        sr = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
    }

    void Update()
    {
        // ================================================================
        // 状态机优先级 (State Priority):
        // 1. 受击 (Hurting) - 最高优先级，完全锁定
        // 2. 蹬墙跳 (WallJumping) - 锁定移动，由物理惯性控制
        // 3. 冲刺 (Dashing) - 锁定输入，忽略重力
        // 4. 正常移动 (Normal Move) - 默认状态
        // ================================================================

        if (isWallJumping) return;
        if (isDashing) return; // Lock input while dashing

        // 1. Input Processing
        moveInput = Input.GetAxisRaw("Horizontal"); 

        // 2. Wall Logic (Check first)
        isTouchingWall = Physics2D.OverlapCircle(frontCheck.position, checkRadius, whatIsWall);
        
        // 防止穿墙时触发滑墙逻辑 (!isFallingThroughPlatform)
        if(isTouchingWall && !isGrounded && rb.velocity.y < 0 && !isFallingThroughPlatform)
        {
            isWallSliding = true;
        }
        else 
        {
            isWallSliding = false;
        }

        if(isWallSliding)
        {
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlideSpeed, float.MaxValue));
        }

        // 3. Wall Jump (Highest Priority Jump)
        if(Input.GetButtonDown("Jump") && isWallSliding)
        {
            WallJump();
            return; // Exit Update to prevent double jump logic
        }

        // 4. Ground Check
        isGrounded = Physics2D.OverlapCircle(feetPos.position, checkRadius, ground);

        // 3. Coyote Time Logic (土狼时间)
        // 允许玩家在离开平台的一小段时间内（coyoteTime）仍然可以起跳，优化手感
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // 4. Jump Buffer Logic (跳跃预输入)
        // 允许玩家在落地前提前按下跳跃键（jumpBufferTime），落地瞬间自动起跳
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // 5. Execute Jump
        if (coyoteTimeCounter > 0f && jumpBufferCounter > 0f && !isWallJumping)
        {
            // 单向平台下落检测
            if(Input.GetAxisRaw("Vertical") < 0 && currentPlatformCollider != null)
            {
                StartCoroutine(DisableCollision());
            }
            else 
            {
                // 普通跳跃
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            }
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }

        // 6. Variable Jump Height (小跳/大跳)
        // 松开按键时减速，实现按得越久跳得越高
        // 修复：增加 !isDashing 防止冲刺跳跃时意外减速
        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0f && !isWallJumping && !isDashing)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
        }

        // 7. Dash Input
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            StartCoroutine(Dash());
        }

        // 8. Attack Input
        // 优化：使用 "Fire1" 代替硬编码的 KeyCode.J，支持自定义键位
        if (Input.GetButtonDown("Fire1"))
        {
            TryAttack();
        }

        // 9. Flip Character
        if (moveInput > 0) transform.localScale = new Vector3(1, 1, 1);
        else if (moveInput < 0) transform.localScale = new Vector3(-1, 1, 1);
        //
    }

    void WallJump()
    {
        isWallJumping = true;
        
        Invoke("StopWallJumping", wallJumpTime);
        
        // 计算蹬墙方向：如果面向右(1)，则向左(-1)蹬
        int wallDir = transform.localScale.x > 0 ? 1 : -1;
        
        // 施加力：X轴反向弹射，Y轴向上
        rb.velocity = new Vector2(-wallDir * wallJumpForce.x, wallJumpForce.y);
        
        // Prevent accidental double jump (Coyote/Buffer)
        // 清空跳跃缓存，防止蹬墙跳后立即触发普通二段跳
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;

        // Flip character immediately to face away from wall
        // 立即转身，方便接续后续操作
        transform.localScale = new Vector3(-wallDir, 1, 1);
    }

    void StopWallJumping()
    {
        isWallJumping = false;
    }

    void FixedUpdate()
    {
        if (isHurting) return; // 受击硬直期间禁止移动
        if (isDashing) return;
        if (isWallJumping) return; // Lock movement while wall jumping

        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
    }

    public void TakeDamage(int amount,Vector3 sourcePosition, float knockbackForce)
    {
        if (isHurting) return; // 防止连续受击

        currentHealth -= amount;
        // Update Health Bar
        if(UIManager.instance != null)
        {
            UIManager.instance.UpdateHealthBar(currentHealth,maxHealth);
        }
        
        // 1. Knockback Logic
        if (rb != null)
        {
            // 进入受击状态，锁定移动
            StartCoroutine(KnockbackRoutine(knockbackForce));
            
            Vector2 direction = (transform.position - sourcePosition).normalized;
            // 击退方向稍微向上一点，防止贴地摩擦过大
            Vector2 force = direction * knockbackForce + Vector2.up * (knockbackForce * 0.5f);
            
            rb.velocity = Vector2.zero;
            rb.AddForce(force, ForceMode2D.Impulse);
        }

        // 2. Visual Feedback
        StartCoroutine(FlashEffect());

        if(impulseSource != null)
        {
            // 强制给一个随机方向的力，确保一定会震动
            Vector3 shakeVelocity = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), 0) * 0.5f;
            impulseSource.GenerateImpulse(shakeVelocity);
        }
        if(hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab,transform.position,Quaternion.identity);
        }

        if (currentHealth <= 0) Die();
    }
    //
    System.Collections.IEnumerator FlashEffect()
    {
        if(sr != null)
        {
            Color original = sr.color;
            sr.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            sr.color = original;
        }
    }
    void Die()
    {
        Debug.Log("Game Over!");
        Time.timeScale = 0f;
    }

    void TryAttack()
    {
        if(currentWeapon == null) return;
        if(Time.time >= nextAttackTime)
        {
            // 播放攻击动画
            if(weaponAnimator != null)
            {
                // 性能优化：使用 Hash ID
                weaponAnimator.SetTrigger(AttackAnimID);
            }
            currentWeapon.Attack(this);
            nextAttackTime = Time.time + currentWeapon.cooldown;
        }
    }

    // 受击硬直协程
    IEnumerator KnockbackRoutine(float force)
    {
        isHurting = true;
        // 根据受击力度动态调整硬直时间，或者固定一个时间（例如 0.2s）
        yield return new WaitForSeconds(0.2f);
        isHurting = false;
    }

    // 持续检测脚下的单向平台
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "OneWayPlatform")
        {
            currentPlatformCollider = collision.collider;
        }
    }
    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "OneWayPlatform")
        {
            currentPlatformCollider = collision.collider;
        }
    }
    void OnCollisionExit2D(Collision2D collision)
    {
        // 只有当离开的平台是当前记录的平台时，才置空
        if (collision.gameObject.tag == "OneWayPlatform" && collision.collider == currentPlatformCollider)
        {
            currentPlatformCollider = null;
        }
    }

    IEnumerator DisableCollision()
    {
        // 安全检查：如果当前没有平台，直接退出
        if (currentPlatformCollider == null) yield break;

        // 锁定该次操作针对的平台（防止协程中途 currentPlatformCollider 发生变化）
        Collider2D targetPlatform = currentPlatformCollider;

        isFallingThroughPlatform = true; // 开始穿墙

        Physics2D.IgnoreCollision(playerCollider, targetPlatform, true);
        
        // 施加向下的初始速度
        rb.velocity = new Vector2(rb.velocity.x, platformFallForce);

        yield return new WaitForSeconds(platformFallTime);
        
        // 恢复碰撞前再次检查对象是否存在
        if (targetPlatform != null)
        {
            Physics2D.IgnoreCollision(playerCollider, targetPlatform, false);
        }

        isFallingThroughPlatform = false; // 结束穿墙
    }

    IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f; // Disable gravity during dash
        
        // Dash direction based on facing
        float dashDir = transform.localScale.x; 
        rb.velocity = new Vector2(dashDir * dashSpeed, 0f);

        if (tr != null) tr.emitting = true;

        yield return new WaitForSeconds(dashTime);

        if (tr != null) tr.emitting = false;
        rb.gravityScale = originalGravity;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    void OnDrawGizmos()
    {
        if(attackPoint != null && currentWeapon != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(attackPoint.position, currentWeapon.attackRange);
        }
        Gizmos.color = Color.red;
        if (feetPos != null)
            Gizmos.DrawWireSphere(feetPos.position, checkRadius);
    }
}
