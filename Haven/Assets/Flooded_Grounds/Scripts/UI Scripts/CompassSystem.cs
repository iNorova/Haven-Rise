using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Compass system that displays tracked objects at the top of the screen (God of War style).
/// Shows directional indicators for important objects like the end game ship.
/// </summary>
public class CompassSystem : MonoBehaviour
{
    [Header("Compass Settings")]
    [Tooltip("Show compass on screen.")]
    public bool showCompass = true;
    
    [Tooltip("Compass bar image (horizontal bar at top of screen). Will be created automatically if not assigned.")]
    public Image compassBar;
    
    [Tooltip("Width of the compass bar (in pixels).")]
    public float compassWidth = 800f;
    
    [Tooltip("Height of the compass bar (in pixels).")]
    public float compassHeight = 40f;
    
    [Tooltip("Distance from top of screen (in pixels).")]
    public float topOffset = 20f;
    
    [Tooltip("Color of the compass bar background.")]
    public Color compassBarColor = new Color(0f, 0f, 0f, 0.5f);
    
    [Header("Trackable Objects")]
    [Tooltip("List of objects to track on the compass. Add your end game ship and other important objects here.")]
    public List<TrackableObject> trackableObjects = new List<TrackableObject>();
    
    [Header("Indicator Settings")]
    [Tooltip("Prefab for compass indicators (icons that show on compass). Will use default if not assigned.")]
    public GameObject indicatorPrefab;
    
    [Tooltip("Maximum distance to show indicators (objects beyond this won't show on compass).")]
    public float maxDistance = 500f;
    
    [Tooltip("Minimum distance to show indicators (objects closer than this always show).")]
    public float minDistance = 10f;

    [System.Serializable]
    public class TrackableObject
    {
        [Tooltip("The GameObject to track (e.g., end game ship).")]
        public GameObject targetObject;
        
        [Tooltip("Name/label for this tracked object (for organization).")]
        public string label = "";
        
        [Tooltip("Color of the indicator for this object.")]
        public Color indicatorColor = Color.green;
        
        [Tooltip("Icon/sprite for this indicator (optional).")]
        public Sprite indicatorIcon;
        
        [Tooltip("Priority (higher = shows first if multiple objects in same direction).")]
        [Range(0, 10)]
        public int priority = 5;
        
        [Tooltip("Show this object on compass.")]
        public bool isActive = true;
    }

    private Camera playerCamera;
    private GameObject player;
    private List<GameObject> indicatorInstances = new List<GameObject>();
    private Canvas compassCanvas;
    private bool isCompassAcquired = false;
    private float playerCheckTimer = 0f;
    private const float PLAYER_CHECK_INTERVAL = 2f; // Check every 2 seconds instead of every frame

    void Awake()
    {
        // Find player camera
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }

