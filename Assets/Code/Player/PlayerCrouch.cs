using UnityEngine;

class PlayerCrouch : MonoBehaviour
{
    [SerializeField] float crouchHeightPct;
    [SerializeField] float crouchSpeed;
    [SerializeField] CapsuleCollider col;

    float heightOriginal;

    void Awake()
    {
        heightOriginal = col.height;
    }

    public void Crouch()
    {
        if (Input.GetButton("Crouch"))
        {
            col.height = Mathf.MoveTowards(col.height, heightOriginal * crouchHeightPct, crouchSpeed * Time.deltaTime);
        }
        else
        {
            col.height = Mathf.MoveTowards(col.height, heightOriginal, crouchSpeed * Time.deltaTime);
        }
    }
}