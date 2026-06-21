using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using AngryBirds.Combat;

namespace AngryBirds.Player
{
    /// <summary>
    /// 玩家战斗组件 —— 武器管理、攻击判定、挥剑动画。
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Weapons")]
        public List<WeaponData> weaponInventory;
        public WeaponData currentWeapon;
        private int currentWeaponIndex;

        [Header("Swing Animation")]
        public AnimationCurve swingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public float swingDuration = 0.25f;
        public float maxSwingAngle = 120f;

        [Header("Attack Transforms")]
        public Transform attackOrigin;
        public Transform weaponPivot;

        // 运行时
        private float nextAttackTime;
        private Coroutine swingCoroutine;

        private PlayerController playerController;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            if (weaponPivot == null && attackOrigin != null)
                weaponPivot = attackOrigin;

            if (weaponInventory != null && weaponInventory.Count > 0)
            {
                currentWeapon = weaponInventory[0];
                currentWeaponIndex = 0;
            }
        }

        public void SwitchWeapon()
        {
            if (weaponInventory == null || weaponInventory.Count == 0) return;
            currentWeaponIndex = (currentWeaponIndex + 1) % weaponInventory.Count;
            currentWeapon = weaponInventory[currentWeaponIndex];
            Debug.Log($"Switched to weapon: {currentWeapon.name}");
        }

        public bool CanAttack()
        {
            return currentWeapon != null && Time.time >= nextAttackTime;
        }

        public void Attack()
        {
            if (currentWeapon == null) return;
            currentWeapon.Attack(playerController);
            nextAttackTime = Time.time + currentWeapon.cooldown;

            if (currentWeapon.useMeleeSwing)
                StartSwing();
        }

        private void StartSwing()
        {
            if (swingCoroutine != null) StopCoroutine(swingCoroutine);
            swingCoroutine = StartCoroutine(PlaySwingCurve());
        }

        private IEnumerator PlaySwingCurve()
        {
            if (weaponPivot == null || swingDuration <= 0f || swingCurve == null)
            {
                swingCoroutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < swingDuration)
            {
                float progress = elapsed / swingDuration;
                float curveValue = swingCurve.Evaluate(progress);
                weaponPivot.localRotation = Quaternion.Euler(0f, 0f, -curveValue * maxSwingAngle);
                elapsed += Time.deltaTime;
                yield return null;
            }

            weaponPivot.localRotation = Quaternion.identity;
            swingCoroutine = null;
        }

        private void OnDrawGizmos()
        {
            if (attackOrigin != null && currentWeapon != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(attackOrigin.position, currentWeapon.attackRange);
            }
        }
    }
}
