using UnityEngine;

public class EnemyMeleeAI : EnemyAI_Base
{
    [Header("Melee")]
    [SerializeField] GameObject weapon;
    [SerializeField] Transform attackPos;
    [SerializeField] float attackRange;
    [SerializeField] float attackCooldown;
    [SerializeField] int meleeDamage;

    float attackTimer;

    
    void Awake()
    {
        if(!mover) mover = GetComponent<EnemyMovementBaseRB>();
        
    }

    // Update is called once per frame
    protected override void Update()
    {

        
    }

    void DoMeleeAttack()
    {
        IDamage dmg = player.GetComponent<IDamage>();
        if (dmg != null)
        {
            dmg.TakeDamage(meleeDamage);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (attackPos == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPos.position, attackRange);
    }
#endif
}
