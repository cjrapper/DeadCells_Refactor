using System;
using System.Collections;
using UnityEngine;

public class TelegraphState : EnemyState
{
    private static readonly WaitForSeconds PopUpDelay = new WaitForSeconds(0.1f);
    private float timer;
    private Coroutine popUpCoroutine;

    public TelegraphState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

    public override void Enter()
    {
        timer = enemy.windupTime;
        enemy.rb.velocity = new Vector2(0, enemy.rb.velocity.y);
        if(enemy.player != null && enemy.alertSign != null)
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


    public override void LogicUpdate()
    {
        timer -= Time.deltaTime;
        
        enemy.UpdateVisuals();

        if (timer <= 0)
        {
            stateMachine.ChangeState(enemy.attackState);
        }
    }

    public override void Exit()
    {
        if (popUpCoroutine != null)
        {
            enemy.StopCoroutine(popUpCoroutine);
            popUpCoroutine = null;
        }

        if(enemy.player != null && enemy.alertSign != null)
        {
            enemy.alertSign.SetActive(false);
        }
    }
}
