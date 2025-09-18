using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public abstract class EnemyMovementBaseRB : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected Animator anim;
    [Header("Target")]
    [SerializeField] protected Transform target;

    [Header("Motion")]
    [SerializeField] protected float maxSpeed;
    [SerializeField] protected float acceleration;
    [SerializeField] protected float decel;
    [SerializeField] protected float turnSpeed;

    [Header("Stop")]
    [SerializeField] protected float stopDistance;

    [Header("Simple Avoidance")]
    [SerializeField] protected LayerMask obstacleMask = ~0;
    [SerializeField] protected float avoidProbeDist;
    [SerializeField] protected float sideProbeOffset;
    [SerializeField] protected float avoidSteerStrength;


    [Header("Roam (when no target)")]
    [SerializeField] protected bool enableRoam = true;
    [SerializeField] protected float roamRadius;
    [SerializeField] protected float minHopDistance;
    [SerializeField] protected float arriveRadius;
    [SerializeField] protected Vector2 dwellRange = new Vector2();
    [SerializeField] protected LayerMask roamGroundMask = ~0;

    // stuck/progress tracking 
    Vector3 spawn;
    Vector3 roamTarget;
    float dwellTimer;
    float timeSincePick;
    float repickCooldown;
    Vector3 lastProgressPos;
    float progressSampleT;

    protected Rigidbody rb;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation; // rotate via code

        spawn = transform.position;

        // init roam
        ResetRoam(true);
    }

    protected virtual void FixedUpdate()
    {
        if (!target)
        {
            NoTargetStep();
            ApplyGravity();
            return;
        }

        // When a target exists, child class drives movement each physics step
        TickMovement();
        ApplyGravity();
    }

    // ---------------- CHILD OVERRIDES ----------------
    // Child movers implement their chase/leap behavior here (only called when target != null)
    protected abstract void TickMovement();

    // derived movers can apply gravity here (base does nothing)
    protected virtual void ApplyGravity() { /* no-op by default */ }

    protected virtual void NoTargetStep()
    {
        // default: the old behavior (roam if enabled)
        if (enableRoam) RoamStep();
        else BrakeToStop();
    }

    // ---------------- ROAM CORE ----------------
    void RoamStep()
    {
        // timers
        timeSincePick += Time.fixedDeltaTime;
        if (repickCooldown > 0f) repickCooldown -= Time.fixedDeltaTime;

        // sample progress every ~0.4s
        progressSampleT += Time.fixedDeltaTime;
        if (progressSampleT >= 0.4f)
        {
            Vector2 now = new Vector2(transform.position.x, transform.position.z);
            Vector2 was = new Vector2(lastProgressPos.x, lastProgressPos.z);
            if ((now - was).sqrMagnitude > 0.04f) lastProgressPos = transform.position; // ~0.2m
            progressSampleT = 0f;
        }

        // dwell before moving
        if (dwellTimer > 0f)
        {
            dwellTimer -= Time.fixedDeltaTime;
            BrakeToStop();
            Face(rb.linearVelocity.sqrMagnitude > 0.01f ? rb.linearVelocity : transform.forward);
            return;
        }

        bool reached = Reached(roamTarget);
        bool reallyStuck = false;

        if (!reached && timeSincePick > 0.6f && repickCooldown <= 0f)
        {
            Vector3 pv = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            bool slow = pv.sqrMagnitude < 0.0025f; // ~0.05m/s
            float prog = (new Vector2(transform.position.x, transform.position.z) -
                          new Vector2(lastProgressPos.x, lastProgressPos.z)).sqrMagnitude;
            bool noProgress = prog < 0.04f;       // ~0.2m travel
            reallyStuck = slow && noProgress;
        }

        if (reached || reallyStuck)
        {
            BeginDwell();
            PickRoamTarget(false);
            return;
        }

        // move toward roam target with avoidance
        Vector3 to = roamTarget - transform.position; to.y = 0f;
        Vector3 dir = to.sqrMagnitude > 0.01f ? to.normalized : Vector3.zero;
        dir = ApplyAvoidance(dir);

        Face(dir);
        MoveHorizontal(dir);
    }

    void ResetRoam(bool firstPick)
    {
        PickRoamTarget(firstPick);
        BeginDwell();

        lastProgressPos = transform.position;
        timeSincePick = 0f;
        repickCooldown = 0f;
        progressSampleT = 0f;
    }

    void BeginDwell() => dwellTimer = Random.Range(dwellRange.x, dwellRange.y);

    void PickRoamTarget(bool firstPick)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 r = Random.insideUnitCircle * roamRadius;
            Vector3 probe = spawn + new Vector3(r.x, 10f, r.y);

            if (Physics.Raycast(probe, Vector3.down, out RaycastHit hit, 50f, roamGroundMask))
            {
                float planar = Vector2.Distance(
                    new Vector2(transform.position.x, transform.position.z),
                    new Vector2(hit.point.x, hit.point.z));

                if (firstPick || planar >= minHopDistance)
                {
                    roamTarget = hit.point;

                    timeSincePick = 0f;
                    repickCooldown = 0.4f;
                    lastProgressPos = transform.position;
                    progressSampleT = 0f;
                    return;
                }
            }
        }

        // fallback if no ground hit
        Vector2 rf = Random.insideUnitCircle * roamRadius;
        roamTarget = spawn + new Vector3(rf.x, 0f, rf.y);

        timeSincePick = 0f;
        repickCooldown = 0.4f;
        lastProgressPos = transform.position;
        progressSampleT = 0f;
    }

    bool Reached(Vector3 worldpos)
    {
        Vector2 a = new Vector2(transform.position.x, transform.position.z);
        Vector2 b = new Vector2(worldpos.x, worldpos.z);
        return Vector2.Distance(a, b) <= arriveRadius;
    }

    // ---------------- SHARED UTILS ----------------
    protected void Face(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion targetRot = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z), Vector3.up);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime));
    }

    protected void BrakeToStop()
    {
        Vector3 v = rb.linearVelocity;
        float mag = v.magnitude;
        float drop = decel * Time.fixedDeltaTime;
        rb.linearVelocity = (mag <= drop) ? Vector3.zero : v * ((mag - drop) / Mathf.Max(mag, 0.0001f));
    }

    protected Vector3 ApplyAvoidance(Vector3 dirWorld)
    {
        Vector3 origin = transform.position + Vector3.up * 0.2f;
        Vector3 fwd = transform.forward;
        Vector3 steer = Vector3.zero;

        // forward probe
        if (Physics.Raycast(origin, fwd, out _, avoidProbeDist, obstacleMask, QueryTriggerInteraction.Ignore))
            steer += Vector3.Cross(Vector3.up, fwd); // nudge right

        // side probes
        Vector3 leftOrigin = origin - transform.right * sideProbeOffset;
        Vector3 rightOrigin = origin + transform.right * sideProbeOffset;

        if (Physics.Raycast(leftOrigin, fwd, out _, avoidProbeDist * 0.9f, obstacleMask)) steer += transform.right;
        if (Physics.Raycast(rightOrigin, fwd, out _, avoidProbeDist * 0.9f, obstacleMask)) steer -= transform.right;

        if (steer != Vector3.zero)
            dirWorld = (dirWorld + steer.normalized * (avoidSteerStrength * Time.fixedDeltaTime)).normalized;

        return dirWorld;
    }

    protected void MoveHorizontal(Vector3 desiredDirXZ)
    {
        Vector3 vel = rb.linearVelocity;
        Vector3 horiz = new Vector3(vel.x, 0f, vel.z);
        Vector3 desired = desiredDirXZ * maxSpeed;

        Vector3 delta = desired - horiz;
        float a = (desired.sqrMagnitude >= horiz.sqrMagnitude) ? acceleration : decel;
        Vector3 change = Vector3.ClampMagnitude(delta, a * Time.fixedDeltaTime);

        Vector3 newHoriz = horiz + change;
        rb.linearVelocity = new Vector3(newHoriz.x, vel.y, newHoriz.z);
    }

    protected void MoveFree(Vector3 desiredDir3D)
    {
        Vector3 vel = rb.linearVelocity;
        Vector3 desired = desiredDir3D * maxSpeed;

        Vector3 delta = desired - vel;
        float a = (desired.sqrMagnitude >= vel.sqrMagnitude) ? acceleration : decel;
        Vector3 change = Vector3.ClampMagnitude(delta, a * Time.fixedDeltaTime);

        rb.linearVelocity = vel + change;
    }

    public void SetTarget(Transform t) => target = t;

    public float GetStopDistance() => stopDistance;
    public void SetStopDistance(float v) => stopDistance = Mathf.Max(0f, v);


#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()
    {
        if (!enableRoam) return;

        // draw circle at spawn position if in play mode,
        // otherwise at current transform position in editor
        Vector3 center = Application.isPlaying ? spawn : transform.position;

        // roam radius
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.25f);
        Gizmos.DrawWireSphere(center, roamRadius);

        // arrive radius at current target
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(roamTarget, arriveRadius);
        }
    }
#endif

}
