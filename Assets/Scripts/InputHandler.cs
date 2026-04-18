using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{

    public InventoryManager inventoryManager;
    public InteractionManager interactionManager;
    public PlayerInventory playerInventory;

    public void OnInventory(InputValue value)
    {
        if(value.isPressed)
        {
            inventoryManager.ToggleInventory();
        }
    }

    public void OnInteract(InputValue value)
    {
        if(value.isPressed)
        {
            interactionManager.Interact();
        }
    }

    public void OnAttack(InputValue value)
    {
        if(value.isPressed)
        {
            playerInventory.UseItemInHand();
        }
    }
    
}
