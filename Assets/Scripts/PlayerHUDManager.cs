using UnityEngine;
using UnityEngine.UIElements;

public class PlayerHUDManager : MonoBehaviour
{
    [Header("UI Setup")]
    public UIDocument hudDocument;

    [Header("Player References")]
    [Tooltip("Drag the Player GameObject here, or leave empty to find by Tag")]
    public HealthComponent playerHealth; 
    public DrunkenEffectManager drunkManager;

    private VisualElement healthFill;
    private VisualElement drunkFill; 

    void OnEnable()
    {
        var root = hudDocument.rootVisualElement;
        healthFill = root.Q<VisualElement>("PlayerHealthFill");
        drunkFill = root.Q<VisualElement>("PlayerDrunkFill"); 

        GameObject player = null;
        if (!playerHealth || !drunkManager)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (!playerHealth && player)
            playerHealth = player.GetComponent<HealthComponent>();

        if (!drunkManager && player)
            drunkManager = player.GetComponent<DrunkenEffectManager>(); 

        if (drunkManager != null)
        {
            Debug.Log("✅ SUCCÈS : Le HUD a trouvé le DrunkenEffectManager et s'y abonne !");
            drunkManager.OnDrunkennessChanged += UpdateDrunkBar;
        }
        else
        {
            Debug.LogError("❌ ERREUR : Le drunkManager est NULL au moment de s'abonner !");
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthBar;
            // Force une mise à jour initiale
            UpdateHealthBar((float)playerHealth.GetType().GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(playerHealth) / playerHealth.maxHealth); 
        }
    }

    void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthBar;
        }

        if (drunkManager != null)
        {
            drunkManager.OnDrunkennessChanged -= UpdateDrunkBar;
        }
    }

    private void UpdateHealthBar(float percent)
    {
        if (healthFill != null)
        {
            // Met à jour la largeur pour la barre de vie (remplissage horizontal)
            healthFill.style.width = Length.Percent(percent * 100);
            
            // J'ai supprimé le changement de couleur ici car l'image stylisée gère déjà le visuel !
        }
    }

    private void UpdateDrunkBar(float percent)
    {
        if (drunkFill != null)
        {
            // Met à jour la HAUTEUR pour la gourde (remplissage vertical de bas en haut)
            drunkFill.style.height = Length.Percent(percent * 100);
        }
    }
}