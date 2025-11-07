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

	private bool isOpen = false;
	private bool isInteractable = true;
	private bool isAnimating = false;
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

	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(transform.position, interactRange);
	}
}


