using UnityEngine;

public class BedInteraction : MonoBehaviour
{
    [Header("Sleep Settings")]
    public KeyCode sleepKey = KeyCode.E;       // Key to sleep in the bed
    public KeyCode wakeKey = KeyCode.Escape;   // Key to wake up
    public float interactionRange = 3f;        // How close player needs to be to interact
    public Transform sleepCameraPosition;      // Optional: specific camera position for sleep view
    public Vector3 sleepCameraOffset = new Vector3(0, 1.5f, 2f); // Camera offset from bed center if no position specified

    private Camera playerCamera;
    private GameObject player;
    private CharController_Motor playerMotor;
    public bool isSleeping = false; // Made public so BedPickup can check it
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private Vector3 originalPlayerPosition;
    private Quaternion originalPlayerRotation;
    
    // Static flag to prevent pause menu from opening on same frame bed is exited
    private static bool bedJustExitedViaEsc = false;

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("BedInteraction: No main camera found!");
        }

        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMotor = player.GetComponent<CharController_Motor>();
        }
    }

    void Update()
    {
        // Don't process input if player is sleeping (they'll use ESC to wake up)
        if (isSleeping)
        {
            if (Input.GetKeyDown(wakeKey))
            {
                bedJustExitedViaEsc = true; // Set flag before waking up
                WakeUp();
            }
            return;
        }

        // Check if player is looking at this bed and pressing E
        if (playerCamera != null && Input.GetKeyDown(sleepKey))
        {
            TrySleep();
        }
    }
    
    void LateUpdate()
    {
        // Reset the flag at end of frame (allows pause menu to open on next frame)
        bedJustExitedViaEsc = false;
    }
    
    // Static method for other scripts to check if bed was just exited via ESC
    public static bool WasBedJustExitedViaEsc()
    {
        return bedJustExitedViaEsc;
    }
    
    // Static method to check if any bed is currently being slept in
    public static bool IsAnyBedActive()
    {
        BedInteraction[] beds = FindObjectsByType<BedInteraction>(FindObjectsSortMode.None);
        foreach (BedInteraction bed in beds)
        {
            if (bed != null && bed.isSleeping)
            {
                return true;
            }
        }
        return false;
    }

    void TrySleep()
    {
        // Check if player is close enough and looking at the bed
        if (player == null || playerCamera == null)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (distanceToPlayer > interactionRange)
        {
            return;
        }

        // Raycast to check if player is looking at this bed
        RaycastHit hit;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            if (hit.collider.gameObject == this.gameObject || hit.collider.transform.IsChildOf(transform))
            {
                Sleep();
            }
        }
    }

    void Sleep()
    {
        if (isSleeping || playerCamera == null || player == null)
            return;

        Debug.Log("BedInteraction: Player is now sleeping.");
        isSleeping = true;

        // Store original positions
        originalCameraPosition = playerCamera.transform.position;
        originalCameraRotation = playerCamera.transform.rotation;
        originalPlayerPosition = player.transform.position;
        originalPlayerRotation = player.transform.rotation;

        // Disable player movement
        if (playerMotor != null)
        {
            playerMotor.SetInputActive(false);
        }

        // Move camera to sleep position
        Vector3 targetCameraPos;
        Quaternion targetCameraRot;

        if (sleepCameraPosition != null)
        {
            targetCameraPos = sleepCameraPosition.position;
            targetCameraRot = sleepCameraPosition.rotation;
        }
        else
        {
            // Calculate camera position relative to bed
            targetCameraPos = transform.position + transform.TransformDirection(sleepCameraOffset);
            targetCameraRot = Quaternion.LookRotation(transform.position - targetCameraPos);
        }

        playerCamera.transform.position = targetCameraPos;
        playerCamera.transform.rotation = targetCameraRot;

        // Lock cursor (optional, for better sleep experience)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void WakeUp()
    {
        if (!isSleeping || playerCamera == null || player == null)
            return;

        Debug.Log("BedInteraction: Player is waking up.");
        isSleeping = false;

        // Restore original camera position
        playerCamera.transform.position = originalCameraPosition;
        playerCamera.transform.rotation = originalCameraRotation;

        // Restore player position (optional - you might want to keep them at the bed)
        // player.transform.position = originalPlayerPosition;
        // player.transform.rotation = originalPlayerRotation;

        // Re-enable player movement
        if (playerMotor != null)
        {
            playerMotor.SetInputActive(true);
        }

        // Unlock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Visualize interaction range in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // Draw sleep camera position
        if (sleepCameraPosition != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(sleepCameraPosition.position, 0.2f);
            Gizmos.DrawLine(transform.position, sleepCameraPosition.position);
        }
        else
        {
            Vector3 cameraPos = transform.position + transform.TransformDirection(sleepCameraOffset);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(cameraPos, 0.2f);
            Gizmos.DrawLine(transform.position, cameraPos);
        }
    }
}

