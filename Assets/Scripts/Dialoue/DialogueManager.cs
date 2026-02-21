using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

// Assurez-vous que le namespace est correct

namespace Dialoue
{
    public class DialogueManager : MonoBehaviour
    {
        public static bool IsDialogueActive { get; private set; }
        public static event Action<bool> OnDialogueStateChanged;

        [SerializeField] private AudioClip clickSound;

        private VisualElement container;
        private Label nameLabel, messageLabel;
        private Button clickZone;
        private AudioSource audioSource;
    
        private DialogueData currentDialogue;
        private int currentIndex;
        private Coroutine typingCoroutine;
        private bool isTyping;
        private string fullText;

        void OnEnable()
        {
            audioSource = GetComponent<AudioSource>();
            var root = GetComponent<UIDocument>().rootVisualElement;
            container = root.Q<VisualElement>("DialogueContainer");
            nameLabel = root.Q<Label>("CharacterName");
            messageLabel = root.Q<Label>("DialogueText");
            clickZone = root.Q<Button>("ClickZone");
            clickZone.clicked += OnClickZonePressed;
        }

        public void StartDialogue(DialogueData data)
        {
            IsDialogueActive = true;
            OnDialogueStateChanged?.Invoke(true);
            currentDialogue = data;
            currentIndex = 0;
            container.AddToClassList("dialogue-container--visible");
            DisplayLine();
        }

        private void OnClickZonePressed()
        {
            if (!IsDialogueActive) return;
            if (clickSound) audioSource.PlayOneShot(clickSound);

            if (isTyping)
            {
                CompleteLineInstantly();
            }
            else
            {
                // --- NOUVEAU : On déclenche les événements de la ligne que l'on vient de FINIR ---
                TriggerCurrentLineEvents();
            
                currentIndex++;
            
                if (currentIndex < currentDialogue.lines.Length)
                    DisplayLine();
                else
                    EndDialogue();
            }
        }

        private void TriggerCurrentLineEvents()
        {
            var line = currentDialogue.lines[currentIndex];
        
            // Déclenchement multiple par IDs
            if (line.eventIDs != null)
            {
                foreach (string id in line.eventIDs)
                {
                    DialogueEventReceiver.SendEvent(id);
                }
            }
        
            // Déclenchement de l'UnityEvent classique
            line.onLineComplete?.Invoke();
        }

        void DisplayLine()
        {
            var line = currentDialogue.lines[currentIndex];
            nameLabel.text = line.character.characterName;
            nameLabel.style.color = line.character.nameColor;

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(line.text, line.character));
        }

        // --- RETOUR DU SON TYPEWRITER ---
        IEnumerator TypeText(string textToType, CharacterData data)
        {
            fullText = textToType;
            messageLabel.text = "";
            isTyping = true;

            foreach (char letter in textToType.ToCharArray())
            {
                messageLabel.text += letter;
            
                if (data.voiceSound && !char.IsWhiteSpace(letter))
                {
                    // Variation de pitch pour l'effet Animal Crossing
                    audioSource.pitch = data.basePitch + UnityEngine.Random.Range(-data.pitchVariation, data.pitchVariation);
                    audioSource.PlayOneShot(data.voiceSound);
                }
                yield return new WaitForSeconds(data.typingSpeed);
            }
            isTyping = false;
        }

        void CompleteLineInstantly()
        {
            StopCoroutine(typingCoroutine);
            messageLabel.text = fullText;
            isTyping = false;
        }

        void EndDialogue()
        {
            IsDialogueActive = false;
            OnDialogueStateChanged?.Invoke(false);
            container.RemoveFromClassList("dialogue-container--visible");
        }
    }
}