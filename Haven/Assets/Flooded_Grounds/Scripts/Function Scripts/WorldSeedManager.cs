using UnityEngine;
using System;

public class WorldSeedManager : MonoBehaviour
{
    [SerializeField] private string seedString = "";  // For manual seed input
    [SerializeField] private bool useRandomSeed = true;
    
    private int currentSeed;
    private System.Random seededRandom;

    void Awake()
    {
        InitializeSeed();
    }

    void InitializeSeed()
    {
        if (useRandomSeed)
        {
            // Generate a random seed if none is provided
            currentSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            seedString = currentSeed.ToString();
        }
        else
        {
            // Use the provided seed string
            if (string.IsNullOrEmpty(seedString))
            {
                seedString = "0";
            }
            currentSeed = seedString.GetHashCode();
        }

        // Initialize our seeded random number generator
        seededRandom = new System.Random(currentSeed);
        Debug.Log($"Initialized world with seed: {seedString} (Hash: {currentSeed})");
    }

    // Get a random float between 0 and 1 based on the seed
    public float GetSeededRandom()
    {
        return (float)seededRandom.NextDouble();
    }

    // Get a random int between min and max (inclusive) based on the seed
    public int GetSeededRandomRange(int min, int max)
    {
        return seededRandom.Next(min, max + 1);
    }

    // Get a random position within specified bounds
    public Vector3 GetRandomPosition(Vector3 minBounds, Vector3 maxBounds)
    {
        return new Vector3(
            Mathf.Lerp(minBounds.x, maxBounds.x, (float)seededRandom.NextDouble()),
            Mathf.Lerp(minBounds.y, maxBounds.y, (float)seededRandom.NextDouble()),
            Mathf.Lerp(minBounds.z, maxBounds.z, (float)seededRandom.NextDouble())
        );
    }

    // Get the current seed string
    public string GetSeedString()
    {
        return seedString;
    }

    // Set a new seed and reinitialize
    public void SetSeed(string newSeed)
    {
        seedString = newSeed;
        useRandomSeed = false;
        InitializeSeed();
    }
}
