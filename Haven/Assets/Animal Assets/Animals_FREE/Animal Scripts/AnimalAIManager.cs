using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class AnimalAIManager : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float closeDetectionRadius = 2f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float idleSpeed = 1f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float fleeDistance = 15f;
    [SerializeField] private float minFleeDistance = 5f;
    [SerializeField] private float rotationSmoothTime = 0.1f;
    [SerializeField] private float maxFleeDistance = 100f; // Maximum distance to check for valid flee positions

    [Header("Animation Settings")]
    [SerializeField] private float animationBlendSpeed = 5f;
    [SerializeField] private float animationSpeedMultiplier = 1f;

    [Header("Health Settings")]
    [SerializeField] private int maxHP = 100;
    private int currentHP;

    [Header("References")]
    [SerializeField] private Transform playerTransform;
    private NavMeshAgent navAgent;
    private Animator animator;
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;

    // Animation parameters
    private static readonly int State = Animator.StringToHash("State");
    private static readonly int Vert = Animator.StringToHash("Vert");
    private static readonly int Speed = Animator.StringToHash("Speed");

    // States
    private enum AnimalState { Idle, Fleeing, Dead }
    private AnimalState currentState = AnimalState.Idle;

    private Vector3 lastFleePosition;
    private float fleePositionUpdateInterval = 1f;
    private float lastFleePositionUpdate;
    private Vector3 currentVelocity;
    private Vector3 rotationVelocity;
    private Quaternion targetRotation;
    private float currentAnimationSpeed;
    private Vector3 lastPosition;
    private float currentSpeed;
    private bool isFleeing = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get components
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        // Configure NavMeshAgent
        if (navAgent != null)
        {
            navAgent.acceleration = 8f;
            navAgent.angularSpeed = 0f;
            navAgent.stoppingDistance = 0.5f;
            navAgent.radius = 0.5f;
            navAgent.height = 1f;
            navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            navAgent.avoidancePriority = 50;
            navAgent.updateRotation = false;
        }

        // Initialize
        currentHP = maxHP;
        navAgent.speed = idleSpeed;
        lastPosition = transform.position;

        // Find player if not assigned
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        // Configure animator
        if (animator != null)
        {
            animator.SetFloat(Speed, 0f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (currentState == AnimalState.Dead) return;

        // Calculate actual movement speed
        currentSpeed = Vector3.Distance(transform.position, lastPosition) / Time.deltaTime;
        lastPosition = transform.position;

        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            CharController_Motor playerController = playerTransform.GetComponent<CharController_Motor>();

            if (playerController != null)
            {
                // Check if player is within detection radius
                if (playerController.IsCrouching())
                {
                    if (distanceToPlayer <= closeDetectionRadius)
                    {
                        StartFleeing();
                    }
                    else if (isFleeing && distanceToPlayer > closeDetectionRadius * 2f)
                    {
                        SetIdle();
                    }
                }
                else if (playerController.IsWalking() || playerController.IsSprinting())
                {
                    if (distanceToPlayer <= detectionRadius)
                    {
                        StartFleeing();
                    }
                    else if (isFleeing && distanceToPlayer > detectionRadius * 2f)
                    {
                        SetIdle();
                    }
                }
                else if (isFleeing && distanceToPlayer > detectionRadius * 2f)
                {
                    SetIdle();
                }
            }
        }

        // Update flee position periodically
        if (currentState == AnimalState.Fleeing && Time.time - lastFleePositionUpdate > fleePositionUpdateInterval)
        {
            UpdateFleePosition();
            lastFleePositionUpdate = Time.time;
        }

        // Handle movement and rotation
        if (navAgent.velocity.magnitude > 0.1f)
        {
            // Calculate target rotation based on movement direction
            targetRotation = Quaternion.LookRotation(navAgent.velocity.normalized);
            
            // Smoothly rotate towards the movement direction
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );

            // Update animation speed based on actual movement
            float targetSpeed = currentState == AnimalState.Fleeing ? 1f : 0.5f;
            currentAnimationSpeed = Mathf.Lerp(currentAnimationSpeed, targetSpeed, Time.deltaTime * animationBlendSpeed);

            // Update animator parameters
            animator.SetFloat(State, currentAnimationSpeed);
            animator.SetFloat(Vert, currentAnimationSpeed);
            animator.SetFloat(Speed, currentSpeed * animationSpeedMultiplier);
        }
        else
        {
            // Smoothly transition to idle
            currentAnimationSpeed = Mathf.Lerp(currentAnimationSpeed, 0f, Time.deltaTime * animationBlendSpeed);
            animator.SetFloat(State, currentAnimationSpeed);
            animator.SetFloat(Vert, currentAnimationSpeed);
            animator.SetFloat(Speed, 0f);
        }
    }

    private void SetIdle()
    {
        if (currentState != AnimalState.Idle)
        {
            currentState = AnimalState.Idle;
            isFleeing = false;
            navAgent.speed = idleSpeed;
            navAgent.isStopped = true;
        }
    }

    private void StartFleeing()
    {
        if (currentState != AnimalState.Fleeing)
        {
            currentState = AnimalState.Fleeing;
            isFleeing = true;
            navAgent.speed = runSpeed;
            navAgent.isStopped = false;
            UpdateFleePosition();
        }
    }

    private void UpdateFleePosition()
    {
        if (playerTransform == null) return;

        // Calculate flee direction
        Vector3 fleeDirection = transform.position - playerTransform.position;
        fleeDirection.y = 0;
        fleeDirection.Normalize();

        // Add some randomness to the flee direction
        float randomAngle = Random.Range(-30f, 30f);
        fleeDirection = Quaternion.Euler(0, randomAngle, 0) * fleeDirection;

        // Try to find a valid position at increasing distances
        float currentDistance = fleeDistance;
        bool foundValidPosition = false;
        Vector3 fleePosition = transform.position;

        while (!foundValidPosition && currentDistance <= maxFleeDistance)
        {
            fleePosition = transform.position + fleeDirection * currentDistance;
            
            // Sample position on NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(fleePosition, out hit, currentDistance, NavMesh.AllAreas))
            {
                // Check if the new position is far enough from the last one
                if (Vector3.Distance(hit.position, lastFleePosition) > minFleeDistance)
                {
                    navAgent.SetDestination(hit.position);
                    lastFleePosition = hit.position;
                    foundValidPosition = true;
                }
            }
            
            currentDistance += fleeDistance; // Increase search distance
        }

        // If no valid position found, try a random direction
        if (!foundValidPosition)
        {
            Vector3 randomDirection = Random.insideUnitSphere;
            randomDirection.y = 0;
            randomDirection.Normalize();
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position + randomDirection * fleeDistance, out hit, fleeDistance, NavMesh.AllAreas))
            {
                navAgent.SetDestination(hit.position);
                lastFleePosition = hit.position;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (currentState == AnimalState.Dead) return;

        currentHP -= damage;
        
        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        currentState = AnimalState.Dead;
        
        // Disable components
        navAgent.isStopped = true;
        navAgent.enabled = false;
        rb.isKinematic = true;
        capsuleCollider.enabled = false;

        // Start death sequence
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // Wait a short moment before destroying
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }

    // Optional: Visualize detection ranges in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, closeDetectionRadius);
    }
}
