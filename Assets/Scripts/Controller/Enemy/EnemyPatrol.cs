using UnityEngine;

/// <summary>
/// 敌人巡逻（严格四方向：先X后Y，不斜向、不偏移、完全稳定）
/// 决定敌人巡逻时的移动方向
/// </summary>
public class EnemyPatrol : MonoBehaviour
{
    [Header("巡逻设置")]
    public EnemyMove enemyMove;
    public float patrolRange = 4f;   // 巡逻范围
    public float waitTime = 1.5f;     // 到达后等待时间

    private Vector2 originPos;        // 出生中心点
    private Vector2 targetPos;        // 最终目标点
    private float waitCounter;

    // 巡逻状态：先X后Y
    private bool isMovingX = false;
    private bool isMovingY = false;

    private EnemyObstacleAvoidance obstacleAvoidance;

    void Start()
    {
        originPos = transform.position;
        waitCounter = waitTime;
        SetNewRandomTarget();

        obstacleAvoidance = GetComponent<EnemyObstacleAvoidance>();
    }

    void Update()
    {
        // 如果正在等待，倒计时
        if (waitCounter > 0)
        {
            waitCounter -= Time.deltaTime;
            enemyMove.SetMoveDirection(Vector2.zero); // 待机
            return;
        }

        // 1. 先处理 X 方向移动
        if (!isMovingX && !isMovingY)
        {
            isMovingX = true;
        }

        // 2. 移动 X
        if (isMovingX)
        {
            MoveX();
        }
        // 3. X 走完 再 移动 Y
        else if (isMovingY)
        {
            MoveY();
        }
    }

    /// <summary>
    /// 先生成随机目标点
    /// </summary>
    void SetNewRandomTarget()
    {
        targetPos = originPos + Random.insideUnitCircle * patrolRange;
        isMovingX = false;
        isMovingY = false;
    }

    /// <summary>
    /// 第一步：走 X 轴
    /// </summary>
    void MoveX()
    {
        float dx = targetPos.x - transform.position.x;

        // X 已经到了 再 切换到走 Y
        if (Mathf.Abs(dx) < 0.1f)
        {
            isMovingX = false;
            isMovingY = true;
            enemyMove.SetMoveDirection(Vector2.zero);
            return;
        }

        // 否则继续走 X
        Vector2 dir = new Vector2(Mathf.Sign(dx), 0);
        enemyMove.SetMoveDirection(dir);
    }

    /// <summary>
    /// 第二步：走 Y 轴
    /// </summary>
    void MoveY()
    {
        float dy = targetPos.y - transform.position.y;

        // Y 也到了 再 到达目标点
        if (Mathf.Abs(dy) < 0.1f)
        {
            isMovingY = false;
            waitCounter = waitTime;
            SetNewRandomTarget(); // 下一个点
            enemyMove.SetMoveDirection(Vector2.zero);
            return;
        }

        // 否则继续走 Y
        Vector2 dir = new Vector2(0, Mathf.Sign(dy));

        Vector2 finalDir = obstacleAvoidance.GetAvoidedDirection(dir);
        enemyMove.SetMoveDirection(finalDir);
    }
}