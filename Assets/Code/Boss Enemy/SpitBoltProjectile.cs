using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(AudioSource))]
public class SpitBoltProjectile : MonoBehaviour
{
    [Header("Damage / Lifetime")]
    public float damage;
    public float lifetime;
    public LayerMask hitMask = ~0;

    [Header("Homing (gentle)")]
    public Transform player;                 // optional tiny mid-flight aim assist
    public float gentleHomeStrength = 0f;    // set 0 to disable

    [Header("Ownership")]
    public Transform owner;                  // spawner passes itself; ignored on hit

    // --------- AUDIO ----------
    [Header("Audio (uses this object's AudioSource)")]
    [SerializeField] AudioSource sfx;        // assign in prefab; falls back to GetComponent
    [SerializeField] AudioClip launchClip;   // one-shot at spawn
    [SerializeField] AudioClip loopClip;     // looping whoosh while flying
    [SerializeField] AudioClip impactClip;   // one-shot on impact / expire
    [Range(0f, 1f)] public float launchVolume = 1f;
    [Range(0f, 1f)] public float loopVolume = 0.6f;
    [Range(0f, 1f)] public float impactVolume = 1f;
    [Tooltip("± pitch variance for one-shots/loop init.")]
    [Range(0f, 0.2f)] public float pitchJitter = 0.05f;

    Rigidbody rb;
    float t;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        rb.useGravity = false;

        if (!sfx) sfx = GetComponent<AudioSource>();   // use the Audio Source shown in your screenshot

        // launch one-shot 
        if (launchClip) sfx.PlayOneShot(launchClip, launchVolume);

        // setup loop (on the same AudioSource so it uses your mixer group)
        if (loopClip)
        {
            sfx.clip = loopClip;
            sfx.loop = true;
            sfx.volume = loopVolume;
            sfx.spatialBlend = 1f;                          // 3D
            sfx.rolloffMode = AudioRolloffMode.Logarithmic;
            sfx.minDistance = 2f;
            sfx.maxDistance = 25f;
            sfx.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            sfx.Play();
        }
    }

    void Update()
    {
        t += Time.deltaTime;
        if (t >= lifetime)
        {
            PlayImpactAndDie();
        }
    }

    void FixedUpdate()
    {
        if (player && gentleHomeStrength > 0f && rb.linearVelocity.sqrMagnitude > 0.0001f)
        {
            Vector3 desiredDir = (player.position + Vector3.up * 1.2f - transform.position).normalized;
            Vector3 newVel = Vector3.Lerp(
                rb.linearVelocity,
                desiredDir * rb.linearVelocity.magnitude,
                Time.fixedDeltaTime * gentleHomeStrength
            );
            rb.linearVelocity = newVel;
            rb.MoveRotation(Quaternion.LookRotation(newVel.normalized, Vector3.up));
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Respect mask
        if (((1 << other.gameObject.layer) & hitMask) == 0) return;

        // Ignore owner (and its children)
        if (owner && (other.transform == owner || other.transform.IsChildOf(owner))) return;

        // Damage
        var dmg = other.GetComponentInParent<IDamage>() ?? other.GetComponentInChildren<IDamage>();
        if (dmg != null) dmg.TakeDamage((int)damage);

        PlayImpactAndDie();
    }

    // ---- helpers ----

    void PlayImpactAndDie()
    {
        // stop loop on the main source
        if (sfx && sfx.isPlaying) sfx.Stop();

        // If we have an impact clip, spawn a tiny "detached" AudioSource
        // that copies settings from our main source (so it uses the same mixer),
        // plays the clip, then destroys itself.
        if (impactClip && sfx)
        {
            var go = new GameObject("ProjectileImpactSFX");
            go.transform.position = transform.position;

            var a = go.AddComponent<AudioSource>();
            // copy key settings so it behaves like the prefab's source
            a.outputAudioMixerGroup = sfx.outputAudioMixerGroup;
            a.spatialBlend = 1f;
            a.rolloffMode = AudioRolloffMode.Logarithmic;
            a.minDistance = sfx.minDistance;
            a.maxDistance = sfx.maxDistance;
            a.volume = impactVolume;
            a.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            a.PlayOneShot(impactClip, impactVolume);
            Destroy(go, impactClip.length * (1f / Mathf.Max(0.01f, a.pitch)));
        }

        Destroy(gameObject);
    }
}
