using UnityEngine;

/// <summary>
/// 通用朝向跟踪工具类
/// 记录角色（玩家/敌人）的最后移动朝向，支持获取攻击/待机朝向
/// </summary>
public class DirectionTracker : MonoBehaviour
{
    // 最后一次有效移动的朝向（X/Y轴）
    private Vector2 lastDirection;
    // 默认朝向（比如角色初始朝右）
    [Header("默认朝向")]
    public Vector2 defaultDirection = Vector2.up;

    /// <summary>
    /// 获取最后面朝方向（对外只读）
    /// </summary>
    public Vector2 LastDirection
    {
        get
        {
            // 如果从未移动过，返回默认朝向
            return lastDirection.magnitude < 0.1f ? defaultDirection : lastDirection;
        }
    }

    /// <summary>
    /// 更新移动朝向（角色移动时调用）
    /// </summary>
    /// <param name="currentMoveDir">当前移动方向</param>
    public void UpdateMoveDirection(Vector2 currentMoveDir)
    {
        // 过滤无效输入（避免微小数值干扰）
        if (currentMoveDir.magnitude > 0.1f)
        {
            // 归一化，保证方向向量长度为1
            lastDirection = currentMoveDir.normalized;
        }
    }

    /// <summary>
    /// 重置朝向（可选，比如角色重生时）
    /// </summary>
    public void ResetDirection()
    {
        lastDirection = defaultDirection;
    }
}