using UnityEngine;

public class PlayerFallState : PlayerInAirState
{
    public PlayerFallState(PlayerController player, PlayerStateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // Coyote Time Jump
        if (Input.GetButtonDown("Jump") && player.CoyoteTimeCounter > 0)
        {
            stateMachine.ChangeState(player.JumpState);
        }
    }
}
