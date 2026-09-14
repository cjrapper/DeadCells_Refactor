using System;
using UnityEngine;
using XLua;

using DeadCells.Core;

namespace DeadCells.Player
{
    /// <summary>
    /// Player → Lua HFSM 桥接组件。
    /// 挂在玩家 GameObject 上，Start 时加载 PlayerHFSM.lua 并接管状态机。
    /// 与 EnemyLuaBridge 不同：本组件把 core(PlayerController) 传给 Lua 驱动。
    /// </summary>
    [LuaCallCSharp]
    public class PlayerLuaBridge : MonoBehaviour
    {
        // Lua 侧函数引用
        private LuaFunction luaLogicUpdate;
        private LuaFunction luaPhysicsUpdate;
        private LuaFunction luaChangeState;
        private LuaFunction luaGetCurrent;

        // core：传给 Lua 的 C# 玩家引用
        private PlayerController player;
        private bool isInitialized;
        private bool isDead;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        /// <summary>受击：打断 ACTIVE 层（冲刺/攻击）+ 进入 ABNORMAL 层，BASE 冻结</summary>
        private void OnPlayerHurt()
        {
            if (!isInitialized || luaChangeState == null) return;
            // 死亡后不再接受受击事件（否则 Lua hurt 状态会覆盖死亡动画）
            if (IsPlayerDead()) return;
            try
            {
                // ① 先打断 ACTIVE 层。
                //    HFSM 的"ABNORMAL 独占"是在 ACTIVE 之前直接 return 的 —— 它根本不会碰 ACTIVE 的 current，
                //    所以受击打断冲刺/攻击时 dash.Exit / attack.Exit 不会执行，
                //    IsDashing / SetDashInvincible(true) / gravityScale=0 / IsAttacking 会一直挂到
                //    受击结束才补跑（表现：被打飞却浮在空中、无敌帧残留）。
                //    受击打断攻击/冲刺是动作游戏惯例，这里显式清掉，让 Exit 的清理逻辑照常跑到。
                luaChangeState.Call((int)StateLayer.ACTIVE, null, player);

                // ② 进 ABNORMAL 层（State.StateLayer.ABNORMAL == 3）
                luaChangeState.Call((int)StateLayer.ABNORMAL, "hurt", player);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerLuaBridge] 切换受击状态异常: {e}");
            }
        }

