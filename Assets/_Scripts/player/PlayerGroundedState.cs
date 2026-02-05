using UnityEngine;

public class PlayerGroundedState : PlayerState
{
    public PlayerGroundedState(PlayerController player, PlayerStateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        player.SpawnDust(player.landDustPrefab);
    }
    
    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // One Way Platform Drop Logic (S + Jump)
        if (Input.GetKey(KeyCode.S) && Input.GetButtonDown("Jump"))
        {
            player.StartCoroutine("DisableCollision");
            return;
        }

        // Jump Logic
        if (player.JumpBufferCounter > 0 && player.CheckGrounded())
        {
            stateMachine.ChangeState(player.JumpState);
            return;
        }

        // Dash Logic
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            stateMachine.ChangeState(player.DashState);
            return;
        }

        // Attack Logic
        if (Input.GetButtonDown("Fire1"))
        {
            stateMachine.ChangeState(player.AttackState);
            return;
        }

        // Transition to Fall if not grounded
        if (!player.CheckGrounded())
        {
            stateMachine.ChangeState(player.FallState);
        }
    }
}
