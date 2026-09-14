using System.Collections;
using UnityEngine;
using Cinemachine;
using XLua;

using DeadCells.Core;
using DeadCells.Data;
using DeadCells.Save;

namespace DeadCells.Player
{
    /// <summary>
    /// 玩家生命/受击组件 —— 处理血量、受伤、死亡、击退、闪红、屏幕震动。
    /// </summary>
    [LuaCallCSharp]
    public class PlayerHealth : MonoBehaviour, IDamageable,ISaveable
    {
        [Header("Health")]
        public int maxHealth = 100;
        public int CurrentHealth { get; private set; }

        [Header("VFX")]
        public GameObject hitEffectPrefab;
        public CinemachineImpulseSource impulseSource;

        /// <summary>是否处于受击硬直（驱动 Lua 的 hurt 状态：不能操作）</summary>
        public bool IsHurting { get; private set; }

        /// <summary>
        /// 是否处于受击无敌帧（i-frames：不会再被打，但已经能动）。
        /// ★ 必须和 IsHurting 分开：以前这两件事共用同一个 0.2 秒的 IsHurting，
        ///   结果硬直一结束玩家立刻又能被打——站在敌人的持续判定里（贴脸近战、
        ///   陷阱、滞留的弹幕）就会"硬直刚结束又被硬直"，表现就是一直被受击。
        /// </summary>
        public bool IsInvincible { get; private set; }

        /// <summary>是否被眩晕（Boss 冲撞命中后施加）—— 期间无法操作</summary>
        public bool IsStunned { get; private set; }

        /// <summary>
        /// 眩晕：禁用玩家操作一段时间（Boss 冲撞命中的惩罚）。
        /// 实现上只是把输入清零 —— Lua 状态机照常跑，但读到的输入全是 0，
        /// 表现就是玩家站在原地看着自己挨打。不用给状态机加新状态。
        /// </summary>
        public void Stun(float duration)
        {
            if (IsDead || duration <= 0f) return;
            if (stunRoutine != null) StopCoroutine(stunRoutine);
            stunRoutine = StartCoroutine(StunRoutine(duration));
        }

        private IEnumerator StunRoutine(float duration)
        {
            IsStunned = true;
            yield return new WaitForSeconds(duration);
            IsStunned = false;
            stunRoutine = null;
        }

        [Header("Hurt")]
        [Tooltip("受击硬直时长（秒）——玩家不能操作的时间")]
        public float hurtDuration = 0.2f;
        [Tooltip("受击后的无敌时长（秒）。必须 ≥ 硬直时长；太短会让玩家在持续判定里被连续硬直")]
        public float invincibleDuration = 1f;
        [Tooltip("打开后打印每次受击/被无敌帧挡下的伤害来源——查'谁在打我'时很有用")]
        public bool debugLogHits = false;

        /// <summary>是否已死亡（死亡动画播放期间为 true；Controller/Bridge 据此停止驱动）</summary>
        public bool IsDead { get; private set; }

        [Header("Death")]
        [Tooltip("死亡动画时长（没配 Animator 死亡状态时的兜底等待）")]
        public float deathAnimDelay = 0.9f;
        [Tooltip("死亡动画的 Animator 状态名（以你 Animator 里的实际名字为准，如 PlayerDeadthState）")]
        public string deathAnimName = "Death";
        [Tooltip("死亡动画播完后的停顿（秒）——别让结算面板紧贴着动画弹出来，留一点情绪缓冲")]
        public float deathSettleDelay = 1.5f;

