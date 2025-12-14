using UnityEngine;

/// <summary>
/// Component attached to the player GameObject that implements IDamageable interface.
/// Forwards damage calls to UIManager which manages the actual health system.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    /// <summary>
    /// Called by enemies (like Ghoul Zombie) to damage the player.
    /// Forwards the damage to UIManager which handles health reduction and UI updates.
    /// </summary>
    public void ApplyDamage(float amount)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ApplyDamage(amount);
        }
        else
        {
            Debug.LogWarning("PlayerHealth: UIManager.Instance is null! Cannot apply damage.");
        }
    }
}



