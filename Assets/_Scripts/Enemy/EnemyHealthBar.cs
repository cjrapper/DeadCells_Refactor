using UnityEngine;

namespace DeadCells.Enemy
{
    /// <summary>
    /// 敌人简易血条 —— 显示在敌人头顶。Enemy.Awake 会自动补上这个组件，不用逐个配。
    ///
    /// 为什么不用 UI：
    ///   · World Space Canvas：每只敌人一个 Canvas，重且难对齐
    ///   · 屏幕空间 UI：每帧要做世界→屏幕坐标转换 + 处理出屏/被遮挡
    ///   · 两个 SpriteRenderer 最省：跟着敌人跑、自动排序、和像素风对得上
    ///
    /// 贴图不用你准备：运行时用 1×1 白图生成，靠缩放当条。
    /// 材质强制用不受光的 Sprites/Default —— 否则血条会被场景光照压暗，
    /// "暗处的敌人血条看不见"就是这么来的。
    /// </summary>
    public class EnemyHealthBar : MonoBehaviour
    {
        [Header("尺寸与位置")]
        public Vector2 size = new Vector2(1.1f, 0.14f);
        [Tooltip("血条底边离敌人贴图顶部的高度")]
        public float heightOffset = 0.15f;

        [Header("颜色")]
        public Color backColor = new Color(0f, 0f, 0f, 0.8f);
        public Color fillColor = new Color(0.9f, 0.2f, 0.2f, 1f);

        [Header("显隐")]
        [Tooltip("满血时也隐藏（想让血条只在受伤时冒出来就勾上）")]
        public bool hideWhenFull = false;
        [Tooltip("受伤后显示多久（秒）；<= 0 = 一直显示")]
        public float showDuration = 0f;

        private Enemy enemy;
        private Renderer bodyRenderer;
        private Transform barRoot;
        private Transform fillTf;
        private SpriteRenderer backSr;
        private SpriteRenderer fillSr;

        private float lastRatio = 1f;
        private float hideAtTime = -1f;

        private static Sprite whiteSprite;      // 所有血条共用一个 1×1 白图
        private static Material unlitMaterial;  // 所有血条共用一个不受光材质

        private void Awake()
        {
            enemy = GetComponent<Enemy>();
            bodyRenderer = GetComponentInChildren<Renderer>();
            BuildBar();
        }

        // ==================== 构建 ====================

        private void BuildBar()
        {
            GameObject root = new GameObject("HealthBar");
            root.transform.SetParent(transform, false);
            barRoot = root.transform;

            // 底：深色，固定大小
            backSr = CreateLayer("Back", backColor, 0);
            // ★ 必须和填充条用同一个起点：白图的轴心在【左边】，
            //   不偏移的话底条会从 x=0 往右画、填充条从 x=-halfW 往右画，
            //   两条错开半个身位，看起来就像"血条没满"
            backSr.transform.localPosition = new Vector3(-size.x * 0.5f, 0f, 0f);
            backSr.transform.localScale = new Vector3(size.x, size.y, 1f);

            // 填充：轴心在左边（Sprite 生成时 pivot 设为 (0, 0.5)），
            // 所以 localScale.x = 血量比例 时是从右往左缩
            fillSr = CreateLayer("Fill", fillColor, 1);
            fillSr.transform.localPosition = new Vector3(-size.x * 0.5f, 0f, 0f);
            fillTf = fillSr.transform;
            fillTf.localScale = new Vector3(size.x, size.y * 0.7f, 1f);

            barRoot.gameObject.SetActive(false);
        }

        private SpriteRenderer CreateLayer(string name, Color color, int orderOffset)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(barRoot, false);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteSprite;
            sr.color = color;
            sr.material = UnlitMaterial;                 // 不受场景光照影响
            sr.sortingLayerID = 0;                       // Default
            sr.sortingOrder = 100 + orderOffset;         // 盖在场景所有东西上面

            return sr;
        }

        /// <summary>1×1 白图，轴心在左边中点 —— 这样横向缩放就是"从左往右填充"</summary>
        private static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite == null)
                {
                    Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    tex.filterMode = FilterMode.Point;   // 像素风不糊
                    whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0f, 0.5f), 1f);
                    whiteSprite.name = "EnemyHealthBarWhite";
                }
                return whiteSprite;
            }
        }

        private static Material UnlitMaterial
        {
            get
            {
                if (unlitMaterial == null)
                {
                    // Sprites/Default 是内置不受光材质（也在 Always Included Shaders 里，打包不会丢）
                    Shader s = Shader.Find("Sprites/Default");
                    if (s != null) unlitMaterial = new Material(s);
                }
                return unlitMaterial;
            }
        }

        // ==================== 每帧 ====================

        private void LateUpdate()
        {
            if (enemy == null || barRoot == null) return;

            float ratio = enemy.HealthRatio;

            // 血量变化 → 刷新条 + 重新计时
            if (!Mathf.Approximately(ratio, lastRatio))
            {
                lastRatio = ratio;
                fillTf.localScale = new Vector3(Mathf.Max(0f, size.x * ratio), size.y * 0.7f, 1f);
                if (showDuration > 0f) hideAtTime = Time.time + showDuration;
            }

            bool show = !(hideWhenFull && enemy.IsFullHealth) && !enemy.IsDyingNow;
            if (show && hideAtTime > 0f && Time.time > hideAtTime) show = false;
            if (barRoot.gameObject.activeSelf != show) barRoot.gameObject.SetActive(show);
            if (!show) return;

            UpdateTransform();
        }

        private void UpdateTransform()
        {
            // 位置：敌人贴图顶边中点再往上抬一点
            float topY = bodyRenderer != null
                ? bodyRenderer.bounds.max.y + heightOffset
                : transform.position.y + 1.2f;
            barRoot.position = new Vector3(transform.position.x, topY, 0f);

            // ★ 敌人靠 localScale.x = -1 翻转朝向，子物体血条会被一起镜像。
            //   把血条自己的 X 缩放取成"和父物体同号"就能抵消（负×负=正）。
            float sign = transform.localScale.x < 0f ? -1f : 1f;
            barRoot.localScale = new Vector3(sign, 1f, 1f);
        }
    }
}
