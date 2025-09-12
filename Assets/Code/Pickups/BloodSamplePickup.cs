using UnityEngine;

public class BloodSamplePickup : Pickup
{
    [SerializeField] int amount;

    public override void ApplyPickup()
    {
        base.ApplyPickup();
        GameManager.instance.playerScript.PickupBloodSample(amount);
        Destroy(gameObject);
    }
}
