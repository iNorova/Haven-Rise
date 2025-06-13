using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HotbarManager : MonoBehaviour
{
    [Header("Hotbar Setup")]
    public int maxSlots = 5;
    public Transform handHolder; // Assign in inspector
    public Image[] slotImages;   // Assign your slot UI Images in inspector
    public Sprite emptySlotSprite; // Sprite for empty slot

    private GameObject[] heldItems;
    private Sprite[] itemIcons;
    private int selectedSlot = 0;

    void Start()
    {
        heldItems = new GameObject[maxSlots];
        itemIcons = new Sprite[maxSlots];
        UpdateHotbarUI();
    }

    void Update()
    {
        // Select slot with 1-5
        for (int i = 0; i < maxSlots; i++)
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

        // NEW: Handle primary action (e.g., swinging axe)
        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            if (heldItems[selectedSlot] != null)
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
                for (int i = 0; i < maxSlots; i++)
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
        for (int i = 0; i < maxSlots; i++)
            if (heldItems[i] == null)
                return i;
        return -1;
    }

    void PickupItem(GameObject item, int slot)
    {
        Debug.Log($"Picking up {item.name} into slot {slot}");
        heldItems[slot] = item;

        // Get icon (assumes item has a script with a public Sprite icon field, or use a default)
        Sprite icon = null;
        var iconProvider = item.GetComponent<ItemIconProvider>();
        if (iconProvider != null)
            icon = iconProvider.icon;
        itemIcons[slot] = icon;

        // Parent to handHolder, using custom offset if available
        var offset = item.GetComponent<ItemHoldOffset>();
        if (offset != null)
            offset.ApplyOffset(handHolder);
        else
        {
            item.transform.SetParent(handHolder);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
        }
        item.SetActive(slot == selectedSlot);

        UpdateHotbarUI();
    }

    void SelectSlot(int slot)
    {
        Debug.Log($"Selecting slot {slot}");
        if (slot < 0 || slot >= maxSlots) return;

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

            // Force the Animator to the Idle state and reset all its triggers
            Animator itemAnimator = heldItems[selectedSlot].GetComponentInChildren<Animator>();
            if (itemAnimator != null)
            {
                // Play the Idle state immediately (assuming you have an "Idle" state in your Animator)
                itemAnimator.Play("Idle", 0, 0f); // Play "Idle" state on base layer (0), from start (0f)

                // Reset all triggers on this Animator
                foreach (var param in itemAnimator.parameters)
                {
                    if (param.type == AnimatorControllerParameterType.Trigger)
                    {
                        itemAnimator.ResetTrigger(param.name);
                        Debug.Log($"Resetting trigger {param.name} for {heldItems[selectedSlot].name}");
                    }
                }
            }
        }

        UpdateHotbarUI();
    }

    void DropSelectedItem()
    {
        if (heldItems[selectedSlot] != null)
        {
            GameObject item = heldItems[selectedSlot];
            item.SetActive(true);
            item.transform.SetParent(null);
            // Add drop logic (e.g., throw forward)
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.AddForce(Camera.main.transform.forward * 5f, ForceMode.Impulse);
            }
            heldItems[selectedSlot] = null;
            itemIcons[selectedSlot] = null;
            UpdateHotbarUI();
        }
    }

    void UpdateHotbarUI()
    {
        for (int i = 0; i < maxSlots; i++)
        {
            if (slotImages[i] != null)
                slotImages[i].sprite = itemIcons[i] != null ? itemIcons[i] : emptySlotSprite;
        }
    }
} 