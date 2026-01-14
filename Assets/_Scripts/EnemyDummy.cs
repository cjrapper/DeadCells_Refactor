using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDummy : MonoBehaviour,IDamageable
{
    [SerializeField] private int health = 100;
    private int currentHealth;

    void Start()
    {
        currentHealth = health;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log($"<color = red>{gameObject.name} ’µΩ{amount}µ„…À∫¶£° £”‡{currentHealth}</color>");
        if(currentHealth < 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("µ–»ÀÀ¿Õˆ");
        Destroy(gameObject);
    }
}
