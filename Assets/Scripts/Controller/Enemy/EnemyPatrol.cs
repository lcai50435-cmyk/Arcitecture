using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [Header("巡逻设置")]
    public float patrolRange = 3f;
    public float waitTime = 2f;

    [Header("禁止生成点配置")]
    public LayerMask obstacleLayers;
    public string[] forbiddenTags = new string[] { "Water", "Building" };
    public float forbiddenCheckRadius = 1f;

    [HideInInspector] public Vector2 originPos;
    [HideInInspector] public Vector2 currentTarget;

    private float waitCounter;
    private bool isWaiting;

    private EnemyMove move;
    private EnemyAvoidObstacle avoidObstacle;

    void Awake()
    {
        originPos = transform.position;
    }

    void Start()
    {
        move = GetComponent<EnemyMove>();
        avoidObstacle = GetComponent<EnemyAvoidObstacle>();
        waitCounter = waitTime;
        SetNewRandomTarget();
    }

    public void ExecutePatrol()
    {
        if (isWaiting)
        {
            if (waitCounter > 0)
            {
                waitCounter -= Time.deltaTime;
                move.SetMoveDirection(Vector2.zero);
            }
            else
            {
                isWaiting = false;
                SetNewRandomTarget();
            }
            return;
        }

        // 前进方向
        Vector2 forwardDir = (currentTarget - (Vector2)transform.position).normalized;

        // 避障检测：如果启动绕障，则执行绕障移动，不再继续正常移动
        if (avoidObstacle != null && avoidObstacle.CheckAndStartAvoid(forwardDir, currentTarget))
        {
            avoidObstacle.DoAvoidMove();
            return;   // 关键：绕障期间不执行下面的正常移动
        }

        // 正常巡逻移动
        move.SetMoveDirection(forwardDir);

        // 到达目标点后进入等待
        if (Vector2.Distance(transform.position, currentTarget) < 0.2f)
        {
            isWaiting = true;
            waitCounter = waitTime;
        }
    }

    public void ReturnToOrigin()
    {
        Vector2 dir = (originPos - (Vector2)transform.position).normalized;

        // 回原点时也检测避障（传入原点作为目标）
        if (avoidObstacle != null && avoidObstacle.CheckAndStartAvoid(dir, originPos))
        {
            avoidObstacle.DoAvoidMove();
            return;
        }

        move.SetMoveDirection(dir);

        if (Vector2.Distance(transform.position, originPos) < 0.2f)
        {
            SetNewRandomTarget();
            isWaiting = false;
        }
    }

    public void SetNewRandomTarget()
    {
        Vector2 point;
        int maxTry = 10;
        bool isPointValid = false;

        do
        {
            point = originPos + Random.insideUnitCircle * patrolRange;
            maxTry--;

            bool isObstacle = Physics2D.OverlapCircle(point, 0.15f, obstacleLayers);
            bool hasForbiddenTag = CheckForbiddenTagsInRange(point, forbiddenCheckRadius);

            isPointValid = !isObstacle && !hasForbiddenTag;

        } while (maxTry > 0 && !isPointValid);

        currentTarget = isPointValid ? point : originPos;
    }

    private bool CheckForbiddenTagsInRange(Vector2 position, float radius)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, radius);
        foreach (Collider2D col in colliders)
        {
            foreach (string tag in forbiddenTags)
            {
                if (col.CompareTag(tag))
                    return true;
            }
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(currentTarget, 0.2f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(currentTarget, forbiddenCheckRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(originPos, patrolRange);
    }
}