using UnityEngine;

public class AmmoPickup : MonoBehaviour, IPickup
{
    [SerializeField] int ammoAmount;
    [SerializeField] AudioClip pickupSound;

    public void ApplyPickup()
    {
        GameManager.instance.playerScript.audioSource.PlayOneShot(pickupSound);
        GameManager.instance.playerScript.shoot.AddAmmo(ammoAmount);
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