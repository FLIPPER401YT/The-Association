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
    }

    protected override void Update()
    {
        base.Update();

        shootTimer += Time.deltaTime;
        if (!player) return;

        if (!playerInTrigger) return;
        if (!CanSeePlayer()) return;

        float d = Vector3.Distance(transform.position, player.position);

        
        if (mover)
        {
            if (d <= shootRange)
            {
                mover.SetTarget(null); // stop chasing
            }
            else
            {
                mover.SetTarget(player); // keep chasing until in range
                return; 
            }
        }

      
        playerDir = player.position - transform.position;
        FaceTarget();

        if (shootTimer >= shootCooldown)
        {
            shootTimer = 0f;
            Shoot();
        }
    }

    void Shoot()
    {
        if (!bullet || !shootPos || !player) return;

        Vector3 dir = (player.position - shootPos.position).normalized;

        Instantiate(bullet, shootPos.position, Quaternion.LookRotation(dir));
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!shootPos) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(shootPos.position, shootRange);
    }
#endif
}
