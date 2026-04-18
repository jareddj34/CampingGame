using UnityEngine;

public abstract class Item : AInteractable
{

    public string itemName;
    public Sprite itemIcon;

    public void Pickup()
    {
        Debug.Log($"Picking up {itemName}");
        var inventoryManager = FindObjectOfType<InventoryManager>();
        inventoryManager.AddItem(this);
        Destroy(gameObject);
    }

    [ContextMenu("Test Pickup")]
    private void TestPickup()
    {
        Pickup();
    }

    public override void Interact()
    {
        Pickup();
    }

    public virtual void UseItem()
    {

    }

    public virtual void ConsumeItem()
    {

    }

    public override void OnHover()
    {
        Debug.Log($"Hovering over {itemName}");
    }

    public override void OnStopHover()
    {
        Debug.Log($"Stopped hovering over {itemName}");
    }
}
