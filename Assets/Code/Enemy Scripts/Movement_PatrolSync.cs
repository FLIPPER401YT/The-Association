using UnityEngine;
using System.Collections;

public class Movement_PatrolSync : EnemyMovementBaseRB
{
    [SerializeField] private EnemyPatrol patrol;
    [SerializeField] private float pause;
    private bool isPaused = false;
    protected override void TickMovement()
    {
        if (isPaused) return;
        Vector3 targetPosition = target ? target.position : patrol.PointTarget;
        Vector3 Target = targetPosition - transform.position;
        Target.y = 0;
        Face(Target);
        if (Target.magnitude > stopDistance) MoveHorizontal(Target.normalized);
        else if (!target) StartCoroutine(PauseAndAdvance());
    }
    private IEnumerator PauseAndAdvance()
    {
        isPaused = true;
        BrakeToStop();
        yield return new WaitForSeconds(pause);
        patrol.NextPoint();
        isPaused = false;
    }
}
