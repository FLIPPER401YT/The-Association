using UnityEngine;
using System.Collections;

public class MothmanBoss : Base_Boss_AI
{
    [Header("Flight")]
    [SerializeField] float cruiseAltitude;
    [SerializeField] float altitudeLerp;
    [SerializeField] float verticalAccel;

    [Header("Ranges")]
    [SerializeField] float rangedRange;          // Spit Bolt
    [SerializeField] float swoopTriggerRange;

    [Header("Ranged: Spit Bolt")]
    [SerializeField] GameObject spitBoltPrefab;  // has Rigidbody + trigger collider + SpitBoltProjectile
    [SerializeField] Transform castMuzzle;
    [SerializeField] float boltSpeed;
    [SerializeField] float boltDamage;
    [SerializeField] float rangedCooldown;

    [Header("Swoop Melee")]
    [SerializeField] float diveSpeed;
    [SerializeField] float diveTime;
    [SerializeField] float clawDamage;
    [SerializeField] float clawRadius;
    [SerializeField] float postSwoopRecover;

    // explicit attack position support
    [Tooltip("If set, swoop uses this position instead of the offset below.")]
    [SerializeField] Transform swoopAttackPos;
    [Tooltip("Local-space offset used when no attack Transform is assigned.")]
    [SerializeField] Vector3 clawOffset = new Vector3(0f, -0.2f, 1.2f);
    [Tooltip("Layers the claw can hit (set to Player for safety).")]
    [SerializeField] LayerMask meleeHitMask = ~0;

    [Header("Blinding Shriek (No Damage)")]
    [SerializeField] float blindRadius;     // “you heard it, you’re blinded”
    [SerializeField] float blindDuration;
    [SerializeField] float blindCooldown;
    [SerializeField] AudioClip shriekClip;
    [SerializeField, Range(0f, 1f)] float shriekVolume = 1f;

    [Header("Audio")]
    [SerializeField] AudioSource sfx;                    // assign in inspector (3D, routed to your mixer)
    [SerializeField, Tooltip("Small random pitch variance per shriek.")]
    float shriekPitchJitter = 0.05f;

    // Random choice weight when both melee & ranged are valid
    [Header("Attack Selection")]
    [SerializeField, Range(0f, 1f)]
    float meleeBiasWhenBothValid = 0.6f; // e.g. 60% chance to favor Swoop if both valid

    float rangedCD;
    float blindCD;

    [Header("Separation")]
    [SerializeField] float minPersonalSpace = 3f;
    [SerializeField] float separationSpeed = 6f;
    [SerializeField] bool keepSpaceWhileAttacking = true;

    [Header("Debug")]
    [SerializeField] bool logAttacks = true;

    [Header("Debug Gizmos")]
    [SerializeField] int ringSegments = 24;

    protected override void Awake()
    {
        base.Awake();
        if (rb) rb.useGravity = false; // always flying
        if (!sfx) sfx = GetComponent<AudioSource>(); // convenience fallback
    }

    // ------------ Flying movement overrides ------------
    protected override Vector3 DesiredRoamVelocity()
    {
        Vector3 targetXZ = roamTarget; targetXZ.y = spawn.y + cruiseAltitude;
        Vector3 to = targetXZ - transform.position;
        Vector3 planar = new Vector3(to.x, 0f, to.z);
        Vector3 vel = planar.sqrMagnitude > 0.1f ? planar.normalized * maxSpeed : Vector3.zero;

        float targetY = spawn.y + cruiseAltitude;
        float dy = targetY - transform.position.y;
        float vy = Mathf.Clamp(dy * altitudeLerp, -verticalAccel, verticalAccel);

        return new Vector3(vel.x, vy, vel.z);
    }

    protected override Vector3 DesiredChaseVelocity()
    {
        if (!player) return Vector3.zero;
        Vector3 to = (player.position - transform.position);
        Vector3 planar = new Vector3(to.x, 0f, to.z).normalized * chaseSpeed;

        float targetY = Mathf.Max(spawn.y + cruiseAltitude, player.position.y + 2.0f);
        float dy = targetY - transform.position.y;
        float vy = Mathf.Clamp(dy * altitudeLerp, -verticalAccel, verticalAccel);

        return new Vector3(planar.x, vy, planar.z);
    }

    protected override void FixedUpdate()
    {
        // tick local cooldowns
        float dt = Time.fixedDeltaTime;
        rangedCD -= dt; blindCD -= dt;

        // keep a little horizontal (XZ) distance from the player
        MaintainPersonalSpace();

        base.FixedUpdate();
    }

    protected override void AttackStepWhileActive()
    {
        Vector3 v = GetPlanarVel();
        if (v.sqrMagnitude > 0.0001f)
        {
            Quaternion q = Quaternion.LookRotation(v, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.fixedDeltaTime * (turnLerp + 4f));
        }
    }

    protected override bool CanAttack(float dist)
    {
        // allow considering an attack if within outer ranged band OR Shriek is ready
        return dist <= rangedRange + 2f || blindCD <= 0f;
    }

