using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class BigfootBoss : Base_Boss_AI
{
    [Header("Ranges")]
    [SerializeField] float meleeRange;
    [SerializeField] float leapMinDist;
    [SerializeField] float leapMaxDist;
    [SerializeField] float rushMinDist;

    [Header("Swipe")]
    [SerializeField] float swipeDamage;
    [SerializeField] float swipeWindup;
    [SerializeField] float swipeRecover;
    [SerializeField] float swipeRadius;
    [SerializeField] float swipeCooldown;
    [SerializeField] Vector3 swipeOffset;
    [Tooltip("If set, swipe uses this position instead of swipeOffset.")]
    [SerializeField] Transform attackPos;

    [Header("Rush / Ram")]
    [SerializeField] float rushSpeed;
    [SerializeField] float rushTime;
    [SerializeField] float rushDamage;
    [SerializeField] float rushKnockback;     // StatusEffects knockback strength
    [SerializeField] float rushUpwardKick;    // physics fallback
    [SerializeField] float rushRestDuration;
    [SerializeField] float rushCooldown;
    [SerializeField] float rushShoulderCastRadius;
    [SerializeField] float rushShoulderCastLength;

    [Header("Leap Ground Pound")]
    [SerializeField] float leapForce;
    [SerializeField] float leapUpBoost;

    [Tooltip("Outer radius of the damage ring at impact.")]
    [SerializeField] float slamOuterRadius;
    [Tooltip("Inner radius (no damage inside this). 0 = solid disk.")]
    [SerializeField] float slamInnerRadius;

    [SerializeField] float slamDamage;
    [SerializeField] float slamStunDuration;
    [SerializeField] float slamKnock;         // StatusEffects knockback strength
    [SerializeField] float fleeTime;
    [SerializeField] float fleeSpeed;
    [SerializeField] float leapCooldown;

    [Header("Landing Telegraph")]
    [SerializeField] bool telegraphLanding = true;
    [SerializeField] float telegraphDuration;
    [SerializeField] GameObject landingIndicatorPrefab;

    [Header("Hit Masks")]
    [SerializeField] LayerMask rushHitMask = ~0;
    [SerializeField] LayerMask slamHitMask = ~0;

    [Header("Tags")]
    [SerializeField] string playerTag = "Player";

    [Header("Colliders")]
    [Tooltip("Bigfoot's main collider (used to compute closest point for rush knockback). If null, will GetComponent at runtime.")]
    [SerializeField] Collider bodyCol;

    float swipeCD;
    float rushCD;
    float leapCD;
    bool rushDidHit;

    // leap state
    bool isLeaping;
    bool slamTriggered;
    GameObject currentIndicator;

    // Attack guards
    bool MeleeEnabled => swipeDamage > 0f && swipeRadius > 0f;
    bool RushEnabled => rushSpeed > 0f && rushTime > 0f && rushDamage > 0f;
    bool LeapEnabled => leapForce > 0f && leapUpBoost > 0f && slamOuterRadius > 0f;

    // Helpers
    bool IsPlayerObj(Transform t)
        => (player && (t == player || t.IsChildOf(player))) || t.CompareTag(playerTag);

    static IDamage FindDamage(Component c)
        => c.GetComponentInParent<IDamage>() ?? c.GetComponentInChildren<IDamage>();

    static StatusEffects FindStatus(Component c)
        => c.GetComponentInParent<StatusEffects>() ?? c.GetComponentInChildren<StatusEffects>();

    // NEW: always get StatusEffects from the real player root
    StatusEffects GetPlayerStatus()
    {
        if (!player) return null;
        return player.GetComponentInChildren<StatusEffects>();
    }

    // NEW: a reliable player “center” for push direction
    Vector3 GetPlayerCenter()
    {
        if (!player) return transform.position;

        var cc = player.GetComponent<CharacterController>();
        if (cc) return cc.bounds.center;

        var col = player.GetComponentInChildren<Collider>();
        if (col) return col.bounds.center;

        return player.position;
    }

    protected override void Awake()
    {
        base.Awake();                    // IMPORTANT: keep all base init
        if (!bodyCol) bodyCol = GetComponent<Collider>();
    }

    protected override bool CanAttack(float distToPlayer)
    {
        return
            (MeleeEnabled && distToPlayer <= meleeRange + 0.5f) ||
            (LeapEnabled && distToPlayer >= leapMinDist && distToPlayer <= leapMaxDist) ||
            (RushEnabled && distToPlayer >= rushMinDist);
    }

    protected override void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        swipeCD -= dt; rushCD -= dt; leapCD -= dt;
        base.FixedUpdate();
    }

    protected override IEnumerator PickAndRunAttack(float distToPlayer)
    {
        if (MeleeEnabled && distToPlayer <= meleeRange && swipeCD <= 0f)
        {
            Debug.Log("[Bigfoot] ATTACK: Swipe");
            yield return StartCoroutine(DoSwipe());
            yield break;
        }

        bool canLeap = LeapEnabled && leapCD <= 0f &&
                       distToPlayer >= leapMinDist && distToPlayer <= leapMaxDist;

        bool canRush = RushEnabled && rushCD <= 0f &&
                       distToPlayer >= rushMinDist;

        if (canRush && (!canLeap || distToPlayer > (leapMaxDist + 0.5f)))
        {
            Debug.Log("[Bigfoot] ATTACK: Rush (outside leap range)");
            yield return StartCoroutine(DoRush());
            yield break;
        }

        if (canRush && canLeap)
        {
            bool pickRush = Random.value < 0.7f;
            Debug.Log("[Bigfoot] ATTACK pick (both valid): " + (pickRush ? "Rush" : "Leap"));
            yield return StartCoroutine(pickRush ? DoRush() : DoLeapSlam());
            yield break;
        }

        if (canRush)
        {
            Debug.Log("[Bigfoot] ATTACK: Rush");
            yield return StartCoroutine(DoRush());
            yield break;
        }
        if (canLeap)
        {
            Debug.Log("[Bigfoot] ATTACK: Leap");
            yield return StartCoroutine(DoLeapSlam());
            yield break;
        }
    }

    // ---------- SWIPE ----------
    IEnumerator DoSwipe()
    {
        ChangeState(BossState.Attack);
        FacePlayer();
        BrakePlanar();
        yield return new WaitForSeconds(swipeWindup);

        Vector3 center = attackPos ? attackPos.position
                                   : transform.position + transform.TransformVector(swipeOffset);

        var hits = Physics.OverlapSphere(center, swipeRadius, slamHitMask, QueryTriggerInteraction.Collide);
        foreach (var h in hits)
        {
            if (!IsPlayerObj(h.transform)) continue;
            var dmg = FindDamage(h);
            if (dmg != null) dmg.TakeDamage((int)swipeDamage);
            break;
        }

        yield return new WaitForSeconds(swipeRecover);
        swipeCD = swipeCooldown;
        attackLockout = globalAttackCooldown;
        ChangeState(BossState.Recover);
    }

    // ---------- RUSH ----------
    IEnumerator DoRush()
    {
        ChangeState(BossState.Attack);
        rushDidHit = false;

        float t = 0f;
        while (t < rushTime)
        {
            t += Time.fixedDeltaTime;

            Vector3 to = (player ? player.position - transform.position : Vector3.zero); to.y = 0f;
            Vector3 desired = (to.sqrMagnitude > 0.1f ? to.normalized : transform.forward) * rushSpeed;
            Vector3 accel = Vector3.ClampMagnitude(desired - GetPlanarVel(), maxAccel * 1.25f);
            rb.AddForce(new Vector3(accel.x, 0f, accel.z), ForceMode.Acceleration);

            ClampPlanarSpeed(rushSpeed);
            FaceVelocity();

            if (!rushDidHit)
            {
                Vector3 origin = transform.position + Vector3.up * 0.4f;
                Vector3 top = origin + Vector3.up * 1.6f;
                float radius = Mathf.Max(0.05f, rushShoulderCastRadius);

                var cols = Physics.OverlapCapsule(origin, top, radius, rushHitMask, QueryTriggerInteraction.Collide);
                foreach (var c in cols)
                {
                    if (!IsPlayerObj(c.transform)) continue;

                    rushDidHit = true;

                    // damage
                    var dmg = FindDamage(c);
                    if (dmg != null) dmg.TakeDamage((int)rushDamage);

                    // --- robust, root-targeted knockback ---
                    StatusEffects status = GetPlayerStatus();
                    Vector3 playerCenter = GetPlayerCenter();
                    Vector3 hitOrigin = bodyCol ? bodyCol.ClosestPoint(playerCenter) : transform.position;

                    // push from Bigfoot towards the player's center (flat)
                    Vector3 dir = playerCenter - hitOrigin;
                    Debug.Log($"dir equals {dir}");
                    dir.y = 0f;
                    Debug.Log($"dir equals {dir}");
                    if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
                    Debug.Log($"dir equals {dir}");
                    //Debug.Break();
                    Debug.DrawLine(playerCenter, playerCenter + dir*2, Color.red);
                    dir.Normalize();
                    Debug.Log($"dir equals {dir}");

                    if (status)
                    {
                        status.ApplyKnockbackDirection(dir, rushKnockback, 0.12f);
                        Debug.Log("apply kb");
                    }
                    else
                    {
                        var prb = player ? (player.GetComponent<Rigidbody>() ??
                                            player.GetComponentInChildren<Rigidbody>()) : null;
                        Debug.Log("no kb");
                        if (prb)
                        {
                            Vector3 impulse = dir * rushKnockback + Vector3.up * Mathf.Max(0.5f, rushUpwardKick);
                            prb.AddForce(impulse, ForceMode.Impulse);
                            Debug.Log("kb 3");
                        }
                    }

                    // let the player slide out cleanly if overlapping
                    if (bodyCol && player)
                    {
                        var pCol = player.GetComponentInChildren<Collider>();
                        if (pCol) StartCoroutine(TemporarilyIgnoreCollision(bodyCol, pCol, 0.25f));
                    }

#if UNITY_6000_0_OR_NEWER
                    rb.linearVelocity = Vector3.zero;
#else
                    rb.velocity = Vector3.zero;
#endif
                    t = rushTime;
                    break;
                }
            }
            yield return new WaitForFixedUpdate();
        }

        BrakePlanar();
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector3.zero;
#else
        rb.velocity = Vector3.zero;
#endif
        yield return new WaitForSeconds(rushRestDuration);

        rushCD = rushCooldown;
        attackLockout = globalAttackCooldown;
        ChangeState(BossState.Recover);
    }

    IEnumerator TemporarilyIgnoreCollision(Collider a, Collider b, float seconds)
    {
        if (!a || !b) yield break;
        Physics.IgnoreCollision(a, b, true);
        yield return new WaitForSeconds(seconds);
        if (a && b) Physics.IgnoreCollision(a, b, false);
    }

    // ---------- LEAP + SLAM ----------
    IEnumerator DoLeapSlam()
    {
        ChangeState(BossState.Attack);
        FacePlayer();

        Vector3 target = player ? player.position : transform.position + transform.forward * 5f;
        if (Physics.Raycast(target + Vector3.up * 10f, Vector3.down, out RaycastHit ghit, 30f, groundMask))
            target = ghit.point;

        if (telegraphLanding && landingIndicatorPrefab)
        {
            currentIndicator = Instantiate(landingIndicatorPrefab, target, Quaternion.identity);
            currentIndicator.transform.localScale = new Vector3(slamOuterRadius * 2f, 1f, slamOuterRadius * 2f);
        }
        if (telegraphLanding) yield return new WaitForSeconds(Mathf.Max(0.05f, telegraphDuration * 0.5f));

        Vector3 dir = (target - transform.position); dir.y = 0f; dir.Normalize();
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
#else
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
#endif
        rb.AddForce(dir * leapForce + Vector3.up * leapUpBoost, ForceMode.VelocityChange);

        isLeaping = true;
        slamTriggered = false;

        float waited = 0f, maxWait = 3f;
        while (!slamTriggered && waited < maxWait)
        {
            waited += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // flee after slam
        float t = 0f;
        while (t < fleeTime)
        {
            t += Time.fixedDeltaTime;
            if (player)
            {
                Vector3 away = (transform.position - player.position); away.y = 0f;
                Vector3 desired = away.normalized * fleeSpeed;
                Vector3 accel = Vector3.ClampMagnitude(desired - GetPlanarVel(), maxAccel);
                rb.AddForce(new Vector3(accel.x, 0f, accel.z), ForceMode.Acceleration);
                ClampPlanarSpeed(fleeSpeed);
                FaceVelocity();
            }
            yield return new WaitForFixedUpdate();
        }

        leapCD = leapCooldown;
        attackLockout = globalAttackCooldown;
        ChangeState(BossState.Recover);
    }

    void OnCollisionEnter(Collision c)
    {
        if (!isLeaping || slamTriggered) return;
        if (((1 << c.collider.gameObject.layer) & groundMask) == 0) return;

        slamTriggered = true;
        isLeaping = false;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
#else
        rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
#endif
        rb.angularVelocity = Vector3.zero;

        if (currentIndicator) { Destroy(currentIndicator); currentIndicator = null; }

        DoSlamImpact();
    }

    void DoSlamImpact()
    {
        Vector3 slamCenter = transform.position + Vector3.up * 0.3f;

        var aoe = Physics.OverlapSphere(slamCenter, slamOuterRadius, slamHitMask, QueryTriggerInteraction.Collide);
        foreach (var h in aoe)
        {
            if (!IsPlayerObj(h.transform)) continue;

            Vector3 p = h.bounds.ClosestPoint(slamCenter);
            float dx = p.x - slamCenter.x;
            float dz = p.z - slamCenter.z;
            float planarDist = Mathf.Sqrt(dx * dx + dz * dz);
            if (planarDist < slamInnerRadius) continue;

            var dmg = FindDamage(h);
            if (dmg != null) dmg.TakeDamage((int)slamDamage);

            var status = FindStatus(h);
            if (status)
            {
                Vector3 dir = (h.bounds.center - slamCenter); dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
                dir.Normalize();
                status.ApplyKnockbackDirection(dir, slamKnock, 0.12f);
                if (slamStunDuration > 0f) status.ApplyStun(slamStunDuration);
            }
            else
            {
                var prb = h.attachedRigidbody ?? h.GetComponentInParent<Rigidbody>();
                if (prb)
                {
                    Vector3 away = (h.transform.position - transform.position).normalized;
                    prb.AddForce(away * slamKnock + Vector3.up * 0.5f, ForceMode.Impulse);
                }
            }
        }
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (attackPos)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPos.position, swipeRadius);
        }

        Gizmos.color = new Color(1f, 0.7f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, slamOuterRadius);
        if (slamInnerRadius > 0f)
        {
            Gizmos.color = new Color(1f, 0.2f, 0f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, slamInnerRadius);
        }
    }
#endif
}
