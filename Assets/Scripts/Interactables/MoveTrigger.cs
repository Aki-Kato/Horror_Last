using System.Collections;
using UnityEngine;

public abstract class MoveTrigger : InteractableTrigger
{
    [SerializeField] private AnimationClip moveAnimation;

    public override bool Interact()
    {
        if (!CanBeUsed()) return false;
        var preMoveAction = GetPreMoveAction();
        playerController.DoInteractionMove(moveAnimation, preMoveAction);
        playerController.SetObjectToInteract(null);
        return true;
    }
    protected abstract bool CanBeUsed();

    protected abstract IEnumerator GetPreMoveAction();
}
