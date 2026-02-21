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

        [Header("Drunken Camera Wobble (Nouveau !)")]
        [Tooltip("Contrôlé par le DrunkenEffectManager (0 = sobre, 1 = maximum)")]
        [Range(0f, 1f)] public float drunkenWobbleIntensity = 0f;
        
        [Tooltip("Vitesse de l'oscillation (titubement)")]
        public float wobbleSpeed = 1.2f;
        
        [Tooltip("Distance maximale de dérive de la caméra")]
        public float positionalWobbleAmount = 1.5f;
        
        [Tooltip("Angle maximal d'inclinaison de la caméra (roulis)")]
        public float rotationalWobbleAmount = 3.5f;

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
        
            cam.orthographic = true;
            transform.rotation = Quaternion.Euler(30f, 45f, 0f);
        
            if (target)
            {
                transform.position = target.position + offset;
            }
        }

        void LateUpdate()
        {
            if (!target) return;

            // Position de base idéale
            Vector3 desiredPosition = target.position + offset;

            // --- DÉBUT DE LA LOGIQUE D'IVRESSE ---
            if (drunkenWobbleIntensity > 0f)
            {
                // Le temps qui avance, multiplié par notre vitesse de titubement
                float time = Time.time * wobbleSpeed;

                // Le PerlinNoise retourne des valeurs entre 0 et 1.
                // On fait (valeur - 0.5) * 2 pour avoir un résultat entre -1 et 1 (gauche/droite, haut/bas).
                // On utilise des coordonnées différentes pour X, Z et Rot pour désynchroniser les mouvements.
                float noiseX = (Mathf.PerlinNoise(time, 0f) - 0.5f) * 2f;
                float noiseZ = (Mathf.PerlinNoise(0f, time) - 0.5f) * 2f;
                float noiseRot = (Mathf.PerlinNoise(time, time + 10f) - 0.5f) * 2f;

                // 1. Dérive de la position
                Vector3 wobbleOffset = new Vector3(noiseX, 0f, noiseZ) * positionalWobbleAmount * drunkenWobbleIntensity;
                desiredPosition += wobbleOffset;

                // 2. Inclinaison de la caméra (roulis sur l'axe Z)
                float currentRoll = noiseRot * rotationalWobbleAmount * drunkenWobbleIntensity;
                transform.rotation = Quaternion.Euler(30f, 45f, currentRoll);
            }
            else
            {
                // On s'assure que la caméra est parfaitement droite quand le joueur est sobre
                transform.rotation = Quaternion.Euler(30f, 45f, 0f);
            }
            // --- FIN DE LA LOGIQUE D'IVRESSE ---

            // Déplacement fluide vers la position calculée
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        }

        // --- DÉBUT DE LA LOGIQUE PIXEL PERFECT (URP) ---
        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != cam) return;

            smoothPosition = transform.position;

            float unitsPerPixel = (cam.orthographicSize * 2f) / Screen.height;
            float virtualPixelSize = unitsPerPixel * pixelSize;

            float dotRight = Vector3.Dot(smoothPosition, transform.right);
            float dotUp = Vector3.Dot(smoothPosition, transform.up);

            float snappedRight = Mathf.Round(dotRight / virtualPixelSize) * virtualPixelSize;
            float snappedUp = Mathf.Round(dotUp / virtualPixelSize) * virtualPixelSize;

            float deltaRight = snappedRight - dotRight;
            float deltaUp = snappedUp - dotUp;

            transform.position = smoothPosition + (transform.right * deltaRight) + (transform.up * deltaUp);

            if (postProcessMaterial != null)
            {
                float offsetX = deltaRight / virtualPixelSize;
                float offsetY = deltaUp / virtualPixelSize;
                postProcessMaterial.SetVector("_SubpixelOffset", new Vector4(offsetX, offsetY, 0, 0));
            }
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != cam) return;
            transform.position = smoothPosition;
        }
    }
}