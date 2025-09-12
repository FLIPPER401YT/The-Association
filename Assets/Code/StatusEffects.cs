using UnityEngine;
using System;
using System.Collections;

[DisallowMultipleComponent]
public class StatusEffects : MonoBehaviour
{
    [Header("Stun Settings")]
    [Space(10)]

    [SerializeField] bool showDebug = true;

    public bool IsStunned => stunTimer > 0f;
    public event Action<bool> OnStunChanged; // true = stunned, false = unstunned

    float stunTimer;

    [Space(10)]
    [Header("Knockback")]
    [Space(10)]

    [SerializeField] Rigidbody rb;

    void Update()
    {
        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                stunTimer = 0f;
                if (showDebug) Debug.Log($"{gameObject.name} recovered from STUN");
                OnStunChanged?.Invoke(false);
            }
        }
    }


    //Apply stun for a duration
    public void ApplyStun(float duration)
    {
        bool wasStunned = IsStunned;
        stunTimer = Mathf.Max(stunTimer, duration); // refresh with max duration
        if (!wasStunned)
        {
            if (showDebug) Debug.Log($"{gameObject.name} is STUNNED for {duration:F1}s");
            OnStunChanged?.Invoke(true);
        }
    }

    //Force-remove stun immediately
    public void ClearStun()
    {
        stunTimer = 0f;
        OnStunChanged?.Invoke(false);
    }

    public void ApplyKnockback(Vector3 origin, float strength)
    {
        rb.AddForce((transform.position - origin) * strength, ForceMode.Impulse);
    }
}
