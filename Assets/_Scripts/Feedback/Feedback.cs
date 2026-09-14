using UnityEngine;

using DeadCells.Data;

namespace DeadCells.Core
{
    /// <summary>
    /// 反馈统一入口 —— 一次调用 = 顿帧 + 震屏 + 音效（+ 命中特效）。
    ///
    /// 为什么要有这一层：
    ///   命中反馈散落在近战/激光/投射物/陷阱/被击…七八个地方，
    ///   每个地方各写一遍"顿帧+震屏+音效+特效"必然会漏（比如某些武器有顿帧没震动）。
    ///   收敛成一个静态入口后，调手感只改这里一处，而且新加的武器默认就有完整反馈。
    ///
    /// 手感参数约定（都给了默认值，一般不用传）：
    ///   hitStop 近战 0.05 / 激光 0.02（高频，必须更短，否则卡顿感）
    ///   shake   轻 0.12（被打） / 中 0.25（命中敌人） / 重 0.5（Boss 落地、玩家死亡）
    /// </summary>
    public static class Feedback
    {
        /// <summary>命中反馈：顿帧 + 震屏 + 命中音 + 命中特效</summary>
        public static void Hit(Vector3 worldPos, float hitStop = 0.05f, float shake = 0.25f,
            SfxId sfx = SfxId.Hit, GameObject vfxPrefab = null)
        {
            if (hitStop > 0f) HitStop.Stop(hitStop);
            CameraShake.Shake(shake);
            AudioManager.PlayAt(sfx, worldPos);
            SpawnVfx(vfxPrefab, worldPos);
        }

        /// <summary>只有音效（UI、拾取这类不需要打击感的）</summary>
        public static void Sound(SfxId id) => AudioManager.Play(id);

        /// <summary>带位置的音效</summary>
        public static void SoundAt(SfxId id, Vector3 worldPos) => AudioManager.PlayAt(id, worldPos);

        /// <summary>只有震屏</summary>
        public static void Shake(float amount) => CameraShake.Shake(amount);

        /// <summary>
        /// 生成一次性特效：优先走对象池（和 PlayerHealth 的命中特效同一套降级逻辑），
        /// 池不可用就 Instantiate + 定时销毁，避免特效永远留在场景里
        /// </summary>
        public static void SpawnVfx(GameObject prefab, Vector3 worldPos, float life = 1f)
        {
            if (prefab == null) return;

            PoolManager pool = PoolManager.Instance;
            if (pool != null)
            {
                GameObject go = pool.Spawn(PoolType.HitEffect, worldPos, Quaternion.identity);
                if (go != null)
                {
                    pool.ReturnAfterDelay(PoolType.HitEffect, go, life);
                    return;
                }
            }

            Object.Destroy(Object.Instantiate(prefab, worldPos, Quaternion.identity), life);
        }
    }
}
