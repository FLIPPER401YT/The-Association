using UnityEngine;

public class HealthPickup : Pickup
{
    [SerializeField] int healAmount;

    public override void ApplyPickup()
    {
        base.ApplyPickup();
        GameManager.instance.playerScript.Heal(healAmount);
        Destroy(gameObject);
    }
}