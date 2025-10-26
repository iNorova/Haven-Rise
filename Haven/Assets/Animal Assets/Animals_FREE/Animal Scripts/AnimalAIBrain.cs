using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// AI Brain - Makes decisions only, doesn't move the animal
/// Outputs movement commands to CreatureMover
/// </summary>
public class AnimalAIBrain : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private AnimalAIConfig config;
    
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool logStateChanges = false;
    
    [Header("Events")]
    public UnityEvent<AnimalState> onStateChanged;
    public UnityEvent onPlayerDetected;
    public UnityEvent onPlayerLost;

    // Movement output (read by CreatureMover)
    public Vector3 DesiredDirection { get; private set; }
    public float DesiredSpeed { get; private set; }
    public bool ShouldRun { get; private set; }
    public Vector3 LookTarget { get; private set; }
    
    // Components (cached)
    private CharController_Motor cachedPlayerController;
    
    // States
    public enum AnimalState { Idle, Wandering, Fleeing, Alert, Dead }
    private AnimalState currentState = AnimalState.Idle;
    private AnimalState previousState = AnimalState.Idle;
    
    // Decision tracking
    private Vector3 wanderTarget;
    private Vector3 fleeDirection;
    private float nextWanderTime;
    private float nextFleeUpdateTime;
    private float stateEnterTime;
    
    // Health
    private int currentHP;
    
    // Public properties
    public AnimalState CurrentState => currentState;
    public AnimalAIConfig Config => config;
    public bool IsAlive => currentState != AnimalState.Dead;

    #region Initialization

    private void Awake()
    {
        if (config == null)
        {
            LogWarning("No config assigned - creating default runtime config");
            config = ScriptableObject.CreateInstance<AnimalAIConfig>();
        }
    }

    private void Start()
    {
        FindPlayer();
        InitializeState();
    }

    private void FindPlayer()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        
        if (playerTransform != null)
        {
            cachedPlayerController = playerTransform.GetComponent<CharController_Motor>();
        }
    }

    private void InitializeState()
    {
        currentHP = config.maxHP;
        ChangeState(AnimalState.Idle);
        
        // Initialize outputs
        DesiredDirection = Vector3.zero;
        DesiredSpeed = 0f;
        ShouldRun = false;
        LookTarget = transform.position + transform.forward * 10f;
    }

    #endregion

    #region Update Loop

    private void Update()
    {
        if (currentState == AnimalState.Dead) return;
        
        CheckPlayerProximity();
        UpdateStateBehavior();
        UpdateMovementOutput();
    }

    #endregion

    #region State Machine

    private void ChangeState(AnimalState newState)
    {
        if (currentState == newState) return;
        
        previousState = currentState;
        currentState = newState;
        stateEnterTime = Time.time;
        
        OnStateEnter(newState);
        onStateChanged?.Invoke(newState);
        
        if (logStateChanges)
        {
            Log($"State changed: {previousState} -> {newState}");
        }
    }

    private void OnStateEnter(AnimalState state)
    {
        switch (state)
        {
            case AnimalState.Idle:
                ScheduleNextWander();
                break;
                
            case AnimalState.Wandering:
                PickWanderTarget();
                break;
                
            case AnimalState.Fleeing:
                UpdateFleeDirection();
                onPlayerDetected?.Invoke();
                break;
                
            case AnimalState.Alert:
                // Stop and look at player
                break;
        }
    }

    private void UpdateStateBehavior()
    {
        switch (currentState)
        {
            case AnimalState.Idle:
                HandleIdle();
                break;
                
            case AnimalState.Wandering:
                HandleWandering();
                break;
                
            case AnimalState.Fleeing:
                HandleFleeing();
                break;
                
            case AnimalState.Alert:
                HandleAlert();
                break;
        }
    }

    #endregion

    #region Player Detection

    private void CheckPlayerProximity()
    {
        if (playerTransform == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        bool shouldFlee = false;
        
        if (cachedPlayerController != null)
        {
            shouldFlee = ShouldFleeFromPlayer(distanceToPlayer);
        }
        else
        {
            // Fallback: simple distance check
            shouldFlee = distanceToPlayer <= config.detectionRadius;
        }
        
        // State transitions based on player proximity
        if (shouldFlee && currentState != AnimalState.Fleeing)
        {
            ChangeState(AnimalState.Fleeing);
        }
        else if (!shouldFlee && currentState == AnimalState.Fleeing)
        {
            if (distanceToPlayer > config.detectionRadius * 2f)
            {
                onPlayerLost?.Invoke();
                ChangeState(AnimalState.Idle);
            }
        }
    }

    private bool ShouldFleeFromPlayer(float distance)
    {
        if (cachedPlayerController.IsCrouching())
        {
            return distance <= config.closeDetectionRadius;
        }
        else if (cachedPlayerController.IsWalking() || cachedPlayerController.IsSprinting())
        {
            return distance <= config.detectionRadius;
        }
        
        return false;
    }

    #endregion

    #region Idle Behavior

    private void HandleIdle()
    {
        if (Time.time >= nextWanderTime)
        {
            ChangeState(AnimalState.Wandering);
        }
    }

    private void ScheduleNextWander()
    {
        float variance = config.wanderInterval * 0.3f;
        nextWanderTime = Time.time + Random.Range(config.wanderInterval - variance, config.wanderInterval + variance);
    }

    #endregion

    #region Wander Behavior

    private void HandleWandering()
    {
        // Check if reached wander target
        float distanceToTarget = Vector3.Distance(transform.position, wanderTarget);
        
        if (distanceToTarget < config.wanderStoppingDistance)
        {
            ChangeState(AnimalState.Idle);
            return;
        }
        
        // Occasionally change direction for natural movement
        if (Random.value < config.wanderDirectionChange * Time.deltaTime)
        {
            PickWanderTarget();
        }
    }

    private void PickWanderTarget()
    {
        Vector3 randomDirection = Random.insideUnitSphere * config.wanderRadius;
        randomDirection.y = 0f;
        wanderTarget = transform.position + randomDirection;
    }

    #endregion

    #region Flee Behavior

    private void HandleFleeing()
    {
        if (Time.time >= nextFleeUpdateTime)
        {
            UpdateFleeDirection();
            nextFleeUpdateTime = Time.time + config.fleeUpdateInterval;
        }
    }

    private void UpdateFleeDirection()
    {
        if (playerTransform == null) return;
        
        // Calculate flee direction away from player
        fleeDirection = (transform.position - playerTransform.position).normalized;
        fleeDirection.y = 0;
        
        // Add variation to make flee behavior less predictable
        float randomAngle = Random.Range(-config.fleeAngleVariation, config.fleeAngleVariation);
        fleeDirection = Quaternion.Euler(0, randomAngle, 0) * fleeDirection;
    }

    #endregion

    #region Alert Behavior

    private void HandleAlert()
    {
        // Stay still and watch player
        float timeInAlert = Time.time - stateEnterTime;
        
        if (timeInAlert > 2f) // After 2 seconds, decide what to do
        {
            if (playerTransform != null)
            {
                float distance = Vector3.Distance(transform.position, playerTransform.position);
                if (distance < config.detectionRadius * 0.5f)
                {
                    ChangeState(AnimalState.Fleeing);
                }
                else
                {
                    ChangeState(AnimalState.Idle);
                }
            }
            else
            {
                ChangeState(AnimalState.Idle);
            }
        }
    }

    #endregion

    #region Movement Output

    private void UpdateMovementOutput()
    {
        switch (currentState)
        {
            case AnimalState.Idle:
                DesiredDirection = Vector3.zero;
                DesiredSpeed = 0f;
                ShouldRun = false;
                LookTarget = transform.position + transform.forward * 10f;
                break;
                
            case AnimalState.Wandering:
                DesiredDirection = (wanderTarget - transform.position).normalized;
                DesiredSpeed = config.walkSpeed;
                ShouldRun = false;
                LookTarget = wanderTarget;
                break;
                
            case AnimalState.Fleeing:
                DesiredDirection = fleeDirection;
                DesiredSpeed = config.runSpeed;
                ShouldRun = true;
                LookTarget = transform.position + fleeDirection * 10f;
                break;
                
            case AnimalState.Alert:
                DesiredDirection = Vector3.zero;
                DesiredSpeed = 0f;
                ShouldRun = false;
                if (playerTransform != null)
                {
                    LookTarget = playerTransform.position;
                }
                break;
                
            case AnimalState.Dead:
                DesiredDirection = Vector3.zero;
                DesiredSpeed = 0f;
                ShouldRun = false;
                break;
        }
    }

    #endregion

    #region Health System

    public void TakeDamage(int damage)
    {
        if (currentState == AnimalState.Dead) return;
        
        currentHP -= damage;
        
        if (currentHP <= 0)
        {
            Die();
        }
        else
        {
            // Flee when damaged
            if (currentState != AnimalState.Fleeing)
            {
                ChangeState(AnimalState.Fleeing);
            }
        }
    }

    private void Die()
    {
        ChangeState(AnimalState.Dead);
        
        // Movement system will handle death animation/physics
        Destroy(gameObject, 5f); // Cleanup after 5 seconds
    }

    #endregion

    #region Debug & Logging

    private void Log(string message)
    {
        if (logStateChanges)
        {
            Debug.Log($"[{gameObject.name}] {message}", this);
        }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[{gameObject.name}] {message}", this);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos || config == null) return;
        
        // Detection radii
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, config.detectionRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, config.closeDetectionRadius);
        
        // Current target/direction
        if (currentState == AnimalState.Wandering)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, wanderTarget);
            Gizmos.DrawWireSphere(wanderTarget, 0.5f);
        }
        else if (currentState == AnimalState.Fleeing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, fleeDirection * 5f);
        }
        
        // Look target
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position + Vector3.up, LookTarget);
        
        // State indicator
        Gizmos.color = GetStateColor();
        Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.3f);
    }

    private Color GetStateColor()
    {
        switch (currentState)
        {
            case AnimalState.Idle: return Color.white;
            case AnimalState.Wandering: return Color.green;
            case AnimalState.Fleeing: return Color.red;
            case AnimalState.Alert: return Color.yellow;
            case AnimalState.Dead: return Color.black;
            default: return Color.gray;
        }
    }

    #endregion
}

