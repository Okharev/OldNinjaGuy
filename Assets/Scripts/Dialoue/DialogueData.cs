using UnityEngine;
using UnityEngine.Events;

namespace Dialoue
{
    [System.Serializable]
    public struct DialogueLine
    {
        public CharacterData character;
        [TextArea(3, 10)] public string text;
        public string[] eventIDs; // ID pour spawner/porte
        public UnityEvent onLineComplete; // Événements simples Unity
    }

    [CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/System")]
    public class DialogueData : ScriptableObject
    {
        public DialogueLine[] lines;
    }
}