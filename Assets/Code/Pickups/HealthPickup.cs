using UnityEngine;

public class HealthPickup : MonoBehaviour, IPickup
{
    [SerializeField] int healAmount;
    [SerializeField] AudioClip pickupSound;

    public void ApplyPickup()
    {
        GameManager.instance.playerScript.audioSource.PlayOneShot(pickupSound);
        GameManager.instance.playerScript.Heal(healAmount);
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyPickup();
        }
    }
}