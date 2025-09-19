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

    public bool isKnockingBack => isKnockback;

    bool isKnockback = false;
    Vector3 knockbackDir;

    // ---------------------- BLIND ----------------------
    public bool IsBlinded => blindTimer > 0f;

    //Raised when blindness starts (true) or ends (false).
    public event Action<bool> OnBlindChanged;

    float blindTimer;
    

    void Update()
    {
        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                GameManager.instance.playerStunEffect.SetActive(false);
                stunTimer = 0f;
                if (showDebug) Debug.Log($"{gameObject.name} recovered from STUN");
                OnStunChanged?.Invoke(false);
            }
        }

        if (isKnockback)
        {
            knockbackDir = Vector3.MoveTowards(knockbackDir, Vector3.zero, 9.8f * Time.deltaTime);
            rb.AddForce(knockbackDir, ForceMode.Force);
            if (Vector3.Distance(knockbackDir, Vector3.zero) <= 0.01)
            {
                isKnockback = false;
            }
        }

        // ---------------- BLIND TICK ----------------
        if (blindTimer > 0f)
        {
            blindTimer -= Time.deltaTime;
            if (blindTimer <= 0f)
            {
                GameManager.instance.playerBlindEffect.SetActive(false);
                blindTimer = 0f;
                if (showDebug) Debug.Log($"{gameObject.name} recovered from BLIND");
                OnBlindChanged?.Invoke(false);
            }
        }
        
    }


    //Apply stun for a duration
    public void ApplyStun(float duration)
    {
        bool wasStunned = IsStunned;
        GameManager.instance.playerStunEffect.SetActive(true);
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
        rb.linearVelocity = Vector3.zero;
        knockbackDir = ((transform.position - origin).normalized * strength) + (transform.up * strength / 2);
        rb.AddForce(knockbackDir, ForceMode.Impulse);
        knockbackDir = new Vector3(knockbackDir.x, 0, knockbackDir.z);
        isKnockback = true;
    }

    // -------------------- BLIND API --------------------
    
    //Apply or refresh blind on THIS actor. Uses the max of remaining vs new duration.
    
    public void ApplyBlind(float duration)
    {
        GameManager.instance.playerBlindEffect.SetActive(true);
        bool wasBlinded = IsBlinded;
        blindTimer = Mathf.Max(blindTimer, duration);

        if (showDebug) Debug.Log($"{gameObject.name} is BLINDED for {duration:F1}s (remaining: {blindTimer:F1}s)");

        if (!wasBlinded)
        {
            OnBlindChanged?.Invoke(true);
        }
    }

    
    //Clear blindness immediately.
    
    public void ClearBlind()
    {
        if (blindTimer <= 0f) return;
        blindTimer = 0f;
        if (showDebug) Debug.Log($"{gameObject.name} BLIND cleared");
        OnBlindChanged?.Invoke(false);
    }
   
}
