using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PLayerEffect : MonoBehaviour
{
   public GameObject ghostPrefab;
   private Rigidbody2D rb;
   private SpriteRenderer sr;
   public SamplePool ghostPool;
   public float ghostInterval = 0.1f; // 多久生一个
   private float ghostTimer;
    private bool isEffectActive;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        
        // 自动尝试寻找场景中的 GhostPool
        if (ghostPool == null)
        {
            var poolObj = GameObject.Find("GhostPool");
            if (poolObj == null) poolObj = GameObject.Find("GohstPool"); // Fallback for typo
            
            if (poolObj != null)
            {
                ghostPool = poolObj.GetComponent<SamplePool>();
            }
            else
            {
                Debug.LogWarning("PLayerEffect: Could not find GameObject named 'GhostPool' or 'GohstPool' in the scene.");
            }
        }
    }

    public void SetEffectActive(bool active)
    {
        isEffectActive = active;
    }

    void Update()
    {
        if (rb == null) return;

        // 触发逻辑：要么被外部状态激活（如冲刺中），要么速度极快
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
            // Try to find it again if it was added late or missed
            sr = GetComponentInChildren<SpriteRenderer>();
            if (sr == null) return;
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
