using UnityEngine;

namespace Dialoue
{
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "Dialogue/Character")]
    public class CharacterData : ScriptableObject
    {
        public string characterName;
        public Color nameColor = Color.white;
    
        [Header("Paramètres Vocaux")]
        public AudioClip voiceSound;
        public float typingSpeed = 0.05f;
        [Range(0.1f, 2.0f)] public float basePitch = 1.0f;
        [Range(0.0f, 0.5f)] public float pitchVariation = 0.1f;
    }
}