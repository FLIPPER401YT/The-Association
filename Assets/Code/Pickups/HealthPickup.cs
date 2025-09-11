using UnityEngine;

public class HealthPickup : MonoBehaviour, IPickup
{
    [SerializeField] int healAmount;

    public void ApplyPickup()
    {
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