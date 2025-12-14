using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles picking up the compass asset and showing objective panel.
/// </summary>
public class CompassPickup : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("Key to pick up/interact with compass.")]
    public KeyCode interactKey = KeyCode.F;
    
    [Tooltip("How close player needs to be to interact.")]
    public float interactionRange = 3f;
    
    [Header("UI References")]
    [Tooltip("Objective panel (dim black panel with text). Will be created automatically if not assigned.")]
    public GameObject objectivePanel;
    
    [Tooltip("Text component for objectives. Will be created automatically if not assigned.")]
    public TextMeshProUGUI objectiveText;
    
    [Tooltip("Objective text to display when compass is interacted with.")]
    [TextArea(5, 10)]
    public string objectiveMessage = "Your objective is to repair the ship and escape the island. Collect materials, survive the dangers, and complete your mission!";
    
    [Header("Visual Indicator")]
    [Tooltip("Floating arrow above compass to guide players. Will be created automatically if not assigned.")]
    public GameObject floatingArrow;
    
    [Tooltip("Height above compass for floating arrow.")]
    public float arrowHeight = 2f;
    
    [Tooltip("Speed of arrow floating animation.")]
    public float arrowFloatSpeed = 2f;
    
    [Tooltip("Amplitude of arrow floating animation.")]
    public float arrowFloatAmplitude = 0.3f;
    
    private Camera playerCamera;
    private GameObject player;
    private bool isCompassAcquired = false;
    private bool isShowingObjective = false;
    private Vector3 arrowStartPosition;
    private CompassSystem compassSystem;
    private float referenceCheckTimer = 0f;
    private const float REFERENCE_CHECK_INTERVAL = 2f; // Check every 2 seconds
    
    void Start()
    {
        RefreshReferences();
        
        // Create floating arrow if not assigned
        if (floatingArrow == null)
        {
            CreateFloatingArrow();
        }
        else
        {
            arrowStartPosition = floatingArrow.transform.position;
        }
        
        // Hide compass UI until acquired
        if (compassSystem != null)
        {
            compassSystem.SetCompassVisible(false);
        }
    }

    private void RefreshReferences()
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
        
        if (compassSystem == null)
        {
            compassSystem = FindObjectOfType<CompassSystem>();
            if (compassSystem == null)
            {
                Debug.LogWarning("CompassPickup: CompassSystem not found in scene! Make sure there's a GameObject with CompassSystem component.");
            }
            else
            {
                Debug.Log($"CompassPickup: Found CompassSystem: {compassSystem.gameObject.name}");
            }
        }
    }
    
    void Update()
    {
        // Periodically refresh references
        referenceCheckTimer += Time.deltaTime;
        if (referenceCheckTimer >= REFERENCE_CHECK_INTERVAL)
        {
            RefreshReferences();
            referenceCheckTimer = 0f;
        }

        // Animate floating arrow if compass not acquired
        if (floatingArrow != null && !isCompassAcquired)
        {
            float newY = arrowStartPosition.y + Mathf.Sin(Time.time * arrowFloatSpeed) * arrowFloatAmplitude;
            floatingArrow.transform.position = new Vector3(arrowStartPosition.x, newY, arrowStartPosition.z);
            
            // Make arrow face player
            if (player != null)
            {
                Vector3 directionToPlayer = (player.transform.position - floatingArrow.transform.position).normalized;
                directionToPlayer.y = 0f; // Keep arrow upright
                if (directionToPlayer != Vector3.zero)
                {
                    floatingArrow.transform.rotation = Quaternion.LookRotation(directionToPlayer);
                }
            }
        }
        
        // Don't process input if showing objective panel
        if (isShowingObjective)
        {
            // Check for any key press to close objective panel
            if (Input.anyKeyDown && !Input.GetKeyDown(KeyCode.Escape))
            {
                CloseObjectivePanel();
            }
            return;
        }
        
        // Check for interaction
        if (Input.GetKeyDown(interactKey) && !isCompassAcquired)
        {
            TryPickupCompass();
        }
        
        // If compass is acquired, check for interaction to show objectives
        if (isCompassAcquired && Input.GetKeyDown(interactKey))
        {
            TryShowObjectivePanel();
        }
    }
    
    void TryPickupCompass()
    {
        if (player == null || playerCamera == null)
        {
            return;
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (distanceToPlayer > interactionRange)
        {
            return;
        }
        
        // Check if player is looking at compass
        RaycastHit hit;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        if (Physics.Raycast(ray, out hit, interactionRange * 2f))
        {
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
            {
                PickupCompass();
                return;
            }
        }
        
        // Fallback: if close enough, allow pickup
        if (distanceToPlayer <= interactionRange)
        {
            PickupCompass();
        }
    }
    
    void PickupCompass()
    {
        isCompassAcquired = true;
        
        // Hide floating arrow
        if (floatingArrow != null)
        {
            floatingArrow.SetActive(false);
        }
        
        // Hide compass asset (or make it invisible)
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }
        
        // Disable collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
        
        // Enable compass UI
        if (compassSystem != null)
        {
            compassSystem.SetCompassVisible(true);
            Debug.Log("CompassPickup: Enabled compass UI via CompassSystem.");
        }
        else
        {
            // Try to find it again in case it wasn't found at Start
            compassSystem = FindObjectOfType<CompassSystem>();
            if (compassSystem != null)
            {
                compassSystem.SetCompassVisible(true);
                Debug.Log("CompassPickup: Found CompassSystem on retry. Enabled compass UI.");
            }
            else
            {
                Debug.LogError("CompassPickup: CompassSystem not found! Cannot enable compass UI. Make sure there's a GameObject with CompassSystem component in the scene.");
            }
        }
        
        Debug.Log("Compass acquired! Press F while holding compass to view objectives.");
    }
    
    void TryShowObjectivePanel()
    {
        if (player == null || playerCamera == null)
        {
            return;
        }
        
        // Check if player is close enough (optional - can remove this check if you want to show objectives from anywhere)
        // For now, we'll allow showing objectives from anywhere once compass is acquired
        
        ShowObjectivePanel();
    }
    
    void ShowObjectivePanel()
    {
        if (isShowingObjective) return;
        
        isShowingObjective = true;
        
        // Create objective panel if not assigned
        if (objectivePanel == null)
        {
            CreateObjectivePanel();
        }
        
        // Show panel
        if (objectivePanel != null)
        {
            objectivePanel.SetActive(true);
        }
        
        // Update text
        if (objectiveText != null)
        {
            objectiveText.text = objectiveMessage;
        }
        
        // Pause game
        Time.timeScale = 0f;
        
        // Disable player controls
        DisablePlayerControls();
        
        Debug.Log("Objective panel shown. Press any key to close.");
    }
    
    void CloseObjectivePanel()
    {
        if (!isShowingObjective) return;
        
        isShowingObjective = false;
        
        // Hide panel
        if (objectivePanel != null)
        {
            objectivePanel.SetActive(false);
        }
        
        // Resume game
        Time.timeScale = 1f;
        
        // Enable player controls
        EnablePlayerControls();
        
        Debug.Log("Objective panel closed.");
    }
    
    void CreateFloatingArrow()
    {
        // Create arrow GameObject
        floatingArrow = new GameObject("CompassFloatingArrow");
        floatingArrow.transform.position = transform.position + Vector3.up * arrowHeight;
        arrowStartPosition = floatingArrow.transform.position;
        
        // Create arrow mesh (simple triangle pointing down)
        MeshFilter meshFilter = floatingArrow.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = floatingArrow.AddComponent<MeshRenderer>();
        
        // Create arrow mesh
        Mesh arrowMesh = new Mesh();
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0, 0.5f, 0),      // Top point
            new Vector3(-0.3f, -0.2f, 0), // Bottom left
            new Vector3(0.3f, -0.2f, 0), // Bottom right
            new Vector3(0, -0.2f, 0.1f), // Back point
            new Vector3(0, -0.2f, -0.1f) // Front point
        };
        
        int[] triangles = new int[]
        {
            0, 1, 2, // Front face
            0, 2, 4, // Right face
            0, 4, 1, // Left face
            1, 2, 3, // Bottom
            2, 4, 3,
            4, 1, 3
        };
        
        arrowMesh.vertices = vertices;
        arrowMesh.triangles = triangles;
        arrowMesh.RecalculateNormals();
        
        meshFilter.mesh = arrowMesh;
        
        // Create material (green/yellow color)
        Material arrowMaterial = new Material(Shader.Find("Standard"));
        arrowMaterial.color = Color.yellow;
        arrowMaterial.SetFloat("_Metallic", 0f);
        arrowMaterial.SetFloat("_Glossiness", 0.5f);
        meshRenderer.material = arrowMaterial;
        
        // Make arrow face down initially
        floatingArrow.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        
        Debug.Log("Floating arrow created above compass.");
    }
    
    void CreateObjectivePanel()
    {
        // Find or create Canvas
        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Create panel
        GameObject panelObj = new GameObject("ObjectivePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.8f); // Dim black
        
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;
        
        objectivePanel = panelObj;
        
        // Create text
        GameObject textObj = new GameObject("ObjectiveText");
        textObj.transform.SetParent(panelObj.transform, false);
        
        objectiveText = textObj.AddComponent<TextMeshProUGUI>();
        objectiveText.text = objectiveMessage;
        objectiveText.fontSize = 24;
        objectiveText.color = Color.white;
        objectiveText.alignment = TextAlignmentOptions.Center;
        objectiveText.verticalAlignment = VerticalAlignmentOptions.Middle;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.1f);
        textRect.anchorMax = new Vector2(0.9f, 0.9f);
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        // Initially hide panel
        panelObj.SetActive(false);
        
        Debug.Log("Objective panel created.");
    }
    
    void DisablePlayerControls()
    {
        if (player == null) return;
        
        CharController_Motor motor = player.GetComponent<CharController_Motor>();
        if (motor != null)
        {
            motor.SetInputActive(false);
        }
    }
    
    void EnablePlayerControls()
    {
        if (player == null) return;
        
        CharController_Motor motor = player.GetComponent<CharController_Motor>();
        if (motor != null)
        {
            motor.SetInputActive(true);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}

