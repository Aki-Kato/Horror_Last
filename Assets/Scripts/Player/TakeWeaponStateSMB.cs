using UnityEngine;
public sealed class TakeWeaponStateSMB : StateMachineBehaviour
{
    private PlayerController _pc;

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _pc ??= animator.GetComponent<PlayerController>();
        _pc.EndTakeWeapon();
    }
}




