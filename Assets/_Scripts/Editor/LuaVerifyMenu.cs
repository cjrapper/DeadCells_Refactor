using System.Reflection;
using UnityEngine;
using UnityEditor;
using DeadCells.Core;

namespace DeadCells.EditorTools
{
    /// <summary>
    /// 临时验证脚本：菜单栏点一下，测试 Lua HFSM 能否加载。
    /// 编辑模式下不执行 Awake，用反射手动初始化；并强制重建 LuaEnv
    /// 以清除 Play 模式残留的 require 缓存（旧版 lua 文件）。
    /// 验证完可删除本文件。
    /// </summary>
    public static class LuaVerifyMenu
    {
        [MenuItem("Tools/Lua/Verify PlayerHFSM")]
        public static void VerifyPlayerHFSM()
        {
            // —— 强制释放残留 LuaEnv（Play 模式退出后静态字段不清，require 缓存会读旧文件）——
            if (LuaManager.LuaEnv != null)
            {
                Debug.Log("[LuaVerify] 发现残留 LuaEnv，释放中...");
                try { LuaManager.LuaEnv.Dispose(); } catch (System.Exception e) { Debug.LogWarning("[LuaVerify] Dispose 残留 LuaEnv 异常: " + e.Message); }
                // 私有 setter 置 null
                typeof(LuaManager).GetProperty("LuaEnv")
                    .GetSetMethod(true)
                    .Invoke(null, new object[] { null });
            }

            // —— 新建 LuaEnv（编辑模式不触发 Awake，反射调用）——
            GameObject temp = new GameObject("__TempLuaVerify__");
            LuaManager mgr = temp.AddComponent<LuaManager>();
            MethodInfo awake = typeof(LuaManager).GetMethod("Awake",
                BindingFlags.NonPublic | BindingFlags.Instance);
            try
            {
                awake.Invoke(mgr, null);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[LuaVerify] 初始化 LuaManager 异常: " + (e.InnerException ?? e).Message);
                Object.DestroyImmediate(temp);
                return;
            }

            if (LuaManager.LuaEnv == null)
            {
                Debug.LogError("[LuaVerify] LuaEnv 仍为 null，看上面的异常信息");
                Object.DestroyImmediate(temp);
                return;
            }

            Debug.Log("[LuaVerify] === 开始验证 ===");

            // 1. State 工厂
            LuaManager.DoString(@"
                local ok, State = pcall(function() return require('Framework.Fsm.State') end)
                if not ok then print('[LuaVerify] FAIL State.lua: ' .. tostring(State)) return end
                print('[LuaVerify] State.lua OK, StateLayer.ACTIVE=' .. tostring(State.StateLayer.ACTIVE))
            ");

            // 2. HFSM 调度器
            LuaManager.DoString(@"
                local ok, HFSM = pcall(function() return require('Framework.Fsm.HFSM') end)
                if not ok then print('[LuaVerify] FAIL HFSM.lua: ' .. tostring(HFSM)) return end
                print('[LuaVerify] HFSM.lua OK, has LogicUpdate=' .. tostring(HFSM.LogicUpdate ~= nil))
            ");

            // 3. 完整装配（PlayerHFSM 会 require 全部 7 个状态）
            LuaManager.DoString(@"
                local ok, PlayerHFSM = pcall(function() return require('Logic.Player.PlayerHFSM') end)
                if not ok then
                    print('[LuaVerify] FAIL PlayerHFSM.lua: ' .. tostring(PlayerHFSM))
                    return
                end
                print('[LuaVerify] PlayerHFSM OK')
                print('[LuaVerify] BASE current = ' .. PlayerHFSM.GetCurrent(1))
                print('[LuaVerify] ACTIVE current = ' .. PlayerHFSM.GetCurrent(2))
                print('[LuaVerify] ABNORMAL current = ' .. PlayerHFSM.GetCurrent(3))
            ");

            // 清理临时对象（OnDestroy 会 Dispose LuaEnv）
            Object.DestroyImmediate(temp);

            // 兜底：删除场景中任何残留的 __TempLuaVerify__（防止污染场景文件）
            var leftovers = Object.FindObjectsOfType<LuaManager>();
            foreach (var lm in leftovers)
            {
                if (lm.gameObject.name.StartsWith("__TempLuaVerify__"))
                    Object.DestroyImmediate(lm.gameObject);
            }

            Debug.Log("[LuaVerify] === 验证结束 ===");
        }
    }
}
