using UnityEngine;

public class AxeAnimationHandler : MonoBehaviour
{
    public Animator animator;
    public string swingTrigger = "Swing"; // Name of the trigger in your Animator

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator != null)
        {
            animator.ResetTrigger(swingTrigger);
            Debug.Log($"AxeAnimationHandler: Resetting {swingTrigger} trigger on Awake for {gameObject.name}");
        }
    }

    // Public method to play the swing animation
    public void PlaySwingAnimation()
    {
        if (animator != null)
        {
            Debug.Log("Playing Axe Swing Animation!");
            animator.SetTrigger(swingTrigger);
        }
    }
} 