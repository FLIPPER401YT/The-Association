using UnityEngine;
using UnityEngine.Audio; // <-- mixer
using System.Collections;

public class WendigoBossV2 : Base_Boss_AI
{
    // ---------------- RANGES / GATING ----------------
    [Header("Ranges")]
    [SerializeField] float meleeRange = 2.2f;          // Swipe
    [SerializeField] float rushMinDist = 5f;           // Rush if at/over this distance

    [Header("Stop")]
    [SerializeField] float stopDistance = 1.75f;

    // ---------------- SWIPE (MELEE) ----------------
    [Header("Swipe")]
    [SerializeField] float swipeDamage = 15f;
    [SerializeField] float swipeWindup = 0.35f;
    [SerializeField] float swipeRecover = 0.4f;
    [SerializeField] float swipeRadius = 1.4f;
    [SerializeField] float swipeCooldown = 1.25f;
    [SerializeField] Vector3 swipeOffset = new Vector3(0f, 0.9f, 1.1f);
    [Tooltip("If set, swipe uses this position instead of swipeOffset.")]
    [SerializeField] Transform swipeAttackPos;

    // ---------------- RUSH (RAM) ----------------
    [Header("Rush / Ram")]
    [SerializeField] float rushSpeed = 10f;
    [SerializeField] float rushTime = 1.1f;
    [SerializeField] float rushDamage = 20f;
    [SerializeField] float rushKnockback = 12f;
    [SerializeField] float rushUpwardKick = 2f;                // physics fallback
    [SerializeField] float rushRestDuration = 0.6f;
    [SerializeField] float rushCooldown = 4f;
    [SerializeField] float rushShoulderCastRadius = 0.6f;
    [SerializeField] LayerMask rushHitMask = ~0;

    // ---------------- RANGED (SPIT BOLT) ----------------
    [Header("Ranged: Spit Bolt")]
    [SerializeField] GameObject spitBoltPrefab;               // has Rigidbody + trigger collider + SpitBoltProjectile
    [SerializeField] Transform castMuzzle;
    [SerializeField] float rangedRange = 12f;
    [SerializeField] float boltSpeed = 18f;
    [SerializeField] float boltDamage = 10f;
    [SerializeField] float rangedCooldown = 2.25f;
    [SerializeField] float gentleHomeStrength = 4f;           // 0 = off

    // ---------------- SUMMON ----------------
    [Header("Summon")]
    [Tooltip("Different enemy prefabs the Wendigo can summon.")]
    [SerializeField] GameObject[] summonPrefabs;
    [Tooltip("Where to drop them in. If empty, spawns around the boss.")]
    [SerializeField] Transform[] summonPoints;
    [SerializeField] int summonCount = 2;                     // how many per cast
    [SerializeField] float summonCooldown = 10f;
    [SerializeField] int maxSummonsAlive = 6;
    [SerializeField] string summonTag = "Enemy";

    // ---------------- AUDIO ----------------
    [Header("Audio (Optional)")]
    [SerializeField] AudioClip roarLoop;          // idle/roam loop (voice)
    [SerializeField] AudioClip swipeSFX;
    [SerializeField] AudioClip rushSFX;
    [SerializeField] AudioClip rangedSFX;
    [SerializeField] AudioClip summonSFX;
    [Range(0f, 1f)][SerializeField] float sfxVolume = 0.9f;

    [Header("Audio Routing")]
    [SerializeField] AudioMixerGroup outputMixerGroup; // assign your "SoundEffects (Mixer)" group
    [SerializeField] AudioSource voiceSrc;             // looping / roar
    [SerializeField] AudioSource fxSrc;                // one-shots
    [Range(0f, 0.2f)][SerializeField] float pitchJitter = 0.06f;

    // ---------------- DEBUG ----------------
    [Header("Debug")]
    [SerializeField] bool logAttacks = false;

    // cooldowns
    float swipeCD, rushCD, rangedCD, summonCD;

    bool rushDidHit;

