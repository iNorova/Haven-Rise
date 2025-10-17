using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Add UI namespace

public class CharController_Motor : MonoBehaviour {
    
    public float speed = 10.0f;
    public float sprintMultiplier = 1.5f; // New: Sprint multiplier
    public float sensitivity = 2.0f; // Lowered for better control
    public float WaterHeight = 15.5f;
    public float jumpForce = 8.0f;
    public float gravityValue = -9.8f;

    // Crouch parameters
    public float crouchHeight = 1.0f;
    public float standingHeight = 2.0f;
    public float crouchSpeed = 10f;
    public float crouchSpeedMultiplier = 0.5f;
    private float currentHeight;
    private bool isCrouching;
    private Vector3 originalCameraPos;

    // Head bob parameters
    public float bobFrequency = 5f;
    public float bobHorizontalAmplitude = 0.1f;
    public float bobVerticalAmplitude = 0.1f;
    private float bobTimer;
    private Vector3 targetCameraPosition;

    private float verticalVelocity = 0f;
    private CharacterController character;
    public GameObject cam;
    private float cameraPitch = 0f;
    public float maxLookAngle = 85f;
    public bool webGLRightClickRotation = true;

    // Add hand transform for holding items
    public Transform playerHand; // Reference to the hand transform

    // New: Control input reception
    private bool canReceiveInput = true;

    void Start(){
        character = GetComponent<CharacterController>();
        if (Application.isEditor) {
            webGLRightClickRotation = false;
            sensitivity = sensitivity * 1.5f;
        }
        // Cursor lock/hide handled by InventoryUIManager now
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;

        // Initialize crouch and head bob
        currentHeight = standingHeight;
        character.height = standingHeight;
        originalCameraPos = cam.transform.localPosition;
        targetCameraPosition = originalCameraPos;

        // Create hand transform if it doesn't exist
        if (playerHand == null)
        {
            // First check if the hand already exists as a child of the camera
            Transform existingHand = cam.transform.Find("PlayerHand");
            if (existingHand != null)
            {
                playerHand = existingHand;
            }
            else
            {
                // Create new hand transform
                GameObject handObj = new GameObject("PlayerHand");
                handObj.transform.SetParent(cam.transform, false);
                handObj.transform.localPosition = new Vector3(0.3f, -0.2f, 0.5f);
                handObj.transform.localRotation = Quaternion.identity;
                playerHand = handObj.transform;
                
                // Save the changes to the scene
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(gameObject);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
                #endif
            }
        }
    }

    void CheckForWaterHeight(){
        if (transform.position.y < WaterHeight) {
            verticalVelocity = 0f;
        }
    }

    void Update(){
    // ✅ Stop all input when the game is paused
    if (Time.timeScale == 0f)
        return;

    if (canReceiveInput)
    {
        HandleMouseLook();
        HandleMovement();
        HandleCrouch();
        HandleHeadBob();
    }
}
    // New: Public method to set input active/inactive
    public void SetInputActive(bool active)
    {
        canReceiveInput = active;
    }

    void HandleMouseLook() {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        transform.Rotate(0, mouseX, 0);
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);
        cam.transform.localEulerAngles = new Vector3(cameraPitch, 0, 0);
    }

    void HandleMovement() {
        float currentSpeed = speed;
        bool isSprintKeyPressed = Input.GetKey(KeyCode.LeftShift);
        bool canSprint = UIManager.Instance.CanSprint() && isSprintKeyPressed && !isCrouching;
        
        if (canSprint) {
            currentSpeed *= sprintMultiplier;
            UIManager.Instance.UseStamina();
        }
        else if (!isSprintKeyPressed) {
            UIManager.Instance.StopUsingStamina();
        }

        if (isCrouching) {
            currentSpeed *= crouchSpeedMultiplier;
        }

        float moveLR = Input.GetAxis("Horizontal");
        float moveFB = Input.GetAxis("Vertical");
        Vector3 move = transform.right * moveLR + transform.forward * moveFB;
        move *= currentSpeed;

        if (character.isGrounded) {
            verticalVelocity = -2f;
            if (Input.GetKeyDown(KeyCode.Space) && !isCrouching) {
                verticalVelocity = jumpForce;
            }
        } else {
            verticalVelocity += gravityValue * Time.deltaTime;
        }

        move.y = verticalVelocity;
        character.Move(move * Time.deltaTime);
    }

    void HandleCrouch() {
        if (Input.GetKeyDown(KeyCode.C)) {
            isCrouching = !isCrouching;
        }

        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        if (currentHeight != targetHeight) {
            currentHeight = Mathf.Lerp(currentHeight, targetHeight, crouchSpeed * Time.deltaTime);
            character.height = currentHeight;
            
            // Adjust camera position when crouching
            Vector3 camPos = cam.transform.localPosition;
            camPos.y = originalCameraPos.y * (currentHeight / standingHeight);
            cam.transform.localPosition = camPos;
        }
    }

    void HandleHeadBob() {
        if (!character.isGrounded) return;

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        bool isMoving = Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f;

        if (isMoving) {
            // Increment the bob timer based on movement speed
            float bobSpeedMultiplier = isCrouching ? 0.5f : 1f;
            bobTimer += Time.deltaTime * bobFrequency * bobSpeedMultiplier;

            // Calculate bob offsets
            float horizontalBob = Mathf.Sin(bobTimer) * bobHorizontalAmplitude;
            float verticalBob = Mathf.Sin(bobTimer * 2f) * bobVerticalAmplitude;

            // Calculate target position with bob
            targetCameraPosition = originalCameraPos + new Vector3(horizontalBob, verticalBob, 0f);
        } else {
            // Reset to original position when not moving
            targetCameraPosition = originalCameraPos;
            bobTimer = 0f;
        }

        // Smoothly interpolate to target position
        cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, targetCameraPosition, Time.deltaTime * 5f);
    }

    void CameraRotation(GameObject cam, float rotX, float rotY){
        transform.Rotate(0, rotX * Time.deltaTime, 0);
        cam.transform.Rotate(-rotY * Time.deltaTime, 0, 0);
    }

    // Public properties for AI detection
    public bool IsCrouching() { return isCrouching; }
    public bool IsSprinting() { return UIManager.Instance.CanSprint() && Input.GetKey(KeyCode.LeftShift) && !isCrouching; }
    public bool IsWalking() {
        float moveLR = Input.GetAxis("Horizontal");
        float moveFB = Input.GetAxis("Vertical");
        bool isMoving = Mathf.Abs(moveLR) > 0.1f || Mathf.Abs(moveFB) > 0.1f;
        return isMoving && !IsSprinting() && !isCrouching;
    }
}
