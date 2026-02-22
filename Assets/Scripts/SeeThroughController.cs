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

    void Update()
    {
        // Sécurité : si le joueur n'est pas assigné, on ne fait rien
        if (playerTransform == null) return;

        // 1. On envoie toujours la position du joueur au Shader
        Shader.SetGlobalVector("_GlobalPlayerPos", playerTransform.position);

        // 2. Calcul de la direction et de la distance entre la Caméra et le Joueur
        Vector3 directionToPlayer = playerTransform.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        // 3. LA CORRECTION EST ICI : 
        // On crée un objet "Ray" qui "emballe" le point de départ et la direction.
        Ray castRay = new Ray(transform.position, directionToPlayer.normalized);

        // 4. Le SphereCast avec notre nouveau 'Ray'
        bool isHittingTransparentLayer = Physics.SphereCast(
            castRay,              // Le rayon (origine + direction)
            castRadius,           // L'épaisseur du tube (générosité)
            distanceToPlayer,     // Longueur maximale (jusqu'au joueur)
            transparentWallLayer  // Le filtre : on ne détecte QUE ce Layer
        );

        // 5. On parle au Shader en fonction du résultat
        if (isHittingTransparentLayer)
        {
            // La sphère a touché un mur ! On active le trou.
            Shader.SetGlobalFloat("_HoleVisibilityMask", 1f);
        }
        else
        {
            // La voie est libre, on désactive le trou.
            Shader.SetGlobalFloat("_HoleVisibilityMask", 0f);
        }
    }
}