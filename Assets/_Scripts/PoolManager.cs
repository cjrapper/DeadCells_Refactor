using UnityEngine;

namespace AngryBirds.Core
{
    /// <summary>
    /// 对象池类型枚举
    /// </summary>
    public enum PoolType
    {
        JumpDust,
        LandDust,
        HitEffect,
        Ghost,
        Projectile
    }

    /// <summary>
    /// 中心化对象池管理器 —— 替代各脚本中散落的 GameObject.Find("...Pool")。
    /// 在场景中挂载到任意 GameObject，把所有 SamplePool 拖入 Inspector 即可。
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        [Header("对象池引用")]
        [SerializeField] private SamplePool jumpDustPool;
        [SerializeField] private SamplePool landDustPool;
        [SerializeField] private SamplePool hitEffectPool;
        [SerializeField] private SamplePool ghostPool;
        [SerializeField] private SamplePool projectilePool;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>从对应池中获取一个对象</summary>
        public GameObject Spawn(PoolType type)
        {
            SamplePool pool = GetPool(type);
            return pool != null ? pool.Get() : null;
        }

        /// <summary>从对应池中获取一个对象并设置位置和旋转</summary>
        public GameObject Spawn(PoolType type, Vector3 position, Quaternion rotation)
        {
            GameObject obj = Spawn(type);
            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
            }
            return obj;
        }

        /// <summary>将对象归还到对应池</summary>
        public void Return(PoolType type, GameObject obj)
        {
            SamplePool pool = GetPool(type);
            pool?.Return(obj);
        }

        /// <summary>延迟归还</summary>
        public void ReturnAfterDelay(PoolType type, GameObject obj, float delay)
        {
            StartCoroutine(DelayedReturn(type, obj, delay));
        }

        private System.Collections.IEnumerator DelayedReturn(PoolType type, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null)
                Return(type, obj);
        }

        /// <summary>获取原始的 SamplePool 引用（供 GhostEffect 等需要直接操作池子的场景使用）</summary>
        public SamplePool GetPool(PoolType type)
        {
            return type switch
            {
                PoolType.JumpDust => jumpDustPool,
                PoolType.LandDust => landDustPool,
                PoolType.HitEffect => hitEffectPool,
                PoolType.Ghost => ghostPool,
                PoolType.Projectile => projectilePool,
                _ => null,
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 在编辑器中自动查找场景中的 Pool，方便使用
            if (jumpDustPool == null) jumpDustPool = FindPoolByName("JumpDustPool");
            if (landDustPool == null) landDustPool = FindPoolByName("LandDustPool");
            if (hitEffectPool == null) hitEffectPool = FindPoolByName("HitEffectPool");
            if (ghostPool == null) ghostPool = FindPoolByName("GhostPool");
            if (projectilePool == null) projectilePool = FindPoolByName("ProjectilePool");
        }

        private SamplePool FindPoolByName(string name)
        {
            var go = GameObject.Find(name);
            return go != null ? go.GetComponent<SamplePool>() : null;
        }
#endif
    }
}