        // Find player
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            CharController_Motor motor = FindObjectOfType<CharController_Motor>();
            if (motor != null)
            {
                player = motor.gameObject;
            }
        }
    }

    void Start()
    {
        // Don't show compass until acquired (will be enabled by CompassPickup)
        // Create UI but keep it hidden
        if (showCompass)
        {
            CreateCompassUI();
            CreateIndicators();
            // Initially hide compass
            SetCompassVisible(false);
        }
    }

    void Update()
    {
        if (!showCompass || compassBar == null)
        {
            return;
        }

        // Periodically refresh player and camera references
        playerCheckTimer += Time.deltaTime;
        if (playerCheckTimer >= PLAYER_CHECK_INTERVAL)
        {
            RefreshPlayerReferences();
            playerCheckTimer = 0f;
        }

        if (playerCamera == null || player == null)
        {
            return;
        }

        UpdateIndicators();
    }

    private void RefreshPlayerReferences()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindObjectOfType<Camera>();
            }
        }

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                CharController_Motor motor = FindObjectOfType<CharController_Motor>();
                if (motor != null)
                {
                    player = motor.gameObject;
                }
            }
        }
    }

    void CreateCompassUI()
    {
        // Find or create Canvas
        GameObject canvasObj = GameObject.Find("CompassCanvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("CompassCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // High sorting order to appear on top
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            compassCanvas = canvas;
        }
        else
        {
            compassCanvas = canvasObj.GetComponent<Canvas>();
        }

        // Create compass bar if not assigned
        if (compassBar == null)
        {
            GameObject barObj = new GameObject("CompassBar");
            barObj.transform.SetParent(compassCanvas.transform, false);
            
            compassBar = barObj.AddComponent<Image>();
            compassBar.color = compassBarColor;
            
            RectTransform rectTransform = barObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.sizeDelta = new Vector2(compassWidth, compassHeight);
            rectTransform.anchoredPosition = new Vector2(0f, -topOffset);
            
            compassBar.raycastTarget = false;
        }
    }

    void CreateIndicators()
    {
        // Clear existing indicators
        foreach (GameObject indicator in indicatorInstances)
        {
            if (indicator != null)
            {
                Destroy(indicator);
            }
        }
        indicatorInstances.Clear();

        // Create indicators for each trackable object
        foreach (TrackableObject trackable in trackableObjects)
        {
            if (trackable != null && trackable.targetObject != null && trackable.isActive)
            {
                GameObject indicator = CreateIndicator(trackable);
                if (indicator != null)
                {
                    indicatorInstances.Add(indicator);
                }
            }
        }
    }

    GameObject CreateIndicator(TrackableObject trackable)
    {
        GameObject indicator;
        
        if (indicatorPrefab != null)
        {
            indicator = Instantiate(indicatorPrefab, compassBar.transform);
        }
        else
        {
            // Create default indicator
            indicator = new GameObject($"Indicator_{trackable.label}");
            indicator.transform.SetParent(compassBar.transform, false);
            
            Image indicatorImage = indicator.AddComponent<Image>();
            indicatorImage.color = trackable.indicatorColor;
            
            // Use custom icon if provided, otherwise use default downward arrow
            if (trackable.indicatorIcon != null)
            {
                indicatorImage.sprite = trackable.indicatorIcon;
            }
            else
            {
                // Create a simple downward arrow sprite
                indicatorImage.sprite = CreateDownwardArrowSprite();
            }
            
            indicatorImage.raycastTarget = false;
        }

        // Set size
        RectTransform rectTransform = indicator.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(20f, 20f);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        
        // Store reference to trackable object
        CompassIndicator indicatorScript = indicator.GetComponent<CompassIndicator>();
        if (indicatorScript == null)
        {
            indicatorScript = indicator.AddComponent<CompassIndicator>();
        }
        indicatorScript.trackableObject = trackable;
        
        return indicator;
    }

    Sprite CreateDownwardArrowSprite()
    {
        // Create a downward arrow texture (green)
        Texture2D texture = new Texture2D(32, 32);
        Color[] colors = new Color[32 * 32];
        
        // Green color for the arrow
        Color arrowColor = Color.green;
        
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float centerX = 16f;
                float centerY = 16f;
                float distFromCenterX = Mathf.Abs(x - centerX);
                
                // Create downward arrow shape
                // Arrow head (triangle pointing down)
                float arrowHeadTop = 8f;
                float arrowHeadBottom = 20f;
                float arrowHeadWidth = 12f;
                
                // Arrow shaft (rectangle)
                float shaftTop = 20f;
                float shaftBottom = 24f;
                float shaftWidth = 4f;
                
                bool isInArrow = false;
                
                // Check if pixel is in arrow head (triangle pointing down)
                if (y >= arrowHeadTop && y <= arrowHeadBottom)
                {
                    float yPos = y - arrowHeadTop;
                    float maxWidth = arrowHeadWidth * (1f - (yPos / (arrowHeadBottom - arrowHeadTop)));
                    if (distFromCenterX <= maxWidth / 2f)
                    {
                        isInArrow = true;
                    }
                }
                // Check if pixel is in arrow shaft
                else if (y >= shaftTop && y <= shaftBottom)
                {
                    if (distFromCenterX <= shaftWidth / 2f)
                    {
                        isInArrow = true;
                    }
                }
                
                if (isInArrow)
                {
                    colors[y * 32 + x] = arrowColor;
                }
                else
                {
                    colors[y * 32 + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        // Pivot at top center (arrow points down)
        return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 1f));
    }

    void UpdateIndicators()
    {
        if (indicatorInstances == null || indicatorInstances.Count == 0 || trackableObjects == null)
        {
            return;
        }

        // Get player forward direction (cached)
        Vector3 playerForward = playerCamera.transform.forward;
        playerForward.y = 0f; // Project to horizontal plane
        float forwardMagnitude = playerForward.magnitude;
        if (forwardMagnitude > 0.001f)
        {
            playerForward /= forwardMagnitude; // Normalize
        }
        else
        {
            return; // Invalid forward direction
        }

        // Cache player position
        Vector3 playerPosition = player.transform.position;

        // Update each indicator
        int maxCount = Mathf.Min(indicatorInstances.Count, trackableObjects.Count);
        for (int i = 0; i < maxCount; i++)
        {
            GameObject indicator = indicatorInstances[i];
            TrackableObject trackable = trackableObjects[i];
            
            if (indicator == null || trackable == null || trackable.targetObject == null || !trackable.isActive)
            {
                if (indicator != null)
                {
                    indicator.SetActive(false);
                }
                continue;
            }

            // Calculate direction to target (use cached player position)
            Vector3 toTarget = trackable.targetObject.transform.position - playerPosition;
            float distance = toTarget.magnitude;
            
            // Check if within range
            if (distance > maxDistance || distance < minDistance)
            {
                indicator.SetActive(false);
                continue;
            }
            
            indicator.SetActive(true);
            
            // Project to horizontal plane
            toTarget.y = 0f;
            toTarget.Normalize();
            
            // Calculate angle from player forward
            float angle = Vector3.SignedAngle(playerForward, toTarget, Vector3.up);
            
            // Convert angle to position on compass (-180 to 180 degrees maps to -compassWidth/2 to compassWidth/2)
            float normalizedAngle = Mathf.Clamp(angle / 180f, -1f, 1f);
            float xPosition = normalizedAngle * (compassWidth * 0.5f - 20f); // Leave margin for indicator size
            
            // Update indicator position
            RectTransform rectTransform = indicator.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(xPosition, 0f);
            
            // Update color if needed
            Image indicatorImage = indicator.GetComponent<Image>();
            if (indicatorImage != null)
            {
                indicatorImage.color = trackable.indicatorColor;
            }
        }
    }

    /// <summary>
    /// Add an object to track on the compass.
    /// </summary>
    public void AddTrackableObject(GameObject target, string label = "", Color color = default, int priority = 5)
    {
        if (color == default)
        {
            color = Color.white;
        }

        TrackableObject newTrackable = new TrackableObject
        {
            targetObject = target,
            label = string.IsNullOrEmpty(label) ? target.name : label,
            indicatorColor = color,
            priority = priority,
            isActive = true
        };

        trackableObjects.Add(newTrackable);
        
        // Recreate indicators
        CreateIndicators();
    }

    /// <summary>
    /// Remove an object from tracking.
    /// </summary>
    public void RemoveTrackableObject(GameObject target)
    {
        trackableObjects.RemoveAll(t => t.targetObject == target);
        CreateIndicators();
    }

    /// <summary>
    /// Enable/disable tracking for a specific object.
    /// </summary>
    public void SetTrackableActive(GameObject target, bool active)
    {
        foreach (TrackableObject trackable in trackableObjects)
        {
            if (trackable.targetObject == target)
            {
                trackable.isActive = active;
                break;
            }
        }
    }

    /// <summary>
    /// Show/hide the compass.
    /// </summary>
    public void SetCompassVisible(bool visible)
    {
        showCompass = visible;
        
        Debug.Log($"[CompassSystem] SetCompassVisible called: {visible}, compassBar: {(compassBar != null ? compassBar.name : "NULL")}");
        
        if (compassBar != null)
        {
            compassBar.gameObject.SetActive(visible);
            Debug.Log($"[CompassSystem] Compass bar {(visible ? "enabled" : "disabled")}.");
        }
        else
        {
            Debug.LogWarning("[CompassSystem] Compass bar is null! Cannot show/hide. Creating compass UI...");
            if (visible)
            {
                CreateCompassUI();
                CreateIndicators();
            }
        }
        
        if (indicatorInstances != null)
        {
            foreach (GameObject indicator in indicatorInstances)
            {
                if (indicator != null)
                {
                    indicator.SetActive(visible);
                }
            }
        }
    }
}

/// <summary>
/// Helper component for compass indicators.
/// </summary>
public class CompassIndicator : MonoBehaviour
{
    public CompassSystem.TrackableObject trackableObject;
}