        private SpriteRenderer spriteRenderer;
        private Animator animator;
        private Rigidbody2D rb;
        private Coroutine hurtRoutine;                 // 受击流程句柄：死亡时要能立刻掐断
        private Coroutine stunRoutine;                 // 眩晕计时
        private Color baseSpriteColor = Color.white;   // 精灵初始色（被闪烁改花后靠它还原）
        private static readonly WaitForSeconds FlashWait = new WaitForSeconds(0.1f);
        private static readonly WaitForSeconds BlinkWait = new WaitForSeconds(0.08f);
        private static float previousTimeScale = 1f;
        private float lastHurtTime = -99f;
        private float lastBlockedLogTime = -99f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            animator = GetComponentInChildren<Animator>();
            if (spriteRenderer != null) baseSpriteColor = spriteRenderer.color;
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(int amount, Vector3 sourcePosition, float knockbackForce)
        {
            // 无敌帧挡下：不是"忽略"，而是"根本没打中"。
            // debugLogHits 打开时能看清是谁在持续判定里反复摸你（限流，避免刷屏）
            if (IsInvincible)
            {
                if (debugLogHits && Time.time - lastBlockedLogTime > 0.5f)
                {
                    lastBlockedLogTime = Time.time;
                    Debug.Log($"[PlayerHealth] 无敌帧挡下 -{amount}（来源 {sourcePosition}）");
                }
                return;
            }
            if (IsDead) return;   // 死亡后免疫一切伤害处理（否则持续接触陷阱会反复触发受击流程，覆盖死亡动画）

            CurrentHealth -= amount;
            EventCenter.Instance?.Broadcast(
                EventCenter.EventType.PlayerHealthChange.ToString(),
                CurrentHealth, maxHealth);

            if (debugLogHits)
            {
                float gap = Time.time - lastHurtTime;
                Debug.Log($"[PlayerHealth] 受击 -{amount} → HP {CurrentHealth}/{maxHealth}；" +
                          $"来源 {sourcePosition}；距上次受击 {gap:F2}s");
            }
            lastHurtTime = Time.time;

            // 击退
            if (rb != null)
            {
                hurtRoutine = StartCoroutine(HurtRoutine());
                Vector2 dir = (transform.position - sourcePosition).normalized;
                Vector2 force = dir * knockbackForce + Vector2.up * (knockbackForce * 0.5f);
                rb.velocity = Vector2.zero;
                rb.AddForce(force, ForceMode2D.Impulse);
            }

            // 受击事件：驱动 Lua HFSM 的 ABNORMAL 层（PlayerLuaBridge 订阅）
            EventCenter.Instance?.Broadcast(EventCenter.EventType.PlayerHurt.ToString());

            // 闪红
            StartCoroutine(FlashEffect());

            // 音效 + 真正的屏幕震动。
            // ★ CinemachineImpulseSource 只有在场景里有 Cinemachine（Brain + ImpulseListener）时才有反应；
            //   关卡场景里没有 Cinemachine，所以以前那句 GenerateImpulse 调了等于没调 ——
            //   "接了震屏但打起来没感觉"就是这种静默失效。CameraShake 是零依赖版本。
            Feedback.Sound(SfxId.PlayerHurt);
            CameraShake.Shake(0.35f);

            if (impulseSource != null)
            {
                Vector3 shake = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    0) * 0.5f;
                impulseSource.GenerateImpulse(shake);
            }

            // 受击特效
            if (hitEffectPrefab != null)
            {
                GameObject effect = PoolManager.Instance?.Spawn(PoolType.HitEffect, transform.position, Quaternion.identity);
                if (effect != null)
                {
                    PoolManager.Instance?.ReturnAfterDelay(PoolType.HitEffect, effect, 1f);
                }
                else
                {
                    Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
                }
            }

            if (CurrentHealth <= 0) Die();
        }

        /// <summary>
        /// 受击流程 = 硬直（hurtDuration）+ 无敌帧（invincibleDuration 剩余部分）。
        /// 两者分开计时，是这个类里最容易被写错的地方：
        /// 合在一起的话，"硬直结束"就等于"可以再被打"，敌人贴脸时会被无限连续硬直。
        /// </summary>
        private IEnumerator HurtRoutine()
        {
            IsHurting = true;
            IsInvincible = true;

            // ① 硬直
            yield return new WaitForSeconds(hurtDuration);
            IsHurting = false;

            // ② 无敌帧剩余部分（半透明闪烁，让玩家自己看出"现在打不到我"）
            float remain = invincibleDuration - hurtDuration;
            if (remain > 0f)
                yield return BlinkRoutine(remain);

            IsInvincible = false;
        }

