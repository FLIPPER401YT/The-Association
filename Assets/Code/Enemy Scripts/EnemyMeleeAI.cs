using System.Collections;
using UnityEditor.Animations;
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
        base.Update();
        attackTimer += Time.deltaTime;

        if (!player) return;

        if (!playerInTrigger) return;

        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        if(distance <= attackRange)
        {
            playerDir = toPlayer;
            FaceTarget();

            if(attackTimer >= attackCooldown)
            {
                attackTimer = 0f;
                StartCoroutine(DoMeleeAttack());
            }
        } 
        
    }

    IEnumerator DoMeleeAttack()
    {
        anim.SetTrigger("Attack");
        AnimationClip clip = null;
        if (anim.runtimeAnimatorController is AnimatorController controller)
        {
            foreach (ChildAnimatorState state in controller.layers[0].stateMachine.states)
                if (state.state.name.Equals("Melee Attack")) clip = state.state.motion as AnimationClip;
        }
        float attackCheckTime = clip != null ? clip.length / 2.5f : 0;

        yield return new WaitForSeconds(attackCheckTime);

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
