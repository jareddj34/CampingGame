using UnityEngine;
using System.Collections.Generic;
using System;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] private List<Item> allItems = new List<Item>();

    
    public InventoryItem itemPrefab;
    public List<InventorySlot> slots;
    public List<ActionSlot> actionSlots = new List<ActionSlot>();
    private ActionSlot activeActionSlot;
    private PlayerInventory playerInventory;

    private InventoryItemData[] inventoryData;

    public CanvasGroup inventoryGroup;

    private void Awake()
    {
        inventoryData = new InventoryItemData[slots.Count];
        inventoryGroup.alpha = 0;
        inventoryGroup.interactable = false;
        inventoryGroup.blocksRaycasts = false;

        playerInventory = FindObjectOfType<PlayerInventory>();
        PlayerInventory.localInventory = playerInventory;
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

                var itemToSpawn = GetItemByName(data.itemName);
                if(itemToSpawn != null)
                {
                    var itemObject = Instantiate(itemToSpawn.gameObject, PlayerController.localPlayerController.transform.position + PlayerController.localPlayerController.transform.forward + Vector3.up * 0.5f, Quaternion.identity);
                }

                if(DeductItem(inventoryItem) <= 0)
                {
                    PlayerInventory.localInventory.UnequipItem(itemToSpawn);
                }
                break;
            }
        }
    }

    private int DeductItem(InventoryItem inventoryItem)
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

                return 0;
            }
            else
            {
                data.inventoryItem.Init(data.itemName, data.itemIcon, data.amount);
                inventoryData[i] = data;
                return data.amount;
            }
        }

        return 0;
    }

    public void ItemMoved(InventoryItem item, InventorySlot newSlot)
    {
        var newSlotIndex = slots.IndexOf(newSlot);
        var oldSlotIndex = Array.FindIndex(inventoryData, data => data.inventoryItem == item);
        if(oldSlotIndex == -1)
        {
            return;
        }

        var oldData = inventoryData[oldSlotIndex];
        inventoryData[oldSlotIndex] = default;
        inventoryData[newSlotIndex] = oldData;

    }

    private Item GetItemByName(string itemName)
    {
        return allItems.Find(item => item.itemName == itemName);
    }

    private Item GetItemByActionSlot(ActionSlot actionSlot)
    {
        var inventorySlot = actionSlot.GetComponent<InventorySlot>();
        for (int i = slots.Count-1; i >= 0; i--)
        {
            if(slots[i] == inventorySlot)
            {
                return GetItemByName(inventoryData[i].itemName);
            }
        }

        return null;
    }

    public void SetActionSlotActive(ActionSlot actionSlot)
    {
        if(activeActionSlot == actionSlot)
        {
            return;
        }

        if(activeActionSlot != null)
        {
            PlayerInventory.localInventory.UnequipItem(GetItemByActionSlot(activeActionSlot));
            activeActionSlot.ToggleActive(false);
        }

        actionSlot.ToggleActive(true);
        activeActionSlot = actionSlot;
        
        Debug.Log($"Equipped {GetItemByActionSlot(actionSlot)?.itemName}");
        PlayerInventory.localInventory.EquipItem(GetItemByActionSlot(actionSlot));
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
        inventoryGroup.alpha = inventoryGroup.alpha == 0 ? 1 : 0;
        inventoryGroup.interactable = inventoryGroup.alpha == 1;
        inventoryGroup.blocksRaycasts = inventoryGroup.alpha == 1;
        Cursor.lockState = inventoryGroup.alpha == 1 ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = inventoryGroup.alpha == 1;
    }
}
