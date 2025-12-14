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
    }
    
    void Update()
    {
        if (!isInteractable) return;
        
        bool canInteract = IsPlayerInRange();
        if (canInteract && Input.GetKeyDown(interactKey))
        {
            Debug.Log("ShipInteraction: Interact key pressed.");
            TryInteract();
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
        // Find the first part that isn't repaired yet
        ShipRepairPart partToRepair = null;
        foreach (var part in requiredParts)
        {
            if (part != null && !IsPartRepaired(part.partName))
            {
                partToRepair = part;
                break;
            }
        }
        
        if (partToRepair == null)
        {
            Debug.Log("ShipInteraction: All parts are already repaired!");
            PlaySfx(allPartsRepairedSfx);
            return;
        }
        
        // Check if player has the required item
        if (craftingService != null)
        {
            int itemCount = craftingService.CountByItemName(partToRepair.requiredItemName);
            if (itemCount < partToRepair.requiredQuantity)
            {
                Debug.Log($"ShipInteraction: Need {partToRepair.requiredQuantity} {partToRepair.requiredItemName}, but only have {itemCount}.");
                // TODO: Show UI message to player
                return;
            }
        }
        else
        {
            Debug.LogWarning("ShipInteraction: CraftingService not found. Cannot check for items.");
            return;
        }
        
        // Start the minigame for this part
        Debug.Log($"ShipInteraction: Starting repair minigame for {partToRepair.partName}.");
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
        else
        {
            // Add more minigame types here later (propeller, metal scraps, wood, etc.)
            Debug.LogWarning($"ShipInteraction: Minigame type {part.minigameType} not yet implemented!");
            OnRepairMinigameComplete(part, false);
        }
    }
    
    private void OnRepairMinigameComplete(ShipRepairPart part, bool success)
    {
        // Re-enable interaction
        isInteractable = true;
        
        if (success)
        {
            // Consume the item
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
            Debug.Log($"ShipInteraction: Failed to repair {part.partName}. Try again!");
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
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
