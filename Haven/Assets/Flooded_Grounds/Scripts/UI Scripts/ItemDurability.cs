using UnityEngine;
using UnityEngine.UI;

public class ItemDurability : MonoBehaviour
{
    [Header("Durability Settings")]
    public float maxDurability = 15f;        // Total durability (15 hits)
    public float currentDurability;         // Current durability (public so InventorySlot can access it)
    
    [Header("UI (Optional)")]
    public Slider durabilitySlider;          // Optional: UI slider to show durability
    public Image durabilityFillImage;        // Optional: Fill image for slider
    public Color normalColor = Color.green;  // Color when durability is good
    public Color lowColor = Color.yellow;    // Color when durability is low
    public Color criticalColor = Color.red;  // Color when durability is critical
    
    [Header("Visual Feedback")]
    public bool showUI = true;               // Whether to show durability UI
    
    private bool isBroken = false;
    
    void Start()
    {
        // Initialize durability to max
        currentDurability = maxDurability;
        
        // Initialize UI if assigned
        if (durabilitySlider != null)
        {
            durabilitySlider.maxValue = maxDurability;
            durabilitySlider.minValue = 0f;
            durabilitySlider.value = currentDurability;
        }
        
        UpdateUI();
    }
    
    // Reduce durability when item is used
    public void ReduceDurability(float amount = 1f)
    {
        if (isBroken) return;
        
        currentDurability -= amount;
        currentDurability = Mathf.Max(0f, currentDurability);
        
        UpdateUI();
        
        Debug.Log($"[ItemDurability] {gameObject.name}: Durability reduced by {amount}. Current: {currentDurability}/{maxDurability}");
        
        // Check if item is broken
        if (currentDurability <= 0f)
        {
            BreakItem();
        }
    }
    
    // Break the item when durability reaches 0
    private void BreakItem()
    {
        if (isBroken) return;
        
        isBroken = true;
        Debug.Log($"[ItemDurability] {gameObject.name} is broken!");
        
        // Notify listeners that the item is broken
        OnItemBroken?.Invoke();
    }
    
    // Check if item is broken
    public bool IsBroken()
    {
        return isBroken || currentDurability <= 0f;
    }
    
    // Update UI to reflect current durability (made public for save/load system)
    public void UpdateUI()
    {
        if (!showUI || durabilitySlider == null) return;
        
        durabilitySlider.value = currentDurability;
        
        // Update color based on durability percentage
        if (durabilityFillImage != null)
        {
            float durabilityPercent = currentDurability / maxDurability;
            if (durabilityPercent <= 0.33f)
            {
                durabilityFillImage.color = criticalColor;
            }
            else if (durabilityPercent <= 0.66f)
            {
                durabilityFillImage.color = lowColor;
            }
            else
            {
                durabilityFillImage.color = normalColor;
            }
        }
    }
    
    // Get current durability as percentage (0-1)
    public float GetDurabilityPercent()
    {
        return maxDurability > 0 ? currentDurability / maxDurability : 0f;
    }
    
    // Event for when item breaks
    public System.Action OnItemBroken;
}
