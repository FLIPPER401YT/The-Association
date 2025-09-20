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
    [SerializeField] AnimationClip swipeAnimation;

    [Header("Rush / Ram")]
    [SerializeField] float rushSpeed;
    [SerializeField] float rushTime;
    [SerializeField] float rushDamage;
    [SerializeField] float rushKnockback;
    [SerializeField] float rushUpwardKick; // physics fallback
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
    [SerializeField] LayerMask slamHitMask = ~0;

    [Header("Tags")]
    [SerializeField] string playerTag = "Player";

    [Header("Colliders")]
    [Tooltip("Bigfoot's main collider used for closest-point rush origin. If null, will GetComponent at runtime.")]
    [SerializeField] Collider bodyCol;

    // ---------------- AUDIO ----------------
    [Header("Audio Sources")]
    [Tooltip("Looping roar/voice source (3D, loop ON).")]
    [SerializeField] AudioSource roarSrc;
    [Tooltip("One-shot FX source for attacks/impacts.")]
    [SerializeField] AudioSource fxSrc;

    [Header("Roar Loop")]
    [SerializeField] AudioClip roarLoop;
    [Range(0f, 1f)][SerializeField] float roarVolume;

    [Header("Swipe SFX")]
    [SerializeField] AudioClip swipeWindupSfx;
    [SerializeField] AudioClip swipeHitSfx;
    [Range(0f, 1f)][SerializeField] float swipeWindupVol;
    [Range(0f, 1f)][SerializeField] float swipeHitVol;

    [Header("Rush SFX")]
    [SerializeField] AudioClip rushStartSfx;
    [SerializeField] AudioClip rushHitSfx;
    [SerializeField] AudioClip rushEndSfx;
    [Range(0f, 1f)][SerializeField] float rushStartVol;
    [Range(0f, 1f)][SerializeField] float rushHitVol;
    [Range(0f, 1f)][SerializeField] float rushEndVol;

    [Header("Leap/Slam SFX")]
    [SerializeField] AudioClip leapStartSfx;
    [SerializeField] AudioClip slamImpactSfx;
    [Range(0f, 1f)][SerializeField] float leapStartVol;
    [Range(0f, 1f)][SerializeField] float slamImpactVol;

    [Header("SFX Tuning")]
    [Tooltip("Small random pitch variance for one-shots.")]
    [Range(0f, 0.2f)][SerializeField] float sfxPitchJitter;

    // ---------------- Personal Space ----------------
    [Header("Separation")]
    [SerializeField, Tooltip("Minimum XZ distance Bigfoot keeps from the player.")]
    float minPersonalSpace;

    [SerializeField, Tooltip("How fast he corrects if too close.")]
    float separationSpeed;

    [SerializeField, Tooltip("Apply separation during normal/attack updates (disabled while mid-leap).")]
    bool keepSpaceWhileAttacking = true;

    float swipeCD, rushCD, leapCD;
    bool rushDidHit;

    // leap state
    bool isLeaping;
    bool slamTriggered;
    GameObject currentIndicator;

    bool MeleeEnabled => swipeDamage > 0f && swipeRadius > 0f;
    bool RushEnabled => rushSpeed > 0f && rushTime > 0f && rushDamage > 0f;
    bool LeapEnabled => leapForce > 0f && leapUpBoost > 0f && slamOuterRadius > 0f;

    bool IsPlayerObj(Transform t)
        => (player && (t == player || t.IsChildOf(player))) || t.CompareTag(playerTag);

    static IDamage FindDamage(Component c)
        => c.GetComponentInParent<IDamage>() ?? c.GetComponentInChildren<IDamage>();

    static StatusEffects FindStatus(Component c)
        => c.GetComponentInParent<StatusEffects>() ?? c.GetComponentInChildren<StatusEffects>();

    StatusEffects GetPlayerStatus() => player ? player.GetComponentInChildren<StatusEffects>() : null;

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
        base.Awake();
        if (!bodyCol) bodyCol = GetComponent<Collider>();

        // --- Audio init ---
        if (!roarSrc) roarSrc = GetComponent<AudioSource>(); // fallback if you put one source on the root
        if (roarSrc)
        {
            roarSrc.loop = true;
            roarSrc.spatialBlend = 1f;
            roarSrc.rolloffMode = AudioRolloffMode.Logarithmic;
            if (roarLoop)
            {
                roarSrc.clip = roarLoop;
                roarSrc.volume = roarVolume;
                if (!roarSrc.isPlaying) roarSrc.Play();
            }
        }

        // fxSrc can be the same as roarSrc if you only have one source.
        if (!fxSrc) fxSrc = roarSrc;
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

        if (keepSpaceWhileAttacking && !isLeaping) MaintainPersonalSpace();

        base.FixedUpdate();
    }

    protected override void Die()
    {
        anim.SetBool("Rushing", false);
        anim.SetBool("Stunned", false);
        anim.SetTrigger("Death");
        anim.SetBool("Running", false);

        // stop roar on death
        if (roarSrc) roarSrc.Stop();

        base.Die();
    }

    // ---------------- Separation helper ----------------
    void MaintainPersonalSpace()
    {
        if (!player) return;

        Vector3 playerCenter = GetPlayerCenter();
        Vector3 delta = transform.position - playerCenter;
        delta.y = 0f;
        float dist = delta.magnitude;

        if (dist < 0.001f) delta = -transform.forward;
        if (dist >= minPersonalSpace) return;

        Vector3 outward = delta.normalized;
        Vector3 targetXZ = player.position + outward * minPersonalSpace;

        Vector3 newPos = Vector3.MoveTowards(
            new Vector3(transform.position.x, transform.position.y, transform.position.z),
            new Vector3(targetXZ.x, transform.position.y, targetXZ.z),
            separationSpeed * Time.fixedDeltaTime
        );

        if (rb) rb.MovePosition(newPos);
        else transform.position = newPos;
    }

    protected override IEnumerator PickAndRunAttack(float distToPlayer)
    {
        anim.SetBool("Running", false);

        if (MeleeEnabled && distToPlayer <= meleeRange && swipeCD <= 0f)
        {
            yield return StartCoroutine(DoSwipe());
            yield break;
        }

        bool canLeap = LeapEnabled && leapCD <= 0f &&
                       distToPlayer >= leapMinDist && distToPlayer <= leapMaxDist;

        bool canRush = RushEnabled && rushCD <= 0f &&
                       distToPlayer >= rushMinDist;

        if (canRush && (!canLeap || distToPlayer > (leapMaxDist + 0.5f)))
        {
            yield return StartCoroutine(DoRush());
            yield break;
        }

        if (canRush && canLeap)
        {
            bool pickRush = Random.value < 0.7f;
            yield return StartCoroutine(pickRush ? DoRush() : DoLeapSlam());
            yield break;
        }

        if (canRush)
        {
            yield return StartCoroutine(DoRush());
            yield break;
        }
        if (canLeap)
        {
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

        if (keepSpaceWhileAttacking) MaintainPersonalSpace();

        // SFX: windup
        PlayOneShot(swipeWindupSfx, swipeWindupVol);

        anim.SetTrigger("Swipe");
        float attackCheckTime = swipeAnimation != null ? swipeAnimation.length / 3f : swipeWindup;

        yield return new WaitForSeconds(attackCheckTime);

        Vector3 center = attackPos ? attackPos.position
                                   : transform.position + transform.TransformVector(swipeOffset);

        var hits = Physics.OverlapSphere(center, swipeRadius, slamHitMask, QueryTriggerInteraction.Collide);
        foreach (var h in hits)
        {
            if (!IsPlayerObj(h.transform)) continue;
            var dmg = FindDamage(h);
            if (dmg != null) dmg.TakeDamage((int)swipeDamage);

            // SFX: hit
            PlayOneShot(swipeHitSfx, swipeHitVol);
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

        // SFX: start
        PlayOneShot(rushStartSfx, rushStartVol);

        anim.SetBool("Rushing", true);

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

                    var status = GetPlayerStatus();
                    Vector3 playerCenter = GetPlayerCenter();
                    Vector3 hitOrigin = transform.position;

                    if (status) status.ApplyKnockback(hitOrigin, rushKnockback);
                    else
                    {
                        var prb = player ? (player.GetComponent<Rigidbody>() ?? player.GetComponentInChildren<Rigidbody>()) : null;
                        if (prb)
                        {
                            Vector3 dir = (playerCenter - hitOrigin); dir.y = 0f;
                            if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
                            dir.Normalize();
                            prb.AddForce(dir * rushKnockback + Vector3.up * Mathf.Max(0.5f, rushUpwardKick), ForceMode.Impulse);
                        }
                    }

                    // SFX: hit
                    PlayOneShot(rushHitSfx, rushHitVol);

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

        anim.SetBool("Rushing", false);
        anim.SetBool("Stunned", true);

        // SFX: end / breathe / stumble
        PlayOneShot(rushEndSfx, rushEndVol);

        if (keepSpaceWhileAttacking) MaintainPersonalSpace();

        yield return new WaitForSeconds(rushRestDuration);

        rushCD = rushCooldown;
        attackLockout = globalAttackCooldown;
        ChangeState(BossState.Recover);

        anim.SetBool("Stunned", false);
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

        anim.SetTrigger("Slam");

        Vector3 target = player ? player.position : transform.position + transform.forward * 5f;
        if (Physics.Raycast(target + Vector3.up * 10f, Vector3.down, out RaycastHit ghit, 30f, groundMask))
            target = ghit.point;

        if (telegraphLanding && landingIndicatorPrefab)
        {
            currentIndicator = Instantiate(landingIndicatorPrefab, target, Quaternion.identity);
            currentIndicator.transform.localScale = new Vector3(slamOuterRadius * 2f, 1f, slamOuterRadius * 2f);
        }
        if (telegraphLanding) yield return new WaitForSeconds(Mathf.Max(0.05f, telegraphDuration * 0.5f));

        // SFX: leap start
        PlayOneShot(leapStartSfx, leapStartVol);

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
        anim.SetBool("Running", true);

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

        anim.SetBool("Running", false);

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

        // SFX: ground impact
        PlayOneShot(slamImpactSfx, slamImpactVol);

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
                status.ApplyKnockback(slamCenter, slamKnock);
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

        Gizmos.color = new Color(0f, 0.6f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, minPersonalSpace);
    }
#endif

    // ---------------- AUDIO UTILS ----------------
    void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (!clip || !fxSrc) return;
        float basePitch = 1f + Random.Range(-sfxPitchJitter, sfxPitchJitter);
        fxSrc.pitch = basePitch;
        fxSrc.PlayOneShot(clip, volume);
    }
}
