using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    [Header("Crosshair Settings")]
    public Color normalColor = Color.white;
    public Color targetColor = Color.red;
    public float size = 4f;
    public float thickness = 2f;
    public float length = 10f;

    private RectTransform rectTransform;
    private ObjectInteractionController interactionController;
    private LayerMask interactableLayer;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        interactionController = FindObjectOfType<ObjectInteractionController>();

        if (rectTransform == null)
        {
            Debug.LogError("No RectTransform found on crosshair!");
            return;
        }

        if (interactionController == null)
        {
            Debug.LogError("No ObjectInteractionController found in scene!");
            return;
        }

        // Set the interactable layer
        interactableLayer = (1 << 8); // Layer 8 is the Destroyable layer

        // Set initial size
        rectTransform.sizeDelta = new Vector2(length * 2, length * 2);
    }

    void Update()
    {
        // Check if looking at a destroyable object
        RaycastHit hit;
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        if (Physics.Raycast(ray, out hit, interactionController.hitRange, interactableLayer))
        {
            if (hit.collider.CompareTag("Destroyable"))
            {
                // Change color when looking at a destroyable object
                foreach (Image img in GetComponentsInChildren<Image>())
                {
                    img.color = targetColor;
                }
            }
        }
        else
        {
            // Reset to normal color
            foreach (Image img in GetComponentsInChildren<Image>())
            {
                img.color = normalColor;
            }
        }
    }
} 