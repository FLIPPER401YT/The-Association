using UnityEngine;

public class AmmoPickup : MonoBehaviour, IPickup
{
    [SerializeField] int ammoAmount;

    public void ApplyPickup()
    {
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