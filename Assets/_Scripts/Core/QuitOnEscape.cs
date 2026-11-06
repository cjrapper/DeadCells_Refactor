using UnityEngine;

namespace DeadCells.Core
{
    /// <summary>
    /// 按 ESC 退出游戏 —— 全局行为，**不需要在任何场景里摆物体**。
    ///
    /// 用 [RuntimeInitializeOnLoadMethod] 自举：游戏启动时自动建一个常驻物体。
    /// 这样主菜单场景和关卡场景都自动生效，不用两边各挂一次（少一处会忘的配置）。
    ///
    /// 编辑器里按 ESC = 停止 Play 模式（Application.Quit() 在编辑器里是没反应的，
    /// 这是"打包后能退、编辑器里退不了"的常见困惑来源）。
    ///
    /// 以后要做"ESC 打开暂停菜单"，把这里换成打开暂停面板即可。
    /// </summary>
    public class QuitOnEscape : MonoBehaviour
    {
        public static QuitOnEscape Instance { get; private set; }

        [Tooltip("退出键")]
        public KeyCode quitKey = KeyCode.Escape;

        [Tooltip("勾上后需要按住 0.5 秒才退出（防止误触）")]
        public bool requireHold = false;

        private float holdTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            GameObject go = new GameObject("[QuitOnEscape]");
            DontDestroyOnLoad(go);
            go.AddComponent<QuitOnEscape>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (!requireHold)
            {
                if (Input.GetKeyDown(quitKey)) Quit();
                return;
            }

            // 按住模式：按住 0.5 秒才退，防止手滑
            if (Input.GetKey(quitKey))
            {
                holdTime += Time.unscaledDeltaTime;
                if (holdTime >= 0.5f) Quit();
            }
            else
            {
                holdTime = 0f;
            }
        }

        /// <summary>退出游戏（也挂在按钮上用过：MainMenuUI 的退出按钮）</summary>
        public void Quit()
        {
            Debug.Log("[QuitOnEscape] 退出游戏");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
