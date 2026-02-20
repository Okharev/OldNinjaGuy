using UnityEngine;
using UnityEngine.Rendering;

namespace Movement
{
    [RequireComponent(typeof(Camera))]
    public class PixelPerfectCameraSnapper : MonoBehaviour
    {
        [Header("Paramètres Pixel Art")]
        [Tooltip("Le Material qui utilise ton shader OutlinePixel")]
        public Material postProcessMaterial;
    
        [Tooltip("Doit correspondre à la valeur _PixelSize de ton Material")]
        public float pixelSize = 1f;

        private Camera cam;
        private Vector3 smoothPosition;

        private void OnEnable()
        {
            cam = GetComponent<Camera>();
        
            // On s'abonne aux événements de l'URP
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        private void OnDisable()
        {
            // On se désabonne quand le script est désactivé pour éviter les erreurs
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        }

        // Juste avant que la caméra ne dessine l'écran...
        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            // On s'assure qu'on agit uniquement sur CETTE caméra
            if (camera != cam) return;

            // 1. On sauvegarde la position réelle (fluide)
            smoothPosition = transform.position;

            // 2. On calcule la taille d'un pixel en unités du monde Unity
            float unitsPerPixel = (cam.orthographicSize * 2f) / Screen.height;
            float virtualPixelSize = unitsPerPixel * pixelSize;

            // 3. On calcule la position "claquée" (snapped) sur la grille
            float snapX = Mathf.Round(smoothPosition.x / virtualPixelSize) * virtualPixelSize;
            float snapY = Mathf.Round(smoothPosition.y / virtualPixelSize) * virtualPixelSize;
        
            Vector3 snappedPosition = new Vector3(snapX, snapY, smoothPosition.z);

            // 4. On déplace temporairement la caméra sur la position claquée
            transform.position = snappedPosition;

            // 5. On calcule l'erreur (la différence entre la position fluide et la position claquée)
            // On divise par virtualPixelSize pour obtenir une fraction (entre -0.5 et 0.5) pour le Shader
            float offsetX = (smoothPosition.x - snappedPosition.x) / virtualPixelSize;
            float offsetY = (smoothPosition.y - snappedPosition.y) / virtualPixelSize;

            // 6. On envoie cette erreur à notre Shader
            if (postProcessMaterial != null)
            {
                postProcessMaterial.SetVector("_SubpixelOffset", new Vector4(offsetX, offsetY, 0, 0));
            }
        }

        // Juste après que la caméra a fini de dessiner l'écran...
        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != cam) return;

            // On remet la caméra à sa position fluide pour ne pas perturber le reste du jeu
            transform.position = smoothPosition;
        }
    }
}