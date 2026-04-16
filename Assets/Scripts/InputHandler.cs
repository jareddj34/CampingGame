using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{

    public InventoryManager inventoryManager;

    public void OnInventory(InputValue value)
    {
        if(value.isPressed)
        {
            inventoryManager.ToggleInventory();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Item")
        {
            other.gameObject.GetComponent<Item>().Pickup();
        }
    }
}
