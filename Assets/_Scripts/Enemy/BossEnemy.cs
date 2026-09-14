using UnityEngine;

using DeadCells.Core;

namespace DeadCells.Enemy
{
    /// <summary>
    /// Boss 敌人 —— 在史莱姆基础上加"死亡广播"：
    /// 死亡时广播 BossDefeated 事件，胜利结算（UI/门/音效等模块订阅，互不引用）。
    /// </summary>
    public class BossEnemy : SlimeEnemy
    {
        protected override void Die()
        {
            // 先广播再销毁（Destroy 是帧末执行，广播时机安全）
            EventCenter.Instance?.Broadcast(EventCenter.EventType.BossDefeated.ToString());
            Debug.Log("[BossEnemy] Boss 已被击败");
            base.Die();
        }
    }
}
