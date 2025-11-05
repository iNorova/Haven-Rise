using System;
using UnityEditor;
using UnityEngine;

namespace Controller
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    [DisallowMultipleComponent]
    public class CreatureMover : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField]
        private float m_WalkSpeed = 1f;
        [SerializeField]
        private float m_RunSpeed = 4f;
        [SerializeField]
        private float m_FleeSpeed = 6f;
        [SerializeField, Range(0f, 360f)]
        private float m_RotateSpeed = 90f;
        [SerializeField]
        private Space m_Space = Space.Self;
        [SerializeField]
        private float m_JumpHeight = 5f;

        [Header("Player Detection")]
        [SerializeField, Tooltip("Distance at which the animal detects walking/sprinting player")]
        private float m_DetectionRadius = 10f;
        [SerializeField, Tooltip("Distance at which the animal detects crouching player")]
        private float m_CloseDetectionRadius = 2f;
        [SerializeField, Tooltip("Reference to the player transform. Will auto-find if not set")]
        private Transform m_PlayerTransform;
        [SerializeField, Tooltip("How far the animal tries to flee from the player")]
        private float m_FleeDistance = 20f;
        [SerializeField, Tooltip("Distance player must be beyond before animal stops fleeing (prevents jittering). Should be larger than Detection Radius")]
        private float m_FleeStopRadius = 25f;
        [SerializeField, Tooltip("Distance player must be beyond before animal stops fleeing when crouching (prevents jittering)")]
        private float m_CloseFleeStopRadius = 5f;
        [SerializeField, Tooltip("How quickly the animal turns when fleeing (higher = faster)")]
        private float m_FleeTurnSpeed = 180f;
        [SerializeField, Tooltip("Minimum time between state changes (in seconds)")]
        private float m_StateChangeCooldown = 1f;
        [SerializeField, Tooltip("How far to check for obstacles when fleeing")]
        private float m_ObstacleCheckDistance = 2f;
        [SerializeField, Tooltip("Layer mask for obstacle detection")]
        private LayerMask m_ObstacleLayerMask;
        [SerializeField, Tooltip("Layer mask for structures/trees to avoid (Tree and Structure layers)")]
        private LayerMask m_StructureLayerMask;
        [SerializeField, Tooltip("Number of raycasts to check for obstacles")]
        private int m_RaycastCount = 5;
        [SerializeField, Tooltip("Spread angle for raycasts in degrees")]
        private float m_RaycastSpread = 30f;
        [SerializeField, Tooltip("Height of raycasts from ground")]
        private float m_RaycastHeight = 0.5f;
        [SerializeField, Tooltip("How far ahead to check for obstacles when fleeing")]
        private float m_FleeObstacleCheckDistance = 3f;
        [SerializeField, Tooltip("How strongly to avoid obstacles (higher = more avoidance)")]
        private float m_ObstacleAvoidanceStrength = 2f;
        [SerializeField, Tooltip("Minimum distance to maintain from player")]
        private float m_MinPlayerDistance = 5f;
        [SerializeField, Tooltip("How quickly the animal accelerates when fleeing")]
        private float m_FleeAcceleration = 2f;

        [Header("Animator")]
        [SerializeField]
        private string m_VerticalID = "Vert";
        [SerializeField]
        private string m_StateID = "State";
        [SerializeField]
        private LookWeight m_LookWeight = new(1f, 0.3f, 0.7f, 1f);

        [Header("Ground Detection")]
        [SerializeField, Tooltip("Layer mask for ground detection")]
        private LayerMask m_GroundLayerMask;
        [SerializeField, Tooltip("How far to check for ground")]
        private float m_GroundCheckDistance = 0.5f;
        [SerializeField, Tooltip("Maximum slope angle the animal can climb")]
        private float m_MaxSlopeAngle = 45f;
        [SerializeField, Tooltip("How high to check for ground")]
        private float m_GroundCheckHeight = 0.2f;

        private Transform m_Transform;
        private CharacterController m_Controller;
        private Animator m_Animator;

        private MovementHandler m_Movement;
        private AnimationHandler m_Animation;

        private Vector2 m_Axis;
        private Vector3 m_Target;
        private bool m_IsRun;
        private bool m_IsMoving;
        private bool m_IsFleeing;
        private Vector3 m_LastFleeDirection;
        private float m_LastStateChangeTime;
        private Vector3 m_LastPlayerPosition;
        private float m_PlayerMovementThreshold = 0.5f;
        private float m_CurrentFleeSpeed;
        private Vector3 m_SmoothedFleeDirection;

        private Vector3 m_FleeVelocity = Vector3.zero;
        private float m_FleeSpeedVelocity = 0f;

        private Vector3 m_LastGroundedPosition;
        private float m_TimeSinceLastGrounded;
        private const float MAX_TIME_OFF_GROUND = 1f;

        private Vector3 m_WanderTarget;
        private float m_NextWanderTime;
        private float m_DistanceWhenFleeStarted; // Track distance when fleeing began
        private Vector3 m_LastPosition; // Track position to detect if stuck
        private float m_StuckCheckTimer = 0f;
        private const float STUCK_CHECK_INTERVAL = 0.5f; // Check every 0.5 seconds
        private const float STUCK_THRESHOLD = 0.2f; // If moved less than 0.2 units, considered stuck
        private float m_LastStuckTime = 0f;
        private Vector3 m_LastStuckAvoidDirection; // Remember last avoidance direction when stuck
        private float m_TimeFleeing = 0f; // Track how long we've been fleeing
        private float m_PlayerStationaryTime = 0f; // Track how long player has been stationary
        private const float MAX_FLEE_TIME = 10f; // Maximum time to flee before forcing idle
        private const float PLAYER_STATIONARY_THRESHOLD = 0.1f; // Player movement threshold to consider stationary
        private const float PLAYER_STATIONARY_TIME_FOR_IDLE = 2f; // If player stationary for 2s, easier to go idle
        private Vector3 m_FleeStartPosition; // Track where we started fleeing
        private float m_CircleCheckTimer = 0f; // Timer for checking if circling
        private const float CIRCLE_CHECK_INTERVAL = 3f; // Check every 3 seconds
        private const float CIRCLE_DETECTION_RADIUS = 5f; // If we're within this radius of start position, might be circling

        // Public properties for debugging
        public bool IsFleeing => m_IsFleeing;
        public float CurrentDetectionRadius => m_DetectionRadius;
        public float CurrentCloseDetectionRadius => m_CloseDetectionRadius;

        public Vector2 Axis => m_Axis;
        public Vector3 Target => m_Target;
        public bool IsRun => m_IsRun;

        private void OnValidate()
        {
            m_WalkSpeed = Mathf.Max(m_WalkSpeed, 0f);
            m_RunSpeed = Mathf.Max(m_RunSpeed, m_WalkSpeed);
            m_FleeSpeed = Mathf.Max(m_FleeSpeed, m_RunSpeed);
            m_DetectionRadius = Mathf.Max(m_DetectionRadius, 0f);
            m_CloseDetectionRadius = Mathf.Max(m_CloseDetectionRadius, 0f);
            m_FleeDistance = Mathf.Max(m_FleeDistance, m_DetectionRadius);
            m_FleeStopRadius = Mathf.Max(m_FleeStopRadius, m_DetectionRadius * 1.5f); // Ensure it's larger than detection
            m_CloseFleeStopRadius = Mathf.Max(m_CloseFleeStopRadius, m_CloseDetectionRadius * 2f); // Ensure it's larger than close detection
            m_FleeTurnSpeed = Mathf.Max(m_FleeTurnSpeed, 0f);
            m_StateChangeCooldown = Mathf.Max(m_StateChangeCooldown, 0.1f);
            m_ObstacleCheckDistance = Mathf.Max(m_ObstacleCheckDistance, 0.5f);
            m_FleeObstacleCheckDistance = Mathf.Max(m_FleeObstacleCheckDistance, m_ObstacleCheckDistance);
            m_ObstacleAvoidanceStrength = Mathf.Max(m_ObstacleAvoidanceStrength, 0.1f);

            m_Movement?.SetStats(m_WalkSpeed / 3.6f, m_RunSpeed / 3.6f, m_RotateSpeed, m_JumpHeight, m_Space);
        }

        private void Awake()
        {
            m_Transform = transform;
            m_Controller = GetComponent<CharacterController>();
            m_Animator = GetComponent<Animator>();

            m_Movement = new MovementHandler(m_Controller, m_Transform, m_WalkSpeed, m_RunSpeed, m_RotateSpeed, m_JumpHeight, m_Space);
            m_Animation = new AnimationHandler(m_Animator, m_VerticalID, m_StateID);

            // Find player if not assigned
            if (m_PlayerTransform == null)
            {
                m_PlayerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            }

            // Initialize variables
            if (m_PlayerTransform != null)
            {
                m_LastPlayerPosition = m_PlayerTransform.position;
            }
            m_SmoothedFleeDirection = m_Transform.forward;
            m_CurrentFleeSpeed = m_WalkSpeed;
            m_LastGroundedPosition = m_Transform.position;
            m_DistanceWhenFleeStarted = float.MaxValue;
            m_LastPosition = m_Transform.position;
            m_StuckCheckTimer = 0f;
            m_LastStuckTime = 0f;
            m_LastStuckAvoidDirection = Vector3.zero;
            m_TimeFleeing = 0f;
            m_PlayerStationaryTime = 0f;
            m_FleeStartPosition = m_Transform.position;
            m_CircleCheckTimer = 0f;
            
            // Auto-setup structure layer mask if not configured (Tree and Structure layers)
            if (m_StructureLayerMask == 0)
            {
                int treeLayer = LayerMask.NameToLayer("Tree");
                int structureLayer = LayerMask.NameToLayer("Structure");
                if (treeLayer != -1) m_StructureLayerMask |= (1 << treeLayer);
                if (structureLayer != -1) m_StructureLayerMask |= (1 << structureLayer);
                if (m_StructureLayerMask != 0)
                {
                    Debug.Log($"CreatureMover: Auto-configured Structure Layer Mask to include Tree and Structure layers");
                }
            }
        }

        private void Update()
        {
            CheckPlayerProximity();
            CheckGroundStatus();
            
            if (m_IsFleeing)
            {
                HandleFleeing();
                // When fleeing, animation state should be 'run'
                m_Animation.Animate(m_Axis, 1f, Time.deltaTime); 
            }
            else
            {
                // Wandering logic
                if (Vector3.Distance(m_Transform.position, m_WanderTarget) < 1f || Time.time > m_NextWanderTime)
                {
                    SetNewWanderTarget();
                }
                Vector3 direction = (m_WanderTarget - m_Transform.position).normalized;
                m_Axis = new Vector2(direction.x, direction.z);
                m_IsRun = false; 
                m_IsMoving = true; // Set to true for wandering movement

                // Determine animation state for wandering: idle or walk
                float targetAnimState = 0f; // Default to idle
                if (m_Axis.sqrMagnitude > 0.01f) // If there's actual movement input
                {
                    targetAnimState = 0.5f; // Set to walk state (assuming 0.5 is walk)
                }

                m_Movement.Move(Time.deltaTime, m_Axis, m_WanderTarget, false, true, out var animAxis, out var isAir);
                m_Animation.Animate(animAxis, targetAnimState, Time.deltaTime); 
            }
        }

        private void CheckPlayerProximity()
        {
            if (m_PlayerTransform == null) return;

            float distanceToPlayer = Vector3.Distance(m_Transform.position, m_PlayerTransform.position);
            
            // Track player movement to detect if stationary
            float playerMovement = Vector3.Distance(m_LastPlayerPosition, m_PlayerTransform.position);
            if (playerMovement < PLAYER_STATIONARY_THRESHOLD)
            {
                m_PlayerStationaryTime += Time.deltaTime;
            }
            else
            {
                m_PlayerStationaryTime = 0f; // Reset if player moved
            }
            
            // Always update last player position
            m_LastPlayerPosition = m_PlayerTransform.position;
            
            // Skip proximity check if player hasn't moved and we're not fleeing (to save performance)
            if (!m_IsFleeing && playerMovement < PLAYER_STATIONARY_THRESHOLD && m_PlayerStationaryTime > 1f)
            {
                return; // Player is stationary, we're not fleeing, no need to check
            }

            CharController_Motor playerController = m_PlayerTransform.GetComponent<CharController_Motor>();

            if (playerController != null)
            {
                if (m_IsFleeing)
                {
                    // Track time spent fleeing
                    m_TimeFleeing += Time.deltaTime;
                    m_CircleCheckTimer += Time.deltaTime;
                    
                    // Check if we're circling (back near where we started fleeing)
                    bool isCircling = false;
                    if (m_CircleCheckTimer >= CIRCLE_CHECK_INTERVAL && m_TimeFleeing > 3f)
                    {
                        float distanceFromStart = Vector3.Distance(m_Transform.position, m_FleeStartPosition);
                        if (distanceFromStart < CIRCLE_DETECTION_RADIUS && distanceToPlayer < m_FleeStopRadius * 0.8f)
                        {
                            // We're back near where we started and player is still close - likely circling
                            isCircling = true;
                            Debug.LogWarning($"CreatureMover: Detected circling behavior! Distance from start: {distanceFromStart:F1}m, Player distance: {distanceToPlayer:F1}m");
                        }
                        m_CircleCheckTimer = 0f;
                    }
                    
                    // PRIORITY: Check if player is stationary and far enough - go idle immediately
                    bool playerIsStationary = m_PlayerStationaryTime > PLAYER_STATIONARY_TIME_FOR_IDLE;
                    bool playerIsFarEnough = false;
                    float requiredDistance = m_FleeStopRadius;
                    
                    if (playerController.IsCrouching())
                    {
                        requiredDistance = m_CloseFleeStopRadius;
                    }
                    
                    // If player is stationary, reduce distance requirement significantly
                    if (playerIsStationary)
                    {
                        requiredDistance = m_DetectionRadius * 1.2f; // Only need to be 20% beyond detection radius
                        playerIsFarEnough = distanceToPlayer > requiredDistance;
                        
                        Debug.Log($"CreatureMover: Player stationary for {m_PlayerStationaryTime:F1}s. Distance: {distanceToPlayer:F1}m, Required: {requiredDistance:F1}m");
                    }
                    else
                    {
                        // Normal check - player is moving
                        playerIsFarEnough = distanceToPlayer > requiredDistance;
                        
                        // Also check if we've increased distance from when fleeing started (hysteresis)
                        bool hasIncreasedDistance = distanceToPlayer > m_DistanceWhenFleeStarted + 3f;
                        
                        if (!hasIncreasedDistance && !playerIsFarEnough)
                        {
                            // Not far enough yet, continue fleeing
                            return;
                        }
                    }
                    
                    // If player is far enough (or stationary and far enough), go idle
                    // OR if we're circling and player is stationary, go idle
                    if (playerIsFarEnough || (isCircling && playerIsStationary))
                    {
                        // Check if enough time has passed since last state change
                        if (Time.time - m_LastStateChangeTime >= m_StateChangeCooldown)
                        {
                            string reason = isCircling ? "circling detected" : "player far enough";
                            Debug.Log($"CreatureMover: Going back to idle. Distance: {distanceToPlayer:F1}m, Player stationary: {playerIsStationary}, Flee time: {m_TimeFleeing:F1}s, Reason: {reason}");
                            m_IsFleeing = false;
                            m_LastStateChangeTime = Time.time;
                            m_DistanceWhenFleeStarted = float.MaxValue;
                            m_TimeFleeing = 0f;
                            m_PlayerStationaryTime = 0f;
                            m_CircleCheckTimer = 0f;
                            SetNewWanderTarget();
                            return;
                        }
                    }
                    
                    // Safety: Force idle if fleeing too long (prevents infinite running)
                    if (m_TimeFleeing > MAX_FLEE_TIME)
                    {
                        Debug.LogWarning($"CreatureMover: Forcing idle after {m_TimeFleeing:F1}s of fleeing (max time exceeded)");
                        m_IsFleeing = false;
                        m_LastStateChangeTime = Time.time;
                        m_DistanceWhenFleeStarted = float.MaxValue;
                        m_TimeFleeing = 0f;
                        m_PlayerStationaryTime = 0f;
                        m_CircleCheckTimer = 0f;
                        SetNewWanderTarget();
                        return;
                    }
                    
                    // Force idle if circling and player has been stationary for a while
                    if (isCircling && m_PlayerStationaryTime > PLAYER_STATIONARY_TIME_FOR_IDLE * 1.5f)
                    {
                        Debug.LogWarning($"CreatureMover: Forcing idle - circling detected and player stationary for {m_PlayerStationaryTime:F1}s");
                        m_IsFleeing = false;
                        m_LastStateChangeTime = Time.time;
                        m_DistanceWhenFleeStarted = float.MaxValue;
                        m_TimeFleeing = 0f;
                        m_PlayerStationaryTime = 0f;
                        m_CircleCheckTimer = 0f;
                        SetNewWanderTarget();
                        return;
                    }
                }
                else
                {
                    // Not fleeing - check if we should start fleeing
                    bool shouldFlee = false;
                    float triggerRadius = m_DetectionRadius;
                    
                    // Check if player is crouching
                    if (playerController.IsCrouching())
                    {
                        triggerRadius = m_CloseDetectionRadius;
                        if (distanceToPlayer <= m_CloseDetectionRadius)
                        {
                            shouldFlee = true;
                        }
                    }
                    // Check if player is moving (walking or sprinting)
                    else if (playerController.IsWalking() || playerController.IsSprinting())
                    {
                        if (distanceToPlayer <= m_DetectionRadius)
                        {
                            shouldFlee = true;
                        }
                    }

                    // Only change state if it's different and cooldown has passed
                    if (shouldFlee && Time.time - m_LastStateChangeTime >= m_StateChangeCooldown)
                    {
                        m_IsFleeing = true;
                        m_LastStateChangeTime = Time.time;
                        m_DistanceWhenFleeStarted = distanceToPlayer; // Record distance when fleeing started
                        m_TimeFleeing = 0f; // Reset flee timer
                        m_PlayerStationaryTime = 0f; // Reset stationary timer
                        m_FleeStartPosition = m_Transform.position; // Record starting position
                        m_CircleCheckTimer = 0f; // Reset circle check timer
                        Debug.Log($"CreatureMover: Starting to flee. Distance: {distanceToPlayer:F1}m");
                    }
                }
            }
        }

        private void CheckGroundStatus()
        {
            if (IsGrounded())
            {
                m_LastGroundedPosition = m_Transform.position;
                m_TimeSinceLastGrounded = 0f;
            }
            else
            {
                m_TimeSinceLastGrounded += Time.deltaTime;
                
                // If we've been off ground too long, try to recover
                if (m_TimeSinceLastGrounded > MAX_TIME_OFF_GROUND)
                {
                    Vector3 recoveryDirection = (m_LastGroundedPosition - m_Transform.position).normalized;
                    recoveryDirection.y = 0;
                    m_Transform.position = Vector3.Lerp(m_Transform.position, m_LastGroundedPosition, Time.deltaTime * 2f);
                }
            }
        }

        private bool IsGrounded()
        {
            // Check multiple points around the character for ground
            Vector3[] checkPoints = new Vector3[]
            {
                m_Transform.position,
                m_Transform.position + m_Transform.forward * 0.2f,
                m_Transform.position - m_Transform.forward * 0.2f,
                m_Transform.position + m_Transform.right * 0.2f,
                m_Transform.position - m_Transform.right * 0.2f
            };

            foreach (Vector3 point in checkPoints)
            {
                if (Physics.Raycast(point + Vector3.up * m_GroundCheckHeight, Vector3.down, m_GroundCheckDistance, m_GroundLayerMask))
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleFleeing()
        {
            if (m_PlayerTransform == null) return;

            // Check if stuck (position not changing)
            m_StuckCheckTimer += Time.deltaTime;
            bool isStuck = false;
            if (m_StuckCheckTimer >= STUCK_CHECK_INTERVAL)
            {
                float distanceMoved = Vector3.Distance(m_Transform.position, m_LastPosition);
                isStuck = distanceMoved < STUCK_THRESHOLD;
                m_LastPosition = m_Transform.position;
                m_StuckCheckTimer = 0f;
                
                if (isStuck)
                {
                    m_LastStuckTime = Time.time;
                    Debug.Log($"CreatureMover: Deer is stuck! Distance moved: {distanceMoved:F2}m");
                }
            }

            // Calculate base flee direction (away from player)
            Vector3 toPlayer = m_PlayerTransform.position - m_Transform.position;
            toPlayer.y = 0;
            float distanceToPlayer = toPlayer.magnitude;

            Vector3 fleeDirection;
            
            // If stuck, force a completely different direction
            if (isStuck || (Time.time - m_LastStuckTime < 1f && Time.time - m_LastStuckTime > 0f))
            {
                // When stuck, try perpendicular directions first, then opposite
                Vector3 awayFromPlayer = m_Transform.position - m_PlayerTransform.position;
                awayFromPlayer.y = 0;
                awayFromPlayer.Normalize();
                
                // Try perpendicular first (90 degrees)
                Vector3 perpendicular = Vector3.Cross(awayFromPlayer, Vector3.up);
                if (UnityEngine.Random.value > 0.5f) perpendicular = -perpendicular;
                
                // Check if perpendicular direction is clear
                if (IsDirectionClear(perpendicular, m_FleeObstacleCheckDistance))
                {
                    fleeDirection = perpendicular;
                    m_LastStuckAvoidDirection = perpendicular;
                }
                else
                {
                    // Try opposite direction
                    fleeDirection = -awayFromPlayer;
                    m_LastStuckAvoidDirection = fleeDirection;
                }
                
                Debug.Log($"CreatureMover: Forcing new direction due to being stuck: {fleeDirection}");
            }
            else if (distanceToPlayer < m_MinPlayerDistance)
            {
                // If too close to player, force a stronger flee direction
                float randomAngle = UnityEngine.Random.Range(-60f, 60f);
                fleeDirection = Quaternion.Euler(0, randomAngle, 0) * -toPlayer.normalized;
            }
            else
            {
                fleeDirection = m_Transform.position - m_PlayerTransform.position;
                fleeDirection.y = 0;
                fleeDirection.Normalize();
            }

            // ENSURE flee direction is AWAY from player (never toward)
            Vector3 awayFromPlayerDir = m_Transform.position - m_PlayerTransform.position;
            awayFromPlayerDir.y = 0;
            awayFromPlayerDir.Normalize();
            
            // Verify fleeDirection is not toward player (dot product check)
            float dotTowardPlayer = Vector3.Dot(fleeDirection, toPlayer.normalized);
            if (dotTowardPlayer > 0.1f) // If more than 10% toward player, fix it
            {
                Debug.LogWarning($"CreatureMover: Flee direction was toward player! Fixing... Dot: {dotTowardPlayer:F2}");
                fleeDirection = awayFromPlayerDir; // Force away from player
            }

            // Check for structures/trees DIRECTLY AHEAD in the intended flee direction
            if (CheckForObstacleAhead(fleeDirection, m_FleeObstacleCheckDistance))
            {
                // Something directly ahead in flee direction - force avoidance
                fleeDirection = GetAvoidanceDirection(fleeDirection, fleeDirection, awayFromPlayerDir);
            }

            // Check for nearby structures/trees and adjust flee direction to avoid them (more aggressive)
            fleeDirection = AvoidNearbyStructures(fleeDirection, isStuck);
            
            // Final safety check: ensure we're still not moving toward player after all adjustments
            dotTowardPlayer = Vector3.Dot(fleeDirection, toPlayer.normalized);
            if (dotTowardPlayer > 0.1f)
            {
                Debug.LogWarning($"CreatureMover: After avoidance, direction still toward player! Forcing away. Dot: {dotTowardPlayer:F2}");
                fleeDirection = awayFromPlayerDir; // Force away from player as last resort
            }

            // Smooth the flee direction with damping (less smoothing if stuck)
            float smoothTime = isStuck ? 0.1f : 0.3f; // Faster response when stuck
            m_SmoothedFleeDirection = Vector3.SmoothDamp(
                m_SmoothedFleeDirection,
                fleeDirection,
                ref m_FleeVelocity,
                smoothTime
            );
            m_SmoothedFleeDirection.Normalize();

            // Check for obstacles in the smoothed direction (with longer range for fleeing)
            Vector3 finalFleeDirection = GetSafeFleeDirection(m_SmoothedFleeDirection, true);

            // Smoothly rotate towards the flee direction with damping
            Quaternion targetRotation = Quaternion.LookRotation(finalFleeDirection);
            m_Transform.rotation = Quaternion.Slerp(
                m_Transform.rotation,
                targetRotation,
                Time.deltaTime * m_FleeTurnSpeed
            );

            // Gradually increase flee speed with damping
            m_CurrentFleeSpeed = Mathf.SmoothDamp(
                m_CurrentFleeSpeed,
                m_FleeSpeed,
                ref m_FleeSpeedVelocity,
                smoothTime
            );

            // Calculate a point to flee to with some randomness
            Vector3 fleeTarget = m_Transform.position + finalFleeDirection * m_FleeDistance;
            fleeTarget += new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                0,
                UnityEngine.Random.Range(-1f, 1f)
            ) * 2f;

            // Set movement parameters for fleeing
            m_Axis = new Vector2(finalFleeDirection.x, finalFleeDirection.z);
            m_IsRun = true;
            m_IsMoving = true;

            // Store the last flee direction for smooth transitions
            m_LastFleeDirection = finalFleeDirection;

            // Move the creature with current flee speed
            Vector2 animAxis;
            bool isAir;
            m_Movement.SetStats(m_WalkSpeed / 3.6f, m_CurrentFleeSpeed / 3.6f, m_RotateSpeed, m_JumpHeight, m_Space);
            m_Movement.Move(Time.deltaTime, m_Axis, fleeTarget, true, true, out animAxis, out isAir);
            m_Animation.Animate(animAxis, 1f, Time.deltaTime);
        }

        private Vector3 GetSafeFleeDirection(Vector3 preferredDirection, bool isFleeing = false)
        {
            // Use longer check distance when fleeing
            float checkDistance = isFleeing ? m_FleeObstacleCheckDistance : m_ObstacleCheckDistance;
            
            // Create a fan of raycasts (wider spread when fleeing)
            float spread = isFleeing ? m_RaycastSpread * 1.5f : m_RaycastSpread;
            float angleStep = spread / (m_RaycastCount - 1);
            float startAngle = -spread / 2f;

            // Store the best direction found
            Vector3 bestDirection = preferredDirection;
            float bestScore = float.MinValue;

            // Check each raycast direction
            for (int i = 0; i < m_RaycastCount; i++)
            {
                float currentAngle = startAngle + (angleStep * i);
                Vector3 direction = Quaternion.Euler(0, currentAngle, 0) * preferredDirection;
                
                // Calculate score for this direction
                float score = EvaluateDirection(direction, checkDistance);
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDirection = direction;
                }
            }

            // If no good direction found, try to find any clear path
            if (bestScore <= 0)
            {
                // Try more directions in a wider spread (360 degrees)
                for (int i = 0; i < 16; i++)
                {
                    float angle = i * 22.5f; // 16 directions = 22.5 degrees each
                    Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                    
                    if (IsDirectionClear(direction, checkDistance))
                    {
                        return direction;
                    }
                }
                
                // If still no clear path, try moving perpendicular to obstacles
                RaycastHit hit;
                LayerMask combinedMask = m_ObstacleLayerMask | m_StructureLayerMask;
                if (Physics.Raycast(m_Transform.position + Vector3.up * m_RaycastHeight, preferredDirection, out hit, checkDistance, combinedMask))
                {
                    Vector3 avoidDirection = Vector3.Cross(hit.normal, Vector3.up);
                    if (Vector3.Dot(avoidDirection, preferredDirection) < 0)
                    {
                        avoidDirection = -avoidDirection;
                    }
                    return avoidDirection.normalized;
                }
            }

            return bestDirection;
        }

        private bool CheckForObstacleAhead(Vector3 direction, float distance)
        {
            if (m_StructureLayerMask == 0 && m_ObstacleLayerMask == 0) return false;
            
            LayerMask combinedMask = m_ObstacleLayerMask | m_StructureLayerMask;
            Vector3 rayStart = m_Transform.position + Vector3.up * m_RaycastHeight;
            
            // Check multiple points ahead (center, left, right)
            Vector3[] checkPoints = new Vector3[]
            {
                direction,
                Quaternion.Euler(0, -15f, 0) * direction, // Left
                Quaternion.Euler(0, 15f, 0) * direction  // Right
            };
            
            foreach (Vector3 checkDir in checkPoints)
            {
                if (Physics.Raycast(rayStart, checkDir, distance, combinedMask))
                {
                    return true;
                }
            }
            
            return false;
        }

        private Vector3 GetAvoidanceDirection(Vector3 preferredDirection, Vector3 blockedDirection, Vector3 awayFromPlayer)
        {
            // Try perpendicular directions relative to blocked direction (left and right)
            Vector3 perpendicular = Vector3.Cross(blockedDirection, Vector3.up);
            
            // Try left first (perpendicular)
            Vector3 leftDir = perpendicular.normalized;
            // Ensure left direction is not toward player
            float leftDot = Vector3.Dot(leftDir, (m_PlayerTransform.position - m_Transform.position).normalized);
            if (leftDot < 0.1f && IsDirectionClear(leftDir, m_FleeObstacleCheckDistance))
            {
                return leftDir;
            }
            
            // Try right (opposite perpendicular)
            Vector3 rightDir = -perpendicular.normalized;
            float rightDot = Vector3.Dot(rightDir, (m_PlayerTransform.position - m_Transform.position).normalized);
            if (rightDot < 0.1f && IsDirectionClear(rightDir, m_FleeObstacleCheckDistance))
            {
                return rightDir;
            }
            
            // Try angles around the preferred direction (away from player) - 45, 90, 135 degrees
            for (int i = 1; i <= 4; i++)
            {
                float angle = i * 45f; // 45, 90, 135, 180 degrees
                
                // Try both left and right of preferred direction
                Vector3 testDir1 = Quaternion.Euler(0, angle, 0) * awayFromPlayer;
                Vector3 testDir2 = Quaternion.Euler(0, -angle, 0) * awayFromPlayer;
                
                // Check if these directions are away from player
                float dot1 = Vector3.Dot(testDir1, (m_PlayerTransform.position - m_Transform.position).normalized);
                float dot2 = Vector3.Dot(testDir2, (m_PlayerTransform.position - m_Transform.position).normalized);
                
                if (dot1 < 0.1f && IsDirectionClear(testDir1, m_FleeObstacleCheckDistance))
                {
                    return testDir1.normalized;
                }
                if (dot2 < 0.1f && IsDirectionClear(testDir2, m_FleeObstacleCheckDistance))
                {
                    return testDir2.normalized;
                }
            }
            
            // Last resort: return perpendicular (but ensure it's not toward player)
            if (Vector3.Dot(perpendicular.normalized, (m_PlayerTransform.position - m_Transform.position).normalized) < 0.1f)
            {
                return perpendicular.normalized;
            }
            
            // Absolute last resort: just go away from player
            Debug.LogWarning("CreatureMover: All avoidance directions blocked, forcing away from player");
            return awayFromPlayer;
        }

        private Vector3 AvoidNearbyStructures(Vector3 preferredDirection, bool isStuck = false)
        {
            if (m_StructureLayerMask == 0) return preferredDirection; // No structure layer set
            
            // Larger avoidance radius when stuck
            float avoidanceRadius = isStuck ? 8f : 6f; // Increased from 5f
            
            Collider[] nearbyStructures = Physics.OverlapSphere(
                m_Transform.position,
                avoidanceRadius,
                m_StructureLayerMask
            );
            
            if (nearbyStructures.Length == 0) return preferredDirection;
            
            // Calculate avoidance vector (away from structures)
            Vector3 avoidanceVector = Vector3.zero;
            float closestStructureDistance = float.MaxValue;
            
            foreach (Collider structure in nearbyStructures)
            {
                if (structure == null) continue;
                
                // Get closest point on structure bounds
                Vector3 structurePos = structure.ClosestPoint(m_Transform.position);
                Vector3 toStructure = structurePos - m_Transform.position;
                toStructure.y = 0;
                float distance = toStructure.magnitude;
                
                if (distance < 0.1f) continue;
                
                // Track closest structure
                if (distance < closestStructureDistance)
                {
                    closestStructureDistance = distance;
                }
                
                // Much stronger avoidance - inverse square law
                float avoidanceStrength = m_ObstacleAvoidanceStrength / (distance * distance);
                avoidanceVector -= toStructure.normalized * avoidanceStrength;
            }
            
            // If we have an avoidance vector, blend it more aggressively
            if (avoidanceVector.sqrMagnitude > 0.01f)
            {
                avoidanceVector.Normalize();
                
                // More aggressive blending - especially if stuck or very close to structure
                float blendRatio = isStuck ? 0.3f : 0.5f; // When stuck, only 30% preferred, 70% avoidance
                if (closestStructureDistance < 2f)
                {
                    blendRatio = 0.2f; // Very close = 80% avoidance
                }
                
                Vector3 blendedDirection = (preferredDirection * blendRatio + avoidanceVector * (1f - blendRatio) * m_ObstacleAvoidanceStrength).normalized;
                
                // Safety check: ensure blended direction is not toward player
                if (m_PlayerTransform != null)
                {
                    Vector3 toPlayer = (m_PlayerTransform.position - m_Transform.position);
                    toPlayer.y = 0;
                    float dotTowardPlayer = Vector3.Dot(blendedDirection, toPlayer.normalized);
                    if (dotTowardPlayer > 0.1f)
                    {
                        // Blended direction is toward player - fall back to preferred direction
                        Debug.LogWarning($"CreatureMover: AvoidNearbyStructures created direction toward player! Dot: {dotTowardPlayer:F2}. Using preferred direction.");
                        return preferredDirection;
                    }
                }
                
                return blendedDirection;
            }
            
            return preferredDirection;
        }

        private float EvaluateDirection(Vector3 direction, float checkDistance)
        {
            float score = 1f;
            Vector3 rayStart = m_Transform.position + Vector3.up * m_RaycastHeight;
            LayerMask combinedMask = m_ObstacleLayerMask | m_StructureLayerMask;

            // Check for obstacles and structures
            if (Physics.Raycast(rayStart, direction, out RaycastHit hit, checkDistance, combinedMask))
            {
                // Heavily penalize structures/trees (they're solid obstacles)
                bool isStructure = (m_StructureLayerMask != 0) && ((1 << hit.collider.gameObject.layer) & m_StructureLayerMask) != 0;
                
                if (isStructure)
                {
                    // Completely avoid structures - very heavy penalty
                    score -= 2f * m_ObstacleAvoidanceStrength;
                }
                else
                {
                    // Check slope angle for regular obstacles
                    float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                    if (slopeAngle > m_MaxSlopeAngle)
                    {
                        score -= 1f; // Completely avoid steep slopes
                    }
                    else
                    {
                        // Reduce score based on distance to obstacle and slope
                        score -= (1f - (hit.distance / checkDistance)) * (1f + slopeAngle / m_MaxSlopeAngle);
                    }
                }
            }

            // Check if there's ground to walk on
            Vector3 groundCheckPos = rayStart + direction * checkDistance;
            if (!Physics.Raycast(groundCheckPos + Vector3.up * m_GroundCheckHeight, Vector3.down, m_GroundCheckDistance, m_GroundLayerMask))
            {
                score -= 0.5f; // Penalize directions without ground
            }

            // Check for obstacles/structures at different heights
            for (float height = 0.2f; height <= 1f; height += 0.2f)
            {
                Vector3 heightCheckPos = rayStart + Vector3.up * height;
                if (Physics.Raycast(heightCheckPos, direction, out RaycastHit heightHit, checkDistance, combinedMask))
                {
                    bool isHeightStructure = (m_StructureLayerMask != 0) && ((1 << heightHit.collider.gameObject.layer) & m_StructureLayerMask) != 0;
                    if (isHeightStructure)
                    {
                        score -= 0.5f * m_ObstacleAvoidanceStrength; // Heavy penalty for structures at height
                    }
                    else
                    {
                        score -= 0.2f; // Penalize directions with obstacles at different heights
                    }
                }
            }

            // HEAVILY penalize directions that lead towards the player
            if (m_PlayerTransform != null)
            {
                Vector3 toPlayer = (m_PlayerTransform.position - m_Transform.position);
                toPlayer.y = 0;
                float dotProduct = Vector3.Dot(direction, toPlayer.normalized);
                if (dotProduct > 0.1f) // If direction is even slightly towards player (10% or more)
                {
                    score -= 10f; // HEAVY penalty - effectively makes this direction impossible
                    Debug.LogWarning($"CreatureMover: Direction toward player detected! Dot: {dotProduct:F2}, Score: {score}");
                }
            }

            return score;
        }

        private bool IsDirectionClear(Vector3 direction, float checkDistance = -1f)
        {
            if (checkDistance < 0) checkDistance = m_ObstacleCheckDistance;
            
            Vector3 rayStart = m_Transform.position + Vector3.up * m_RaycastHeight;
            LayerMask combinedMask = m_ObstacleLayerMask | m_StructureLayerMask;
            
            // Check for obstacles and structures
            if (Physics.Raycast(rayStart, direction, checkDistance, combinedMask))
            {
                return false;
            }

            // Check for ground
            Vector3 groundCheckPos = rayStart + direction * checkDistance;
            if (!Physics.Raycast(groundCheckPos + Vector3.up * 0.1f, Vector3.down, 0.2f, m_GroundLayerMask))
            {
                return false;
            }

            return true;
        }

        private void OnAnimatorIK()
        {
            m_Animation.AnimateIK(in m_Target, m_LookWeight);
        }

        public void SetInput(in Vector2 axis, in Vector3 target, in bool isRun, in bool isJump)
        {
            if (m_IsFleeing) return; // Ignore input while fleeing

            m_Axis = axis;
            m_Target = target;
            m_IsRun = isRun;

            if (m_Axis.sqrMagnitude < Mathf.Epsilon)
            {
                m_Axis = Vector2.zero;
                m_IsMoving = false;
            }
            else
            {
                m_Axis = Vector3.ClampMagnitude(m_Axis, 1f);
                m_IsMoving = true;
            }
            
            // Apply movement and animation based on input
            Vector2 animAxis;
            bool isAir;
            m_Movement.Move(Time.deltaTime, m_Axis, m_Target, m_IsRun, m_IsMoving, out animAxis, out isAir);

            // Determine animation state: idle, walk, or run
            float targetAnimState = 0f; // Default to idle
            if (m_IsMoving)
            {
                if (m_IsRun)
                {
                    targetAnimState = 1f; // Run
                }
                else
                {
                    targetAnimState = 0.5f; // Walk
                }
            }
            m_Animation.Animate(animAxis, targetAnimState, Time.deltaTime);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if(hit.normal.y > m_Controller.stepOffset)
            {
                m_Movement.SetSurface(hit.normal);
            }
        }

        private void SetNewWanderTarget()
        {
            // Pick a random point within 5 units of the current position
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * 5f;
            m_WanderTarget = m_Transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            m_NextWanderTime = Time.time + UnityEngine.Random.Range(3f, 7f); // Wander for 3-7 seconds
        }

        [Serializable]
        private struct LookWeight
        {
            public float weight;
            public float body;
            public float head;
            public float eyes;

            public LookWeight(float weight, float body, float head, float eyes)
            {
                this.weight = weight;
                this.body = body;
                this.head = head;
                this.eyes = eyes;
            }
        }

        #region Handlers
        private class MovementHandler
        {
            private readonly CharacterController m_Controller;
            private readonly Transform m_Transform;

            private float m_WalkSpeed;
            private float m_RunSpeed;
            private float m_RotateSpeed;

            private Space m_Space;

            private readonly float m_Luft = 75f;

            private float m_TargetAngle;
            private bool m_IsRotating = false;

            private Vector3 m_Normal;
            private Vector3 m_GravityAcelleration = Physics.gravity;

            private float m_jumpTimer;
            private Vector3 m_LastForward;

            public MovementHandler(CharacterController controller, Transform transform, float walkSpeed, float runSpeed, float rotateSpeed, float jumpHeight, Space space)
            {
                m_Controller = controller;
                m_Transform = transform;

                m_WalkSpeed = walkSpeed;
                m_RunSpeed = runSpeed;
                m_RotateSpeed = rotateSpeed;

                m_Space = space;
            }

            public void SetStats(float walkSpeed, float runSpeed, float rotateSpeed, float jumpHeight, Space space)
            {
                m_WalkSpeed = walkSpeed;
                m_RunSpeed = runSpeed;
                m_RotateSpeed = rotateSpeed;

                m_Space = space;
            }

            public void SetSurface(in Vector3 normal)
            {
                m_Normal = normal;
            }

            public void Move(float deltaTime, in Vector2 axis, in Vector3 target, bool isRun, bool isMoving, out Vector2 animAxis, out bool isAir)
            {
                var cameraLook = Vector3.Normalize(target - m_Transform.position);
                var targetForward = m_LastForward;

                ConvertMovement(in axis, in cameraLook, out var movement);
                if (movement.sqrMagnitude > 0.5f) {
                    m_LastForward = Vector3.Normalize(movement);
                }

                // Smoothly rotate to face movement direction
                if (movement.sqrMagnitude > 0.01f) {
                    Quaternion targetRotation = Quaternion.LookRotation(movement.normalized);
                    m_Transform.rotation = Quaternion.Slerp(m_Transform.rotation, targetRotation, deltaTime * m_RotateSpeed);
                }

                CaculateGravity(deltaTime, out isAir);
                Displace(deltaTime, in movement, isRun);
                GenAnimationAxis(in movement, out animAxis);
            }

            private void ConvertMovement(in Vector2 axis, in Vector3 targetForward, out Vector3 movement)
            {
                Vector3 forward;
                Vector3 right;

                if (m_Space == Space.Self)
                {
                    forward = new Vector3(targetForward.x, 0f, targetForward.z).normalized;
                    right = Vector3.Cross(Vector3.up, forward).normalized;
                }
                else
                {
                    forward = Vector3.forward;
                    right = Vector3.right;
                }

                movement = axis.x * right + axis.y * forward;
                movement = Vector3.ProjectOnPlane(movement, m_Normal);
            }

            private void Displace(float deltaTime, in Vector3 movement, bool isRun)
            {
                Vector3 displacement = (isRun ? m_RunSpeed : m_WalkSpeed) * movement;
                displacement += m_GravityAcelleration;
                displacement *= deltaTime;

                m_Controller.Move(displacement);
            }

            private void CaculateGravity(float deltaTime, out bool isAir)
            {
                m_jumpTimer = Mathf.Max(m_jumpTimer - deltaTime, 0f);

                if (m_Controller.isGrounded)
                {
                    m_GravityAcelleration = Physics.gravity;
                    isAir = false;

                    return;
                }

                isAir = true;

                m_GravityAcelleration += Physics.gravity * deltaTime;
                return;
            }

            private void GenAnimationAxis(in Vector3 movement, out Vector2 animAxis)
            {
                if(m_Space == Space.Self)
                {
                    animAxis = new Vector2(Vector3.Dot(movement, m_Transform.right), Vector3.Dot(movement, m_Transform.forward));
                }
                else
                {
                    animAxis = new Vector2(Vector3.Dot(movement, Vector3.right), Vector3.Dot(movement, Vector3.forward));
                }
            }

            private void Turn(in Vector3 targetForward, bool isMoving)
            {
                var angle = Vector3.SignedAngle(m_Transform.forward, Vector3.ProjectOnPlane(targetForward, Vector3.up), Vector3.up);

                if (!m_IsRotating)
                {
                    if (!isMoving && Mathf.Abs(angle) < m_Luft)
                    {
                        m_IsRotating = false;
                        return;
                    }

                    m_IsRotating = true;
                }

                m_TargetAngle = angle;
            }

            private void UpdateRotation(float deltaTime)
            {
                if(!m_IsRotating)
                {
                    return;
                }

                var rotDelta = m_RotateSpeed * deltaTime;
                if (rotDelta + Mathf.PI * 2f + Mathf.Epsilon >= Mathf.Abs(m_TargetAngle))
                {
                    rotDelta = m_TargetAngle;
                    m_IsRotating = false;
                }
                else
                {
                    rotDelta *= Mathf.Sign(m_TargetAngle);
                }

                m_Transform.Rotate(Vector3.up, rotDelta);
            }
        }

        private class AnimationHandler
        {
            private readonly Animator m_Animator;
            private readonly string m_VerticalID;
            private readonly string m_StateID;

            private readonly float k_InputFlow = 4.5f;

            private float m_FlowState;
            private Vector2 m_FlowAxis;

            public AnimationHandler(Animator animator, string verticalID, string stateID)
            {
                m_Animator = animator;
                m_VerticalID = verticalID;
                m_StateID = stateID;
            }

            public void Animate(in Vector2 axis, float state, float deltaTime)
            {
                // Ensure we have valid input
                if (axis.sqrMagnitude > 0.01f)
                {
                    // Smoothly update the flow axis
                    m_FlowAxis = Vector2.Lerp(m_FlowAxis, axis, deltaTime * k_InputFlow);
                    m_FlowAxis = Vector2.ClampMagnitude(m_FlowAxis, 1f);
                }
                else
                {
                    // Smoothly return to zero when no input
                    m_FlowAxis = Vector2.Lerp(m_FlowAxis, Vector2.zero, deltaTime * k_InputFlow);
                }

                // Smoothly update the state
                m_FlowState = Mathf.Lerp(m_FlowState, state, deltaTime * k_InputFlow);
                m_FlowState = Mathf.Clamp01(m_FlowState);

                // Update animator parameters
                m_Animator.SetFloat(m_VerticalID, m_FlowAxis.magnitude);
                m_Animator.SetFloat(m_StateID, m_FlowState);
            }

            public void AnimateIK(in Vector3 target, in LookWeight lookWeight)
            {
                m_Animator.SetLookAtPosition(target);
                m_Animator.SetLookAtWeight(lookWeight.weight, lookWeight.body, lookWeight.head, lookWeight.eyes);
            }
        }
        #endregion
    }
}