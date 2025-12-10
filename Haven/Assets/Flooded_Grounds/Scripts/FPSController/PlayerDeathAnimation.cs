using UnityEngine;

/// <summary>
/// Handles player death animation when health reaches zero.
/// Attach this to the player GameObject along with an Animator component.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerDeathAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Name of the death trigger parameter in the Animator Controller.")]
    public string deathTriggerName = "Die";
    
    [Tooltip("Name of the death bool parameter (alternative to trigger).")]
    public string deathBoolName = "IsDead";
    
    [Tooltip("Use bool instead of trigger for death state.")]
    public bool useBoolInsteadOfTrigger = false;

    [Header("Death Effects")]
    [Tooltip("Disable player movement when dead.")]
    public bool disableMovementOnDeath = true;
    
    [Tooltip("Disable camera rotation when dead.")]
    public bool disableCameraOnDeath = true;
    
    [Tooltip("Time to wait before showing respawn UI (allows death animation to play).")]
    public float deathAnimationDelay = 1f;

    private Animator animator;
    private CharController_Motor playerMotor;
    private bool isDead = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        playerMotor = GetComponent<CharController_Motor>();
        
        // Subscribe to death event
        UIManager.OnPlayerDeath += OnPlayerDeath;
    }

    void OnDestroy()
    {
        // Unsubscribe from death event
        UIManager.OnPlayerDeath -= OnPlayerDeath;
    }

    private void OnPlayerDeath()
    {
        if (isDead) return; // Prevent multiple death triggers
        
        isDead = true;
        PlayDeathAnimation();
        DisablePlayerControls();
    }

    private void PlayDeathAnimation()
    {
        if (animator == null)
        {
            Debug.LogWarning("PlayerDeathAnimation: Animator component not found! Cannot play death animation.");
            return;
        }

        // Check if parameter exists before setting it
        bool hasTrigger = HasParameter(deathTriggerName, AnimatorControllerParameterType.Trigger);
        bool hasBool = HasParameter(deathBoolName, AnimatorControllerParameterType.Bool);

        if (useBoolInsteadOfTrigger && hasBool)
        {
            animator.SetBool(deathBoolName, true);
            Debug.Log($"PlayerDeathAnimation: Set death bool '{deathBoolName}' to true.");
        }
        else if (hasTrigger)
        {
            animator.SetTrigger(deathTriggerName);
            Debug.Log($"PlayerDeathAnimation: Triggered death animation '{deathTriggerName}'.");
        }
        else if (hasBool)
        {
            // Fallback to bool if trigger doesn't exist
            animator.SetBool(deathBoolName, true);
            Debug.Log($"PlayerDeathAnimation: Set death bool '{deathBoolName}' to true (fallback).");
        }
        else
        {
            Debug.LogWarning($"PlayerDeathAnimation: Neither death trigger '{deathTriggerName}' nor bool '{deathBoolName}' found in Animator Controller!");
        }
    }

    private void DisablePlayerControls()
    {
        if (disableMovementOnDeath && playerMotor != null)
        {
            playerMotor.SetInputActive(false);
        }

        if (disableCameraOnDeath && playerMotor != null)
        {
            // Disable camera by disabling the component's Update method
            // The CharController_Motor will check canReceiveInput
            playerMotor.SetInputActive(false);
        }
    }

    /// <summary>
    /// Called when player respawns to reset death state.
    /// </summary>
    public void OnPlayerRespawn()
    {
        isDead = false;
        
        if (animator != null)
        {
            // Reset death bool if using bool
            if (HasParameter(deathBoolName, AnimatorControllerParameterType.Bool))
            {
                animator.SetBool(deathBoolName, false);
            }
        }

        // Re-enable player controls
        if (playerMotor != null)
        {
            playerMotor.SetInputActive(true);
        }
    }

    private bool HasParameter(string paramName, AnimatorControllerParameterType type)
    {
        if (animator == null || string.IsNullOrEmpty(paramName))
            return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName && param.type == type)
                return true;
        }
        return false;
    }
}


