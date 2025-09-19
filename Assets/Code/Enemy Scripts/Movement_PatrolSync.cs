using UnityEngine;
using System.Collections;

public class Movement_PatrolSync : EnemyMovementBaseRB
{
    [SerializeField] private EnemyPatrol patrol;
    [SerializeField] private float pause;
    private bool isPaused;

    [Header("Audio")]
    [SerializeField] private AudioSource sfx;           // assign in Inspector
    [SerializeField] private AudioClip[] footstepClips; // walking footstep sounds
    [SerializeField] private float stepInterval = 0.6f; // seconds between footsteps
    [SerializeField] private float stepVolume = 1f;
    [SerializeField] private float pitchJitter = 0.05f; // ±5% pitch variation

    private float nextStepTime;

    protected override void Awake()
    {
        base.Awake();
        if (!patrol) patrol = GetComponent<EnemyPatrol>();
        enableRoam = false;

        if (!sfx) sfx = GetComponent<AudioSource>();
        if (sfx) sfx.spatialBlend = 1f; // make it 3D
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

            // walking footsteps
            TryPlayFootstep();
            anim.SetBool("Walking", true);
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

        patrol.NextPoint();
        isPaused = false;
    }

    private void TryPlayFootstep()
    {
        if (!sfx || footstepClips.Length == 0) return;

        if (Time.time >= nextStepTime)
        {
            var clip = footstepClips[Random.Range(0, footstepClips.Length)];
            float originalPitch = sfx.pitch;
            sfx.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);

            sfx.PlayOneShot(clip, stepVolume);

            sfx.pitch = originalPitch;
            nextStepTime = Time.time + stepInterval;
        }
    }
}
