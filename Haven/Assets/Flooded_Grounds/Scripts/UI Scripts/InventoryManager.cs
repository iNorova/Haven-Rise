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
            // Ensure currentItem and visual are in sync
            inventorySlots[i].SetItem(inventoryItems[i], emptySlotSprite);
        }
    }

    // Public method to add an item to the inventory
    public bool AddItem(GameObject itemToAdd)
    {
        Debug.Log($"[InventoryManager] AddItem: Attempting to add item {itemToAdd.name}");
        for (int i = 0; i < inventoryItems.Length; i++)
        {
            if (inventoryItems[i] == null)
            {
                inventoryItems[i] = itemToAdd;
                inventorySlots[i].SetItem(itemToAdd); // Update the InventorySlot with the actual item
                
                // Parenting and deactivation will be handled by InventorySystem for transfers
                // if (itemToAdd != null)
                // {
                //     itemToAdd.transform.SetParent(hiddenItemsParent);
                //     itemToAdd.transform.localPosition = Vector3.zero;
                //     itemToAdd.transform.localRotation = Quaternion.identity;
                //     itemToAdd.SetActive(false);
                // }
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
            inventorySlots[slotIndex].SetItem(null); // Clear the InventorySlot and its visual
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
            inventorySlots[index].SetItem(item, emptySlotSprite); // Update the slot's visual and currentItem

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