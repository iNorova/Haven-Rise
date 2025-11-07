using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChestSkillCheck : MonoBehaviour
{
	[Header("Gameplay")]
	public int requiredSuccesses = 3;
	public KeyCode hitKey = KeyCode.Space;
	public KeyCode altHitKey = KeyCode.E;
	public bool allowMouseClick = true;
	public float needleSpeed = 200f; // degrees per second
	public float successWindowCenter = 0f; // degrees (0 = up)
	public float successWindowWidth = 40f; // degrees width
	public float showDuration = 3f; // seconds before auto-fail if no input

	[Header("Difficulty")]
	[Tooltip("Current difficulty level (1 = easiest)")]
	public int difficultyLevel = 1;
	[Tooltip("Success window width at level 1 (degrees)")]
	public float baseWindowWidth = 40f;
	[Tooltip("Change per level for window width (negative to shrink)")]
	public float windowWidthPerLevel = -5f;
	[Tooltip("Needle speed at level 1 (deg/s)")]
	public float baseNeedleSpeed = 200f;
	[Tooltip("Change per level for needle speed (positive to speed up)")]
	public float needleSpeedPerLevel = 40f;
	[Tooltip("Minimum window width (degrees)")]
	public float minWindowWidth = 8f;

	[Header("UI (Worldspace)")]
	public Canvas worldCanvas;        // Optional. If null, created automatically
	public Image dialCircle;          // Background circle
	public Image successArc;          // Visual window (radial filled)
	public RectTransform needle;      // Rotating needle
	public Vector3 canvasOffset = new Vector3(0, 1.2f, 0);
	public float canvasScale = 0.004f;
	public bool faceCamera = true;
	public TextMeshProUGUI progressText; // Optional progress like 1/3
	public TextMeshProUGUI instructionText; // UI hint

	[Header("Audio")]
	public AudioSource sfxSource;     // Optional, created automatically if null
	public AudioClip successSfx;
	public AudioClip failSfx;
	[Range(0f,1f)] public float sfxVolume = 0.8f;
	[Tooltip("Looping background clip while skill check is active")]
	public AudioClip bgmLoopClip;
	[Range(0f,1f)] public float bgmVolume = 0.6f;
	[Tooltip("Playback pitch for the BGM loop")] public float bgmPitch = 1.0f;
	[Tooltip("Start time within the BGM clip (seconds)")] public float bgmStartTime = 0f;
	public AudioSource bgmSource;     // Optional. Created automatically if null when bgmLoopClip set

	private ChestInteraction chest;
	private bool active;
	private float angle;
	private float timer;
	private int successes;
	private static Sprite s_DefaultSprite;
	private AudioSource CreateAudioSource(string name, bool spatial)
	{
		GameObject go = new GameObject(name);
		go.transform.SetParent(transform);
		go.transform.localPosition = Vector3.zero;
		var source = go.AddComponent<AudioSource>();
		source.playOnAwake = false;
		source.loop = false;
		source.spatialBlend = spatial ? 1f : 0f;
		if (spatial)
		{
			source.minDistance = 3f;
			source.maxDistance = 15f;
		}
		return source;
	}

	void Awake()
	{
	if (sfxSource == null)
	{
		sfxSource = CreateAudioSource("ChestSkillCheck_Audio", false);
	}

		if (bgmSource == null && bgmLoopClip != null)
		{
			bgmSource = CreateAudioSource("ChestSkillCheck_BGM", false);
		}
	}

	void Update()
	{
		if (!active) return;

		timer += Time.deltaTime;
		if (timer > showDuration)
		{
			Fail();
			return;
		}

		// Rotate needle
		angle += needleSpeed * Time.deltaTime;
		if (angle >= 360f) angle -= 360f;
		if (needle != null)
		{
			needle.localEulerAngles = new Vector3(0, 0, -angle);
		}


		// Billboard canvas towards camera (face the camera)
		if (faceCamera && worldCanvas != null && Camera.main != null)
		{
			Vector3 toCam = Camera.main.transform.position - worldCanvas.transform.position;
			if (toCam.sqrMagnitude > 0.0001f)
				worldCanvas.transform.rotation = Quaternion.LookRotation(toCam, Vector3.up);
		}

		bool hitPressed = Input.GetKeyDown(hitKey) || Input.GetKeyDown(altHitKey) || (allowMouseClick && Input.GetMouseButtonDown(0));
		if (hitPressed)
		{
			Debug.Log("ChestSkillCheck: Hit input received.");
			float half = successWindowWidth * 0.5f;
			float diff = Mathf.DeltaAngle(angle, successWindowCenter);
			bool ok = Mathf.Abs(diff) <= half;
			if (ok)
			{
				Play(successSfx);
				successes++;
				UpdateProgress();
				if (successes >= requiredSuccesses)
				{
					End(true);
					return;
				}
				// reset round
				timer = 0f;
				// randomize window center each round
				successWindowCenter = Random.Range(0f, 360f);
				UpdateSuccessArc();
			}
			else
			{
				Fail();
			}
		}
	}

	public void Begin(ChestInteraction target)
	{
		if (active) return;
		chest = target;
		successes = 0;
		timer = 0f;
		angle = 0f;
		successWindowCenter = Random.Range(0f, 360f);
		ApplyDifficulty();
		EnsureUI();
		UpdateSuccessArc();
		UpdateProgress();
		SetUIVisible(true);
		active = true;
		Debug.Log("ChestSkillCheck: UI shown and active.");
		PlayBgm();
	}

	private void End(bool success)
	{
		active = false;
		SetUIVisible(false);
		StopBgm();
		if (chest != null)
		{
			chest.OnSkillCheckComplete(success);
		}
	}

	private void Fail()
	{
		Play(failSfx);
		End(false);
	}

	private void Play(AudioClip clip)
	{
		if (sfxSource != null && clip != null)
		{
			sfxSource.PlayOneShot(clip, sfxVolume);
		}
	}

	private void PlayBgm()
	{
		if (bgmLoopClip == null) return;
		if (bgmSource == null)
		{
			bgmSource = CreateAudioSource("ChestSkillCheck_BGM", false);
		}
		bgmSource.clip = bgmLoopClip;
		bgmSource.loop = true;
		bgmSource.volume = bgmVolume;
		bgmSource.pitch = bgmPitch;
		bgmSource.Stop();
		float start = Mathf.Clamp(bgmStartTime, 0f, Mathf.Max(0f, bgmLoopClip.length - 0.01f));
		bgmSource.time = start;
		if (!bgmSource.isPlaying)
		{
			bgmSource.Play();
		}
	}

	private void StopBgm()
	{
		if (bgmSource != null && bgmSource.isPlaying)
		{
			bgmSource.Stop();
		}
	}

	private void EnsureUI()
	{
		// Create default sprite (1x1 white) if needed
		if (s_DefaultSprite == null)
		{
			Texture2D tex = Texture2D.whiteTexture;
			s_DefaultSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
		}

		if (worldCanvas == null)
		{
			GameObject c = new GameObject("ChestSkillCheck_Canvas");
			c.transform.SetParent(transform);
			c.transform.localPosition = canvasOffset;
			c.transform.localRotation = Quaternion.identity;
			worldCanvas = c.AddComponent<Canvas>();
			worldCanvas.renderMode = RenderMode.WorldSpace;
			worldCanvas.overrideSorting = true;
			worldCanvas.sortingOrder = 1000;
			if (Camera.main != null)
				worldCanvas.worldCamera = Camera.main;
			CanvasScaler scaler = c.AddComponent<CanvasScaler>();
			scaler.dynamicPixelsPerUnit = 30f;
			c.AddComponent<GraphicRaycaster>();
			c.transform.localScale = Vector3.one * canvasScale;
			// Force onto Default layer to ensure camera renders it
			c.layer = LayerMask.NameToLayer("Default");
		}

		if (dialCircle == null)
		{
			GameObject d = new GameObject("Dial");
			d.transform.SetParent(worldCanvas.transform);
			var rt = d.AddComponent<RectTransform>();
			rt.sizeDelta = new Vector2(300, 300);
			rt.localScale = Vector3.one;
			rt.localRotation = Quaternion.identity;
			rt.anchoredPosition = Vector2.zero;
			dialCircle = d.AddComponent<Image>();
			dialCircle.sprite = s_DefaultSprite;
			dialCircle.type = Image.Type.Sliced;
			dialCircle.color = new Color(0,0,0,0.6f);
			d.layer = LayerMask.NameToLayer("Default");
		}

		if (successArc == null)
		{
			GameObject a = new GameObject("SuccessArc");
			a.transform.SetParent(worldCanvas.transform);
			var rt = a.AddComponent<RectTransform>();
			rt.sizeDelta = new Vector2(300, 300);
			rt.localScale = Vector3.one;
			rt.localRotation = Quaternion.identity;
			rt.anchoredPosition = Vector2.zero;
			successArc = a.AddComponent<Image>();
			successArc.sprite = s_DefaultSprite;
			successArc.type = Image.Type.Filled;
			successArc.fillMethod = Image.FillMethod.Radial360;
			successArc.fillOrigin = (int)Image.Origin360.Top;
			successArc.fillClockwise = true;
			successArc.fillAmount = 0.1f;
			successArc.color = new Color(0.2f, 1f, 0.2f, 0.85f);
			a.layer = LayerMask.NameToLayer("Default");
		}

		if (needle == null)
		{
			GameObject n = new GameObject("Needle");
			n.transform.SetParent(worldCanvas.transform);
			needle = n.AddComponent<RectTransform>();
			needle.sizeDelta = new Vector2(8, 140);
			var img = n.gameObject.AddComponent<Image>();
			img.sprite = s_DefaultSprite;
			img.color = Color.white;
			needle.pivot = new Vector2(0.5f, 0f);
			needle.localScale = Vector3.one;
			needle.localRotation = Quaternion.identity;
			needle.anchoredPosition = Vector2.zero;
			n.layer = LayerMask.NameToLayer("Default");
		}

		if (progressText == null)
		{
			GameObject t = new GameObject("ProgressText");
			t.transform.SetParent(worldCanvas.transform);
			var rt = t.AddComponent<RectTransform>();
			rt.sizeDelta = new Vector2(260, 60);
			rt.anchoredPosition = new Vector2(0, -180);
			progressText = t.AddComponent<TextMeshProUGUI>();
			progressText.fontSize = 42f;
			progressText.alignment = TextAlignmentOptions.Center;
			progressText.color = Color.white;
			t.layer = LayerMask.NameToLayer("Default");
		}

		if (instructionText == null)
		{
			GameObject it = new GameObject("InstructionText");
			it.transform.SetParent(worldCanvas.transform);
			var rt = it.AddComponent<RectTransform>();
			rt.sizeDelta = new Vector2(360, 60);
			rt.anchoredPosition = new Vector2(0, 200);
			instructionText = it.AddComponent<TextMeshProUGUI>();
			instructionText.fontSize = 36f;
			instructionText.alignment = TextAlignmentOptions.Center;
			instructionText.color = Color.white;
			instructionText.text = "Press SPACE / E when in green";
		}
	}

	private void UpdateSuccessArc()
	{
		if (successArc == null) return;
		// Convert center/width in degrees to radial360 fill range
		float half = Mathf.Clamp(successWindowWidth, 1f, 359f) * 0.5f;
		float startAngle = Mathf.Repeat(successWindowCenter - half + 360f, 360f);
		float endAngle = Mathf.Repeat(successWindowCenter + half + 360f, 360f);
		// Use fillAmount to represent width (as 0..1 of 360)
		successArc.fillAmount = Mathf.Clamp01(successWindowWidth / 360f);
		// Shift arc origin by rotating the rect (simpler than changing origin index)
		successArc.rectTransform.localEulerAngles = new Vector3(0, 0, -startAngle);
	}

	private void ApplyDifficulty()
	{
		// Evaluate level-based parameters
		int lvl = Mathf.Max(1, difficultyLevel);
		float w = baseWindowWidth + (lvl - 1) * windowWidthPerLevel;
		successWindowWidth = Mathf.Max(minWindowWidth, w);
		needleSpeed = baseNeedleSpeed + (lvl - 1) * needleSpeedPerLevel;
	}

	public void SetDifficulty(int level)
	{
		difficultyLevel = level;
		ApplyDifficulty();
		UpdateSuccessArc();
	}

	private void SetUIVisible(bool v)
	{
		if (worldCanvas != null) worldCanvas.gameObject.SetActive(v);
	}

	private void UpdateProgress()
	{
		if (progressText != null)
		{
			progressText.text = successes.ToString() + "/" + requiredSuccesses.ToString();
		}
	}
}


