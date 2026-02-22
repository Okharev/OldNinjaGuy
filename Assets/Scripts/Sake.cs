using UnityEngine;

public class Sake : MonoBehaviour
{
    [Header("Effets du Saké")]
    [Tooltip("Le nombre de points de vie que le saké restaure au joueur")]
    public int healAmount = 20;

    [Header("Animation de l'objet (Flottement)")]
    [Tooltip("Vitesse de rotation sur l'axe Y (en degrés par seconde)")]
    public float spinSpeed = 90f; 
    [Tooltip("Vitesse du mouvement de haut en bas")]
    public float bobSpeed = 2f;   
    [Tooltip("Hauteur maximale du mouvement de haut en bas")]
    public float bobHeight = 0.25f; 

    // On garde en mémoire la position où l'objet est tombé
    private Vector3 startPosition;

    void Start()
    {
        // On enregistre la position initiale au moment où l'objet est créé
        startPosition = transform.position;
    }

    void Update()
    {
        // 1. Faire tourner l'objet sur l'axe Y (Vector3.up)
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);

        // 2. Calculer le mouvement de haut en bas (flottement fluide)
        // Mathf.Sin crée une vague qui monte et descend doucement au fil du temps
        float newY = startPosition.y + (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        
        // On applique la nouvelle position en gardant les axes X et Z intacts
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void OnTriggerEnter(Collider other)
    {
        // On vérifie si l'objet qui entre en collision est bien le joueur
        if (other.CompareTag("Player"))
        {
            // 1. Appliquer l'effet d'ivresse
            DrunkenEffectManager drunkenManager = FindAnyObjectByType<DrunkenEffectManager>();
            if (drunkenManager != null)
            {
                drunkenManager.DrinkSake();
            }

            // 2. Soigner le joueur
            HealthComponent playerHealth = other.GetComponent<HealthComponent>();
            if (playerHealth != null)
            {
                playerHealth.Heal(healAmount);
            }
        
            // 3. Détruire la bouteille de saké
            Destroy(gameObject); 
        }
    }
}