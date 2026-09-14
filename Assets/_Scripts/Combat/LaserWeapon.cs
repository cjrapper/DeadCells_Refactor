using UnityEngine;

using DeadCells.Core;
using DeadCells.Data;
using DeadCells.Player;

namespace DeadCells.Combat
{
    /// <summary>
    /// 激光武器 —— 持续输出型（channel）：
    /// 按住攻击键：每 FireInterval 秒发射一次激光（Raycast 命中即伤），保持发射动画；
    /// 松开攻击键：进入后摇（recovery），播结束动画，后摇结束前不可再攻击。
    /// 武器数据全配置化（伤害/射程/冷却/节奏），加新武器不改代码。
    /// </summary>
    [CreateAssetMenu(fileName = "New Laser Weapon", menuName = "Combat/Laser Weapon")]
    public class LaserWeapon : WeaponData
    {
        [Header("Laser Settings")]
        public float laserRange = 6f;      // 激光长度
        public LayerMask targetLayer;      // 命中层（Enemy + Ground：打墙就停）

        [Header("Channel 节奏")]
        [Tooltip("按住期间每次发射间隔（秒），小 = 连发快")]
        public float fireInterval = 0.15f;
        [Tooltip("松开攻击键后的后摇时长（秒）")]
        public float recoveryTime = 0.3f;
        [Tooltip("激光无击退（低伤害高频定位，不打断敌人）")]
        public bool useKnockback = false;

        [Header("动画（素材的远程攻击动作，Animator 状态名）")]
        [Tooltip("持续发射时的动画状态名（播放后保持循环/定格）")]
        public string channelAnimName = "Attack_Ranged";
        [Tooltip("后摇结束动画状态名（松开后播一次）")]
        public string recoveryAnimName = "Attack_Ranged_End";

        // ============ 持续攻击接口（WeaponData 虚属性覆写）============

        public override bool IsChannelAttack => true;
        public override float FireInterval => fireInterval;
        public override float RecoveryTime => recoveryTime;
        public override string ChannelAnimName => channelAnimName;
        public override string RecoveryAnimName => recoveryAnimName;

        public override void PlayRecoveryAnim(PlayerController holder)
        {
            // 走安全播放入口（状态名不存在时静默跳过，不报 GotoState 错）
            if (!string.IsNullOrEmpty(recoveryAnimName))
                holder.PlayAnim(recoveryAnimName, 0.05f);
        }

        public override void Attack(PlayerController holder)
        {
            if (holder == null) return;
            // 注：发射动画由攻击状态机在进入时播放（attack.lua Enter 播一次），
            // 这里只负责伤害/特效——动画职责不在武器层（之前每帧 CrossFade 导致延迟）

            // 发射点：优先用当前武器的攻击原点（枪口做进外观预制体后会跟着武器动），
            // 没有就用玩家中心
            Transform origin = holder.CurrentAttackOrigin != null
                ? holder.CurrentAttackOrigin
                : holder.transform;

            // 发射方向：面朝方向（scale.x 正右负左）
            float facing = holder.transform.localScale.x >= 0f ? 1f : -1f;
            Vector2 from = origin.position;
            Vector2 dir = new Vector2(facing, 0f);

            // 射线命中（只打目标层：敌人/地面）
            RaycastHit2D hit = Physics2D.Raycast(from, dir, laserRange, targetLayer);

            // 视觉：激光线从发射点画到命中点（LaserEffect 负责显隐）
            float endX = hit ? hit.point.x : from.x + dir.x * laserRange;
            LaserEffect fx = holder.GetComponent<LaserEffect>();
            fx?.ShowLaser(from, new Vector2(endX, from.y));

            // 发射音（高频武器：音高抖动小一点，不然连发会像警笛）
            AudioManager.PlayAt(SfxId.LaserFire, from);

            // 伤害（命中敌人 → IDamageable；激光无击退，低伤害高频定位）
            if (hit)
            {
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage, holder.transform.position,
                        useKnockback ? knockbackForce : 0f);
                    // 高频轻顿帧 + 轻震动（比近战短，防"卡顿感"）
                    Feedback.Hit(hit.point, hitStop: 0.02f, shake: 0.12f);
                }
            }
        }
    }
}
