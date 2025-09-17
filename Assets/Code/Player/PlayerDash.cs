using UnityEngine;

class PlayerDash : MonoBehaviour
{
    [SerializeField] int dashMax;
    [SerializeField] float dashDistance;
    [SerializeField] float dashReloadTime;
    [SerializeField] float dashTimeLength;
    [SerializeField] float fovChangeSpeed;
    [SerializeField] Rigidbody rb;
    [SerializeField] PlayerMovement movement;

    int dashes = 0;
    float dashTimer = 0;
    float dashingTimer = 0;
    float camFovOriginal;
    bool dashing = false;

    void Start()
    {
        camFovOriginal = Camera.main.fieldOfView;
    }

    public void Dash()
    {
        if (!dashing && Input.GetButtonDown("Dash") && dashes < dashMax)
        {
            Debug.Log("Dash");
            dashing = true;
            dashingTimer = 0;
            //rb.AddForce((movement.moveDir != Vector3.zero ? movement.moveDir.normalized : transform.forward) * dashDistance * Time.deltaTime, ForceMode.Impulse);
            dashes++;
        }
    }

    void FixedUpdate()
    {
        if (dashing)
        {
            dashingTimer += Time.fixedDeltaTime;
            Camera.main.fieldOfView = Mathf.MoveTowards(Camera.main.fieldOfView, camFovOriginal + 30, fovChangeSpeed * Time.deltaTime);
            rb.AddForce((movement.moveDir != Vector3.zero ? movement.moveDir.normalized : transform.forward) * dashDistance * Time.fixedDeltaTime, ForceMode.Impulse);
            if (dashingTimer >= dashTimeLength)
            {
                dashing = false;
                dashingTimer = 0;
            }
        }
        else
        {
            Camera.main.fieldOfView = Mathf.MoveTowards(Camera.main.fieldOfView, camFovOriginal, fovChangeSpeed * Time.deltaTime);
        }
    }

    void Update()
    {
        if (dashes > 0)
        {
            dashTimer += Time.deltaTime;
            if (dashTimer >= dashReloadTime)
            {
                dashes--;
                dashTimer = 0;
            }
        }
        else dashTimer = 0;
    }
}