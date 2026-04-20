using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reads the current enemy state and performs only chase / patrol / return movement.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyMove))]
[RequireComponent(typeof(EnemyPatrol))]
[RequireComponent(typeof(EnemyStatsManager))]
public class EnemyChase : MonoBehaviour
{
    [Header("Components")]
    public EnemyMove move;
    public EnemyPatrol patrol;
    public EnemyAvoidObstacle avoidObstacle;
    public EnemyStatsManager statsManager;
    public CircleCollider2D chaseRangeTrigger;

    [Header("Chase")]
    public float arriveThreshold = 0.12f;
    public bool repickPatrolTargetImmediatelyWhenBlocked = true;
    public bool autoExpandOwnTriggerCollider = true;
    public bool onlyExpandOwnTriggerIfSmaller = true;
    public float minOwnCircleTriggerRadius = 2f;
    public Vector2 minOwnBoxTriggerSize = new Vector2(3f, 2.2f);

    [Header("Debug")]
    public bool showChaseDebugGizmos = true;
    public bool drawChaseDebugAlways = false;

    [HideInInspector] public Vector2 targetPos;

    private void Awake()
    {
        if (move == null)
        {
            move = GetComponent<EnemyMove>();
        }

        if (patrol == null)
        {
            patrol = GetComponent<EnemyPatrol>();
        }

        if (avoidObstacle == null)
        {
            avoidObstacle = GetComponent<EnemyAvoidObstacle>();
        }

        if (statsManager == null)
        {
            statsManager = GetComponent<EnemyStatsManager>();
        }

        if (statsManager == null)
        {
            statsManager = gameObject.AddComponent<EnemyStatsManager>();
        }

        ResolveChaseRangeTrigger();
        EnsureChaseRangeTriggerRelay();
        ConfigureOwnTriggerColliders();
    }

    private void OnEnable()
    {
        if (statsManager == null)
        {
            statsManager = GetComponent<EnemyStatsManager>();
        }

        if (statsManager != null)
        {
            statsManager.OnStateChanged += HandleStateChanged;
        }
    }

    private void Start()
    {
        statsManager?.ResolvePlayerTargetIfMissing();
        HandleStateChanged(statsManager != null ? statsManager.CurrentState : EnemyState.Patrol);
    }

    private void Update()
    {
        UpdateStateMachine();
    }

    private void FixedUpdate()
    {
        UpdateMovement();
    }

    private void OnDisable()
    {
        if (statsManager != null)
        {
            statsManager.OnStateChanged -= HandleStateChanged;
        }

        avoidObstacle?.ResetAvoidance();
        move?.StopMovement();
    }

