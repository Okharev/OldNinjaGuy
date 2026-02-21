using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System; // AJOUT : Nécessaire pour utiliser 'Action'

public class DrunkenEffectManager : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Drag your Global Volume here")]
    public Volume globalVolume;
    [Tooltip("Drag the main camera with your custom CameraFollow script here")]
    public Movement.CameraFollow mainCameraFollow;

    [Header("Drunken Settings")]
    [Tooltip("The maximum level of drunkenness possible")]
    public float maxDrunkenness = 100f; 
    [Tooltip("How much drunkenness a single sake item adds")]
    public float sakeIntensity = 25f;
    [Tooltip("How much drunkenness the player loses per second")]
    public float recoveryRate = 5f;

    [Header("Sickness Animation")]
    [Tooltip("How fast the screen pulses in and out")]
    public float wobbleSpeed = 3f;
    [Tooltip("How extreme the pulsing effect is")]
    public float wobbleAmount = 0.2f;

    // Tracks the current level of drunkenness smoothly
    private float currentDrunkenness = 0f;
    
    // AJOUT : L'événement que le HUD va écouter
    public event Action<float> OnDrunkennessChanged;
    
    // Post-processing components
    private LensDistortion lensDistortion;
    private ChromaticAberration chromaticAberration;
    private MotionBlur motionBlur;

    void Start()
    {
        // Fetch all three effects from the volume profile
        if (globalVolume.profile.TryGet(out lensDistortion) && 
            globalVolume.profile.TryGet(out chromaticAberration) &&
            globalVolume.profile.TryGet(out motionBlur))
        {
            UpdateDrunkenVisuals();
        }
        else
        {
            Debug.LogWarning("Please add Lens Distortion, Chromatic Aberration, and Motion Blur to your Volume Profile!");
        }
    }

    void Update()
    {
        // Only process the fade-out if the player is currently drunk
        if (currentDrunkenness > 0f)
        {
            currentDrunkenness -= recoveryRate * Time.deltaTime;
            
            if (currentDrunkenness < 0f)
            {
                currentDrunkenness = 0f;
            }

            // Animate effects every frame (required for the Sine wave and fade out)
            UpdateDrunkenVisuals();
        }
    }

    [ContextMenu("Drink Sake (Test)")]
    public void DrinkSake()
    {
        currentDrunkenness += sakeIntensity;
        
        if (currentDrunkenness > maxDrunkenness)
        {
            currentDrunkenness = maxDrunkenness;
        }
        
        UpdateDrunkenVisuals();
        Debug.Log("Sake consumed! Current drunkenness: " + currentDrunkenness);
    }

    private void UpdateDrunkenVisuals()
    {
        // Calculate the percentage (0.0 to 1.0) for our effects
        float drunkPercentage = currentDrunkenness / maxDrunkenness;

        // AJOUT : On prévient le HUD que la valeur a changé et on lui donne le pourcentage
        OnDrunkennessChanged?.Invoke(drunkPercentage);

        // 1. Lens Distortion (Bulge + Nausea Breathing)
        if (lensDistortion != null)
        {
            float baseDistortion = Mathf.Lerp(0f, -0.6f, drunkPercentage);
            float sicknessPulse = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmount * drunkPercentage;
            lensDistortion.intensity.value = baseDistortion + sicknessPulse;
        }

        // 2. Chromatic Aberration (Color Splitting)
        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.value = Mathf.Lerp(0f, 1f, drunkPercentage);
        }

        // 3. Motion Blur (Ghosting / Duplicates)
        if (motionBlur != null)
        {
            motionBlur.intensity.value = Mathf.Lerp(0f, 1f, drunkPercentage);
        }

        // 4. Camera Wobble (Send data to the CameraFollow script)
        if (mainCameraFollow != null)
        {
            mainCameraFollow.drunkenWobbleIntensity = drunkPercentage;
        }
    }
}