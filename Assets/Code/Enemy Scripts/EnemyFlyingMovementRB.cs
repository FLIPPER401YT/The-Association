using UnityEngine;

public class EnemyFlyingMovementRB : EnemyMovementBaseRB
{
    [Header("Engage Ranges")]
    [SerializeField] float approachRange;
    [SerializeField] float rangeDeadZone;  // small hysteresis around approachRange

    [Header("Strafe")]
    [SerializeField] bool strafeInsideRange = true;
    [SerializeField] float strafeSpeed;
    [SerializeField] float strafeSwitchInterval;

    [Header("Flight Speeds")]
    [SerializeField] float moveSpeed;   // outside range (closing in)

    [Header("Turning / Smoothing")]
    [SerializeField] float planSmoothing;   // how fast desired horizontal dir smooths
    [SerializeField] float faceSmoothing;   // how fast facing smooths

    [Header("Altitude")]
    [SerializeField] bool lockToStartAltitude = true;      // hover at the Y you spawned at
    [SerializeField] float altitudeOffsetFromPlayer;  // else hover above/below player
    [SerializeField] float altitudeLerp;              // vertical correction strength

    [Header("Extra Flying Avoidance (adds to base)")]
    [SerializeField] float avoidRadius;   // spherecast radius
    [SerializeField] float avoidWeight;  // how much to blend avoidance steering
    [SerializeField] float avoidStickTime; // keep same side briefly for smoothness

    [Header("Roam (when not engaged)")]
    [SerializeField] float roamPause;  // pause before picking new roam point
    [SerializeField] float roamSpeed;  // horizontal cruise speed while roaming

    [Header("Engage Hysteresis (multiplies approachRange)")]
    [SerializeField] float engageEnterFactor;  // enter engage just inside approachRange
    [SerializeField] float engageExitFactor;  // leave engage a bit outside

    // --- runtime state ---
    Vector3 smoothPlanarDir;   // smoothed horizontal flight direction
    Vector3 smoothFaceDir;     // smoothed facing direction
    float startY;

    bool engaged;
    float strafeTimer;
    int strafeSign = 1;

    // roam state
    Vector3 flyRoamCenter;
    Vector3 flyRoamTarget;
    float flyRoamTimer;
    bool hasFlyRoamTarget;

    // avoidance memory
    int avoidSign = 0;
    float avoidStickTimer = 0f;

    protected override void Awake()
    {
        base.Awake();

        // Flyers don't use gravity; base already freezes rotation for scripted turning.
        rb.useGravity = false;

        startY = transform.position.y;
        flyRoamCenter = transform.position;
        PickNewFlyRoamTarget();

        // init smoothing dirs
        smoothPlanarDir = transform.forward; smoothPlanarDir.y = 0f;
        if (smoothPlanarDir.sqrMagnitude < 0.001f) smoothPlanarDir = Vector3.forward;
        smoothFaceDir = smoothPlanarDir;
    }

    // Flyers: no gravity.
    protected override void ApplyGravity() { }

    // When no target, use our flying roam (instead of the ground roam in base)
    protected override void NoTargetStep()
    {
        FlyingRoamStep();
    }

