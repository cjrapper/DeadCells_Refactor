using UnityEngine;

// 剑类武器脚本
[CreateAssetMenu(fileName = "New Sword Weapon", menuName = "Combat/Sword Weapon")]
public class SwordWeapon : WeaponData
{
    [Header("特有设置")]
    public LayerMask targetLayer;

    // 攻击逻辑
    public override void Attack(PlayerController holder)
    {
        Vector3 origin = holder.attackPoint.position;
        //画圆法检测攻击范围内的目标
        Collider2D[] hitRange = Physics2D.OverlapCircleAll(origin, attackRange, targetLayer);
        //遍历所有命中的目标并造成伤害
        foreach (var enemy in hitRange)
        {
            IDamageable damageable = enemy.GetComponent<IDamageable>();
            if(damageable != null)
            {
                damageable.TakeDamage(damage);
            }
        }
        Debug.Log($"玩家使用{weaponName}击中了{hitRange.Length}个敌人");
    }
}
