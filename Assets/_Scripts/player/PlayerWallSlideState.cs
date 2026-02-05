using UnityEngine;

public class PlayerWallSlideState : PlayerInAirState
{
    public PlayerWallSlideState(PlayerController player, PlayerStateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // Transition to Grounded states if grounded
        if (player.CheckGrounded())
        {
            stateMachine.ChangeState(player.IdleState);
            return;
        }

        // Exit wall slide if not touching wall or moving away from it
        // Note: You can add a small "sticky" time here if needed
        if (!player.CheckTouchingWall() || player.MoveInput == -player.transform.localScale.x)
        {
            stateMachine.ChangeState(player.FallState);
            return;
        }

        // Wall Jump Logic
        if (Input.GetButtonDown("Jump"))
        {
            stateMachine.ChangeState(player.WallJumpState);
            return;
        }

        // Apply wall slide friction
        player.RB.velocity = new Vector2(player.RB.velocity.x, Mathf.Clamp(player.RB.velocity.y, -player.wallSlideSpeed, float.MaxValue));
    }
}
