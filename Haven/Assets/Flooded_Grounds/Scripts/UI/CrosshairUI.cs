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
    private TreeCuttingController treeCutter;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        treeCutter = FindObjectOfType<TreeCuttingController>();

        if (rectTransform == null)
        {
            Debug.LogError("No RectTransform found on crosshair!");
            return;
        }

        // Set initial size
        rectTransform.sizeDelta = new Vector2(length * 2, length * 2);
    }

    void Update()
    {
        // Check if looking at a tree
        RaycastHit hit;
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        if (Physics.Raycast(ray, out hit, treeCutter.hitRange, treeCutter.treeLayer))
        {
            if (hit.collider.GetComponent<TreeComponent>() != null)
            {
                // Change color when looking at a tree
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