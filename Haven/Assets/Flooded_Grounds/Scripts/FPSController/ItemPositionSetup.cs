using UnityEngine;

public class ItemPositionSetup : MonoBehaviour
{
    public ItemPickup itemPickup;

    void Start()
    {
        if (itemPickup == null)
        {
            itemPickup = GetComponent<ItemPickup>();
        }

        // Set up custom positions for specific items
        // Format: itemPickup.SetCustomItemPosition("ItemName", position, rotation);
        
        // Example for Axe
        itemPickup.SetCustomItemPosition(
            "Axe",  // The exact name of your axe GameObject
            new Vector3(0.5f, -0.3f, 1f),  // Custom position
            new Vector3(0f, 90f, 0f)       // Custom rotation
        );

        // Example for Rock
        itemPickup.SetCustomItemPosition(
            "Rock",  // The exact name of your rock GameObject
            new Vector3(0.3f, -0.2f, 0.8f),  // Custom position
            new Vector3(0f, 0f, 0f)          // Custom rotation
        );
    }
} 