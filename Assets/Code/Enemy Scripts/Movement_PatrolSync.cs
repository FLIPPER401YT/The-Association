using UnityEngine;
using System.Collections;

public class Movement_PatrolSync : EnemyMovementBaseRB
{
    [SerializeField] private EnemyPatrol patrol;
    [SerializeField] private float pause;
    private bool isPaused;

    protected override void Awake()
    {
        base.Awake();
        if (!patrol) patrol = GetComponent<EnemyPatrol>();
        // We�re handling �no target� ourselves; don�t use the base roam.
        enableRoam = false;
    }

    // Called when a target exists (AI set target = player)
    protected override void TickMovement()
    {
        if (!target) return;

        Vector3 to = target.position - transform.position; to.y = 0f;
        float dist = to.magnitude;

        if (stopDistance > 0f && dist <= stopDistance)
        {
            BrakeToStop();
            if (dist > 0.001f) Face(to.normalized);
            return;
        }

        if (dist <= 0.001f) { BrakeToStop(); return; }

        Vector3 dir = ApplyAvoidance(to.normalized);
        Face(dir);
        MoveHorizontal(dir);
    }

    // Called when there is NO target (patrol mode)
    protected override void NoTargetStep()
    {
        if (!patrol || patrol.PatrolLength == 0)
        {
            BrakeToStop();
            return;
        }

        if (isPaused) { BrakeToStop(); return; }

        Vector3 p = patrol.PointTarget;
        Vector3 to = p - transform.position; to.y = 0f;

        if (to.magnitude > GetStopDistance())
        {
            Vector3 dir = ApplyAvoidance(to.normalized);
            Face(dir);
            MoveHorizontal(dir);
        }
        else
        {
            StartCoroutine(PauseAndAdvance());
        }
    }

    private IEnumerator PauseAndAdvance()
    {
        anim.SetBool("Walking", false);

        if (isPaused) yield break;
        isPaused = true;
        BrakeToStop();
        yield return new WaitForSeconds(pause);

        anim.SetBool("Walking", true);
        
        patrol.NextPoint();
        isPaused = false;
    }
}
