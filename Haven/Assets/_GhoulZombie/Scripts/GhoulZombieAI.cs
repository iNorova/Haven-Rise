using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Simple damageable contract used by the ghoul zombie to hurt targets (e.g. the player).
/// Implement this on any component that should react to enemy damage.
/// </summary>
public interface IDamageable
{
	void ApplyDamage(float amount);
}

/// <summary>
/// Controls the Ghoul zombie behaviour (patrol -> chase -> attack -> death) and keeps the animator in sync.
/// Drop this on the Ghoul prefab, assign an Animator + NavMeshAgent, and tune the exposed fields for your scene.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class GhoulZombieAI : MonoBehaviour
{
	private enum GhoulState
	{
		Idle,
		Patrol,
		Chase,
		Attack,
		Dead
	}

	[Header("Targeting")]
	[Tooltip("Player transform the ghoul should chase. If empty the script will try to auto-find the FPS controller or an object tagged Player.")]
	public Transform player;
	[Tooltip("Angle in degrees for the zombie's field of view check.")]
	[Range(30f, 360f)] public float fieldOfView = 160f;
	[Tooltip("Layers considered when raycasting line of sight checks.")]
	public LayerMask visibilityMask = Physics.DefaultRaycastLayers;

	[Header("Detection Distances")]
	[Tooltip("Ghoul starts reacting when the player is inside this radius.")]
	public float detectionRadius = 15f;
	[Tooltip("Distance at which the ghoul switches from walk to full sprint while chasing.")]
	public float runThreshold = 7.5f;
	[Tooltip("If the player goes beyond this distance the ghoul returns to patrol/idle.")]
	public float loseInterestRadius = 22f;
	[Tooltip("Range required to initiate the attack animation.")]
	public float attackRange = 2.25f;

	[Header("Movement")]
	public float walkSpeed = 1.4f;
	public float runSpeed = 3.8f;
	[Tooltip("How far the ghoul can wander when patrolling.")]
	public float patrolRadius = 6f;
	[Tooltip("Seconds spent waiting at each patrol point.")]
	public float patrolWaitTime = 2f;
	[Header("Day/Night Behavior")]
	[Tooltip("Sunrise hour (default: 6 AM). Ghoul walks during day time.")]
	public float sunriseHour = 6f;
	[Tooltip("Sunset hour (default: 6 PM). Ghoul runs during night time.")]
	public float sunsetHour = 18f;

	[Header("Combat")]
	public float attackDamage = 15f;
	public float attackCooldown = 1.4f;
	[Tooltip("Optional delay between triggering the attack and dealing damage. Use if you are NOT driving damage from animation events.")]
	public float attackImpactDelay = 0.35f;

	[Header("Health")]
	public float maxHealth = 100f;
	public float despawnDelay = 10f;

	[Header("Animation Parameters")]
	public string speedParam = "Speed";
	public string walkBool = "IsWalking";
	public string runBool = "IsRunning";
	public string attackTrigger = "Attack";
	public string deathTrigger = "Die";

	[Header("Unity Events")]
	public UnityEvent onAttackStarted;
	public UnityEvent onAttackHit;
	public UnityEvent onDeath;

	private Animator animator;
	private NavMeshAgent agent;
	private readonly HashSet<string> animatorParameters = new();

	private GhoulState currentState = GhoulState.Patrol;
	private float patrolTimer;
	private Vector3 currentPatrolTarget;
	private float attackTimer;
	private float currentHealth;
	private bool isAttacking;
	private bool damagePendingFromAutoDelay;
	private float damageDelayTimer;

	private IDamageable cachedDamageable;

	private void Awake()
	{
		animator = GetComponent<Animator>();
		agent = GetComponent<NavMeshAgent>();
		currentHealth = Mathf.Clamp(maxHealth, 1f, float.MaxValue);

		if (animator != null)
		{
			foreach (var parameter in animator.parameters)
			{
				animatorParameters.Add(parameter.name);
			}
		}
	}

	private void Start()
	{
		if (player == null)
		{
			var motor = FindFirstObjectByType<CharController_Motor>();
			if (motor != null)
			{
				player = motor.transform;
			}
			else
			{
				var taggedPlayer = GameObject.FindGameObjectWithTag("Player");
				if (taggedPlayer != null)
				{
					player = taggedPlayer.transform;
				}
			}
		}

		if (player != null)
		{
			cachedDamageable = player.GetComponentInParent<IDamageable>();
		}

		PickNewPatrolPoint();
	}

	private void Update()
	{
		if (currentState == GhoulState.Dead)
		{
			return;
		}

		attackTimer -= Time.deltaTime;
		if (damagePendingFromAutoDelay)
		{
			damageDelayTimer -= Time.deltaTime;
			if (damageDelayTimer <= 0f)
			{
				damagePendingFromAutoDelay = false;
				DealDamageToPlayer();
			}
		}

		var playerVisible = PlayerWithinSight(out var playerDistance);

		switch (currentState)
		{
			case GhoulState.Patrol:
				if (playerVisible && playerDistance <= detectionRadius)
				{
					SetState(GhoulState.Chase);
				}
				else
				{
					RunPatrol();
				}
				break;

			case GhoulState.Chase:
				if (!playerVisible && playerDistance > loseInterestRadius)
				{
					SetState(GhoulState.Patrol);
				}
				else if (playerVisible && playerDistance <= attackRange)
				{
					SetState(GhoulState.Attack);
				}
				else
				{
					RunChase(playerDistance);
				}
				break;

			case GhoulState.Attack:
				if (!playerVisible || playerDistance > attackRange + 0.5f)
				{
					SetState(GhoulState.Chase);
				}
				else
				{
					RunAttack();
				}
				break;
		}

		UpdateAnimator(agent.velocity.magnitude);
	}

	private void RunPatrol()
	{
		agent.stoppingDistance = 0f;
		// Use walk speed during day, run speed during night
		agent.speed = IsNightTime() ? runSpeed : walkSpeed;
		if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
		{
			patrolTimer += Time.deltaTime;
			if (patrolTimer >= patrolWaitTime)
			{
				patrolTimer = 0f;
				PickNewPatrolPoint();
			}
		}
	}

	private void RunChase(float playerDistance)
	{
		agent.stoppingDistance = attackRange - 0.1f;
		// During night: always use run speed when chasing
		// During day: use walk speed unless player is far away
		if (IsNightTime())
		{
			agent.speed = runSpeed;
		}
		else
		{
			agent.speed = playerDistance > runThreshold ? runSpeed : walkSpeed;
		}
		if (player != null)
		{
			agent.SetDestination(player.position);
		}
	}

	private void RunAttack()
	{
		if (player == null)
		{
			SetState(GhoulState.Patrol);
			return;
		}

		agent.ResetPath();
		agent.velocity = Vector3.zero;

		var direction = player.position - transform.position;
		direction.y = 0f;
		if (direction != Vector3.zero)
		{
			var targetRotation = Quaternion.LookRotation(direction);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
		}

		if (attackTimer <= 0f && !isAttacking)
		{
			isAttacking = true;
			attackTimer = attackCooldown;
			TriggerAnimator(attackTrigger);
			onAttackStarted?.Invoke();

			if (attackImpactDelay > 0f)
			{
				damagePendingFromAutoDelay = true;
				damageDelayTimer = attackImpactDelay;
			}
		}
	}

	private bool PlayerWithinSight(out float distance)
	{
		distance = Mathf.Infinity;
		if (player == null)
		{
			return false;
		}

		var dir = player.position - transform.position;
		distance = dir.magnitude;
		if (distance > detectionRadius && currentState != GhoulState.Chase && currentState != GhoulState.Attack)
		{
			return false;
		}

		dir.Normalize();
		if (Vector3.Angle(transform.forward, dir) > fieldOfView * 0.5f)
		{
			// Outside FOV, still considered if very close to avoid blind-spot exploits.
			if (distance > attackRange * 1.5f)
			{
				return false;
			}
		}

		if (Physics.Raycast(transform.position + Vector3.up * 1.6f, dir, out var hit, Mathf.Max(detectionRadius, distance), visibilityMask, QueryTriggerInteraction.Ignore))
		{
			return hit.transform == player || hit.transform.IsChildOf(player);
		}

		return true;
	}

	private void PickNewPatrolPoint()
	{
		Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
		randomDirection += transform.position;

		if (NavMesh.SamplePosition(randomDirection, out var hit, patrolRadius, NavMesh.AllAreas))
		{
			currentPatrolTarget = hit.position;
			agent.SetDestination(currentPatrolTarget);
		}
		else
		{
			currentPatrolTarget = transform.position;
		}
	}

	private void SetState(GhoulState newState)
	{
		if (currentState == newState || currentState == GhoulState.Dead)
		{
			return;
		}

		currentState = newState;

		switch (newState)
		{
			case GhoulState.Patrol:
				isAttacking = false;
				damagePendingFromAutoDelay = false;
				agent.isStopped = false;
				PickNewPatrolPoint();
				break;
			case GhoulState.Chase:
				isAttacking = false;
				damagePendingFromAutoDelay = false;
				agent.isStopped = false;
				break;
			case GhoulState.Attack:
				agent.isStopped = true;
				break;
			case GhoulState.Dead:
				agent.isStopped = true;
				agent.enabled = false;
				break;
		}
	}

	/// <summary>
	/// Animation event hook – call this from the attack animation when the hit should register.
	/// </summary>
	public void AnimationEvent_DealDamage()
	{
		DealDamageToPlayer();
		onAttackHit?.Invoke();
	}

	private void DealDamageToPlayer()
	{
		if (currentState == GhoulState.Dead || player == null)
		{
			return;
		}

		if (Vector3.Distance(transform.position, player.position) > attackRange + 0.5f)
		{
			return;
		}

		if (cachedDamageable == null)
		{
			cachedDamageable = player.GetComponentInParent<IDamageable>();
		}

		cachedDamageable?.ApplyDamage(attackDamage);
		isAttacking = false;
	}

	public void ApplyDamage(float amount)
	{
		if (currentState == GhoulState.Dead)
		{
			return;
		}

		currentHealth -= Mathf.Abs(amount);
		if (currentHealth <= 0f)
		{
			Die();
		}
	}

	private void Die()
	{
		currentState = GhoulState.Dead;
		damagePendingFromAutoDelay = false;
		isAttacking = false;

		TriggerAnimator(deathTrigger);
		onDeath?.Invoke();

		foreach (var collider in GetComponentsInChildren<Collider>())
		{
			collider.enabled = false;
		}

		Destroy(gameObject, despawnDelay);
	}

	private void UpdateAnimator(float speed)
	{
		if (animator == null)
		{
			return;
		}

		if (SupportsAnimatorParam(speedParam))
		{
			animator.SetFloat(speedParam, speed);
		}

		// Determine if ghoul is running based on current speed
		bool isRunningSpeed = agent.speed > walkSpeed + 0.1f;
		
		if (SupportsAnimatorParam(walkBool))
		{
			// Walking: patrol state OR chase state with walk speed
			bool walking = (currentState == GhoulState.Patrol && !isRunningSpeed) || 
			               (currentState == GhoulState.Chase && !isRunningSpeed);
			animator.SetBool(walkBool, walking);
		}

		if (SupportsAnimatorParam(runBool))
		{
			// Running: patrol or chase state with run speed
			bool running = (currentState == GhoulState.Patrol || currentState == GhoulState.Chase) && isRunningSpeed;
			animator.SetBool(runBool, running);
		}
	}

	private void TriggerAnimator(string triggerName)
	{
		if (animator == null || !SupportsAnimatorParam(triggerName) || string.IsNullOrEmpty(triggerName))
		{
			return;
		}

		animator.SetTrigger(triggerName);
	}

	private bool SupportsAnimatorParam(string paramName) => !string.IsNullOrEmpty(paramName) && animatorParameters.Contains(paramName);

	/// <summary>
	/// Checks if it's currently night time based on DayNightCycle.
	/// </summary>
	private bool IsNightTime()
	{
		if (DayNightCycle.Instance == null)
		{
			// If no day/night cycle found, default to day (walk speed)
			return false;
		}
		return DayNightCycle.Instance.IsNightTime(sunriseHour, sunsetHour);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, detectionRadius);

		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, attackRange);

		Gizmos.color = Color.gray;
		Gizmos.DrawWireSphere(transform.position, loseInterestRadius);
	}
}

