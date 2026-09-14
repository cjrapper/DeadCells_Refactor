using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XLua;

using DeadCells.Combat;
using DeadCells.Core;
using DeadCells.Data;
using DeadCells.Save;

namespace DeadCells.Player
{
    /// <summary>
    /// 玩家战斗组件 —— 武器管理、攻击判定、挥剑动画。
    /// </summary>
    [LuaCallCSharp]
    public class PlayerCombat : MonoBehaviour,ISaveable
    {
        [Header("Weapons")]
        public List<WeaponData> weaponInventory;
        public WeaponData currentWeapon;
        private int currentWeaponIndex;

        [Header("Swing Animation")]
        public AnimationCurve swingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public float swingDuration = 0.25f;
        public float maxSwingAngle = 120f;
        [Tooltip("挥完武器再显示多久（秒）才隐藏；0 = 挥完立即隐藏")]
        public float weaponShowAfterSwing = 3f;

        [Header("Attack Transforms")]
        public Transform attackOrigin;
        public Transform weaponPivot;
        [Tooltip("手持外观的挂载点（每把武器的外观预制体实例化到这里）。\n" +
                 "建议拖 WeaponHolder；留空则退化用 weaponPivot")]
        public Transform weaponHolder;

        // 运行时
        private float nextAttackTime;
        private Coroutine swingCoroutine;
        private Coroutine hideWeaponRoutine;   // 挥完延迟隐藏武器的协程
        private GameObject heldVisual;         // 当前武器外观预制体的实例
        private WeaponHitbox hitbox;           // 当前武器外观上的碰撞盒（有就用它判定命中）
        private Quaternion visualRestRotation = Quaternion.identity;   // 外观的"静止朝向"

        /// <summary>当前武器是否用外观上的碰撞盒判定命中（有的话武器脚本就不再做圆形判定）</summary>
        public bool HasHitbox => hitbox != null;

        /// <summary>
        /// 当前该被"显隐 / 挥动"的那个根节点：
        ///   配了 heldVisualPrefab → 实例化出来的预制体
        ///   没配 → 退回旧的 weaponPivot（兼容原来挂在 SwordPivot 上的那套）
        /// </summary>
        public Transform VisualRoot => heldVisual != null ? heldVisual.transform : weaponPivot;

        /// <summary>
        /// 当前武器的攻击原点：
        ///   外观预制体里配了 attackOriginChildName → 用那个子物体（枪口会跟着枪一起动）
        ///   否则 → 玩家身上固定的 attackOrigin
        /// </summary>
        public Transform CurrentAttackOrigin
        {
            get
            {
                if (heldVisual != null && currentWeapon != null
                    && !string.IsNullOrEmpty(currentWeapon.attackOriginChildName))
                {
                    Transform t = heldVisual.transform.Find(currentWeapon.attackOriginChildName);
                    if (t != null) return t;
                }
                return attackOrigin;
            }
        }

        /// <summary>
        /// 是否正在攻击（attack 状态设置）—— PlayerController 用它禁用攻击中的武器切换，
        /// 防止"激光攻击中切近战，旧动画残留"这类状态错乱
        /// </summary>
        public bool IsAttacking { get; set; }

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

        private void Start()
        {
            // 放在 Start 而不是 Awake：PlayerController.Awake 才把 weaponHolder / 武器列表注入进来，
            // 两个组件的 Awake 顺序是不保证的
            RefreshHeldVisual();
        }

