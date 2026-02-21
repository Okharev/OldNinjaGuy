using UnityEngine;
using System;

public class HealthComponent : MonoBehaviour
{
    [Header("Stats & Santé")]
    public int maxHealth = 100;
    
    // We use a property to easily read the current health from other scripts if needed
    public int CurrentHealth { get; private set; }

    // --- EVENTS ---
    // Other scripts (like UI, Audio Manager, or Game Manager) will listen to these
    public event Action<float> OnHealthChanged; 
    public event Action OnDeath;
    
    // Passing damage, source, and knockback is great for hit reactions/VFX
    public event Action<int, Vector3, float> OnDamageTaken; 

    private bool isDead = false;

    void Start()
    {
        CurrentHealth = maxHealth;
        
        // Notify UI of initial health state (1.0 = 100%)
        OnHealthChanged?.Invoke(1f); 
    }

    /// <summary>
    /// Call this from enemy attacks, traps, or bullets.
    /// </summary>
    public void TakeDamage(int damageAmount, Vector3 sourcePosition = default, float knockbackForce = 0f)
    {
        if (isDead) return;

        CurrentHealth -= damageAmount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth); // Prevent negative HP

        // Calculate percentage (0.0 to 1.0) for the UI bar
        float healthPercent = (float)CurrentHealth / maxHealth;
        
        // Broadcast that we took damage
        OnDamageTaken?.Invoke(damageAmount, sourcePosition, knockbackForce);
        OnHealthChanged?.Invoke(healthPercent);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Call this from health potions or healing zones.
    /// </summary>
    public void Heal(int healAmount)
    {
        if (isDead) return;

        CurrentHealth += healAmount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);
        
        float healthPercent = (float)CurrentHealth / maxHealth;
        OnHealthChanged?.Invoke(healthPercent);
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        OnDeath?.Invoke();
        
        // NOTE: We don't Destroy(gameObject) here! 
        // The PlayerController or EnemyController should listen to OnDeath 
        // and handle their own specific death animations/destruction.
    }
}