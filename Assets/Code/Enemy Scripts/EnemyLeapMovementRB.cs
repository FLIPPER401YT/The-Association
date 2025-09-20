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

    // --- Audio ---
    [Header("Audio")]
    [SerializeField] private AudioSource sfx;              // assign a 3D AudioSource 
    [SerializeField] private AudioClip[] chaseFootsteps;   // footsteps to use during chase
    [SerializeField] private float stepInterval;   // seconds between steps at normal run
    [SerializeField] private float stepVolume;

    [Space(6)]
    [SerializeField] private AudioClip[] leapWindupClips;  // play before leap
    [SerializeField] private AudioClip[] leapLaunchClips;  // play on takeoff
    [SerializeField] private AudioClip[] leapLandClips;    // play on landing
    [SerializeField] private float leapVolume;
    [SerializeField] private float pitchJitter;    

    float cooldownTimer;
    bool controlLocked;
    bool grounded;

    // audio/runtime helpers
    float stepTimer;
    bool inLeap;            // currently in the leap arc
    bool lastGrounded;      // for landing detection

    protected override void TickMovement()
    {
        anim.SetBool("Running", true);

        // update grounded + gravity, also handles landing sound
        GroundCheckAndGravity();

        if (!target) { BrakeToStop(); return; }

        Vector3 to = target.position - transform.position;
        float dist = new Vector3(to.x, 0f, to.z).magnitude;

        // stop if close
        if (stopDistance > 0f && dist <= stopDistance)
        {
            anim.SetBool("Running", false);
            ResetFootsteps();
            BrakeToStop();
            if (dist > 0.001f) Face(new Vector3(to.x, 0f, to.z).normalized);
            return;
        }

        // during leap, let physics carry it (no steering / no footsteps)
        if (controlLocked) return;

        cooldownTimer += Time.fixedDeltaTime;

        bool inLeapWindow = dist >= minLeapDist && dist <= maxLeapDist;
        bool canLeap = inLeapWindow && cooldownTimer >= leapCooldown;

        if (canLeap)
        {
            anim.SetBool("Running", false);
            ResetFootsteps();
            PlayOneShotRandom(leapWindupClips, leapVolume);   // windup (optional)
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

            // footsteps while running
            PlayFootstepsIfMoving();
        }
        else
        {
            anim.SetBool("Running", false);
            ResetFootsteps();
            BrakeToStop();
        }
    }

    void DoBallisticLeap(Vector3 targetPos)
    {
        cooldownTimer = 0f;
        controlLocked = true;
        inLeap = true; // mark airborne leap

        // snapshot pos
        Vector3 p0 = transform.position;
        Vector3 pT = targetPos;

        // deltas
        Vector3 to = pT - p0;
        Vector3 toXZ = new Vector3(to.x, 0f, to.z);
        float dXZ = toXZ.magnitude;
        float dY = to.y;

        if (dXZ < 0.01f) // basically on top — nudge forward a bit
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

        // play launch SFX
        PlayOneShotRandom(leapLaunchClips, leapVolume);

        // brief lock so chase steering doesn't fight the arc
        Invoke(nameof(UnlockControl), controlLockDuration);
    }

    void UnlockControl() => controlLocked = false;

    void GroundCheckAndGravity()
    {
        // cache last grounded for landing detect
        bool wasGrounded = grounded;

        grounded = Physics.SphereCast(
            transform.position + Vector3.up * 0.1f,
            groundCheckRadius,
            Vector3.down,
            out _,
            0.2f + groundCheckOffset,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        // landing sound: only when we were in a leap and just touched ground
        if (inLeap && grounded && !wasGrounded)
        {
            inLeap = false;
            PlayOneShotRandom(leapLandClips, leapVolume);
        }

        Vector3 v = rb.linearVelocity;
        if (grounded && v.y < 0f) v.y = -2f;   // stick to ground
        else v.y += gravity * Time.fixedDeltaTime;
        rb.linearVelocity = v;

        lastGrounded = grounded;
    }

    // --- footsteps during chase ---
    void PlayFootstepsIfMoving()
    {
        if (!sfx || chaseFootsteps == null || chaseFootsteps.Length == 0) return;

        // planar speed threshold
        Vector3 v = rb.linearVelocity;
        v.y = 0f;
        if (v.sqrMagnitude < 0.04f) { stepTimer = 0f; return; } // too slow, reset timer

        stepTimer += Time.fixedDeltaTime;
        if (stepTimer >= stepInterval)
        {
            stepTimer = 0f;
            PlayOneShotRandom(chaseFootsteps, stepVolume);
        }
    }

    void ResetFootsteps() => stepTimer = 0f;

    // --- tiny audio helper used everywhere ---
    void PlayOneShotRandom(AudioClip[] bank, float vol)
    {
        if (!sfx || bank == null || bank.Length == 0) return;

        var clip = bank[Random.Range(0, bank.Length)];
        float original = sfx.pitch;
        sfx.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        sfx.PlayOneShot(clip, vol);
        sfx.pitch = original;
    }
}