    // Priority + Random:
    // 1. Shriek when off cooldown
    // 2. If both melee and ranged valid -> random pick with melee bias
    // 3. Else whichever is valid
    protected override IEnumerator PickAndRunAttack(float distToPlayer)
    {
        if (blindCD <= 0f)
        {
            if (logAttacks) Debug.Log($"[Mothman] Pick: Shriek (dist {distToPlayer:F2})");
            yield return StartCoroutine(DoBlindingShriek());
            yield break;
        }

        bool inSwoopRange = distToPlayer <= swoopTriggerRange;
        bool canRanged = (rangedCD <= 0f) && (distToPlayer <= rangedRange);

        if (inSwoopRange && canRanged)
        {
            bool pickMelee = Random.value < meleeBiasWhenBothValid;
            if (logAttacks) Debug.Log($"[Mothman] Pick (both valid): {(pickMelee ? "Swoop" : "SpitBolt")}  dist={distToPlayer:F2}");
            if (pickMelee) yield return StartCoroutine(DoSwoopClaw());
            else yield return StartCoroutine(DoSpitBolt());
            yield break;
        }
        else if (inSwoopRange)
        {
            if (logAttacks) Debug.Log($"[Mothman] Pick: Swoop (dist {distToPlayer:F2})");
            yield return StartCoroutine(DoSwoopClaw());
            yield break;
        }
        else if (canRanged)
        {
            if (logAttacks) Debug.Log($"[Mothman] Pick: SpitBolt (dist {distToPlayer:F2})");
            yield return StartCoroutine(DoSpitBolt());
            yield break;
        }

        // Otherwise keep chasing this tick
        yield return null;
    }

    // ---------------- Attacks ----------------

    IEnumerator DoSpitBolt()
    {
        if (logAttacks) Debug.Log("[Mothman] Start SpitBolt");

        // small hover brake + face
        FacePlayer();
        rb.AddForce(-GetPlanarVel(), ForceMode.VelocityChange);

        if (rangedCD <= 0f && spitBoltPrefab && castMuzzle && player)
        {
            Vector3 dir = (player.position + Vector3.up * 1.2f - castMuzzle.position).normalized;
            var go = Instantiate(spitBoltPrefab, castMuzzle.position, Quaternion.LookRotation(dir, Vector3.up));

            // initial velocity (keep your preferred API)
            var rbProj = go.GetComponent<Rigidbody>();
            if (rbProj) rbProj.linearVelocity = dir * boltSpeed;

            // configure projectile
            var proj = go.GetComponent<SpitBoltProjectile>();
            if (proj)
            {
                proj.damage = boltDamage;
                proj.lifetime = 6f;
                proj.hitMask = ~0;         // tune if needed
                proj.player = player;      // optional gentle homing
                proj.owner = transform;    // so it won't hit the boss
            }

            rangedCD = rangedCooldown;
            if (logAttacks) Debug.Log("[Mothman] SpitBolt fired");
        }

        yield return null;
    }

    IEnumerator DoSwoopClaw()
    {
        if (logAttacks) Debug.Log("[Mothman] Start Swoop");

        float t = 0f;

        // brief rise for telegraph
        float riseTime = 0.25f;
        while (t < riseTime)
        {
            t += Time.fixedDeltaTime;
            rb.AddForce(Vector3.up * verticalAccel, ForceMode.Acceleration);
            yield return new WaitForFixedUpdate();
        }

        // Deal damage at most once for this entire swoop
        bool dealtDamageThisSwoop = false;

        // Dive window
        t = 0f;
        while (t < diveTime)
        {
            t += Time.fixedDeltaTime;

            Vector3 to = (player.position - transform.position); to.y = 0f;
            Vector3 desired = (to.sqrMagnitude > 0.1f ? to.normalized : transform.forward) * diveSpeed;
            Vector3 accel = Vector3.ClampMagnitude(desired - GetPlanarVel(), maxAccel * 1.2f);
            rb.AddForce(new Vector3(accel.x, -verticalAccel, accel.z), ForceMode.Acceleration);

            // Damage bubble at claws (uses explicit attack point)
            if (!dealtDamageThisSwoop)
            {
                Vector3 center = swoopAttackPos
                    ? swoopAttackPos.position
                    : transform.position + transform.TransformVector(clawOffset);

                var hits = Physics.OverlapSphere(center, clawRadius, meleeHitMask, QueryTriggerInteraction.Ignore);
                foreach (var h in hits)
                {
                    // skip self/same-rigidbody
                    if (h.transform == transform || h.transform.IsChildOf(transform)) continue;
                    if (h.attachedRigidbody && h.attachedRigidbody == rb) continue;

                    // only hit the player hierarchy (optional guard)
                    if (player && !(h.transform == player || h.transform.IsChildOf(player))) continue;

                    var dmg = FindDamage(h);
                    if (dmg != null)
                    {
                        dmg.TakeDamage((int)clawDamage);
                        if (logAttacks) Debug.Log($"[Mothman] Swoop HIT for {clawDamage} at {center}");
                        dealtDamageThisSwoop = true;
                        break;
                    }
                }
            }

            ClampPlanarSpeed(diveSpeed);
            yield return new WaitForFixedUpdate();
        }

        if (logAttacks) Debug.Log("[Mothman] End Swoop (recover)");
        yield return new WaitForSeconds(postSwoopRecover);
    }

