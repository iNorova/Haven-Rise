using UnityEngine;
using UnityEditor;
using System.IO;

public class CreateAnimalConfigs : EditorWindow
{
    [MenuItem("Tools/Create Default Animal Configs")]
    public static void CreateDefaultConfigs()
    {
        string path = "Assets/Animal Assets/Animals_FREE/Configs";
        
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        
        CreateDeerConfig(path);
        CreateRabbitConfig(path);
        CreateBearConfig(path);
        
        AssetDatabase.Refresh();
        Debug.Log("Created default animal configs in: " + path);
    }
    
    private static void CreateDeerConfig(string path)
    {
        var config = ScriptableObject.CreateInstance<AnimalAIConfig>();
        config.name = "Deer_Config";
        
        // Detection
        config.detectionRadius = 12f;
        config.closeDetectionRadius = 3f;
        
        // Movement
        config.idleSpeed = 0.5f;
        config.walkSpeed = 2f;
        config.runSpeed = 7f;
        config.rotationSpeed = 6f;
        
        // Flee
        config.fleeDistance = 20f;
        config.minFleeDistance = 8f;
        config.maxFleeDistance = 150f;
        config.fleeAngleVariation = 25f;
        
        // Wander
        config.wanderRadius = 8f;
        config.wanderInterval = 6f;
        config.wanderDirectionChange = 0.2f;
        
        // Obstacle
        config.obstacleCheckDistance = 2f;
        config.obstacleCheckDistanceMultiplier = 2.5f;
        
        // Bounce
        config.bounceBackDistance = 3f;
        config.bounceAngleJitter = 20f;
        
        // Health
        config.maxHP = 50;
        
        AssetDatabase.CreateAsset(config, $"{path}/Deer_Config.asset");
    }
    
    private static void CreateRabbitConfig(string path)
    {
        var config = ScriptableObject.CreateInstance<AnimalAIConfig>();
        config.name = "Rabbit_Config";
        
        // Detection (very skittish)
        config.detectionRadius = 8f;
        config.closeDetectionRadius = 2f;
        
        // Movement (fast and erratic)
        config.idleSpeed = 0.3f;
        config.walkSpeed = 1.5f;
        config.runSpeed = 6f;
        config.rotationSpeed = 10f;
        
        // Flee (quick escapes)
        config.fleeDistance = 10f;
        config.minFleeDistance = 4f;
        config.maxFleeDistance = 80f;
        config.fleeAngleVariation = 40f;
        
        // Wander (short, frequent movements)
        config.wanderRadius = 4f;
        config.wanderInterval = 3f;
        config.wanderDirectionChange = 0.5f;
        
        // Obstacle (agile)
        config.obstacleCheckDistance = 1f;
        config.obstacleCheckDistanceMultiplier = 3f;
        
        // Bounce (quick reactions)
        config.bounceBackDistance = 2f;
        config.bounceAngleJitter = 30f;
        config.bounceCooldown = 0.3f;
        
        // Health (fragile)
        config.maxHP = 20;
        
        AssetDatabase.CreateAsset(config, $"{path}/Rabbit_Config.asset");
    }
    
    private static void CreateBearConfig(string path)
    {
        var config = ScriptableObject.CreateInstance<AnimalAIConfig>();
        config.name = "Bear_Config";
        
        // Detection (confident, less scared)
        config.detectionRadius = 6f;
        config.closeDetectionRadius = 1.5f;
        
        // Movement (slow but powerful)
        config.idleSpeed = 0.8f;
        config.walkSpeed = 1.5f;
        config.runSpeed = 4f;
        config.rotationSpeed = 3f;
        
        // Flee (reluctant to flee)
        config.fleeDistance = 15f;
        config.minFleeDistance = 6f;
        config.maxFleeDistance = 100f;
        config.fleeAngleVariation = 15f;
        
        // Wander (larger territory)
        config.wanderRadius = 12f;
        config.wanderInterval = 8f;
        config.wanderDirectionChange = 0.1f;
        
        // Obstacle (pushes through)
        config.obstacleCheckDistance = 1.5f;
        config.obstacleCheckDistanceMultiplier = 1.5f;
        
        // Bounce (heavy, less reactive)
        config.bounceBackDistance = 2f;
        config.bounceAngleJitter = 10f;
        config.bounceCooldown = 0.8f;
        
        // Health (tough)
        config.maxHP = 200;
        
        AssetDatabase.CreateAsset(config, $"{path}/Bear_Config.asset");
    }
}

