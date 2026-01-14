using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{ 
    [Header("参数设置")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("手感优化")]
    //预输入
    public float jumpBufferTime = 0.2f;
    //允许玩家在离地面一段时间后仍然可以跳跃（土狼时间）
    public float coyoteTime = 0.1f;

    [Header("地面检测")]
    public Transform feetPos;
    public float checkRadius = 0.3f;
    public LayerMask ground;

    [Header("战斗系统")]
    public Transform attackPoint;
    public WeaponData currentWeapon;//当前武器
    private float nextAttackTime = 0f;//下次攻击时间


    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;

    //计时器变量
    private float jumpBufferCounter;//预输入计时器
    private float coyoteTimeCounter;//土狼时间计时器

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        //获取输入
        moveInput = Input.GetAxis("Horizontal");
        //地面检测,
        isGrounded = Physics2D.OverlapCircle(feetPos.position, checkRadius, ground);//在feetPos位置画一个半径为checkRadius的圆圈，检测是否与ground图层有碰撞

        //手感优化：预输入
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;//在地面上时，重置土狼时间计时器
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;//不在地面上时，倒计时开始
        }
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;//按下跳跃键时，重置预输入计时器
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;//未按下跳跃键时，倒计时开始
        }
        //执行跳跃
        if(jumpBufferCounter >0 && coyoteTimeCounter >0)
        {
            Jump();
            jumpBufferCounter = 0;//跳跃后重置预输入计时器，防止连续跳跃
        }
        //攻击输入
        if (Input.GetButtonDown("Fire1"))
        {
            TryAttack();
        }
    }

    void TryAttack()
    {
        if(currentWeapon == null) return;
        if(Time.time > nextAttackTime)
        {
            //调用武器的攻击方法，传入玩家自身作为持有者
            currentWeapon.Attack(this);
            //更新下次攻击时间
            nextAttackTime = Time.time + currentWeapon.cooldown;

        }
    }
    void FixedUpdate()//物理相关操作放在FixedUpdate中
    {

        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
    }
    void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        coyoteTimeCounter = 0;//跳跃后重置土狼时间计时器，防止连续跳跃
    }

    //调试
    void OnDrawGizmos()
    {
    if(attackPoint != null && currentWeapon != null)
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(attackPoint.position, currentWeapon.attackRange);
    }
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(feetPos.position, checkRadius);
    }
}
