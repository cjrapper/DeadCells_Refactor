using System;
using System.Collections.Generic;
using UnityEngine;
using XLua;

using DeadCells.Core;

namespace DeadCells.Enemy
{
    /// <summary>
    /// Enemy → Lua 桥接组件。
    /// 挂在敌人 GameObject 上，Awake 时加载 enemy_fsm.lua 并注入状态脚本。
    /// </summary>
    [LuaCallCSharp]
    public class EnemyLuaBridge : MonoBehaviour
    {
        [Header("Lua 状态脚本路径（相对于 Lua 根目录，不含 .lua）")]
        public string fsmMain = "enemy_fsm";
        public List<string> stateModules = new List<string>
        {
            "patrol_state",
            "chase_state",
            "telegraph_state",
            "attack_state",
            "hurt_state"
        };

        // Lua 侧的函数引用
        private LuaFunction luaInit;
        private LuaFunction luaUpdate;
        private LuaFunction luaPhysicsUpdate;
        private LuaFunction luaChangeState;
        private LuaFunction luaGetStateName;

        // 对应的 C# Enemy 组件
        private Enemy enemy;
        private bool isInitialized;

        private void Awake()
        {
            enemy = GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.useLuaFSM = true;  // 告诉 Enemy：用 Lua 状态机，跳过 C# FSM
                Debug.Log("[EnemyLuaBridge] 已接管 " + enemy.name);
            }
        }

        private void Start()
        {
            if (LuaManager.LuaEnv == null)
            {
                Debug.LogError("[EnemyLuaBridge] LuaManager 未初始化！请确保场景中有挂载 LuaManager 的 GameObject");
                return;
            }

            InitLua();
        }

        private void InitLua()
        {
            try
            {
                // 1. 加载 combat_util（未来扩展用，目前为空壳）
                LuaManager.DoString("require 'combat_util'");

                // 2. 加载 FSM 主文件
                LuaManager.DoString("local fsm = require 'enemy_fsm'");

                // 3. 获取 Function 引用
                var env = LuaManager.LuaEnv.Global;
                luaInit = env.Get<LuaFunction>("Init");
                luaUpdate = env.Get<LuaFunction>("LogicUpdate");
                luaPhysicsUpdate = env.Get<LuaFunction>("PhysicsUpdate");
                luaChangeState = env.Get<LuaFunction>("ChangeState");
                luaGetStateName = env.Get<LuaFunction>("GetCurrentStateName");

                // 4. 注入自身和状态模块列表
                luaInit.Call(enemy, stateModules);
                luaChangeState.Call("patrol");

                isInitialized = true;
                Debug.Log("[EnemyLuaBridge] Lua 状态机初始化完成，当前状态: patrol");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EnemyLuaBridge] Lua 初始化失败: {e}");
            }
        }

        private void Update()
        {
            if (!isInitialized) return;
            try
            {
                luaUpdate?.Call();
            }
            catch (Exception e)
            {
                Debug.LogError($"[EnemyLuaBridge] Lua Update 异常: {e}");
            }
        }

        private void FixedUpdate()
        {
            if (!isInitialized) return;
            try
            {
                luaPhysicsUpdate?.Call();
            }
            catch (Exception e)
            {
                Debug.LogError($"[EnemyLuaBridge] Lua PhysicsUpdate 异常: {e}");
            }
        }

        /// <summary>获取当前 Lua 状态名（供编辑器/调试）</summary>
        public string GetCurrentStateName()
        {
            if (luaGetStateName == null) return "未初始化";
            try
            {
                var ret = luaGetStateName.Call();
                return ret.Length > 0 ? ret[0]?.ToString() ?? "?" : "?";
            }
            catch
            {
                return "错误";
            }
        }

        private void OnDestroy()
        {
            luaInit?.Dispose();
            luaUpdate?.Dispose();
            luaPhysicsUpdate?.Dispose();
            luaChangeState?.Dispose();
            luaGetStateName?.Dispose();
        }
    }
}
