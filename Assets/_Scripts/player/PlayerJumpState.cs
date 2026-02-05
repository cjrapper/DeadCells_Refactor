using UnityEngine;

public class PlayerJumpState : PlayerInAirState
{
    private int amountOfJumpsLeft;

    public PlayerJumpState(PlayerController player, PlayerStateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
        amountOfJumpsLeft = player.amountOfJumps;
    }

    public override void Enter()
    {
        base.Enter();
        player.SpawnDust(player.jumpDustPrefab);
        SetYVelocity(player.jumpForce);
        player.SetJumpBuffer(0);
        player.SetCoyoteTime(0);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (player.RB.velocity.y <= 0)
        {
            stateMachine.ChangeState(player.FallState);
        }
    }
}
