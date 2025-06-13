using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public Image itemImage; // The Image component that displays the item's icon (should be a child of this slot)
    public static InventorySlot itemBeingDraggedSlot; 
    public GameObject currentItem; // The actual GameObject representing the item in this slot

    private RectTransform rectTransform;
    private Canvas canvas;
    private GameObject draggedIconGameObject; // The temporary GameObject that follows the mouse
    private Image draggedIconImage; // The Image component of the temporary dragged icon

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        // The itemImage should ideally be assigned in the Inspector to a child Image.
        // If not assigned, try to find a child Image or warn.
        if (itemImage == null)
        {
            itemImage = GetComponentInChildren<Image>();
            if (itemImage == null)
            {
                Debug.LogWarning($"[InventorySlot] {gameObject.name}: itemImage is not assigned and no child Image found.");
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem != null && itemImage != null && itemImage.sprite != null && itemImage.color.a > 0.1f) 
        {
            Debug.Log($"[InventorySlot] OnBeginDrag: {gameObject.name} - Dragging item: {currentItem.name}");
            itemBeingDraggedSlot = this;

            // Create a temporary GameObject for the dragged icon
            draggedIconGameObject = new GameObject("DraggedItemIcon");
            draggedIconGameObject.transform.SetParent(canvas.transform); // Parent to canvas for overlay
            draggedIconImage = draggedIconGameObject.AddComponent<Image>();
            draggedIconImage.sprite = itemImage.sprite; // Copy the sprite from the slot's item image
            draggedIconImage.color = new Color(itemImage.color.r, itemImage.color.g, itemImage.color.b, 0.6f); // Semi-transparent
            draggedIconImage.rectTransform.sizeDelta = itemImage.rectTransform.sizeDelta; // Match size
            draggedIconImage.raycastTarget = false; // Disable raycasting

            // Hide the original item image in the slot (it will be redrawn by SetItem later)
            itemImage.enabled = false; 
        }
        else
        {
            Debug.Log($"[InventorySlot] OnBeginDrag: {gameObject.name} - No item to drag or invalid state.");
            itemBeingDraggedSlot = null;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (itemBeingDraggedSlot == this && draggedIconGameObject != null)
        {
            draggedIconImage.rectTransform.position = eventData.position; // Directly set position for smooth follow
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (itemBeingDraggedSlot == this)
        {
            Debug.Log($"[InventorySlot] OnEndDrag: {gameObject.name} - Ended dragging item: {(currentItem != null ? currentItem.name : "null")}");

            // Destroy the temporary dragged icon
            if (draggedIconGameObject != null)
            {
                Destroy(draggedIconGameObject);
                draggedIconGameObject = null;
                draggedIconImage = null;
            }

            // If the item was not dropped on a valid slot, return it to its original position visually
            // The actual item GameObject will be handled by the InventorySystem based on the drop outcome
            if (eventData.pointerEnter == null || (eventData.pointerEnter != null && eventData.pointerEnter.GetComponent<InventorySlot>() == null))
            {
                // If dropped outside any valid slot, or on a non-slot UI element, return to original visual state.
                InventorySystem.Instance.ReturnItemToOriginalSlot(this);
            }
            else
            {
                // If dropped on a valid slot, the InventorySystem.RequestItemTransfer will handle updates.
                // Ensure our original slot's image is enabled, as it might have been disabled during drag.
                if (itemImage != null) itemImage.enabled = true;
            }

            // itemBeingDraggedSlot will be nullified by OnDrop if successful, or by manager if returned.
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (itemBeingDraggedSlot != null && itemBeingDraggedSlot != this)
        {
            Debug.Log($"[InventorySlot] OnDrop: {gameObject.name} - Item dropped from {itemBeingDraggedSlot.gameObject.name} to {gameObject.name}");
            // Request the InventorySystem to handle the item transfer
            InventorySystem.Instance.RequestItemTransfer(itemBeingDraggedSlot, this);

            // Make sure the original slot's image is re-enabled if it was disabled during drag
            if (itemBeingDraggedSlot.itemImage != null) itemBeingDraggedSlot.itemImage.enabled = true;

            // Reset itemBeingDraggedSlot immediately after requesting transfer
            itemBeingDraggedSlot = null;
        }
    }

    // Public method to set the item for this slot
    public void SetItem(GameObject item, Sprite emptySlotDefaultSprite = null)
    {
        currentItem = item;
        if (itemImage != null)
        {
            if (item != null)
            {
                Sprite itemIcon = null;
                ItemIconProvider iconProvider = item.GetComponent<ItemIconProvider>();
                if (iconProvider != null)
                {
                    itemIcon = iconProvider.icon;
                    Debug.Log($"[InventorySlot] SetItem: {gameObject.name} - Set to item {item.name}, icon: {(itemIcon != null ? itemIcon.name : "null")}");
                }
                else
                {
                    Debug.LogWarning($"[InventorySlot] SetItem: {gameObject.name} - Item {item.name} has no ItemIconProvider. Using null icon.");
                }
                
                itemImage.sprite = itemIcon; // Set item icon
                itemImage.color = Color.white;
                itemImage.enabled = true; // Ensure image is visible
            }
            else // Slot is empty
            {
                Debug.Log($"[InventorySlot] SetItem: {gameObject.name} - Set to empty, using emptySprite: {(emptySlotDefaultSprite != null ? emptySlotDefaultSprite.name : "null")}");
                itemImage.sprite = emptySlotDefaultSprite; // Use the provided empty slot sprite
                itemImage.color = Color.gray; // Consistent empty color
                itemImage.enabled = true; // Still enabled to show empty state
            }
        }
    }

    // Public method to get the item from this slot
    public GameObject GetItem()
    {
        return currentItem;
    }
} 