using UnityEngine;

public class WoodAnimationHandler : MonoBehaviour
{
    public Animator animator;
    public string useTrigger = "Chop"; // Name of the trigger in your Animator for wood action

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator != null)
        {
            animator.ResetTrigger(useTrigger);
            Debug.Log($"WoodAnimationHandler: Resetting {useTrigger} trigger on Awake for {gameObject.name}");
        }
    }

    // Public method to play the wood action animation
    public void PlayWoodActionAnimation()
    {
        if (animator != null)
        {
            Debug.Log("Playing Wood Action Animation!");
            animator.SetTrigger(useTrigger);
        }
    }
} 