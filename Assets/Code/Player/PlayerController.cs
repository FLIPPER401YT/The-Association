using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float speed;
    [SerializeField] PlayerJump jump;
    [SerializeField] PlayerCrouch crouch;
    [SerializeField] Rigidbody rb;

    Vector3 moveDir;

    void FixedUpdate()
    {
        Movement();
    }

    void Update()
    {
        jump.Jump();
        crouch.Crouch();
    }

    void Movement()
    {
        moveDir = ((transform.forward * Input.GetAxis("Vertical")) + (transform.right * Input.GetAxis("Horizontal"))) * speed * Time.fixedDeltaTime;
        rb.linearVelocity = moveDir + new Vector3(0, rb.linearVelocity.y, 0);
    }

    
}
