// This script manages the player's hotbar, including selecting items, picking up into hotbar slots, and dropping.
// Ensure that the 'hotbarSlots' array is populated with only the hotbar UI InventorySlot components.
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HotbarManager : MonoBehaviour
{
    [Header("Hotbar Setup")]
    // public int maxSlots = 5; // Max slots will now be determined by the array length
    public Transform handHolder; // Assign in inspector
    public InventorySlot[] hotbarSlots;   // Assign your hotbar UI InventorySlot components in inspector
    public Sprite emptySlotSprite; // Sprite for empty slot

    private GameObject[] heldItems;
    // private Sprite[] itemIcons; // Item icons will be managed by InventorySlot itself
    public int selectedSlot = 0; // Made public for InventorySystem access

    private bool _canProcessItemInput = true; // New flag to control item input processing

    void Start()
    {
        // Initialize heldItems array based on the number of hotbar slots assigned in the Inspector
        if (hotbarSlots != null && hotbarSlots.Length > 0)
        {
            heldItems = new GameObject[hotbarSlots.Length];
            // itemIcons = new Sprite[maxSlots]; // No longer needed

            // Initialize the currentItem and visuals for each hotbar slot
            for (int i = 0; i < hotbarSlots.Length; i++)
            {
                hotbarSlots[i].SetItem(heldItems[i], emptySlotSprite); // Set to null initially, with empty sprite
            }
            UpdateHotbarUI();
        }
        else
        {
            Debug.LogWarning("No hotbar slots assigned to HotbarManager!");
        }
    }

    void Update()
    {
        // Select slot with 1-X (where X is hotbarSlots.Length)
        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                SelectSlot(i);
        }

        // Pickup with F
        if (Input.GetKeyDown(KeyCode.F))
            TryPickupItem();

        // Drop with Q
        if (Input.GetKeyDown(KeyCode.Q))
            DropSelectedItem();

        // NEW: Handle primary action (e.g., swinging axe) only if input is active
        if (_canProcessItemInput && heldItems[selectedSlot] != null && Input.GetMouseButtonDown(0)) // Left mouse button
        {
            // Try to get an animation handler for the current item
            AxeAnimationHandler axeAnimHandler = heldItems[selectedSlot].GetComponentInChildren<AxeAnimationHandler>();
            if (axeAnimHandler != null)
            {
                axeAnimHandler.PlaySwingAnimation();
            }
            else
            {
                // NEW: Check for RockAnimationHandler
                RockAnimationHandler rockAnimHandler = heldItems[selectedSlot].GetComponentInChildren<RockAnimationHandler>();
                if (rockAnimHandler != null)
                {
                    rockAnimHandler.PlaySwingAnimation();
                }
                // Add more item-specific handlers here as needed
            }
        }

        // Live update the offset for the currently held item
        if (heldItems[selectedSlot] != null)
        {
            var offset = heldItems[selectedSlot].GetComponent<ItemHoldOffset>();
            if (offset != null)
                offset.ApplyOffset(handHolder);
        }
    }

    void TryPickupItem()
    {
        // Only pick up if there is an empty slot
        int slot = FindFirstEmptySlot();
        if (slot == -1)
        {
            Debug.Log("No empty hotbar slot available!");
            return;
        }

        RaycastHit hit;
        float sphereRadius = 0.5f;
        float pickupRange = 5f;
        if (Physics.SphereCast(Camera.main.transform.position, sphereRadius, Camera.main.transform.forward, out hit, pickupRange))
        {
            if (hit.collider.CompareTag("Pickupable"))
            {
                GameObject itemToPickUp = hit.collider.gameObject;

                // NEW CHECK: Ensure the item is not already in our hotbar
                for (int i = 0; i < hotbarSlots.Length; i++)
                {
                    if (heldItems[i] == itemToPickUp)
                    {
                        Debug.Log($"Item {itemToPickUp.name} is already in hotbar slot {i}. Not picking up again.");
                        return; // Item is already in the hotbar, do nothing
                    }
                }

                Debug.Log($"Found pickupable: {itemToPickUp.name}, first empty slot: {slot}");
                PickupItem(itemToPickUp, slot);
            }
        }
    }

    int FindFirstEmptySlot()
    {
        for (int i = 0; i < hotbarSlots.Length; i++)
            if (heldItems[i] == null)
                return i;
        return -1;
    }

    void PickupItem(GameObject item, int slot)
    {
        Debug.Log($"Picking up {item.name} into slot {slot}");
        heldItems[slot] = item;
        hotbarSlots[slot].SetItem(item); // Update the InventorySlot with the actual item

        // Set parenting and active state in SelectSlot
        // item.transform.SetParent(handHolder);
        // item.transform.localPosition = Vector3.zero;
        // item.transform.localRotation = Quaternion.identity;
        // item.SetActive(slot == selectedSlot);

        // UpdateHotbarUI(); // Redundant now as SetItem updates individually
    }

    public void SelectSlot(int slot) // Made public for InventorySystem access
    {
        Debug.Log($"Selecting slot {slot}");
        if (slot < 0 || slot >= hotbarSlots.Length) return;

        // Deactivate the previously selected item
        if (heldItems[selectedSlot] != null)
        {
            heldItems[selectedSlot].SetActive(false);
        }

        selectedSlot = slot;

        // Activate the newly selected item
        if (heldItems[selectedSlot] != null)
        {
            heldItems[selectedSlot].SetActive(true);

            // Re-apply the ItemHoldOffset to ensure correct position/rotation on activation
            var offset = heldItems[selectedSlot].GetComponent<ItemHoldOffset>();
            if (offset != null)
                offset.ApplyOffset(handHolder);
            else // If no custom offset, reset to default local position/rotation relative to HandHolder
            {
                heldItems[selectedSlot].transform.SetParent(handHolder); // Ensure parenting
                heldItems[selectedSlot].transform.localPosition = Vector3.zero;
                heldItems[selectedSlot].transform.localRotation = Quaternion.identity;
            }

            // Force the Animator to its Idle state and reset all its triggers for a clean start
            Animator itemAnimator = heldItems[selectedSlot].GetComponentInChildren<Animator>();
            if (itemAnimator != null)
            {
                // Reset all triggers first to clear any pending ones
                foreach (var param in itemAnimator.parameters)
                {
                    if (param.type == AnimatorControllerParameterType.Trigger)
                    {
                        itemAnimator.ResetTrigger(param.name);
                        Debug.Log($"Resetting trigger {param.name} for {heldItems[selectedSlot].name} on select.");
                    }
                }
                // Now, force it to the Idle state. This will override any immediate transitions.
                itemAnimator.Play("Idle", 0, 0f); // Play "Idle" state on base layer (0), from start (0f)
            }
        }

        // UpdateHotbarUI(); // Redundant now as SetItem updates individually
    }

    public void DropSelectedItem()
    {
        if (heldItems[selectedSlot] != null)
        {
            GameObject item = heldItems[selectedSlot];
            item.SetActive(true); // Make sure it's active before dropping
            item.transform.SetParent(null); // Unparent from hand
            // Add drop logic (e.g., throw forward)
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.AddForce(Camera.main.transform.forward * 5f, ForceMode.Impulse);
            }
            heldItems[selectedSlot] = null;
            hotbarSlots[selectedSlot].SetItem(null); // Clear the InventorySlot and its visual

            // UpdateHotbarUI(); // Redundant now as SetItem updates individually
        }
    }

    public void UpdateHotbarUI() // Made public for InventoryUIManager access
    {
        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            hotbarSlots[i].SetItem(heldItems[i], emptySlotSprite); // Ensure currentItem and visual are in sync
        }
    }

    // Public method to be called by InventorySlot.OnDrop to transfer items
    public void TransferItem(InventorySlot sourceSlot, InventorySlot targetSlot)
    {
        // This method will be implemented in a later step to coordinate transfers
        // between HotbarManager and InventoryManager.
        // For now, the direct swap in InventorySlot.OnDrop handles the visuals.
    }

    // New: Public method to get an item at a specific index
    public GameObject GetItem(int index)
    {
        if (index >= 0 && index < heldItems.Length)
        {
            return heldItems[index];
        }
        return null;
    }

    // New: Public method to set an item at a specific index
    public void SetItem(int index, GameObject item)
    {
        if (index >= 0 && index < heldItems.Length)
        {
            Debug.Log($"[HotbarManager] SetItem: Setting item {(item != null ? item.name : "null")} at index {index}");
            heldItems[index] = item;
            hotbarSlots[index].SetItem(item, emptySlotSprite); // Update the slot's visual and currentItem
            // Parenting and activation will be handled by SelectSlot for equipped items, or InventorySystem for transfers
        }
        else
        {
            Debug.LogWarning($"[HotbarManager] SetItem: Invalid index {index} for item {(item != null ? item.name : "null")}");
        }
    }

    public void SetInputActive(bool active)
    {
        _canProcessItemInput = active;
        Debug.Log($"[HotbarManager] SetInputActive: Input processing set to {active}");
    }
} 