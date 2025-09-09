using UnityEngine;

class PlayerJump : MonoBehaviour
{
    [SerializeField] float jumpHeight;
    [SerializeField] int jumpMax;
    int jumps = 0;
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Jump()
    {
        if (Input.GetButtonDown("Jump") && jumps < jumpMax)
        {
            rb.AddForce(transform.up * jumpHeight, ForceMode.Impulse);
            jumps++;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            jumps = 0;
        }
    }
}