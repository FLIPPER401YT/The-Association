using Unity.VisualScripting;
using UnityEngine;

public class Pickup : MonoBehaviour
{
    [SerializeField] AudioClip pickupSound;
    
    public virtual void ApplyPickup()
    {
        GameManager.instance.playerScript.audioSource.PlayOneShot(pickupSound);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyPickup();
        }
    }
}
