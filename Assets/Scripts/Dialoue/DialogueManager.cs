using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dialoue
{
    public class DialogueManager : MonoBehaviour
    {
        public static bool IsDialogueActive { get; private set; }
        public static event Action<bool> OnDialogueStateChanged;

        [SerializeField] private AudioClip clickSound;

        // Éléments UI existants
        private VisualElement container;
        private Label nameLabel, messageLabel;
        private Button clickZone;
        
        // NOUVEAU : Références pour le fond et le fondu
        private VisualElement backgroundArt;
        private VisualElement blackScreen;
        private bool isTransitioning; // Empêche de cliquer pendant un fondu au noir

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

            backgroundArt = root.Q<VisualElement>("BackgroundArt");
            blackScreen = root.Q<VisualElement>("BlackScreen");

            // --- CORRECTION ICI ---
            // On force UI Toolkit à ignorer totalement les clics sur ces deux éléments
            if (backgroundArt != null) backgroundArt.pickingMode = PickingMode.Ignore;
            if (blackScreen != null) blackScreen.pickingMode = PickingMode.Ignore;
            // ----------------------

            if (blackScreen != null) blackScreen.AddToClassList("black-screen--hidden");
        }
        
        public void StartDialogue(DialogueData data)
        {
            if (IsDialogueActive || isTransitioning) return;
            StartCoroutine(StartDialogueSequence(data));
        }

        private IEnumerator StartDialogueSequence(DialogueData data)
        {
            isTransitioning = true;
            IsDialogueActive = true;
            OnDialogueStateChanged?.Invoke(true);

            // 1. Fondu au noir EN DOUCEUR depuis le jeu
            blackScreen.RemoveFromClassList("black-screen--hidden");
            yield return new WaitForSeconds(1f); // On attend 1 seconde que l'écran soit 100% noir

            // 2. L'écran est noir ! On prépare l'interface en cachette
            currentDialogue = data;
            currentIndex = 0;

            if (data.backgroundArt != null)
            {
                backgroundArt.style.backgroundImage = new StyleBackground(data.backgroundArt);
                backgroundArt.AddToClassList("background-art--visible"); 
            }
            else
            {
                backgroundArt.style.backgroundImage = null;
            }

            container.AddToClassList("dialogue-container--visible");

            // L'ASTUCE ANTI-FLASH : On attend 2 frames pour laisser à UI Toolkit le temps de charger l'image
            yield return null; 
            yield return null; 

            // 3. Fondu d'ouverture (l'écran noir disparaît pour révéler le dialogue prêt et chargé)
            blackScreen.AddToClassList("black-screen--hidden");
            yield return new WaitForSeconds(1f); 

            // 4. On lance le texte !
            isTransitioning = false;
            DisplayLine();
        }
        
        private void OnClickZonePressed()
        {
            if (!IsDialogueActive || isTransitioning) return; 
    
            if (clickSound) audioSource.PlayOneShot(clickSound);

            if (isTyping)
            {
                CompleteLineInstantly();
            }
            else
            {
                // On vérifie si nous sommes à la dernière ligne ou non
                if (currentIndex < currentDialogue.lines.Length - 1)
                {
                    // Ce n'est pas la dernière ligne : on déclenche l'événement et on passe à la suite
                    TriggerCurrentLineEvents();
                    currentIndex++;
                    DisplayLine();
                }
                else
                {
                    // C'est la dernière ligne ! On ne déclenche PAS les événements ici.
                    // On lance la fin du dialogue, qui s'occupera de déclencher les événements.
                    EndDialogue(); 
                }
            }
        }

        private void TriggerCurrentLineEvents()
        {
            var line = currentDialogue.lines[currentIndex];
            if (line.eventIDs != null)
            {
                foreach (string id in line.eventIDs) DialogueEventReceiver.SendEvent(id);
            }
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
            if (isTransitioning) return;
            StartCoroutine(EndDialogueSequence());
        }

        private IEnumerator EndDialogueSequence()
        {
            isTransitioning = true;

            // 1. Fondu au noir depuis le dialogue
            blackScreen.RemoveFromClassList("black-screen--hidden");
            yield return new WaitForSeconds(1f); 

            // 2. L'écran est noir ! On cache toute l'interface incognito
            IsDialogueActive = false;
            OnDialogueStateChanged?.Invoke(false);
            container.RemoveFromClassList("dialogue-container--visible");
            backgroundArt.RemoveFromClassList("background-art--visible"); 
            backgroundArt.style.backgroundImage = null; // On nettoie l'image
    
            // L'ASTUCE ANTI-FLASH : On attend 2 frames pour laisser UI Toolkit tout désactiver
            yield return null;
            yield return null;
            yield return null;

            // 3. L'écran noir disparaît pour révéler le jeu en fondu
            blackScreen.AddToClassList("black-screen--hidden");
            yield return new WaitForSeconds(1f); 
    
            // 4. Le petit délai avant l'événement (comme on l'a fait précédemment)
            float extraDelayBeforeEvent = 0.5f; 
            yield return new WaitForSeconds(extraDelayBeforeEvent);

            // 5. On déclenche l'événement
            TriggerCurrentLineEvents();
    
            isTransitioning = false;
        }
    }
}