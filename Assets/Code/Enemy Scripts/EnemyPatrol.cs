using System.Collections;
using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [SerializeField] private GameObject[] patrolPoints;
    [SerializeField] private float delay;
    private int pointIndex = -1;
    public Vector3 CurrentPointTarget {  get; private set; }
    public bool IsPatrolling { get; private set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        if (patrolPoints.Length > 0) StartCoroutine(PatrolRoutine());
    }

    // Update is called once per frame
    private IEnumerator PatrolRoutine()
    {
        IsPatrolling = true;
        while (true)
        {
            int index;
            do
            {
                index = Random.Range(0, patrolPoints.Length);
            } while (index == pointIndex && patrolPoints.Length > 1);
            pointIndex = index;
            CurrentPointTarget = patrolPoints[pointIndex].transform.position;
            yield return new WaitUntil(() => AtTarget());
            yield return new WaitForSeconds(delay);
        }
    }
    private bool AtTarget()
    {
        return Vector3.Distance(transform.position, CurrentPointTarget) < 0.1f;
    }
}
