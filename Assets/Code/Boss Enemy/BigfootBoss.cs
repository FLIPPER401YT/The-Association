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
    [SerializeField] float rushKnockback;
    [SerializeField] float rushUpwardKick;
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
    [SerializeField] float slamKnock;
    [SerializeField] float fleeTime;
    [SerializeField] float fleeSpeed;
    [SerializeField] float leapCooldown;

    [Header("Landing Telegraph")]
    [SerializeField] bool telegraphLanding = true;
    [SerializeField] float telegraphDuration;
    [SerializeField] GameObject landingIndicatorPrefab;

    [Header("Hit Masks")]
    [SerializeField] LayerMask rushHitMask = ~0;
    [SerializeField] LayerMask slamHitMask = ~0; // used for swipe; slam ignores mask when debugSlam=true

    [Header("Tags")]
    [SerializeField] string playerTag = "Player";

    [Header("Debug")]
    [SerializeField] bool debugSlam = false;

    float swipeCD;
    float rushCD;
    float leapCD;
    bool rushDidHit;

    // Attack guards
    bool MeleeEnabled => swipeDamage > 0f && swipeRadius > 0f;
    bool RushEnabled => rushSpeed > 0f && rushTime > 0f && rushDamage > 0f;
    bool LeapEnabled => leapForce > 0f && leapUpBoost > 0f && slamOuterRadius > 0f;

    // Helpers
    bool IsPlayerObj(Transform t)
        => (player && (t == player || t.IsChildOf(player))) || t.CompareTag(playerTag);

    static IDamage FindDamage(Component c)
        => c.GetComponentInParent<IDamage>() ?? c.GetComponentInChildren<IDamage>();

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
        var choices = new List<IEnumerator>();
        if (MeleeEnabled && distToPlayer <= meleeRange && swipeCD <= 0f) choices.Add(DoSwipe());
        if (LeapEnabled && distToPlayer >= leapMinDist && distToPlayer <= leapMaxDist && leapCD <= 0f) choices.Add(DoLeapSlam());
        if (RushEnabled && distToPlayer >= rushMinDist && rushCD <= 0f) choices.Add(DoRush());

        if (choices.Count == 0) yield break;
        yield return StartCoroutine(choices[Random.Range(0, choices.Count)]);
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

        // keep using the configured mask for swipe
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

                    var dmg = FindDamage(c);
                    if (dmg != null) dmg.TakeDamage((int)rushDamage);

                    var prb = c.attachedRigidbody ?? c.GetComponentInParent<Rigidbody>();
                    if (prb)
                    {
                        Vector3 impulse = transform.forward * rushKnockback + Vector3.up * rushUpwardKick;
                        prb.AddForce(impulse, ForceMode.Impulse);
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

    // --- grounded helper ---
    bool Grounded(float radius = 0.35f, float checkDist = 0.2f)
    {
        Vector3 origin = transform.position + Vector3.up * (radius + 0.05f);
        return Physics.SphereCast(origin, radius, Vector3.down, out _, checkDist + 0.05f, groundMask, QueryTriggerInteraction.Ignore);
    }

    // ---------- LEAP + SLAM ----------
    IEnumerator DoLeapSlam()
    {
        ChangeState(BossState.Attack);
        FacePlayer();

        // ground point under player (if any)
        Vector3 target = player ? player.position : transform.position + transform.forward * 5f;
        if (Physics.Raycast(target + Vector3.up * 10f, Vector3.down, out RaycastHit ghit, 30f, groundMask))
            target = ghit.point;

        GameObject indicator = null;
        if (telegraphLanding && landingIndicatorPrefab)
        {
            indicator = Instantiate(landingIndicatorPrefab, target, Quaternion.identity);
            indicator.transform.localScale = new Vector3(slamOuterRadius * 2f, 1f, slamOuterRadius * 2f);
        }
        if (telegraphLanding) yield return new WaitForSeconds(Mathf.Max(0.1f, telegraphDuration * 0.5f));

        Vector3 dir = (target - transform.position); dir.y = 0f; dir.Normalize();
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
#else
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
#endif
        rb.AddForce(dir * leapForce + Vector3.up * leapUpBoost, ForceMode.VelocityChange);

        // wait to land (with timeout)
        float waited = 0f, maxWait = 3f;
        while (!Grounded() && waited < maxWait)
        {
            waited += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
#else
        rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
#endif
        rb.angularVelocity = Vector3.zero;

        if (indicator) Destroy(indicator);
        yield return new WaitForSeconds(0.12f);

        // ---------- AOE DAMAGE ----------
        Vector3 slamCenter = transform.position + Vector3.up * 0.3f;

        // Use Everything while debugging so the player can't be masked out.
        int maskToUse = debugSlam ? ~0 : slamHitMask;

        var aoe = Physics.OverlapSphere(
            slamCenter,
            slamOuterRadius,
            maskToUse,
            QueryTriggerInteraction.Collide
        );

        int damaged = 0, seen = aoe.Length;
        for (int i = 0; i < aoe.Length; i++)
        {
            var h = aoe[i];

            bool isPlayer =
                (player && (h.transform == player || h.transform.IsChildOf(player))) ||
                h.CompareTag(playerTag);

            if (!isPlayer) continue;

            // Planar distance for ring test (XZ)
            Vector3 cp = h.bounds.ClosestPoint(slamCenter);
            float dx = cp.x - slamCenter.x;
            float dz = cp.z - slamCenter.z;
            float planar = Mathf.Sqrt(dx * dx + dz * dz);
            if (planar < slamInnerRadius) continue; // inside safe hole

            var dmg = FindDamage(h);
            if (dmg != null)
            {
                dmg.TakeDamage((int)slamDamage);
                damaged++;
            }

            var status = player ? player.GetComponentInChildren<StatusEffects>() : null;
            if (status) status.ApplyStun(slamStunDuration);
        }

        // Optional knockback to nearby rigidbodies
        for (int i = 0; i < aoe.Length; i++)
        {
            var h = aoe[i];
            if (h.attachedRigidbody && h.transform != transform)
            {
                Vector3 away = (h.transform.position - transform.position).normalized;
                h.attachedRigidbody.AddForce(away * slamKnock, ForceMode.Impulse);
            }
        }

        if (debugSlam)
        {
            Debug.Log($"[Bigfoot] Slam overlap={seen}, damaged={damaged}, center={slamCenter}, R={slamOuterRadius}, rInner={slamInnerRadius}");
        }
        // -----------------------------------------------

        // flee
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

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (attackPos)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPos.position, swipeRadius);
        }

        // visualize slam ring
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