    private void OnValidate()
    {
        if (arriveThreshold < 0.01f) arriveThreshold = 0.01f;
        if (minOwnCircleTriggerRadius < 0f) minOwnCircleTriggerRadius = 0f;
        if (minOwnBoxTriggerSize.x < 0f) minOwnBoxTriggerSize.x = 0f;
        if (minOwnBoxTriggerSize.y < 0f) minOwnBoxTriggerSize.y = 0f;

        ResolveChaseRangeTrigger();
        EnsureChaseRangeTriggerRelay();
        ConfigureOwnTriggerColliders();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (chaseRangeTrigger != null)
        {
            return;
        }

        NotifyPlayerEnteredRange(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (chaseRangeTrigger != null)
        {
            return;
        }

        NotifyPlayerExitedRange(other);
    }

    public void NotifyPlayerEnteredRange(Collider2D other)
    {
        statsManager?.NotifyPlayerEnteredRange(other);
    }

    public void NotifyPlayerExitedRange(Collider2D other)
    {
        statsManager?.NotifyPlayerExitedRange(other);
    }

    private void UpdateStateMachine()
    {
        if (move == null || patrol == null || statsManager == null)
        {
            return;
        }

        statsManager.ResolvePlayerTargetIfMissing();

        if ((statsManager.CurrentState == EnemyState.Chase || statsManager.CurrentState == EnemyState.Attack) &&
            statsManager.PlayerTarget == null)
        {
            statsManager.HandleMissingPlayerTarget();
            return;
        }

        switch (statsManager.CurrentState)
        {
            case EnemyState.Patrol:
                patrol.TickPatrol(move.Position, arriveThreshold);
                break;

            case EnemyState.Chase:
            case EnemyState.Attack:
                if (statsManager.PlayerTarget != null)
                {
                    targetPos = statsManager.PlayerTarget.position;
                }
                break;

            case EnemyState.ReturnToSpawn:
                if (move.HasArrived(patrol.SpawnPoint, arriveThreshold))
                {
                    statsManager.NotifyReturnedToSpawn();
                }
                break;
        }
    }

    private void UpdateMovement()
    {
        if (move == null || patrol == null || statsManager == null)
        {
            return;
        }

        switch (statsManager.CurrentState)
        {
            case EnemyState.Patrol:
                if (patrol.IsWaiting)
                {
                    move.StopMovement();
                }
                else
                {
                    targetPos = patrol.CurrentTarget;
                    MoveToTarget(targetPos);
                }
                break;

            case EnemyState.Chase:
                if (statsManager.PlayerTarget != null)
                {
                    targetPos = statsManager.PlayerTarget.position;
                    MoveToTarget(targetPos);
                }
                else
                {
                    move.StopMovement();
                }
                break;

            case EnemyState.ReturnToSpawn:
                targetPos = patrol.SpawnPoint;
                MoveToTarget(targetPos);
                break;

            case EnemyState.Attack:
                move.StopMovement();
                break;
        }
    }

    private void MoveToTarget(Vector2 destination)
    {
        if (move.HasArrived(destination, arriveThreshold))
        {
            move.StopMovement();
            return;
        }

        Vector2 preferredDirection = move.GetDirectionTo(destination);

        if (statsManager != null &&
            statsManager.CurrentState == EnemyState.Patrol &&
            repickPatrolTargetImmediatelyWhenBlocked &&
            preferredDirection != Vector2.zero &&
            avoidObstacle != null &&
            avoidObstacle.IsDirectionBlocked(move.Position, preferredDirection))
        {
            patrol?.ForcePickNextTarget(move.Position);
            move.StopMovement();
            return;
        }

        Vector2 finalDirection = preferredDirection;

        if (avoidObstacle != null)
        {
            finalDirection = avoidObstacle.ResolveDirection(move.Position, destination, preferredDirection);
        }

        if (statsManager != null &&
            statsManager.CurrentState == EnemyState.Patrol &&
            repickPatrolTargetImmediatelyWhenBlocked &&
            finalDirection == Vector2.zero)
        {
            patrol?.ForcePickNextTarget(move.Position);
            move.StopMovement();
            return;
        }

        move.SetMoveDirection(finalDirection);
    }

    private void HandleStateChanged(EnemyState state)
    {
        if (move == null || patrol == null)
        {
            return;
        }

        avoidObstacle?.ResetAvoidance();

        switch (state)
        {
            case EnemyState.Patrol:
                patrol.ResetPatrol(move.Position);
                targetPos = patrol.CurrentTarget;
                break;

            case EnemyState.Chase:
                if (statsManager != null && statsManager.PlayerTarget != null)
                {
                    targetPos = statsManager.PlayerTarget.position;
                }
                break;

            case EnemyState.ReturnToSpawn:
                targetPos = patrol.SpawnPoint;
                break;

            case EnemyState.Attack:
                move.StopMovement();
                if (statsManager != null && statsManager.PlayerTarget != null)
                {
                    targetPos = statsManager.PlayerTarget.position;
                }
                break;
        }
    }

    private void ResolveChaseRangeTrigger()
    {
        if (chaseRangeTrigger != null)
        {
            return;
        }

        Transform chaseCircle = transform.Find("ChaseCircle");
        if (chaseCircle != null)
        {
            CircleCollider2D namedCollider = chaseCircle.GetComponent<CircleCollider2D>();
            if (namedCollider != null)
            {
                chaseRangeTrigger = namedCollider;
                return;
            }
        }

        CircleCollider2D[] childCircles = GetComponentsInChildren<CircleCollider2D>(true);
        for (int i = 0; i < childCircles.Length; i++)
        {
            CircleCollider2D circle = childCircles[i];
            if (circle != null && circle.isTrigger)
            {
                chaseRangeTrigger = circle;
                return;
            }
        }
    }

    private void EnsureChaseRangeTriggerRelay()
    {
        if (chaseRangeTrigger != null)
        {
            chaseRangeTrigger.isTrigger = true;
            EnemyChaseTrigger2D relay = chaseRangeTrigger.GetComponent<EnemyChaseTrigger2D>();
            if (relay == null)
            {
                relay = chaseRangeTrigger.gameObject.AddComponent<EnemyChaseTrigger2D>();
            }

            relay.SetOwner(this);
            return;
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (col == null || col.transform == transform || !col.isTrigger)
            {
                continue;
            }

            EnemyChaseTrigger2D relay = col.GetComponent<EnemyChaseTrigger2D>();
            if (relay != null)
            {
                relay.SetOwner(this);
                return;
            }
        }
    }

    private void ConfigureOwnTriggerColliders()
    {
        if (!autoExpandOwnTriggerCollider)
        {
            return;
        }

        CircleCollider2D[] circles = GetComponents<CircleCollider2D>();
        for (int i = 0; i < circles.Length; i++)
        {
            CircleCollider2D circle = circles[i];
            if (circle == null || !circle.isTrigger)
            {
                continue;
            }

            circle.radius = onlyExpandOwnTriggerIfSmaller
                ? Mathf.Max(circle.radius, minOwnCircleTriggerRadius)
                : minOwnCircleTriggerRadius;
        }

        BoxCollider2D[] boxes = GetComponents<BoxCollider2D>();
        for (int i = 0; i < boxes.Length; i++)
        {
            BoxCollider2D box = boxes[i];
            if (box == null || !box.isTrigger)
            {
                continue;
            }

            box.size = onlyExpandOwnTriggerIfSmaller
                ? new Vector2(
                    Mathf.Max(box.size.x, minOwnBoxTriggerSize.x),
                    Mathf.Max(box.size.y, minOwnBoxTriggerSize.y))
                : minOwnBoxTriggerSize;
        }
    }

    private void OnDrawGizmos()
    {
        if (drawChaseDebugAlways)
        {
            DrawChaseDebugGizmos();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawChaseDebugAlways)
        {
            DrawChaseDebugGizmos();
        }
    }

    private void DrawChaseDebugGizmos()
    {
        if (!showChaseDebugGizmos)
        {
            return;
        }

        Gizmos.color = Color.green;

        if (chaseRangeTrigger != null)
        {
            DrawTriggerColliderGizmo(chaseRangeTrigger);
            return;
        }

        bool drewAnyTrigger = false;
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (col == null || !col.isTrigger)
            {
                continue;
            }

            DrawTriggerColliderGizmo(col);
            drewAnyTrigger = true;
        }

        if (!drewAnyTrigger)
        {
            Gizmos.DrawWireSphere(transform.position, minOwnCircleTriggerRadius);
        }
    }

    private void DrawTriggerColliderGizmo(Collider2D col)
    {
        if (col is CircleCollider2D circle)
        {
            float scale = Mathf.Max(
                Mathf.Abs(circle.transform.lossyScale.x),
                Mathf.Abs(circle.transform.lossyScale.y));
            Vector3 center = circle.transform.TransformPoint(circle.offset);
            Gizmos.DrawWireSphere(center, circle.radius * scale);
            return;
        }

        if (col is BoxCollider2D box)
        {
            Vector3 center = box.transform.TransformPoint(box.offset);
            Vector3 scale = box.transform.lossyScale;
            Vector3 size = new Vector3(
                Mathf.Abs(box.size.x * scale.x),
                Mathf.Abs(box.size.y * scale.y),
                0.02f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
