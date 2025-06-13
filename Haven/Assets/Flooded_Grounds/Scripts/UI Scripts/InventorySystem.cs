using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    public HotbarManager hotbarManager;
    public InventoryManager inventoryManager;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // This method will be called by InventorySlot.OnDrop
    public void RequestItemTransfer(InventorySlot sourceSlot, InventorySlot targetSlot)
    {
        Debug.Log($"[InventorySystem] RequestItemTransfer: Source Slot {sourceSlot.gameObject.name} (Item: {(sourceSlot.GetItem() != null ? sourceSlot.GetItem().name : "null")}) to Target Slot {targetSlot.gameObject.name} (Item: {(targetSlot.GetItem() != null ? targetSlot.GetItem().name : "null")})");

        GameObject sourceItem = sourceSlot.GetItem();
        GameObject targetItem = targetSlot.GetItem();

        // Determine source and target slot indices within their respective managers
        int sourceIndex = -1;
        bool sourceIsHotbar = false;
        for (int i = 0; i < hotbarManager.hotbarSlots.Length; i++)
        {
            if (hotbarManager.hotbarSlots[i] == sourceSlot)
            {
                sourceIndex = i;
                sourceIsHotbar = true;
                break;
            }
        }

        if (sourceIndex == -1)
        {
            for (int i = 0; i < inventoryManager.inventorySlots.Length; i++)
            {
                if (inventoryManager.inventorySlots[i] == sourceSlot)
                {
                    sourceIndex = i;
                    sourceIsHotbar = false;
                    break;
                }
            }
        }

        int targetIndex = -1;
        bool targetIsHotbar = false;
        for (int i = 0; i < hotbarManager.hotbarSlots.Length; i++)
        {
            if (hotbarManager.hotbarSlots[i] == targetSlot)
            {
                targetIndex = i;
                targetIsHotbar = true;
                break;
            }
        }

        if (targetIndex == -1)
        {
            for (int i = 0; i < inventoryManager.inventorySlots.Length; i++)
            {
                if (inventoryManager.inventorySlots[i] == targetSlot)
                {
                    targetIndex = i;
                    targetIsHotbar = false;
                    break;
                }
            }
        }

        // Perform the actual item swap/transfer in the managers' internal arrays
        if (sourceIsHotbar && targetIsHotbar) // Hotbar to Hotbar swap
        {
            Debug.Log($"[InventorySystem] Hotbar to Hotbar Swap: Source {sourceIndex}, Target {targetIndex}");
            hotbarManager.SetItem(sourceIndex, targetItem); // Put target item in source slot
            hotbarManager.SetItem(targetIndex, sourceItem); // Put source item in target slot
        }
        else if (!sourceIsHotbar && !targetIsHotbar) // Inventory to Inventory swap
        {
            Debug.Log($"[InventorySystem] Inventory to Inventory Swap: Source {sourceIndex}, Target {targetIndex}");
            inventoryManager.SetItem(sourceIndex, targetItem); // Put target item in source slot
            inventoryManager.SetItem(targetIndex, sourceItem); // Put source item in target slot
        }
        else if (sourceIsHotbar && !targetIsHotbar) // Hotbar to Inventory transfer
        {
            Debug.Log($"[InventorySystem] Hotbar to Inventory Transfer: Item {sourceItem.name} from Hotbar {sourceIndex} to Inventory {targetIndex}");
            // Move from hotbar to inventory
            hotbarManager.SetItem(sourceIndex, null); // Clear hotbar slot
            inventoryManager.SetItem(targetIndex, sourceItem); // Place in inventory

            // Deactivate and parent to hidden parent
            if (sourceItem != null)
            {
                sourceItem.SetActive(false);
                sourceItem.transform.SetParent(inventoryManager.hiddenItemsParent);
                Debug.Log($"[InventorySystem] Item {sourceItem.name} deactivated and parented to {inventoryManager.hiddenItemsParent.name}");
            }
        }
        else if (!sourceIsHotbar && targetIsHotbar) // Inventory to Hotbar transfer
        {
            Debug.Log($"[InventorySystem] Inventory to Hotbar Transfer: Item {sourceItem.name} from Inventory {sourceIndex} to Hotbar {targetIndex}");
            // Move from inventory to hotbar
            inventoryManager.SetItem(sourceIndex, null); // Clear inventory slot
            hotbarManager.SetItem(targetIndex, sourceItem); // Place in hotbar

            // Parent to handHolder, active state handled by SelectSlot
            if (sourceItem != null)
            {
                sourceItem.transform.SetParent(hotbarManager.handHolder); 
                sourceItem.transform.localPosition = Vector3.zero;
                sourceItem.transform.localRotation = Quaternion.identity;
                sourceItem.SetActive(false); // Default to inactive, SelectSlot will activate if needed
                Debug.Log($"[InventorySystem] Item {sourceItem.name} parented to {hotbarManager.handHolder.name} and set inactive (SelectSlot will activate).");
            }
        }

        // After transfer, ensure the currently selected hotbar item is correctly active
        if (hotbarManager.hotbarSlots.Length > 0 && hotbarManager.selectedSlot < hotbarManager.hotbarSlots.Length)
        {
            Debug.Log($"[InventorySystem] Re-selecting hotbar slot {hotbarManager.selectedSlot} to update active item.");
            hotbarManager.SelectSlot(hotbarManager.selectedSlot); // Re-select current hotbar slot to update active item
        }
    }

    // This method is called by InventorySlot.OnEndDrag when no valid drop target is found
    public void ReturnItemToOriginalSlot(InventorySlot itemSlot)
    {
        Debug.Log($"[InventorySystem] ReturnItemToOriginalSlot: Returning item in slot {itemSlot.gameObject.name}");
        // Find out which manager the item slot belongs to
        int slotIndex = -1;
        bool isHotbar = false;
        for (int i = 0; i < hotbarManager.hotbarSlots.Length; i++)
        {
            if (hotbarManager.hotbarSlots[i] == itemSlot)
            {
                slotIndex = i;
                isHotbar = true;
                break;
            }
        }

        if (slotIndex == -1)
        {
            for (int i = 0; i < inventoryManager.inventorySlots.Length; i++)
            {
                if (inventoryManager.inventorySlots[i] == itemSlot)
                {
                    slotIndex = i;
                    isHotbar = false;
                    break;
                }
            }
        }

        if (slotIndex != -1)
        {
            // No need to get item from slot, it's already in the manager's array from original drag state
            // We just need to refresh the visual of the slot from its manager's data
            if (isHotbar)
            {
                itemSlot.SetItem(hotbarManager.GetItem(slotIndex), hotbarManager.emptySlotSprite);
            }
            else
            {
                itemSlot.SetItem(inventoryManager.GetItem(slotIndex), inventoryManager.emptySlotSprite);
            }
        }

        // Reset the parent of the itemImage back to its original slot's transform
        itemSlot.itemImage.transform.SetParent(itemSlot.transform); 
        itemSlot.itemImage.rectTransform.anchoredPosition = Vector2.zero; // Reset position to center of slot
        Debug.Log($"[InventorySystem] Visual of {itemSlot.gameObject.name} returned to original position.");
    }

    // This method will be called after a successful or failed drag-and-drop operation
    public void PostTransferCleanup()
    {
        Debug.Log("[InventorySystem] PostTransferCleanup: Clearing itemBeingDraggedSlot.");
        // Reset the static itemBeingDraggedSlot in InventorySlot
        if (InventorySlot.itemBeingDraggedSlot != null)
        {
            // This logic is primarily handled by ReturnItemToOriginalSlot or successful transfer
        }
        InventorySlot.itemBeingDraggedSlot = null; // Clear the static reference
    }
} 