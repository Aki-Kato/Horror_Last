using Unity.VisualScripting;
using UnityEngine;

public abstract class InteractableTrigger : MonoBehaviour, IInteractable
{
    protected PlayerController playerController;
    public abstract bool Interact();

    protected virtual void Start()
    {
        playerController = PlayerController.Instance;  
    }


    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject != playerController.gameObject)
        {
            return;
        }
        playerController.SetObjectToInteract(this);
    }
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject != playerController.gameObject)
        {
            return;
        }
        playerController.SetObjectToInteract(null);
    }

}
