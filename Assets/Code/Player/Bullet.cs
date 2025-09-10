using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Vector3 targetPoint = Vector3.zero;
    public float bulletSpeed = -1;
    public float distance = -1;

    Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        if (bulletSpeed != -1)
        {
            if (targetPoint != Vector3.zero)
            {
                transform.LookAt(targetPoint);
                transform.position = Vector3.MoveTowards(transform.position, targetPoint, bulletSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, targetPoint) <= 0.05) Destroy(gameObject);
            }
            else if (distance != -1)
            {
                Vector3 targetPos = startPos + (transform.forward * distance);
                transform.position = Vector3.MoveTowards(transform.position, targetPos, bulletSpeed * Time.deltaTime);
                if (Vector3.Distance(transform.position, targetPos) <= 0.05) Destroy(gameObject);
            }
        }
    }
}
