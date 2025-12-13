using UnityEngine;

public class ChestInteraction : MonoBehaviour
{
	[Header("Interaction")]
	public KeyCode interactKey = KeyCode.E;
	public float interactRange = 2.0f; // used only if no trigger is available
	public bool openOnceThenDisable = true; // when true, chest opens once and is no longer interactable

	[Header("Animation (Animator path)")]
	public Animator animator;           // Optional. If assigned, we will use trigger parameters below
	public string openTrigger = "Open"; // Animator trigger to open
	public string closeTrigger = "Close"; // Animator trigger to close

	[Header("Animation (Fallback hinge)")]
	public Transform lid;              // Optional fallback. Assign the lid pivot transform
	public float openAngle = 90f;      // Degrees to rotate around local X
	public float openDuration = 0.5f;  // Seconds

	[Header("Audio")]
	public AudioSource sfxSource;      // Optional, created automatically if null
	public AudioClip openSfx;
	public AudioClip closeSfx;
	[Range(0f,1f)] public float sfxVolume = 0.8f;

	[Header("Skill Check (Optional)")]
	public bool useSkillCheck = false;
	public ChestSkillCheck skillCheck;

	[Header("Item Spawning")]
	[Tooltip("List of item prefabs that can spawn from this chest. Leave empty to disable item spawning.")]
	public GameObject[] itemPrefabs = new GameObject[0];
	[Tooltip("Minimum number of items to spawn when chest opens")]
	public int minItems = 2;
	[Tooltip("Maximum number of items to spawn when chest opens")]
	public int maxItems = 5;
	[Tooltip("Distance in front of chest to spawn items")]
	public float forwardDistance = 1.5f;
	[Tooltip("How far items scatter left/right from center (in units)")]
	public float scatterWidth = 1.0f;
	[Tooltip("Force applied to dropped items for physics")]
	public float dropForce = 3f;
	[Tooltip("Spawn items only when skill check succeeds (if useSkillCheck is enabled)")]
	public bool spawnOnlyOnSkillCheckSuccess = true;
	[Tooltip("Use direction toward player instead of chest forward direction")]
	public bool spawnTowardPlayer = true;

	private bool isOpen = false;
	private bool isInteractable = true;
	private bool isAnimating = false;
	private bool itemsSpawned = false; // Track if items have already been spawned
	private Transform player;
	private Quaternion lidClosedRot;
	private Quaternion lidOpenRot;

	void Start()
	{
		// Auto-find player
		GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
		player = playerObj != null ? playerObj.transform : null;

		// Auto-wire skill check if enabled and not assigned
		if (useSkillCheck && skillCheck == null)
		{
			skillCheck = GetComponent<ChestSkillCheck>();
			if (skillCheck == null)
			{
				Debug.LogWarning("ChestInteraction: useSkillCheck is enabled but no ChestSkillCheck component found on this object.");
			}
		}

		// Ensure AudioSource
		if (sfxSource == null)
		{
			GameObject audioObj = new GameObject("ChestAudioSource");
			audioObj.transform.SetParent(transform);
			audioObj.transform.localPosition = Vector3.zero;
			sfxSource = audioObj.AddComponent<AudioSource>();
			sfxSource.playOnAwake = false;
			sfxSource.spatialBlend = 1f;
			sfxSource.minDistance = 3f;
			sfxSource.maxDistance = 15f;
		}

		// Fallback hinge setup
		if (lid != null)
		{
			lidClosedRot = lid.localRotation;
			lidOpenRot = Quaternion.Euler(openAngle, 0f, 0f) * lidClosedRot;
		}

		// Ensure we have a trigger collider for proximity if possible
		bool hasTrigger = false;
		var cols = GetComponents<Collider>();
		foreach (var c in cols) { if (c.isTrigger) { hasTrigger = true; break; } }
		if (!hasTrigger)
		{
			SphereCollider trig = gameObject.AddComponent<SphereCollider>();
			trig.isTrigger = true;
			trig.radius = 1.25f;
		}
	}

	void Update()
	{
		if (isAnimating || !isInteractable) return;

		bool canInteract = IsPlayerInRange();
		if (canInteract && Input.GetKeyDown(interactKey))
		{
			Debug.Log("ChestInteraction: Interact key pressed.");
			if (!isOpen && useSkillCheck && skillCheck != null)
			{
				Debug.Log("ChestInteraction: Starting skill check.");
				skillCheck.Begin(this);
			}
			else
			{
				Debug.Log("ChestInteraction: Toggling chest (no skill check).");
				Toggle();
			}
		}
	}

