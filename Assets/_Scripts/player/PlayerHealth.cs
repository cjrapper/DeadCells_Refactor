using System.Collections;
using UnityEngine;
using Cinemachine;

using DeadCells.Core;
using DeadCells.Save;

namespace DeadCells.Player
{
    /// <summary>
    /// 玩家生命/受击组件 —— 处理血量、受伤、死亡、击退、闪红、屏幕震动。
    /// </summary>
    public class PlayerHealth : MonoBehaviour, IDamageable,ISaveable
    {
        [Header("Health")]
        public int maxHealth = 100;
        public int CurrentHealth { get; private set; }

        [Header("VFX")]
        public GameObject hitEffectPrefab;
        public CinemachineImpulseSource impulseSource;

        public bool IsHurting { get; private set; }

        private SpriteRenderer spriteRenderer;
        private Rigidbody2D rb;
        private static readonly WaitForSeconds FlashWait = new WaitForSeconds(0.1f);
        private static readonly WaitForSeconds KnockbackWait = new WaitForSeconds(0.2f);
        private static float previousTimeScale = 1f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(int amount, Vector3 sourcePosition, float knockbackForce)
        {
            if (IsHurting) return;

            CurrentHealth -= amount;
            EventCenter.Instance?.Broadcast(
                EventCenter.EventType.PlayerHealthChange.ToString(),
                CurrentHealth, maxHealth);

            // 击退
            if (rb != null)
            {
                StartCoroutine(KnockbackRoutine());
                Vector2 dir = (transform.position - sourcePosition).normalized;
                Vector2 force = dir * knockbackForce + Vector2.up * (knockbackForce * 0.5f);
                rb.velocity = Vector2.zero;
                rb.AddForce(force, ForceMode2D.Impulse);
            }

            // 闪红
            StartCoroutine(FlashEffect());

            // 屏幕震动
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

        private IEnumerator KnockbackRoutine()
        {
            IsHurting = true;
            yield return KnockbackWait;
            IsHurting = false;
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
