using System.Collections;
using UnityEngine;

public class HoleTrigger : MoveTrigger
{
    [SerializeField] private float maxUsableAngle;
    [SerializeField] private Transform initialTransform;
    [SerializeField] private float preMoveAnimationSpeed;

    protected override bool CanBeUsed()
    {
        var triggerForward = transform.forward;
        var playerForward = playerController.transform.forward;
        return Vector3.Angle(triggerForward, playerForward) < maxUsableAngle;
    }

    protected override IEnumerator GetPreMoveAction()
    {
        var playerPosition = playerController.transform.position;
        var initialPosition = new Vector3(initialTransform.position.x, playerPosition.y, initialTransform.position.z);
        var playerRotation = playerController.transform.rotation;
        var initialRotation = initialTransform.rotation;
        var time = 0.0f;
        while (time <= 1f)
        {
            playerController.transform.position = Vector3.Lerp(playerPosition, initialPosition, time);
            playerController.transform.rotation = Quaternion.Lerp(playerRotation, initialRotation, time);
            time += Time.deltaTime * preMoveAnimationSpeed;
            yield return null;
        }
    }
}