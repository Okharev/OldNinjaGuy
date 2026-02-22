using UnityEngine;

public class SeeThroughController : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Glisse ici le Transform de ton joueur depuis la hiérarchie")]
    public Transform playerTransform;

    [Header("Paramètres de Détection")]
    [Tooltip("Choisis ici le ou les Layers qui ont le droit de devenir transparents")]
    public LayerMask transparentWallLayer;
    
    [Tooltip("L'épaisseur du rayon. Plus cette valeur est grande, plus la détection est généreuse !")]
    public float castRadius = 0.5f;

    [Header("Animation du Trou")]
    [Tooltip("Vitesse à laquelle le trou s'ouvre et se ferme (Plus c'est grand, plus c'est rapide)")]
    public float fadeSpeed = 5f;

    // Cette variable va garder en mémoire l'état actuel de l'ouverture (entre 0 et 1)
    private float currentVisibility = 0f;

    void Update()
    {
        // Sécurité : si le joueur n'est pas assigné, on ne fait rien
        if (playerTransform == null) return;

        // 1. On envoie toujours la position du joueur au Shader
        Shader.SetGlobalVector("_GlobalPlayerPos", playerTransform.position);

        // 2. Calcul de la direction et de la distance entre la Caméra et le Joueur
        Vector3 directionToPlayer = playerTransform.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        // 3. On crée un objet "Ray" pour lancer notre sphère
        Ray castRay = new Ray(transform.position, directionToPlayer.normalized);

        // 4. Le SphereCast pour détecter les murs
        bool isHittingTransparentLayer = Physics.SphereCast(
            castRay,              
            castRadius,           
            distanceToPlayer,     
            transparentWallLayer  
        );

        // 5. ANIMATION AVEC LERP
        // On définit notre "Cible" : 1 (ouvert) si on touche un mur, 0 (fermé) si la voie est libre
        float targetVisibility = isHittingTransparentLayer ? 1f : 0f;

        // Mathf.Lerp va glisser doucement de 'currentVisibility' vers 'targetVisibility'
        // Time.deltaTime permet à l'animation d'avoir la même vitesse peu importe les FPS du jeu !
        currentVisibility = Mathf.Lerp(currentVisibility, targetVisibility, Time.deltaTime * fadeSpeed);

        // 6. On envoie cette valeur animée (ex: 0.1, 0.4, 0.8...) au Shader
        Shader.SetGlobalFloat("_HoleVisibilityMask", currentVisibility);
    }
}