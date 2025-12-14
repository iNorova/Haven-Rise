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
public class GhoulZombieAI : MonoBehaviour, IDamageable
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

	[Header("Audio")]
	[Tooltip("Roaming audio clips that loop continuously while patrolling, chasing, sprinting, or attacking. Volume adjusts based on distance from player. Multiple clips will be randomly selected for variation. Drag your MP3 audio files here.")]
	public AudioClip[] roamingAudioClips;
	[Tooltip("Audio clip that plays when attacking while walking. Drag your MP3 audio file here.")]
	public AudioClip walkAttackAudioClip;
	[Tooltip("Audio clip that plays when attacking while sprinting. Drag your MP3 audio file here.")]
	public AudioClip sprintAttackAudioClip;
	[Tooltip("Audio clip that plays when the ghoul dies. Drag your MP3 audio file here.")]
	public AudioClip dyingAudioClip;
	[Tooltip("Minimum distance for 3D audio (volume starts decreasing beyond this).")]
	public float audioMinDistance = 1f;
	[Tooltip("Maximum distance for 3D audio (volume reaches zero at this distance).")]
	public float audioMaxDistance = 25f;
	[Tooltip("Time in seconds before switching to a different roaming audio clip (for variation). Set to 0 to play one clip until state changes.")]
	public float roamingAudioSwitchInterval = 10f;
	private AudioSource audioSource; // Internal AudioSource component for playing clips
	private AudioSource roamingAudioSource; // Separate AudioSource for looping roaming audio
	private float roamingAudioSwitchTimer = 0f;
	private int currentRoamingClipIndex = -1;

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

	// Stuck detection
	private Vector3 lastPosition;
	private float stuckTimer;
	private const float STUCK_THRESHOLD = 0.1f; // Distance considered "stuck"
	private const float STUCK_TIME = 3f; // Seconds before considered stuck
	private float navMeshCheckTimer;
	private const float NAVMESH_CHECK_INTERVAL = 2f; // Check NavMesh every 2 seconds

	private void Awake()
	{
		animator = GetComponent<Animator>();
		agent = GetComponent<NavMeshAgent>();
		currentHealth = Mathf.Clamp(maxHealth, 1f, float.MaxValue);

		// Get or create AudioSource component for playing one-shot audio clips
		audioSource = GetComponent<AudioSource>();
		if (audioSource == null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
		}

		// Create separate AudioSource for looping roaming audio with 3D spatial settings
		roamingAudioSource = gameObject.AddComponent<AudioSource>();
		roamingAudioSource.spatialBlend = 1f; // Full 3D audio
		roamingAudioSource.rolloffMode = AudioRolloffMode.Linear;
		roamingAudioSource.minDistance = audioMinDistance;
		roamingAudioSource.maxDistance = audioMaxDistance;
		roamingAudioSource.loop = true; // Loop the roaming audio
		roamingAudioSource.playOnAwake = false;

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
		// Ensure NavMeshAgent is properly placed on NavMesh
		EnsureOnNavMesh();

		// Initialize stuck detection
		lastPosition = transform.position;
		stuckTimer = 0f;
		navMeshCheckTimer = 0f;

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
		
		// Start roaming audio if starting in patrol state
		if (currentState == GhoulState.Patrol)
		{
			StartRoamingAudio();
		}
	}

	/// <summary>
	/// Ensures the ghoul is placed on a valid NavMesh position. If not, tries to find the nearest valid position.
	/// </summary>
	private void EnsureOnNavMesh()
	{
		if (agent == null) return;

		// Check if agent is on NavMesh
		if (!agent.isOnNavMesh)
		{
			// Try progressively larger radii to find valid NavMesh position
			float[] searchRadii = { 5f, 10f, 20f, 50f };
			
			foreach (float radius in searchRadii)
			{
				UnityEngine.AI.NavMeshHit hit;
				if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, radius, UnityEngine.AI.NavMesh.AllAreas))
				{
					transform.position = hit.position;
					agent.Warp(hit.position);
					
					if (radius > 10f)
					{
						Debug.LogWarning($"GhoulZombieAI: Repositioned ghoul {gameObject.name} to valid NavMesh position ({radius}m radius).");
					}
					return; // Successfully repositioned
				}
			}
			
			// If all radii failed, log error but don't destroy the ghoul
			Debug.LogError($"GhoulZombieAI: Could not find valid NavMesh position for ghoul {gameObject.name} within 50m. Ghoul may be stuck! Position: {transform.position}");
		}
		else
		{
			// Even if on NavMesh, occasionally warp to current position to ensure agent is properly initialized
			// This helps with edge cases where the agent thinks it's on NavMesh but isn't actually working
			if (Random.value < 0.1f) // 10% chance per check to avoid doing it every frame
			{
				agent.Warp(transform.position);
			}
		}
	}

	private void Update()
	{
		if (currentState == GhoulState.Dead)
		{
			return;
		}

		// Periodic NavMesh validation
		navMeshCheckTimer += Time.deltaTime;
		if (navMeshCheckTimer >= NAVMESH_CHECK_INTERVAL)
		{
			navMeshCheckTimer = 0f;
			if (agent != null && !agent.isOnNavMesh)
			{
				EnsureOnNavMesh();
			}
		}

		// Stuck detection
		CheckIfStuck();

		// Handle roaming audio clip switching for variation
		if (roamingAudioSwitchInterval > 0f && roamingAudioSource != null && roamingAudioSource.isPlaying && roamingAudioClips != null && roamingAudioClips.Length > 1)
		{
			roamingAudioSwitchTimer += Time.deltaTime;
			if (roamingAudioSwitchTimer >= roamingAudioSwitchInterval)
			{
				roamingAudioSwitchTimer = 0f;
				SwitchRoamingAudioClip();
			}
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
		if (agent == null || !agent.isOnNavMesh)
		{
			return;
		}

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
		if (agent == null || !agent.isOnNavMesh)
		{
			return;
		}

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

		if (agent == null || !agent.isOnNavMesh)
		{
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

			// Play attack audio based on movement speed
			if (audioSource != null)
			{
				bool isRunningSpeed = agent.speed > walkSpeed + 0.1f;
				if (isRunningSpeed && sprintAttackAudioClip != null)
				{
					audioSource.PlayOneShot(sprintAttackAudioClip);
				}
				else if (!isRunningSpeed && walkAttackAudioClip != null)
				{
					audioSource.PlayOneShot(walkAttackAudioClip);
				}
			}

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
		if (agent == null || !agent.isOnNavMesh)
		{
			EnsureOnNavMesh();
			if (agent == null || !agent.isOnNavMesh)
			{
				return; // Can't pick patrol point if not on NavMesh
			}
		}

		// Try multiple random directions to find a valid NavMesh position
		for (int attempts = 0; attempts < 10; attempts++)
		{
			Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
			randomDirection.y = 0; // Keep it on the same Y level
			Vector3 targetPosition = transform.position + randomDirection;

			if (NavMesh.SamplePosition(targetPosition, out var hit, patrolRadius * 1.5f, NavMesh.AllAreas))
			{
				currentPatrolTarget = hit.position;
				agent.SetDestination(currentPatrolTarget);
				return; // Successfully found a valid patrol point
			}
		}

		// If all attempts failed, try to find any valid NavMesh position nearby
		if (NavMesh.SamplePosition(transform.position, out var fallbackHit, patrolRadius * 2f, NavMesh.AllAreas))
		{
			currentPatrolTarget = fallbackHit.position;
			agent.SetDestination(currentPatrolTarget);
		}
		else
		{
			// Last resort: stay in place but try to ensure we're on NavMesh
			currentPatrolTarget = transform.position;
			EnsureOnNavMesh();
		}
	}

	/// <summary>
	/// Checks if the ghoul is stuck and attempts to unstick it.
	/// </summary>
	private void CheckIfStuck()
	{
		if (agent == null || currentState == GhoulState.Attack || currentState == GhoulState.Dead)
		{
			return;
		}

		// Check if ghoul has moved significantly
		float distanceMoved = Vector3.Distance(transform.position, lastPosition);
		
		if (distanceMoved < STUCK_THRESHOLD)
		{
			stuckTimer += Time.deltaTime;
			
			// If stuck for too long and agent has a destination, try to recover
			if (stuckTimer >= STUCK_TIME && agent.hasPath)
			{
				Debug.LogWarning($"GhoulZombieAI: {gameObject.name} appears to be stuck. Attempting recovery...");
				
				// Try to ensure we're on NavMesh
				EnsureOnNavMesh();
				
				// Reset the current destination and pick a new one
				agent.ResetPath();
				
				// If in patrol, pick a new point
				if (currentState == GhoulState.Patrol)
				{
					PickNewPatrolPoint();
				}
				// If chasing, try to set destination again
				else if (currentState == GhoulState.Chase && player != null)
				{
					agent.SetDestination(player.position);
				}
				
				// Reset stuck timer
				stuckTimer = 0f;
			}
		}
		else
		{
			// Ghoul is moving, reset stuck timer
			stuckTimer = 0f;
			lastPosition = transform.position;
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
				StartRoamingAudio();
				break;
			case GhoulState.Chase:
				isAttacking = false;
				damagePendingFromAutoDelay = false;
				agent.isStopped = false;
				StartRoamingAudio();
				break;
			case GhoulState.Attack:
				agent.isStopped = true;
				StartRoamingAudio();
				break;
			case GhoulState.Dead:
				agent.isStopped = true;
				agent.enabled = false;
				StopRoamingAudio();
				break;
		}
	}

	private void StartRoamingAudio()
	{
		if (roamingAudioSource != null && roamingAudioClips != null && roamingAudioClips.Length > 0)
		{
			if (!roamingAudioSource.isPlaying)
			{
				SelectRandomRoamingClip();
				if (currentRoamingClipIndex >= 0 && currentRoamingClipIndex < roamingAudioClips.Length && roamingAudioClips[currentRoamingClipIndex] != null)
				{
					roamingAudioSource.clip = roamingAudioClips[currentRoamingClipIndex];
					roamingAudioSource.Play();
					roamingAudioSwitchTimer = 0f; // Reset timer when starting new audio
				}
			}
		}
	}

	private void SelectRandomRoamingClip()
	{
		if (roamingAudioClips == null || roamingAudioClips.Length == 0)
		{
			currentRoamingClipIndex = -1;
			return;
		}

		// Filter out null clips
		var validClips = new System.Collections.Generic.List<int>();
		for (int i = 0; i < roamingAudioClips.Length; i++)
		{
			if (roamingAudioClips[i] != null)
			{
				validClips.Add(i);
			}
		}

		if (validClips.Count == 0)
		{
			currentRoamingClipIndex = -1;
			return;
		}

		// If only one valid clip, use it
		if (validClips.Count == 1)
		{
			currentRoamingClipIndex = validClips[0];
			return;
		}

		// Select a random clip different from the current one
		int newIndex;
		do
		{
			newIndex = validClips[Random.Range(0, validClips.Count)];
		}
		while (newIndex == currentRoamingClipIndex && validClips.Count > 1);

		currentRoamingClipIndex = newIndex;
	}

	private void SwitchRoamingAudioClip()
	{
		if (roamingAudioSource == null || roamingAudioClips == null || roamingAudioClips.Length == 0)
		{
			return;
		}

		// Select a new random clip
		SelectRandomRoamingClip();
		
		if (currentRoamingClipIndex >= 0 && currentRoamingClipIndex < roamingAudioClips.Length && roamingAudioClips[currentRoamingClipIndex] != null)
		{
			roamingAudioSource.clip = roamingAudioClips[currentRoamingClipIndex];
			roamingAudioSource.Play(); // Restart with new clip
		}
	}

	private void StopRoamingAudio()
	{
		if (roamingAudioSource != null && roamingAudioSource.isPlaying)
		{
			roamingAudioSource.Stop();
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
		if (currentState == GhoulState.Dead)
		{
			return;
		}

		currentState = GhoulState.Dead;
		damagePendingFromAutoDelay = false;
		isAttacking = false;

		// Stop roaming audio when dying
		StopRoamingAudio();

		// Play dying audio sound
		if (audioSource != null && dyingAudioClip != null)
		{
			audioSource.PlayOneShot(dyingAudioClip);
		}

		TriggerAnimator(deathTrigger);
		onDeath?.Invoke();

		if (agent != null)
		{
			agent.isStopped = true;
			agent.enabled = false;
		}

		foreach (var collider in GetComponentsInChildren<Collider>())
		{
			collider.enabled = false;
		}

		// Snap to ground so death pose isn't floating
		if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out var hit, 5f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
		{
			transform.position = hit.point;
			var forwardFlat = new Vector3(transform.forward.x, 0f, transform.forward.z);
			if (forwardFlat.sqrMagnitude < 0.001f)
			{
				forwardFlat = Vector3.forward;
			}
			transform.rotation = Quaternion.LookRotation(forwardFlat.normalized, hit.normal);
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

