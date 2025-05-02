using UnityEngine;
using System.Collections.Generic;

public class ItemPickup : MonoBehaviour
{
    public float pickupRange = 3f;
    public KeyCode pickupKey = KeyCode.F;
    public KeyCode dropKey = KeyCode.Q;
    public Transform holdPoint;
    public float throwForce = 10f;

    // Default position and rotation for items without custom settings
    public Vector3 defaultHeldItemPosition = new Vector3(0.5f, -0.3f, 1f);
    public Vector3 defaultHeldItemRotation = new Vector3(0f, 0f, 0f);
    public float itemSmoothSpeed = 15f;
    public float maxDistanceFromTarget = 0.5f; // Maximum distance the item can be from its target position
    public float maxSmoothFactor = 0.3f; // Maximum smoothing factor when item is far from target

    // Dictionary to store custom positions for specific items
    private Dictionary<string, ItemPositionSettings> customItemPositions = new Dictionary<string, ItemPositionSettings>();

    private GameObject heldItem;
    private bool isHoldingItem = false;
    private Camera playerCamera;
    private Collider[] itemColliders;

    [System.Serializable]
    public class ItemPositionSettings
    {
        public Vector3 position;
        public Vector3 rotation;
    }

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            Debug.LogError("Player camera not found!");
        }

        // Set up custom positions for specific items
        // Format: customItemPositions.Add("ItemName", new ItemPositionSettings { position = position, rotation = rotation });

        // Axe position (adjust these values to match your axe model)
        customItemPositions.Add("Axe", new ItemPositionSettings 
        { 
            position = new Vector3(0.5f, -0.88f, 0.68f),
            rotation = new Vector3(90f, -90f, 0f)
        });

        // Rock position (adjust these values to match your rock model)
        customItemPositions.Add("Rock", new ItemPositionSettings 
        { 
            position = new Vector3(0.3f, -0.2f, 0.8f),
            rotation = new Vector3(0f, 0f, 0f)
        });
    }

    void Update()
    {
        if (Input.GetKeyDown(pickupKey))
        {
            if (!isHoldingItem)
            {
                TryPickupItem();
            }
            else
            {
                DropItem();
            }
        }

        if (Input.GetKeyDown(dropKey) && isHoldingItem)
        {
            DropItem();
        }

        if (isHoldingItem && heldItem != null)
        {
            UpdateHeldItemPosition();
        }
    }

    void UpdateHeldItemPosition()
    {
        if (playerCamera == null) return;

        // Get the appropriate position and rotation settings for the current item
        Vector3 targetPosition;
        Vector3 targetRotation;

        if (customItemPositions.TryGetValue(heldItem.name, out ItemPositionSettings settings))
        {
            targetPosition = settings.position;
            targetRotation = settings.rotation;
        }
        else
        {
            targetPosition = defaultHeldItemPosition;
            targetRotation = defaultHeldItemRotation;
        }

        // Calculate final position in world space
        Vector3 finalPosition = playerCamera.transform.position + 
            playerCamera.transform.right * targetPosition.x +
            playerCamera.transform.up * targetPosition.y +
            playerCamera.transform.forward * targetPosition.z;

        // Calculate current distance from target
        float currentDistance = Vector3.Distance(heldItem.transform.position, finalPosition);

        // Calculate smoothing factor based on distance
        float distanceFactor = Mathf.Clamp01(currentDistance / maxDistanceFromTarget);
        float smoothFactor = Mathf.Lerp(
            Mathf.Clamp01(Time.deltaTime * itemSmoothSpeed),
            maxSmoothFactor,
            distanceFactor
        );

        // Apply position smoothing
        heldItem.transform.position = Vector3.Lerp(
            heldItem.transform.position,
            finalPosition,
            smoothFactor
        );

        // Calculate target rotation
        Quaternion targetRot = playerCamera.transform.rotation * Quaternion.Euler(targetRotation);
        
        // Apply rotation smoothing (use a fixed smooth factor for rotation)
        float rotationSmoothFactor = Mathf.Clamp01(Time.deltaTime * itemSmoothSpeed);
        heldItem.transform.rotation = Quaternion.Slerp(
            heldItem.transform.rotation,
            targetRot,
            rotationSmoothFactor
        );
    }

    void TryPickupItem()
    {
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, pickupRange))
        {
            if (hit.collider.CompareTag("Pickupable"))
            {
                PickupItem(hit.collider.gameObject);
            }
        }
    }

    void PickupItem(GameObject item)
    {
        heldItem = item;
        isHoldingItem = true;

        // Store and disable all colliders
        itemColliders = heldItem.GetComponents<Collider>();
        foreach (Collider col in itemColliders)
        {
            col.enabled = false;
        }

        // Disable physics
        Rigidbody rb = heldItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Set initial position and rotation
        UpdateHeldItemPosition();
    }

    void DropItem()
    {
        if (heldItem != null)
        {
            // Re-enable all colliders
            if (itemColliders != null)
            {
                foreach (Collider col in itemColliders)
                {
                    col.enabled = true;
                }
            }

            // Re-enable physics
            Rigidbody rb = heldItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.AddForce(playerCamera.transform.forward * throwForce, ForceMode.Impulse);
            }

            isHoldingItem = false;
            heldItem = null;
            itemColliders = null;
        }
    }

    // Public method to add or update custom positions for items
    public void SetCustomItemPosition(string itemName, Vector3 position, Vector3 rotation)
    {
        if (customItemPositions.ContainsKey(itemName))
        {
            customItemPositions[itemName] = new ItemPositionSettings { position = position, rotation = rotation };
        }
        else
        {
            customItemPositions.Add(itemName, new ItemPositionSettings { position = position, rotation = rotation });
        }
    }
} 