using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeadCells.Core
{
    /// <summary>
    /// 说明/提示面板 —— 挂在 Canvas 下的空物体上（如 Canvas/SignPanel）。
    ///
    /// 版面 + 内容全部由你在场景里自己搭（这样位置、贴图、颜色、文字都能直接在 Editor 里调）：
    ///
    ///   SignPanel                ← 本组件挂这里，运行时自动 SetActive(false)
    ///   ├── Dim      (Image)     ← 可选：全屏压暗
    ///   ├── Panel    (Image)     ← 可选：底板贴图
    ///   ├── Title    (TMP)       ← 可选：标题
    ///   ├── Body     (TMP)       ← 【必需】正文。文字直接写在 TMP 组件里，代码不碰它
    ///   └── Tip      (TMP)       ← 可选：底部说明
    ///
    /// 代码只做一件事：把面板显示/隐藏。正文内容、位置、样式全在场景里。
    /// 名字必须叫 Body（可以放在任意层级，代码是递归找的）。
    /// </summary>
    public class SignPanel : MonoBehaviour
    {
        public static SignPanel Instance { get; private set; }

        private const string BodyName = "Body";
        private const string FallbackRootName = "FallbackLayout";

        private bool useSceneLayout;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            useSceneLayout = FindLabel(BodyName) != null;

            if (!useSceneLayout)
                Debug.LogWarning("[SignPanel] 场景里没找到名为 Body 的 TMP 文字 → 面板会是空的。" +
                                 "请在 SignPanel 下面建一个 TextMeshPro 文字并命名为 Body，内容直接写在里面");

            gameObject.SetActive(false);
        }

        /// <summary>递归找同名的 TMP 文字（含未激活的子物体，也支持放在任意层级）</summary>
        private TextMeshProUGUI FindLabel(string childName)
        {
            foreach (TextMeshProUGUI t in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.name == childName) return t;
            }
            return null;
        }

        public void Show()
        {
            gameObject.SetActive(true);

            // 场景里有 Body → 什么都不用做：文字是你写在 TMP 组件里的
            if (useSceneLayout) return;

            BuildFallback();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Toggle()
        {
            if (gameObject.activeSelf) Hide();
            else Show();
        }

        /// <summary>
        /// 兜底版面：场景里没搭 Body 时才生成（只为"不崩"，样式很朴素）。
        /// 只删自己上次生成的那一坨（FallbackLayout），绝不去动场景里手工搭的物体。
        /// </summary>
        private void BuildFallback()
        {
            Transform old = transform.Find(FallbackRootName);
            if (old != null) Destroy(old.gameObject);

            GameObject root = new GameObject(FallbackRootName, typeof(RectTransform));
            root.transform.SetParent(transform, false);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            // 全屏压暗：别太黑，0.45 足够把面板托出来又不糊掉场景
            UIFactory.CreatePanel(root.transform, "Dim", new Color(0f, 0f, 0f, 0.45f));

            // 居中底板
            RectTransform panel = UIFactory.CreatePanel(root.transform, "Panel",
                new Color(0.18f, 0.19f, 0.25f, 0.92f));
            UIFactory.Center(panel, Vector2.zero, new Vector2(760f, 400f));

            UIFactory.CreateText(root.transform, "Title", "NOTICE", 26)
                .rectTransform.anchoredPosition = new Vector2(0f, 145f);

            UIFactory.CreateText(root.transform, "Tip", "(E or walk away to close)", 16)
                .rectTransform.anchoredPosition = new Vector2(0f, -160f);
        }
    }
}
