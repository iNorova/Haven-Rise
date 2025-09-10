using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class TutorialPopupTrigger : MonoBehaviour
{
	[Header("Detection")]
	[SerializeField] private string playerTag = "Player";

	[Header("Popup Reference")]
	[SerializeField] private GameObject popupPanel; // UI Panel to show/hide
	[SerializeField] private bool startHidden = true; // Hide on Start

	[Header("Behavior")]
	[SerializeField] private bool showOnlyOnce = false; // If true, shows only the first time
	[SerializeField] private bool hideOnExit = true; // Hide when the player exits the trigger
	[SerializeField] private bool autoHide = false; // If true, auto hide after delay
	[SerializeField] private float autoHideDelay = 2f; // Seconds before auto hide

	[Header("Animation")]
	[SerializeField, Min(0f)] private float fadeDuration = 0.25f; // Fade time seconds
	[SerializeField] private bool disablePanelOnHidden = true; // Set inactive after fade out

	[Header("Sound (Optional)")]
	[SerializeField] private AudioSource audioSource; // Optional dedicated source
	[SerializeField] private AudioClip showSfx;
	[SerializeField] private AudioClip hideSfx;

	private CanvasGroup canvasGroup;
	private Coroutine fadeRoutine;
	private Coroutine autoHideRoutine;
	private bool hasShownOnce = false;
	private Collider triggerCollider;

	private void Reset()
	{
		// Ensure collider is trigger
		var col = GetComponent<Collider>();
		if (col != null) col.isTrigger = true;
	}

	private void Awake()
	{
		triggerCollider = GetComponent<Collider>();
		if (triggerCollider != null && !triggerCollider.isTrigger)
		{
			triggerCollider.isTrigger = true;
		}

		if (popupPanel != null)
		{
			canvasGroup = popupPanel.GetComponent<CanvasGroup>();
			if (canvasGroup == null)
			{
				canvasGroup = popupPanel.AddComponent<CanvasGroup>();
			}
		}
	}

	private void Start()
	{
		// Initialize visibility
		if (popupPanel != null)
		{
			if (startHidden)
			{
				SetVisibleImmediate(false);
			}
			else
			{
				SetVisibleImmediate(true);
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!other.CompareTag(playerTag)) return;
		if (showOnlyOnce && hasShownOnce) return;

		ShowPopup();
		if (showOnlyOnce) hasShownOnce = true;

		if (autoHide)
		{
			StartAutoHide();
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (!other.CompareTag(playerTag)) return;
		if (hideOnExit)
		{
			HidePopup();
			CancelAutoHide();
		}
	}

	public void ShowPopup()
	{
		if (popupPanel == null) return;
		CancelAutoHide();

		if (disablePanelOnHidden && !popupPanel.activeSelf)
		{
			popupPanel.SetActive(true);
		}

		PlaySfx(showSfx);
		StartFade(true);
	}

	public void HidePopup()
	{
		if (popupPanel == null) return;
		CancelAutoHide();
		PlaySfx(hideSfx);
		StartFade(false);
	}

	private void StartAutoHide()
	{
		CancelAutoHide();
		autoHideRoutine = StartCoroutine(AutoHideAfterDelay());
	}

	private void CancelAutoHide()
	{
		if (autoHideRoutine != null)
		{
			StopCoroutine(autoHideRoutine);
			autoHideRoutine = null;
		}
	}

	private IEnumerator AutoHideAfterDelay()
	{
		yield return new WaitForSeconds(autoHideDelay);
		HidePopup();
	}

	private void StartFade(bool show)
	{
		if (fadeRoutine != null)
		{
			StopCoroutine(fadeRoutine);
		}
		fadeRoutine = StartCoroutine(FadeCoroutine(show));
	}

	private IEnumerator FadeCoroutine(bool show)
	{
		if (canvasGroup == null)
		{
			// Fallback: toggle active if no CanvasGroup is available
			popupPanel.SetActive(show);
			yield break;
		}

		float start = canvasGroup.alpha;
		float end = show ? 1f : 0f;
		float time = 0f;

		// Make sure it is active before fade in
		if (show && disablePanelOnHidden && !popupPanel.activeSelf)
		{
			popupPanel.SetActive(true);
		}

		canvasGroup.blocksRaycasts = show;
		canvasGroup.interactable = show;

		if (fadeDuration <= 0f)
		{
			canvasGroup.alpha = end;
		}
		else
		{
			while (time < fadeDuration)
			{
				time += Time.unscaledDeltaTime; // Unscaled so it works when paused
				canvasGroup.alpha = Mathf.Lerp(start, end, time / fadeDuration);
				yield return null;
			}
			canvasGroup.alpha = end;
		}

		if (!show && disablePanelOnHidden)
		{
			popupPanel.SetActive(false);
		}

		fadeRoutine = null;
	}

	private void SetVisibleImmediate(bool visible)
	{
		if (popupPanel == null) return;
		if (canvasGroup == null)
		{
			popupPanel.SetActive(visible);
			return;
		}
		if (disablePanelOnHidden) popupPanel.SetActive(visible);
		canvasGroup.alpha = visible ? 1f : 0f;
		canvasGroup.blocksRaycasts = visible;
		canvasGroup.interactable = visible;
	}

	private void PlaySfx(AudioClip clip)
	{
		if (clip == null) return;
		if (audioSource != null)
		{
			audioSource.PlayOneShot(clip);
		}
		else
		{
			AudioSource.PlayClipAtPoint(clip, transform.position);
		}
	}
}
