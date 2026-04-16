using UnityEngine;
using System.Collections.Generic;
using System;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] private List<Item> allItems = new List<Item>();

    public InventoryItem itemPrefab;
    public List<InventorySlot> slots;

    private InventoryItemData[] inventoryData;

    public Canvas inventoryCanvas;

    private void Awake()
    {
        inventoryData = new InventoryItemData[slots.Count];
    }

    public void AddItem(Item item)
    {
        // allItems.Add(item);
        if(!TryStackItem(item))
        {
            AddNewItem(item);
        }
    }

    private bool TryStackItem(Item item)
    {

        for(int i = 0; i < inventoryData.Length; i++)
        {
            var data = inventoryData[i];
            if(string.IsNullOrEmpty(data.itemName))
            {
                continue;
            }

            if(data.itemName != item.itemName)
            {
                continue;
            }

            // Found same item
            data.amount++;
            data.inventoryItem.Init(item.itemName, item.itemIcon, data.amount);
            inventoryData[i] = data;

            return true;
        }
        return false;

    }

    private void AddNewItem(Item item)
    {
        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if(slot.isEmpty)
            {
                var inventoryItem = Instantiate(itemPrefab, slot.transform);
                inventoryItem.Init(item.itemName, item.itemIcon, 1);
                var itemData = new InventoryItemData
                {
                    itemName = item.itemName,
                    inventoryItem = inventoryItem,
                    itemIcon = item.itemIcon,
                    amount = 1
                };

                inventoryData[i] = itemData;
                slot.SetItem(inventoryItem);

                return;
            }
        }
    }

    public void DropItem(InventoryItem inventoryItem)
    {
        for(int i = 0; i < inventoryData.Length; i++)
        {
            var data = inventoryData[i];
            if(data.inventoryItem == inventoryItem)
            {

                var itemToSpawn = allItems.Find(item => item.itemName == data.itemName);
                if(itemToSpawn != null)
                {
                    var itemObject = Instantiate(itemToSpawn.gameObject, PlayerController.localPlayerController.transform.position + PlayerController.localPlayerController.transform.forward + Vector3.up * 0.5f, Quaternion.identity);
                }

                DeductItem(inventoryItem);
                break;
            }
        }
    }

    private void DeductItem(InventoryItem inventoryItem)
    {
        for(int i = 0; i < inventoryData.Length; i++)
        {
            var data = inventoryData[i];
            if(data.inventoryItem != inventoryItem)
            {
                continue;
            }

            data.amount--;
            if(data.amount <= 0)
            {
                inventoryData[i] = default;
                slots[i].SetItem(null);
                Destroy(inventoryItem.gameObject);
            }
            else
            {
                data.inventoryItem.Init(data.itemName, data.itemIcon, data.amount);
                inventoryData[i] = data;
            }
        }
    }

    public void ItemMoved(InventoryItem item, InventorySlot newSlot)
    {
        var newSlotIndex = slots.IndexOf(newSlot);
        var oldSlotIndex = Array.FindIndex(inventoryData, data => data.inventoryItem == item);
        if(oldSlotIndex == -1 || newSlotIndex == -1)
        {
            return;
        }

        var oldData = inventoryData[oldSlotIndex];
        inventoryData[oldSlotIndex] = default;
        inventoryData[newSlotIndex] = oldData;

    }

    [Serializable]
    public struct InventoryItemData
    {
        public string itemName;
        public Sprite itemIcon;
        public InventoryItem inventoryItem;
        public int amount;
    }

    public void ToggleInventory()
    {
        inventoryCanvas.enabled = !inventoryCanvas.enabled;
        Cursor.lockState = inventoryCanvas.enabled ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = inventoryCanvas.enabled;
    }
}
