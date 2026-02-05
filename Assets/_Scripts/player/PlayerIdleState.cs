using UnityEngine;

public class PlayerIdleState : PlayerGroundedState
{
    public PlayerIdleState(PlayerController player, PlayerStateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        SetXVelocity(0);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (player.MoveInput != 0)
        {
            stateMachine.ChangeState(player.MoveState);
        }
    }
}
