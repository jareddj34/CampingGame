using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IDropHandler
{

    private InventoryItem item;
    public bool isEmpty => item == null;
    public InventoryItem Item => item;

    public void SetItem(InventoryItem item)
    {
        this.item = item;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if(eventData.pointerDrag == null)
        {
            return;
        }

        eventData.pointerDrag.transform.SetParent(transform);
        var inventoryItem = eventData.pointerDrag.GetComponent<InventoryItem>();
        inventoryItem.SetAvailable();

        var inventoryManager = GetComponentInParent<InventoryManager>();
        inventoryManager.ItemMoved(inventoryItem, this);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
