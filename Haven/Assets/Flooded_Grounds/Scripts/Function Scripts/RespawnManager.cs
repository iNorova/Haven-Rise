using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    [Header("Respawn Settings")]
    public float respawnRadius = 5f;          // Distance from death spot to place player
    public float groundRaycastHeight = 100f;  // Height to start downward ray for ground
    public LayerMask groundMask = ~0;         // What counts as ground when raycasting

    [Header("Start Location")]
    public Transform explicitStartPoint;      // Optional explicit start spawn point

    private Vector3 startPosition;
    private Vector3 lastDeathPosition;

    private void OnEnable()
    {
        UIManager.OnPlayerDeath += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        UIManager.OnPlayerDeath -= HandlePlayerDeath;
    }

    private void Start()
    {
        // Capture starting position
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (explicitStartPoint != null)
        {
            startPosition = explicitStartPoint.position;
        }
        else if (player != null)
        {
            startPosition = player.transform.position;
        }
        else
        {
            startPosition = Vector3.zero;
        }
    }

    private void HandlePlayerDeath()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        lastDeathPosition = player != null ? player.transform.position : lastDeathPosition;
        // Drop all inventory and hotbar items at death location
        TryDropAllItemsAt(lastDeathPosition);
        // Defer actual respawn until UI choice (handled by DeathScreenUI)
    }

    private void TryDropAllItemsAt(Vector3 center)
    {
        // Try via InventorySystem first
        InventorySystem invSystem = FindObjectOfType<InventorySystem>();
        if (invSystem != null)
        {
            if (invSystem.hotbarManager != null)
            {
                invSystem.hotbarManager.DropAllHotbarItems(center);
            }
            if (invSystem.inventoryManager != null)
            {
                invSystem.inventoryManager.DropAllItems(center);
            }
            return;
        }

        // Fallback: find managers individually
        HotbarManager hotbar = FindObjectOfType<HotbarManager>();
        if (hotbar != null) hotbar.DropAllHotbarItems(center);
        InventoryManager inv = FindObjectOfType<InventoryManager>();
        if (inv != null) inv.DropAllItems(center);
    }

    public void RespawnAtDeathSpot()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("RespawnManager: Player not found with tag 'Player'.");
            return;
        }
        Vector3 targetPosition = GetRespawnPositionNear(lastDeathPosition);
        TeleportPlayer(player, targetPosition);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ResetAllStats();
        }
    }

    public void RespawnAtStart()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("RespawnManager: Player not found with tag 'Player'.");
            return;
        }
        Vector3 targetPosition = GetRespawnPositionNear(startPosition);
        TeleportPlayer(player, targetPosition);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ResetAllStats();
        }
    }

    private Vector3 GetRespawnPositionNear(Vector3 origin)
    {
        // Choose a random horizontal offset within radius
        Vector2 offset2D = Random.insideUnitCircle * respawnRadius;
        Vector3 candidate = new Vector3(origin.x + offset2D.x, origin.y + groundRaycastHeight, origin.z + offset2D.y);

        // Raycast down to find ground
        if (Physics.Raycast(candidate, Vector3.down, out RaycastHit hit, groundRaycastHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }

        // Fallback: keep player's current Y if raycast fails
        candidate.y = origin.y;
        return candidate;
    }

    private void TeleportPlayer(GameObject player, Vector3 position)
    {
        // If CharacterController is present, disable during teleport to avoid unwanted physics correction
        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        player.transform.position = position + Vector3.up * 0.1f; // small lift to avoid clipping

        if (rb != null)
        {
            rb.isKinematic = false;
        }
        if (characterController != null)
        {
            characterController.enabled = true;
        }
    }
}


