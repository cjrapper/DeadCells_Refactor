using UnityEngine;
using System;

public class SlimeEnemy : Enemy
{
    [Header("Slime Bounce Settings")]
    public float jumpForce = 5f;
    public float jumpAmount = 0.1f;

    public override void UpdateVisuals()
    {
        // 只有在有速度时才进行缩放动画
        HandleBounce();
        // 处理翻转
        CheckFlip(rb.velocity.x);
    }

    private void HandleBounce()
    {
        if (rb.velocity.magnitude > 0.1f)
        {
            float single = MathF.Sin(Time.time * jumpForce);
            float yScale = 1 + single * jumpAmount;
            float xScale = 1 - single * jumpAmount;
            
            // 保持当前的面向方向
            float currentXScale = transform.localScale.x;
            xScale = (currentXScale > 0) ? xScale : -xScale;
            
            transform.localScale = new Vector3(xScale, yScale, 1);
        }
        else
        {
            // 停止运动时缓慢恢复正常比例
            float currentXDir = transform.localScale.x > 0 ? 1 : -1;
            Vector3 target = new Vector3(currentXDir, 1f, 1f);
            transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * 5f);
        }
    }

    protected override void Die()
    {
        // 史莱姆特有的死亡效果可以在这里添加
        base.Die();
    }
}