	private bool IsPlayerInRange()
	{
		// Prefer trigger events, but also support distance check as fallback
		if (player == null) return true; // allow interaction if player ref is missing
		float dist = Vector3.Distance(player.position, transform.position);
		bool within = dist <= interactRange;
		return within;
	}

	private void Toggle()
	{
		if (isOpen) Close(); else Open();
	}

	public void Open()
	{
		if (isAnimating || !isInteractable) return;
		isOpen = true;
		PlaySfx(openSfx);
		if (animator != null)
		{
			isAnimating = true;
			if (!string.IsNullOrEmpty(openTrigger)) animator.SetTrigger(openTrigger);
			Invoke(nameof(ClearAnimating), openDuration > 0f ? openDuration : 0.5f);
		}
		else if (lid != null)
		{
			StartCoroutine(RotateLid(lidClosedRot, lidOpenRot, openDuration));
		}

		// Spawn items when chest opens (if not restricted to skill check success)
		if (!spawnOnlyOnSkillCheckSuccess && !itemsSpawned)
		{
			SpawnRandomItems();
		}

		// If configured one-shot, disable interaction after opening
		if (openOnceThenDisable)
		{
			float delay = Mathf.Max(openDuration, 0.05f);
			Invoke(nameof(DisableInteraction), delay);
		}
	}

	public void Close()
	{
		if (isAnimating) return;
		isOpen = false;
		PlaySfx(closeSfx);
		if (animator != null)
		{
			isAnimating = true;
			if (!string.IsNullOrEmpty(closeTrigger)) animator.SetTrigger(closeTrigger);
			Invoke(nameof(ClearAnimating), openDuration > 0f ? openDuration : 0.5f);
		}
		else if (lid != null)
		{
			StartCoroutine(RotateLid(lidOpenRot, lidClosedRot, openDuration));
		}
	}

	private System.Collections.IEnumerator RotateLid(Quaternion from, Quaternion to, float duration)
	{
		isAnimating = true;
		float t = 0f;
		while (t < duration)
		{
			t += Time.deltaTime;
			float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
			if (lid != null) lid.localRotation = Quaternion.Slerp(from, to, k);
			yield return null;
		}
		if (lid != null) lid.localRotation = to;
		isAnimating = false;
	}

	private void PlaySfx(AudioClip clip)
	{
		if (sfxSource != null && clip != null)
		{
			sfxSource.PlayOneShot(clip, sfxVolume);
		}
	}

	private void ClearAnimating()
	{
		isAnimating = false;
	}

	// Callback from ChestSkillCheck
	public void OnSkillCheckComplete(bool success)
	{
		if (!success)
		{
			return; // exit without opening; player can try again
		}
		// Open once successful
		Open();
		
		// Spawn items when skill check succeeds
		if (spawnOnlyOnSkillCheckSuccess && !itemsSpawned)
		{
			SpawnRandomItems();
		}
	}

	private void DisableInteraction()
	{
		isInteractable = false;
		// Optionally disable trigger colliders to avoid prompts
		var cols = GetComponents<Collider>();
		foreach (var c in cols)
		{
			if (c != null && c.isTrigger) c.enabled = false;
		}
		// Optionally disable this component if you want zero Update cost
		// enabled = false; // uncomment if desired
	}

