using System.Collections.Generic;
using UnityEngine;

using DeadCells.Core;
using DeadCells.Data;

namespace DeadCells.Combat
{
    /// <summary>
    /// 武器碰撞盒 —— 挂在武器外观预制体上（和贴图同一个物体）。
    ///
    /// 为什么用碰撞盒而不是"在攻击原点画个圆"：
    ///   圆形判定只有一个固定圆心 + 半径，剑挥出去的整条弧线打不到；
    ///   而碰撞盒是【所见即所得】—— 你在预制体里把盒子框住刀刃，挥到谁就砍到谁。
    ///   长枪、大剑这类武器尤其明显。
    ///
    /// 工作方式：
    ///   · 平时碰撞盒是【关的】—— 不然走路贴着敌人也在掉血
    ///   · 挥砍期间由 PlayerCombat 调 BeginSwing 打开、EndSwing 关闭
    ///   · 一次挥砍对同一个目标只结算一次（否则一刀会在几帧内打出好几次伤害）
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class WeaponHitbox : MonoBehaviour
    {
        [Tooltip("打谁：勾上敌人所在的层（Enemy）")]
        public LayerMask targetLayer;

        [Tooltip("要开关的碰撞盒。留空 = 用自己身上的 Collider2D\n" +
                 "（碰撞盒不建议做成子物体：2D 物理的回调只发给碰撞体所在的物体，子物体上的脚本收不到）")]
        public Collider2D hitbox;

        [Header("命中反馈")]
        public float hitStop = 0.05f;
        public float shake = 0.25f;
        public SfxId hitSfx = SfxId.Hit;

        private int damage;
        private float knockback;
        private bool swinging;

        // 一次挥砍内已经打过的目标：用 Collider2D 引用去重
        private readonly HashSet<Collider2D> alreadyHit = new HashSet<Collider2D>();

        private void Awake()
        {
            if (hitbox == null) hitbox = GetComponent<Collider2D>();
            if (hitbox != null) hitbox.enabled = false;   // 平时关着
        }

        /// <summary>挥砍开始：清空命中记录 + 打开碰撞盒（由 PlayerCombat 调用）</summary>
        public void BeginSwing(int dmg, float kb)
        {
            damage = dmg;
            knockback = kb;
            alreadyHit.Clear();
            swinging = true;
            if (hitbox != null) hitbox.enabled = true;
        }

        /// <summary>挥砍结束：关掉碰撞盒</summary>
        public void EndSwing()
        {
            swinging = false;
            if (hitbox != null) hitbox.enabled = false;
        }

        private void OnTriggerEnter2D(Collider2D other) => TryHit(other);

        // 也要 Stay：碰撞盒打开的瞬间如果敌人已经在里面了，Enter 不会触发；
        // 敌人不动、剑挥得慢的时候也会漏
        private void OnTriggerStay2D(Collider2D other) => TryHit(other);

        private void TryHit(Collider2D other)
        {
            if (!swinging || other == null) return;
            if (((1 << other.gameObject.layer) & targetLayer) == 0) return;
            if (alreadyHit.Contains(other)) return;          // 这一刀已经打过它了

            IDamageable target = other.GetComponent<IDamageable>();
            if (target == null) return;

            alreadyHit.Add(other);   // 先记录再结算：TakeDamage 里可能触发死亡/销毁
            target.TakeDamage(damage, transform.position, knockback);
            Feedback.Hit(other.bounds.center, hitStop, shake, hitSfx);
        }
    }
}
