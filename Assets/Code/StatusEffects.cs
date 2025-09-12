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

    bool isKnockback = false;
    Vector3 knockbackPos;
    Vector3 knockbackStartPos;

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

        if (isKnockback)
        {
            transform.position = Vector3.MoveTowards(transform.position, knockbackStartPos + knockbackPos, 30 * Time.deltaTime);
            if (Vector3.Distance(transform.position, knockbackStartPos + knockbackPos) <= 0.01)
            {
                isKnockback = false;
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
        knockbackPos = ((transform.position - origin).normalized * 2 * strength) + (transform.up * strength / 2);
        knockbackStartPos = transform.position;
        isKnockback = true;
    }
}
