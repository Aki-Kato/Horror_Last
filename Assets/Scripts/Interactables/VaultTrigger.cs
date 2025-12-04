using System.Collections;
using UnityEngine;

public class VaultTrigger : MoveTrigger
{
    [SerializeField] private float maxUsableAngle;

    protected override bool CanBeUsed()
    {
        var triggerForward = transform.forward;
        var playerForward = playerController.transform.forward;
        return Vector3.Angle(triggerForward, playerForward) < maxUsableAngle;
    }

    protected override IEnumerator GetPreMoveAction()
    {
        return null;
    }
}
