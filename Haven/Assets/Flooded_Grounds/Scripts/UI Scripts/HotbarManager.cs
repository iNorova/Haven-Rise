using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Haven.Items; // NEW: Added for GenericItemHandler
using TMPro; // NEW: Added for TextMeshProUGUI

public class HotbarManager : MonoBehaviour
{
    [Header("Hotbar Setup")]
    // public int maxSlots = 5; // Max slots will now be determined by the array length
    public Transform handHolder; // Assign in inspector
    public InventorySlot[] hotbarSlots;   // Assign your hotbar UI InventorySlot components in inspector
    public Sprite emptySlotSprite; // Sprite for empty slot
    public TextMeshProUGUI selectedItemNameText; // Changed from Text to TextMeshProUGUI
	[Header("Inventory Fallback")]
	public InventoryManager inventoryManager; // Assign to allow pickup into inventory when hotbar is full

    [Header("Audio")]
    public AudioSource sfxSource; // Optional; if null we will use PlayClipAtPoint
    public AudioClip pickupSfx;   // Assign a pickup sound from Sound Effects
    [Range(0f,1f)] public float pickupSfxVolume = 0.85f;

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

            // Update the display for the initially selected item
            UpdateSelectedItemNameDisplay();
        }
        else
        {
            Debug.LogWarning("No hotbar slots assigned to HotbarManager!");
        }
        
        // Auto-resolve InventoryManager if not assigned
        if (inventoryManager == null)
        {
            inventoryManager = FindObjectOfType<InventoryManager>();
            if (inventoryManager == null)
            {
                Debug.LogWarning("[HotbarManager] Could not find InventoryManager in scene. Pickup-to-inventory fallback will be disabled.");
            }
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
                    // Try Pickaxe handler (uses the same "Swing" trigger)
                    PickaxeAnimationHandler pickaxeAnimHandler = heldItems[selectedSlot].GetComponentInChildren<PickaxeAnimationHandler>();
                    if (pickaxeAnimHandler != null)
                    {
                        pickaxeAnimHandler.PlaySwingAnimation();
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
		int slot = FindFirstEmptySlot(); // -1 if hotbar full

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 cameraPos = cam.transform.position;
        Vector3 cameraForward = cam.transform.forward;
        float pickupRange = 5f;
        float sphereRadius = 0.5f;

        // Use OverlapSphere to find all nearby pickupable items
        Collider[] nearbyColliders = Physics.OverlapSphere(cameraPos + cameraForward * (pickupRange * 0.5f), sphereRadius + pickupRange * 0.5f);
        
        List<GameObject> validPickupables = new List<GameObject>();
        
        foreach (Collider col in nearbyColliders)
        {
            if (!col.CompareTag("Pickupable")) continue;
            
            GameObject item = col.gameObject;
            
            // Skip if item is already in hotbar
            bool isHeld = false;
            for (int i = 0; i < hotbarSlots.Length; i++)
            {
                if (heldItems[i] == item)
                {
                    isHeld = true;
                    break;
                }
            }
            if (isHeld) continue;
            
            // Skip if item is currently selected and active in hand
            if (heldItems[selectedSlot] == item && item.activeSelf) continue;
            
            // Check if item is within pickup range and roughly in front of camera
            Vector3 toItem = (item.transform.position - cameraPos);
            float distance = toItem.magnitude;
            if (distance > pickupRange) continue;
            
            // Check if item is in front of camera (dot product check)
            float dot = Vector3.Dot(cameraForward.normalized, toItem.normalized);
            if (dot < 0.3f) continue; // Only pick up items roughly in front (30 degree cone)
            
            // Also do a raycast to make sure nothing is blocking (but ignore held items and trees)
            RaycastHit hit;
            Vector3 rayDirection = toItem.normalized;
            float rayDistance = distance;
            
            // Raycast but ignore held items and certain layers
            int layerMask = ~(LayerMask.GetMask("Ignore Raycast")); // Exclude ignore raycast layer
            if (Physics.Raycast(cameraPos, rayDirection, out hit, rayDistance, layerMask))
            {
                // If we hit the item itself, it's valid
                if (hit.collider.gameObject == item)
                {
                    validPickupables.Add(item);
                }
                // If we hit something else, check if it's a tree/structure that we can pick through
                else if (hit.collider.CompareTag("Pickupable") || hit.collider.CompareTag("Destroyable"))
                {
                    // Allow picking through destroyable objects (trees, etc.)
                    // But prefer the closer item
                    validPickupables.Add(item);
                }
                // Otherwise, something solid is blocking - skip this item
            }
            else
            {
                // No hit, item should be visible
                validPickupables.Add(item);
            }
        }
        
        // Sort by distance to camera (closest first)
        if (validPickupables.Count > 0)
        {
            validPickupables.Sort((a, b) => 
            {
                float distA = Vector3.Distance(cameraPos, a.transform.position);
                float distB = Vector3.Distance(cameraPos, b.transform.position);
                return distA.CompareTo(distB);
            });
            
            GameObject itemToPickUp = validPickupables[0];

			if (slot != -1)
			{
				Debug.Log($"Found pickupable: {itemToPickUp.name}, first empty hotbar slot: {slot}");
				PickupItem(itemToPickUp, slot);
			}
			else
			{
				// Hotbar full – try to place directly into inventory
				if (TryPickupIntoInventory(itemToPickUp))
				{
					Debug.Log($"Picked up {itemToPickUp.name} into inventory (hotbar full).");
				}
				else
				{
					Debug.Log("Hotbar and inventory are full. Cannot pick up item.");
				}
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

    public void PickupItem(GameObject item, int slot)
    {
        Debug.Log($"Picking up {item.name} into slot {slot}");
        heldItems[slot] = item;
        hotbarSlots[slot].SetItem(item); // Update the InventorySlot with the actual item

        // Mark item as DontDestroyOnLoad so it persists across scene loads
        if (item.scene.name != null && item.scene.name != "DontDestroyOnLoad")
        {
            DontDestroyOnLoad(item);
            Debug.Log($"[HotbarManager] PickupItem: Marked {item.name} as DontDestroyOnLoad");
        }

        // Parent the item to the hand holder immediately
        item.transform.SetParent(handHolder);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        // Ensure physics is disabled while held
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false; // Disable gravity while held
            Debug.Log($"[HotbarManager] PickupItem: {item.name} Rigidbody set to Kinematic and Gravity OFF."); // Debug Log
        }

        // Disable ALL colliders (items can have multiple colliders)
        Collider[] allColliders = item.GetComponents<Collider>();
        foreach (Collider col in allColliders)
        {
            if (col != null)
            {
                col.enabled = false; // Disable collider to prevent self-collision and raycast interference
            }
        }
        
        // Also disable colliders in children
        Collider[] childColliders = item.GetComponentsInChildren<Collider>();
        foreach (Collider col in childColliders)
        {
            if (col != null)
            {
                col.enabled = false;
            }
        }
        
        if (allColliders.Length > 0 || childColliders.Length > 0)
        {
            Debug.Log($"[HotbarManager] PickupItem: {item.name} - Disabled {allColliders.Length + childColliders.Length} collider(s).");
        }
        
        // Move to Ignore Raycast layer to prevent interference with pickup detection
        item.layer = LayerMask.NameToLayer("Ignore Raycast");
        
        item.SetActive(false); // Item will be activated in SelectSlot when its slot is chosen

        // After picking up, ensure the item name display is updated
        UpdateSelectedItemNameDisplay();

        // Play pickup SFX
        PlayPickupSfx();

        // After picking up, ensure the item is selected if it's in the currently active slot
        // or if it's the first item being picked up into the default selected slot.
        if (slot == selectedSlot)
        {
            SelectSlot(selectedSlot);
        }
    }

	bool TryPickupIntoInventory(GameObject item)
	{
		if (inventoryManager == null)
		{
			Debug.LogWarning("InventoryManager not assigned on HotbarManager; cannot pick up into inventory.");
			return false;
		}

		// Attempt to add to inventory; AddItem returns false if full
		bool added = inventoryManager.AddItem(item);
		if (!added) return false;

		// Mark item as DontDestroyOnLoad so it persists across scene loads
		if (item.scene.name != null && item.scene.name != "DontDestroyOnLoad")
		{
			DontDestroyOnLoad(item);
			Debug.Log($"[HotbarManager] TryPickupIntoInventory: Marked {item.name} as DontDestroyOnLoad");
		}

		// Parent to hidden items and deactivate for storage
		if (inventoryManager.hiddenItemsParent != null)
		{
			item.transform.SetParent(inventoryManager.hiddenItemsParent);
			item.transform.localPosition = Vector3.zero;
			item.transform.localRotation = Quaternion.identity;
		}

		Rigidbody rb2 = item.GetComponent<Rigidbody>();
		if (rb2 != null)
		{
			rb2.isKinematic = true;
			rb2.useGravity = false;
		}
		// Disable ALL colliders (including children)
		Collider[] allColliders2 = item.GetComponentsInChildren<Collider>();
		foreach (Collider col in allColliders2)
		{
			if (col != null) col.enabled = false;
		}
		// Move to Ignore Raycast layer
		item.layer = LayerMask.NameToLayer("Ignore Raycast");
		item.SetActive(false);

        // Play pickup SFX (for inventory fallback case)
        PlayPickupSfx();
		return true;
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

        // Deactivate the previously selected item and handle its physics
        if (heldItems[selectedSlot] != null)
        {
            heldItems[selectedSlot].SetActive(false);
            // Clear animator reference for the deactivated item
            _currentHeldItemAnimator = null;

            // Ensure physics is disabled while stored in inventory (not active)
            Rigidbody prevRb = heldItems[selectedSlot].GetComponent<Rigidbody>();
            if (prevRb != null)
            {
                prevRb.isKinematic = true; // Keep kinematic while stored
                prevRb.useGravity = false;
                Debug.Log($"[HotbarManager] SelectSlot: {heldItems[selectedSlot].name} (prev) Rigidbody set to Kinematic and Gravity OFF."); // Debug Log
            }
            // Disable ALL colliders on previous item (including children)
            Collider[] prevColliders = heldItems[selectedSlot].GetComponentsInChildren<Collider>();
            foreach (Collider col in prevColliders)
            {
                if (col != null) col.enabled = false;
            }
            // Ensure it's on Ignore Raycast layer
            heldItems[selectedSlot].layer = LayerMask.NameToLayer("Ignore Raycast");
            if (prevColliders.Length > 0)
            {
                Debug.Log($"[HotbarManager] SelectSlot: {heldItems[selectedSlot].name} (prev) - Disabled {prevColliders.Length} collider(s).");
            }
        }

        selectedSlot = slot;

        // Activate the newly selected item and handle its physics
        if (heldItems[selectedSlot] != null)
        {
            heldItems[selectedSlot].SetActive(true);
            
            // Special handling for beds - ensure BedPlacement initializes properly
            BedPlacement bedPlacement = heldItems[selectedSlot].GetComponent<BedPlacement>();
            
            // Check if this is a bed (by name, even if BedPlacement is missing)
            string itemName = heldItems[selectedSlot].name.ToLower();
            bool isBed = itemName.Contains("bed");
            
            // If no BedPlacement found but this is a bed, try to add it
            // (This handles cases where the bed was restored but BedPlacement wasn't added properly)
            if (bedPlacement == null && isBed)
            {
                Debug.LogWarning($"[HotbarManager] Bed '{heldItems[selectedSlot].name}' selected but missing BedPlacement component. Adding it now...");
                bedPlacement = heldItems[selectedSlot].AddComponent<BedPlacement>();
                if (bedPlacement != null)
                {
                    bedPlacement.InitializeReferences();
                    bedPlacement.enabled = true;
                    Debug.Log($"[HotbarManager] SelectSlot: Added and initialized BedPlacement for '{heldItems[selectedSlot].name}'");
                }
            }
            
            // If BedPlacement exists, initialize it and clean up the name
            if (bedPlacement != null)
            {
                bedPlacement.InitializeReferences();
                Debug.Log($"[HotbarManager] SelectSlot: Initialized BedPlacement for '{heldItems[selectedSlot].name}'");
                
                // Also clean up the name if it still has "_Placed" suffix (happens if name cleanup failed during pickup)
                string currentName = heldItems[selectedSlot].name;
                if (currentName.Contains("_Placed") || currentName.Contains("_placed"))
                {
                    string cleanedName = currentName.Replace("_Placed", "").Replace("_placed", "").Trim();
                    heldItems[selectedSlot].name = cleanedName;
                    Debug.Log($"[HotbarManager] SelectSlot: Cleaned bed name from '{currentName}' to '{cleanedName}'");
                }
            }
            
            ResetHeldItemPosition(); // Ensure correct position/rotation in hand
            
            Rigidbody newRb = heldItems[selectedSlot].GetComponent<Rigidbody>();
            if (newRb != null)
            {
                newRb.isKinematic = true; // Set kinematic when active in hand
                newRb.useGravity = false;
                Debug.Log($"[HotbarManager] SelectSlot: {heldItems[selectedSlot].name} (new) Rigidbody set to Kinematic and Gravity OFF."); // Debug Log
            }
            // Disable ALL colliders on newly selected item (including children)
            Collider[] newColliders = heldItems[selectedSlot].GetComponentsInChildren<Collider>();
            foreach (Collider col in newColliders)
            {
                if (col != null) col.enabled = false;
            }
            // Ensure it's on Ignore Raycast layer to prevent pickup interference
            heldItems[selectedSlot].layer = LayerMask.NameToLayer("Ignore Raycast");
            if (newColliders.Length > 0)
            {
                Debug.Log($"[HotbarManager] SelectSlot: {heldItems[selectedSlot].name} (new) - Disabled {newColliders.Length} collider(s).");
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

        // Update the display for the newly selected item
        UpdateSelectedItemNameDisplay();
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
        if (heldItems[selectedSlot] == null)
        {
            Debug.Log("No item to drop in the selected slot.");
            return;
        }

        GameObject itemToDrop = heldItems[selectedSlot];
        Debug.Log($"Dropping {itemToDrop.name} from slot {selectedSlot}");

        // Remove from hotbar array
        heldItems[selectedSlot] = null;

        // Update the hotbar UI to show the slot as empty
        hotbarSlots[selectedSlot].SetItem(null, emptySlotSprite);

        // Ensure the item name display is updated after dropping
        UpdateSelectedItemNameDisplay();

        // Reset parent and enable physics for dropping
        itemToDrop.transform.SetParent(null); // Unparent from hand holder

        Rigidbody rb = itemToDrop.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false; // Enable physics
            rb.useGravity = true; // Enable gravity
            rb.AddForce(Camera.main.transform.forward * 5f, ForceMode.Impulse); // Add a forward force
            Debug.Log($"[HotbarManager] DropSelectedItem: {itemToDrop.name} Rigidbody set to NOT Kinematic and Gravity ON."); // Debug Log
        }

        // Enable ALL colliders (including children) when dropping
        Collider[] allDropColliders = itemToDrop.GetComponentsInChildren<Collider>();
        foreach (Collider col in allDropColliders)
        {
            if (col != null) col.enabled = true;
        }
        // Restore to Default layer (or Pickup layer if it exists)
        int pickupLayer = LayerMask.NameToLayer("Pickup");
        itemToDrop.layer = (pickupLayer != -1) ? pickupLayer : LayerMask.NameToLayer("Default");
        
        if (allDropColliders.Length > 0)
        {
            Debug.Log($"[HotbarManager] DropSelectedItem: {itemToDrop.name} - Enabled {allDropColliders.Length} collider(s) and restored layer.");
        }

        itemToDrop.SetActive(true); // Ensure the dropped item is active
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
    
    // Public method to reset animation state (useful when item breaks)
    public void ResetAnimationState()
    {
        _isItemAnimating = false;
        _currentHeldItemAnimator = null;
        Debug.Log("[HotbarManager] Animation state reset");
    }

    // NEW: Method to clear the currently selected hotbar slot
    public void ClearCurrentHotbarSlot()
    {
        if (heldItems[selectedSlot] != null)
        {
            GameObject itemToClear = heldItems[selectedSlot];
            Debug.Log($"[HotbarManager] Clearing item {itemToClear.name} from slot {selectedSlot}");
            
            // Set item to inactive and unparent it from handHolder immediately
            itemToClear.SetActive(false);
            itemToClear.transform.SetParent(null);

            heldItems[selectedSlot] = null; // Clear the item from the array
            hotbarSlots[selectedSlot].SetItem(null, emptySlotSprite); // Update UI to empty
            _currentHeldItemAnimator = null; // Clear animator reference

            // Ensure the item name display is updated after clearing the slot
            UpdateSelectedItemNameDisplay();
        }
    }

    private void UpdateSelectedItemNameDisplay()
    {
        if (selectedItemNameText != null)
        {
            if (heldItems[selectedSlot] != null)
            {
                // Try to get a more user-friendly name, otherwise use the GameObject's name
                string itemName = heldItems[selectedSlot].name;
                ItemIconProvider iconProvider = heldItems[selectedSlot].GetComponent<ItemIconProvider>();
                if (iconProvider != null && !string.IsNullOrEmpty(iconProvider.itemName))
                {
                    itemName = iconProvider.itemName;
                }
                selectedItemNameText.text = $"Current Item: {itemName}";
            }
            else
            {
                selectedItemNameText.text = "Current Item: Empty";
            }
        }
        else
        {
            Debug.LogWarning("[HotbarManager] selectedItemNameText is not assigned. Cannot update item name display.");
        }
    }

    // Drop all hotbar items into the world near a center position (e.g., death spot)
    public void DropAllHotbarItems(Vector3 center)
    {
        if (hotbarSlots == null) return;
        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            GameObject item = GetItem(i);
            if (item == null) continue;

            // Clear slot first
            SetItem(i, null);

            // Unparent from hand and enable physics
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
        }

        UpdateHotbarUI();
        UpdateSelectedItemNameDisplay();
    }

    // --- Audio helpers ---
    private void PlayPickupSfx()
    {
        if (pickupSfx == null) return;
        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(pickupSfx, pickupSfxVolume);
        }
        else if (Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(pickupSfx, Camera.main.transform.position, pickupSfxVolume);
        }
    }
} 