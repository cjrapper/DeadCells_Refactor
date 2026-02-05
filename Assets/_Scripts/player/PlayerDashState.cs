using UnityEngine;

public class PlayerDashState : PlayerAbilityState
{
    private float lastDashTime = -10f; // Initialize to allow immediate dash
    private float originalGravity;

    public PlayerDashState(PlayerController player, PlayerStateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (Time.time < lastDashTime + player.dashCooldown)
        {
            isAbilityDone = true;
            return;
        }

        // Trigger animation
        if(player.Anim != null) player.Anim.SetTrigger("Dash");

        lastDashTime = Time.time;
        originalGravity = player.RB.gravityScale;
        player.RB.gravityScale = 0f;

        float dashDir = player.transform.localScale.x;
        player.RB.velocity = new Vector2(dashDir * player.dashSpeed, 0f);
        
        if (player.Effects != null) player.Effects.SetEffectActive(true);
        if (player.TR != null) player.TR.emitting = true;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (Time.time >= startTime + player.dashTime)
        {
            isAbilityDone = true;
        }
    }

    public override void Exit()
    {
        base.Exit();

        if (player.Effects != null) player.Effects.SetEffectActive(false);
        if (player.TR != null) player.TR.emitting = false;
        player.RB.gravityScale = originalGravity;
    }
}
