using UnityEngine;
using System.Collections;

public abstract class Base_Boss_AI : MonoBehaviour, IDamage
{
    public enum BossState { Roam, Chase, Attack, Recover, Dead }

    [Header("Refs")]
    [SerializeField] protected Transform player;
    [SerializeField] protected Rigidbody rb;

    [Header("Health")]
    [SerializeField] protected int maxHP;
    [SerializeField] protected int currentHP;

    [Header("Perception")]
    [SerializeField] protected float aggroRange;
    [SerializeField] protected float leashRange;

    [Header("Roam")]
    [SerializeField] protected float roamRadius;
    [SerializeField] protected float minHopDistance;
    [SerializeField] protected float arriveRadius;
    [SerializeField] protected Vector2 dwellRange = new Vector2(0.5f, 1.5f);
    [SerializeField] protected LayerMask groundMask = ~0;

    [Header("Movement")]
    [SerializeField] protected float maxSpeed;
    [SerializeField] protected float chaseSpeed;
    [SerializeField] protected float maxAccel;
    [SerializeField] protected float turnLerp;

    [Header("Avoidance")]
    [SerializeField] protected LayerMask obstacleMask = ~0;  // exclude Enemy/Ground
    [SerializeField] protected float avoidStrength;
    [SerializeField] protected float lookAhead;
    [SerializeField] protected float whiskerAngle;
    [SerializeField] protected float whiskerLen;
    [SerializeField] protected float avoidRadius;

    [Header("Attacks")]
    [SerializeField] protected float globalAttackCooldown;

    [Header("Debug")]
    [SerializeField] protected bool drawGizmos = true;

    protected BossState state = BossState.Roam;
    protected Vector3 spawn;
    protected Vector3 roamTarget;
    protected float dwellTimer;
    protected float attackLockout;

    // --- Stuck / progress tracking ---
    protected float timeSincePick;     // time since current roam target chosen
    protected float repickCooldown;    // small delay before we’re allowed to repick
    protected Vector3 lastProgressPos; // last sampled position for progress check
    protected float progressSampleT;   // timer for sampling progress

    protected virtual void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        currentHP = Mathf.Clamp(currentHP, 1, maxHP);
        spawn = transform.position;

        // keep upright; allow Y rotation
        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // init roam target & dwell
        PickRoamTarget(true);
        BeginDwell();

