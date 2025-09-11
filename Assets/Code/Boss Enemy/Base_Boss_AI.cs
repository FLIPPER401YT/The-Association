using UnityEngine;
using System.Collections;

public class Base_Boss_AI : MonoBehaviour, IDamage
{
    [Header("Refs")]
    [SerializeField] protected Transform player;
    [SerializeField] protected Rigidbody rb;

    [Header("Health")]
    [SerializeField] protected int maxHP;
    [SerializeField] protected int currentHP;

    [Header("Perception")]
    [SerializeField] protected float aggroRange;
    [SerializeField] protected float leashRange;

    [Header("Roam")]
    [SerializeField] protected float roamRadius;
    [SerializeField] protected float minHopDistance;
    [SerializeField] protected float arriveRadius;
    [SerializeField] protected Vector2 dwellRange = new Vector2();
    [SerializeField] protected LayerMask groundMask = ~0;

    [Header("Movement")]
    [SerializeField] protected float maxSpeed;
    [SerializeField] protected float chaseSpeed;
    [SerializeField] protected float maxAccel;
    [SerializeField] protected float turnLerp;

    [Header("Avoidance")]
    [SerializeField] protected LayerMask obstacleMask = ~0;
    [SerializeField] protected float avoidStrength;
    [SerializeField] protected float lookAhead;
    [SerializeField] protected float whiskerAngle;
    [SerializeField] protected float whiskerLen;
    [SerializeField] protected float avoidRadius;

    [Header("Attacks")]
    [SerializeField] protected float globalAttackCooldown;

    [Header("Debug")]
    [SerializeField] protected bool drawGizmos;

    protected BossState state = BossState.Roam;
    protected Vector3 spawn;
    protected Vector3 roamTarget;
}
