using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public float pickupDistance = 2f; // Distance at which items can be picked up
    public KeyCode pickupKey = KeyCode.E; // Key to press for pickup
    public KeyCode dropKey = KeyCode.Q; // Key to press for dropping
    public Transform playerHand; // Where items will be held
    public LayerMask pickupLayer; // Layer mask for items that can be picked up
    public Camera playerCamera; // Reference to the player's camera

    private bool isHoldingItem = false;
    private GameObject heldItem;
    private GameObject currentTargetItem;

    void Start()
    {
        // If camera isn't assigned, try to find it
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                Debug.LogError("No camera found! Please assign a camera to the ItemPickup component.");
            }
        }
    }

    void Update()
    {
        if (playerCamera == null) return;

        // Debug ray visualization
        Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * pickupDistance, Color.red);

        // Check if player is looking at an item and within pickup distance
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, pickupDistance, pickupLayer))
        {
            Debug.Log("Raycast hit: " + hit.collider.gameObject.name);
            currentTargetItem = hit.collider.gameObject;
            
            // Show pickup prompt
            if (!isHoldingItem)
            {
                Debug.Log("Press " + pickupKey.ToString() + " to pick up " + currentTargetItem.name);
            }
            else
            {
                Debug.Log("Press " + dropKey.ToString() + " to drop " + heldItem.name);
            }

            // Handle pickup input
            if (Input.GetKeyDown(pickupKey) && !isHoldingItem)
            {
                PickupItem(currentTargetItem);
            }
        }

        // Handle drop input (can be pressed anytime while holding an item)
        if (isHoldingItem && Input.GetKeyDown(dropKey))
        {
            DropItem();
        }
    }

    void PickupItem(GameObject item)
    {
        isHoldingItem = true;
        heldItem = item;
        
        // Parent the item to the player's hand
        heldItem.transform.parent = playerHand;
        heldItem.transform.localPosition = Vector3.zero;
        heldItem.transform.localRotation = Quaternion.identity;
        
        // Disable physics if it has a rigidbody
        Rigidbody rb = heldItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        // Disable collider to prevent physics issues
        Collider col = heldItem.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
    }

    void DropItem()
    {
        if (heldItem == null) return;
        
        isHoldingItem = false;
        
        // Unparent the item
        heldItem.transform.parent = null;
        
        // Enable physics if it has a rigidbody
        Rigidbody rb = heldItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }
        
        // Enable collider
        Collider col = heldItem.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }
        
        heldItem = null;
    }
} 