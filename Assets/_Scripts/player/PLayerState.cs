using UnityEngine;

public abstract class PlayerState
{
    protected PlayerController player;
    protected PlayerStateMachine stateMachine;
    protected float startTime;
    protected string animBoolName; // The name of the animation to play

    protected bool isAnimationFinished;
    protected bool isExitingState;

    public PlayerState(PlayerController player, PlayerStateMachine stateMachine, string animBoolName)
    {
        this.player = player;
        this.stateMachine = stateMachine;
        this.animBoolName = animBoolName;
    }
    public virtual void Enter()
    {
        startTime = Time.time;
        // Directly play the animation state by name
        // CrossFade allows smooth transition without needing Animator transitions
        if(player.Anim != null)
            player.Anim.CrossFade(animBoolName, 0.1f);
            
        isAnimationFinished = false;
        isExitingState = false;
    }
    public virtual void HandleInput(){}
    public virtual void LogicUpdate(){}
    public virtual void PhysicsUpdate(){}

    public virtual void Exit()
    {
        // No need to set bool false anymore
        isExitingState = true;
    }

    // 方便设置速度
     protected void SetXVelocity(float _xVelocity)
    {
        player.RB.velocity = new Vector2(_xVelocity, player.RB.velocity.y);
    }

    protected void SetYVelocity(float _yVelocity)
    {
        player.RB.velocity = new Vector2(player.RB.velocity.x, _yVelocity);
    }
}