        /// <summary>
        /// 按当前武器刷新手持外观：销毁上一个外观，实例化 currentWeapon.heldVisualPrefab。
        ///
        /// 为什么"每把武器一个外观预制体"而不是共用一套贴图：
        ///   剑的枢轴在手柄、枪的枢轴在握把 + 枪口位置也不同，
        ///   共用一套必然对不齐（而且枪不可能跟着剑一起挥）。
        ///   做成预制体后，握把位置/朝向/挂点全都由武器自己决定。
        /// </summary>
        public void RefreshHeldVisual()
        {
            if (heldVisual != null)
            {
                Destroy(heldVisual);
                heldVisual = null;
            }

            // ★ 切武器时必须掐掉上一把武器留下的"延迟隐藏"计时器。
            //   否则：拿剑挥一下 → 切到枪 → 3 秒后那个计时器把【枪】也隐藏掉
            //   （表现就是"切到枪之后过一会儿枪自己消失了"，和攻击没关系）
            if (hideWeaponRoutine != null)
            {
                StopCoroutine(hideWeaponRoutine);
                hideWeaponRoutine = null;
            }

            Transform holder = weaponHolder != null ? weaponHolder : weaponPivot;
            if (holder == null) return;

            GameObject prefab = currentWeapon != null ? currentWeapon.heldVisualPrefab : null;

            if (prefab != null)
            {
                // ★ 第三个参数 false = 保留预制体自己的 localPosition/localRotation/localScale。
                //   不加这个参数（或写成 Instantiate(prefab, holder)）会按"保持世界坐标"实例化，
                //   预制体被摆到错误的位置；而如果实例化后再手动写 localPosition/localRotation，
                //   又会把你在预制体里调好的握把偏移和角度【清成 0】——
                //   这一类"代码常量覆盖预制体配置"的 bug 已经踩过三次了（子弹朝向、预警圈角度、这次）。
                heldVisual = Instantiate(prefab, holder, false);
                heldVisual.name = $"Held_{currentWeapon.weaponName}";

                // 旧的挂点（SwordPivot 上原来那把）关掉，否则两把武器会叠在一起
                if (weaponPivot != null && weaponPivot != holder)
                    weaponPivot.gameObject.SetActive(false);

                // 记下外观的静止朝向：挥砍是"在这个朝向基础上加旋转"，
                // 而不是直接写绝对角度（否则又会把预制体的角度覆盖掉）
                visualRestRotation = heldVisual.transform.localRotation;

                // 外观上挂了碰撞盒 → 命中判定交给它，武器脚本不再做圆形判定
                hitbox = heldVisual.GetComponentInChildren<WeaponHitbox>(true);
            }
            else if (weaponPivot != null && weaponPivot != holder)
            {
                hitbox = null;
                weaponPivot.gameObject.SetActive(true);   // 退回旧结构
                visualRestRotation = weaponPivot.localRotation;
            }
            else
            {
                // 没配外观预制体：退回旧的 weaponPivot（可能它本身就是 holder）
                hitbox = null;
                visualRestRotation = weaponPivot != null ? weaponPivot.localRotation : Quaternion.identity;
            }

            // 显隐策略：
            //   远程（枪/激光 useMeleeSwing=false）→ 拿在手上一直显示
            //   近战（useMeleeSwing=true）        → 实例化后【先收起来】，挥砍瞬间才亮出来
            //   ★ 这一句不能省：Instantiate 出来的物体默认是激活的，
            //     不主动关掉近战武器就会一直挂在手上走路
            Transform root = VisualRoot;
            if (root != null)
                root.gameObject.SetActive(currentWeapon != null && !currentWeapon.useMeleeSwing);
        }

        public void SwitchWeapon()
        {
            if (weaponInventory == null || weaponInventory.Count == 0) return;
            currentWeaponIndex = (currentWeaponIndex + 1) % weaponInventory.Count;
            currentWeapon = weaponInventory[currentWeaponIndex];
            Debug.Log($"Switched to weapon: {currentWeapon.name}");

            RefreshHeldVisual();

            // 广播武器变化（背包 UI 订阅刷新高亮）
            EventCenter.Instance?.Broadcast(EventCenter.EventType.WeaponChange.ToString(), currentWeaponIndex);
        }

        /// <summary>商店购买武器入库（去重）</summary>
        public void AddWeapon(WeaponData weapon)
        {
            if (weapon == null) return;
            if (weaponInventory.Contains(weapon))
            {
                Debug.Log($"[PlayerCombat] 已拥有武器 {weapon.name}");
                return;
            }
            weaponInventory.Add(weapon);
            EventCenter.Instance?.Broadcast(EventCenter.EventType.WeaponChange.ToString(), currentWeaponIndex);
        }

        /// <summary>当前武器下标（背包 UI 高亮用）</summary>
        public int CurrentIndex => currentWeaponIndex;

        /// <summary>按下标选择武器（背包 UI 点击切换）</summary>
        public void SelectWeapon(int index)
        {
            if (weaponInventory == null || weaponInventory.Count == 0) return;
            if (index < 0 || index >= weaponInventory.Count) return;
            currentWeaponIndex = index;
            currentWeapon = weaponInventory[index];

            RefreshHeldVisual();

            EventCenter.Instance?.Broadcast(EventCenter.EventType.WeaponChange.ToString(), currentWeaponIndex);
        }

        public bool CanAttack()
        {
            return currentWeapon != null && Time.time >= nextAttackTime;
        }

