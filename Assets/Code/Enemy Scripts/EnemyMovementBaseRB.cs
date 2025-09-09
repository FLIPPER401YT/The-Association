using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class EnemyMovementBaseRB : MonoBehaviour
{
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

    protected Rigidbody rb;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation; // rotate via code
    }

    protected virtual void FixedUpdate()
    {
        if (!target) { BrakeToStop(); return; }
        TickMovement(); // child class drives movement each physics step
    }

    // Child movers implement their behavior here
    protected abstract void TickMovement();

    // Slerp face toward a direction
    protected void Face(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime));
    }

    // Gradually reduce current velocity to zero.
    protected void BrakeToStop()
    {
        Vector3 v = rb.linearVelocity;
        float mag = v.magnitude;
        float drop = decel * Time.fixedDeltaTime;
        rb.linearVelocity = (mag <= drop) ? Vector3.zero : v * ((mag - drop) / Mathf.Max(mag, 0.0001f));
    }

    // Naive raycast-based avoidance, returns adjusted direction.
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

    // Accelerate toward desired horizontal (XZ) direction, keeps current Y velocity
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

    // Accelerate in full 3D
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
}
