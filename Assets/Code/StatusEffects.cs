using UnityEngine;
using System;
using System.Collections;

[DisallowMultipleComponent]
public class StatusEffects : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] bool showDebug = true;

    [Header("Components (optional)")]
    [Tooltip("Auto-fetched if left empty (searched on self or parents).")]
    [SerializeField] Rigidbody rb;                   // may be on parent
    [SerializeField] CharacterController controller; // may be on parent

    [Header("Knockback Tuning")]
    [Range(0f, 0.5f)] public float defaultUpFrac = 0.15f;
    public float ccSlideDuration = 0.25f;
    public float damping = 10f;
    public bool rbContinuous = true;

    // ---------------- STUN ----------------
    public bool IsStunned => stunTimer > 0f;
    public event Action<bool> OnStunChanged;

    float stunTimer;

    void Awake()
    {
        //search on self OR parents so child scripts still find the root controller/RB
        if (!rb) rb = GetComponentInParent<Rigidbody>();
        if (!controller) controller = GetComponentInParent<CharacterController>();
    }

    void Update()
    {
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
    public void ApplyKnockback(Vector3 origin, float strength, float upFrac = -1f)
    {
        Vector3 dir = transform.position - origin;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        dir.Normalize();
        ApplyKnockbackDirection(dir, strength, upFrac);
    }

    public void ApplyKnockbackDirection(Vector3 direction, float strength, float upFrac = -1f)
    {
        if (upFrac < 0f) upFrac = defaultUpFrac;

        Vector3 planar = new Vector3(direction.x, 0f, direction.z);
        if (planar.sqrMagnitude < 0.0001f) planar = transform.forward;
        planar.Normalize();

        Vector3 impulse = planar * strength + Vector3.up * (strength * Mathf.Clamp01(upFrac));

        // Prefer CharacterController (collision-safe, works without RB)
        if (controller && controller.enabled)
        {
            StopAllCoroutines();
            StartCoroutine(KnockbackCC(controller, impulse, ccSlideDuration));
            return;
        }

        // Rigidbody fallback
        var body = rb ? rb : GetComponentInParent<Rigidbody>();
        if (body)
        {
            if (rbContinuous) body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.AddForce(impulse, ForceMode.Impulse);
            return;
        }

        // Last resort: move the root (controller if present)
        StopAllCoroutines();
        var rootToNudge = controller ? controller.transform : (rb ? rb.transform : transform);
        StartCoroutine(KnockbackNudge(rootToNudge, impulse));
    }

    IEnumerator KnockbackCC(CharacterController cc, Vector3 impulse, float duration)
    {
        Vector3 vel = impulse * 6f; // feel like an impulse over ~0.25s
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            cc.Move(vel * Time.deltaTime);
            vel = Vector3.Lerp(vel, Vector3.zero, damping * Time.deltaTime);
            yield return null;
        }
    }

    IEnumerator KnockbackNudge(Transform target, Vector3 impulse)
    {
        Vector3 start = target.position;
        Vector3 end = start + impulse * 0.2f;
        float t = 0f, dur = 0.15f;
        while (t < dur)
        {
            t += Time.deltaTime;
            target.position = Vector3.Lerp(start, end, t / dur);
            yield return null;
        }
    }
}