    protected override void TickMovement()
    {
        if (!target) { FlyingRoamStep(); return; }

        // planar distance to target
        Vector3 toPlayer = target.position - transform.position; toPlayer.y = 0f;
        float distXZ = toPlayer.magnitude;

        // engage hysteresis
        float enterD = approachRange * Mathf.Max(0.1f, engageEnterFactor);
        float exitD = approachRange * Mathf.Max(0.1f, engageExitFactor);

        if (engaged)
        {
            if (distXZ >= exitD) engaged = false;
        }
        else
        {
            if (distXZ <= enterD) engaged = true;
        }

        // Decide desired horizontal direction & speed
        Vector3 targetPlanarDir = Vector3.zero;
        float desiredSpeed = 0f;

        if (!engaged)
        {
            // roaming flight (XZ) with pause
            if (!hasFlyRoamTarget || Vector3.Distance(transform.position, flyRoamTarget) <= arriveRadius)
            {
                flyRoamTimer += Time.fixedDeltaTime;
                if (flyRoamTimer >= roamPause)
                {
                    flyRoamTimer = 0f;
                    PickNewFlyRoamTarget();
                }
            }

            Vector3 toTarget = flyRoamTarget - transform.position; toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.001f)
            {
                targetPlanarDir = toTarget.normalized;
                desiredSpeed = roamSpeed;
            }
        }
        else
        {
            // approach outside range, strafe when inside
            if (distXZ > approachRange + rangeDeadZone)
            {
                targetPlanarDir = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : smoothPlanarDir;
                desiredSpeed = moveSpeed;
            }
            else if (strafeInsideRange && distXZ > 0.001f)
            {
                Vector3 tangent = Vector3.Cross(Vector3.up, toPlayer.normalized) * strafeSign;
                targetPlanarDir = tangent;
                desiredSpeed = strafeSpeed;

                strafeTimer += Time.fixedDeltaTime;
                if (strafeTimer >= strafeSwitchInterval)
                {
                    strafeTimer = 0f;
                    strafeSign *= -1;
                }
            }
            else
            {
                // hold position-ish: gently face & hover
                targetPlanarDir = Vector3.zero;
                desiredSpeed = 0f;
            }
        }

        // smoothing for plan direction
        if (targetPlanarDir.sqrMagnitude < 0.0001f) targetPlanarDir = smoothPlanarDir;
        smoothPlanarDir = Vector3.Slerp(smoothPlanarDir, targetPlanarDir.normalized, Time.fixedDeltaTime * planSmoothing);
        smoothPlanarDir.y = 0f;
        if (smoothPlanarDir.sqrMagnitude < 0.0001f) smoothPlanarDir = targetPlanarDir;

        // altitude control
        float targetY = lockToStartAltitude ? startY : (target.position.y + altitudeOffsetFromPlayer);
        float vy = Mathf.Clamp((targetY - transform.position.y) * altitudeLerp, -acceleration, acceleration);

        // light forward avoidance (steer added in XZ) – uses base obstacleMask/avoidProbeDist
        Vector3 avoid = AvoidXZ(transform.position, smoothPlanarDir, desiredSpeed) * avoidWeight;

        // final wanted velocity vector
        Vector3 horiz = smoothPlanarDir * desiredSpeed + avoid;
        Vector3 wantedVel = new Vector3(horiz.x, vy, horiz.z);

        // Use base helper to move in 3D with accel/decel limits
        float speedCap = Mathf.Max(0.01f, desiredSpeed);
        MoveFree(wantedVel.normalized * Mathf.Min(maxSpeed > 0 ? maxSpeed : speedCap, wantedVel.magnitude));

        // Face the player (or roam direction if not engaged)
        Vector3 faceDir = engaged
            ? (target.position - transform.position)
            : (hasFlyRoamTarget ? (flyRoamTarget - transform.position) : smoothPlanarDir);

