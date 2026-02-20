using UnityEngine;

// ExecuteAlways permet au script de fonctionner même dans l'éditeur (sans appuyer sur Play)
// Très pratique pour ajuster la taille du trou visuellement !
namespace Movement
{
    [ExecuteAlways]
    public class PlayerShaderPosition : MonoBehaviour
    {
        private static readonly int PlayerPositionID = Shader.PropertyToID("_GlobalPlayerPosition");

        void Update()
        {
            // À chaque frame, on envoie la position (X, Y, Z) du joueur à tous les shaders du jeu
            // qui utilisent la variable "_GlobalPlayerPosition"
            Shader.SetGlobalVector(PlayerPositionID, transform.position);
        }
    }
}