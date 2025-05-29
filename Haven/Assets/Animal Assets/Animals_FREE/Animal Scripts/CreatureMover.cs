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
        [SerializeField, Tooltip("How quickly the animal turns when fleeing (higher = faster)")]
        private float m_FleeTurnSpeed = 180f;
        [SerializeField, Tooltip("Minimum time between state changes (in seconds)")]
        private float m_StateChangeCooldown = 1f;
        [SerializeField, Tooltip("How far to check for obstacles when fleeing")]
        private float m_ObstacleCheckDistance = 2f;
        [SerializeField, Tooltip("Layer mask for obstacle detection")]
        private LayerMask m_ObstacleLayerMask;
        [SerializeField, Tooltip("Number of raycasts to check for obstacles")]
        private int m_RaycastCount = 5;
        [SerializeField, Tooltip("Spread angle for raycasts in degrees")]
        private float m_RaycastSpread = 30f;
        [SerializeField, Tooltip("Height of raycasts from ground")]
        private float m_RaycastHeight = 0.5f;
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
            m_FleeTurnSpeed = Mathf.Max(m_FleeTurnSpeed, 0f);
            m_StateChangeCooldown = Mathf.Max(m_StateChangeCooldown, 0.1f);
            m_ObstacleCheckDistance = Mathf.Max(m_ObstacleCheckDistance, 0.5f);

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
        }

        private void Update()
        {
            CheckPlayerProximity();
            CheckGroundStatus();
            
            if (m_IsFleeing)
            {
                HandleFleeing();
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
                m_IsMoving = true;
                m_Movement.Move(Time.deltaTime, m_Axis, m_WanderTarget, false, true, out var animAxis, out var isAir);
                m_Animation.Animate(animAxis, 0f, Time.deltaTime);
            }
        }

        private void CheckPlayerProximity()
        {
            if (m_PlayerTransform == null) return;

            // Check if enough time has passed since last state change
            if (Time.time - m_LastStateChangeTime < m_StateChangeCooldown)
            {
                return;
            }

            float distanceToPlayer = Vector3.Distance(m_Transform.position, m_PlayerTransform.position);
            
            // Check if player has moved significantly
            float playerMovement = Vector3.Distance(m_LastPlayerPosition, m_PlayerTransform.position);
            if (playerMovement < m_PlayerMovementThreshold)
            {
                return;
            }
            m_LastPlayerPosition = m_PlayerTransform.position;

            CharController_Motor playerController = m_PlayerTransform.GetComponent<CharController_Motor>();

            if (playerController != null)
            {
                bool shouldFlee = false;

                // Check if player is crouching
                if (playerController.IsCrouching())
                {
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
                if (shouldFlee != m_IsFleeing)
                {
                    m_IsFleeing = shouldFlee;
                    m_LastStateChangeTime = Time.time;
                }

                // Always stop fleeing if player is far away
                if (m_IsFleeing && distanceToPlayer > m_DetectionRadius * 2f)
                {
                    m_IsFleeing = false;
                    m_LastStateChangeTime = Time.time;
                    SetNewWanderTarget();
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

            // Calculate base flee direction (away from player)
            Vector3 toPlayer = m_PlayerTransform.position - m_Transform.position;
            toPlayer.y = 0;
            float distanceToPlayer = toPlayer.magnitude;

            // If too close to player, force a stronger flee direction
            Vector3 fleeDirection;
            if (distanceToPlayer < m_MinPlayerDistance)
            {
                // Add some randomness to prevent direct movement
                float randomAngle = UnityEngine.Random.Range(-30f, 30f);
                fleeDirection = Quaternion.Euler(0, randomAngle, 0) * -toPlayer.normalized;
            }
            else
            {
                fleeDirection = m_Transform.position - m_PlayerTransform.position;
                fleeDirection.y = 0;
                fleeDirection.Normalize();
            }

            // Smooth the flee direction with damping
            float smoothTime = 0.3f;
            m_SmoothedFleeDirection = Vector3.SmoothDamp(
                m_SmoothedFleeDirection,
                fleeDirection,
                ref m_FleeVelocity,
                smoothTime
            );
            m_SmoothedFleeDirection.Normalize();

            // Check for obstacles in the smoothed direction
            Vector3 finalFleeDirection = GetSafeFleeDirection(m_SmoothedFleeDirection);

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

        private Vector3 GetSafeFleeDirection(Vector3 preferredDirection)
        {
            // Create a fan of raycasts
            float angleStep = m_RaycastSpread / (m_RaycastCount - 1);
            float startAngle = -m_RaycastSpread / 2f;

            // Store the best direction found
            Vector3 bestDirection = preferredDirection;
            float bestScore = float.MinValue;

            // Check each raycast direction
            for (int i = 0; i < m_RaycastCount; i++)
            {
                float currentAngle = startAngle + (angleStep * i);
                Vector3 direction = Quaternion.Euler(0, currentAngle, 0) * preferredDirection;
                
                // Calculate score for this direction
                float score = EvaluateDirection(direction);
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDirection = direction;
                }
            }

            // If no good direction found, try to find any clear path
            if (bestScore <= 0)
            {
                // Try more directions in a wider spread
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 45f;
                    Vector3 direction = Quaternion.Euler(0, angle, 0) * preferredDirection;
                    
                    if (IsDirectionClear(direction))
                    {
                        return direction;
                    }
                }
                
                // If still no clear path, try moving perpendicular to obstacles
                if (Physics.Raycast(m_Transform.position, preferredDirection, out RaycastHit hit, m_ObstacleCheckDistance, m_ObstacleLayerMask))
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

        private float EvaluateDirection(Vector3 direction)
        {
            float score = 1f;
            Vector3 rayStart = m_Transform.position + Vector3.up * m_RaycastHeight;

            // Check for obstacles
            if (Physics.Raycast(rayStart, direction, out RaycastHit hit, m_ObstacleCheckDistance, m_ObstacleLayerMask))
            {
                // Check slope angle
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle > m_MaxSlopeAngle)
                {
                    score -= 1f; // Completely avoid steep slopes
                }
                else
                {
                    // Reduce score based on distance to obstacle and slope
                    score -= (1f - (hit.distance / m_ObstacleCheckDistance)) * (1f + slopeAngle / m_MaxSlopeAngle);
                }
            }

            // Check if there's ground to walk on
            Vector3 groundCheckPos = rayStart + direction * m_ObstacleCheckDistance;
            if (!Physics.Raycast(groundCheckPos + Vector3.up * m_GroundCheckHeight, Vector3.down, m_GroundCheckDistance, m_GroundLayerMask))
            {
                score -= 0.5f; // Penalize directions without ground
            }

            // Check for obstacles at different heights
            for (float height = 0.2f; height <= 1f; height += 0.2f)
            {
                Vector3 heightCheckPos = rayStart + Vector3.up * height;
                if (Physics.Raycast(heightCheckPos, direction, m_ObstacleCheckDistance, m_ObstacleLayerMask))
                {
                    score -= 0.2f; // Penalize directions with obstacles at different heights
                }
            }

            return score;
        }

        private bool IsDirectionClear(Vector3 direction)
        {
            Vector3 rayStart = m_Transform.position + Vector3.up * m_RaycastHeight;
            
            // Check for obstacles
            if (Physics.Raycast(rayStart, direction, m_ObstacleCheckDistance, m_ObstacleLayerMask))
            {
                return false;
            }

            // Check for ground
            Vector3 groundCheckPos = rayStart + direction * m_ObstacleCheckDistance;
            if (!Physics.Raycast(groundCheckPos + Vector3.up * 0.1f, Vector3.down, 0.2f, m_ObstacleLayerMask))
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
                m_Animator.SetFloat(m_VerticalID, m_FlowAxis.magnitude);
                m_Animator.SetFloat(m_StateID, Mathf.Clamp01(m_FlowState));

                m_FlowAxis = Vector2.ClampMagnitude(m_FlowAxis + k_InputFlow * deltaTime * (axis - m_FlowAxis).normalized, 1f);
                m_FlowState = Mathf.Clamp01(m_FlowState + k_InputFlow * deltaTime * Mathf.Sign(state - m_FlowState));
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