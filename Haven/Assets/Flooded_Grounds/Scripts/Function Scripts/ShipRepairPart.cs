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
    
    [Header("Propeller Minigame Settings")]
    [Tooltip("Time limit in seconds (for Propeller minigame)")]
    public float timeLimit = 15f;
    
    [Tooltip("Target points to reach (out of 100, for Propeller minigame)")]
    public float targetPoints = 80f;
    
    [Tooltip("Points added per click (for Propeller minigame)")]
    public float pointsPerClick = 10f;
    
    [Tooltip("Points lost per second (decay rate, for Propeller minigame)")]
    public float decayRate = 15f;
    
    [Tooltip("Failure threshold - instant fail if bar drops below this (for Propeller minigame)")]
    public float failureThreshold = 20f;
    
    [Tooltip("Difficulty level (affects various settings)")]
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
