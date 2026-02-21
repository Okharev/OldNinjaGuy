using UnityEngine;
using UnityEngine.Events;

namespace Dialoue
{
    [System.Serializable]
    public struct DialogueLine
    {
        public CharacterData character;
        [TextArea(3, 10)] public string text;
        public string[] eventIDs;
        public UnityEvent onLineComplete;
    }

    [CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/System")]
    public class DialogueData : ScriptableObject
    {
        [Header("Mise en scène")]
        public Texture2D backgroundArt; // L'image de fond pour ce dialogue complet

        [Header("Lignes")]
        public DialogueLine[] lines;
    }
}