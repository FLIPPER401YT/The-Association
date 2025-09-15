using UnityEngine;

/// Put this on the same GameObject as the Rigidbody and the other movement components.
/// In EnemyAI_Base, set "Mover" to this component.
public class EnemyMovementSelectorRB : EnemyMovementBaseRB
{
    [Header("Movement Modes")]
    [SerializeField] private EnemyMovementBaseRB patrolMover; // e.g. Movement_PatrolSync
    [SerializeField] private EnemyMovementBaseRB aggroMover;  // e.g. EnemyChaseMovementRB OR EnemyLeapMovementRB
    [SerializeField] private bool startWithPatrol = true;

    EnemyMovementBaseRB activeMover;

    protected override void Awake()
    {
        
        base.Awake();
        enableRoam = false; // make sure the selector itself doesn't roam
        Activate(startWithPatrol ? patrolMover : aggroMover);
    }

    // IMPORTANT: override to stop the base class from trying to move itself.
    protected override void FixedUpdate()
    {
        // Choose desired mover based on whether we currently have a target
        var desired = (target == null) ? patrolMover : aggroMover;
        if (activeMover != desired) Activate(desired);

       
    }

    // We never use the base moving ticks in the selector.
    protected override void TickMovement() { }
    protected override void ApplyGravity() { }
    protected override void NoTargetStep() { }

    void Activate(EnemyMovementBaseRB next)
    {
        if (patrolMover) patrolMover.enabled = false;
        if (aggroMover) aggroMover.enabled = false;

        activeMover = next;
        if (activeMover)
        {
            activeMover.enabled = true;
            // make sure it has the same target the AI thinks we have
            activeMover.SetTarget(target);
        }
    }

    // Keep both sub-movers in sync with the current target
    public new void SetTarget(Transform t)
    {
        base.SetTarget(t);
        if (patrolMover) patrolMover.SetTarget(t == null ? null : null); // patrol never follows a target
        if (aggroMover) aggroMover.SetTarget(t);                        // aggro mover follows player
        // The selection itself happens in FixedUpdate each frame.
    }

#if UNITY_EDITOR
    void Reset()
    {
        // try auto-wiring on add
        if (!patrolMover) patrolMover = GetComponent<Movement_PatrolSync>();
        if (!aggroMover)
        {
            aggroMover = GetComponent<EnemyChaseMovementRB>();
            if (!aggroMover) aggroMover = GetComponent<EnemyLeapMovementRB>();
        }
    }
#endif
}
