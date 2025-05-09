using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class ItemPickup : MonoBehaviour
{
    public float pickupRange = 5f;
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

    // Animation related variables
    private Animator itemAnimator;
    private bool isAnimating = false;

    // Animation state names
    private const string SWING_ANIMATION = "Swing";

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

        // Axe position (adjusted to be more visible in FOV)
        customItemPositions.Add("Axe", new ItemPositionSettings 
        { 
            position = new Vector3(0.5f, -0.6f, 0.8f),
            rotation = new Vector3(90f, 360f, 90f)
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

        // Add animation control for axe swing
        if (isHoldingItem && heldItem != null && heldItem.name == "Axe" && Input.GetMouseButtonDown(0))
        {
            PlaySwingAnimation();
        }
    }

    void LateUpdate()
    {
        if (isHoldingItem && heldItem != null)
        {
            UpdateHeldItemPosition();
        }
    }

    void UpdateHeldItemPosition()
    {
        if (playerCamera == null || heldItem == null) return;

        // Only set transform if not animating
        if (!isAnimating)
        {
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

            // Set local position and rotation relative to the camera
            heldItem.transform.localPosition = targetPosition;
            heldItem.transform.localRotation = Quaternion.Euler(targetRotation);
        }
    }

    void TryPickupItem()
    {
        RaycastHit hit;
        float sphereRadius = 0.5f; // Wider detection area for easier pickup
        if (Physics.SphereCast(playerCamera.transform.position, sphereRadius, playerCamera.transform.forward, out hit, pickupRange))
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

        // Get the animator component if it exists
        itemAnimator = heldItem.GetComponent<Animator>();

        // Parent the item to the camera
        heldItem.transform.SetParent(playerCamera.transform);
        UpdateHeldItemPosition();
    }

    void DropItem()
    {
        if (heldItem != null)
        {
            // Unparent the item
            heldItem.transform.SetParent(null);

            // Move the item slightly forward to avoid overlapping with the player
            Vector3 dropOffset = playerCamera.transform.forward * 1.0f; // 1 unit in front of the camera
            heldItem.transform.position = playerCamera.transform.position + dropOffset;

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
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Prevent tunneling
                rb.linearDamping = 3.0f; // Increase drag to reduce sliding
                rb.AddForce(playerCamera.transform.forward * throwForce, ForceMode.Impulse);
            }

            isHoldingItem = false;
            heldItem = null;
            itemColliders = null;
            itemAnimator = null;
        }
    }

    // Animation control method
    private void PlaySwingAnimation()
    {
        if (itemAnimator != null && !isAnimating)
        {
            itemAnimator.Play(SWING_ANIMATION);
            isAnimating = true;
            StartCoroutine(ResetAnimationState());
        }
    }

    private System.Collections.IEnumerator ResetAnimationState()
    {
        // Wait for the animation to complete
        yield return new WaitForSeconds(itemAnimator.GetCurrentAnimatorStateInfo(0).length);

        // Add a small delay to ensure the last frame is rendered
        yield return new WaitForSeconds(0.07f); // You can tweak this value

        isAnimating = false;
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