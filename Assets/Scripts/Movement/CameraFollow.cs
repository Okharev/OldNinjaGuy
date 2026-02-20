using UnityEngine;
using UnityEngine.Rendering; // Nécessaire pour les événements URP

namespace Movement
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Targeting")]
        [Tooltip("Drag your Player object here.")]
        public Transform target;
    
        [Tooltip("The mathematical offset to maintain the isometric angle.")]
        public Vector3 offset = new Vector3(-10f, 15f, -10f);

        [Header("Game Feel Smoothing")]
        [Tooltip("Lower is snappier, higher is more floaty/smooth.")]
        public float smoothTime = 0.15f;

        [Header("Pixel Perfect Setup")]
        [Tooltip("Le Material qui utilise ton shader OutlinePixel")]
        public Material postProcessMaterial;
        
        [Tooltip("Doit correspondre à la valeur _PixelSize de ton Material")]
        public float pixelSize = 1f;

        // Internal variables
        private Vector3 velocity = Vector3.zero;
        private Camera cam;
        private Vector3 smoothPosition;

        void Start()
        {
            cam = GetComponent<Camera>();
        
            // 1. Enforce True Isometric Rendering
            cam.orthographic = true;
        
            // 2. Enforce the exact isometric angle
            transform.rotation = Quaternion.Euler(30f, 45f, 0f);
        
            // 3. Snap to the target immediately when the scene starts
            if (target)
            {
                transform.position = target.position + offset;
            }
        }

        // We use LateUpdate for cameras to ensure the player has already finished moving this frame
        void LateUpdate()
        {
            if (!target) return;

            // Where the camera should ideally be right now
            Vector3 desiredPosition = target.position + offset;

            // Smoothly glide towards the desired position
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        }

        // --- DÉBUT DE LA LOGIQUE PIXEL PERFECT (URP) ---

        private void OnEnable()
        {
            // On s'abonne aux événements de rendu de l'URP quand le script est activé
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        private void OnDisable()
        {
            // On se désabonne pour éviter les fuites de mémoire
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != cam) return;

            // 1. On sauvegarde la position fluide calculée dans le LateUpdate
            smoothPosition = transform.position;

            // 2. On calcule la taille d'un pixel virtuel dans l'espace du monde
            float unitsPerPixel = (cam.orthographicSize * 2f) / Screen.height;
            float virtualPixelSize = unitsPerPixel * pixelSize;

            // 3. On calcule la position de la caméra alignée sur sa propre vue (View-Aligned)
            float dotRight = Vector3.Dot(smoothPosition, transform.right);
            float dotUp = Vector3.Dot(smoothPosition, transform.up);

            // 4. On claque ces valeurs sur notre grille de pixels virtuels
            float snappedRight = Mathf.Round(dotRight / virtualPixelSize) * virtualPixelSize;
            float snappedUp = Mathf.Round(dotUp / virtualPixelSize) * virtualPixelSize;

            // 5. On calcule la distance exacte de déplacement pour le claquage
            float deltaRight = snappedRight - dotRight;
            float deltaUp = snappedUp - dotUp;

            // 6. On déplace physiquement la caméra sur la position claquée
            transform.position = smoothPosition + (transform.right * deltaRight) + (transform.up * deltaUp);

            // 7. On envoie l'erreur de claquage au Shader pour corriger le mouvement
            if (postProcessMaterial != null)
            {
                // On convertit le décalage en fraction de pixel (entre -0.5 et 0.5)
                float offsetX = deltaRight / virtualPixelSize;
                float offsetY = deltaUp / virtualPixelSize;
                
                // Note : Selon la façon dont ton shader gère les UVs, il est parfois nécessaire d'inverser le signe (-offsetX, -offsetY)
                postProcessMaterial.SetVector("_SubpixelOffset", new Vector4(offsetX, offsetY, 0, 0));
            }
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != cam) return;

            // On restaure la position fluide pour que la logique de jeu (et le LateUpdate) reste intacte
            transform.position = smoothPosition;
        }
    }
}