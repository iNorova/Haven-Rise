// This script manages the player's hotbar, including selecting items, picking up into hotbar slots, and dropping.
// Ensure that the 'hotbarSlots' array is populated with only the hotbar UI InventorySlot components.
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Haven.Items; // NEW: Added for GenericItemHandler

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
    private bool _isItemAnimating = false; // NEW: Flag to track if an item animation is playing
    private Animator _currentHeldItemAnimator; // NEW: Reference to the animator of the currently held item

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

        // NEW: Handle primary action (e.g., swinging axe) only if input is active AND no animation is playing
        if (_canProcessItemInput && heldItems[selectedSlot] != null && !_isItemAnimating && Input.GetMouseButtonDown(0)) // Left mouse button
        {
            // Try to get an animation handler for the current item
            _currentHeldItemAnimator = heldItems[selectedSlot].GetComponentInChildren<Animator>(); // Get Animator reference
            if (_currentHeldItemAnimator != null)
            {
                AxeAnimationHandler axeAnimHandler = heldItems[selectedSlot].GetComponentInChildren<AxeAnimationHandler>();
                if (axeAnimHandler != null)
                {
                    axeAnimHandler.PlaySwingAnimation();
                    _isItemAnimating = true; // Set flag when animation starts
                }
                else
                {
                    RockAnimationHandler rockAnimHandler = heldItems[selectedSlot].GetComponentInChildren<RockAnimationHandler>();
                    if (rockAnimHandler != null)
                    {
                        rockAnimHandler.PlaySwingAnimation();
                        _isItemAnimating = true; // Set flag when animation starts
                    }
                    // NEW: Check for WoodAnimationHandler or other item-specific handlers
                    else
                    {
                        WoodAnimationHandler woodAnimHandler = heldItems[selectedSlot].GetComponentInChildren<WoodAnimationHandler>();
                        if (woodAnimHandler != null)
                        {
                            woodAnimHandler.PlayWoodActionAnimation(); // Call the specific wood animation method
                            _isItemAnimating = true; // Set flag when animation starts
                        }
                        // Add more item-specific handlers here as needed
                        else
                        {
                            // NEW: Check for GenericItemHandler (no animation needed)
                            GenericItemHandler genericHandler = heldItems[selectedSlot].GetComponentInChildren<GenericItemHandler>();
                            if (genericHandler != null)
                            {
                                // If it's a generic item with no specific animation, we don't set _isItemAnimating to true.
                                // We can add a debug log or specific non-animation action here if needed.
                                Debug.Log($"Using generic item: {heldItems[selectedSlot].name} (no animation triggered).");
                            }
                        }
                    }
                }
            }
        }

        // NEW: Check if current item animation has finished
        if (_isItemAnimating && _currentHeldItemAnimator != null)
        {
            AnimatorStateInfo stateInfo = _currentHeldItemAnimator.GetCurrentAnimatorStateInfo(0); // Get state info from base layer
            if (stateInfo.normalizedTime >= 1.0f && !stateInfo.loop) // Check if animation has completed and is not looping
            {
                _isItemAnimating = false; // Animation finished
                ResetHeldItemPosition(); // Reset position after animation
                _currentHeldItemAnimator = null; // Clear animator reference
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

        // NEW: Prevent slot selection if an item animation is currently playing
        if (_isItemAnimating)
        {
            Debug.Log("Cannot switch slots while an item animation is playing.");
            return;
        }

        // Deactivate the previously selected item
        if (heldItems[selectedSlot] != null)
        {
            heldItems[selectedSlot].SetActive(false);
            // Clear animator reference for the deactivated item
            _currentHeldItemAnimator = null;
        }

        selectedSlot = slot;

        // Activate the newly selected item
        if (heldItems[selectedSlot] != null)
        {
            heldItems[selectedSlot].SetActive(true);
            ResetHeldItemPosition(); // NEW: Call the new method to reset position
            
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

    // NEW: Helper method to reset the held item's position and rotation
    private void ResetHeldItemPosition()
    {
        if (heldItems[selectedSlot] != null)
        {
            var offset = heldItems[selectedSlot].GetComponent<ItemHoldOffset>();
            if (offset != null)
                offset.ApplyOffset(handHolder);
            else // If no custom offset, reset to default local position/rotation relative to HandHolder
            {
                heldItems[selectedSlot].transform.SetParent(handHolder); // Ensure parenting
                heldItems[selectedSlot].transform.localPosition = Vector3.zero;
                heldItems[selectedSlot].transform.localRotation = Quaternion.identity;
            }
        }
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
                rb.AddForce(Camera.main.transform.forward * 0.5f, ForceMode.Impulse); // Reduced force for testing
            }
            heldItems[selectedSlot] = null;
            hotbarSlots[selectedSlot].SetItem(null, emptySlotSprite); // FIX: Pass the emptySlotSprite explicitly

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

    // NEW: Method to clear the currently selected hotbar slot
    public void ClearCurrentHotbarSlot()
    {
        if (selectedSlot >= 0 && selectedSlot < heldItems.Length)
        {
            // The item in the hand holder should already be destroyed by the consuming script (e.g., SeedInteraction).
            // This method's role is to clear the internal hotbar state and UI.
            heldItems[selectedSlot] = null; // Clear the reference in our array
            hotbarSlots[selectedSlot].SetItem(null, emptySlotSprite); // Update the UI to show an empty slot
            Debug.Log($"HotbarManager: Cleared item from slot {selectedSlot + 1}.");
        }
    }
} 