    protected override void Awake()
    {
        base.Awake();

        // Ground boss
        if (rb) rb.useGravity = true;

        // Ensure local audio sources exist
        if (!voiceSrc) voiceSrc = gameObject.AddComponent<AudioSource>();
        if (!fxSrc) fxSrc = gameObject.AddComponent<AudioSource>();

        // Route to mixer
        if (outputMixerGroup)
        {
            voiceSrc.outputAudioMixerGroup = outputMixerGroup;
            fxSrc.outputAudioMixerGroup = outputMixerGroup;
        }

        // 3D audio setup
        foreach (var src in new[] { voiceSrc, fxSrc })
        {
            src.playOnAwake = false;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.dopplerLevel = 0f;
            src.minDistance = 2f;
            src.maxDistance = 30f;
        }

        // Looping roar (optional)
        if (roarLoop)
        {
            voiceSrc.clip = roarLoop;
            voiceSrc.loop = true;
            if (!voiceSrc.isPlaying) voiceSrc.Play();
        }
    }

    protected override void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        swipeCD = Mathf.Max(0f, swipeCD - dt);
        rushCD = Mathf.Max(0f, rushCD - dt);
        rangedCD = Mathf.Max(0f, rangedCD - dt);
        summonCD = Mathf.Max(0f, summonCD - dt);

