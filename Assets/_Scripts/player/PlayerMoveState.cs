using UnityEngine;

public class PlayerMoveState : PlayerGroundedState
{
    public PlayerMoveState(PlayerController player, PlayerStateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (player.MoveInput == 0)
        {
            stateMachine.ChangeState(player.IdleState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        float targetVelocityX = player.MoveInput * player.moveSpeed;
        float acceleration = 50f; // Ground acceleration
        float newX = Mathf.MoveTowards(player.RB.velocity.x, targetVelocityX, acceleration * Time.fixedDeltaTime);
        SetXVelocity(newX);
    }
}
