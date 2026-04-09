using UnityEngine;

/// <summary>
/// 敌人攻击行为：仅在攻击状态下执行
/// </summary>
public class EnemyAttack : MonoBehaviour
{
    [Header("攻击设置")]
    public float attackRange = 1f; // 玩家攻击范围
    public float attackDuration = 1f; // 攻击持续时间
    public float attackCooldown = 0.5f; // 攻击冷却时间

    private bool isAttacking = false; // 判断是否正在攻击
    private bool isOnCooldown = false; // 是否冷却时间
    private Transform currentTarget; // 敌人攻击目标位置
    private EnemyStateManager stateManager;
    private EnemyMove move;

    void Start()
    {
        stateManager = GetComponent<EnemyStateManager>();
        move = GetComponent<EnemyMove>();
    }

    /// <summary>
    /// 由 StateManager 每帧调用
    /// </summary>
    public void DoAttack()
    {
        if (currentTarget == null || isAttacking || isOnCooldown) return;

        // 超出攻击范围 则 切回追逐状态
        if (Vector2.Distance(transform.position, currentTarget.position) > attackRange)
        {
            stateManager.SetAttack(false);
            stateManager.SetChase(true);
            return;
        }

        // 停止移动，执行攻击
        move.SetMoveDirection(Vector2.zero);
        StartAttack();
    }

    /// <summary>
    /// 执行攻击逻辑
    /// </summary>
    void StartAttack()
    {
        isAttacking = true;

        Debug.Log("敌人发起攻击！");
        // 示例：播放攻击动画
        // move.animator?.SetTrigger("Attack");

        // 攻击结束回调
        Invoke(nameof(EndAttack), attackDuration);
    }

    /// <summary>
    /// 攻击结束
    /// </summary>
    void EndAttack()
    {
        isAttacking = false;
        // 开启攻击冷却
        isOnCooldown = true;
        Invoke(nameof(EndCooldown), attackCooldown);

        // 冷却期间如果仍在攻击范围，保持攻击状态；否则切回追逐
        if (currentTarget != null && Vector2.Distance(transform.position, currentTarget.position) > attackRange)
        {
            stateManager.SetAttack(false);
            stateManager.SetChase(true);
        }
    }

    /// <summary>
    /// 攻击冷却结束
    /// </summary>
    void EndCooldown()
    {
        isOnCooldown = false;
    }

    /// <summary>
    /// 设置攻击目标（由Chase模块传递）
    /// </summary>
    public void SetTarget(Transform target)
    {
        currentTarget = target;
    }

    /// <summary>
    /// 清空攻击目标（停止攻击/追逐时调用）
    /// </summary>
    public void ClearTarget()
    {
        currentTarget = null;
        isAttacking = false;
        isOnCooldown = false;
        // 取消所有未执行的攻击回调
        CancelInvoke(nameof(EndAttack));
        CancelInvoke(nameof(EndCooldown));
    }
}