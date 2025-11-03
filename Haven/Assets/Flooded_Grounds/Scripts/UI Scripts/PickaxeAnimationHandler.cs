using UnityEngine;

public class PickaxeAnimationHandler : MonoBehaviour
{
    public Animator animator;
    public string swingTrigger = "Swing"; // Use the same trigger as Axe to reuse the animation

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator != null)
        {
            animator.ResetTrigger(swingTrigger);
            Debug.Log($"PickaxeAnimationHandler: Resetting {swingTrigger} trigger on Awake for {gameObject.name}");
        }
    }

    // Public method to play the swing animation
    public void PlaySwingAnimation()
    {
        if (animator != null)
        {
            Debug.Log("Playing Pickaxe Swing Animation!");
            animator.SetTrigger(swingTrigger);
        }
    }
}