    IEnumerator DoBlindingShriek()
    {
        if (logAttacks) Debug.Log("[Mothman] Start Shriek");

        // Play sound from attached AudioSource instead of one-shot
        if (sfx && shriekClip)
        {
            sfx.clip = shriekClip;
            sfx.volume = shriekVolume;
            sfx.pitch = 1f + Random.Range(-shriekPitchJitter, shriekPitchJitter);
            sfx.Play();
        }

        // brief windup hover
        FacePlayer();
        rb.AddForce(-GetPlanarVel(), ForceMode.VelocityChange);
        yield return new WaitForSeconds(0.25f);

        // Global AOE via distance (line-of-sight ignored)
        Vector3 center = transform.position;
        var hits = Physics.OverlapSphere(center, blindRadius, ~0, QueryTriggerInteraction.Ignore);

        foreach (var h in hits)
        {
            if (player && (h.transform == player || h.transform.IsChildOf(player)))
            {
                var status = player.GetComponentInChildren<StatusEffects>();
                if (status) status.ApplyBlind(blindDuration);
                if (logAttacks) Debug.Log($"[Mothman] Shriek applied BLIND for {blindDuration:F2}s");
            }
        }

        blindCD = blindCooldown;
        if (logAttacks) Debug.Log("[Mothman] End Shriek");
        yield return new WaitForSeconds(0.2f);
    }

    // ---------------- Helpers ----------------
    void MaintainPersonalSpace()
    {
        if (!keepSpaceWhileAttacking || !player) return;

        // keep a buffer on XZ only; vertical handled by flight controller already
        Vector3 playerPos = player.position;
        Vector3 delta = transform.position - playerPos;
        delta.y = 0f;
        float dist = delta.magnitude;

        if (dist < 0.001f) delta = -transform.forward; // fallback
        if (dist >= minPersonalSpace) return;

        Vector3 outward = delta.normalized;
        Vector3 targetXZ = playerPos + outward * minPersonalSpace;

        Vector3 cur = transform.position;
        Vector3 newPos = Vector3.MoveTowards(
            new Vector3(cur.x, cur.y, cur.z),
            new Vector3(targetXZ.x, cur.y, targetXZ.z),
            separationSpeed * Time.fixedDeltaTime
        );

        if (rb) rb.MovePosition(newPos);
        else transform.position = newPos;
    }

    static IDamage FindDamage(Component c)
        => c.GetComponentInParent<IDamage>() ?? c.GetComponentInChildren<IDamage>();

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        if (drawGizmos)
        {
            // Cruise plane (use spawn if running; transform if editing)
            float baseY = Application.isPlaying ? spawn.y : transform.position.y;
            float cruiseY = baseY + cruiseAltitude;

            // Ranged range
            Gizmos.color = new Color(1f, 0.85f, 0f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, rangedRange);

            // Swoop range
            Gizmos.color = new Color(1f, 0.25f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, swoopTriggerRange);

            // Shriek radius
            Gizmos.color = new Color(0.2f, 1f, 1f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, blindRadius);

            // Claw bubble (attack pos or offset)
            Gizmos.color = new Color(1f, 0.4f, 0.8f, 0.8f);
            Vector3 clawCenter = swoopAttackPos
                ? swoopAttackPos.position
                : transform.position + transform.TransformVector(clawOffset);
            Gizmos.DrawWireSphere(clawCenter, clawRadius);

            // Cruise altitude XZ ring
            Gizmos.color = new Color(0.75f, 0.75f, 1f, 0.9f);
            DrawCircleXZ(new Vector3(transform.position.x, cruiseY, transform.position.z),
                         Mathf.Max(rangedRange, swoopTriggerRange, 3f), ringSegments);

            // Muzzle helper
            if (castMuzzle)
            {
                Gizmos.color = new Color(0.9f, 0.9f, 0.9f, 0.7f);
                Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, castMuzzle.position);
                Gizmos.DrawWireSphere(castMuzzle.position, 0.1f);
            }

            // Personal space radius
            Gizmos.color = new Color(0f, 0.6f, 1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, minPersonalSpace);
        }

        // Keep base class gizmos (roam ring/target, aggro)
        base.OnDrawGizmosSelected();
    }

    void DrawCircleXZ(Vector3 center, float radius, int segments)
    {
        if (segments < 4) segments = 4;
        float step = Mathf.PI * 2f / segments;
        Vector3 prev = center + new Vector3(Mathf.Cos(0f) * radius, 0f, Mathf.Sin(0f) * radius);
        for (int i = 1; i <= segments; i++)
        {
            float a = i * step;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif
}
