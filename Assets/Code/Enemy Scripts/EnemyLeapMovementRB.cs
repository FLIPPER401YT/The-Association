using UnityEngine;

public class EnemyLeapMovementRB : EnemyMovementBaseRB
{
    [Header("Leap Settings")]
    [SerializeField] float minLeapDist;
    [SerializeField] float maxLeapDist;
    [SerializeField] float leapForce;
    [SerializeField] float upwardBoost;
    [SerializeField] float leapCooldown;
    [SerializeField] float controlLockDuration;

    [Header("Leap Ballistics")]
    [SerializeField] float minAirTime;   
    [SerializeField] float maxAirTime;   
    [SerializeField] float maxLeapSpeed;    

    [Header("Grounding")]
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float gravity;
    [SerializeField] float groundCheckRadius;
    [SerializeField] float groundCheckOffset;

    float cooldownTimer;
    bool controlLocked;
    bool grounded;

    protected override void TickMovement()
    {
        anim.SetBool("Running", true);

        GroundCheckAndGravity();

        if (!target) { BrakeToStop(); return; }

        Vector3 to = target.position - transform.position;
        float dist = new Vector3(to.x, 0f, to.z).magnitude;

        // stop if close
        if (stopDistance > 0f && dist <= stopDistance)
        {
            anim.SetBool("Running", false);

            BrakeToStop();
            if (dist > 0.001f) Face(new Vector3(to.x, 0f, to.z).normalized);
            return;
        }

        // during leap, let physics carry it
        if (controlLocked) return;

        cooldownTimer += Time.fixedDeltaTime;

        bool inLeapWindow = dist >= minLeapDist && dist <= maxLeapDist;
        bool canLeap = inLeapWindow && cooldownTimer >= leapCooldown;

        if (canLeap)
        {
            anim.SetBool("Running", false);
            anim.SetTrigger("Leap");

            DoBallisticLeap(target.position);
            return;
        }

        // default chase (planar)
        Vector3 dirXZ = new Vector3(to.x, 0f, to.z);
        if (dirXZ.sqrMagnitude > 0.0001f)
        {
            Vector3 dir = ApplyAvoidance(dirXZ.normalized);
            Face(dir);
            MoveHorizontal(dir);
        }
        else
        {
            anim.SetBool("Running", false);

            BrakeToStop();
        }
    }

    void DoBallisticLeap(Vector3 targetPos)
    {
        cooldownTimer = 0f;
        controlLocked = true;

        // snapshot pos
        Vector3 p0 = transform.position;
        Vector3 pT = targetPos;

        // deltas
        Vector3 to = pT - p0;
        Vector3 toXZ = new Vector3(to.x, 0f, to.z);
        float dXZ = toXZ.magnitude;
        float dY = to.y;

        if (dXZ < 0.01f) // basically on top�nudge forward a bit
        {
            toXZ = transform.forward;
            dXZ = 1f;
            dY = 0f;
        }

        // choose airtime based on distance within leap window
        float t = Mathf.Lerp(minAirTime, maxAirTime, Mathf.InverseLerp(minLeapDist, maxLeapDist, dXZ));
        t = Mathf.Clamp(t, minAirTime, maxAirTime);

        float g = Mathf.Abs(gravity); // gravity magnitude

        Vector3 vxz = (toXZ / dXZ) * (dXZ / t);
        float vy = (dY + 0.5f * g * t * t) / t;
        Vector3 v0 = vxz + Vector3.up * vy;

        // clamp speed
        float speed = v0.magnitude;
        if (speed > maxLeapSpeed) v0 *= (maxLeapSpeed / speed);

        // clear downward velocity for consistency
        Vector3 v = rb.linearVelocity;
        if (v.y < 0f) v.y = 0f;
        rb.linearVelocity = v;

        // face horizontal launch direction
        Vector3 faceDir = new Vector3(v0.x, 0f, v0.z);
        if (faceDir.sqrMagnitude > 0.001f) Face(faceDir.normalized);

        // set launch instantly (mass-independent)
        rb.AddForce(v0, ForceMode.VelocityChange);

        // brief lock so chase steering doesn't fight the arc
        Invoke(nameof(UnlockControl), controlLockDuration);
    }

    void UnlockControl() => controlLocked = false;

    void GroundCheckAndGravity()
    {
        grounded = Physics.SphereCast(
            transform.position + Vector3.up * 0.1f,
            groundCheckRadius,
            Vector3.down,
            out _,
            0.2f + groundCheckOffset,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        Vector3 v = rb.linearVelocity;
        if (grounded && v.y < 0f) v.y = -2f;   // stick to ground
        else v.y += gravity * Time.fixedDeltaTime;
        rb.linearVelocity = v;
    }
}
