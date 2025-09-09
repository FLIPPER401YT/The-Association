using UnityEngine;

class PlayerMovement : MonoBehaviour
{
    [SerializeField] public float speed;
    [SerializeField] Rigidbody rb;

    public Vector3 moveDir;
    public float speedOriginal;


    void Start()
    {
        speedOriginal = speed;
    }

    public void Movement()
    {
        moveDir = ((transform.forward * Input.GetAxis("Vertical")) + (transform.right * Input.GetAxis("Horizontal"))) * speed * Time.fixedDeltaTime;
        rb.linearVelocity = moveDir + new Vector3(0, rb.linearVelocity.y, 0);
    }
}