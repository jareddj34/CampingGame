using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{

    public InventoryManager inventoryManager;
    public InteractionManager interactionManager;
    public PlayerInventory playerInventory;

    // public void OnInventory(InputValue value)
    // {
    //     if (!GameStateManager.Instance.IsPlayerInputEnabled && !inventoryManager.IsInventoryOpen)
    //         return;

    //     if(value.isPressed)
    //     {
    //         inventoryManager.ToggleInventory();
    //         GameStateManager.Instance.SetInputEnabled(!inventoryManager.IsInventoryOpen);
    //     }
    // }

    public void OnInteract(InputValue value)
    {
        if (!GameStateManager.Instance.IsPlayerInputEnabled)
            return;

        if(value.isPressed)
        {
            interactionManager.Interact();
        }
    }

    public void OnAttack(InputValue value)
    {
        if (!GameStateManager.Instance.IsPlayerInputEnabled)
            return;

        if(value.isPressed)
        {
            Debug.Log("Attack button pressed");
            playerInventory.UseItemInHand();
        }
        else
        {
            Debug.Log("Attack button released");
            playerInventory.ReleaseItemInHand();
        }
    }

    public void OnPause(InputValue value)
    {
        if(value.isPressed)
        {
            GameStateManager.Instance.TogglePause();
        }
    }
    
}
