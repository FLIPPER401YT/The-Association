using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WendigoBoss : Base_Boss_AI
{
    [Header("Range")][SerializeField] float swipeRange, projectileMinRange, projectileMaxRange, rushRange, dashRange;

    [Header("Summons")][SerializeField] GameObject[] minions, miniBosses;
    [SerializeField] float minionCooldown;

    [Header("Swipe")][SerializeField] float swipeDamage, swipeWindup, swipeRecover, swipeRadius, swipeCooldown;
    [SerializeField] Vector3 swipeOffset;
    [SerializeField] Transform attackPosition;

    [Header("Spine")][SerializeField] float boltSpeed, boltDamage, boltCooldown;
    [SerializeField] GameObject boltPrefab;
    [SerializeField] Transform castMuzzle;

    [Header("Rush")][SerializeField] float rushSpeed, rushTime, rushDamage, rushKnockback, rushKickup, rushRest, rushCooldown, rushShoulderRadius, rushShoulderLength;

    [Header("Evade")][SerializeField] float dashSpeed, dashTime, dashCooldown;

    [Header("Hit Masks & Tags")][SerializeField] LayerMask rushHitMask = ~0;
    [SerializeField] Collider bodyCollider;
    [SerializeField]string playerTag = "Player";

    [Header("Separation")][SerializeField] float personalSpace, separationSpeed;
    [SerializeField] bool keepSpace = true;

    Base_Boss_AI WendigoStats;
    float swipeCD, boltCD, rushCD, summonCD, dashCD;
    bool rushHit;
    bool MeleeEnabled => swipeDamage > 0f && swipeRadius > 0f;
    bool RangeEnabled;
    bool RushEnabled => rushSpeed > 0f && rushTime > 0f && rushDamage > 0f;
    bool SummonEnabled;
    bool EvadeEnabled;

    #region Awake and Update
    protected override void Awake()
    {
        base.Awake();
        if(!bodyCollider) bodyCollider = GetComponent<Collider>();
    }
    protected override void FixedUpdate()
    {
        float time = Time.fixedDeltaTime;
        swipeCD -= time; rushCD -= time; summonCD -= time; dashCD -= time;
        //Include PersonalSpace call later when I have my own version written.
        base.FixedUpdate();
    }
    #endregion
    #region Getters and Setters
    bool IsPlayerObject(Transform transform) => (player && (transform == player || transform.IsChildOf(player))) || transform.CompareTag(playerTag);
    static IDamage FindDamage(Component component) => component.GetComponentInParent<IDamage>() ?? component.GetComponentInChildren<IDamage>();
    Vector3 GetPlayerCenter()
    {
        if(!player) return transform.position;
        var character = player.GetComponent<CharacterController>();
        if (character) return character.bounds.center;
        var collider = player.GetComponentInChildren<Collider>();
        if (collider) return collider.bounds.center;
        return player.position;
    }
    #endregion
    #region Movement and Decisions
    void PersonalSpace()
    {
        if (!player) return;
        Vector3 playerCenter = GetPlayerCenter();
        Vector3 delta = transform.position - playerCenter;
        delta.y = 0f;
        float distance = delta.magnitude;
        if (distance < 0.01f) delta = -transform.forward;
        if (distance >= personalSpace) return;
        Vector3 outward = delta.normalized;
        Vector3 targetXZ = player.position + outward * personalSpace;
        //Vector3 newPosition = new Vector3.MoveTowards(
        //    new Vector3(transform.position.x, transform.position.y, transform.position.z),
        //    new Vector3(targetXZ.x, transform.position.y, targetXZ.z),
        //    separationSpeed * Time.fixedDeltaTime
        //    );
        //if (rb) rb.MovePosition(newPosition);
        //else transform.position = newPosition;
    }
    protected override bool CanAttack(float playerDistance)
    {
        return (MeleeEnabled && playerDistance <= swipeRange + 0.5f) ||
            (RangeEnabled && playerDistance <= projectileMaxRange && playerDistance >= projectileMinRange) ||
            (RushEnabled && playerDistance <= rushRange);
    }
    //protected bool CanSupport(float playerDistance, int bossHealth)
    //{
    //    return (EvadeEnabled && playerDistance <= dashRange) ||
    //        (SummonEnabled && bossHealth == WendigoStats.currentHP)
    //}
    protected override IEnumerator PickAndRunAttack(float distToPlayer)
    {
        throw new System.NotImplementedException();
    }

    #endregion
}
