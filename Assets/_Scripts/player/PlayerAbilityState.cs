using UnityEngine;

public class PlayerAbilityState : PlayerState
{
    protected bool isAbilityDone;

    public PlayerAbilityState(PlayerController player, PlayerStateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        isAbilityDone = false;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (isAbilityDone)
        {
            if (player.CheckGrounded() && player.RB.velocity.y <= 0.1f)
            {
                if (player.MoveInput == 0)
                    stateMachine.ChangeState(player.IdleState);
                else
                    stateMachine.ChangeState(player.MoveState);
            }
            else
            {
                stateMachine.ChangeState(player.FallState);
            }
        }
    }
}
