using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using DeadCells.Player;

namespace DeadCells.Core
{
    /// <summary>
    /// 主菜单 —— 挂在 MainMenu 场景的 Canvas/Menu 下。
    ///
    /// 按钮绑定策略（两种模式自动二选一，不会同时存在）：
    ///   ① 场景里自己搭了 Button → 全部代码绑定，不再生成任何 UI（尊重你搭的布局）
    ///   ② 场景里一个 Button 都没有 → 用 UIFactory 运行时生成整套兜底 UI
    /// 为什么必须二选一：Canvas 的子物体按顺序绘制，后画的盖先画的。
    /// 之前 Menu 在 Image 前面，代码生成的标题/按钮全被木牌盖住了（已踩坑）。
    ///
    /// 按钮命名约定（认不出来的一律当"开始游戏"）：
    ///   名字里含 quit / exit → 退出游戏；其余 → 开始游戏
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("标题配置（仅在场景里没有按钮、走代码兜底时使用）")]
        public string title = "PROJECT DASH";
        public string subtitle = "2D Action Platformer Demo";

        [Header("关卡场景名（必须和 Build Settings 里的名字一致）")]
        public string levelSceneName = "11";

        [Header("按钮贴图（可选，仅在走代码兜底时使用）")]
        public Sprite buttonSprite;

        private void Awake()
        {
            // 优先绑定场景里手工搭的按钮；一个都没有才兜底生成整套 UI
            if (!BindSceneButtons())
                BuildUI();
        }

        /// <summary>
        /// 绑定场景里手工搭的按钮，返回是否找到了按钮。
        ///
        /// 用"从 Canvas 递归查找"而不是 transform.Find：
        /// 自己搭的按钮经常不在本物体下面（比如 Canvas/Image/Button，和 Menu 是兄弟分支），
        /// 而 transform.Find 只能搜自己的子物体 —— 这个坑踩过了。
        /// </summary>
        private bool BindSceneButtons()
        {
            Transform canvas = transform.parent != null ? transform.parent : transform;
            Button[] sceneButtons = canvas.GetComponentsInChildren<Button>(true);
            if (sceneButtons.Length == 0) return false;

            foreach (Button btn in sceneButtons)
            {
                // 先清掉 Inspector 里可能绑过的事件，避免重复触发（规范：按钮一律代码绑定）
                btn.onClick.RemoveAllListeners();

                string n = btn.name.ToLowerInvariant();
                if (n.Contains("quit") || n.Contains("exit"))
                {
                    btn.onClick.AddListener(OnQuit);
                    Debug.Log($"[MainMenu] 绑定按钮「{btn.name}」→ 退出游戏");
                }
                else
                {
                    btn.onClick.AddListener(OnStartGame);
                    Debug.Log($"[MainMenu] 绑定按钮「{btn.name}」→ 开始游戏（加载场景 \"{levelSceneName}\"）");
                }
            }
            return true;
        }

        /// <summary>场景里没按钮时的兜底：整套 UI 运行时生成（零拖拽，符合项目规范）</summary>
        private void BuildUI()
        {
            Debug.Log("[MainMenu] 场景里没找到 Button，改用代码生成兜底 UI");

            // 标题
            // 注意：anchoredPosition 是 RectTransform 的成员，变量必须声明为 RectTransform
            RectTransform titleText = UIFactory.CreateText(transform, "Title", title, 64).rectTransform;
            titleText.anchoredPosition = new Vector2(0f, 150f);

            RectTransform subtitleText = UIFactory.CreateText(transform, "Subtitle", subtitle, 24).rectTransform;
            subtitleText.anchoredPosition = new Vector2(0f, 95f);

            // 开始按钮（事件代码绑定，符合规范；有 buttonSprite 就自动用九宫格贴图）
            Button startBtn = UIFactory.CreateButton(transform, "StartBtn", "START GAME", OnStartGame, buttonSprite);
            RectTransform sRt = startBtn.GetComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(0f, -10f);
            sRt.sizeDelta = new Vector2(240f, 56f);

            // 退出按钮
            Button quitBtn = UIFactory.CreateButton(transform, "QuitBtn", "QUIT", OnQuit, buttonSprite);
            RectTransform qRt = quitBtn.GetComponent<RectTransform>();
            qRt.anchoredPosition = new Vector2(0f, -85f);
            qRt.sizeDelta = new Vector2(240f, 48f);
        }

        private void OnStartGame()
        {
            if (string.IsNullOrEmpty(levelSceneName))
            {
                Debug.LogError("[MainMenu] levelSceneName 没配！请在 Inspector 里填关卡场景名（比如 11）");
                return;
            }

            // 场景必须在 Build Settings 里勾选过，否则 LoadScene 会在运行时抛异常
            if (!Application.CanStreamedLevelBeLoaded(levelSceneName))
            {
                Debug.LogError($"[MainMenu] 场景 \"{levelSceneName}\" 不在 Build Settings 里，加载不了。" +
                               "请 File → Build Settings 把它加进去并勾选");
                return;
            }

            // 双保险：上一次死亡/顿帧可能留下 timeScale = 0，会把新关卡直接卡死
            Time.timeScale = 1f;
            PlayerHealth.RestoreTimeScale();
            SceneManager.LoadScene(levelSceneName);
        }

        private void OnQuit()
        {
            // 统一走 QuitOnEscape：编辑器里 Application.Quit() 没反应，
            // 那里会改成停止 Play 模式（"打包后能退、编辑器里退不了"的坑）
            if (QuitOnEscape.Instance != null) QuitOnEscape.Instance.Quit();
            else Application.Quit();
        }
    }
}
