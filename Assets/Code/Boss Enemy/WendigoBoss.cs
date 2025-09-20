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

    [Header("Range")]
    [SerializeField] float swipeRange;
    [SerializeField] float boltMinRange;
    [SerializeField] float boltMaxRange;
    [SerializeField] float rushRange;
    [SerializeField] float dashRange;

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

    public enum WendigoState { Chase, Melee, Range, Evade, Dead };
    WendigoState state;
    Vector3 spawn; float attackLockout;
    float swipeCD, boltCD, dashCD;
    bool healthPercent70 = false, healthPercent30 = false;
    bool MeleeEnabled => swipeDamage > 0f && swipeRadius > 0f;
    bool RangeEnabled => boltDamage > 0f && boltMinRange > 0f && boltMaxRange > 0f;
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
        swipeCD = boltCD = dashCD = 0f;
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
        boltCD -= Time.fixedDeltaTime; dashCD -= Time.fixedDeltaTime;
        float playerDistance = player ? Vector3.Distance(transform.position, player.position) : Mathf.Infinity;
        switch (state)
        {
            case WendigoState.Chase:
                ChasePlayer();
                CheckTransitions(playerDistance);
                break;
            case WendigoState.Melee:
                MeleeAttack();
                CheckTransitions(playerDistance);
                break;
            case WendigoState.Range:
                RangeAttack();
                CheckTransitions(playerDistance);
                break;
            case WendigoState.Evade:
                PerformEvade();
                CheckTransitions(playerDistance);
                break;
        } 
    }
    #endregion
    #region Movement and Actions
    void ChasePlayer()
    {
        if (!player) return;
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
            anim.SetBool("Walking", true);
            Quaternion targetRotation = Quaternion.LookRotation(planarVelocity, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * turnLerp);
        }
        else anim.SetBool("Walking", false);
    }
    void MeleeAttack()
    {
        anim.SetBool("Walking", false);
        if (attackLockout > 0f)
        {
            return;
        }
        StartCoroutine(MeleeRoutine());
    }
    void RangeAttack()
    {
        anim.SetBool("Walking", false);
        if (attackLockout > 0f) return;
        StartCoroutine(RangeRoutine());
    }
    void PerformEvade()
    {
        anim.SetBool("Walking", false);
        if (attackLockout < 0f) return;
        StartCoroutine(EvadeRoutine());
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
        if (state == WendigoState.Melee || state == WendigoState.Range || state == WendigoState.Evade) return;
        if (MeleeEnabled && distance <= swipeRange && swipeCD <= 0f) { state = WendigoState.Melee; return; }
        if (RangeEnabled && distance >= boltMinRange && distance <= boltMaxRange && boltCD <= 0f) { state = WendigoState.Range; return; }
        if (EvadeEnabled && dashCD <= 0f && (healthPercent70 || healthPercent30)) { state = WendigoState.Evade; return; }
        state = WendigoState.Chase;
    }
    IEnumerator FacePlayer(float facePlayer = 10f)
    {
        while (player != null)
        {
            Vector3 playerDirection = (player.position - transform.position).normalized;
            playerDirection.y = 0f;
            if (playerDirection.sqrMagnitude < 0.01f) yield break;
            Quaternion targetRotation = Quaternion.LookRotation(playerDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, facePlayer * Time.deltaTime);
            float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);
            if (angleDifference < .01f) break;
            yield return null;
        }
    }
    IEnumerator MeleeRoutine()
    {
        Debug.Log("MeleeRoutine started");
        attackLockout = swipeCooldown;
        swipeCD = swipeCooldown;
        anim.SetTrigger("Swipe");
        yield return new WaitForSeconds(swipeWindup);
        Vector3 attack = transform.position + transform.rotation * swipeOffset;
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
        yield return StartCoroutine(FacePlayer());
        attackLockout = boltCooldown;
        boltCD = boltCooldown;
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
    IEnumerator EvadeRoutine()
    {
        attackLockout = dashCooldown;
        dashCD = dashCooldown;
        anim.SetTrigger("Evade");
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
    #endregion
    #region Damage and Death
    public void TakeDamage(int damage)
    {
        if (state == WendigoState.Dead) return;
        healthCurrent -= Mathf.Abs(damage);
        if (healthCurrent <= 0) Death();
    }
    void Death()
    {
        state = WendigoState.Dead;
        anim.SetBool("Walking", false);
        anim.SetTrigger("Death");
        StartCoroutine(DestroyOffset());
    }
    IEnumerator DestroyOffset()
    {
        yield return new WaitForSeconds(deathAnimation.length);
        Destroy(gameObject);
    }
    #endregion
}