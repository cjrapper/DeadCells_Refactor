using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

using DeadCells.Player;

namespace DeadCells.Core
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager instance;

        // ==================== UI 引用（Awake 自动获取，不依赖手动拖拽）====================
        private Image healthBarFill;
        private GameObject gameOverPanel;
        private GameObject victoryPanel;      // 胜利结算面板
        private TextMeshProUGUI coinText;     // 场景文本是 TMP（TextMeshPro），不是老版 UGUI Text
        private Transform canvasTransform;    // 缓存 Canvas，通关面板兜底创建时要用（RefreshUI 里的局部变量留不住）

        void Awake()
        {
            if (instance == null) instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            // GameManager 挂了 EventCenter（DontDestroyOnLoad）→ 本组件随物体跨场景存活。
            // 场景重载（死亡 Restart）后 Canvas 是全新的，必须重新获取引用（关键！）
            RefreshUI();
        }

        void OnEnable()
        {
            // 场景加载后刷新 UI 引用（重载后旧引用指向已销毁的 Canvas → 血条/金币/面板全挂）
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single)
                RefreshUI();
        }

        /// <summary>（重新）获取 UI 引用 + 重新绑定按钮 —— Awake 和每次场景加载都调用</summary>
        private void RefreshUI()
        {
            // GameObject.Find 是全场景查找（Canvas 是场景根物体，
            // 不是 UIManager 的子物体，transform.Find 只能搜子物体——已踩坑）
            Transform canvas = GameObject.Find("Canvas")?.transform;
            if (canvas == null)
            {
                Debug.LogError("[UIManager] 未找到 Canvas（场景重载后 UI 刷新失败）");
                return;
            }
            canvasTransform = canvas;

            healthBarFill = canvas.Find("HealthBar_BG/HealthBar_Fill")?.GetComponent<Image>();
            // Find 按"物体名"匹配（不是文本内容），找不到要空判断，不能 NRE 中断 Awake
            coinText = canvas.Find("CoinText")?.GetComponent<TextMeshProUGUI>();
            gameOverPanel = canvas.Find("Panel")?.gameObject;
            victoryPanel = canvas.Find("VictoryPanel")?.gameObject;

            // 按钮点击事件一律代码绑定，不在 Inspector 绑定（场景重载后按钮是新的，必须重绑）
            Button restartBtn = canvas.Find("Panel/Button")?.GetComponent<Button>();
            if (restartBtn != null)
            {
                restartBtn.onClick.RemoveAllListeners();
                restartBtn.onClick.AddListener(RestartGame);
            }

            // 胜利面板：重来 + 回主菜单按钮（全部代码绑定）
            Button retryBtn = canvas.Find("VictoryPanel/RetryBtn")?.GetComponent<Button>();
            if (retryBtn != null)
            {
                retryBtn.onClick.RemoveAllListeners();
                retryBtn.onClick.AddListener(RestartGame);
            }
            Button menuBtn = canvas.Find("VictoryPanel/MenuBtn")?.GetComponent<Button>();
            if (menuBtn != null)
            {
                menuBtn.onClick.RemoveAllListeners();
                menuBtn.onClick.AddListener(LoadMainMenu);
            }

            if (healthBarFill == null)
            {
                Debug.LogError("[UIManager] 未找到 Canvas/HealthBar_BG/HealthBar_Fill，血条不会动。" +
                               "要求：底图物体名严格是 HealthBar_BG，它的【直接】子物体叫 HealthBar_Fill，" +
                               "并且子物体上挂的是 Image（不是 SpriteRenderer）");
            }
            else
            {
                // fillAmount 只在 Image Type = Filled 时有效。
                // 这个坑最阴的地方是【完全不报错】：代码照跑、fillAmount 照写，就是没反应。
                // 所以这里主动验一次配置，"血条不动"以后能直接看日志定位。
                if (healthBarFill.type != Image.Type.Filled)
                {
                    Debug.LogError($"[UIManager] HealthBar_Fill 的 Image Type 是 {healthBarFill.type}，" +
                                   "不是 Filled → fillAmount 写了也没效果。" +
                                   "请在 Inspector 把 Type 改成 Filled、Fill Method 选 Horizontal、Fill Origin 选 Left");
                }
                if (!healthBarFill.gameObject.activeSelf)
                    Debug.LogWarning("[UIManager] HealthBar_Fill 物体是未激活的 → 血条不会显示（Inspector 里勾上它）");

                // 进场先满血：别沿用上一次死亡/重开时留在 Inspector 里的残留 fillAmount
                healthBarFill.fillAmount = 1f;
            }

            if (coinText == null)
            {
                Debug.LogError("[UIManager] 未找到 Canvas/CoinText（必须是 Canvas 的直接子物体、名字就叫 CoinText），" +
                               "金币数不会显示");
            }
            else
            {
                // 开局先把当前金币数写进去。
                // 更新金币的代码只在 CoinChange 事件里跑，不初始化的话开局那一格是空的，
                // 要捡到第一枚金币才出现数字（HUD 看起来像坏了）
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                PlayerWallet wallet = player != null ? player.GetComponent<PlayerWallet>() : null;
                coinText.text = wallet != null ? wallet.Coins.ToString() : "0";
            }
            if (gameOverPanel == null)
                Debug.LogError("[UIManager] 未找到 Canvas/Panel，请检查场景层级");
            if (victoryPanel == null)
                Debug.Log("[UIManager] 未找到 Canvas/VictoryPanel（没做通关界面？）");
        }

        void Start()
        {
            if (EventCenter.Instance != null)
            {
                EventCenter.Instance.AddListener<int, int>(EventCenter.EventType.PlayerHealthChange.ToString(), UpdateHealthBar);
                EventCenter.Instance.AddListener(EventCenter.EventType.PlayerDead.ToString(), ShowGameOverPanel);
                EventCenter.Instance.AddListener<int>(EventCenter.EventType.CoinChange.ToString(), UpdateCoinText);
                EventCenter.Instance.AddListener(EventCenter.EventType.BossDefeated.ToString(), ShowVictoryPanel);
            }
            else
            {
                Debug.LogError("EventCenter.Instance is null! 请确保场景中有一个物体挂载了 EventCenter 脚本。");
            }
        }

        public void UpdateCoinText(int coin)
        {
            if(coinText != null)coinText.text = coin.ToString();
        }

        public void UpdateHealthBar(int current, int max)
        {
            // max <= 0 会算出 NaN，NaN 写进 fillAmount 之后血条会彻底不动（比不动更糟：再也回不来）
            if (healthBarFill == null || max <= 0) return;

            healthBarFill.fillAmount = Mathf.Clamp01((float)current / max);
        }

        private void ShowGameOverPanel()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }

        // 重新加载当前场景
        public void RestartGame()
        {
            // 双保险：直接置 1（不依赖静态 previousTimeScale——重载卡死的常见根因）
            Time.timeScale = 1f;
            PlayerHealth.RestoreTimeScale();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>返回主菜单（GameOver 面板的"回主菜单"按钮绑定这个）</summary>
        public void LoadMainMenu()
        {
            Time.timeScale = 1f;   // 双保险（同上）
            PlayerHealth.RestoreTimeScale();
            SceneManager.LoadScene("MainMenu");
        }

        /// <summary>Boss 击败 → 胜利结算：暂停 + 显示面板</summary>
        private void ShowVictoryPanel()
        {
            // 场景里没搭 VictoryPanel 时用代码兜底建一个，保证"击败 Boss → 通关"这条链路能走完。
            // 之前这里第一行就是 if (victoryPanel == null) return; —— 静默什么都不做，
            // 表现就是"Boss 死了但游戏没结束"，非常难查（踩坑）
            if (victoryPanel == null) BuildFallbackVictoryPanel();

            if (victoryPanel == null)
            {
                Debug.LogError("[UIManager] Boss 已被击败，但没有 Canvas/VictoryPanel，兜底也建不出来。" +
                               "请确认：① Boss 身上 isBoss 已勾选 ② 场景里有 Canvas");
                return;
            }

            // 冻结游戏（返回主菜单/重开时有 RestoreTimeScale 兜底）
            Time.timeScale = 0f;
            victoryPanel.SetActive(true);
            Debug.Log("[UIManager] 通关！");
        }

        /// <summary>
        /// 通关面板兜底：正式出包建议自己在 Canvas 里搭好看的（Hierarchy 名严格按下面来，
        /// 代码检测到就不会走兜底）：
        ///   Canvas/VictoryPanel          ← 整个面板，默认 SetActive(false)
        ///   Canvas/VictoryPanel/RetryBtn ← 按钮，重开
        ///   Canvas/VictoryPanel/MenuBtn  ← 按钮，回主菜单
        /// </summary>
        private void BuildFallbackVictoryPanel()
        {
            if (canvasTransform == null) return;

            Debug.LogWarning("[UIManager] 场景里没有 Canvas/VictoryPanel，已用代码兜底创建一个（样式很朴素，" +
                             "正式版请自己在场景里搭一个，名字保持一致即可自动接管）");

            RectTransform panel = UIFactory.CreatePanel(canvasTransform, "VictoryPanel",
                new Color(0f, 0f, 0f, 0.75f));
            victoryPanel = panel.gameObject;

            TextMeshProUGUI title = UIFactory.CreateText(panel, "Title", "VICTORY", 64);
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(1f, 0.85f, 0.35f, 1f);
            RectTransform titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 0.6f);
            titleRt.anchorMax = new Vector2(1f, 0.8f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;

            Button retryBtn = UIFactory.CreateButton(panel, "RetryBtn", "RETRY", RestartGame);
            PlacePanelButton(retryBtn, 0.42f, 220f);
            Button menuBtn = UIFactory.CreateButton(panel, "MenuBtn", "MAIN MENU", LoadMainMenu);
            PlacePanelButton(menuBtn, 0.30f, 220f);
        }

        /// <summary>把兜底面板的按钮摆到屏幕中下方（anchorY 越大越靠上）</summary>
        private static void PlacePanelButton(Button btn, float anchorY, float width)
        {
            RectTransform rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, anchorY);
            rt.anchorMax = new Vector2(0.5f, anchorY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(width, 56f);
        }

        void OnDestroy()
        {
            if (EventCenter.Instance != null)
            {
                EventCenter.Instance.RemoveListener<int, int>(EventCenter.EventType.PlayerHealthChange.ToString(), UpdateHealthBar);
                EventCenter.Instance.RemoveListener(EventCenter.EventType.PlayerDead.ToString(), ShowGameOverPanel);
                EventCenter.Instance.RemoveListener<int>(EventCenter.EventType.CoinChange.ToString(), UpdateCoinText);
                EventCenter.Instance.RemoveListener(EventCenter.EventType.BossDefeated.ToString(), ShowVictoryPanel);
            }
        }

    }
}
