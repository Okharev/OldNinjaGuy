using UnityEngine;
using UnityEngine.UIElements;

public class PlayerHUDManager : MonoBehaviour
{
    [Header("UI Setup")]
    public UIDocument hudDocument;

    [Header("Player Reference")]
    [Tooltip("Drag the Player GameObject here, or leave empty to find by Tag")]
    public HealthComponent playerHealth; 

    private VisualElement healthFill;

    void OnEnable()
    {
        // 1. Get the UI Elements
        var root = hudDocument.rootVisualElement;
        healthFill = root.Q<VisualElement>("PlayerHealthFill");

        // 2. Find the player if not assigned
        if (!playerHealth)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player)
            {
                playerHealth = player.GetComponent<HealthComponent>();
            }
        }

        // 3. Subscribe to the health event
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthBar;
            
            // Force an update immediately just in case UI loads after Player Start
            UpdateHealthBar((float)playerHealth.GetType().GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(playerHealth) / playerHealth.maxHealth); 
        }
        else
        {
            Debug.LogWarning("PlayerHUDManager: Could not find Player HealthComponent!");
        }
    }

    void OnDisable()
    {
        // ALWAYS unsubscribe to prevent memory leaks when changing scenes
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthBar;
        }
    }

    private void UpdateHealthBar(float percent)
    {
        // Update the width of the green fill bar
        if (healthFill != null)
        {
            healthFill.style.width = Length.Percent(percent * 100);
            
            // Optional Polish: Change color to red if health is low (< 25%)
            if (percent < 0.25f) {
                healthFill.style.backgroundColor = new StyleColor(new Color(0.9f, 0.2f, 0.2f)); 
            } else {
                // Reset back to green
                healthFill.style.backgroundColor = new StyleColor(new Color(0.18f, 0.8f, 0.44f)); 
            }
        }
    }
}