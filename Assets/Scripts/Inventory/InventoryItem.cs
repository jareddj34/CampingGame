using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class InventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public TMP_Text itemAmountText;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Canvas canvas;

    private Image itemIcon;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        itemIcon = GetComponent<Image>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        canvasGroup.blocksRaycasts = false;
        rectTransform.SetParent(rectTransform.root);
    }

    public void OnDrag(PointerEventData eventData)
    {

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // If we ended over any InventorySlot (directly or via a child item), OnDrop handled it.
        if (eventData.pointerEnter != null &&
            eventData.pointerEnter.GetComponentInParent<InventorySlot>() != null)
        {
            return;
        }

        // Otherwise, snap back to where we started.
        rectTransform.SetParent(originalParent);
        SetAvailable();
    }

    public void SetAvailable()
    {
        canvasGroup.blocksRaycasts = true;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    public void Init(string itemName, Sprite itemIcon, int amount)
    {
        this.itemIcon.sprite = itemIcon;
        itemAmountText.text = amount.ToString();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.button == PointerEventData.InputButton.Right)
        {
            Debug.Log("Right Clicked");
            var inventoryManager = GetComponentInParent<InventoryManager>();
            inventoryManager.DropItem(this);
        }
    }

}
