using UnityEngine;

class PlayerCrouch : MonoBehaviour
{
    [SerializeField] float crouchHeightPct;
    [SerializeField] float crouchSpeedMod;
    [SerializeField] float crouchDownSpeed;
    [SerializeField] CapsuleCollider col;
    [SerializeField] PlayerMovement movement;

    float heightOriginal;

    void Awake()
    {
        heightOriginal = col.height;
    }

    public void Crouch()
    {
        if (Input.GetButton("Crouch"))
        {
            col.height = Mathf.MoveTowards(col.height, heightOriginal * crouchHeightPct, crouchDownSpeed * Time.deltaTime);
            movement.speed = movement.speedOriginal * crouchSpeedMod;
        }
        else
        {
            col.height = Mathf.MoveTowards(col.height, heightOriginal, crouchDownSpeed * Time.deltaTime);
            movement.speed = movement.speedOriginal;
        }
    }
}