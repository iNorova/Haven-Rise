using UnityEngine;
using System.Collections;

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
            // Store references to the actual GameObjects before modifying the arrays
            GameObject itemAtSourceBeforeSwap = hotbarManager.GetItem(sourceIndex);
            GameObject itemAtTargetBeforeSwap = hotbarManager.GetItem(targetIndex);

            hotbarManager.SetItem(sourceIndex, itemAtTargetBeforeSwap); // Put target item in source slot
            hotbarManager.SetItem(targetIndex, itemAtSourceBeforeSwap); // Put source item in target slot

            // Deactivate both items after they have been moved. SelectSlot will handle re-activation.
            if (itemAtSourceBeforeSwap != null)
            {
                itemAtSourceBeforeSwap.SetActive(false);
                Debug.Log($"[InventorySystem] Deactivated itemAtSourceBeforeSwap: {itemAtSourceBeforeSwap.name}");
            }
            if (itemAtTargetBeforeSwap != null)
            {
                itemAtTargetBeforeSwap.SetActive(false);
                Debug.Log($"[InventorySystem] Deactivated itemAtTargetBeforeSwap: {itemAtTargetBeforeSwap.name}");
            }
        }
        else if (!sourceIsHotbar && !targetIsHotbar) // Inventory to Inventory swap
        {
            Debug.Log($"[InventorySystem] Inventory to Inventory Swap: Source {sourceIndex}, Target {targetIndex}");
            inventoryManager.SetItem(sourceIndex, targetItem); // Put target item in source slot
            inventoryManager.SetItem(targetIndex, sourceItem); // Put source item in target slot
        }
        else if (sourceIsHotbar && !targetIsHotbar) // Hotbar to Inventory transfer/swap
        {
            Debug.Log($"[InventorySystem] Hotbar to Inventory Transfer/Swap: Item {sourceItem.name} from Hotbar {sourceIndex} to Inventory {targetIndex}");
            
            // Handle swapping if target slot has an item
            if (targetItem != null)
            {
                // Swap: Put target item in source hotbar slot
                hotbarManager.SetItem(sourceIndex, targetItem);
                
                // Explicitly update the source slot visual to ensure it's correct
                if (sourceIndex >= 0 && sourceIndex < hotbarManager.hotbarSlots.Length && hotbarManager.hotbarSlots[sourceIndex] != null)
                {
                    hotbarManager.hotbarSlots[sourceIndex].SetItem(targetItem, hotbarManager.emptySlotSprite);
                }
                
                // Handle the target item (coming from inventory to hotbar)
                if (targetItem != null)
                {
                    targetItem.transform.SetParent(hotbarManager.handHolder);
                    targetItem.transform.localPosition = Vector3.zero;
                    targetItem.transform.localRotation = Quaternion.identity;
                    targetItem.SetActive(false); // Default to inactive, SelectSlot will activate if needed
                    Debug.Log($"[InventorySystem] Swapped: Item {targetItem.name} moved from Inventory to Hotbar slot {sourceIndex}");
                }
            }
            else
            {
                // No target item, just clear the source slot and explicitly update with empty sprite
                hotbarManager.SetItem(sourceIndex, null);
                // Explicitly update the slot visual to ensure empty sprite is shown
                if (hotbarManager.hotbarSlots[sourceIndex] != null)
                {
                    hotbarManager.hotbarSlots[sourceIndex].SetItem(null, hotbarManager.emptySlotSprite);
                }
            }
            
            // Place source item in target inventory slot
            inventoryManager.SetItem(targetIndex, sourceItem);
            
            // Explicitly update the target slot visual to ensure it's correct
            if (targetIndex >= 0 && targetIndex < inventoryManager.inventorySlots.Length && inventoryManager.inventorySlots[targetIndex] != null)
            {
                inventoryManager.inventorySlots[targetIndex].SetItem(sourceItem, inventoryManager.emptySlotSprite);
            }
            
            // Handle the source item (coming from hotbar to inventory)
            if (sourceItem != null)
            {
                sourceItem.SetActive(false);
                sourceItem.transform.SetParent(inventoryManager.hiddenItemsParent);
                Debug.Log($"[InventorySystem] Item {sourceItem.name} moved from Hotbar to Inventory slot {targetIndex}");
            }
        }
        else if (!sourceIsHotbar && targetIsHotbar) // Inventory to Hotbar transfer/swap
        {
            Debug.Log($"[InventorySystem] Inventory to Hotbar Transfer/Swap: Item {sourceItem.name} from Inventory {sourceIndex} to Hotbar {targetIndex}");
            
            // Handle swapping if target slot has an item
            if (targetItem != null)
            {
                // Swap: Put target item in source inventory slot
                inventoryManager.SetItem(sourceIndex, targetItem);
                
                // Explicitly update the source slot visual to ensure it's correct
                if (sourceIndex >= 0 && sourceIndex < inventoryManager.inventorySlots.Length && inventoryManager.inventorySlots[sourceIndex] != null)
                {
                    inventoryManager.inventorySlots[sourceIndex].SetItem(targetItem, inventoryManager.emptySlotSprite);
                }
                
                // Handle the target item (coming from hotbar to inventory)
                if (targetItem != null)
                {
                    targetItem.SetActive(false);
                    targetItem.transform.SetParent(inventoryManager.hiddenItemsParent);
                    Debug.Log($"[InventorySystem] Swapped: Item {targetItem.name} moved from Hotbar to Inventory slot {sourceIndex}");
                }
            }
            else
            {
                // No target item, just clear the source slot and explicitly update with empty sprite
                inventoryManager.SetItem(sourceIndex, null);
                // Explicitly update the slot visual to ensure empty sprite is shown
                if (inventoryManager.inventorySlots[sourceIndex] != null)
                {
                    inventoryManager.inventorySlots[sourceIndex].SetItem(null, inventoryManager.emptySlotSprite);
                }
            }
            
            // Place source item in target hotbar slot
            hotbarManager.SetItem(targetIndex, sourceItem);
            
            // Explicitly update the target slot visual to ensure it's correct
            if (targetIndex >= 0 && targetIndex < hotbarManager.hotbarSlots.Length && hotbarManager.hotbarSlots[targetIndex] != null)
            {
                hotbarManager.hotbarSlots[targetIndex].SetItem(sourceItem, hotbarManager.emptySlotSprite);
            }
            
            // Handle the source item (coming from inventory to hotbar)
            if (sourceItem != null)
            {
                sourceItem.transform.SetParent(hotbarManager.handHolder); 
                sourceItem.transform.localPosition = Vector3.zero;
                sourceItem.transform.localRotation = Quaternion.identity;
                sourceItem.SetActive(false); // Default to inactive, SelectSlot will activate if needed
                Debug.Log($"[InventorySystem] Item {sourceItem.name} moved from Inventory to Hotbar slot {targetIndex}");
            }
        }

        // Explicitly update any slots that might have become empty to ensure they show the empty sprite
        // This fixes cases where slots show white instead of the proper empty sprite
        if (sourceIsHotbar && !targetIsHotbar)
        {
            // Check if source hotbar slot is now empty
            if (sourceIndex >= 0 && sourceIndex < hotbarManager.hotbarSlots.Length && 
                hotbarManager.GetItem(sourceIndex) == null && 
                hotbarManager.hotbarSlots[sourceIndex] != null && 
                hotbarManager.emptySlotSprite != null)
            {
                hotbarManager.hotbarSlots[sourceIndex].SetItem(null, hotbarManager.emptySlotSprite);
            }
            // Check if target inventory slot is now empty (shouldn't happen in swap, but check anyway)
            if (targetIndex >= 0 && targetIndex < inventoryManager.inventorySlots.Length && 
                inventoryManager.GetItem(targetIndex) == null && 
                inventoryManager.inventorySlots[targetIndex] != null && 
                inventoryManager.emptySlotSprite != null)
            {
                inventoryManager.inventorySlots[targetIndex].SetItem(null, inventoryManager.emptySlotSprite);
            }
        }
        else if (!sourceIsHotbar && targetIsHotbar)
        {
            // Check if source inventory slot is now empty
            if (sourceIndex >= 0 && sourceIndex < inventoryManager.inventorySlots.Length && 
                inventoryManager.GetItem(sourceIndex) == null && 
                inventoryManager.inventorySlots[sourceIndex] != null && 
                inventoryManager.emptySlotSprite != null)
            {
                inventoryManager.inventorySlots[sourceIndex].SetItem(null, inventoryManager.emptySlotSprite);
            }
            // Check if target hotbar slot is now empty (shouldn't happen in swap, but check anyway)
            if (targetIndex >= 0 && targetIndex < hotbarManager.hotbarSlots.Length && 
                hotbarManager.GetItem(targetIndex) == null && 
                hotbarManager.hotbarSlots[targetIndex] != null && 
                hotbarManager.emptySlotSprite != null)
            {
                hotbarManager.hotbarSlots[targetIndex].SetItem(null, hotbarManager.emptySlotSprite);
            }
        }
        
        // Update UI for both managers to reflect the changes
        hotbarManager.UpdateHotbarUI();
        inventoryManager.UpdateInventoryUI();
        
        // CRITICAL FIX: After UI updates, explicitly refresh the source inventory slot if it was involved in an inventory->hotbar swap
        // This ensures the slot visual is correctly updated, especially for slots that show white instead of proper sprites
        if (!sourceIsHotbar && targetIsHotbar)
        {
            // Start a coroutine to refresh the slot after a frame delay to ensure all updates are complete
            StartCoroutine(RefreshInventorySlotAfterDelay(sourceIndex));
            
            // Also force update immediately in case coroutine isn't needed
            RefreshInventorySlot(sourceIndex);
            
            // Also force update the target hotbar slot
            if (targetIndex >= 0 && targetIndex < hotbarManager.hotbarSlots.Length && 
                hotbarManager.hotbarSlots[targetIndex] != null &&
                hotbarManager.emptySlotSprite != null)
            {
                GameObject currentItemInSlot = hotbarManager.GetItem(targetIndex);
                hotbarManager.hotbarSlots[targetIndex].SetItem(currentItemInSlot, hotbarManager.emptySlotSprite);
            }
        }
        
        // After transfer, ensure the currently selected hotbar item is correctly active
        if (hotbarManager.hotbarSlots.Length > 0 && hotbarManager.selectedSlot < hotbarManager.hotbarSlots.Length)
        {
            Debug.Log($"[InventorySystem] Re-selecting hotbar slot {hotbarManager.selectedSlot} to update active item.");
            hotbarManager.SelectSlot(hotbarManager.selectedSlot); // Re-select current hotbar slot to update active item
        }
    }

    // Helper method to refresh a specific inventory slot
    private void RefreshInventorySlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < inventoryManager.inventorySlots.Length && 
            inventoryManager.inventorySlots[slotIndex] != null && 
            inventoryManager.emptySlotSprite != null)
        {
            GameObject currentItemInSlot = inventoryManager.GetItem(slotIndex);
            // Verify slot's currentItem matches manager's data, force sync if needed
            if (inventoryManager.inventorySlots[slotIndex].currentItem != currentItemInSlot)
            {
                Debug.LogWarning($"[InventorySystem] Slot {slotIndex} currentItem mismatch! Manager has {(currentItemInSlot != null ? currentItemInSlot.name : "null")}, Slot has {(inventoryManager.inventorySlots[slotIndex].currentItem != null ? inventoryManager.inventorySlots[slotIndex].currentItem.name : "null")}. Forcing sync.");
            }
            // Force update the slot visual with correct data
            inventoryManager.inventorySlots[slotIndex].SetItem(currentItemInSlot, inventoryManager.emptySlotSprite);
            Debug.Log($"[InventorySystem] Refreshed inventory slot {slotIndex} with item {(currentItemInSlot != null ? currentItemInSlot.name : "null")}");
        }
    }
    
    // Coroutine to refresh inventory slot after a frame delay
    private IEnumerator RefreshInventorySlotAfterDelay(int slotIndex)
    {
        // Wait for end of frame to ensure all updates are complete
        yield return new WaitForEndOfFrame();
        // Refresh the slot one more time
        RefreshInventorySlot(slotIndex);
        // Also wait one more frame and refresh again for stubborn slots
        yield return null;
        RefreshInventorySlot(slotIndex);
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