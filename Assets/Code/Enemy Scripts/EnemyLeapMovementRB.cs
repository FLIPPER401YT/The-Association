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
        GroundCheckAndGravity();

        if (!target) { BrakeToStop(); return; }

        Vector3 to = target.position - transform.position;
        float dist = to.magnitude;

        if(stopDistance > 0f && dist <= stopDistance)
        {
            BrakeToStop();
            if (dist > 0.001f) Face(new Vector3(to.x, 0f, to.z).normalized);
            return;
        }

        if (controlLocked) return;

        cooldownTimer += Time.fixedDeltaTime;

        bool canLeap = dist >= minLeapDist && dist <= maxLeapDist && cooldownTimer >= leapCooldown;

        if (canLeap)
        {
            DoLeap(to);
            return;
        }

        Vector3 dirXZ = new Vector3(to.x, 0f, to.z);
        if(dirXZ.sqrMagnitude > 0.0001f)
        {
            Vector3 dir = ApplyAvoidance(dirXZ.normalized);
            Face(dir);
            MoveHorizontal(dir);
        }
        else
        {
            BrakeToStop();
        }
        
    }

    void DoLeap(Vector3 toTarget)
    {
        cooldownTimer = 0f;
        controlLocked = true;

        // Face the target on the flat plane
        Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);
        if (flat.sqrMagnitude > 0.0001f) Face(flat.normalized);

        // Compose leap direction with upward bias
        Vector3 dir = toTarget.normalized;
        dir.y += upwardBoost;
        dir.Normalize();

        // Zero downward velocity to make leap arcs consistent
        Vector3 v = rb.linearVelocity;
        if (v.y < 0f) v.y = 0f;
        rb.linearVelocity = v;

        // Impulse jump toward target
        rb.AddForce(dir * leapForce, ForceMode.Impulse);

        // Unlock after a short delay
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
