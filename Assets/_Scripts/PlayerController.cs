using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{ 
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
