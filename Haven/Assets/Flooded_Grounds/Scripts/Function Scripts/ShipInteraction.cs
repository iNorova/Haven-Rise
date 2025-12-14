using UnityEngine;
using System.Collections.Generic;
using Haven.CraftingUI;

public class ShipInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.E;
    public float interactRange = 5.0f;
    
    [Header("Ship Repair Parts")]
    [Tooltip("List of all parts needed to repair the ship")]
    public ShipRepairPart[] requiredParts;
    
    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip interactSfx;
    public AudioClip partRepairedSfx;
    public AudioClip allPartsRepairedSfx;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;
    
    private Transform player;
    private bool isInteractable = true;
    private Dictionary<string, bool> repairedParts = new Dictionary<string, bool>();
    private CraftingService craftingService;
    
    void Start()
    {
        // Auto-find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        player = playerObj != null ? playerObj.transform : null;
        
        // Find CraftingService for inventory checking
        craftingService = FindObjectOfType<CraftingService>();
        if (craftingService == null)
        {
            Debug.LogError("ShipInteraction: CraftingService not found! Cannot check for repair parts.");
        }
        
        // Initialize repaired parts dictionary
        if (requiredParts != null)
        {
            foreach (var part in requiredParts)
            {
                if (part != null && !string.IsNullOrEmpty(part.partName))
                {
                    repairedParts[part.partName] = false;
                }
            }
        }
        
        // Ensure AudioSource
        if (sfxSource == null)
        {
            GameObject audioObj = new GameObject("ShipAudioSource");
            audioObj.transform.SetParent(transform);
            audioObj.transform.localPosition = Vector3.zero;
            sfxSource = audioObj.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 1f;
            sfxSource.minDistance = 3f;
            sfxSource.maxDistance = 15f;
        }
        
        // Ensure trigger collider
        bool hasTrigger = false;
        var cols = GetComponents<Collider>();
        foreach (var c in cols) { if (c.isTrigger) { hasTrigger = true; break; } }
        if (!hasTrigger)
        {
            SphereCollider trig = gameObject.AddComponent<SphereCollider>();
            trig.isTrigger = true;
            trig.radius = interactRange;
        }
        
        Debug.Log($"ShipInteraction: Initialized with {requiredParts?.Length ?? 0} required parts.");
        if (requiredParts != null && requiredParts.Length > 0)
        {
            foreach (var part in requiredParts)
            {
                if (part != null)
                {
                    Debug.Log($"  - Part: {part.partName}, Item: {part.requiredItemName}, Type: {part.minigameType}");
                }
            }
        }
        
        // Debug: List all items in inventory/hotbar to help with setup
        if (craftingService != null)
        {
            Debug.Log("ShipInteraction: Debug - Listing items in player inventory...");
            ListAllPlayerItems();
        }
    }
    
    void Update()
    {
        if (!isInteractable)
        {
            // Debug log why interaction is disabled
            // Debug.Log("ShipInteraction: Not interactable (minigame active or disabled).");
            return;
        }
        
        bool canInteract = IsPlayerInRange();
        if (canInteract && Input.GetKeyDown(interactKey))
        {
            Debug.Log("ShipInteraction: Interact key pressed.");
            TryInteract();
        }
        else if (Input.GetKeyDown(interactKey))
        {
            float dist = player != null ? Vector3.Distance(player.position, transform.position) : -1f;
            Debug.LogWarning($"ShipInteraction: E pressed but cannot interact. Player in range: {canInteract}, Distance: {dist:F2}, Range: {interactRange}, Player: {(player != null ? player.name : "NULL")}");
        }
    }
    
    private bool IsPlayerInRange()
    {
        if (player == null) return false;
        float dist = Vector3.Distance(player.position, transform.position);
        return dist <= interactRange;
    }
    
    private void TryInteract()
    {
        Debug.Log("ShipInteraction: TryInteract called.");
        
        // Check if requiredParts array is set up
        if (requiredParts == null || requiredParts.Length == 0)
        {
            Debug.LogError("ShipInteraction: Required Parts array is null or empty! Please configure it in the Inspector.");
            return;
        }
        
        // Get the currently held item from hotbar
        HotbarManager hotbarManager = FindObjectOfType<HotbarManager>();
        GameObject heldItem = null;
        string heldItemName = null;
        
        if (hotbarManager != null)
        {
            heldItem = hotbarManager.GetItem(hotbarManager.selectedSlot);
            if (heldItem != null)
            {
                // Try to get item name from ItemIconProvider
                ItemIconProvider iconProvider = heldItem.GetComponent<ItemIconProvider>();
                if (iconProvider != null && !string.IsNullOrEmpty(iconProvider.itemName))
                {
                    heldItemName = iconProvider.itemName;
                }
                else
                {
                    // Fallback to GameObject name
                    heldItemName = heldItem.name.Replace("(Clone)", "").Trim();
                }
                Debug.Log($"ShipInteraction: Player is holding '{heldItemName}' (from slot {hotbarManager.selectedSlot})");
            }
            else
            {
                Debug.LogWarning("ShipInteraction: No item in selected hotbar slot. Please select the repair part you want to use.");
                return;
            }
        }
        else
        {
            Debug.LogError("ShipInteraction: HotbarManager not found! Cannot check held item.");
            return;
        }
        
        // Find the part that matches the held item
        ShipRepairPart partToRepair = null;
        foreach (var part in requiredParts)
        {
            if (part == null)
            {
                Debug.LogWarning("ShipInteraction: Found null part in requiredParts array!");
                continue;
            }
            
            // Check if this part's required item matches what player is holding
            if (MatchesItemName(heldItem, part.requiredItemName))
            {
                bool isRepaired = IsPartRepaired(part.partName);
                Debug.Log($"ShipInteraction: Found matching part '{part.partName}' for held item '{heldItemName}' - Repaired: {isRepaired}, Minigame Type: {part.minigameType}");
                
                if (isRepaired)
                {
                    Debug.LogWarning($"ShipInteraction: Part '{part.partName}' is already repaired!");
                    return;
                }
                
                partToRepair = part;
                break;
            }
        }
        
        if (partToRepair == null)
        {
            Debug.LogWarning($"ShipInteraction: No repair part found that matches held item '{heldItemName}'. Make sure the item name matches the part's 'Required Item Name' in the Inspector.");
            return;
        }
        
        // Check if player has enough of the required item
        if (craftingService == null)
        {
            Debug.LogError("ShipInteraction: CraftingService not found! Cannot check for items.");
            return;
        }
        
        Debug.Log($"ShipInteraction: Checking for item '{partToRepair.requiredItemName}' in inventory...");
        int itemCount = craftingService.CountByItemName(partToRepair.requiredItemName);
        Debug.Log($"ShipInteraction: Found {itemCount} of '{partToRepair.requiredItemName}' (need {partToRepair.requiredQuantity})");
        
        if (itemCount < partToRepair.requiredQuantity)
        {
            Debug.LogWarning($"ShipInteraction: Need {partToRepair.requiredQuantity} {partToRepair.requiredItemName}, but only have {itemCount}. Make sure the item name matches exactly!");
            // TODO: Show UI message to player
            return;
        }
        
        // Start the minigame for this part
        Debug.Log($"ShipInteraction: Starting repair minigame for {partToRepair.partName} (Type: {partToRepair.minigameType}).");
        StartRepairMinigame(partToRepair);
    }
    
    private void StartRepairMinigame(ShipRepairPart part)
    {
        // Disable interaction while minigame is active
        isInteractable = false;
        
        // Start the appropriate minigame based on part type
        if (part.minigameType == ShipRepairPart.MinigameType.Engine)
        {
            EngineRepairMiniGame engineMiniGame = GetComponent<EngineRepairMiniGame>();
            if (engineMiniGame == null)
            {
                engineMiniGame = gameObject.AddComponent<EngineRepairMiniGame>();
            }
            
            engineMiniGame.Begin(part, OnRepairMinigameComplete);
        }
        else if (part.minigameType == ShipRepairPart.MinigameType.Propeller)
        {
            PropellerRepairMiniGame propellerMiniGame = GetComponent<PropellerRepairMiniGame>();
            if (propellerMiniGame == null)
            {
                propellerMiniGame = gameObject.AddComponent<PropellerRepairMiniGame>();
            }
            
            propellerMiniGame.Begin(part, OnRepairMinigameComplete);
        }
        else
        {
            // Add more minigame types here later (metal scraps, wood, etc.)
            Debug.LogWarning($"ShipInteraction: Minigame type {part.minigameType} not yet implemented!");
            OnRepairMinigameComplete(part, false);
        }
    }
    
    private void OnRepairMinigameComplete(ShipRepairPart part, bool success)
    {
        // Re-enable interaction (player can try again if failed)
        isInteractable = true;
        
        if (success)
        {
            // Consume the item only on success
            if (craftingService != null)
            {
                // Consume from inventory/hotbar
                ConsumeRepairItem(part.requiredItemName, part.requiredQuantity);
            }
            
            // Mark part as repaired
            repairedParts[part.partName] = true;
            PlaySfx(partRepairedSfx);
            
            Debug.Log($"ShipInteraction: Successfully repaired {part.partName}!");
            
            // Check if all parts are repaired
            if (AreAllPartsRepaired())
            {
                Debug.Log("ShipInteraction: ALL PARTS REPAIRED! Ship is ready!");
                PlaySfx(allPartsRepairedSfx);
                OnAllPartsRepaired();
            }
        }
        else
        {
            // Failure - don't consume item, player can try again
            string partName = part != null && !string.IsNullOrEmpty(part.partName) ? part.partName : "Unknown Part";
            Debug.Log($"ShipInteraction: Failed to repair {partName}. Press E again to retry!");
        }
    }
    
    private void ConsumeRepairItem(string itemName, int quantity)
    {
        if (craftingService == null) return;
        
        // Use CraftingService's consume methods (similar to crafting)
        // We'll need to access private methods or create a public method
        // For now, manually consume from inventory/hotbar
        
        InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();
        HotbarManager hotbarManager = FindObjectOfType<HotbarManager>();
        
        int remaining = quantity;
        
        // Consume from hotbar first
        if (hotbarManager != null && remaining > 0)
        {
            for (int i = 0; i < hotbarManager.hotbarSlots.Length && remaining > 0; i++)
            {
                GameObject item = hotbarManager.GetItem(i);
                if (item != null && MatchesItemName(item, itemName))
                {
                    InventorySlot slot = hotbarManager.hotbarSlots[i];
                    int stackCount = slot.GetStackCount();
                    int toConsume = Mathf.Min(stackCount, remaining);
                    
                    if (toConsume >= stackCount)
                    {
                        hotbarManager.SetItem(i, null);
                        Destroy(item);
                        remaining -= stackCount;
                    }
                    else
                    {
                        slot.SetStackCount(stackCount - toConsume);
                        remaining -= toConsume;
                    }
                }
            }
            hotbarManager.UpdateHotbarUI();
        }
        
        // Consume from inventory
        if (inventoryManager != null && remaining > 0)
        {
            for (int i = 0; i < inventoryManager.inventorySlots.Length && remaining > 0; i++)
            {
                GameObject item = inventoryManager.GetItem(i);
                if (item != null && MatchesItemName(item, itemName))
                {
                    InventorySlot slot = inventoryManager.inventorySlots[i];
                    int stackCount = slot.GetStackCount();
                    int toConsume = Mathf.Min(stackCount, remaining);
                    
                    if (toConsume >= stackCount)
                    {
                        inventoryManager.RemoveItem(i);
                        Destroy(item);
                        remaining -= stackCount;
                    }
                    else
                    {
                        slot.SetStackCount(stackCount - toConsume);
                        remaining -= toConsume;
                    }
                }
            }
            inventoryManager.UpdateInventoryUI();
        }
    }
    
    private bool MatchesItemName(GameObject item, string targetName)
    {
        ItemIconProvider iconProvider = item.GetComponent<ItemIconProvider>();
        if (iconProvider != null && !string.IsNullOrEmpty(iconProvider.itemName))
        {
            return iconProvider.itemName == targetName;
        }
        return item.name.Contains(targetName) || targetName.Contains(item.name);
    }
    
    public bool IsPartRepaired(string partName)
    {
        return repairedParts.ContainsKey(partName) && repairedParts[partName];
    }
    
    public bool AreAllPartsRepaired()
    {
        foreach (var kvp in repairedParts)
        {
            if (!kvp.Value) return false;
        }
        return true;
    }
    
    protected virtual void OnAllPartsRepaired()
    {
        // Override this in a derived class or use UnityEvent for custom behavior
        Debug.Log("ShipInteraction: Ship is fully repaired and ready to use!");
    }
    
    private void PlaySfx(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }
    
    // Debug helper to list all items player has
    private void ListAllPlayerItems()
    {
        InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();
        HotbarManager hotbarManager = FindObjectOfType<HotbarManager>();
        
        System.Collections.Generic.HashSet<string> itemNames = new System.Collections.Generic.HashSet<string>();
        
        // Check hotbar
        if (hotbarManager != null)
        {
            for (int i = 0; i < hotbarManager.hotbarSlots.Length; i++)
            {
                GameObject item = hotbarManager.GetItem(i);
                if (item != null)
                {
                    ItemIconProvider provider = item.GetComponent<ItemIconProvider>();
                    string name = provider != null && !string.IsNullOrEmpty(provider.itemName) 
                        ? provider.itemName 
                        : item.name.Replace("(Clone)", "").Trim();
                    itemNames.Add(name);
                    Debug.Log($"  Hotbar Slot {i}: {name}");
                }
            }
        }
        
        // Check inventory
        if (inventoryManager != null)
        {
            for (int i = 0; i < inventoryManager.inventorySlots.Length; i++)
            {
                GameObject item = inventoryManager.GetItem(i);
                if (item != null)
                {
                    ItemIconProvider provider = item.GetComponent<ItemIconProvider>();
                    string name = provider != null && !string.IsNullOrEmpty(provider.itemName) 
                        ? provider.itemName 
                        : item.name.Replace("(Clone)", "").Trim();
                    itemNames.Add(name);
                    Debug.Log($"  Inventory Slot {i}: {name}");
                }
            }
        }
        
        Debug.Log($"ShipInteraction: Player has {itemNames.Count} unique item type(s) total.");
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
