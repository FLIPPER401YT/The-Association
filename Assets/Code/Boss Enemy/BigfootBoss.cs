using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;

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
    [SerializeField] Vector3 swipeOffset = new Vector3();

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
    [SerializeField] LayerMask rushHitMask = ~0;

    float swipeCD;
    float rushCD;
    float leapCD;
    bool rushDidHit;

    //Attack guards so zeroed blocks never get picked
    bool MeleeEnabled => swipeDamage > 0f && swipeRadius > 0f;
    bool RushEnabled => rushSpeed > 0f && rushTime > 0f && rushDamage > 0f;
    bool LeapEnabled => leapForce > 0f && leapUpBoost > 0f && slamRadius > 0f;

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

    IEnumerator DoSwipe()
    {
        ChangeState(BossState.Attack);
        FacePlayer();
        BrakePlanar();
        yield return new WaitForSeconds(swipeWindup);

        Vector3 center = transform.position + transform.TransformVector(swipeOffset);
        foreach (var h in Physics.OverlapSphere(center, swipeRadius, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.transform == player)
            {
                var dmg = h.GetComponent<IDamage>();
                if (dmg != null) dmg.TakeDamage((int)swipeDamage);
            }
        }

        yield return new WaitForSeconds(swipeRecover);
        swipeCD = swipeCooldown;
        attackLockout = globalAttackCooldown;
        ChangeState(BossState.Recover);
    }

    IEnumerator DoRush()
    {
        ChangeState(BossState.Attack);
        rushDidHit = false;

        float t = 0f;
        while (t < rushTime)
        {
            t += Time.fixedDeltaTime;

            Vector3 to = (player.position - transform.position); to.y = 0f;
            Vector3 desired = (to.sqrMagnitude > 0.1f ? to.normalized : transform.forward) * rushSpeed;
            Vector3 accel = Vector3.ClampMagnitude(desired - GetPlanarVel(), maxAccel * 1.25f);
            rb.AddForce(new Vector3(accel.x, 0f, accel.z), ForceMode.Acceleration);

            ClampPlanarSpeed(rushSpeed);
            FaceVelocity();

            if (!rushDidHit)
            {
                // capsule in front of chest/shoulders
                Vector3 origin = transform.position + Vector3.up * 0.4f;
                Vector3 top = origin + Vector3.up * 1.6f;
                float radius = Mathf.Max(0.05f, rushShoulderCastRadius);
                foreach (var c in Physics.OverlapCapsule(origin, top, radius, rushHitMask, QueryTriggerInteraction.Ignore))
                {
                    if (c.transform == player)
                    {
                        rushDidHit = true;

                        var dmg = c.GetComponent<IDamage>();
                        if (dmg != null) dmg.TakeDamage((int)rushDamage);

                        var prb = c.attachedRigidbody;
                        if (prb)
                        {
                            Vector3 impulse = transform.forward * rushKnockback + Vector3.up * rushUpwardKick;
                            prb.AddForce(impulse, ForceMode.Impulse);
                        }

                        // stop the rush immediately
#if UNITY_6000_0_OR_NEWER
                        rb.linearVelocity = Vector3.zero;
#else
                        rb.velocity = Vector3.zero;
#endif
                        t = rushTime;
                        break;
                    }
                }
            }
            yield return new WaitForFixedUpdate();
        }

        // rest after rush
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

    IEnumerator DoLeapSlam()
    {
        ChangeState(BossState.Attack);
        FacePlayer();

        // Predict ground target under the player
        Vector3 target = player.position;
        if (Physics.Raycast(new Vector3(player.position.x, player.position.y + 10f, player.position.z), Vector3.down, out RaycastHit ghit, 30f, groundMask))
            target = ghit.point;

        // Telegraph
        GameObject indicator = null;
        if (telegraphLanding && landingIndicatorPrefab)
        {
            indicator = Instantiate(landingIndicatorPrefab, target, Quaternion.identity);
            indicator.transform.localScale = new Vector3(slamRadius * 2f, 1f, slamRadius * 2f);
        }
        if (telegraphLanding) yield return new WaitForSeconds(Mathf.Max(0.1f, telegraphDuration * 0.5f));

        // Launch toward target
        Vector3 dir = (target - transform.position); dir.y = 0f; dir.Normalize();
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
#else
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
#endif
        rb.AddForce(dir * leapForce + Vector3.up * leapUpBoost, ForceMode.VelocityChange);

        // Wait until grounded
        while (!Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out _, 1.0f, groundMask))
            yield return new WaitForFixedUpdate();

        // Kill residual slide/spin
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
#else
        rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
#endif
        rb.angularVelocity = Vector3.zero;

        if (indicator) Destroy(indicator);
        yield return new WaitForSeconds(0.15f); // let physics settle

        // Slam AOE (Damage + Stun + OutwardForce)
        Vector3 slamCenter = transform.position + Vector3.up * 0.3f;
        foreach (var h in Physics.OverlapSphere(slamCenter, slamRadius, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.transform == player)
            {
                var dmg = h.GetComponent<IDamage>();
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

        // Flee after slam
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
}
