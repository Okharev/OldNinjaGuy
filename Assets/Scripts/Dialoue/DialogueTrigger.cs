using UnityEngine;

namespace Dialoue
{
    public class DialogueTrigger : MonoBehaviour
    {
        public DialogueData dialogueToTrigger;
        public DialogueManager manager; // Ce nom doit correspondre au type de classe ci-dessus

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // C'est ici que l'erreur se produit si StartDialogue n'est pas public
                manager.StartDialogue(dialogueToTrigger);
            
                // On désactive le trigger pour ne pas le relancer en boucle
                gameObject.SetActive(false);
            }
        }
    }
}