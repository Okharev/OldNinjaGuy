using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Dialoue
{
    public class DialogueEventReceiver : MonoBehaviour
    {
        // On utilise une liste car plusieurs objets peuvent avoir le même ID (ex: 2 portes qui s'ouvrent)
        private static Dictionary<string, List<DialogueEventReceiver>> registry = new();

        public string eventID;
        public UnityEvent onTrigger;

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(eventID)) return;
            if (!registry.ContainsKey(eventID)) registry[eventID] = new List<DialogueEventReceiver>();
            registry[eventID].Add(this);
        }

        private void OnDisable()
        {
            if (registry.ContainsKey(eventID)) registry[eventID].Remove(this);
        }

        public static void SendEvent(string id)
        {
            if (registry.TryGetValue(id, out var receivers))
            {
                foreach (var r in receivers) r.onTrigger?.Invoke();
            }
        }
        
        public void Trigger() => onTrigger?.Invoke();
    }
}