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

        private void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        private void Start()
        {
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
            luaLogicUpdate?.Dispose();
            luaPhysicsUpdate?.Dispose();
            luaChangeState?.Dispose();
            luaGetCurrent?.Dispose();
        }
    }
}
