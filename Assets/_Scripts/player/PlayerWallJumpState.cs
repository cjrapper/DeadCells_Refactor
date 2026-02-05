using UnityEngine;

public class PlayerWallJumpState : PlayerAbilityState
{
    public PlayerWallJumpState(PlayerController player, PlayerStateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (player.Effects != null) player.Effects.SetEffectActive(true);

        // Check which side the wall is on
        // If we are touching wall, we jump AWAY from it
        int wallDir = player.transform.localScale.x > 0 ? 1 : -1;
        
        // Apply force: X-axis opposite to wall, Y-axis upward
        player.RB.velocity = new Vector2(-wallDir * player.wallJumpForce.x, player.wallJumpForce.y);
        
        // Flip character immediately to face away from wall
        player.transform.localScale = new Vector3(-wallDir, 1, 1);
        
        // Clear jump counters
        player.SetJumpBuffer(0);
        player.SetCoyoteTime(0);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (Time.time >= startTime + player.wallJumpTime)
        {
            isAbilityDone = true;
        }
    }

    public override void Exit()
    {
        base.Exit();
        if (player.Effects != null) player.Effects.SetEffectActive(false);
    }
}
