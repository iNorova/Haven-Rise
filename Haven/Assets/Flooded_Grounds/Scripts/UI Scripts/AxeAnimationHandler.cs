using UnityEngine;

public class AxeAnimationHandler : MonoBehaviour
{
    public Animator animator;
    public string swingTrigger = "Swing"; // Name of the trigger in your Animator

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
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