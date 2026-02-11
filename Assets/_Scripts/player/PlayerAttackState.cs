using UnityEngine;

public class PlayerAttackState : PlayerAbilityState
{
    public PlayerAttackState(PlayerController player, PlayerStateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        
        // Weapon Attack
        if (player.currentWeapon != null)
        {
            player.UpdateNextAttackTime();
            player.currentWeapon.Attack(player);
            
            // Trigger swing animation logic only if the weapon uses it
            if (player.currentWeapon.useMeleeSwing)
            {
                player.SendMessage("StartSwing", SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (Time.time >= startTime + player.swingDuration)
        {
            isAbilityDone = true;
        }
    }
}
