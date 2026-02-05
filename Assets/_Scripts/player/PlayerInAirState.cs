using UnityEngine;

public class PlayerInAirState : PlayerState
{
    private bool isGrounded;
    private int xInput;

    public PlayerInAirState(PlayerController player, PlayerStateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // Check for ground to transition to Idle/Move
        if (player.CheckGrounded() && player.RB.velocity.y <= 0.1f)
        {
            if (player.MoveInput == 0)
                stateMachine.ChangeState(player.IdleState);
            else
                stateMachine.ChangeState(player.MoveState);
            return;
        }

        // Dash Logic (Air Dash)
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            stateMachine.ChangeState(player.DashState);
            return;
        }

        // Wall Sliding Logic
        if (player.CheckTouchingWall() && player.RB.velocity.y < 0)
        {
            stateMachine.ChangeState(player.WallSlideState);
            return;
        }

        // Wall Jump Logic
        if (Input.GetButtonDown("Jump") && player.CheckTouchingWall() && !player.CheckGrounded())
        {
            stateMachine.ChangeState(player.WallJumpState);
            return;
        }

        // Jump Height Variation
        if (Input.GetButtonUp("Jump") && player.RB.velocity.y > 0)
        {
            SetYVelocity(player.RB.velocity.y * 0.5f);
        }

        // Air Attack Logic
        if (Input.GetButtonDown("Fire1") && player.CanAttack())
        {
            stateMachine.ChangeState(player.AttackState);
            return;
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // 1. Variable Gravity
        if (player.RB.velocity.y > 0)
        {
            player.RB.gravityScale = player.jumpGravityScale;
        }
        else
        {
            player.RB.gravityScale = player.fallGravityScale;
        }

        // 2. Horizontal movement in air
        float targetVelocityX = player.MoveInput * player.moveSpeed;
        float acceleration = 25f; // Air acceleration
        float newX = Mathf.MoveTowards(player.RB.velocity.x, targetVelocityX, acceleration * Time.fixedDeltaTime);
        SetXVelocity(newX);
    }

    public override void Exit()
    {
        base.Exit();
        // Reset gravity scale when leaving air state
        player.RB.gravityScale = player.jumpGravityScale;
    }
}
