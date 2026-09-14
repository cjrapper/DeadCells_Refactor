using UnityEngine;
using UnityEngine.UI;
using TMPro;

using DeadCells.Core;

namespace DeadCells.Interaction
{
    /// <summary>
    /// 交互提示 —— 一个"贴在物体头顶"的屏幕空间文字。
    ///
    /// 为什么不用"给每个物体挂一个提示物体"：
    ///   ① 提示要跟着物体在世界里跑。用世界空间 Canvas，每个物体都得配一套（重且难对齐）；
    ///      用 SpriteRenderer 做文字又很丑。
    ///   ② 屏幕空间 UI 分辨率无关、永远清晰，代价是必须每帧把世界坐标转成屏幕坐标。
    /// 所以做法是：全局只有一份提示，谁靠近就把它"顶到自己头上"。
    ///
    /// 用法（调用方每帧上报一次，没上报就自动隐藏）：
    ///     InteractionPrompt.Show(this, "Press E", anchorWorldPos);
    /// 自动隐藏很重要：物体被销毁 / 被禁用 / 忘了调 Hide，都不会在屏幕上留下一个卡住的提示。
    /// </summary>
    public class InteractionPrompt : MonoBehaviour
    {
        public static InteractionPrompt Instance;

        [Header("样式")]
        [Tooltip("字号（像素）。用的是 TMP 默认字体，改字体请在场景里放一个本组件再改")]
        public int fontSize = 36;
        public Color textColor = new Color(1f, 0.85f, 0.35f, 1f);
        [Tooltip("文字相对锚点再往上抬多少（世界单位）")]
        public float worldOffsetY = 0.15f;

        private RectTransform rect;
        private TextMeshProUGUI label;
        private Camera cam;

        private Component owner;        // 当前占用提示的物体
        private int lastShowFrame = -1;
        private Vector3 anchor;

        /// <summary>
        /// 显示提示（调用方每帧调一次）。
        /// 同一帧里先上报的赢 —— 两个可交互物挨着时不会每帧互相抢，文字不会闪。
        /// </summary>
        public static void Show(Component owner, string text, Vector3 worldAnchor)
        {
            if (Instance == null) CreateRuntime();
            Instance.ShowInternal(owner, text, worldAnchor);
        }

        /// <summary>主动隐藏（离开范围时调；不调也会因为"本帧没上报"在下一帧自动隐藏）</summary>
        public static void Hide(Component owner)
        {
            if (Instance == null) return;
            if (Instance.owner == owner) Instance.owner = null;
        }

        private static void CreateRuntime()
        {
            // 场景里没放就自己建一个。
            // 显式带上 RectTransform：Canvas 需要它，让 Unity 去"替换 Transform"不如一开始就给对
            // （AddComponent 会同步执行 Awake，建完 UI 立刻可用）
            GameObject go = new GameObject("[InteractionPrompt]", typeof(RectTransform));
            go.AddComponent<InteractionPrompt>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildUI();
        }

        private void BuildUI()
        {
            // 自带一个 Overlay Canvas：
            //   ① 不依赖场景里有没有 Canvas、叫什么名字
            //   ② sortingOrder 拉高，保证不会被 HUD 挡住（提示被挡住是最坑的，玩家根本看不见）
            //   ③ 不加 CanvasScaler = Constant Pixel Size，坐标和 WorldToScreenPoint 的像素值一一对应
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            label = UIFactory.CreateText(transform, "Label", "", fontSize);
            label.color = textColor;
            // Bottom = 水平居中 + 垂直贴底：配合下面的 pivot，文字正好"坐"在锚点上
            label.alignment = TextAlignmentOptions.Bottom;

            rect = label.rectTransform;
            rect.sizeDelta = new Vector2(400f, 60f);
            rect.pivot = new Vector2(0.5f, 0f);   // 以底边中点为锚点
            label.gameObject.SetActive(false);
        }

        private void ShowInternal(Component src, string text, Vector3 worldAnchor)
        {
            // 本帧已经被别人占用了 → 让先来的那个继续显示（否则两个物体挨着时会每帧闪烁）
            if (owner != null && owner != src && lastShowFrame == Time.frameCount) return;

            owner = src;
            lastShowFrame = Time.frameCount;
            anchor = worldAnchor;

            if (label != null && label.text != text) label.text = text;
        }

        private void LateUpdate()
        {
            if (owner == null || lastShowFrame != Time.frameCount)
            {
                // 本帧没人上报 → 隐藏。这就是"自动失效"的机制
                owner = null;
                SetVisible(false);
                return;
            }

            if (cam == null) cam = Camera.main;      // 缓存：Camera.main 内部是 FindGameObjectWithTag，不能每帧调
            if (cam == null) { SetVisible(false); return; }

            Vector3 screenPos = cam.WorldToScreenPoint(anchor + Vector3.up * worldOffsetY);
            if (screenPos.z < 0f) { SetVisible(false); return; }   // 目标在相机背后

            SetVisible(true);
            rect.position = screenPos;   // Overlay Canvas 的"世界坐标"就是屏幕像素坐标
        }

        private void SetVisible(bool visible)
        {
            if (label != null && label.gameObject.activeSelf != visible)
                label.gameObject.SetActive(visible);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