        /// <summary>无敌期间半透明闪烁。用固定 WaitForSeconds（不在循环里 new 对象）</summary>
        private IEnumerator BlinkRoutine(float duration)
        {
            if (spriteRenderer == null)
            {
                yield return new WaitForSeconds(duration);
                yield break;
            }

            Color orig = spriteRenderer.color;
            float elapsed = 0f;
            bool dim = false;
            while (elapsed < duration)
            {
                if (IsDead) break;   // 死了就别闪了，否则无敌闪烁会盖在死亡动画上
                dim = !dim;
                Color c = orig;
                c.a = dim ? orig.a * 0.35f : orig.a;
                spriteRenderer.color = c;
                yield return BlinkWait;
                elapsed += 0.08f;
            }
            spriteRenderer.color = orig;
        }

        private IEnumerator FlashEffect()
        {
            if (spriteRenderer != null)
            {
                Color orig = spriteRenderer.color;
                spriteRenderer.color = Color.red;
                yield return FlashWait;
                spriteRenderer.color = orig;
            }
        }

        private void Die()
        {
            if (IsDead) return;
            IsDead = true;

            // 死亡反馈：音效 + 大震动
            Feedback.Sound(SfxId.PlayerDeath);
            CameraShake.Shake(0.7f);

            // ① 立刻掐断受击流程（硬直 + 无敌闪烁）。
            //    StopCoroutine 会连同它 yield 出去的 BlinkRoutine 一起停，
            //    但协程最后一行的"还原颜色"不会执行 —— 必须自己补一次，
            //    否则尸体会停在半透明状态（StopCoroutine 的经典坑）
            if (hurtRoutine != null)
            {
                StopCoroutine(hurtRoutine);
                hurtRoutine = null;
            }
            IsHurting = false;
            IsInvincible = false;
            if (spriteRenderer != null) spriteRenderer.color = baseSpriteColor;

            // ② 广播"死亡瞬间"：PlayerLuaBridge 收到后清空 ACTIVE/ABNORMAL 层并停止驱动 Lua。
            //    必须放在下面定格之前 —— dash.Exit 会把重力还原成 fallingGravityScale，
            //    若先定格再清层，尸体会在死亡动画期间往下掉（顺序不能反）
            EventCenter.Instance?.Broadcast(EventCenter.EventType.PlayerDie.ToString());

            // ③ 死亡定格：清速度 + 关重力（动画期间不再下落/位移）
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.gravityScale = 0f;
            }

            // ④ 播死亡动画（按 Inspector 配的状态名；没配就直接进结算）
            string resolvedDeathAnim = ResolveDeathAnimName();
            bool hasDeathAnim = resolvedDeathAnim != null;
            if (hasDeathAnim)
                animator.CrossFade(resolvedDeathAnim, 0.05f);
            else                Debug.LogWarning($"[PlayerHealth] 玩家 Animator 里找不到任何死亡状态" +
                                 $"（配的是 \"{deathAnimName}\"，也试过 Death/Die/PlayerDeath/PlayerDeadthState），" +
                                 "跳过死亡动画直接结算");

