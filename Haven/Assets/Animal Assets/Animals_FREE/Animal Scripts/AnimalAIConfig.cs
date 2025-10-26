using UnityEngine;

[CreateAssetMenu(fileName = "AnimalAIConfig", menuName = "AI/Animal Configuration", order = 1)]
public class AnimalAIConfig : ScriptableObject
{
    [Header("Detection")]
    [Tooltip("Distance at which the animal detects walking/sprinting player")]
    public float detectionRadius = 10f;
    [Tooltip("Distance at which the animal detects crouching player")]
    public float closeDetectionRadius = 2f;
    
    [Header("Movement Speeds")]
    public float idleSpeed = 1f;
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float rotationSpeed = 5f;
    
    [Header("Flee Behavior")]
    public float fleeDistance = 15f;
    public float minFleeDistance = 5f;
    public float maxFleeDistance = 100f;
    [Range(0f, 45f)]
    public float fleeAngleVariation = 30f;
    public float fleeUpdateInterval = 1f;
    
    [Header("Wander Behavior")]
    public float wanderRadius = 6f;
    public float wanderInterval = 5f;
    public float wanderStoppingDistance = 0.5f;
    [Range(0f, 1f)]
    public float wanderDirectionChange = 0.3f;
    
    [Header("Obstacle Avoidance")]
    public float obstacleCheckRadius = 0.5f;
    public float obstacleCheckDistance = 1.5f;
    public float obstacleCheckDistanceMultiplier = 2f; // Multiplied by velocity
    public LayerMask obstacleLayerMask = ~0;
    public float sideStepDistance = 1.5f;
    public float avoidanceCheckInterval = 0.2f;
    
    [Header("Bounce Settings")]
    public float bounceBackDistance = 2.5f;
    [Range(0f, 45f)]
    public float bounceAngleJitter = 15f;
    public float bounceCooldown = 0.5f;
    public float cornerDetectionAngle = 120f;
    
    [Header("Stuck Recovery")]
    public float stuckSpeedThreshold = 0.05f;
    public float stuckTimeThreshold = 0.5f;
    public float stuckRotationThreshold = 5f; // degrees per second
    
    [Header("Animation")]
    public float animationBlendSpeed = 5f;
    public float animationSpeedMultiplier = 1f;
    
    [Header("Health")]
    public int maxHP = 100;
    
    [Header("Physics")]
    public bool useTriggerCollider = true;
    public bool useKinematicRigidbody = true;
}

