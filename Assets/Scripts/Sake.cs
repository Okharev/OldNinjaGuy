
using UnityEngine;

public class Sake : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Find the manager and tell it to increase drunkenness
            FindAnyObjectByType<DrunkenEffectManager>().DrinkSake();
        
            // Destroy the sake bottle
            Destroy(gameObject); 
        }
    }
}
