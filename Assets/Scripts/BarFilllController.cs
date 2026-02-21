using UnityEngine;
using UnityEngine.UIElements;

public class HealthBarController : MonoBehaviour
{
    public UIDocument uiDocument;
    private VisualElement fillMask;

    private void OnEnable()
    {
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;
        fillMask = root.Q<VisualElement>("FillMask");
        
        SetHealth(0.3f);
    }

    /// <summary>
    /// Met à jour le niveau de liquide dans la gourde.
    /// </summary>
    public void SetHealth(float healthPercentage)
    {
        if (fillMask == null) return;

        healthPercentage = Mathf.Clamp01(healthPercentage);
        
        // C'est ici que ça change ! On modifie la HAUTEUR (height) au lieu de la largeur.
        fillMask.style.height = new Length(healthPercentage * 100, LengthUnit.Percent);
    }
}