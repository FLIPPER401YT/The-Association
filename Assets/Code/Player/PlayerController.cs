using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float speed;
    [SerializeField] float jumpHeight;
    [SerializeField] int jumpMax;

    Rigidbody rb;
    Vector3 moveDir;
    int jumps = 0;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Movement();
    }

    void Update()
    {
        Jump();
    }

    void Jump()
    {
        if (Input.GetButtonDown("Jump") && jumps < jumpMax)
        {
            rb.AddForce(transform.up * jumpHeight, ForceMode.Impulse);
            jumps++;
        }
    }

    void Movement()
    {
        moveDir = ((transform.forward * Input.GetAxis("Vertical")) + (transform.right * Input.GetAxis("Horizontal"))) * speed * Time.fixedDeltaTime;
        rb.linearVelocity = moveDir + new Vector3(0, rb.linearVelocity.y, 0);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            jumps = 0;
        }
        Debug.Log(collision.gameObject.layer);
    }
}