            // 动画播完 → 冻结游戏 + 广播结算（"先播动画再结束游戏"）
            StartCoroutine(DeathRoutine(hasDeathAnim, resolvedDeathAnim));
        }

        /// <summary>
        /// 找出实际存在的死亡动画状态名。
        ///
        /// ★ 为什么不让玩家"自己把名字配对"：
        ///   这个字段是序列化在预制体上的，很容易被覆盖（换预制体、改 Animator 之后忘了同步），
        ///   而名字对不上的后果是【静默跳过死亡动画】+ 只打一条警告 —— 表现就是"死亡动画又不见了"，
        ///   排查起来要翻 Console。
        ///   所以这里按"配置名 → 常见名"依次试，试到就用，并把结果打出来。
        ///
        /// 顺序里 PlayerDeadthState 是拼写错误的常见状态名，但你已经用上了，所以保留。
        /// </summary>
        private static readonly string[] DeathAnimFallbacks =
            { "Death", "Die", "PlayerDeath", "PlayerDeadthState", "PlayerDeadState" };

        private string ResolveDeathAnimName()
        {
            if (animator == null) return null;

            // ① 先试 Inspector 配的
            if (!string.IsNullOrEmpty(deathAnimName)
                && animator.HasState(0, Animator.StringToHash(deathAnimName)))
            {
                return deathAnimName;
            }

            // ② 再试常见名
            foreach (string name in DeathAnimFallbacks)
            {
                if (animator.HasState(0, Animator.StringToHash(name)))
                {
                    Debug.Log($"[PlayerHealth] 死亡动画用 \"{name}\"（Inspector 里配的 " +
                              $"\"{deathAnimName}\" 在 Animator 里不存在）。" +
                              "建议把 Death Anim Name 改成这个，省一次运行时查找");
                    return name;
                }
            }

            return null;
        }

        private IEnumerator DeathRoutine(bool waitAnim, string animName)
        {
            float waited = 0f;

            if (waitAnim)
            {
                // ① 先等"死亡状态真正成为当前状态"（CrossFade 有过渡；最多等 0.3s）
                float enterTimeout = 0.3f;
                yield return null;
                while (enterTimeout > 0f
                       && animator != null
                       && !animator.GetCurrentAnimatorStateInfo(0).IsName(animName))
                {
                    enterTimeout -= Time.unscaledDeltaTime;
                    yield return null;
                }

                // ② 取该状态的 clip 真实长度，按时长等待一遍播完。
                //    注意：不能用 normalizedTime 轮询——循环动画的 normalizedTime 是"累加"的
                //    （旧状态跑久了已经 > 1），会瞬间判定"播完"导致死亡动画看不到（已踩坑）
                float clipLength = animator != null ? animator.GetCurrentAnimatorStateInfo(0).length : 0f;
                if (clipLength <= 0.05f) clipLength = deathAnimDelay;   // clip 异常时兜底
                Debug.Log($"[PlayerHealth] 死亡动画播放中：状态={animName}, clip 长度={clipLength:F2}s");

                while (waited < clipLength)
                {
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSecondsRealtime(deathAnimDelay);
                waited = deathAnimDelay;
            }

            Debug.Log($"[PlayerHealth] 死亡动画结束（用时 {waited:F2}s）");

            // ③ 演出缓冲：动画播完先静一会儿再弹结算面板。
            //    0.35s 的死亡 clip 播完立刻弹 UI 会显得很仓促——这段停顿是"演出节奏"，
            //    归 Inspector 配置（deathSettleDelay），不写死在代码里
            if (deathSettleDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(deathSettleDelay);
                waited += deathSettleDelay;
            }

            Debug.Log($"[PlayerHealth] 结算（总计 {waited:F2}s）");
            Debug.Log("Game Over!");
            if (Time.timeScale > 0f)
                previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            EventCenter.Instance?.Broadcast(EventCenter.EventType.PlayerDead.ToString());
        }

        public static void RestoreTimeScale()
        {
            Time.timeScale = previousTimeScale;
        }

        private void OnDestroy()
        {
            // 兜底：死亡暂停/顿帧期间玩家被销毁（场景重载、退出）→ 恢复时间流逝，
            // 避免新场景一进就 timeScale=0 卡死（Restart 卡死的常见来源之一）
            if (Time.timeScale == 0f)
                Time.timeScale = 1f;
        }

        /// <summary>回血（道具/药水用），不超过上限</summary>
        public void Heal(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0) return;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            EventCenter.Instance?.Broadcast(
                EventCenter.EventType.PlayerHealthChange.ToString(),
                CurrentHealth, maxHealth);
        }

        public void OnSave(SaveData data)
        {
            data.maxHealth = maxHealth;
            data.currentHealth = CurrentHealth;
        }

        public void OnLoad(SaveData data)
        {
            maxHealth = data.maxHealth;
            CurrentHealth = data.currentHealth;
            EventCenter.Instance?.Broadcast(EventCenter.EventType.PlayerHealthChange.ToString(), CurrentHealth, maxHealth);
        }
    }
}
