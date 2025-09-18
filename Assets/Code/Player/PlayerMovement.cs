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
        if (moveDir != Vector3.zero && stepTimer >= timeBetweenSteps)
        {
            if (GameManager.instance.playerScript.crouch.isCrouching)
            {
                GameManager.instance.playerScript.audioSource.PlayOneShot(crouchStepSound);
            }
            else
            {
                GameManager.instance.playerScript.audioSource.PlayOneShot(walkStepSound);
            }
            stepTimer = 0;
        }
    }
}