        public void Attack()
        {
            if (currentWeapon == null) return;

            // 近战挥砍音：在这里统一播 —— 空挥也要有声音（否则"打了没感觉"），
            // 而且不管用圆形判定还是碰撞盒都响。远程武器音色不同，各自在自己实现里播。
            if (currentWeapon.useMeleeSwing)
                Feedback.SoundAt(SfxId.AttackSwing, transform.position);

            currentWeapon.Attack(playerController);

            // channel 型武器（激光）：发射节奏由状态机 FireInterval 控制，
            // 不设 cooldown（否则 0.8s 冷却会把 0.1s 一发的高频激光卡死）
            if (!currentWeapon.IsChannelAttack)
                nextAttackTime = Time.time + currentWeapon.cooldown;

            if (currentWeapon.useMeleeSwing)
            {
                // 近战：攻击瞬间显示武器（平时隐藏，走路不别扭）
                Transform root = VisualRoot;
                if (root != null)
                {
                    root.gameObject.SetActive(true);
                    if (hideWeaponRoutine != null)   // 取消上一次的隐藏计时（连击时别提前隐藏）
                    {
                        StopCoroutine(hideWeaponRoutine);
                        hideWeaponRoutine = null;
                    }
                }

                // 有碰撞盒 → 打开它，这一刀的伤害由它扫到谁算谁（所见即所得）
                if (hitbox != null)
                    hitbox.BeginSwing(currentWeapon.damage, currentWeapon.knockbackForce);

                StartSwing();
            }
        }

        private void StartSwing()
        {
            if (swingCoroutine != null) StopCoroutine(swingCoroutine);
            swingCoroutine = StartCoroutine(PlaySwingCurve());
        }

        private IEnumerator PlaySwingCurve()
        {
            Transform root = VisualRoot;
            if (root == null || swingDuration <= 0f || swingCurve == null)
            {
                swingCoroutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < swingDuration)
            {
                float progress = elapsed / swingDuration;
                float curveValue = swingCurve.Evaluate(progress);
                // 在"静止朝向"上叠加旋转（不是写绝对角度）——否则预制体里调好的剑的角度会被覆盖成 0
                root.localRotation = visualRestRotation * Quaternion.Euler(0f, 0f, -curveValue * maxSwingAngle);
                elapsed += Time.deltaTime;
                yield return null;
            }

            root.localRotation = visualRestRotation;

            // 挥砍结束：关掉碰撞盒（不然平时贴着敌人也会掉血）
            if (hitbox != null) hitbox.EndSwing();

            // 挥完：武器再延续显示 weaponShowAfterSwing 秒才隐藏（0 = 立即隐藏）
            if (weaponShowAfterSwing > 0f && hideWeaponRoutine == null)
                hideWeaponRoutine = StartCoroutine(HideWeaponAfterDelay(currentWeapon));

            swingCoroutine = null;
        }

        /// <summary>
        /// 挥砍结束后延迟收起武器。
        /// 传入 weapon 是为了"只收起它自己"：等待期间可能已经切到别的武器了，
        /// 不判断的话就会把新武器一起隐藏（切到枪后枪自己消失的根因）。
        /// </summary>
        private System.Collections.IEnumerator HideWeaponAfterDelay(WeaponData weapon)
        {
            yield return new WaitForSeconds(weaponShowAfterSwing);

            // 三个条件都满足才收起：还是同一把武器 + 是近战（远程武器本来就该一直拿着）
            if (currentWeapon == weapon && weapon != null && weapon.useMeleeSwing)
            {
                Transform root = VisualRoot;
                if (root != null) root.gameObject.SetActive(false);
            }

            hideWeaponRoutine = null;
        }

        private void OnDrawGizmos()
        {
            if (attackOrigin != null && currentWeapon != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(attackOrigin.position, currentWeapon.attackRange);
            }
        }

        public void OnSave(SaveData data)
        {
            data.weaponNames.Clear();
            foreach (var w in weaponInventory)
            {
                data.weaponNames.Add(w.name);
            }

            data.currentWeaponIndex = currentWeaponIndex;
        }

        public void OnLoad(SaveData data)
        {
            currentWeaponIndex = data.currentWeaponIndex;

            // 存档里没记录武器 → 保持场景默认配置，不做任何清空
            if (data.weaponNames == null || data.weaponNames.Count == 0) return;

            // 武器资源不在 Resources/Weapons 下时 Resources.Load 会失败；
            // 先完整加载成功再整体替换，避免加载一半把现有武器栏清空
            var loaded = new List<WeaponData>();
            foreach (var wName in data.weaponNames)
            {
                WeaponData wd = Resources.Load<WeaponData>($"Weapons/{wName}");
                if (wd != null) loaded.Add(wd);
                else Debug.LogWarning($"[PlayerCombat] 武器 {wName} 加载失败，请确认 Resources/Weapons 下存在");
            }

            if (loaded.Count > 0)
            {
                weaponInventory = loaded;
                if (currentWeaponIndex < weaponInventory.Count)
                    currentWeapon = weaponInventory[currentWeaponIndex];
            }
        }
    }
}
