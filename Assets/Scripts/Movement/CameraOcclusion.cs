using UnityEngine;

namespace Movement
{
    public class CameraOcclusion : MonoBehaviour
    {
        [Header("Paramètres Principaux")]
        [Tooltip("Glisse ici le Transform de ton joueur")]
        public Transform playerTarget; 
    
        [Tooltip("Sélectionne ici le calque (Layer) de tes murs/obstacles")]
        public LayerMask obstacleLayer; 
    
        [Tooltip("La taille du rayon. Plus c'est grand, plus le trou est large")]
        public float sphereRadius = 0.5f;

        // Variables pour mémoriser l'objet qu'on est en train de cacher
        private Renderer currentObstacle;
        private Material currentMaterial;
        private Color originalColor;

        void Update()
        {
            // 1. Calculer la direction et la distance entre la caméra et le joueur
            Vector3 direction = playerTarget.position - transform.position;
            float distance = direction.magnitude;

            // 2. Lancer la sphère (SphereCast)
            if (Physics.SphereCast(transform.position, sphereRadius, direction, out RaycastHit hit, distance, obstacleLayer))
            {
                Renderer hitRenderer = hit.collider.GetComponent<Renderer>();
            
                // 3. Si on touche un NOUVEL obstacle
                if (hitRenderer != null && hitRenderer != currentObstacle)
                {
                    // On remet l'ancien obstacle à la normale avant de changer le nouveau
                    ResetObstacle(); 
                
                    // On sauvegarde les infos du nouvel obstacle
                    currentObstacle = hitRenderer;
                    currentMaterial = currentObstacle.material;
                    originalColor = currentMaterial.color;

                    // 4. On applique l'effet de transparence (ici, modification de l'Alpha)
                    Color transparentColor = originalColor;
                    transparentColor.a = 0.3f; // 30% d'opacité
                    currentMaterial.color = transparentColor;
                
                    // Note pour le Dither : Si tu utilises un Shader Graph avec Dither, 
                    // tu remplacerais la ligne ci-dessus par quelque chose comme :
                    // currentMaterial.SetFloat("_DitherFade", 0.3f);
                }
            }
            else
            {
                // 5. Si la sphère ne touche rien du tout, on s'assure que tout est normal
                ResetObstacle();
            }
        }

        // Fonction pour redonner au mur son apparence d'origine
        void ResetObstacle()
        {
            if (currentObstacle != null)
            {
                currentMaterial.color = originalColor; // Restaure la couleur
                // currentMaterial.SetFloat("_DitherFade", 1f); // (Version Dither)
                currentObstacle = null;
            }
        }
    }
}