	/// <summary>
	/// Spawns random items from the itemPrefabs list when the chest opens.
	/// Items are scattered around the chest with physics applied.
	/// </summary>
	private void SpawnRandomItems()
	{
		// Prevent spawning multiple times
		if (itemsSpawned)
		{
			return;
		}

		// Check if we have any valid item prefabs
		if (itemPrefabs == null || itemPrefabs.Length == 0)
		{
			Debug.Log("ChestInteraction: No item prefabs assigned. Skipping item spawn.");
			return;
		}

		// Filter out null prefabs
		System.Collections.Generic.List<GameObject> validPrefabs = new System.Collections.Generic.List<GameObject>();
		foreach (GameObject prefab in itemPrefabs)
		{
			if (prefab != null)
			{
				validPrefabs.Add(prefab);
			}
		}

		if (validPrefabs.Count == 0)
		{
			Debug.LogWarning("ChestInteraction: All item prefabs are null! Cannot spawn items.");
			return;
		}

		// Calculate how many items to spawn
		int itemCount = Random.Range(minItems, maxItems + 1);
		itemCount = Mathf.Clamp(itemCount, 0, validPrefabs.Count * 10); // Safety limit

		Debug.Log($"ChestInteraction: Spawning {itemCount} random items from {validPrefabs.Count} available prefab(s).");

		// Determine forward direction (toward player or chest forward)
		Vector3 forwardDirection;
		if (spawnTowardPlayer && player != null)
		{
			Vector3 toPlayer = (player.position - transform.position);
			toPlayer.y = 0; // Keep horizontal
			if (toPlayer.sqrMagnitude > 0.01f)
			{
				forwardDirection = toPlayer.normalized;
			}
			else
			{
				forwardDirection = transform.forward;
			}
		}
		else
		{
			forwardDirection = transform.forward;
		}

		// Spawn items
		for (int i = 0; i < itemCount; i++)
		{
			// Randomly select a prefab
			GameObject selectedPrefab = validPrefabs[Random.Range(0, validPrefabs.Count)];

			// Calculate spawn position in front of chest
			// Base position: forward from chest
			Vector3 basePos = transform.position + forwardDirection * forwardDistance;
			
			// Add random scatter left/right and forward/back
			Vector3 right = Vector3.Cross(forwardDirection, Vector3.up).normalized;
			float randomX = Random.Range(-scatterWidth, scatterWidth); // Left/right
			float randomZ = Random.Range(-scatterWidth * 0.5f, scatterWidth * 0.5f); // Forward/back (less variation)
			
			Vector3 spawnPos = basePos + right * randomX + forwardDirection * randomZ;
			spawnPos.y = transform.position.y + 0.2f; // Slightly above chest position

			// Spawn the item
			GameObject spawnedItem = Instantiate(selectedPrefab, spawnPos, Random.rotation);
			if (spawnedItem == null)
			{
				Debug.LogError($"ChestInteraction: Failed to instantiate item prefab '{selectedPrefab.name}'!");
				continue;
			}

			Debug.Log($"ChestInteraction: Spawned item {i + 1}/{itemCount}: '{spawnedItem.name}' at position {spawnPos}");

			// Ensure the item has a Rigidbody for physics
			Rigidbody rb = spawnedItem.GetComponent<Rigidbody>();
			if (rb == null)
			{
				rb = spawnedItem.AddComponent<Rigidbody>();
			}
			rb.isKinematic = false;
			rb.useGravity = true;

			// Ensure collider is enabled
			Collider col = spawnedItem.GetComponent<Collider>();
			if (col != null)
			{
				col.enabled = true;
			}

			// Add force to scatter the item (mostly forward and slightly upward)
			Vector3 forceDirection = forwardDirection + Vector3.up * 0.3f + right * Random.Range(-0.2f, 0.2f);
			forceDirection.Normalize();
			rb.AddForce(forceDirection * dropForce, ForceMode.Impulse);
		}

		// Mark items as spawned
		itemsSpawned = true;
		Debug.Log($"ChestInteraction: Successfully spawned {itemCount} items from chest.");
	}

	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(transform.position, interactRange);
		
		// Draw spawn area in front of chest
		if (itemPrefabs != null && itemPrefabs.Length > 0)
		{
			// Determine forward direction
			Vector3 forwardDir;
			if (spawnTowardPlayer && player != null)
			{
				Vector3 toPlayer = (player.position - transform.position);
				toPlayer.y = 0;
				if (toPlayer.sqrMagnitude > 0.01f)
				{
					forwardDir = toPlayer.normalized;
				}
				else
				{
					forwardDir = transform.forward;
				}
			}
			else
			{
				forwardDir = transform.forward;
			}
			
			// Draw forward direction line
			Gizmos.color = Color.yellow;
			Vector3 forwardPos = transform.position + forwardDir * forwardDistance;
			Gizmos.DrawLine(transform.position, forwardPos);
			
			// Draw spawn area rectangle
			Vector3 right = Vector3.Cross(forwardDir, Vector3.up).normalized;
			Vector3 center = transform.position + forwardDir * forwardDistance;
			
			// Draw corners of spawn area
			Vector3 corner1 = center + right * scatterWidth + forwardDir * (scatterWidth * 0.5f);
			Vector3 corner2 = center - right * scatterWidth + forwardDir * (scatterWidth * 0.5f);
			Vector3 corner3 = center + right * scatterWidth - forwardDir * (scatterWidth * 0.5f);
			Vector3 corner4 = center - right * scatterWidth - forwardDir * (scatterWidth * 0.5f);
			
			Gizmos.DrawLine(corner1, corner2);
			Gizmos.DrawLine(corner2, corner4);
			Gizmos.DrawLine(corner4, corner3);
			Gizmos.DrawLine(corner3, corner1);
		}
	}
}