        base.FixedUpdate();
    }

    protected override void Die()
    {
        anim.SetBool("Running", false);
        anim.SetBool("Rushing", false);
        anim.SetBool("Death", true);

        base.Die();
    }

    // ---------------- ATTACK GATING ----------------
    protected override bool CanAttack(float distToPlayer)
    {
        bool canSwipe = (swipeCD <= 0f) && (distToPlayer <= meleeRange + 0.25f);
        bool canRush = (rushCD <= 0f) && (distToPlayer >= rushMinDist);
        bool canRanged = (rangedCD <= 0f) && (distToPlayer <= rangedRange);
        bool canSummon = (summonCD <= 0f) && (CountAliveSummons() < maxSummonsAlive);

        return canSwipe || canRush || canRanged || canSummon;
    }

    protected override IEnumerator PickAndRunAttack(float distToPlayer)
    {
        anim.SetBool("Running", false);

        bool canSummon = summonCD <= 0f && CountAliveSummons() < maxSummonsAlive && summonPrefabs != null && summonPrefabs.Length > 0;
        bool canSwipe = swipeCD <= 0f && distToPlayer <= meleeRange + 0.25f;
        bool canRush = rushCD <= 0f && distToPlayer >= rushMinDist;
        bool canRanged = rangedCD <= 0f && distToPlayer <= rangedRange && spitBoltPrefab && castMuzzle;

        if (canSummon && (!canSwipe || Random.value < 0.35f))
        {
            anim.SetTrigger("Summon");
            if (logAttacks) Debug.Log("[Wendigo] ATTACK: Summon");
            yield return StartCoroutine(DoSummon());
            yield break;
        }

        if (canSwipe)
        {
            anim.SetTrigger("Swipe");
            if (logAttacks) Debug.Log("[Wendigo] ATTACK: Swipe");
            yield return StartCoroutine(DoSwipe());
            yield break;
        }

        if (canRush && (!canRanged || distToPlayer > meleeRange * 2f))
        {
            anim.SetBool("Rushing", true);
            if (logAttacks) Debug.Log("[Wendigo] ATTACK: Rush");
            yield return StartCoroutine(DoRush());
            yield break;
        }

        if (canRanged)
        {
            anim.SetTrigger("Range");
            if (logAttacks) Debug.Log("[Wendigo] ATTACK: SpitBolt");
            yield return StartCoroutine(DoSpitBolt());
            yield break;
        }

        yield return null;
    }

    // --- Stop steering when within stopDistance ---
    protected override Vector3 DesiredChaseVelocity()
    {
        if (!player) return Vector3.zero;

        Vector3 to = player.position - transform.position;
        Vector3 planar = new Vector3(to.x, 0f, to.z);
        float dist = planar.magnitude;

        if (stopDistance > 0f && dist <= stopDistance)
            return Vector3.zero;

        return (dist > 0.001f) ? planar.normalized * chaseSpeed : Vector3.zero;
    }

    // ---------------- SWIPE ----------------
    IEnumerator DoSwipe()
    {
        ChangeState(BossState.Attack);
        FacePlayer();
        BrakePlanar();

        PlayOneShot(swipeSFX, sfxVolume);

        yield return new WaitForSeconds(swipeWindup);

        Vector3 center = swipeAttackPos ? swipeAttackPos.position : transform.position + transform.TransformVector(swipeOffset);
        var hits = Physics.OverlapSphere(center, swipeRadius, rushHitMask, QueryTriggerInteraction.Collide);
        foreach (var h in hits)
        {
            if (!IsPlayerObj(h.transform)) continue;
            var dmg = FindDamage(h);
            if (dmg != null) dmg.TakeDamage((int)swipeDamage);
            break;
        }

        yield return new WaitForSeconds(swipeRecover);
        swipeCD = swipeCooldown;
        attackLockout = globalAttackCooldown;
        ChangeState(BossState.Recover);
    }

    // ---------------- RUSH ----------------
    IEnumerator DoRush()
    {
        ChangeState(BossState.Attack);
        rushDidHit = false;

        PlayOneShot(rushSFX, sfxVolume);

        float t = 0f;
        while (t < rushTime)
        {
            t += Time.fixedDeltaTime;

            Vector3 to = (player ? player.position - transform.position : Vector3.zero); to.y = 0f;
            Vector3 desired = (to.sqrMagnitude > 0.1f ? to.normalized : transform.forward) * rushSpeed;
            Vector3 accel = Vector3.ClampMagnitude(desired - GetPlanarVel(), maxAccel * 1.25f);
            rb.AddForce(new Vector3(accel.x, 0f, accel.z), ForceMode.Acceleration);

            ClampPlanarSpeed(rushSpeed);
            FaceVelocity();

            if (!rushDidHit)
            {
                Vector3 origin = transform.position + Vector3.up * 0.4f;
                Vector3 top = origin + Vector3.up * 1.6f;
                float radius = Mathf.Max(0.05f, rushShoulderCastRadius);

                var cols = Physics.OverlapCapsule(origin, top, radius, rushHitMask, QueryTriggerInteraction.Collide);
                foreach (var c in cols)
                {
                    if (!IsPlayerObj(c.transform)) continue;

                    rushDidHit = true;

                    var dmg = FindDamage(c);
                    if (dmg != null) dmg.TakeDamage((int)rushDamage);

                    var status = GetPlayerStatus();
                    Vector3 playerCenter = GetPlayerCenter();
                    Vector3 hitOrigin = transform.position;

                    if (status)
                    {
                        status.ApplyKnockback(hitOrigin, rushKnockback);
                    }
                    else
                    {
                        var prb = player ? (player.GetComponent<Rigidbody>() ??
                                            player.GetComponentInChildren<Rigidbody>()) : null;
                        if (prb)
                        {
                            Vector3 dir = (playerCenter - hitOrigin); dir.y = 0f;
                            if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
                            dir.Normalize();
                            prb.AddForce(dir * rushKnockback + Vector3.up * Mathf.Max(0.5f, rushUpwardKick), ForceMode.Impulse);
                        }
                    }

                    rb.linearVelocity = Vector3.zero;
                    t = rushTime;
                    break;
                }
            }
            yield return new WaitForFixedUpdate();
        }

        BrakePlanar();
        rb.linearVelocity = Vector3.zero;

        anim.SetBool("Rushing", false);

        yield return new WaitForSeconds(rushRestDuration);

        rushCD = rushCooldown;
        attackLockout = globalAttackCooldown;
        ChangeState(BossState.Recover);
    }

    // ---------------- RANGED (SPIT BOLT) ----------------
    IEnumerator DoSpitBolt()
    {
        ChangeState(BossState.Attack);

        PlayOneShot(rangedSFX, sfxVolume);

        FacePlayer();
        BrakePlanar();

        if (spitBoltPrefab && castMuzzle && player)
        {
            Vector3 dir = (player.position + Vector3.up * 1.2f - castMuzzle.position).normalized;
            var go = Instantiate(spitBoltPrefab, castMuzzle.position, Quaternion.LookRotation(dir, Vector3.up));

            var rbProj = go.GetComponent<Rigidbody>();
            if (rbProj) rbProj.linearVelocity = dir * boltSpeed;

            var proj = go.GetComponent<SpitBoltProjectile>();
            if (proj)
            {
                proj.damage = boltDamage;
                proj.lifetime = 6f;
                proj.hitMask = ~0;
                proj.player = player;
                proj.gentleHomeStrength = gentleHomeStrength;
                proj.owner = transform;
            }
            rangedCD = rangedCooldown;
        }

        yield return null;
        attackLockout = globalAttackCooldown;
        ChangeState(BossState.Recover);
    }

    // ---------------- SUMMON ----------------
    IEnumerator DoSummon()
    {
        ChangeState(BossState.Attack);
        FacePlayer();
        BrakePlanar();

        PlayOneShot(summonSFX, sfxVolume);

        yield return new WaitForSeconds(0.6f);

        int alive = CountAliveSummons();
        int slots = Mathf.Max(0, maxSummonsAlive - alive);
        if (slots <= 0 || summonPrefabs == null || summonPrefabs.Length == 0)
        {
            yield return null;
        }
        else
        {
            int toSpawn = Mathf.Min(slots, Mathf.Max(1, summonCount));
            for (int i = 0; i < toSpawn; i++)
            {
                GameObject prefab = summonPrefabs[Random.Range(0, summonPrefabs.Length)];
                if (!prefab) continue;

                Vector3 pos;
                Quaternion rot;

                if (summonPoints != null && summonPoints.Length > 0)
                {
                    Transform p = summonPoints[Random.Range(0, summonPoints.Length)];
                    pos = p.position; rot = p.rotation;
                }
                else
                {
                    Vector2 r = Random.insideUnitCircle.normalized * Random.Range(2.5f, 5f);
                    pos = new Vector3(transform.position.x + r.x, transform.position.y, transform.position.z + r.y);
                    rot = Quaternion.identity;

                    if (Physics.Raycast(pos + Vector3.up * 8f, Vector3.down, out RaycastHit hit, 30f, groundMask))
                        pos = hit.point;
                }

                Instantiate(prefab, pos, rot);
            }
        }

        summonCD = summonCooldown;
        attackLockout = globalAttackCooldown;
        ChangeState(BossState.Recover);
        yield return null;
    }

    // ---------------- HELPERS ----------------
    bool IsPlayerObj(Transform t)
        => (player && (t == player || t.IsChildOf(player))) || t.CompareTag("Player");

    static IDamage FindDamage(Component c)
        => c.GetComponentInParent<IDamage>() ?? c.GetComponentInChildren<IDamage>();

    static StatusEffects FindStatus(Component c)
        => c.GetComponentInParent<StatusEffects>() ?? c.GetComponentInChildren<StatusEffects>();

    StatusEffects GetPlayerStatus() => player ? player.GetComponentInChildren<StatusEffects>() : null;

    Vector3 GetPlayerCenter()
    {
        if (!player) return transform.position;
        var cc = player.GetComponent<CharacterController>();
        if (cc) return cc.bounds.center;
        var col = player.GetComponentInChildren<Collider>();
        if (col) return col.bounds.center;
        return player.position;
    }

    int CountAliveSummons()
    {
        if (string.IsNullOrEmpty(summonTag)) return 0;
        return GameObject.FindGameObjectsWithTag(summonTag).Length;
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = new Color(0.9f, 0.9f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        Gizmos.color = new Color(1f, 0.25f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, rangedRange);

        Gizmos.color = new Color(1f, 0.8f, 0f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, rushMinDist);

        Gizmos.color = new Color(1f, 0.4f, 0.8f, 0.8f);
        Vector3 center = swipeAttackPos
            ? swipeAttackPos.position
            : transform.position + transform.TransformVector(swipeOffset);
        Gizmos.DrawWireSphere(center, swipeRadius);

        if (summonPoints != null)
        {
            Gizmos.color = Color.green;
            foreach (var p in summonPoints)
            {
                if (!p) continue;
                Gizmos.DrawWireSphere(p.position, 0.25f);
                Gizmos.DrawLine(transform.position + Vector3.up * 1.25f, p.position);
            }
        }
    }
#endif

    // --- Local SFX helper ---
    void PlayOneShot(AudioClip clip, float vol = 1f)
    {
        if (!clip || !fxSrc) return;
        fxSrc.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        fxSrc.PlayOneShot(clip, vol);
    }
}
