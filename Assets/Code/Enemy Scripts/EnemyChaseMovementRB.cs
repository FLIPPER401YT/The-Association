using UnityEngine;

public class EnemyChaseMovementRB : EnemyMovementBaseRB
{
    [Header("Grounding")]
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float gravity;
    [SerializeField] float groundCheckRadius;
    [SerializeField] float groundCheckOffset;

    [Header("Audio")]
    [SerializeField] private AudioSource sfx;           // Assign in Inspector
    [SerializeField] private AudioClip[] runStepClips;  // Running footstep clips
    [SerializeField] private float stepInterval = 0.4f; // Faster than walking
    [SerializeField] private float stepVolume = 1f;
    [SerializeField] private float pitchJitter = 0.05f; // ±5% random pitch

    bool grounded;
    float nextStepTime;

    protected override void TickMovement()
    {
        anim.SetBool("Running", true);

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

        float dist = to.magnitude;
        if (stopDistance > 0f && dist <= stopDistance)
        {
            anim.SetBool("Running", false);

            BrakeToStop();
            if (dist > 0.001f) Face(to.normalized);
            ApplyGravity();
            return;
        }

        if (dist <= 0.001f)
        {
            anim.SetBool("Running", false);

            BrakeToStop();
            ApplyGravity();
            return;
        }

        Vector3 dir = ApplyAvoidance(to.normalized);
        Face(dir);
        MoveHorizontal(dir);

        // play chase footsteps
        TryPlayRunStep();

        ApplyGravity();
    }

    protected override void ApplyGravity()
    {
        Vector3 v = rb.linearVelocity;
        if (grounded && v.y < 0f) v.y = -2f;
        else v.y += gravity * Time.fixedDeltaTime;
        rb.linearVelocity = v;
    }

    private void TryPlayRunStep()
    {
        if (!sfx || runStepClips.Length == 0 || !grounded) return;

        if (Time.time >= nextStepTime)
        {
            var clip = runStepClips[Random.Range(0, runStepClips.Length)];
            float originalPitch = sfx.pitch;
            sfx.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);

            sfx.PlayOneShot(clip, stepVolume);

            sfx.pitch = originalPitch;
            nextStepTime = Time.time + stepInterval;
        }
    }
}
