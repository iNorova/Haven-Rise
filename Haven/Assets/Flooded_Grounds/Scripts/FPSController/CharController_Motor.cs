using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Add UI namespace

public class CharController_Motor : MonoBehaviour {

    public float speed = 10.0f;
    public float sprintMultiplier = 1.5f; // New: Sprint multiplier
    public float sensitivity = 30.0f;
    public float WaterHeight = 15.5f;
    public float jumpForce = 8.0f;
    public float gravityValue = -9.8f;

    // Stamina related variables
    public float maxStamina = 100f;
    public float staminaDepletionRate = 25f;
    public float staminaRegenerationRate = 15f;
    public float staminaRegenerationDelay = 1f;
    private float currentStamina;
    private float lastSprintTime;
    public Slider staminaBar; // Reference to UI slider

    private float verticalVelocity = 0f;
    private CharacterController character;
    public GameObject cam;
    private float moveFB, moveLR;
    private float rotX, rotY;
    public bool webGLRightClickRotation = true;

    void Start(){
        character = GetComponent<CharacterController>();
        if (Application.isEditor) {
            webGLRightClickRotation = false;
            sensitivity = sensitivity * 1.5f;
        }
        currentStamina = maxStamina; // Initialize stamina
    }

    void CheckForWaterHeight(){
        if (transform.position.y < WaterHeight) {
            verticalVelocity = 0f;
        }
    }

    void Update(){
        // Determine if sprinting and handle stamina
        float currentSpeed = speed;
        bool canSprint = currentStamina > 0 && Input.GetKey(KeyCode.LeftShift);
        
        if (canSprint) {
            currentSpeed *= sprintMultiplier;
            currentStamina -= staminaDepletionRate * Time.deltaTime;
            lastSprintTime = Time.time;
        }
        else if (Time.time - lastSprintTime >= staminaRegenerationDelay && !Input.GetKey(KeyCode.LeftShift)) {
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenerationRate * Time.deltaTime);
        }

        // Update UI
        if (staminaBar != null) {
            staminaBar.value = currentStamina / maxStamina;
        }

        moveFB = Input.GetAxis("Horizontal") * currentSpeed;
        moveLR = Input.GetAxis("Vertical") * currentSpeed;

        rotX = Input.GetAxis("Mouse X") * sensitivity;
        rotY = Input.GetAxis("Mouse Y") * sensitivity;

        Vector3 movement = new Vector3(moveFB, 0, moveLR);
        movement = transform.rotation * movement;

        // Handle gravity and jumping
        if (character.isGrounded) {
            verticalVelocity = -1f;

            if (Input.GetKeyDown(KeyCode.Space)) {
                verticalVelocity = jumpForce;
            }
        } else {
            verticalVelocity += gravityValue * Time.deltaTime;
        }

        movement.y = verticalVelocity;

        character.Move(movement * Time.deltaTime);

        if (webGLRightClickRotation) {
            if (Input.GetKey(KeyCode.Mouse0)) {
                CameraRotation(cam, rotX, rotY);
            }
        } else {
            CameraRotation(cam, rotX, rotY);
        }
    }

    void CameraRotation(GameObject cam, float rotX, float rotY){
        transform.Rotate(0, rotX * Time.deltaTime, 0);
        cam.transform.Rotate(-rotY * Time.deltaTime, 0, 0);
    }
}
