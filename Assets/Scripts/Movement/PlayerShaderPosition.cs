using UnityEngine;

// ExecuteAlways permet au script de fonctionner même dans l'éditeur (sans appuyer sur Play)
// Très pratique pour ajuster la taille du trou visuellement !
namespace Movement
{
    [ExecuteAlways]
    public class PlayerShaderPosition : MonoBehaviour
    {
        // private static readonly int PlayerPosId = Shader.PropertyToID("_GlobalPlayerPos");
        //
        // void Update()
        // {
        //     // Broadcast the player's world position to all shaders in the project
        //     Shader.SetGlobalVector(PlayerPosId, transform.position);
        // }
    }
}