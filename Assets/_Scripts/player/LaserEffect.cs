using System.Collections;
using UnityEngine;

namespace DeadCells.Player
{
    /// <summary>
    /// 激光视觉组件 —— 挂在玩家上，由 LaserWeapon 调用：
    /// 显示一条从 from 到 to 的激光线 0.15 秒后隐藏（协程）。
    /// LineRenderer 若无则自动创建（挂子物体），无需手动配置。
    /// </summary>
    public class LaserEffect : MonoBehaviour
    {
        public float showDuration = 0.15f;
        [Tooltip("激光主色（橙黄）")]
        public Color laserColor = new Color(1f, 0.75f, 0.25f, 1f);   // 橙黄
        [Tooltip("激光粗细（起点）")]
        public float laserWidth = 0.12f;

        private LineRenderer line;
        private Coroutine hideRoutine;

        private void Awake()
        {
            EnsureLine();
            if (line != null) line.enabled = false;
        }

        private void EnsureLine()
        {
            if (line != null) return;

            // 找已有的，没有就创建一个子物体（一次创建，之后复用）
            line = GetComponentInChildren<LineRenderer>();
            if (line == null)
            {
                GameObject go = new GameObject("LaserLine", typeof(LineRenderer));
                go.transform.SetParent(transform, false);
                line = go.GetComponent<LineRenderer>();
            }

            // 安全兜底：材质/颜色/宽度（Shader 用 Sprites/Default 保证可见）
            if (line.sharedMaterial == null)
                line.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            line.startColor = laserColor;
            line.endColor = laserColor;
            line.startWidth = laserWidth;
            line.endWidth = laserWidth * 0.5f;   // 尾部变细，光束感更强
            line.enabled = false;
        }

        /// <summary>显示一段激光（LaserWeapon.Attack 调用）</summary>
        public void ShowLaser(Vector2 from, Vector2 to)
        {
            EnsureLine();
            if (line == null) return;

            line.enabled = true;
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);

            // 重新开协程（避免连发时旧协程提前隐藏）
            if (hideRoutine != null) StopCoroutine(hideRoutine);
            hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(showDuration);
            if (line != null) line.enabled = false;
        }
    }
}
