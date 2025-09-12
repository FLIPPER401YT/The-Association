using UnityEngine;

public class AmmoPickup : Pickup
{
    [SerializeField] int ammoAmount;

    public override void ApplyPickup()
    {
        base.ApplyPickup();
        GameManager.instance.playerScript.shoot.AddAmmo(ammoAmount);
        Destroy(gameObject);
    }
}