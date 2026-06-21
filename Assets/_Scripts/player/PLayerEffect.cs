using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using AngryBirds.Core;

namespace AngryBirds.Player
{
    public class PLayerEffect : MonoBehaviour
    {
        public GameObject ghostPrefab;
        public GameObject JumpDustPrefab;
        public GameObject LandDustPrefab;
        private Rigidbody2D rb;
        private SpriteRenderer sr;
        private SamplePool ghostPool;
        public float ghostInterval = 0.1f;
        private float ghostTimer;
        private bool isEffectActive;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponentInChildren<SpriteRenderer>();
        }

        /// <summary>由 PoolManager 或外部注入 ghostPool引用</summary>
        public void AssignPool(SamplePool pool)
        {
            ghostPool = pool;
        }

        public void SetEffectActive(bool active)
        {
            isEffectActive = active;
        }

        void Update()
        {
            if (rb == null) return;

            if (isEffectActive || Mathf.Abs(rb.velocity.x) > 10f || Mathf.Abs(rb.velocity.y) > 10f)
            {
                if (Time.time > ghostTimer)
                {
                    SpawnGhost();
                    ghostTimer = Time.time + ghostInterval;
                }
            }
        }

        private void SpawnGhost()
        {
            if (sr == null)
            {
                sr = GetComponentInChildren<SpriteRenderer>();
                if (sr == null) return;
            }

            // 如果还没有 pool 引用，尝试从 PoolManager 获取
            if (ghostPool == null && PoolManager.Instance != null)
            {
                ghostPool = PoolManager.Instance.GetPool(PoolType.Ghost);
            }

            if (ghostPool == null) return;

            GameObject ghost = ghostPool.Get();
            if (ghost == null)
            {
                Debug.LogWarning("PLayerEffect: GhostPool returned null. Check if Pool is full or Prefab is missing.");
                return;
            }

            GhostEffect ghostScript = ghost.GetComponent<GhostEffect>();
            if (ghostScript != null)
            {
                ghostScript.Init(
                    sr.sprite,
                    transform.position,
                    transform.rotation,
                    transform.localScale,
                    ghostPool
                );
            }
        }
    }
}
