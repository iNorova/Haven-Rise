using UnityEngine;

/// <summary>
/// Handles interaction with placed workbenches. Press F to open the boat repair crafting menu.
/// </summary>
public class WorkbenchInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.E;  // Changed to E to avoid conflict with F key pickup
    public float interactionRange = 3f;
    
    private Camera playerCamera;
    private Transform player;
    private WorkbenchCraftingUI craftingUI;
    
    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        
        // Get or create crafting UI
        craftingUI = FindObjectOfType<WorkbenchCraftingUI>();
        if (craftingUI == null)
        {
            Debug.LogWarning("WorkbenchInteraction: WorkbenchCraftingUI not found! Creating new one. Make sure you have a WorkbenchCraftingUI GameObject in your scene.");
            GameObject uiObj = new GameObject("WorkbenchCraftingUI");
            craftingUI = uiObj.AddComponent<WorkbenchCraftingUI>();
            DontDestroyOnLoad(uiObj);
        }
        else
        {
            Debug.Log($"WorkbenchInteraction: Found WorkbenchCraftingUI on '{craftingUI.gameObject.name}'");
        }
        
        // Ensure trigger collider for interaction
        bool hasTrigger = false;
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            if (col.isTrigger)
            {
                hasTrigger = true;
                break;
            }
        }
        
        if (!hasTrigger)
        {
            SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = interactionRange;
        }
    }
    
    void Update()
    {
        // Don't process input if pause menu is open
        if (PauseMenuManager.IsPauseMenuOpen())
            return;
        
        if (player == null || playerCamera == null || craftingUI == null)
            return;
        
        // Check if player is in range
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > interactionRange)
            return;
        
        // Check if player is looking at workbench
        RaycastHit hit;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        bool isLookingAtWorkbench = false;
        if (Physics.Raycast(ray, out hit, interactionRange * 2f))
        {
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
            {
                isLookingAtWorkbench = true;
            }
        }
        
        // Fallback: if close enough, allow interaction
        if (distanceToPlayer <= interactionRange)
        {
            isLookingAtWorkbench = true;
        }
        
        // Check for interaction key press (default is F, not E!)
        if (isLookingAtWorkbench && Input.GetKeyDown(interactKey))
        {
            Debug.Log($"WorkbenchInteraction: {interactKey} key pressed! Opening crafting menu...");
            ToggleCraftingMenu();
        }
        
        // Debug: Log when player is in range but not pressing key (reduced frequency)
        if (isLookingAtWorkbench && Time.frameCount % 180 == 0) // Log every ~3 seconds
        {
            Debug.Log($"WorkbenchInteraction: Player is looking at workbench. Press {interactKey} to open crafting menu. Distance: {distanceToPlayer:F2}m");
        }
    }
    
    private void ToggleCraftingMenu()
    {
        if (craftingUI == null)
        {
            Debug.LogError("WorkbenchInteraction: CraftingUI is null! Cannot open menu.");
            return;
        }
        
        Debug.Log($"WorkbenchInteraction: Toggling crafting menu. Currently open: {craftingUI.IsOpen()}");
        
        if (craftingUI.IsOpen())
        {
            craftingUI.Close();
        }
        else
        {
            craftingUI.Open(this);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}

