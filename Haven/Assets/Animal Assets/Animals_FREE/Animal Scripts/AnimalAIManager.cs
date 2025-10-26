using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using System.Collections;

public class AnimalAIManager : MonoBehaviour
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
    public UnityEvent onObstacleHit;
    public UnityEvent onStuck;

    // Components (cached)
    private NavMeshAgent navAgent;
    private Animator animator;
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private CharController_Motor cachedPlayerController;
    
    // Animation parameters (cached)
    private static readonly int State = Animator.StringToHash("State");
    private static readonly int Vert = Animator.StringToHash("Vert");
    private static readonly int Speed = Animator.StringToHash("Speed");
    
    // States
    public enum AnimalState { Idle, Wandering, Fleeing, Stuck, Dead }
    private AnimalState currentState = AnimalState.Idle;
    private AnimalState previousState = AnimalState.Idle;
    
    // Reusable objects (performance)
    private NavMeshPath reusablePath;
    private Vector3[] groundCheckPoints;
    
    // Movement tracking
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private float currentSpeed;
    private float currentRotationSpeed;
    private Vector3 lastFleePosition;
    private Vector3 wanderTarget;
    
    // Timers
    private float nextWanderTime;
    private float nextFleeUpdateTime;
    private float nextAvoidanceCheckTime;
    private float lastBounceTime;
    private float stuckTimer;
    
    // Animation
    private float currentAnimationSpeed;
    
    // Health
    private int currentHP;
    
    // Coroutines
    private Coroutine avoidanceRoutine;
    
    // Debug
    private Vector3 debugObstaclePoint;
    private Vector3 debugBounceDirection;
    private bool debugShowObstacle;

    #region Initialization

    private void Awake()
    {
        // Avoid conflicting movement systems
        var creatureMover = GetComponent<Controller.CreatureMover>();
        if (creatureMover != null)
        {
            LogWarning("CreatureMover detected - disabling AnimalAIManager to avoid conflicts");
            enabled = false;
            return;
        }
        
        // Load default config if none assigned
        if (config == null)
        {
            LogWarning("No config assigned - creating default runtime config");
            config = ScriptableObject.CreateInstance<AnimalAIConfig>();
        }
        
        // Pre-allocate reusable objects
        reusablePath = new NavMeshPath();
        groundCheckPoints = new Vector3[5];
    }

    private void Start()
    {
        CacheComponents();
        ConfigureNavMeshAgent();
        ConfigurePhysics();
        InitializeState();
        FindPlayer();
        
        // Start coroutines for expensive operations
        avoidanceRoutine = StartCoroutine(ObstacleAvoidanceRoutine());
    }

    private void CacheComponents()
    {
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        
        if (navAgent == null)
        {
            LogError("NavMeshAgent component missing!");
        }
    }

    private void ConfigureNavMeshAgent()
    {
        if (navAgent == null) return;
        
        navAgent.acceleration = 8f;
        navAgent.angularSpeed = 0f;
        navAgent.stoppingDistance = config.wanderStoppingDistance;
        navAgent.radius = 0.5f;
        navAgent.height = 1f;
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        navAgent.avoidancePriority = 50;
        navAgent.updateRotation = false;
        navAgent.speed = config.idleSpeed;
        
        EnsureAgentOnNavMesh();
    }

    private void ConfigurePhysics()
    {
        if (config.useKinematicRigidbody && rb != null)
        {
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        
        if (config.useTriggerCollider && capsuleCollider != null)
        {
            capsuleCollider.isTrigger = true;
        }
    }

    private void InitializeState()
    {
        currentHP = config.maxHP;
        lastPosition = transform.position;
        lastRotation = transform.rotation;
        
        if (animator != null)
        {
            animator.SetFloat(Speed, 0f);
        }
        
        ChangeState(AnimalState.Idle);
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

    #endregion

    #region Update Loop

    private void Update()
    {
        if (currentState == AnimalState.Dead) return;
        
        if (!ValidateNavAgent()) return;
        
        UpdateMovementTracking();
        CheckPlayerProximity();
        UpdateStateBehavior();
        UpdateRotation();
        UpdateAnimation();
        DetectStuck();
    }

    private bool ValidateNavAgent()
    {
        if (navAgent == null || !navAgent.enabled) return false;
        
        if (!navAgent.isOnNavMesh)
        {
            if (!EnsureAgentOnNavMesh())
            {
                return false;
            }
        }
        
        return true;
    }

    private void UpdateMovementTracking()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime < 0.0001f) return; // Avoid division by zero
        
        currentSpeed = Vector3.Distance(transform.position, lastPosition) / deltaTime;
        currentRotationSpeed = Quaternion.Angle(transform.rotation, lastRotation) / deltaTime;
        
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    #endregion

    #region State Machine

    private void ChangeState(AnimalState newState)
    {
        if (currentState == newState) return;
        
        previousState = currentState;
        currentState = newState;
        
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
                if (navAgent != null)
                {
                    navAgent.speed = config.idleSpeed;
                    navAgent.isStopped = true;
                }
                ScheduleNextWander();
                break;
                
            case AnimalState.Wandering:
                if (navAgent != null)
                {
                    navAgent.speed = config.walkSpeed;
                    navAgent.isStopped = false;
                }
                break;
                
            case AnimalState.Fleeing:
                if (navAgent != null)
                {
                    navAgent.speed = config.runSpeed;
                    navAgent.isStopped = false;
                }
                UpdateFleeDestination();
                break;
                
            case AnimalState.Stuck:
                onStuck?.Invoke();
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
                
            case AnimalState.Stuck:
                HandleStuck();
                break;
        }
    }

    #endregion

    #region Player Detection

    private void CheckPlayerProximity()
    {
        if (playerTransform == null || currentState == AnimalState.Stuck) return;
        
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
        
        // State transitions
        if (shouldFlee && currentState != AnimalState.Fleeing)
        {
            ChangeState(AnimalState.Fleeing);
        }
        else if (!shouldFlee && currentState == AnimalState.Fleeing)
        {
            if (distanceToPlayer > config.detectionRadius * 2f)
            {
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
            if (TryGetWanderPoint(out wanderTarget))
            {
                ChangeState(AnimalState.Wandering);
            }
            else
            {
                ScheduleNextWander();
            }
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
        if (navAgent.pathPending) return;
        
        // Check if reached destination
        if (navAgent.remainingDistance <= navAgent.stoppingDistance)
        {
            ChangeState(AnimalState.Idle);
            return;
        }
        
        // Occasionally adjust wander direction for more natural movement
        if (Random.value < config.wanderDirectionChange * Time.deltaTime)
        {
            if (TryGetWanderPoint(out Vector3 newTarget))
            {
                SetDestinationSafely(newTarget);
            }
        }
    }

    private bool TryGetWanderPoint(out Vector3 result)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * config.wanderRadius;
            randomDirection.y = 0f;
            Vector3 samplePos = transform.position + randomDirection;
            
            if (NavMesh.SamplePosition(samplePos, out NavMeshHit hit, config.wanderRadius, NavMesh.AllAreas))
            {
                result = hit.position;
                
                if (SetDestinationSafely(result))
                {
                    return true;
                }
            }
        }
        
        result = transform.position;
        return false;
    }

    #endregion

    #region Flee Behavior

    private void HandleFleeing()
    {
        if (Time.time >= nextFleeUpdateTime)
        {
            UpdateFleeDestination();
            nextFleeUpdateTime = Time.time + config.fleeUpdateInterval;
        }
    }

    private void UpdateFleeDestination()
    {
        if (playerTransform == null) return;
        
        Vector3 fleeDirection = (transform.position - playerTransform.position).normalized;
        fleeDirection.y = 0;
        
        // Add variation to make flee behavior less predictable
        float randomAngle = Random.Range(-config.fleeAngleVariation, config.fleeAngleVariation);
        fleeDirection = Quaternion.Euler(0, randomAngle, 0) * fleeDirection;
        
        // Try multiple distances
        float currentDistToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        for (float distance = config.fleeDistance; distance <= config.maxFleeDistance; distance += config.fleeDistance)
        {
            Vector3 fleePosition = transform.position + fleeDirection * distance;
            
            if (NavMesh.SamplePosition(fleePosition, out NavMeshHit hit, distance, NavMesh.AllAreas))
            {
                float newDistToPlayer = Vector3.Distance(hit.position, playerTransform.position);
                
                if (newDistToPlayer > currentDistToPlayer && 
                    Vector3.Distance(hit.position, lastFleePosition) > config.minFleeDistance)
                {
                    if (SetDestinationSafely(hit.position))
                    {
                        lastFleePosition = hit.position;
                        return;
                    }
                }
            }
        }
        
        // Fallback: try random direction
        TryRandomFleeDirection(currentDistToPlayer);
    }

    private void TryRandomFleeDirection(float currentDistToPlayer)
    {
        Vector3 randomDirection = Random.insideUnitSphere;
        randomDirection.y = 0;
        randomDirection.Normalize();
        
        Vector3 tryPosition = transform.position + randomDirection * config.fleeDistance;
        
        if (NavMesh.SamplePosition(tryPosition, out NavMeshHit hit, config.fleeDistance, NavMesh.AllAreas))
        {
            float newDistToPlayer = Vector3.Distance(hit.position, playerTransform.position);
            if (newDistToPlayer > currentDistToPlayer)
            {
                SetDestinationSafely(hit.position);
                lastFleePosition = hit.position;
            }
        }
    }

    #endregion

    #region Obstacle Avoidance

    private IEnumerator ObstacleAvoidanceRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(config.avoidanceCheckInterval);
            
            if (currentState == AnimalState.Dead || !ValidateNavAgent()) continue;
            
            CheckForObstacles();
        }
    }

    private void CheckForObstacles()
    {
        if (navAgent.velocity.magnitude < 0.1f) return;
        
        // Scale check distance with velocity
        float dynamicCheckDistance = config.obstacleCheckDistance * 
            (1f + (navAgent.velocity.magnitude / config.runSpeed) * config.obstacleCheckDistanceMultiplier);
        
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 direction = navAgent.velocity.normalized;
        
        if (Physics.SphereCast(origin, config.obstacleCheckRadius, direction, 
            out RaycastHit hit, dynamicCheckDistance, config.obstacleLayerMask, QueryTriggerInteraction.Ignore))
        {
            debugObstaclePoint = hit.point;
            debugShowObstacle = true;
            
            HandleObstacleDetected(hit);
        }
        else
        {
            debugShowObstacle = false;
        }
    }

    private void HandleObstacleDetected(RaycastHit hit)
    {
        onObstacleHit?.Invoke();
        
        // Check if cornered (obstacles on multiple sides)
        if (IsCorner())
        {
            HandleCornerSituation();
            return;
        }
        
        // Try bounce
        if (Time.time - lastBounceTime >= config.bounceCooldown)
        {
            TryBounce(hit);
        }
        else
        {
            // Try sidestep
            TrySidestep(hit);
        }
    }

    private bool IsCorner()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        int obstacleCount = 0;
        
        // Check 8 directions
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;
            
            if (Physics.SphereCast(origin, config.obstacleCheckRadius * 0.8f, dir, 
                out _, config.obstacleCheckDistance, config.obstacleLayerMask, QueryTriggerInteraction.Ignore))
            {
                obstacleCount++;
            }
        }
        
        return obstacleCount >= 5; // More than half directions blocked
    }

    private void HandleCornerSituation()
    {
        // Find the most open direction
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        float maxClearance = 0f;
        Vector3 bestDirection = -transform.forward;
        
        for (int i = 0; i < 16; i++)
        {
            float angle = i * 22.5f;
            Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            
            if (Physics.SphereCast(origin, config.obstacleCheckRadius * 0.8f, dir, 
                out RaycastHit hit, config.obstacleCheckDistance * 2f, config.obstacleLayerMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.distance > maxClearance)
                {
                    maxClearance = hit.distance;
                    bestDirection = dir;
                }
            }
            else
            {
                // No hit means clear path
                bestDirection = dir;
                break;
            }
        }
        
        Vector3 escapeTarget = transform.position + bestDirection * config.bounceBackDistance;
        if (TryGetNearestNavMeshPoint(escapeTarget, config.bounceBackDistance, out Vector3 validTarget))
        {
            SetDestinationSafely(validTarget);
        }
    }

    private void TryBounce(RaycastHit hit)
    {
        Vector3 reflectDir = Vector3.Reflect(transform.forward, hit.normal);
        float jitter = Random.Range(-config.bounceAngleJitter, config.bounceAngleJitter);
        reflectDir = Quaternion.Euler(0f, jitter, 0f) * reflectDir;
        reflectDir.y = 0f;
        reflectDir.Normalize();
        
        debugBounceDirection = reflectDir;
        
        Vector3 bounceTarget = transform.position + reflectDir * config.bounceBackDistance;
        
        if (TryGetNearestNavMeshPoint(bounceTarget, config.bounceBackDistance, out Vector3 validTarget))
        {
            if (SetDestinationSafely(validTarget))
            {
                lastBounceTime = Time.time;
                return;
            }
        }
        
        // Fallback to sidestep
        TrySidestep(hit);
    }

    private void TrySidestep(RaycastHit hit)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 left = -transform.right;
        Vector3 right = transform.right;
        
        bool leftClear = !Physics.SphereCast(origin, config.obstacleCheckRadius * 0.8f, left, 
            out _, config.sideStepDistance, config.obstacleLayerMask, QueryTriggerInteraction.Ignore);
        bool rightClear = !Physics.SphereCast(origin, config.obstacleCheckRadius * 0.8f, right, 
            out _, config.sideStepDistance, config.obstacleLayerMask, QueryTriggerInteraction.Ignore);
        
        Vector3 detourDir;
        if (leftClear && !rightClear)
            detourDir = left;
        else if (rightClear && !leftClear)
            detourDir = right;
        else
            detourDir = Random.value > 0.5f ? right : left;
        
        Vector3 detour = transform.position + detourDir * config.sideStepDistance + 
            transform.forward * config.obstacleCheckDistance * 0.5f;
        
        if (TryGetNearestNavMeshPoint(detour, config.sideStepDistance, out Vector3 detourOnMesh))
        {
            SetDestinationSafely(detourOnMesh);
        }
    }

    #endregion

    #region Stuck Detection

    private void DetectStuck()
    {
        if (currentState == AnimalState.Stuck || currentState == AnimalState.Idle) return;
        if (navAgent == null || !navAgent.isOnNavMesh) return;
        
        bool tryingToMove = navAgent.hasPath && !navAgent.pathPending && 
            navAgent.remainingDistance > navAgent.stoppingDistance + 0.2f;
        
        bool movingTooSlow = currentSpeed < config.stuckSpeedThreshold && 
            navAgent.velocity.magnitude < config.stuckSpeedThreshold;
        
        bool spinningInPlace = currentRotationSpeed > config.stuckRotationThreshold && 
            currentSpeed < config.stuckSpeedThreshold;
        
        if (tryingToMove && (movingTooSlow || spinningInPlace))
        {
            stuckTimer += Time.deltaTime;
        }
        else
        {
            stuckTimer = 0f;
        }
        
        if (stuckTimer >= config.stuckTimeThreshold)
        {
            ChangeState(AnimalState.Stuck);
        }
    }

    private void HandleStuck()
    {
        Log("Animal is stuck - attempting recovery");
        
        // Try multiple recovery strategies
        if (TryRecoverFromStuck())
        {
            stuckTimer = 0f;
            ChangeState(previousState == AnimalState.Stuck ? AnimalState.Idle : previousState);
        }
    }

    private bool TryRecoverFromStuck()
    {
        // Strategy 1: Small sidestep
        Vector3 side = (Random.value > 0.5f ? transform.right : -transform.right) * config.sideStepDistance;
        if (SetDestinationSafely(transform.position + side))
        {
            return true;
        }
        
        // Strategy 2: Back up
        Vector3 backward = -transform.forward * config.sideStepDistance;
        if (SetDestinationSafely(transform.position + backward))
        {
            return true;
        }
        
        // Strategy 3: Random nearby point
        if (TryGetWanderPoint(out Vector3 recover))
        {
            return true;
        }
        
        // Strategy 4: Warp to nearest valid NavMesh position
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            if (navAgent.Warp(hit.position))
            {
                return true;
            }
        }
        
        return false;
    }

    #endregion

    #region Movement & Animation

    private void UpdateRotation()
    {
        if (navAgent == null || navAgent.velocity.magnitude < 0.1f) return;
        
        Quaternion targetRotation = Quaternion.LookRotation(navAgent.velocity.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
            Time.deltaTime * config.rotationSpeed);
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;
        
        float targetAnimSpeed = GetTargetAnimationSpeed();
        currentAnimationSpeed = Mathf.Lerp(currentAnimationSpeed, targetAnimSpeed, 
            Time.deltaTime * config.animationBlendSpeed);
        
        animator.SetFloat(State, currentAnimationSpeed);
        animator.SetFloat(Vert, currentAnimationSpeed);
        animator.SetFloat(Speed, currentSpeed * config.animationSpeedMultiplier);
    }

    private float GetTargetAnimationSpeed()
    {
        switch (currentState)
        {
            case AnimalState.Idle:
                return 0f;
            case AnimalState.Wandering:
                return 0.5f;
            case AnimalState.Fleeing:
                return 1f;
            case AnimalState.Stuck:
                return 0.1f;
            default:
                return 0f;
        }
    }

    #endregion

    #region NavMesh Utilities

    private bool SetDestinationSafely(Vector3 target)
    {
        if (navAgent == null || !navAgent.isOnNavMesh) return false;
        
        if (navAgent.CalculatePath(target, reusablePath) && reusablePath.status == NavMeshPathStatus.PathComplete)
        {
            navAgent.isStopped = false;
            navAgent.SetPath(reusablePath);
            return true;
        }
        
        return false;
    }

    private bool TryGetNearestNavMeshPoint(Vector3 position, float searchRadius, out Vector3 result)
    {
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }
        
        result = position;
        return false;
    }

    private bool EnsureAgentOnNavMesh()
    {
        if (navAgent == null || !navAgent.enabled) return false;
        if (navAgent.isOnNavMesh) return true;
        
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            return navAgent.Warp(hit.position);
        }
        
        LogError("Failed to place agent on NavMesh!");
        return false;
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
        
        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.enabled = false;
        }
        
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        if (capsuleCollider != null)
        {
            capsuleCollider.enabled = false;
        }
        
        if (avoidanceRoutine != null)
        {
            StopCoroutine(avoidanceRoutine);
        }
        
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
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

    private void LogError(string message)
    {
        Debug.LogError($"[{gameObject.name}] {message}", this);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos || config == null) return;
        
        // Detection radii
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, config.detectionRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, config.closeDetectionRadius);
        
        // Current path
        if (navAgent != null && navAgent.hasPath)
        {
            Gizmos.color = currentState == AnimalState.Fleeing ? Color.red : Color.green;
            Vector3[] corners = navAgent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }
        
        // Obstacle detection
        if (debugShowObstacle)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(debugObstaclePoint, 0.3f);
            Gizmos.DrawLine(transform.position, debugObstaclePoint);
            
            // Bounce direction
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, debugBounceDirection * config.bounceBackDistance);
        }
        
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
            case AnimalState.Stuck: return Color.yellow;
            case AnimalState.Dead: return Color.black;
            default: return Color.gray;
        }
    }

    #endregion
}
