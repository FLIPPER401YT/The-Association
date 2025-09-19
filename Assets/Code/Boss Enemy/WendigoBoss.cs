using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class WendigoBoss : MonoBehaviour, IDamage
{
    #region Wendigo Statistics
    [Header("References")]
    [SerializeField] Transform player;
    [SerializeField] Rigidbody rigidBody;
    [SerializeField] Animator anim;
    [SerializeField] AnimationClip deathAnimation;

    [Header("Health")]
    [SerializeField] int healthMax;
    [SerializeField] int healthCurrent;

    [Header("Perception")]
    [SerializeField] float aggroRange;
    [SerializeField] float leashRange;

    [Header("Movement")]
    [SerializeField] float maxSpeed;
    [SerializeField] float chaseSpeed;
    [SerializeField] float maxAccel;
    [SerializeField] float turnLerp;

    [Header("Avoidance")]
    [SerializeField] LayerMask obstacleMask = ~0;
    [SerializeField] float avoidStrength;
    [SerializeField] float lookAhead;
    [SerializeField] float whiskerAngle;
    [SerializeField] float whiskerLen;
    [SerializeField] float avoidRadius;

    [Header("Action Radius")]
    [SerializeField] bool drawGizmos = true;

    [Header("Range")]
    [SerializeField] float swipeRange;
    [SerializeField] float boltMinRange;
    [SerializeField] float boltMaxRange;
    [SerializeField] float rushRange;
    [SerializeField] float dashRange;

    [Header("Summons")]
    [SerializeField] GameObject[] minions;
    [SerializeField] float summonWindup;
    [SerializeField] float summonRecover;
    [SerializeField] float minionCooldown;

    [Header("Swipe")]
    [SerializeField] float swipeDamage;
    [SerializeField] float swipeWindup;
    [SerializeField] float swipeRecover;
    [SerializeField] float swipeRadius;
    [SerializeField] float swipeCooldown;
    [SerializeField] Vector3 swipeOffset;
    [SerializeField] Transform attackPosition;

    [Header("Fang Shot")]
    [SerializeField] float boltSpeed;
    [SerializeField] float boltDamage;
    [SerializeField] float boltWindup;
    [SerializeField] float boltRecover;
    [SerializeField] float boltCooldown;
    [SerializeField] GameObject boltPrefab;
    [SerializeField] Transform castMuzzle;

    [Header("Rush")]
    [SerializeField] float rushSpeed;
    [SerializeField] float rushTime;
    [SerializeField] float rushDamage;
    [SerializeField] float rushWindup;
    [SerializeField] float rushRecover;
    [SerializeField] float rushCooldown;
    [SerializeField] float rushShoulderRadius;
    [SerializeField] float rushShoulderLength;

    [Header("Evade")]
    [SerializeField] float dashSpeed;
    [SerializeField] float dashTime;
    [SerializeField] float dashWindup;
    [SerializeField] float dashRecover;
    [SerializeField] float dashCooldown;

    [Header("Hit Masks & Tags")]
    [SerializeField] LayerMask rushHitMask = ~0;
    [SerializeField] Collider bodyCollider;
    [SerializeField] string playerTag = "Player";

    [Header("Separation")]
    [SerializeField] float personalSpace;
    [SerializeField] float separationSpeed;
    [SerializeField] bool keepSpace = true;

    public enum WendigoState { Chase, Melee, Range, Evade, Rush, Summon, Dead };
    WendigoState state;
    Vector3 spawn; float attackLockout;
    float swipeCD, boltCD, rushCD, summonCD, dashCD;
    bool rushHit;
    bool MeleeEnabled => swipeDamage > 0f && swipeRadius > 0f;
    bool RangeEnabled => boltDamage > 0f && boltMinRange > 0f && boltMaxRange > 0f;
    bool RushEnabled => rushSpeed > 0f && rushTime > 0f && rushDamage > 0f;
    bool SummonEnabled;
    bool EvadeEnabled;
    #endregion
    #region Awake and Update
    void Awake()
    {
        if (!rigidBody) rigidBody = GetComponent<Rigidbody>();
        if (!anim) anim = GetComponent<Animator>();
        if (!bodyCollider) bodyCollider = GetComponent<Collider>();
        rigidBody.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        healthCurrent = Mathf.Clamp(healthCurrent, 1, healthMax);
        spawn = transform.position;
        swipeCD = boltCD = rushCD = dashCD = summonCD = 0f;
        SummonEnabled = minions != null && minions.Length > 0;
        EvadeEnabled = dashCooldown > 0f && dashTime > 0f;

    }
    void Start()
    {
        player = GameManager.instance.player.transform;
        state = WendigoState.Chase;
    }
    void FixedUpdate()
    {
        if (state == WendigoState.Dead) return;
        attackLockout -= Time.fixedDeltaTime; swipeCD -= Time.fixedDeltaTime;
        boltCD -= Time.fixedDeltaTime; rushCD -= Time.fixedDeltaTime;
        dashCD -= Time.fixedDeltaTime; summonCD -= Time.fixedDeltaTime;
        float playerDistance = player ? Vector3.Distance(transform.position, player.position) : Mathf.Infinity;
        CheckSummonThreshold();
        switch (state)
        {
            case WendigoState.Chase:
                ChasePlayer();
                CheckTransitions(playerDistance);
                break;
            case WendigoState.Melee:
                MeleeAttack();
                break;
            case WendigoState.Range:
                RangeAttack();
                break;
            case WendigoState.Rush:
                RushAttack();
                break;
            case WendigoState.Evade:
                PerformEvade();
                break;
            case WendigoState.Summon:
                PerformSummon();
                break;
        }
    }
    #endregion
    #region Movement and Actions
    protected virtual void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Vector3 center = Application.isPlaying ? spawn : transform.position;
        if (attackPosition)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPosition.position, swipeRadius);
        }
        Gizmos.color = new Color(1f, 0.7f, 0f, 0.6f);
        Gizmos.color = new Color(0f, 0.6f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, personalSpace);
    }
    void ChasePlayer()
    {
        if (!player) return;
        anim.SetTrigger("Walking");
        Vector3 velocity = (player.position - transform.position).normalized * chaseSpeed;
        velocity.y = 0f;
        Vector3 planarVelocity = new Vector3(rigidBody.linearVelocity.x, 0, rigidBody.linearVelocity.z);
        Vector3 acceleration = Vector3.ClampMagnitude(velocity - planarVelocity, maxAccel) + Avoidance();
        rigidBody.AddForce(acceleration, ForceMode.Acceleration);
        planarVelocity = new Vector3(rigidBody.linearVelocity.x, 0, rigidBody.linearVelocity.z);
        if (planarVelocity.sqrMagnitude > chaseSpeed * chaseSpeed)
        {
            planarVelocity = planarVelocity.normalized * chaseSpeed;
            rigidBody.linearVelocity = new Vector3(planarVelocity.x, rigidBody.linearVelocity.y, planarVelocity.z);
        }
        if (planarVelocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(planarVelocity, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * turnLerp);
        }
    }
    void MeleeAttack()
    {
        if (attackLockout > 0f) return;
        StartCoroutine(MeleeRoutine());
    }
    void RangeAttack()
    {
        if (attackLockout > 0f) return;
        StartCoroutine(RangeRoutine());
    }
    void RushAttack()
    {
        if (attackLockout < 0f) return;
        StartCoroutine(RushRoutine());
    }
    void PerformEvade()
    {
        if (attackLockout < 0f) return;
        StartCoroutine(EvadeRoutine());
    }
    void PerformSummon()
    {
        if (attackLockout < 0f) return;
        StartCoroutine(SummonRoutine());

    }
    #endregion
    #region Checks and Coroutines
    Vector3 Avoidance()
    {
        Vector3 forward = (new Vector3(rigidBody.linearVelocity.x, 0, rigidBody.linearVelocity.z).sqrMagnitude > 0.01f)
                      ? new Vector3(rigidBody.linearVelocity.x, 0, rigidBody.linearVelocity.z).normalized
                      : transform.forward;
        Vector3 origin = transform.position + Vector3.up * 0.5f + forward * 0.5f;
        Vector3 avoidanceForce = Vector3.zero;
        if (Physics.SphereCast(origin, avoidRadius, forward, out RaycastHit hit, lookAhead, obstacleMask))
        {
            if (!hit.collider.transform.IsChildOf(transform))
            {
                // Push away from obstacle surface normal
                avoidanceForce += hit.normal * avoidStrength;
            }
        }
        Vector3 leftDir = Quaternion.AngleAxis(-whiskerAngle, Vector3.up) * forward;
        if (Physics.SphereCast(origin, avoidRadius, leftDir, out RaycastHit hitLeft, whiskerLen, obstacleMask))
        {
            if (!hitLeft.collider.transform.IsChildOf(transform))
            {
                avoidanceForce += Vector3.Cross(Vector3.up, leftDir).normalized * avoidStrength;
            }
        }

        Vector3 rightDir = Quaternion.AngleAxis(whiskerAngle, Vector3.up) * forward;
        if (Physics.SphereCast(origin, avoidRadius, rightDir, out RaycastHit hitRight, whiskerLen, obstacleMask))
        {
            if (!hitRight.collider.transform.IsChildOf(transform))
            {
                avoidanceForce += Vector3.Cross(rightDir, Vector3.up).normalized * avoidStrength;
            }
        }
        avoidanceForce.y = 0f;
        return avoidanceForce;
    }
    void CheckTransitions(float distance)
    {
        if (attackLockout > 0f) return;
        if (MeleeEnabled && distance <= swipeRange && swipeCD <= 0f) { state = WendigoState.Melee; return; }
        if (RangeEnabled && distance >= boltMinRange && distance <= boltMaxRange && boltCD <= 0f) { state = WendigoState.Range; return; }
        if (RushEnabled && distance <= rushRange && rushCD <= 0f) { state = WendigoState.Rush; return; }
        if (EvadeEnabled && dashCD <= 0f && (healthCurrent <= healthMax * 0.7f || healthCurrent <= healthMax * 0.3f)) { state = WendigoState.Evade; return; }
        state = WendigoState.Chase;
    }
    void CheckSummonThreshold()
    {
        if (!SummonEnabled || summonCD > 0f || state == WendigoState.Summon) return;
        if ((healthCurrent <= healthMax * 0.7f || healthCurrent <= healthMax * 0.3f) && dashCD > 0f)
        {
            summonCD = minionCooldown;
            state = WendigoState.Summon;
        }
    }
    IEnumerator MeleeRoutine()
    {
        attackLockout = swipeCooldown;
        anim.SetTrigger("Swipe");
        yield return new WaitForSeconds(swipeWindup);
        Vector3 attack = transform.position + transform.forward * swipeOffset.z + swipeOffset;
        Collider[] hitBox = Physics.OverlapSphere(attack, swipeRadius, rushHitMask);
        foreach (var hit in hitBox)
        {
            if (hit.CompareTag(playerTag))
            {
                IDamage playerHit = hit.GetComponent<IDamage>();
                if (playerHit != null) playerHit.TakeDamage((int)swipeDamage);
            }
        }
        yield return new WaitForSeconds(swipeRecover);
        state = WendigoState.Chase;
    }
    IEnumerator RangeRoutine()
    {
        attackLockout = boltCooldown;
        anim.SetTrigger("Bolt");
        yield return new WaitForSeconds(boltWindup);
        if (boltPrefab && castMuzzle)
        {
            GameObject bolt = Instantiate(boltPrefab, castMuzzle.position, castMuzzle.rotation);
            Rigidbody boltRB = bolt.GetComponent<Rigidbody>();
            if (boltRB) boltRB.linearVelocity = castMuzzle.forward * boltSpeed;
        }
        yield return new WaitForSeconds(boltRecover);
        state = WendigoState.Chase;
    }
    IEnumerator RushRoutine()
    {
        attackLockout = rushCooldown;
        anim.SetTrigger("Rush");
        yield return new WaitForSeconds(rushWindup);
        float time = 0f;
        rushHit = false;
        while (time < rushTime)
        {
            rigidBody.linearVelocity = transform.forward * rushSpeed;
            Collider[] hitBox = Physics.OverlapBox(transform.position + transform.forward * rushShoulderLength,
                new Vector3(rushShoulderRadius, 1f, rushShoulderLength / 2), transform.rotation, rushHitMask);
            foreach (Collider hit in hitBox)
            {
                if (!rushHit && hit.CompareTag(playerTag))
                {
                    IDamage playerHit = hit.GetComponent<IDamage>();
                    if (playerHit != null)
                    {
                        playerHit.TakeDamage((int)rushDamage);
                        rushHit = true;
                    }
                }
            }
            time = Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        rigidBody.linearVelocity = Vector3.zero;
        anim.SetTrigger("RushRecover");
        yield return new WaitForSeconds(rushRecover);
        state = WendigoState.Chase;
    }
    IEnumerator EvadeRoutine()
    {
        attackLockout = dashCooldown;
        anim.SetTrigger("Dash");
        float time = 0f;
        while (time < dashTime)
        {
            Vector3 dashAway = (transform.position - player.position).normalized;
            rigidBody.linearVelocity = dashAway * dashSpeed;
            time += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        rigidBody.linearVelocity = Vector3.zero;
        state = WendigoState.Chase;
    }
    IEnumerator SummonRoutine()
    {
        anim.SetTrigger("Summon");
        yield return new WaitForSeconds(summonWindup);
        foreach (GameObject minionPrefab in minions)
        {
            Vector3 spawnPosition = spawn + UnityEngine.Random.insideUnitSphere * 3f;
            spawnPosition.y = spawn.y;
            Instantiate(minionPrefab, spawnPosition, Quaternion.identity);
        }
        anim.SetTrigger("Idle");
        yield return new WaitForSeconds(summonRecover);
        state = WendigoState.Chase;
    }
    #endregion
    #region Damage and Death
    public void TakeDamage(int damage)
    {
        if (state == WendigoState.Dead) return;
        healthCurrent -= Mathf.Abs(damage);
        if (healthCurrent <= 0) Death();
        else PerformSummon();
    }
    void Death()
    {
        state = WendigoState.Dead;
        anim.SetTrigger("Death");
    }
    #endregion
}