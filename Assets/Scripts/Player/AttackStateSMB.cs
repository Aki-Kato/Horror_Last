using UnityEngine;

public class AttackStateSMB : StateMachineBehaviour
{
    [Header("Combat Timing")]
    public float timingStart = 0.35f;
    public float timingEnd = 0.65f;

    PlayerController player;

    bool combatTiming;
    bool attackFinished;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        player = PlayerController.Instance;

        combatTiming = false;
        attackFinished = false;

        animator.applyRootMotion = true;

        player.SetAttackState(true);
        player.SetCombatTiming(false);
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float t = stateInfo.normalizedTime;

        // Combat timing
        bool timing = t >= timingStart && t <= timingEnd;
        if (combatTiming != timing)
        {
            combatTiming = timing;
            player.SetCombatTiming(combatTiming);
        }

        // Поворот к камере (как было)
        if (player.cameraTransform != null)
        {
            Vector3 fwd = player.cameraTransform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.0001f)
            {
                Quaternion rot = Quaternion.LookRotation(fwd.normalized, Vector3.up);
                player.transform.rotation = Quaternion.RotateTowards(
                    player.transform.rotation,
                    rot,
                    player.rotationSpeed * Time.deltaTime
                );
            }
        }

        // 🔥 ВОТ ЭТОГО НЕ ХВАТАЛО
        if (t >= 1f)
        {
            animator.SetBool(AnimatorParameters.Attack, false);
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        player.SetAttackState(false);
        player.SetCombatTiming(false);

        if (player.ConsumeNextAttackRequest())
            player.StartAttack();
    }
}
