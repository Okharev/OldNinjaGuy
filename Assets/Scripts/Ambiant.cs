using UnityEngine;

public class Ambiant : MonoBehaviour
{
    [SerializeField] private Collider Area;                       
    [SerializeField] private AudioSource Audio;

    void Awake()
    {
        Audio = GetComponent<AudioSource>();
        Area = GetComponent<Collider>(); 
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            
            Audio.Play();
        }
        
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            Audio.Stop();
        }
    }
}
