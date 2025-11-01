using UnityEngine;
using UnityEngine.UI;

/// TemperatureVisualEffects (UI Overlay Version)
/// Shows a full-screen UI Image that blinks and fades based on temperature thresholds.
/// Attach this to any GameObject (e.g., under a Canvas). Assign a full-screen Image.
public class TemperatureVisualEffects : MonoBehaviour
{
	[Header("Overlay Reference")]
	[SerializeField] private Image overlayImage; // Full-screen image on Canvas (Screen Space - Overlay/Camera)
	[SerializeField] private bool startHidden = true; // Hide on start

	[Header("Integration (Optional)")]
	[SerializeField] private UIManager uiManager; // If assigned, can auto-subscribe to UIManager.onTemperatureChanged
	[SerializeField] private bool autoSubscribeToUIManager = true;

	[Header("Thresholds (0-100)")]
	[SerializeField, Range(0f, 100f)] private float mildThreshold = 50f;    // >= 50 faint orange blink
	[SerializeField, Range(0f, 100f)] private float dangerThreshold = 75f;  // >= 75 faster/stronger orange
	[SerializeField, Range(0f, 100f)] private float criticalThreshold = 100f;// >= 100 intense red blink

	[Header("Blink Speeds (cycles/sec)")]
	[SerializeField, Min(0f)] private float mildBlinkSpeed = 0.7f;
	[SerializeField, Min(0f)] private float dangerBlinkSpeed = 1.2f;
	[SerializeField, Min(0f)] private float criticalBlinkSpeed = 2.0f;

	[Header("Overlay Colors")]
	[SerializeField] private Color mildColor = new Color(1f, 0.55f, 0f, 1f);   // Orange
	[SerializeField] private Color dangerColor = new Color(1f, 0.4f, 0f, 1f);  // Darker orange
	[SerializeField] private Color criticalColor = new Color(1f, 0f, 0f, 1f);  // Red

	[Header("Alpha Intensities (0..1)")]
	[SerializeField, Range(0f, 1f)] private float mildAlpha = 0.18f;
	[SerializeField, Range(0f, 1f)] private float dangerAlpha = 0.28f;
	[SerializeField, Range(0f, 1f)] private float criticalAlpha = 0.42f;

	[Header("Smoothing")]
	[SerializeField, Min(0f)] private float fadeSpeed = 6f; // Higher = faster fade/blend

	// Runtime
	private float currentTemperature;
	private float targetBlinkSpeed;
	private Color targetBaseColor;
	private float targetMaxAlpha;
	private bool shouldBlink;
	private float blinkPhase;
    private bool isPaused;

	private void Awake()
	{
		if (overlayImage == null)
		{
			Debug.LogWarning("TemperatureVisualEffects: overlayImage is not assigned.");
		}
		else
		{
			overlayImage.raycastTarget = false; // Not blocking clicks by default
		}
	}

	private void Start()
	{
		if (uiManager != null && autoSubscribeToUIManager)
		{
			uiManager.onTemperatureChanged.AddListener(SetTemperature);
			
			// If UIManager already has a temperature value (from loaded save), restore it immediately
			// This ensures visual effects are restored even if component initializes before/after load
			if (UIManager.Instance != null)
			{
				float currentTemp = UIManager.Instance.GetCurrentTemperature();
				if (currentTemp > 0f)
				{
					SetTemperature(currentTemp);
				}
			}
		}

		if (startHidden) SetOverlayAlphaImmediate(0f);
		UpdateTargetsFromTemperature(currentTemperature);
	}

	private void Update()
	{
		if (isPaused)
		{
			// Ensure overlay is hidden while paused
			SetOverlayAlphaImmediate(0f);
			return;
		}
		// Update blink phase
		if (shouldBlink && targetBlinkSpeed > 0f)
		{
			blinkPhase += Time.unscaledDeltaTime * targetBlinkSpeed * Mathf.PI * 2f;
		}
		else
		{
			blinkPhase = 0f;
		}

		// Pulse 0..1
		float pulse = shouldBlink ? (0.5f + 0.5f * Mathf.Sin(blinkPhase)) : 0f;
		float desiredAlpha = shouldBlink ? (targetMaxAlpha * pulse) : 0f;

		if (overlayImage != null)
		{
			Color current = overlayImage.color;
			Color target = new Color(targetBaseColor.r, targetBaseColor.g, targetBaseColor.b, desiredAlpha);
			float lerp = 1f - Mathf.Exp(-fadeSpeed * Time.unscaledDeltaTime);
			overlayImage.color = Color.Lerp(current, target, lerp);
		}
	}

	/// Public API: called by your temperature system (UIManager) with a 0..100 value
	public void SetTemperature(float value)
	{
		currentTemperature = Mathf.Clamp(value, 0f, 100f);
		UpdateTargetsFromTemperature(currentTemperature);
	}

	private void UpdateTargetsFromTemperature(float temp)
	{
		if (temp >= criticalThreshold)
		{
			shouldBlink = true;
			targetBlinkSpeed = criticalBlinkSpeed;
			targetBaseColor = criticalColor;
			targetMaxAlpha = criticalAlpha;
		}
		else if (temp >= dangerThreshold)
		{
			shouldBlink = true;
			targetBlinkSpeed = dangerBlinkSpeed;
			targetBaseColor = dangerColor;
			targetMaxAlpha = dangerAlpha;
		}
		else if (temp >= mildThreshold)
		{
			shouldBlink = true;
			targetBlinkSpeed = mildBlinkSpeed;
			targetBaseColor = mildColor;
			targetMaxAlpha = mildAlpha;
		}
		else
		{
			shouldBlink = false;
			targetBlinkSpeed = 0f;
			targetBaseColor = mildColor; // not used when not blinking
			targetMaxAlpha = 0f;
		}
	}

	private void SetOverlayAlphaImmediate(float a)
	{
		if (overlayImage == null) return;
		Color c = overlayImage.color;
		c.a = Mathf.Clamp01(a);
		overlayImage.color = c;
	}

	// Public: Pause/resume effects (used by death panel)
	public void SetPaused(bool paused)
	{
		isPaused = paused;
		if (paused)
		{
			SetOverlayAlphaImmediate(0f);
		}
	}
}
