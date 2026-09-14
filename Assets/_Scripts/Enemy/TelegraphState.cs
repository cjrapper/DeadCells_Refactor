using System;
using System.Collections;
using UnityEngine;

namespace DeadCells.Enemy
{
    public class TelegraphState : EnemyState
    {
        public override string AnimName => "Telegraph";
        private static readonly WaitForSeconds PopUpDelay = new WaitForSeconds(0.1f);
        private float timer;
        private Coroutine popUpCoroutine;

        public TelegraphState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

        public override void Enter()
        {
            timer = enemy.windupTime;
            enemy.rb.velocity = new Vector2(0, enemy.rb.velocity.y);
            if (enemy.player != null && enemy.alertSign != null)
            {
                enemy.alertSign.SetActive(true);
                enemy.alertSign.transform.localScale = Vector3.zero;
                if (popUpCoroutine != null) enemy.StopCoroutine(popUpCoroutine);
                popUpCoroutine = enemy.StartCoroutine(PopUp(enemy.alertSign));
            }
        }
        protected IEnumerator PopUp(GameObject alertSign)
        {
            float popUpSpeed = 5f;

            yield return PopUpDelay;

            while (alertSign != null && alertSign.transform.localScale.x < 1)
            {
                alertSign.transform.localScale += Vector3.one * popUpSpeed * Time.deltaTime;
                yield return null;
            }
        }


        public override void PhysicsUpdate()
        {
            // 蓄力期间不许移动（也不许继续跳）——
            // 史莱姆的移动是"蹦跳"，如果这里不管，它会在空中完成蓄力、边飘边扑
            enemy.StopHorizontal();
        }

        public override void LogicUpdate()
        {
            timer -= Time.deltaTime;

            // 蓄力进度 0→1：给子类做"下蹲压扁"这类预备动作
            // （蓄力可读性是动作游戏的关键 —— 只靠一个感叹号玩家反应不过来）
            float progress = enemy.windupTime > 0.01f
                ? Mathf.Clamp01(1f - timer / enemy.windupTime)
                : 1f;
            enemy.OnTelegraphTick(progress);

            // 感叹号跟着蓄力进度脉动：越接近攻击跳得越急
            if (enemy.alertSign != null && enemy.alertSign.activeSelf)
            {
                float pulse = 1f + Mathf.Sin(Time.time * Mathf.Lerp(12f, 30f, progress)) * 0.18f;
                enemy.alertSign.transform.localScale = Vector3.one * pulse;
            }

            enemy.UpdateVisuals();

            if (timer <= 0)
            {
                stateMachine.ChangeState(enemy.attackState);
            }
        }

        public override void Exit()
        {
            enemy.OnTelegraphEnd();
            if (popUpCoroutine != null)
            {
                enemy.StopCoroutine(popUpCoroutine);
                popUpCoroutine = null;
            }

            if (enemy.player != null && enemy.alertSign != null)
            {
                enemy.alertSign.SetActive(false);
            }
        }
    }
}
