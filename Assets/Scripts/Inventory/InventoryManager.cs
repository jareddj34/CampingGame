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

    public bool IsInventoryOpen = false;

    [Header("Audio")]
    public AudioSource equipSound;


    private void Awake()
    {
        inventoryData = new InventoryItemData[slots.Count];
        inventoryGroup.alpha = 0;
        inventoryGroup.interactable = false;
        inventoryGroup.blocksRaycasts = false;

        playerInventory = FindObjectOfType<PlayerInventory>();
        PlayerInventory.localInventory = playerInventory;
    }

    private void Start()
    {
        // Set first action slot active by default
        if (actionSlots != null && actionSlots.Count > 0)
        {
            SetActionSlotActive(actionSlots[0]);
        }
    }

    private void Update()
    {

        if(GameStateManager.Instance.IsPaused)
        {
            return;
        }

        var scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f) || actionSlots.Count == 0)
        {
            return;
        }

        // Don't change hotbar while the inventory is open
        if (inventoryGroup.alpha > 0f)
        {
            return;
        }

        var currentIndex = activeActionSlot != null ? actionSlots.IndexOf(activeActionSlot) : 0;
        var direction = scroll > 0f ? 1 : -1;
        var nextIndex = (currentIndex + direction + actionSlots.Count) % actionSlots.Count;

        SetActionSlotActive(actionSlots[nextIndex]);
    }

    public void AddItem(Item item)
    {
        // allItems.Add(item);
        if(!TryStackItem(item))
        {
            AddNewItem(item);
        }

        equipSound.Play();
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

                SyncActiveSlotEquip();

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
        if (oldSlotIndex == -1 || newSlotIndex == -1 || oldSlotIndex == newSlotIndex)
        {
            return;
        }

        var oldData = inventoryData[oldSlotIndex];
        var newData = inventoryData[newSlotIndex]; // may be default (empty slot)

        // Swap data
        inventoryData[newSlotIndex] = oldData;
        inventoryData[oldSlotIndex] = newData;

        // Keep each slot's Item reference in sync with the data
        slots[newSlotIndex].SetItem(oldData.inventoryItem);
        slots[oldSlotIndex].SetItem(newData.inventoryItem); // null if dest was empty

        // If the destination slot had an item, move its UI into the old slot
        if (newData.inventoryItem != null)
        {
            newData.inventoryItem.transform.SetParent(slots[oldSlotIndex].transform);
            newData.inventoryItem.SetAvailable();
        }

        
        // --- Reconcile equipped item ---
        SyncActiveSlotEquip();
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

        if(GameStateManager.Instance.IsPaused)
        {
            return;
        }

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
        IsInventoryOpen = inventoryGroup.alpha == 1;
    }

    private void SyncActiveSlotEquip()
    {
        if (activeActionSlot == null) return;

        var shouldEquip = GetItemByActionSlot(activeActionSlot);
        var currentlyHeld = PlayerInventory.localInventory.itemInHand;

        // Already holding the right thing
        if (currentlyHeld != null && shouldEquip != null &&
            currentlyHeld.itemName == shouldEquip.itemName)
        {
            return;
        }

        if (currentlyHeld != null)
        {
            PlayerInventory.localInventory.UnequipItem(currentlyHeld);
        }

        if (shouldEquip != null)
        {
            PlayerInventory.localInventory.EquipItem(shouldEquip);
        }
    }
}
