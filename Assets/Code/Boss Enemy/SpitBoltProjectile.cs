using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class SpitBoltProjectile : MonoBehaviour
{
    public float damage;
    public float lifetime;
    public LayerMask hitMask = ~0;

    [Header("Homing")]
    public Transform player;                // optional tiny mid-flight aim assist
    public float gentleHomeStrength;   // set 0 to disable

    [Header("Ownership")]
    public Transform owner;                 // set by spawner; ignored on hit

    Rigidbody rb;
    float t;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        rb.useGravity = false;
    }

    void Update()
    {
        t += Time.deltaTime;
        if (t >= lifetime) Destroy(gameObject);
    }

    void FixedUpdate()
    {
        // light corrective homing toward player 
        if (player && gentleHomeStrength > 0f && rb.linearVelocity.sqrMagnitude > 0.0001f)
        {
            Vector3 desiredDir = (player.position + Vector3.up * 1.2f - transform.position).normalized;
            Vector3 newVel = Vector3.Lerp(rb.linearVelocity, desiredDir * rb.linearVelocity.magnitude, Time.fixedDeltaTime * gentleHomeStrength);
            rb.linearVelocity = newVel;
            rb.MoveRotation(Quaternion.LookRotation(newVel.normalized, Vector3.up));
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Respect layer mask
        if (((1 << other.gameObject.layer) & hitMask) == 0) return;

        // Ignore owner (and its children)
        if (owner && (other.transform == owner || other.transform.IsChildOf(owner))) return;

        // Apply damage via IDamage 
        var dmg = other.GetComponentInParent<IDamage>() ?? other.GetComponentInChildren<IDamage>();
        if (dmg != null)
        {
            dmg.TakeDamage((int)damage);
        }

        // TODO: spawn impact VFX/SFX here if desired
        Destroy(gameObject);
    }
}
