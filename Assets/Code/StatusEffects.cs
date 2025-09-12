using UnityEngine;
using System;
using System.Collections;

[DisallowMultipleComponent]
public class StatusEffects : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] bool showDebug = true;

    [Header("Components (optional)")]
    [Tooltip("If left empty, the script will try GetComponent at runtime.")]
    [SerializeField] Rigidbody rb;                  // optional; auto-fetched
    [SerializeField] CharacterController controller; // optional; auto-fetched

    [Header("Knockback Tuning")]
    [Tooltip("Default vertical fraction of knockback impulse (0 = flat, 0.15 = small hop).")]
    [Range(0f, 0.5f)] public float defaultUpFrac = 0.15f;

    [Tooltip("How long the CharacterController knockback 'burst' lasts.")]
    public float ccSlideDuration = 0.25f;

    [Tooltip("How quickly the knockback velocity damps (bigger = stops sooner).")]
    public float damping = 10f;

    [Tooltip("If true, sets RB to ContinuousDynamic during knockback to avoid tunnelling.")]
    public bool rbContinuous = true;

    // ---------------- STUN ----------------
    public bool IsStunned => stunTimer > 0f;
    public event Action<bool> OnStunChanged; // true = stunned, false = unstunned

    float stunTimer;

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!controller) controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // tick stun
        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                stunTimer = 0f;
                if (showDebug) Debug.Log($"{name} recovered from STUN");
                OnStunChanged?.Invoke(false);
            }
        }
    }

    // Apply stun (refreshes to the longer duration)
    public void ApplyStun(float duration)
    {
        bool was = IsStunned;
        stunTimer = Mathf.Max(stunTimer, duration);
        if (!was)
        {
            if (showDebug) Debug.Log($"{name} is STUNNED for {duration:0.00}s");
            OnStunChanged?.Invoke(true);
        }
    }

    public void ClearStun()
    {
        if (IsStunned && showDebug) Debug.Log($"{name} stun cleared");
        stunTimer = 0f;
        OnStunChanged?.Invoke(false);
    }

    // ------------- KNOCKBACK -------------
    /// <summary>
    /// Pushes away from origin with given strength. upFrac adds a small vertical lift.
    /// </summary>
    public void ApplyKnockback(Vector3 origin, float strength, float upFrac = -1f)
    {
        Vector3 dir = (transform.position - origin);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward; // fallback
        dir.Normalize();
        ApplyKnockbackDirection(dir, strength, upFrac);
    }

    /// <summary>
    /// Pushes in a given direction (XZ prioritized). upFrac adds a small vertical lift.
    /// </summary>
    public void ApplyKnockbackDirection(Vector3 direction, float strength, float upFrac = -1f)
    {
        if (upFrac < 0f) upFrac = defaultUpFrac;

        // mostly horizontal + small up
        Vector3 planar = new Vector3(direction.x, 0f, direction.z);
        if (planar.sqrMagnitude < 0.0001f) planar = transform.forward;
        planar.Normalize();

        Vector3 impulse = planar * strength + Vector3.up * (strength * Mathf.Clamp01(upFrac));

        // Prefer CharacterController path (collision-safe)
        if (controller && controller.enabled)
        {
            StopAllCoroutines();
            StartCoroutine(KnockbackCC(controller, impulse, ccSlideDuration));
            return;
        }

        // Rigidbody fallback
        var body = rb ? rb : GetComponent<Rigidbody>();
        if (body)
        {
            if (rbContinuous)
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.AddForce(impulse, ForceMode.Impulse);
            return;
        }

        // Last resort: gentle positional nudge (kept short to avoid clipping)
        StopAllCoroutines();
        StartCoroutine(KnockbackNudge(transform, impulse));
    }

    IEnumerator KnockbackCC(CharacterController cc, Vector3 impulse, float duration)
    {
        // Convert impulse to a short-lived velocity burst
        // Scale by 6 to feel like an impulse over ~0.25s
        Vector3 vel = impulse * 6f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            // Move handles collisions & steps; gravity handled by your controller elsewhere
            cc.Move(vel * Time.deltaTime);

            // Dampen
            vel = Vector3.Lerp(vel, Vector3.zero, damping * Time.deltaTime);
            yield return null;
        }
    }

    IEnumerator KnockbackNudge(Transform target, Vector3 impulse)
    {
        Vector3 start = target.position;
        Vector3 end = start + impulse * 0.2f; // tiny shove to avoid clipping
        float t = 0f, dur = 0.15f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float a = t / dur;
            target.position = Vector3.Lerp(start, end, a);
            yield return null;
        }
    }
}
