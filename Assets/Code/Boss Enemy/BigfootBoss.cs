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
    [SerializeField] float slamRadius;
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

    [Header("Rush Collision Layers")]
    [SerializeField] LayerMask rushHitMask;

    float swipeCD;
    float rushCD;
    float leapCD;
    bool rushDidHit;

    // Attack guards so zeroed blocks never get picked
    bool MeleeEnabled => swipeDamage > 0f && swipeRadius > 0f;
    bool RushEnabled => rushSpeed > 0f && rushTime > 0f && rushDamage > 0f;
    bool LeapEnabled => leapForce > 0f && leapUpBoost > 0f && slamRadius > 0f;

    //Helpers for reliable player hit detection 
    bool IsPlayer(Transform t) => player && (t == player || t.IsChildOf(player));
    static IDamage FindDamage(Component c) =>
        c.GetComponentInParent<IDamage>() ?? c.GetComponentInChildren<IDamage>();
   

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

        if (choices.Count == 0) yield break; // stay in Chase
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

        var hits = Physics.OverlapSphere(center, swipeRadius, ~0, QueryTriggerInteraction.Collide);
        foreach (var h in hits)
        {
            if (!IsPlayer(h.transform)) continue;

            var dmg = FindDamage(h);
            if (dmg != null) dmg.TakeDamage((int)swipeDamage);
            break; // one hit is enough
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
                    if (!IsPlayer(c.transform)) continue;

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

    // --- Robust grounded helper (spherecast) ---
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
            indicator.transform.localScale = new Vector3(slamRadius * 2f, 1f, slamRadius * 2f);
        }
        if (telegraphLanding) yield return new WaitForSeconds(Mathf.Max(0.1f, telegraphDuration * 0.5f));

        Vector3 dir = (target - transform.position); dir.y = 0f; dir.Normalize();
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
#else
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
#endif
        rb.AddForce(dir * leapForce + Vector3.up * leapUpBoost, ForceMode.VelocityChange);

        // wait to land (with timeout so we never hang)
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

        // AOE
        Vector3 slamCenter = transform.position + Vector3.up * 0.3f;
        var aoe = Physics.OverlapSphere(slamCenter, slamRadius, ~0, QueryTriggerInteraction.Collide);
        foreach (var h in aoe)
        {
            if (IsPlayer(h.transform))
            {
                var dmg = FindDamage(h);
                if (dmg != null) dmg.TakeDamage((int)slamDamage);

                var status = player.GetComponentInChildren<StatusEffects>();
                if (status) status.ApplyStun(slamStunDuration);
            }

            if (h.attachedRigidbody && h.transform != transform)
            {
                Vector3 away = (h.transform.position - transform.position).normalized;
                h.attachedRigidbody.AddForce(away * slamKnock, ForceMode.Impulse);
            }
        }

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
        // draw the base gizmos (roam radius, aggro, current roam target, etc.)
        base.OnDrawGizmosSelected();

        // draw the melee/attack position
        if (attackPos)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPos.position, swipeRadius);
        }
    }
#endif

}
