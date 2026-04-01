using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人自动避障（独立脚本，不影响巡逻/移动）
/// 检测：除了玩家 + 地面以外的所有物体
/// </summary>
public class EnemyObstacleAvoidance : MonoBehaviour
{
    [Header("避障设置")]
    public float checkDistance = 0.6f;   // 检测距离
    public float avoidStrength = 3f;     // 避障强度（越大越灵敏）

    [Header("需要忽略的层")]
    public LayerMask ignoreLayers;       // 勾选 Player + Ground

    private Rigidbody2D rb;
    private EnemyMove enemyMove;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyMove = GetComponent<EnemyMove>();
    }

    /// <summary>
    /// 传入当前移动方向，返回避障后的方向
    /// </summary>
    public Vector2 GetAvoidedDirection(Vector2 originalDir)
    {
        if (originalDir.magnitude < 0.1f) return originalDir;

        Vector2 rightCheck = Quaternion.Euler(0, 0, 25) * originalDir;
        Vector2 leftCheck = Quaternion.Euler(0, 0, -25) * originalDir;

        bool hitRight = CheckObstacle(rightCheck);
        bool hitLeft = CheckObstacle(leftCheck);
        bool hitForward = CheckObstacle(originalDir);

        float avoidX = 0;
        float avoidY = 0;

        if (hitForward)
        {
            if (!hitRight) avoidX = 1;
            else if (!hitLeft) avoidX = -1;
            else avoidX = -originalDir.x;
        }
        else
        {
            if (hitRight) avoidX = -1;
            if (hitLeft) avoidX = 1;
        }

        Vector2 avoidDir = new Vector2(avoidX, avoidY).normalized;
        Vector2 finalDir = (originalDir + avoidDir * avoidStrength).normalized;

        return finalDir;
    }

    /// <summary>
    /// 检测前方是否有障碍物（忽略玩家+地面）
    /// </summary>
    private bool CheckObstacle(Vector2 dir)
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            dir,
            checkDistance,
            ~ignoreLayers        // 忽略指定层，其余都算障碍物
        );

        Debug.DrawRay(transform.position, dir * checkDistance, hit ? Color.red : Color.green);
        return hit;
    }
}
