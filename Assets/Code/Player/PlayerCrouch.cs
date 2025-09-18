using UnityEngine;

public class PlayerCrouch : MonoBehaviour
{
    [SerializeField] float crouchHeightPct;
    [SerializeField] float crouchSpeedMod;
    [SerializeField] float crouchDownSpeed;
    [SerializeField] CapsuleCollider col;
    [SerializeField] PlayerMovement movement;
    [SerializeField] AudioClip crouchSound;
    [SerializeField] AudioClip uncrouchSound;

    public bool isCrouching;

    float heightOriginal;
    int playedSound = 0;

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
            isCrouching = true;
            if (playedSound == 0 || playedSound == 2)
            {
                GameManager.instance.playerScript.audioSource.PlayOneShot(crouchSound);
                playedSound = 1;
            }
        }
        else
        {
            col.height = Mathf.MoveTowards(col.height, heightOriginal, crouchDownSpeed * Time.deltaTime);
            movement.speed = movement.speedOriginal;
            isCrouching = false;
            if (playedSound == 1)
            {
                GameManager.instance.playerScript.audioSource.PlayOneShot(uncrouchSound);
                playedSound = 2;
            }
        }
    }
}