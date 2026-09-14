using UnityEngine;

using DeadCells.Player;

namespace DeadCells.Combat
{
    public abstract class WeaponData : ScriptableObject
{
    [Header("Weapon Stats")]
    public string weaponName;
    public int damage = 20;
    public float cooldown = 0.5f;
    public float attackRange = 1.5f;
    public float knockbackForce = 5f; // New: Knockback power
    public bool useMeleeSwing = false; // Does this weapon use the melee swing animation?

    [Header("手持外观")]
    [Tooltip("武器的持握外观预制体，运行时实例化到玩家的 Weapon Holder 下。\n" +
             "留空 = 不显示手持外观。\n" +
             "★ 预制体的原点请放在【握把】上：近战挥砍是绕这个原点旋转的")]
    public GameObject heldVisualPrefab;

    [Tooltip("攻击原点：外观预制体里叫这个名字的子物体（枪口/剑尖）。\n" +
             "留空 = 用玩家身上的 Attack Origin（近战通常留空就行）")]
    public string attackOriginChildName = "";

    public abstract void Attack(PlayerController holder);

    // ============ 持续攻击（channel）相关：虚属性，激光等按住持续输出的武器覆写 ============

    /// <summary>是否按住持续输出（channel 武器：按住攻击键持续发射，松开进入后摇）</summary>
    public virtual bool IsChannelAttack => false;

    /// <summary>持续输出期间每次发射的间隔（秒）</summary>
    public virtual float FireInterval => 0.15f;

    /// <summary>松开攻击键后的后摇时长（秒），后摇内不可再次攻击</summary>
    public virtual float RecoveryTime => 0.3f;

    /// <summary>持续输出时的动画状态名（Animator；空 = 不播）</summary>
    public virtual string ChannelAnimName => "";

    /// <summary>后摇/结束动画状态名（Animator；空 = 不播）</summary>
    public virtual string RecoveryAnimName => "";

    /// <summary>由状态机在后摇开始时调用（播结束动画等）</summary>
    public virtual void PlayRecoveryAnim(PlayerController holder) { }
    }
}
