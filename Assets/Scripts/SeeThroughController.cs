using UnityEngine;

public class SeeThroughController : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Glisse ici le Transform de ton joueur depuis la hiérarchie")]
    public Transform playerTransform;

    [Header("Paramètres de Calque (Layer)")]
    [Tooltip("Choisis ici le ou les Layers qui ont le droit de devenir transparents")]
    public LayerMask transparentWallLayer;

    void Update()
    {
        // Sécurité : si le joueur n'est pas assigné, on ne fait rien
        if (playerTransform == null) return;

        // 1. On envoie toujours la position du joueur au Shader (tu faisais peut-être déjà ça !)
        Shader.SetGlobalVector("_GlobalPlayerPos", playerTransform.position);

        // 2. Calcul de la direction et de la distance entre la Caméra et le Joueur
        Vector3 directionToPlayer = playerTransform.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        // 3. Le Raycast : On lance un rayon laser invisible de la caméra vers le joueur
        // On limite sa longueur à "distanceToPlayer" pour ne pas toucher les murs derrière lui.
        bool isHittingTransparentLayer = Physics.Raycast(
            transform.position,         // Point de départ (la caméra)
            directionToPlayer.normalized, // Direction (vers le joueur)
            distanceToPlayer,           // Longueur maximale du rayon
            transparentWallLayer        // Le filtre : on ne détecte QUE ce Layer !
        );

        // 4. On parle au Shader en fonction du résultat
        if (isHittingTransparentLayer)
        {
            // On a touché un mur du bon Layer ! On envoie 1 au shader pour activer le trou.
            Shader.SetGlobalFloat("_HoleVisibilityMask", 1f);
        }
        else
        {
            // La vue est dégagée, ou bloquée par un objet non autorisé (ex: le sol).
            // On envoie 0 au shader pour désactiver le trou.
            Shader.SetGlobalFloat("_HoleVisibilityMask", 0f);
        }
    }
}