using UnityEngine;

public class SeeThroughController : MonoBehaviour
{
    public Transform playerTransform;
    public LayerMask transparentWallLayer;
    public float castRadius = 0.5f;
    public float fadeSpeed = 5f;

    private Renderer lastHitRenderer;
    private float currentVisibility = 0f;

    void Update()
    {
        if (playerTransform == null) return;

        Shader.SetGlobalVector("_GlobalPlayerPos", playerTransform.position);

        Vector3 directionToPlayer = playerTransform.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;
        Ray castRay = new Ray(transform.position, directionToPlayer.normalized);

        // NOUVEAU : On récupère les infos de ce qu'on touche (hitInfo)
        RaycastHit hitInfo;
        bool isHitting = Physics.SphereCast(castRay, castRadius, out hitInfo, distanceToPlayer, transparentWallLayer);

        if (isHitting)
        {
            // On récupère le Renderer de l'objet spécifique qu'on a touché
            Renderer currentRenderer = hitInfo.collider.GetComponent<Renderer>();
            
            if (currentRenderer != null)
            {
                // Si on change d'objet, on réinitialise l'ancien
                if (lastHitRenderer != null && lastHitRenderer != currentRenderer)
                    lastHitRenderer.material.SetFloat("_HoleVisibilityMask", 0f);

                lastHitRenderer = currentRenderer;
                currentVisibility = Mathf.Lerp(currentVisibility, 1f, Time.deltaTime * fadeSpeed);
                
                lastHitRenderer.material.SetFloat("_HoleVisibilityMask", currentVisibility);
            }
        }
        else if (lastHitRenderer != null)
        {
            // On ferme le trou doucement quand on ne touche plus rien
            currentVisibility = Mathf.Lerp(currentVisibility, 0f, Time.deltaTime * fadeSpeed);
            lastHitRenderer.material.SetFloat("_HoleVisibilityMask", currentVisibility);
            
            if (currentVisibility <= 0.01f)
            {
                lastHitRenderer.material.SetFloat("_HoleVisibilityMask", 0f);
                lastHitRenderer = null;
            }
        }
    }
}