using UnityEngine;

class PlayerDash : MonoBehaviour
{
    [SerializeField] int dashMax;
    [SerializeField] float dashDistance;
    [SerializeField] float dashReloadTime;
    [SerializeField] float dashTimeLength;
    [SerializeField] Rigidbody rb;
    [SerializeField] PlayerMovement movement;

    int dashes = 0;
    float dashTimer = 0;
    float dashingTimer = 0;
    bool dashing = false;

    public void Dash()
    {
        Debug.Log(Input.GetButtonDown("Dash"));
        if (Input.GetButtonDown("Dash") && dashes < dashMax)
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
            rb.AddForce((movement.moveDir != Vector3.zero ? movement.moveDir.normalized : transform.forward) * dashDistance * Time.fixedDeltaTime, ForceMode.Impulse);
            if (dashingTimer >= dashTimeLength)
            {
                dashing = false;
                dashingTimer = 0;
            }
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