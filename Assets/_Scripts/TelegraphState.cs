using System;
using System.Collections;
using UnityEngine;

public class TelegraphState : EnemyState
{
    private float timer;
    private Coroutine popUpCoroutine;

    public TelegraphState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine) { }

    public override void Enter()
    {
        timer = enemy.windupTime;
        enemy.rb.velocity = Vector2.zero;
        if(enemy.player != null)
        {
            enemy.alertSign.SetActive(true);
            enemy.alertSign.transform.localScale = Vector3.zero;
            if (popUpCoroutine != null) enemy.StopCoroutine(popUpCoroutine);
            popUpCoroutine = enemy.StartCoroutine(PopUp(enemy.alertSign));
        }
    }
    protected IEnumerator PopUp(GameObject alertSign)
    {
        float popUpTime = 0.2f;
        float popUpSpeed = 5f;
        float popUpDelay = 0.1f;

        yield return new WaitForSeconds(popUpDelay);

        while (alertSign.transform.localScale.x < 1)
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

        if(enemy.player != null)
        {
            enemy.alertSign.SetActive(false);
        }
    }
}
