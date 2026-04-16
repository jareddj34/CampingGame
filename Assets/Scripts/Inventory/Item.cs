using UnityEngine;

public class Item : MonoBehaviour
{

    public string itemName;
    public Sprite itemIcon;

    public void Pickup()
    {
        var inventoryManager = FindObjectOfType<InventoryManager>();
        inventoryManager.AddItem(this);
        Destroy(gameObject);
    }

    [ContextMenu("Test Pickup")]
    private void TestPickup()
    {
        Pickup();
    }
}
