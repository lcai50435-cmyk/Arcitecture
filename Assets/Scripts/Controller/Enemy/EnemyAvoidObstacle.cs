using UnityEngine;

public class EnemyAvoidObstacle : MonoBehaviour
{
    [Header("避障设置")]
    public float detectionRange = 1.2f;
    public float avoidanceForce = 1.5f;
    public float stuckResetTime = 0.6f;

    [Header("层级与标签")]
    public LayerMask obstacleLayers;      // 仅包含 Water, Obstacle, Building
    public LayerMask groundLayer;         // 地面层（用于可选检测）
    public string[] forbiddenTags = { "Water", "Obstacle", "Building" };

    [Header("调试")]
    public bool showDebug = true;

    private EnemyMove move;
    private Vector2 lastPosition;
    private float stuckTimer;
    private float avoidStartTime;          // 绕障开始时间

    private bool isAvoiding = false;
    private Vector2 avoidDirection;
    private Vector2 finalTarget;
    private bool hasFinalTarget;

    void Awake()
    {
        move = GetComponent<EnemyMove>();
        lastPosition = transform.position;
    }

    void Update()
    {
        // 卡住检测（备用方案）
        if (Vector2.Distance(transform.position, lastPosition) < 0.02f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckResetTime && !isAvoiding)
            {
                ForceEscape();
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
            lastPosition = transform.position;
        }

        // 绕障超时保护（超过3秒强制退出）
        if (isAvoiding && Time.time - avoidStartTime > 3f)
        {
            Debug.Log("[避障] 超时强制退出");
            isAvoiding = false;
        }
    }

    public bool CheckAndStartAvoid(Vector2 moveDirection, Vector2 target)
    {
        finalTarget = target;
        hasFinalTarget = true;
        return CheckAndStartAvoidInternal(moveDirection);
    }

    public bool CheckAndStartAvoid(Vector2 moveDirection, Transform target)
    {
        if (target == null) return false;
        finalTarget = target.position;
        hasFinalTarget = true;
        return CheckAndStartAvoidInternal(moveDirection);
    }

    private bool CheckAndStartAvoidInternal(Vector2 moveDirection)
    {
        if (isAvoiding) return true;
        if (moveDirection == Vector2.zero) return false;

        // 使用圆形检测代替射线，避免漏检和误检
        Vector2 forwardPos = (Vector2)transform.position + moveDirection * 0.5f;
        if (!IsObstacleNear(forwardPos))
        {
            Vector2 leftPos = (Vector2)transform.position + RotateVector(moveDirection, 30f) * 0.5f;
            Vector2 rightPos = (Vector2)transform.position + RotateVector(moveDirection, -30f) * 0.5f;
            if (!IsObstacleNear(leftPos) && !IsObstacleNear(rightPos))
                return false;
        }

        // 需要绕障，计算垂直方向（原始垂直方向可能是斜的，但我们会转成四方向）
        Vector2 perp = new Vector2(-moveDirection.y, moveDirection.x);
        Vector2 leftDirRaw = perp;
        Vector2 rightDirRaw = -perp;

        // 强制转换为四方向（水平或垂直）
        Vector2 leftDir = ConvertToFourDirection(leftDirRaw);
        Vector2 rightDir = ConvertToFourDirection(rightDirRaw);

        // 检测两侧是否安全（安全 = 该方向前方无阻碍）
        bool leftSafe = IsDirectionSafe(leftDir);
        bool rightSafe = IsDirectionSafe(rightDir);

        if (!leftSafe && !rightSafe)
        {
            // 两侧都不安全，随机选一个方向（已转四方向）
            avoidDirection = Random.value > 0.5f ? leftDir : rightDir;
        }
        else
        {
            // 优先选择离最终目标更近的方向（使用四方向后的位置比较）
            Vector2 leftTarget = (Vector2)transform.position + leftDir * avoidanceForce;
            Vector2 rightTarget = (Vector2)transform.position + rightDir * avoidanceForce;
            float distLeft = Vector2.Distance(leftTarget, finalTarget);
            float distRight = Vector2.Distance(rightTarget, finalTarget);
            bool preferLeft = leftSafe && (distLeft < distRight || !rightSafe);
            avoidDirection = preferLeft ? leftDir : rightDir;
        }

        // 确保绕行方向不为零向量
        if (avoidDirection == Vector2.zero)
            avoidDirection = moveDirection; // 保底

        isAvoiding = true;
        avoidStartTime = Time.time;
        Debug.Log($"[避障] 启动，绕行方向（四方向） = {avoidDirection}");
        return true;
    }

    /// <summary>
    /// 将任意向量转换为四方向（仅保留绝对值较大的轴向，另一轴清零）
    /// </summary>
    private Vector2 ConvertToFourDirection(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return new Vector2(Mathf.Sign(dir.x), 0);
        else if (Mathf.Abs(dir.y) > Mathf.Abs(dir.x))
            return new Vector2(0, Mathf.Sign(dir.y));
        else
            // 两轴绝对值相等时，优先使用原方向中非零的轴（或默认水平）
            return new Vector2(Mathf.Sign(dir.x), 0);
    }

    private bool IsObstacleNear(Vector2 point)
    {
        Collider2D hit = Physics2D.OverlapCircle(point, 0.3f, obstacleLayers);
        if (hit != null) return true;

        // 额外标签检测
        Collider2D[] colliders = Physics2D.OverlapCircleAll(point, 0.3f);
        foreach (var col in colliders)
        {
            foreach (string tag in forbiddenTags)
            {
                if (col.CompareTag(tag)) return true;
            }
        }
        return false;
    }

    private bool IsDirectionSafe(Vector2 direction)
    {
        Vector2 checkPos = (Vector2)transform.position + direction * 0.6f;
        return !IsObstacleNear(checkPos);
    }

    public void DoAvoidMove()
    {
        if (!isAvoiding || move == null) return;

        move.SetMoveDirection(avoidDirection);

        // 结束条件：前方路径通畅且朝向目标的方向与绕行方向不冲突
        if (hasFinalTarget)
        {
            Vector2 forward = (finalTarget - (Vector2)transform.position).normalized;
            // 检测前方是否还有障碍物
            bool pathClear = !IsObstacleNear((Vector2)transform.position + forward * 0.5f);
            // 如果路径通畅，并且目标方向与当前移动方向夹角小于90度（即大致向前），则退出绕障
            if (pathClear && Vector2.Dot(forward, avoidDirection) > 0)
            {
                isAvoiding = false;
                Debug.Log("[避障] 结束绕障（路径通畅）");
            }
        }
    }

    private void ForceEscape()
    {
        if (!isAvoiding)
        {
            float angle = Random.Range(0f, 360f);
            avoidDirection = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            isAvoiding = true;
            avoidStartTime = Time.time;
            Debug.Log("[避障] 卡住强制逃脱");
        }
    }

    public void ResetAvoid()
    {
        isAvoiding = false;
        stuckTimer = 0f;
        lastPosition = transform.position;
    }

    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebug) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        if (isAvoiding)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, avoidDirection * avoidanceForce);
        }
    }
}