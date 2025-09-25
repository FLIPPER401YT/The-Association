using UnityEngine;

class PlayerMovement : MonoBehaviour
{
    [SerializeField] public float speed;
    [SerializeField] float timeBetweenSteps;
    [SerializeField] Rigidbody rb;
    [SerializeField] AudioClip walkStepSound;
    [SerializeField] AudioClip crouchStepSound;

    public float speedOriginal;
    public Vector3 moveDir;

    float stepTimer = 0;

    void Start()
    {
        speedOriginal = speed;
    }

    public void Movement()
    {
        moveDir = ((transform.forward * Input.GetAxis("Vertical")) + (transform.right * Input.GetAxis("Horizontal"))) * speed * Time.fixedDeltaTime;
        rb.linearVelocity = moveDir + new Vector3(0, rb.linearVelocity.y, 0);

        stepTimer += Time.deltaTime;
        if (GameManager.instance.playerScript.dash.dashing) stepTimer = 0;
        if (moveDir != Vector3.zero && stepTimer >= timeBetweenSteps && GameManager.instance.playerScript.jump.Grounded)
        {
            if (GameManager.instance.playerScript.crouch.isCrouching)
            {
                GameManager.instance.playerScript.walkingAudioSource.clip = crouchStepSound;
                GameManager.instance.playerScript.walkingAudioSource.Play();
            }
            else
            {
                GameManager.instance.playerScript.walkingAudioSource.clip = walkStepSound;
                GameManager.instance.playerScript.walkingAudioSource.Play();
            }
            stepTimer = 0;
        }
        else if (moveDir == Vector3.zero || !GameManager.instance.playerScript.jump.Grounded)
        {
            GameManager.instance.playerScript.walkingAudioSource.Stop();
        }
    }
}