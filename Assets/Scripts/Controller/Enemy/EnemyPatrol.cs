using UnityEngine;

/// <summary>
/// Owns patrol spawn data, patrol targets, and wait timing.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyMove))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Components")]
    public EnemyMove move;
    public EnemyAvoidObstacle avoidObstacle;

    [Header("Patrol")]
    public Vector2 patrolRange = new Vector2(4f, 4f);
    public float patrolPointSnap = 1f;
    public float patrolWaitTime = 1f;
    public float minPatrolPointDistance = 1f;
    public int maxPatrolPointTries = 12;
    public float stuckRepickTime = 1.25f;
    public float minTargetLifetimeBeforeRepick = 2f;
    public float progressEpsilon = 0.05f;

    [Header("Debug")]
    public bool showPatrolDebugGizmos = true;
    public bool drawPatrolDebugAlways = false;
    public float patrolTargetDebugRadius = 0.12f;

    public Vector2 SpawnPoint => spawnPoint;
    public Vector2 CurrentTarget => currentTarget;
    public bool IsWaiting => waitTimer > 0f;
    public bool HasTarget => hasTarget;

    private Vector2 spawnPoint;
    private Vector2 currentTarget;
    private float waitTimer;
    private bool hasTarget;
    private float lastDistanceToTarget = float.MaxValue;
    private float stuckTimer;
    private float targetSetTime;

    private void Awake()
    {
        if (move == null)
        {
            move = GetComponent<EnemyMove>();
        }

        if (avoidObstacle == null)
        {
            avoidObstacle = GetComponent<EnemyAvoidObstacle>();
        }

        spawnPoint = GetAlignedPosition(move != null ? move.Position : (Vector2)transform.position);
        currentTarget = spawnPoint;
    }

    private void OnValidate()
    {
        if (patrolRange.x < 0f) patrolRange.x = 0f;
        if (patrolRange.y < 0f) patrolRange.y = 0f;
        if (patrolPointSnap < 0.01f) patrolPointSnap = 0.01f;
        if (patrolWaitTime < 0f) patrolWaitTime = 0f;
        if (minPatrolPointDistance < 0f) minPatrolPointDistance = 0f;
        if (maxPatrolPointTries < 1) maxPatrolPointTries = 1;
        if (stuckRepickTime < 0f) stuckRepickTime = 0f;
        if (minTargetLifetimeBeforeRepick < 0f) minTargetLifetimeBeforeRepick = 0f;
        if (progressEpsilon < 0.001f) progressEpsilon = 0.001f;
        if (patrolTargetDebugRadius < 0.01f) patrolTargetDebugRadius = 0.01f;
    }

    public void ResetPatrol(Vector2 currentPosition)
    {
        waitTimer = 0f;
        hasTarget = false;
        currentTarget = spawnPoint;
        ResetProgressTracking();
        PickNextTarget(GetReachableAlignedPosition(currentPosition, spawnPoint));
    }

    public void TickPatrol(Vector2 currentPosition, float arriveThreshold)
    {
        currentPosition = GetAlignedPosition(currentPosition);

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                waitTimer = 0f;
                PickNextTarget(currentPosition);
            }

            return;
        }

        if (!hasTarget)
        {
            PickNextTarget(currentPosition);
        }

        if (!hasTarget)
        {
            return;
        }

        float distanceToTarget = Vector2.Distance(currentPosition, currentTarget);
        if (distanceToTarget <= arriveThreshold)
        {
            BeginWait(currentPosition);
            return;
        }

        TrackProgress(distanceToTarget);
        if (stuckRepickTime > 0f && stuckTimer >= stuckRepickTime && HasHeldCurrentTargetLongEnough())
        {
            PickNextTarget(currentPosition);
        }
    }

    public void ForcePickNextTarget(Vector2 currentPosition)
    {
        waitTimer = 0f;
        PickNextTarget(GetAlignedPosition(currentPosition));
    }

    private void PickNextTarget(Vector2 currentPosition)
    {
<<<<<<< HEAD
        // 更改的代码
        Vector2 currentAlignedPos = GetAlignedPosition(currentPosition);

        float minimumDistance = Mathf.Max(patrolPointSnap * 0.5f, minPatrolPointDistance);
        Vector2 bestCandidate = currentAlignedPos; // 原来是这个currentPosition
=======
        float minimumDistance = Mathf.Max(patrolPointSnap * 0.5f, minPatrolPointDistance);
        Vector2 bestCandidate = currentPosition;
>>>>>>> 149ea8bf52f63a4570e3c4931af65fd141369a58
        float bestDistance = 0f;
        bool hasFallbackCandidate = false;

        for (int i = 0; i < maxPatrolPointTries; i++)
        {
            Vector2 randomOffset = new Vector2(
                Random.Range(-patrolRange.x, patrolRange.x),
                Random.Range(-patrolRange.y, patrolRange.y));

<<<<<<< HEAD
            Vector2 candidate = GetReachableAlignedPosition(
                SnapPatrolPoint(currentAlignedPos + randomOffset), currentAlignedPos); // 原来是这个spawnPoint

=======
            Vector2 candidate = GetReachableAlignedPosition(SnapPatrolPoint(spawnPoint + randomOffset), spawnPoint);
>>>>>>> 149ea8bf52f63a4570e3c4931af65fd141369a58
            if (avoidObstacle != null && avoidObstacle.IsPointBlocked(candidate))
            {
                continue;
            }

            float distance = Vector2.Distance(currentPosition, candidate);
            if (distance > bestDistance)
            {
                bestDistance = distance;
                bestCandidate = candidate;
                hasFallbackCandidate = true;
            }

            if (distance < minimumDistance)
            {
                continue;
            }

            SetCurrentTarget(candidate);
            return;
        }

        if (hasFallbackCandidate && bestDistance > 0.02f)
        {
            SetCurrentTarget(bestCandidate);
            return;
        }

        hasTarget = false;
        currentTarget = currentPosition;
        BeginWait(currentPosition);
    }

    private Vector2 SnapPatrolPoint(Vector2 point)
    {
        Vector2 offset = point - spawnPoint;
        offset.x = Mathf.Round(offset.x / patrolPointSnap) * patrolPointSnap;
        offset.y = Mathf.Round(offset.y / patrolPointSnap) * patrolPointSnap;
        return spawnPoint + offset;
    }

    private Vector2 GetAlignedPosition(Vector2 worldPosition)
    {
        if (avoidObstacle != null)
        {
            return avoidObstacle.SnapWorldPositionToGrid(worldPosition);
        }

        return worldPosition;
    }

    private Vector2 GetReachableAlignedPosition(Vector2 worldPosition, Vector2 referencePosition)
    {
        if (avoidObstacle != null)
        {
            return avoidObstacle.SnapWorldPositionToReachableGrid(worldPosition, referencePosition);
        }

        return worldPosition;
    }

    private void SetCurrentTarget(Vector2 target)
    {
        currentTarget = target;
        hasTarget = true;
        waitTimer = 0f;
        targetSetTime = Time.time;
        ResetProgressTracking();
    }

    private void BeginWait(Vector2 currentPosition)
    {
        currentTarget = currentPosition;
        hasTarget = false;
        ResetProgressTracking();
        waitTimer = patrolWaitTime;
    }

    private void TrackProgress(float distanceToTarget)
    {
        if (distanceToTarget + progressEpsilon < lastDistanceToTarget)
        {
            lastDistanceToTarget = distanceToTarget;
            stuckTimer = 0f;
            return;
        }

        stuckTimer += Time.deltaTime;
    }

    private void ResetProgressTracking()
    {
        lastDistanceToTarget = float.MaxValue;
        stuckTimer = 0f;
    }

    private bool HasHeldCurrentTargetLongEnough()
    {
        return Time.time - targetSetTime >= minTargetLifetimeBeforeRepick;
    }

    private float GetPatrolDebugRadius()
    {
        return Mathf.Max(patrolRange.x, patrolRange.y);
    }

    private Vector3 GetPatrolDebugCenter()
    {
        if (Application.isPlaying)
        {
            return spawnPoint;
        }

<<<<<<< HEAD
        return GetAlignedPosition(transform.position); // 原来是这个transform.position
=======
        return transform.position;
>>>>>>> 149ea8bf52f63a4570e3c4931af65fd141369a58
    }

    private void OnDrawGizmos()
    {
        if (drawPatrolDebugAlways)
        {
            DrawPatrolDebugGizmos();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawPatrolDebugAlways)
        {
            DrawPatrolDebugGizmos();
        }
    }

    private void DrawPatrolDebugGizmos()
    {
        if (!showPatrolDebugGizmos)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(GetPatrolDebugCenter(), GetPatrolDebugRadius());

        Vector3 targetPoint = Application.isPlaying ? (Vector3)currentTarget : transform.position;
        Gizmos.DrawSphere(targetPoint, patrolTargetDebugRadius);
    }
}