        /// <summary>
        /// 死亡瞬间：清空瞬态层 + 永久停止驱动 Lua。
        /// 死亡是"最高优先级"，但它的实现在 C#（PlayerHealth）而不是 HFSM 状态 ——
        /// 因为 HFSM 的瞬态层约定是"结束就清空本层回 BASE"（可逆），
        /// 而死亡不可逆：写成 Lua 状态一旦 return {} 玩家就复活了。
        /// 这里不是在 C# 里复制一份清理逻辑，而是调 Lua 的 ChangeState(layer, nil)，
        /// 让 dash.Exit / attack.Exit 里已有的还原代码（无敌帧、IsAttacking）照常执行。
        /// </summary>
        private void OnPlayerDie()
        {
            isDead = true;
            if (!isInitialized || luaChangeState == null) return;
            try
            {
                luaChangeState.Call((int)StateLayer.ACTIVE, null, player);      // 2 = ACTIVE
                luaChangeState.Call((int)StateLayer.ABNORMAL, null, player);    // 3 = ABNORMAL
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerLuaBridge] 死亡清空瞬态层异常: {e}");
            }
        }

        /// <summary>与 Lua State.StateLayer 枚举对齐（避免裸写 2/3 这种魔法数字）</summary>
        private enum StateLayer { BASE = 1, ACTIVE = 2, ABNORMAL = 3 }

        private bool IsPlayerDead()
        {
            if (isDead) return true;
            return player != null && player.PlayerHealth != null && player.PlayerHealth.IsDead;
        }

        private void Start()
        {
            // 订阅受击事件：放 Start 而非 Awake —— 保证 EventCenter.Awake 已执行，
            // Instance 必然就绪（Unity 不保证 Awake 执行顺序，Awake 订阅可能丢失）
            EventCenter.Instance?.AddListener(
                EventCenter.EventType.PlayerHurt.ToString(), OnPlayerHurt);
            EventCenter.Instance?.AddListener(
                EventCenter.EventType.PlayerDie.ToString(), OnPlayerDie);

            if (LuaManager.LuaEnv == null)
            {
                Debug.LogError("[PlayerLuaBridge] LuaManager 未初始化！请确保场景中有挂载 LuaManager 的 GameObject");
                return;
            }
            InitLua();
        }

        private void InitLua()
        {
            try
            {
                // 加载 PlayerHFSM.lua（内部 require 全部状态文件 + Register）
                var ret = LuaManager.DoString("return require 'Logic.Player.PlayerHFSM'");
                var hfsm = ret[0] as LuaTable;
                if (hfsm == null)
                {
                    Debug.LogError("[PlayerLuaBridge] PlayerHFSM.lua 返回 nil");
                    return;
                }

                // 从 Lua table 取函数引用
                luaLogicUpdate  = hfsm.Get<LuaFunction>("LogicUpdate");
                luaPhysicsUpdate = hfsm.Get<LuaFunction>("PhysicsUpdate");
                luaChangeState   = hfsm.Get<LuaFunction>("ChangeState");
                luaGetCurrent    = hfsm.Get<LuaFunction>("GetCurrent");

                // 首次驱动：让各层 Enter 执行（current 已在 Register 时设为初始状态）
                luaLogicUpdate?.Call(player);

                isInitialized = true;
                Debug.Log("[PlayerLuaBridge] Lua HFSM 初始化完成，BASE=" + GetCurrentStateName(1));
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerLuaBridge] Lua 初始化失败: {e}");
            }
        }

        private void Update()
        {
            if (!isInitialized) return;
            // 死亡后停止驱动 Lua 状态机（死亡动画期间玩家定格，不再跑 ground/air 逻辑）
            if (IsPlayerDead()) return;
            try
            {
                luaLogicUpdate?.Call(player);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerLuaBridge] Lua Update 异常: {e}");
            }
        }

        private void FixedUpdate()
        {
            if (!isInitialized) return;
            // 死亡后同样要停物理驱动：旧代码只拦了 Update，没拦 FixedUpdate。
            // 当时没炸只是因为 hurt.lua 恰好没有 PhysicsUpdate；一旦哪个状态补上
            // PhysicsUpdate，尸体就会继续被物理逻辑推着走。闸门必须两层都拦。
            if (IsPlayerDead()) return;
            try
            {
                luaPhysicsUpdate?.Call(player);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerLuaBridge] Lua PhysicsUpdate 异常: {e}");
            }
        }

        /// <summary>查询某层当前状态名：1=BASE 2=ACTIVE 3=ABNORMAL</summary>
        public string GetCurrentStateName(int layer)
        {
            if (luaGetCurrent == null) return "未初始化";
            try
            {
                var ret = luaGetCurrent.Call(layer);
                return ret.Length > 0 ? ret[0]?.ToString() ?? "?" : "?";
            }
            catch
            {
                return "错误";
            }
        }

        private void OnDestroy()
        {
            EventCenter.Instance?.RemoveListener(
                EventCenter.EventType.PlayerHurt.ToString(), OnPlayerHurt);
            EventCenter.Instance?.RemoveListener(
                EventCenter.EventType.PlayerDie.ToString(), OnPlayerDie);

            // LuaManager（DontDestroyOnLoad）可能先于本组件销毁，LuaEnv 已被 Dispose；
            // 此时再 Dispose LuaFunction 会抛 "this lua env had disposed!"，必须判空
            if (LuaManager.LuaEnv == null) return;

            luaLogicUpdate?.Dispose();
            luaPhysicsUpdate?.Dispose();
            luaChangeState?.Dispose();
            luaGetCurrent?.Dispose();
        }
    }
}