        faceDir.y = 0f; if (faceDir.sqrMagnitude < 0.0001f) faceDir = smoothPlanarDir;
        smoothFaceDir = Vector3.Slerp(smoothFaceDir, faceDir.normalized, Time.fixedDeltaTime * faceSmoothing);
        Face(smoothFaceDir);
    }

    // --- Flying roam used when no target ---
    void FlyingRoamStep()
    {
        if (!hasFlyRoamTarget || Vector3.Distance(transform.position, flyRoamTarget) <= arriveRadius)
        {
            flyRoamTimer += Time.fixedDeltaTime;
            if (flyRoamTimer >= roamPause)
            {
                flyRoamTimer = 0f;
                PickNewFlyRoamTarget();
            }
        }

        Vector3 to = flyRoamTarget - transform.position; to.y = 0f;
        Vector3 dir = to.sqrMagnitude > 0.01f ? to.normalized : Vector3.zero;

        // smooth plan, compute altitude
        smoothPlanarDir = Vector3.Slerp(
            smoothPlanarDir,
            (dir == Vector3.zero ? smoothPlanarDir : dir),
            Time.fixedDeltaTime * planSmoothing
        );

        float targetY = lockToStartAltitude ? startY : transform.position.y; // hold current if no target
        float vy = Mathf.Clamp((targetY - transform.position.y) * altitudeLerp, -acceleration, acceleration);

        // gentle avoidance
        Vector3 avoid = AvoidXZ(transform.position, smoothPlanarDir, roamSpeed) * avoidWeight;

        Vector3 horiz = smoothPlanarDir * roamSpeed + avoid;
        Vector3 wanted = new Vector3(horiz.x, vy, horiz.z);

        float speedCap = Mathf.Max(0.01f, roamSpeed);
        MoveFree(wanted.normalized * Mathf.Min(maxSpeed > 0 ? maxSpeed : speedCap, wanted.magnitude));

        // face roam direction
        Vector3 faceDir = (flyRoamTarget - transform.position); faceDir.y = 0f;
        if (faceDir.sqrMagnitude < 0.0001f) faceDir = smoothPlanarDir;
        smoothFaceDir = Vector3.Slerp(smoothFaceDir, faceDir.normalized, Time.fixedDeltaTime * faceSmoothing);
        Face(smoothFaceDir);
    }

    // XZ avoidance w/ stickiness, using base obstacleMask & avoidProbeDist
    Vector3 AvoidXZ(Vector3 origin, Vector3 probeDir, float maxMag)
    {
        probeDir.y = 0f;
        if (probeDir.sqrMagnitude < 0.0001f)
        {
            // decay stick
            avoidStickTimer = Mathf.Max(0f, avoidStickTimer - Time.fixedDeltaTime);
            if (avoidStickTimer <= 0f) avoidSign = 0;
            return Vector3.zero;
        }

        Vector3 dir = probeDir.normalized;
        Vector3 start = origin + dir * (avoidRadius + 0.05f);

        if (Physics.SphereCast(start, avoidRadius, dir, out var hit, avoidProbeDist, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            // ignore self
            if (hit.rigidbody == rb || hit.transform == transform || hit.transform.IsChildOf(transform))
                return Vector3.zero;

            // choose a side once and stick briefly for smoothness
            if (avoidStickTimer <= 0f || avoidSign == 0)
            {
                float signed = Vector3.SignedAngle(dir, hit.normal, Vector3.up);
                avoidSign = (signed >= 0f) ? -1 : +1;
                if (avoidSign == 0) avoidSign = 1;
                avoidStickTimer = avoidStickTime;
            }
            else
            {
                avoidStickTimer -= Time.fixedDeltaTime;
            }

            Vector3 tangent = Vector3.Cross(Vector3.up, dir) * avoidSign;
            float mag = Mathf.Min(maxMag, moveSpeed);
            return tangent.normalized * mag;
        }
        else
        {
            // decay stick when clear
            avoidStickTimer = Mathf.Max(0f, avoidStickTimer - Time.fixedDeltaTime);
            if (avoidStickTimer <= 0f) avoidSign = 0;
            return Vector3.zero;
        }
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Approach band
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, approachRange);

        // Inside/strafe hint
        Gizmos.color = new Color(1f, 0.25f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, approachRange - rangeDeadZone));
    }
#endif

    // --- Roam picker for flyers (XZ), uses base obstacleMask/roamRadius ---
    void PickNewFlyRoamTarget()
    {
        for (int i = 0; i < 6; i++)
        {
            Vector3 offset = Random.insideUnitSphere * roamRadius; offset.y = 0f;
            Vector3 p = flyRoamCenter + offset;

            Vector3 dir = (p - transform.position); dir.y = 0f;
            if (dir.sqrMagnitude < 1f) continue;

            if (!Physics.SphereCast(transform.position, avoidRadius, dir.normalized, out _, dir.magnitude, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                flyRoamTarget = p;
                hasFlyRoamTarget = true;
                return;
            }
        }
        flyRoamTarget = flyRoamCenter;
        hasFlyRoamTarget = true;
    }
}
