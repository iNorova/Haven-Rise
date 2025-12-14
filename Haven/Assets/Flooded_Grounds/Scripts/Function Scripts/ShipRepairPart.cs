using UnityEngine;

[System.Serializable]
public class ShipRepairPart
{
    [Header("Part Info")]
    [Tooltip("Name of the repair part (e.g., 'Engine', 'Propeller')")]
    public string partName;
    
    [Header("Item Requirements")]
    [Tooltip("Name of the item needed to repair this part (must match ItemIconProvider.itemName or GameObject name)")]
    public string requiredItemName;
    
    [Tooltip("Quantity of the item required")]
    public int requiredQuantity = 1;
    
    [Header("Minigame Settings")]
    [Tooltip("Type of minigame to play for this part")]
    public MinigameType minigameType = MinigameType.Engine;
    
    [Header("Engine Minigame Settings")]
    [Tooltip("Number of knot pairs (2 boxes per pair = 2 knots per pair, only for Engine minigame)")]
    public int numberOfKnots = 5;
    
    [Tooltip("Number of color pairs to match (each pair = 2 boxes, only for Engine minigame)")]
    public int numberOfKnotPairs = 3;
    
    [Tooltip("Time limit in seconds (only for Engine minigame)")]
    public float timeLimit = 30f;
    
    [Tooltip("Difficulty of knot untying (affects speed/difficulty)")]
    public int difficultyLevel = 1;
    
    public enum MinigameType
    {
        Engine,        // Knot-tying minigame
        Propeller,     // Future: Different minigame
        MetalScraps,   // Future: Different minigame
        Wood,          // Future: Different minigame
        // Add more types as needed
    }
}
