using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    [Header("Inventory Setup")]
    public InventorySlot[] inventorySlots; // Assign your inventory slot InventorySlot components in inspector
    public Sprite emptySlotSprite;     // Sprite for empty inventory slot
    public Transform hiddenItemsParent; // Assign an empty GameObject as a parent for inactive items

    private GameObject[] inventoryItems; // Stores the actual item GameObjects
    private const int MAX_STACK_SIZE = 12; // Maximum stack size for stackable items

    // Start is called before the first frame update
    void Start()
    {
        // Initialize the inventoryItems array based on the number of assigned UI slots
        if (inventorySlots != null && inventorySlots.Length > 0)
        {
            inventoryItems = new GameObject[inventorySlots.Length];

            // Initialize the currentItem and visuals for each inventory slot
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                inventorySlots[i].SetItem(inventoryItems[i], emptySlotSprite); // Set to null initially, with empty sprite
            }
            UpdateInventoryUI(); // Initial update to show empty slots
        }
        else
        {
            Debug.LogWarning("No inventory slots assigned to InventoryManager!");
        }

        // If hiddenItemsParent is not assigned, create one dynamically
        if (hiddenItemsParent == null)
        {
            GameObject hiddenParent = new GameObject("HiddenInventoryItems");
            hiddenParent.transform.SetParent(this.transform); // Parent to InventoryManager
            hiddenParent.transform.localPosition = Vector3.zero;
            hiddenItemsParent = hiddenParent.transform;
        }
    }

    // Method to update the visual representation of the inventory slots
    public void UpdateInventoryUI()
    {
        if (inventorySlots == null || inventoryItems == null) return;

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            // Ensure currentItem and visual are in sync, preserve stack count
            int stackCount = inventorySlots[i].GetStackCount();
            inventorySlots[i].SetItem(inventoryItems[i], emptySlotSprite, stackCount);
        }
    }

    // Helper method to check if an item is stackable (excludes axe, pickaxe, bed, campfire)
    private bool IsItemStackable(GameObject item)
    {
        if (item == null) return false;
        
        string itemName = GetItemName(item).ToLower();
        
        // Items that are NOT stackable
        if (itemName.Contains("axe") || itemName.Contains("pickaxe") || 
            itemName.Contains("bed") || itemName.Contains("campfire"))
        {
            return false;
        }
        
        return true;
    }
    
    // Helper method to get item name (from ItemIconProvider or GameObject name)
    private string GetItemName(GameObject item)
    {
        if (item == null) return "";
        
        ItemIconProvider iconProvider = item.GetComponent<ItemIconProvider>();
        if (iconProvider != null && !string.IsNullOrEmpty(iconProvider.itemName))
        {
            return iconProvider.itemName;
        }
        
        string itemName = item.name;
        if (itemName.Contains("(Clone)"))
        {
            itemName = itemName.Replace("(Clone)", "").Trim();
        }
        return itemName;
    }
    
    // Helper method to check if two items are the same type
    private bool AreItemsSameType(GameObject item1, GameObject item2)
    {
        if (item1 == null || item2 == null) return false;
        
        string name1 = GetItemName(item1);
        string name2 = GetItemName(item2);
        
        return name1 == name2;
    }
    
    // Public method to add an item to the inventory (with stacking support)
    public bool AddItem(GameObject itemToAdd)
    {
        Debug.Log($"[InventoryManager] AddItem: Attempting to add item {itemToAdd.name}");
        
        // Check if item is stackable
        bool isStackable = IsItemStackable(itemToAdd);
        
        if (isStackable)
        {
            // Try to add to existing stack first
            for (int i = 0; i < inventoryItems.Length; i++)
            {
                if (inventoryItems[i] != null && AreItemsSameType(inventoryItems[i], itemToAdd))
                {
                    int currentStackCount = inventorySlots[i].GetStackCount();
                    if (currentStackCount < MAX_STACK_SIZE)
                    {
                        // Add to existing stack
                        inventorySlots[i].SetStackCount(currentStackCount + 1);
                        
                        // Destroy the picked up item since we're stacking
                        Destroy(itemToAdd);
                        Debug.Log($"[InventoryManager] AddItem: Stacked {itemToAdd.name} in slot {i}, new count: {currentStackCount + 1}");
                        return true;
                    }
                }
            }
        }
        
        // If not stackable or no existing stack found, add to empty slot
        for (int i = 0; i < inventoryItems.Length; i++)
        {
            if (inventoryItems[i] == null)
            {
                inventoryItems[i] = itemToAdd;
                inventorySlots[i].SetItem(itemToAdd, emptySlotSprite, 1); // Set stack count to 1
                
                // Mark item as DontDestroyOnLoad so it persists across scene loads
                if (itemToAdd.scene.name != null && itemToAdd.scene.name != "DontDestroyOnLoad")
                {
                    DontDestroyOnLoad(itemToAdd);
                    Debug.Log($"[InventoryManager] AddItem: Marked {itemToAdd.name} as DontDestroyOnLoad");
                }
                
                Debug.Log($"[InventoryManager] AddItem: Successfully added {itemToAdd.name} to slot {i}");
                return true; // Item added successfully
            }
        }
        Debug.Log("Inventory is full!");
        return false; // Inventory is full
    }

    // Public method to remove an item from the inventory
    public GameObject RemoveItem(int slotIndex)
    {
        Debug.Log($"[InventoryManager] RemoveItem: Attempting to remove item from slot {slotIndex}");
        if (slotIndex >= 0 && slotIndex < inventoryItems.Length && inventoryItems[slotIndex] != null)
        {
            GameObject removedItem = inventoryItems[slotIndex];
            inventoryItems[slotIndex] = null;
            inventorySlots[slotIndex].SetItem(null, emptySlotSprite); // Clear the InventorySlot with empty sprite
            Debug.Log($"[InventoryManager] RemoveItem: Successfully removed {removedItem.name} from slot {slotIndex}");
            return removedItem;
        }
        Debug.LogWarning($"[InventoryManager] RemoveItem: No item at slot {slotIndex} or invalid index.");
        return null; // No item at this slot or invalid index
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Public method to get an item at a specific index
    public GameObject GetItem(int index)
    {
        if (index >= 0 && index < inventoryItems.Length)
        {
            return inventoryItems[index];
        }
        return null;
    }

    // Public method to set an item at a specific index
    public void SetItem(int index, GameObject item)
    {
        if (index >= 0 && index < inventoryItems.Length)
        {
            Debug.Log($"[InventoryManager] SetItem: Setting item {(item != null ? item.name : "null")} at index {index}");
            inventoryItems[index] = item;
            
            // Preserve stack count if item is the same, otherwise reset to 1
            int currentStackCount = inventorySlots[index].GetStackCount();
            if (item != null && inventorySlots[index].GetItem() != null && 
                AreItemsSameType(item, inventorySlots[index].GetItem()))
            {
                inventorySlots[index].SetItem(item, emptySlotSprite, currentStackCount);
            }
            else
            {
                inventorySlots[index].SetItem(item, emptySlotSprite, 1);
            }

            // Parenting and deactivation will be handled by InventorySystem for transfers
            // if (item != null)
            // {
            //     item.transform.SetParent(hiddenItemsParent);
            //     item.transform.localPosition = Vector3.zero;
            //     item.transform.localRotation = Quaternion.identity;
            //     item.SetActive(false); // Ensure inventory items are inactive
            // }
        }
        else
        {
            Debug.LogWarning($"[InventoryManager] SetItem: Invalid index {index} for item {(item != null ? item.name : "null")}");
        }
    }

    // Public method to be called by InventorySlot.OnDrop to transfer items
    public void TransferItem(InventorySlot sourceSlot, InventorySlot targetSlot)
    {
        // This method will be implemented in a later step to coordinate transfers
        // between HotbarManager and InventoryManager.
        // For now, the direct swap in InventorySlot.OnDrop handles the visuals.
    }

    // Drop all inventory items into the world near a center position (e.g., death spot)
    public void DropAllItems(Vector3 center)
    {
        if (inventoryItems == null || inventorySlots == null) return;
        for (int i = 0; i < inventoryItems.Length; i++)
        {
            GameObject item = inventoryItems[i];
            if (item == null) continue;

            // Unparent from hidden storage and place near center with slight random offset
            item.transform.SetParent(null);
            Vector2 offset2D = Random.insideUnitCircle * 1.5f;
            Vector3 dropPos = new Vector3(center.x + offset2D.x, center.y + 1f, center.z + offset2D.y);
            item.transform.position = dropPos;
            item.transform.rotation = Quaternion.identity;

            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.AddForce(Vector3.up * 2f, ForceMode.Impulse);
            }
            Collider col = item.GetComponent<Collider>();
            if (col != null) col.enabled = true;
            item.SetActive(true);

            // Clear slot and UI
            inventoryItems[i] = null;
            inventorySlots[i].SetItem(null, emptySlotSprite);
        }

        UpdateInventoryUI();
    }
} 