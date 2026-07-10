using System.IO;
using UnityEngine;
using XLua;

namespace AngryBirds.Core
{
    /// <summary>
    /// Lua 虚拟机管理器 — 全局单例，负责 LuaEnv 生命周期和脚本加载。
    /// 挂载到场景中任意 GameObject 即可。
    /// </summary>
    public class LuaManager : MonoBehaviour
    {
        public static LuaEnv LuaEnv { get; private set; }
        public static LuaManager Instance { get; private set; }

        [Header("Lua 脚本路径（相对于 Resources 或 Assets）")]
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
        /// 自定义 Loader：从 Assets/_Scripts/Lua/ 目录加载 .lua 文件
        /// </summary>
        private byte[] CustomLoader(ref string filepath)
        {
            string fullPath = Path.Combine(luaRootPath, filepath + ".lua");
            if (!File.Exists(fullPath))
            {
                // 也尝试从 Application.dataPath 的相对路径
                fullPath = Path.Combine(Application.dataPath, "_Scripts/Lua", filepath + ".lua");
            }

            if (File.Exists(fullPath))
            {
                string content = File.ReadAllText(fullPath);
                return System.Text.Encoding.UTF8.GetBytes(content);
            }

            return null;
        }

        /// <summary>执行一段 Lua 代码</summary>
        public static object[] DoString(string script)
        {
            return LuaEnv?.DoString(script);
        }

        private void OnDestroy()
        {
            if (LuaEnv != null)
            {
                LuaEnv.Dispose();
                LuaEnv = null;
                Debug.Log("[LuaManager] Lua 虚拟机已释放");
            }
        }
    }
}
