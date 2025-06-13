using UnityEngine;

public class RockAnimationHandler : MonoBehaviour
{
    public Animator animator;
    public string swingTrigger = "RockSwing"; // Make sure this matches your Animator's trigger name

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        // Reset the trigger on Awake to prevent auto-play issues
        if (animator != null)
        {
            animator.ResetTrigger(swingTrigger);
            Debug.Log($"RockAnimationHandler: Resetting {swingTrigger} trigger on Awake for {gameObject.name}");
        }
    }

    // Public method to play the swing animation
    public void PlaySwingAnimation()
    {
        if (animator != null)
        {
            Debug.Log("Playing Rock Swing Animation!");
            animator.SetTrigger(swingTrigger);
        }
    }
} 