using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class PlayerController : MonoBehaviour,IDamageable
{ 
    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;
    private SpriteRenderer sr;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("Jump Feel (Coyote & Buffer)")]
    public float jumpBufferTime = 0.2f; // Buffer jump input before hitting ground
    public float coyoteTime = 0.1f;     // Allow jump shortly after leaving ground

    [Header("Ground Detection")]
    public Transform feetPos;
    public float checkRadius = 0.3f;
    public LayerMask ground;

    [Header("Combat System")]
    public Transform attackPoint;
    public WeaponData currentWeapon;
    private float nextAttackTime = 0f;

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

    // Internal Variables
    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private float jumpBufferCounter;
    private float coyoteTimeCounter;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        tr = GetComponent<TrailRenderer>(); // Ensure TrailRenderer is attached for Dash

        // Fix Wall Stick: Create zero-friction material dynamically
        PhysicsMaterial2D noFriction = new PhysicsMaterial2D("NoFriction");
        noFriction.friction = 0f;
        GetComponent<Collider2D>().sharedMaterial = noFriction;

        sr = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (isDashing) return; // Lock input while dashing

        // 1. Input Processing
        moveInput = Input.GetAxisRaw("Horizontal"); // GetAxisRaw is snappier

        // 2. Ground Check
        isGrounded = Physics2D.OverlapCircle(feetPos.position, checkRadius, ground);

        // 3. Coyote Time Logic
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // 4. Jump Buffer Logic
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // 5. Execute Jump
        if (coyoteTimeCounter > 0f && jumpBufferCounter > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }

        // 6. Variable Jump Height (holding button jumps higher)
        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
        }

        // 7. Dash Input
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            StartCoroutine(Dash());
        }

        // 8. Attack Input
        if (Input.GetKeyDown(KeyCode.J))
        {
            TryAttack();
        }

        // 9. Flip Character
        if (moveInput > 0) transform.localScale = new Vector3(1, 1, 1);
        else if (moveInput < 0) transform.localScale = new Vector3(-1, 1, 1);
    }

    void FixedUpdate()
    {
        if (isDashing) return;
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
    }

    public void TakeDamage(int amount,Vector3 sourcePosition, float knockbackForce)
    {
        currentHealth -= amount;
        // Update Health Bar
        if(UIManager.instance != null)
        {
            UIManager.instance.UpdateHealthBar(currentHealth,maxHealth);
        }
        // 1. Knockback
        if (rb != null)
        {
            Vector2 direction = (transform.position - sourcePosition).normalized;
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
            currentWeapon.Attack(this);
            nextAttackTime = Time.time + currentWeapon.cooldown;
        }
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
