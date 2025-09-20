using System.Collections;
using UnityEditor.Animations;
using UnityEngine;

public class EnemyMeleeAI : EnemyAI_Base
{
    [Header("Melee")]
    [SerializeField] GameObject weapon;
    [SerializeField] Transform attackPos;
    [SerializeField] float attackRange;
    [SerializeField] float attackCooldown;
    [SerializeField] int meleeDamage;

    [Header("Audio (one-shots)")]
    [SerializeField] private AudioClip[] swingClips;
    [SerializeField] private AudioClip[] hitClips;
    [SerializeField] private float swingVolume;
    [SerializeField] private float hitVolume;
    [SerializeField, Range(0f, 0.3f)] private float pitchJitter;

    [Header("Audio (constant roar/ambience)")]
    [SerializeField] private AudioSource loopSrc;        // 3D, separate from sfx
    [SerializeField] private AudioClip[] roarClips;
    [SerializeField, Range(0f, 1f)] private float roarVolume;
    [SerializeField] private Vector2 roarGapSeconds = new Vector2(0f, 0.75f);
    [SerializeField, Range(0f, 0.2f)] private float roarPitchJitter;
    [SerializeField] private bool autoStartRoar = true;

    [Header("3D Roar Settings")]
    [Tooltip("Optional: where the roar should emanate from (mouth/chest).")]
    [SerializeField] private Transform roarAnchor;
    [Tooltip("Within this distance, volume stays near max.")]
    [SerializeField] private float roarMinDistance;
    [Tooltip("Beyond this distance, volume is ~0.")]
    [SerializeField] private float roarMaxDistance;
    [SerializeField] private AudioRolloffMode roarRolloff = AudioRolloffMode.Logarithmic;
    [SerializeField, Range(0f, 5f)] private float dopplerLevel;

    float attackTimer;
    Coroutine roarRoutine;

    void Awake()
    {
        if (!mover) mover = GetComponent<EnemyMovementBaseRB>();
        Configure3DAudio(loopSrc, roarMinDistance, roarMaxDistance, roarRolloff, dopplerLevel);
        Configure3DAudio(sfx, 3f, 30f, AudioRolloffMode.Logarithmic, 0f);
        if (loopSrc != null) loopSrc.loop = false; // code handles looping/jitter
    }

    void OnEnable()
    {
        if (autoStartRoar) StartRoar();
    }

    void OnDisable()
    {
        StopRoar();
    }

    void LateUpdate()
    {
        // keep the roar�s AudioSource positioned at the anchor if provided
        if (roarAnchor && loopSrc) loopSrc.transform.position = roarAnchor.position;
    }

    // -------- roar control --------
    public void StartRoar()
    {
        if (roarRoutine != null) return;
        if (!loopSrc || roarClips == null || roarClips.Length == 0) return;
        roarRoutine = StartCoroutine(RoarLoop());
    }

    public void StopRoar()
    {
        if (roarRoutine != null) { StopCoroutine(roarRoutine); roarRoutine = null; }
        if (loopSrc) loopSrc.Stop();
    }

    IEnumerator RoarLoop()
    {
        while (true)
        {
            var clip = roarClips[Random.Range(0, roarClips.Length)];
            if (!clip) { yield return null; continue; }

            float pitch = 1f + Random.Range(-roarPitchJitter, roarPitchJitter);
            loopSrc.pitch = pitch;
            loopSrc.volume = roarVolume;     // base volume; distance falloff handled by 3D settings
            loopSrc.clip = clip;
            loopSrc.Play();

            float dur = clip.length / Mathf.Max(0.01f, pitch);
            yield return new WaitForSeconds(dur);

            float gap = Mathf.Max(0f, Random.Range(roarGapSeconds.x, roarGapSeconds.y));
            if (gap > 0f) yield return new WaitForSeconds(gap);
        }
    }

    // -------- melee logic (unchanged) --------
    protected override void Update()
    {
        base.Update();
        attackTimer += Time.deltaTime;

        if (!player || !playerInTrigger) return;

        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        if (distance <= attackRange)
        {
            playerDir = toPlayer;
            FaceTarget();

            if (attackTimer >= attackCooldown)
            {
                attackTimer = 0f;
                StartCoroutine(DoMeleeAttack());
            }
        }
    }

    IEnumerator DoMeleeAttack()
    {
        anim.SetTrigger("Attack");

        AnimationClip clip = null;
        if (anim.runtimeAnimatorController is AnimatorController controller)
        {
            foreach (ChildAnimatorState state in controller.layers[0].stateMachine.states)
            {
                if (state.state.name.Equals("Melee Attack"))
                {
                    clip = state.state.motion as AnimationClip;
                    break;
                }
            }
        }

        PlayOneShotRandom(swingClips, swingVolume);

        float attackCheckTime = clip != null ? clip.length / 2.5f : 0f;
        yield return new WaitForSeconds(attackCheckTime);

        IDamage dmg = player.GetComponent<IDamage>();
        if (dmg != null)
        {
            dmg.TakeDamage(meleeDamage);
            PlayOneShotRandom(hitClips, hitVolume);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (attackPos == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPos.position, attackRange);
    }
#endif

    // ---- helpers ----
    void PlayOneShotRandom(AudioClip[] bank, float vol)
    {
        if (!sfx || bank == null || bank.Length == 0) return;
        var clip = bank[Random.Range(0, bank.Length)];
        float original = sfx.pitch;
        sfx.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        sfx.PlayOneShot(clip, vol);
        sfx.pitch = original;
    }

    static void Configure3DAudio(AudioSource src, float minDist, float maxDist, AudioRolloffMode rolloff, float doppler)
    {
        if (!src) return;
        src.spatialBlend = 1f;           // make it 3D
        src.rolloffMode = rolloff;       // Logarithmic is natural
        src.minDistance = Mathf.Max(0.01f, minDist);
        src.maxDistance = Mathf.Max(src.minDistance + 0.01f, maxDist);
        src.dopplerLevel = doppler;      // 0 to disable Doppler
        src.spread = 0f;
        src.spatialize = false;          // set true if using a spatializer plugin
    }
}
