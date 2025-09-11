using UnityEngine;

public class EnemyRangeAI : EnemyAI_Base
{
    [Header("Shooting")]
    [SerializeField] GameObject bullet;
    [SerializeField] Transform shootPos;
    [SerializeField] float shootCooldown;
    [SerializeField] float shootRange;

    float shootTimer;

    void Awake()
    {
        if (!mover) mover = GetComponent<EnemyMovementBaseRB>();
        if (!shootPos) shootPos = transform; // safe fallback
    }

    protected override void Update()
    {
        base.Update();

        shootTimer += Time.deltaTime;
        if (!player) return;

        // vision gate
        if (!CanSeePlayer()) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // approach until in range, then stop
        if (mover)
        {
            if (dist > shootRange)
            {
                mover.SetTarget(player);
                return; // not in range yet
            }
            mover.SetTarget(null); // in range => stop to shoot
        }

        // face target
        playerDir = player.position - transform.position;
        FaceTarget();

        // fire on cooldown
        if (shootTimer >= shootCooldown)
        {
            shootTimer = 0f;
            Shoot();
        }
    }

    void Shoot()
    {
        if (!bullet || !shootPos || !player) return;

        Vector3 aim = GetPlayerAimPoint();                         // collider center
        Vector3 dir = (aim - shootPos.position).normalized;

#if UNITY_EDITOR
        Debug.DrawLine(shootPos.position, aim, Color.red, 0.25f);
#endif

        Instantiate(bullet, shootPos.position, Quaternion.LookRotation(dir));
    }

    // Prefer the player's collider center, fallback to a reasonable upward offset.
    Vector3 GetPlayerAimPoint()
    {
        if (player && player.TryGetComponent<Collider>(out var col))
        {
            // Slight downward nudge in case the collider is very tall
            return col.bounds.center + Vector3.down * 0.1f;
        }
        // Fallback if no collider found (tweak 0.8–1.2 based on your character rig)
        return player ? player.position + Vector3.up * 0.9f : transform.position + transform.forward;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, shootRange);
    }
#endif
}
