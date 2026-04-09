using UnityEngine;

/// <summary>
/// 敌人状态枚举
/// </summary>
public enum EnemyState
{
    Patrol, // 巡逻
    Chase,  // 追击
    Attack  // 攻击
}

/// <summary>
/// 敌人状态管理器：负责状态切换和逻辑分发
/// </summary>
public class EnemyStateManager : MonoBehaviour
{
    public EnemyState currentState; // 当前状态

    [HideInInspector] public bool isChase; // 是否处于追击状态
    [HideInInspector] public bool isAttack; // 是否处于攻击状态

    public EnemyPatrol patrol; // 巡逻组件
    public EnemyChase chase; // 追击组件
    public EnemyAttack attack; // 攻击组件

    private void Update()
    {
        // 状态优先级：攻击 > 追击 > 巡逻
        UpdateCurrentState();

        // 执行当前状态对应的逻辑
        ExecuteCurrentStateLogic();
    }

    /// <summary>
    /// 更新当前状态
    /// </summary>
    private void UpdateCurrentState()
    {
        if (isAttack)
            currentState = EnemyState.Attack;
        else if (isChase)
            currentState = EnemyState.Chase;
        else
            currentState = EnemyState.Patrol;
    }

    /// <summary>
    /// 执行当前状态的核心逻辑
    /// </summary>
    private void ExecuteCurrentStateLogic()
    {
        switch (currentState)
        {
            case EnemyState.Patrol:
                patrol.ExecutePatrol();
                break;
            case EnemyState.Chase:
                chase.ExecuteChase();
                break;
            case EnemyState.Attack:
                attack.DoAttack();
                break;
        }
    }

    // 状态设置封装
    public void SetChase(bool value) => isChase = value;
    public void SetAttack(bool value) => isAttack = value;
}