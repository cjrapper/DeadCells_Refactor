using UnityEngine;

public abstract class WeaponData : ScriptableObject
{
    [Header("Weapon Stats")]
    public string weaponName;
    public int damage = 20;
    public float cooldown = 0.5f;
    public float attackRange = 1.5f;
    public float knockbackForce = 5f; // New: Knockback power

    public abstract void Attack(PlayerController holder);
}
