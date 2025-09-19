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

    [Header("Audio")]
    [SerializeField] private AudioSource sfx;          // assign on the enemy (3D, spatial blend = 1)
    [SerializeField] private AudioClip[] swingClips;   // whoosh/swing options
    [SerializeField] private AudioClip[] hitClips;     // impact options
    [SerializeField] private float swingVolume = 1f;
    [SerializeField] private float hitVolume = 1f;
    [SerializeField] private float pitchJitter = 0.05f; // ±5% random pitch

    float attackTimer;

    void Awake()
    {
        if (!mover) mover = GetComponent<EnemyMovementBaseRB>();
    }

    protected override void Update()
    {
        base.Update();
        attackTimer += Time.deltaTime;

        if (!player || !playerInTrigger) return;

        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        if (distance <= attackRange)
        {
            playerDir = toPlayer;
            FaceTarget();

            if (attackTimer >= attackCooldown)
            {
                attackTimer = 0f;
                StartCoroutine(DoMeleeAttack());
            }
        }
    }

    IEnumerator DoMeleeAttack()
    {
        // start animation
        anim.SetTrigger("Attack");

        // find the clip length of "Melee Attack" (editor controllers only; safe if null)
        AnimationClip clip = null;
        if (anim.runtimeAnimatorController is AnimatorController controller)
        {
            foreach (ChildAnimatorState state in controller.layers[0].stateMachine.states)
            {
                if (state.state.name.Equals("Melee Attack"))
                {
                    clip = state.state.motion as AnimationClip;
                    break;
                }
            }
        }

        // play the swing immediately on wind-up
        PlayOneShotRandom(swingClips, swingVolume);

        // wait until mid animation to apply damage (same timing you had)
        float attackCheckTime = clip != null ? clip.length / 2.5f : 0f;
        yield return new WaitForSeconds(attackCheckTime);

        // do damage
        IDamage dmg = player.GetComponent<IDamage>();
        if (dmg != null)
        {
            dmg.TakeDamage(meleeDamage);
            // impact sound right when the hit lands
            PlayOneShotRandom(hitClips, hitVolume);
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

    // ---- audio helper ----
    void PlayOneShotRandom(AudioClip[] bank, float vol)
    {
        if (!sfx || bank == null || bank.Length == 0) return;

        var clip = bank[Random.Range(0, bank.Length)];
        float original = sfx.pitch;
        sfx.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        sfx.PlayOneShot(clip, vol);
        sfx.pitch = original;
    }
}
