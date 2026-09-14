using System.IO;
using UnityEngine;
using XLua;

namespace DeadCells.Core
{
    /// <summary>
    /// Lua 虚拟机管理器 — 全局单例，负责 LuaEnv 生命周期和脚本加载。
    /// 挂载到场景中任意 GameObject 即可。
    /// </summary>
    public class LuaManager : MonoBehaviour
    {
        public static LuaEnv LuaEnv { get; private set; }

        /// <summary>
        /// 单例引用 —— 必须是 static！
        /// 之前写成实例属性（非 static）导致严重 bug：
        /// 场景重载时新 LuaManager 的 Instance 恒为 null → 不会自我销毁，
        /// 反而 new 一个新 LuaEnv 覆盖静态字段，随后又因其他组件（EventCenter 等）被销毁，
        /// 其 OnDestroy 里 this == 自己的 Instance 成立 → 把玩家正在用的 LuaEnv Dispose 掉，
        /// 表现就是"restart 后报 this lua env had disposed、读不了指令"。
        /// </summary>
        public static LuaManager Instance { get; private set; }

        [Header("Lua 目录")]
        [Tooltip("相对 StreamingAssets 的子目录名。编辑器里 = Assets/StreamingAssets/Lua，" +
                 "打包后 = <游戏>_Data/StreamingAssets/Lua")]
        public string luaFolder = "Lua";

        [Tooltip("兼容兜底：老的工程内相对路径（StreamingAssets 里找不到时才试）")]
        public string luaRootPath = "Assets/_Scripts/Lua";

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LuaEnv = new LuaEnv();
            LuaEnv.AddLoader(CustomLoader);

            Debug.Log("[LuaManager] Lua 虚拟机初始化完成");
        }

        /// <summary>
        /// 自定义 Loader：从 StreamingAssets/Lua 加载 .lua 文件。
        ///
        /// ★ 为什么必须放 StreamingAssets：
        ///   .lua 不是 Unity 认识的资源类型，放在 Assets 下会被当成 DefaultAsset，
        ///   【根本不会被打进包】。而 StreamingAssets 里的文件是【原样拷贝】进包的。
        ///   旧实现只从 Application.dataPath 读，编辑器里能跑，
        ///   打包后那个目录变成 &lt;游戏&gt;_Data，里面没有 _Scripts/Lua
        ///   → 所有 require 失败 → 玩家 HFSM 初始化不了 → 出包后人物完全不动。
        ///   （xLua 最经典的出包坑：编辑器全对，包一打就废）
        ///
        ///   现在编辑器和打包后走的是同一份文件：Application.streamingAssetsPath
        ///   在编辑器里 = Assets/StreamingAssets，在包里 = &lt;游戏&gt;_Data/StreamingAssets。
        /// </summary>
        private byte[] CustomLoader(ref string filepath)
        {
            // 点号路径 → 目录路径：require("Framework.Fsm.State") → Framework/Fsm/State.lua
            filepath = filepath.Replace('.', '/');
            string relative = filepath + ".lua";

            // ① 首选：StreamingAssets/Lua（编辑器和打包后通用）
            string fullPath = Path.Combine(Application.streamingAssetsPath, luaFolder, relative);
            if (File.Exists(fullPath)) return ReadLuaFile(fullPath);

            // ② 兜底：工程内相对路径（万一 StreamingAssets 那份漏了）
            fullPath = Path.Combine(luaRootPath, relative);
            if (File.Exists(fullPath)) return ReadLuaFile(fullPath);

            fullPath = Path.Combine(Application.dataPath, "_Scripts/Lua", relative);
            if (File.Exists(fullPath)) return ReadLuaFile(fullPath);

            // 三条路径都没有：明确报出来，别让它静默变成"人物不动"
            Debug.LogError($"[LuaManager] 找不到 Lua 文件：{relative}\n" +
                           $"  找过：{Path.Combine(Application.streamingAssetsPath, luaFolder, relative)}\n" +
                           $"  找过：{Path.Combine(luaRootPath, relative)}");
            return null;
        }

        private static byte[] ReadLuaFile(string fullPath)
        {
            try
            {
                return System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(fullPath));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LuaManager] 读取 Lua 失败：{fullPath} —— {e.Message}");
                return null;
            }
        }

        /// <summary>执行一段 Lua 代码</summary>
        public static object[] DoString(string script)
        {
            return LuaEnv?.DoString(script);
        }

        private void OnDestroy()
        {
            // 只有"主实例"才允许 Dispose 静态 LuaEnv！
            // 场景重载时，新场景的冗余 LuaManager 会被 Awake 里 Destroy(gameObject) 销毁，
            // 其 OnDestroy 同样会执行——若这里无条件 Dispose，会把主实例正在用的 LuaEnv 干掉，
            // 导致玩家 Lua HFSM 每帧报 "this lua env had disposed"（死亡重启后无法输入，严重 bug）
            if (this != Instance) return;

            if (LuaEnv != null)
            {
                LuaEnv.Dispose();
                LuaEnv = null;
                Debug.Log("[LuaManager] Lua 虚拟机已释放");
            }
        }
        // 临时验证脚本（放 Assets/Editor 或场景里挂个空物体）
    }
}
