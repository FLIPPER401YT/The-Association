using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [SerializeField] float speed;     // units per second
    [SerializeField] float lifetime;   // auto-destroy after seconds
    [SerializeField] int damage;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // move forward each frame
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        // check if we hit player
        if (other.CompareTag("Player"))
        {
            other.GetComponent<IDamage>()?.TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (!other.CompareTag("Enemy")) // don’t blow up on other enemies
        {
            Destroy(gameObject);
        }
    }
}
