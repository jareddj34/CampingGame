using UnityEngine;
using UnityEngine.UI;

public class ActionSlot : MonoBehaviour
{
    public Image slotImage;
    public KeyCode actionKey;
    public Color activeColor;
    private Color originalColor;

    public Image selectedSlotImage; // Reference to the image that indicates the selected slot

    void Awake()
    {
        originalColor = slotImage.color;
    }

    void Update()
    {
        if(!Input.GetKeyDown(actionKey))
        {
            return;
        }

        var inventoryManager = GetComponentInParent<InventoryManager>();
        inventoryManager.SetActionSlotActive(this);
    }

    public void ToggleActive(bool toggle)
    {
        // slotImage.color = toggle ? activeColor : originalColor;
        if (selectedSlotImage != null)
        {
            selectedSlotImage.enabled = toggle;
        }
    }
}
