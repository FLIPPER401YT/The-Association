using UnityEngine;

public class EnemyChaseMovementRB : EnemyMovementBaseRB
{

    [Header("Grounding")]
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float gravity;
    [SerializeField] float groundCheckRadius;
    [SerializeField] float groundCheckOffset;

    bool grounded;

    protected override void TickMovement()
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
        

        Vector3 to = target.position - transform.position;
        to.y = 0f;

        float dist = to .magnitude;
        if(stopDistance > 0f && dist <= stopDistance )
        {
            BrakeToStop();
            if (dist > 0.001f) Face(to.normalized);
            ApplyGravity();
            return;
        }

        if(dist <= 0.001f)
        {
            BrakeToStop();
            ApplyGravity();
            return;
        }

        Vector3 dir = ApplyAvoidance(to.normalized);
        Face(dir);
        MoveHorizontal(dir);
        ApplyGravity();

    }

    void ApplyGravity()
    {
        Vector3 v = rb.linearVelocity;
        if (grounded && v.y < 0f) v.y = -2f;
        else v.y += gravity * Time.fixedDeltaTime;
        rb.linearVelocity = v;
    }
}
