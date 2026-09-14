using UnityEngine;

namespace DeadCells.Enemy
{
    public class HurtState : EnemyState
{
    public override string AnimName => "Hurt";
    private float hurtTimer;
    private float hurtDuration = 0.5f;   // 0.3 → 0.5：受击硬直更长，玩家连击时敌人明显"被打断"

    public HurtState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

    public override void Enter()
    {
        hurtTimer = hurtDuration;
        enemy.UpdateVisuals();
    }

    public override void LogicUpdate()
    {
        hurtTimer -= Time.deltaTime;

        if (hurtTimer <= 0)
        {
            // 回归状态走虚方法：地面敌人回 chase，飞行敌人回飞行轨道（框架扩展点）
            stateMachine.ChangeState(enemy.GetPostHurtState());
        }
    }
}}
