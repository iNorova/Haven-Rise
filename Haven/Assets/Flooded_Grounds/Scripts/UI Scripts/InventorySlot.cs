using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public Image itemImage; // The Image component that displays the item's icon (should be a child of this slot)
    public Slider durabilitySlider; // Optional: Slider to show item durability (should be a child of this slot)
    public Image durabilityFillImage; // Optional: Fill image for durability slider color changes
    public TextMeshProUGUI stackCountText; // Optional: Text to display stack count (should be a child of this slot)
    public static InventorySlot itemBeingDraggedSlot; 
    public GameObject currentItem; // The actual GameObject representing the item in this slot
    public int stackCount = 1; // Number of items in this stack (1 means single item, >1 means stack)

    private RectTransform rectTransform;
    private Canvas canvas;
    private GameObject draggedIconGameObject; // The temporary GameObject that follows the mouse
    private Image draggedIconImage; // The Image component of the temporary dragged icon
    private ItemDurability currentItemDurability; // Reference to the current item's durability component

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
        
        // Try to find durability slider if not assigned
        if (durabilitySlider == null)
        {
            durabilitySlider = GetComponentInChildren<Slider>();
        }
        
        // Try to find durability fill image if not assigned
        if (durabilityFillImage == null && durabilitySlider != null)
        {
            // Look for the fill image in the slider's Fill Area
            Transform fillArea = durabilitySlider.transform.Find("Fill Area");
            if (fillArea != null)
            {
                Transform fill = fillArea.Find("Fill");
                if (fill != null)
                {
                    durabilityFillImage = fill.GetComponent<Image>();
                }
            }
        }
        
        // Try to find stack count text if not assigned
        if (stackCountText == null)
        {
            stackCountText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        // Initially hide durability slider
        if (durabilitySlider != null)
        {
            durabilitySlider.gameObject.SetActive(false);
        }
        
        // Initially hide stack count text
        if (stackCountText != null)
        {
            stackCountText.gameObject.SetActive(false);
        }
    }
    
    void Update()
    {
        // Update durability slider if item has durability
        UpdateDurabilitySlider();
        
        // Update stack count display
        UpdateStackCountDisplay();
    }
    
    // Update the durability slider based on current item's durability
    private void UpdateDurabilitySlider()
    {
        if (durabilitySlider == null) return;
        
        if (currentItem != null && currentItemDurability != null)
        {
            // Show slider and update value
            if (!durabilitySlider.gameObject.activeSelf)
            {
                durabilitySlider.gameObject.SetActive(true);
            }
            
            durabilitySlider.value = currentItemDurability.currentDurability;
            durabilitySlider.maxValue = currentItemDurability.maxDurability;
            
            // Update color based on durability percentage
            if (durabilityFillImage != null)
            {
                float durabilityPercent = currentItemDurability.GetDurabilityPercent();
                if (durabilityPercent <= 0.33f)
                {
                    durabilityFillImage.color = Color.red; // Critical
                }
                else if (durabilityPercent <= 0.66f)
                {
                    durabilityFillImage.color = Color.yellow; // Low
                }
                else
                {
                    durabilityFillImage.color = Color.green; // Good
                }
            }
        }
        else
        {
            // Hide slider if no item or item has no durability
            if (durabilitySlider.gameObject.activeSelf)
            {
                durabilitySlider.gameObject.SetActive(false);
            }
        }
    }
    
    // Update the stack count display
    private void UpdateStackCountDisplay()
    {
        if (stackCountText == null) return;
        
        if (currentItem != null && stackCount > 1)
        {
            // Show stack count text if stack is greater than 1
            if (!stackCountText.gameObject.activeSelf)
            {
                stackCountText.gameObject.SetActive(true);
            }
            stackCountText.text = stackCount.ToString();
        }
        else
        {
            // Hide stack count text if single item or no item
            if (stackCountText.gameObject.activeSelf)
            {
                stackCountText.gameObject.SetActive(false);
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem != null && itemImage != null && itemImage.sprite != null && itemImage.color.a > 0.1f) 
        {
            Debug.Log($"[InventorySlot] OnBeginDrag: {gameObject.name} - Dragging item: {currentItem.name}. Source itemImage sprite: {(itemImage.sprite != null ? itemImage.sprite.name : "NULL")}");
            itemBeingDraggedSlot = this;

            // Create a temporary GameObject for the dragged icon
            draggedIconGameObject = new GameObject("DraggedItemIcon");
            draggedIconGameObject.transform.SetParent(canvas.transform); // Parent to canvas for overlay
            draggedIconGameObject.transform.SetAsLastSibling(); // Ensure it renders on top
            draggedIconGameObject.transform.localScale = Vector3.one; // Ensure scale is 1,1,1
            draggedIconImage = draggedIconGameObject.AddComponent<Image>();
            draggedIconImage.sprite = itemImage.sprite; // Copy the sprite from the slot's item image
            draggedIconImage.color = new Color(itemImage.color.r, itemImage.color.g, itemImage.color.b, 0.6f); // Semi-transparent

            // Ensure sizeDelta is valid, provide fallback if necessary
            if (itemImage.rectTransform.sizeDelta.x > 0 && itemImage.rectTransform.sizeDelta.y > 0)
            {
                draggedIconImage.rectTransform.sizeDelta = itemImage.rectTransform.sizeDelta; // Match size
            }
            else
            {
                draggedIconImage.rectTransform.sizeDelta = new Vector2(50, 50); // Fallback to a default size
                Debug.LogWarning($"[InventorySlot] OnBeginDrag: {gameObject.name} - Original itemImage.sizeDelta was invalid ({itemImage.rectTransform.sizeDelta}). Using fallback size (50,50).");
            }
            draggedIconImage.raycastTarget = false; // Disable raycasting

            // Hide the original item image in the slot (it will be redrawn by SetItem later)
            itemImage.enabled = false; 
            
            Debug.Log($"[InventorySlot] OnBeginDrag: {gameObject.name} - Created draggedIcon. SizeDelta: {draggedIconImage.rectTransform.sizeDelta}, LocalScale: {draggedIconGameObject.transform.localScale}");
        }
        else
        {
            Debug.Log($"[InventorySlot] OnBeginDrag: {gameObject.name} - No item to drag or invalid state. CurrentItem: {(currentItem != null ? currentItem.name : "NULL")}, ItemImage: {(itemImage != null ? itemImage.name : "NULL")}, ItemImage Sprite: {(itemImage != null && itemImage.sprite != null ? itemImage.sprite.name : "NULL")}");
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
        // This method should always attempt to clean up its own dragged icon,
        // as it's called on the *source* slot when the drag operation ends.

        Debug.Log($"[InventorySlot] OnEndDrag: {gameObject.name} - Ended dragging item: {(currentItem != null ? currentItem.name : "null")}");

        // Always destroy the temporary dragged icon for the source slot
        // This must happen regardless of whether drop was successful to prevent white boxes appearing
        if (draggedIconGameObject != null)
        {
            Debug.Log($"[InventorySlot] OnEndDrag: Destroying draggedIconGameObject for {gameObject.name}.");
            // Disable first to hide it immediately before destruction
            draggedIconGameObject.SetActive(false);
            // Destroy it
            Destroy(draggedIconGameObject);
            draggedIconGameObject = null;
            draggedIconImage = null;
        }
        
        // Also check if we're still the dragged slot and clean up static reference
        if (itemBeingDraggedSlot == this)
        {
            itemBeingDraggedSlot = null;
        }

        // Ensure the original slot's item image is enabled, as it might have been disabled during drag.
        if (itemImage != null) itemImage.enabled = true;

        // Handle item return if dropped outside any valid slot
        if (eventData.pointerEnter == null || (eventData.pointerEnter != null && eventData.pointerEnter.GetComponent<InventorySlot>() == null))
        {
            InventorySystem.Instance.ReturnItemToOriginalSlot(this);
        }
        // Note: If dropped on a valid slot, InventorySystem.RequestItemTransfer handles actual GameObject movement.
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (itemBeingDraggedSlot != null && itemBeingDraggedSlot != this)
        {
            Debug.Log($"[InventorySlot] OnDrop: {gameObject.name} - Item dropped from {itemBeingDraggedSlot.gameObject.name} to {gameObject.name}");
            
            // CRITICAL: Destroy the dragged icon immediately when drop happens to prevent it from appearing elsewhere
            if (itemBeingDraggedSlot.draggedIconGameObject != null)
            {
                Debug.Log($"[InventorySlot] OnDrop: Destroying dragged icon from source slot {itemBeingDraggedSlot.gameObject.name}");
                // Disable first to hide it immediately
                itemBeingDraggedSlot.draggedIconGameObject.SetActive(false);
                // Destroy it
                Destroy(itemBeingDraggedSlot.draggedIconGameObject);
                itemBeingDraggedSlot.draggedIconGameObject = null;
                itemBeingDraggedSlot.draggedIconImage = null;
            }
            
            // Request the InventorySystem to handle the item transfer
            InventorySystem.Instance.RequestItemTransfer(itemBeingDraggedSlot, this);

            // Make sure the original slot's image is re-enabled if it was disabled during drag
            if (itemBeingDraggedSlot.itemImage != null) itemBeingDraggedSlot.itemImage.enabled = true;

            // Reset itemBeingDraggedSlot immediately after requesting transfer
            itemBeingDraggedSlot = null;
        }
    }

    // Public method to set the item for this slot
    public void SetItem(GameObject item, Sprite emptySlotDefaultSprite = null, int count = 1)
    {
        currentItem = item;
        stackCount = count;
        
        // Check if item has durability component (check item and its children)
        if (item != null)
        {
            currentItemDurability = item.GetComponent<ItemDurability>();
            // If not found on the item itself, check children (e.g., Axe child of AxeHolder)
            if (currentItemDurability == null)
            {
                currentItemDurability = item.GetComponentInChildren<ItemDurability>();
            }
        }
        else
        {
            currentItemDurability = null;
            stackCount = 1; // Reset stack count when item is null
        }
        
        if (itemImage != null)
        {
            if (item != null)
            {
                Sprite itemIcon = null;
                ItemIconProvider iconProvider = item.GetComponent<ItemIconProvider>();
                if (iconProvider != null)
                {
                    itemIcon = iconProvider.icon;
                    Debug.Log($"[InventorySlot] SetItem: {gameObject.name} - Set to item {item.name}, icon from ItemIconProvider: {(itemIcon != null ? itemIcon.name : "NULL")}");
                }
                else
                {
                    Debug.LogWarning($"[InventorySlot] SetItem: {gameObject.name} - Item {item.name} has no ItemIconProvider. Using null icon.");
                }
                
                itemImage.type = Image.Type.Simple; // Force Image Type to Simple for proper display
                itemImage.sprite = itemIcon; // Set item icon
                itemImage.color = Color.white;
                itemImage.enabled = true; // Ensure image is visible
            }
            else // Slot is empty
            {
                Debug.Log($"[InventorySlot] SetItem: {gameObject.name} - Set to empty. Empty sprite: {(emptySlotDefaultSprite != null ? emptySlotDefaultSprite.name : "NULL")}");
                itemImage.sprite = emptySlotDefaultSprite; // Use the provided empty slot sprite
                itemImage.color = Color.gray; // Consistent empty color
                itemImage.enabled = true; // Still enabled to show empty state
            }
        }
        
        // Update durability slider immediately
        UpdateDurabilitySlider();
        
        // Update stack count display immediately
        UpdateStackCountDisplay();
    }

    // Public method to get the item from this slot
    public GameObject GetItem()
    {
        return currentItem;
    }
    
    // Public method to get the stack count
    public int GetStackCount()
    {
        return stackCount;
    }
    
    // Public method to set the stack count
    public void SetStackCount(int count)
    {
        stackCount = count;
        UpdateStackCountDisplay();
    }

    // Public method to clean up dragged icon (for use by external drop targets)
    public void CleanupDraggedIcon()
    {
        if (draggedIconGameObject != null)
        {
            draggedIconGameObject.SetActive(false);
            Destroy(draggedIconGameObject);
            draggedIconGameObject = null;
            draggedIconImage = null;
        }
        if (itemImage != null) itemImage.enabled = true;
    }
} 