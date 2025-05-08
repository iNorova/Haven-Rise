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

    // States
    private enum AnimalState { Idle, Fleeing, Dead }
    private AnimalState currentState = AnimalState.Idle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get components
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        // Initialize
        currentHP = maxHP;
        navAgent.speed = idleSpeed;

        // Find player if not assigned
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (currentState == AnimalState.Dead) return;

        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            CharController_Motor playerController = playerTransform.GetComponent<CharController_Motor>();

            if (playerController != null)
            {
                // Check if player is crouching
                if (playerController.IsCrouching())
                {
                    if (distanceToPlayer <= closeDetectionRadius)
                    {
                        StartFleeing();
                    }
                    else
                    {
                        SetIdle();
                    }
                }
                // Check if player is moving (walking or sprinting)
                else if (playerController.IsWalking() || playerController.IsSprinting())
                {
                    if (distanceToPlayer <= detectionRadius)
                    {
                        StartFleeing();
                    }
                    else
                    {
                        SetIdle();
                    }
                }
                else
                {
                    SetIdle();
                }
            }
        }

        // Update animation based on movement
        if (navAgent.velocity.magnitude > 0.1f)
        {
            animator.SetFloat(State, 1f); // Running
            animator.SetFloat(Vert, 1f);
        }
        else
        {
            animator.SetFloat(State, 0f); // Idle
            animator.SetFloat(Vert, 0f);
        }
    }

    private void SetIdle()
    {
        if (currentState != AnimalState.Idle)
        {
            currentState = AnimalState.Idle;
            navAgent.speed = idleSpeed;
            navAgent.isStopped = true;
        }
    }

    private void StartFleeing()
    {
        if (currentState != AnimalState.Fleeing)
        {
            currentState = AnimalState.Fleeing;
            navAgent.speed = runSpeed;
            navAgent.isStopped = false;
        }

        // Calculate flee direction
        Vector3 fleeDirection = transform.position - playerTransform.position;
        Vector3 fleePosition = transform.position + fleeDirection.normalized * 10f;
        
        // Set destination
        NavMeshHit hit;
        if (NavMesh.SamplePosition(fleePosition, out hit, 10f, NavMesh.AllAreas))
        {
            navAgent.SetDestination(hit.position);
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
}
