using UnityEngine;
using System.Linq;

public class InteractionManager : MonoBehaviour
{
    public LayerMask interactableLayer;
    public float interactDistance = 3f;

    public Camera cam;
    private AInteractable[] currentHoveredInteractables;

    public GameObject interactionPrompt; // UI element to show when looking at an interactable

    public void Awake()
    {
        cam = Camera.main;

        interactionPrompt.SetActive(false);
    }

    public void Update()
    {
        HandleHover();

    }

    public void Interact()
    {
        if(Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, interactDistance, interactableLayer))
        {
            var interactables = hit.collider.GetComponents<AInteractable>().ToList(); // add ToList() if you want to do the child gameobject thing
            interactables.AddRange(hit.collider.GetComponentsInParent<AInteractable>());
            foreach(var interactable in interactables)
            {

                if(interactable != null && interactable.CanInteract())
                {
                    interactable.Interact();
                }
            }
        }
    }

    public void HandleHover()
    {
        if(Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, interactDistance, interactableLayer))
        {
            var interactables = hit.collider.GetComponents<AInteractable>();
            
            interactionPrompt.SetActive(true); // Show prompt if looking at an interactable, hide otherwise

            if(interactables == null || interactables.Length == 0)
            {
                ClearHover();
                return;
            }

            if(currentHoveredInteractables != null && currentHoveredInteractables.Length > 0)
            {
                if(!currentHoveredInteractables[0] )
                {
                    ClearHover();
                    return;
                }

                if(hit.collider.gameObject == currentHoveredInteractables[0].gameObject)
                    return;
            }

            currentHoveredInteractables = interactables;
            foreach(var interactable in interactables)
            {
                if(interactable.CanInteract())
                {
                    interactable.OnHover();

                }
            }
        }
        else
        {
            ClearHover();
            interactionPrompt.SetActive(false);
        }
    }

    public void ClearHover()
    {
        if(currentHoveredInteractables != null && currentHoveredInteractables.Length > 0)
        {
            foreach(var interactable in currentHoveredInteractables)
            {
                if(interactable)
                {
                    interactable.OnStopHover();

                }

            }
            currentHoveredInteractables = null;
        }
    }   

}


public abstract class AInteractable : MonoBehaviour
{
    public abstract void Interact();

    public virtual void OnHover() {}

    public virtual void OnStopHover() {}

    public virtual bool CanInteract()
    {
        return true;
    }
}
