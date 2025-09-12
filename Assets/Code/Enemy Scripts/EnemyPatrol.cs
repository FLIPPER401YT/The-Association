using System.Collections;
using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [SerializeField] private Transform[] patrolPoints;
    private int pointIndex = 0;
    public Vector3 PointTarget => patrolPoints[pointIndex].position;
    public int PatrolIndex => pointIndex;
    public int PatrolLength => patrolPoints.Length;
    public void NextPoint()
    {
        pointIndex = (pointIndex + 1) % patrolPoints.Length;
    }
    public void SetPatrolPoint(int index)
    {
        if (index >= 0 && index < patrolPoints.Length)
        {
            pointIndex = index;
        }
    }
    public void RandomPatrol()
    {
        if (patrolPoints.Length == 0) return;
        int randomIndex;
        do
        {
            randomIndex = Random.Range(0, patrolPoints.Length);
        } while (randomIndex == pointIndex && patrolPoints.Length > 1);
        pointIndex = randomIndex;
    }
}
