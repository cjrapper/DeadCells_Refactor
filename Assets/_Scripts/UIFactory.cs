using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using DeadCells.Data;

namespace DeadCells.Core
{
    /// <summary>
    /// UI 工厂 —— 运行时用代码创建 UI 元素（符合项目规范：不手动拖拽、按钮代码绑定）。
    /// </summary>
    public static class UIFactory
    {
        /// <summary>
        /// 创建面板。传 bg 就用贴图当底板（配合 Image.Type.Sliced 可做九宫格边框）。
        /// 默认铺满父物体（当全屏遮罩用）；要居中一块面板请改返回值的锚点/尺寸。
        /// </summary>
        public static RectTransform CreatePanel(Transform parent, string name, Color bgColor,
            Sprite bg = null, Image.Type bgType = Image.Type.Sliced)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            Image img = go.GetComponent<Image>();
            img.color = bgColor;
            if (bg != null)
            {
                img.sprite = bg;
                // Sliced 需要贴图在 Sprite Editor 里设过 Border；
                // 没设 Border 时表现和 Simple 一样，不会报错
                img.type = bgType;
            }

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>创建文本，返回 TextMeshProUGUI</summary>
        public static TextMeshProUGUI CreateText(Transform parent, string name, string content, int size = 20)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            Center(text.rectTransform);
            return text;
        }

        /// <summary>创建按钮（含文字），点击事件代码绑定；传 bg 则用九宫格贴图做按钮底</summary>
        public static Button CreateButton(Transform parent, string name, string label, Action onClick, Sprite bg = null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            if (bg != null)
            {
                // 九宫格按钮：贴图拉伸不变形（前提：贴图在 Sprite Editor 里设过 Border）
                img.sprite = bg;
                img.type = Image.Type.Sliced;
            }
            else
            {
                img.color = new Color(0.25f, 0.3f, 0.45f, 1f);
            }

            Button btn = go.GetComponent<Button>();
            // 所有代码生成的按钮都自动带点击音：一处接线，全项目按钮生效
            btn.onClick.AddListener(() => AudioManager.Play(SfxId.UIClick));
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            Center(btn.GetComponent<RectTransform>());

            // 按钮文字子物体（字号 20：之前 18 在 1920×1080 下偏小）
            TextMeshProUGUI text = CreateText(go.transform, "Label", label, 20);
            RectTransform tr = text.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            text.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        /// <summary>
        /// 把 RectTransform 锚定到父物体的正中心。
        ///
        /// 这一步不能省：用编辑器菜单创建 UI 时 Unity 会顺手帮你把锚点设好，
        /// 但代码 new 出来的 RectTransform 不会 —— 锚点没设，anchoredPosition 的含义就不确定，
        /// 运行时生成的 UI 很容易整体偏到屏幕角落去。
        /// 显式设成中心锚点后，(0,0) 就等于"父物体正中心"，后面写坐标才有统一含义。
        /// </summary>
        public static RectTransform Center(RectTransform rt, Vector2? anchoredPos = null, Vector2? size = null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            if (anchoredPos.HasValue) rt.anchoredPosition = anchoredPos.Value;
            if (size.HasValue) rt.sizeDelta = size.Value;
            return rt;
        }

        /// <summary>清空某 Transform 的全部子物体（面板刷新用）</summary>
        public static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
        }

        /// <summary>设置 RectTransform 为一行（水平布局辅助）</summary>
        public static void SetRow(RectTransform rt, float height)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, height);
        }
    }
}
