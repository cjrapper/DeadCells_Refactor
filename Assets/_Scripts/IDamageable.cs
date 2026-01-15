using UnityEngine;

public interface IDamageable
{
    // Updated to include source position and knockback force for combat feel
    void TakeDamage(int amount, Vector3 sourcePosition, float knockbackForce);
}