        // init stuck/progress tracking
        lastProgressPos = transform.position;
        timeSincePick = 0f;
        repickCooldown = 0f;
        progressSampleT = 0f;
    }

    protected virtual void FixedUpdate()
    {
        if (state == BossState.Dead) return;

        // timers
        attackLockout -= Time.fixedDeltaTime;
        timeSincePick += Time.fixedDeltaTime;
        if (repickCooldown > 0f) repickCooldown -= Time.fixedDeltaTime;

        // sample “real progress” every ~0.4s
        progressSampleT += Time.fixedDeltaTime;
        if (progressSampleT >= 0.4f)
        {
            Vector2 now = new Vector2(transform.position.x, transform.position.z);
            Vector2 was = new Vector2(lastProgressPos.x, lastProgressPos.z);
            if ((now - was).sqrMagnitude > 0.04f) // ~0.2 m delta
                lastProgressPos = transform.position;

            progressSampleT = 0f;
        }

        float distToPlayer = player ? Vector3.Distance(transform.position, player.position) : Mathf.Infinity;

        switch (state)
        {
            case BossState.Roam:
                if (distToPlayer <= aggroRange) ChangeState(BossState.Chase);
                RoamStep();
                break;

            case BossState.Chase:
                if (!player || distToPlayer > leashRange) { ChangeState(BossState.Roam); break; }
                ChaseStep();
                if (attackLockout <= 0f && CanAttack(distToPlayer))
                    StartCoroutine(DoAttackEntry(distToPlayer));
                break;

            case BossState.Attack:
                AttackStepWhileActive();
                break;

            case BossState.Recover:
                RecoverStep(distToPlayer);
                break;
        }
    }

    // Damage
    public void TakeDamage(int amount)
    {
        if (state == BossState.Dead) return;
        currentHP -= Mathf.Abs(amount);
        if (currentHP <= 0) Die();
    }

    protected virtual void Die()
    {
        if (state == BossState.Dead) return;
        state = BossState.Dead;

        StopAllCoroutines();
        enabled = false;

        if (rb)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
#endif
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        Destroy(gameObject, 0f);
    }

    // State helpers
    protected void ChangeState(BossState next) { state = next; }

    protected void BeginDwell() => dwellTimer = Random.Range(dwellRange.x, dwellRange.y);

    protected void RoamStep()
    {
        if (dwellTimer > 0f)
        {
            dwellTimer -= Time.fixedDeltaTime;
            BrakePlanar();
            FaceVelocity();
            return;
        }

        bool reached = Reached(roamTarget);

        // “Really stuck” only after a short grace period & when no progress is detected
        bool reallyStuck = false;
        if (!reached && timeSincePick > 0.6f && repickCooldown <= 0f)
        {
            Vector3 pv = GetPlanarVel();
            bool slow = pv.sqrMagnitude < 0.0025f; // ~0.05 m/s
            float prog = (new Vector2(transform.position.x, transform.position.z) -
                          new Vector2(lastProgressPos.x, lastProgressPos.z)).sqrMagnitude;
            bool noProgress = prog < 0.04f;       // ~0.2 m travel

            reallyStuck = slow && noProgress;
        }

        if (reached || reallyStuck)
        {
            BeginDwell();
            PickRoamTarget(false);
            return;
        }

        Vector3 desiredVel = DesiredRoamVelocity();
        Vector3 accel = Vector3.ClampMagnitude(desiredVel - GetPlanarVel(), maxAccel) + Avoidance();
        rb.AddForce(accel, ForceMode.Acceleration);

        ClampPlanarSpeed(maxSpeed);
        FaceVelocity();
    }

    protected void ChaseStep()
    {
        Vector3 desiredVel = DesiredChaseVelocity();
        Vector3 accel = Vector3.ClampMagnitude(desiredVel - GetPlanarVel(), maxAccel) + Avoidance();
        rb.AddForce(accel, ForceMode.Acceleration);

        ClampPlanarSpeed(chaseSpeed);
        FaceVelocity();
    }

    protected virtual void RecoverStep(float distToPlayer)
    {
        BrakePlanar();
        FacePlayer();
        ChangeState(distToPlayer <= leashRange ? BossState.Chase : BossState.Roam);
    }

    // Movement
    protected virtual Vector3 DesiredRoamVelocity()
    {
        Vector3 to = roamTarget - transform.position; to.y = 0f;
        return to.sqrMagnitude > 0.01f ? to.normalized * maxSpeed : Vector3.zero;
    }

    protected virtual Vector3 DesiredChaseVelocity()
    {
        if (!player) return Vector3.zero;
        Vector3 to = player.position - transform.position; to.y = 0f;
        return to.sqrMagnitude > 0.01f ? to.normalized * chaseSpeed : Vector3.zero;
    }

    protected virtual Vector3 Avoidance()
    {
        // Casts start slightly forward and up so we don’t hit our own capsule/ground
        Vector3 forward = GetPlanarVel().sqrMagnitude > 0.01f ? GetPlanarVel().normalized : transform.forward;
        Vector3 origin = transform.position + Vector3.up * 0.4f + forward * 0.6f;
        Vector3 acc = Vector3.zero;

        if (Physics.SphereCast(origin, avoidRadius, forward, out RaycastHit hit, lookAhead, obstacleMask))
        {
            if (!hit.collider.transform.IsChildOf(transform))
                acc += hit.normal * avoidStrength;
        }

        Vector3 L = Quaternion.AngleAxis(-whiskerAngle, Vector3.up) * forward;
        Vector3 R = Quaternion.AngleAxis(whiskerAngle, Vector3.up) * forward;

        if (Physics.SphereCast(origin, avoidRadius, L, out RaycastHit hitL, whiskerLen, obstacleMask))
            if (!hitL.collider.transform.IsChildOf(transform))
                acc += (Vector3.Cross(Vector3.up, L)).normalized * avoidStrength;

        if (Physics.SphereCast(origin, avoidRadius, R, out RaycastHit hitR, whiskerLen, obstacleMask))
            if (!hitR.collider.transform.IsChildOf(transform))
                acc += (Vector3.Cross(R, Vector3.up)).normalized * avoidStrength;

        acc.y = 0f;
        return acc;
    }

    // Attack
    protected virtual bool CanAttack(float distToPlayer) => true;
    protected abstract IEnumerator PickAndRunAttack(float distToPlayer);
    protected virtual void AttackStepWhileActive() { FaceVelocity(); }

    IEnumerator DoAttackEntry(float distToPlayer)
    {
        ChangeState(BossState.Attack);
        yield return StartCoroutine(PickAndRunAttack(distToPlayer));
        attackLockout = globalAttackCooldown;
        ChangeState(BossState.Recover);
    }

    // Utilities
    protected bool Reached(Vector3 worldpos)
    {
        Vector2 a = new Vector2(transform.position.x, transform.position.z);
        Vector2 b = new Vector2(worldpos.x, worldpos.z);
        return Vector2.Distance(a, b) <= arriveRadius;
    }

    protected Vector3 GetPlanarVel() => new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

    protected void ClampPlanarSpeed(float cap)
    {
        Vector3 v = GetPlanarVel();
        if (v.sqrMagnitude > cap * cap)
        {
            v = v.normalized * cap;
            rb.linearVelocity = new Vector3(v.x, rb.linearVelocity.y, v.z);
        }
    }

    protected void FaceVelocity()
    {
        Vector3 v = GetPlanarVel();
        if (v.sqrMagnitude < 0.05f) return; // ignore micro-jitters
        Quaternion targetRot = Quaternion.LookRotation(v, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * turnLerp);
    }

    protected void FacePlayer()
    {
        if (!player) return;
        Vector3 d = player.position - transform.position; d.y = 0f;
        if (d.sqrMagnitude < 0.0001f) return;
        Quaternion q = Quaternion.LookRotation(d, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.fixedDeltaTime * (turnLerp + 4f));
    }

    protected void BrakePlanar()
    {
        Vector3 planar = GetPlanarVel();
        rb.AddForce(-planar, ForceMode.VelocityChange);
    }

    protected void PickRoamTarget(bool firstPick)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 r = Random.insideUnitCircle * roamRadius;
            Vector3 probe = spawn + new Vector3(r.x, 10f, r.y);
            if (Physics.Raycast(probe, Vector3.down, out RaycastHit hit, 50f, groundMask))
            {
                float planar = Vector2.Distance(
                    new Vector2(transform.position.x, transform.position.z),
                    new Vector2(hit.point.x, hit.point.z));
                if (firstPick || planar >= minHopDistance)
                {
                    roamTarget = hit.point;

                    // reset stuck trackers
                    timeSincePick = 0f;
                    repickCooldown = 0.4f;
                    lastProgressPos = transform.position;
                    progressSampleT = 0f;

                    return;
                }
            }
        }
        // fallback
        Vector2 rf = Random.insideUnitCircle * roamRadius;
        roamTarget = spawn + new Vector3(rf.x, 0f, rf.y);

        // reset trackers on fallback too
        timeSincePick = 0f;
        repickCooldown = 0.4f;
        lastProgressPos = transform.position;
        progressSampleT = 0f;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Vector3 center = Application.isPlaying ? spawn : transform.position;
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.25f);
        Gizmos.DrawWireSphere(center, roamRadius);
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(roamTarget, 0.6f);
        }
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
    }
}
