using UnityEngine;

/// <summary>
/// 敌人追击逻辑：触发追击、停止追击、向玩家移动
/// </summary>
public class EnemyChase : MonoBehaviour
{
    [Header("追击配置")]
    [Tooltip("追击移动速度")]
    public float chaseSpeed = 3.5f;

    private Transform _playerTransform; // 玩家位置
    private EnemyStateManager _stateManager; // 状态管理器
    private EnemyMove _enemyMove; // 移动组件
    private EnemyAvoidObstacle _avoidObstacle; // 避障组件

    private void Start()
    {
        // 初始化核心组件
        _stateManager = GetComponent<EnemyStateManager>();
        _enemyMove = GetComponent<EnemyMove>();
        _avoidObstacle = GetComponent<EnemyAvoidObstacle>();

        // 初始化玩家引用
        _playerTransform = GameObject.FindWithTag("Player")?.transform;
    }

    /// <summary>
    /// 执行追击逻辑（由状态管理器每帧调用）
    /// </summary>
    public void ExecuteChase()
    {
        if (_playerTransform == null) return;

        // 进入攻击范围，切换攻击状态
        if (Vector2.Distance(transform.position, _playerTransform.position) <= _stateManager.attack.attackRange)
        {
            _stateManager.SetAttack(true);
            _stateManager.attack.SetTarget(_playerTransform);
            return;
        }

        // 计算朝向玩家的移动方向
        Vector2 chaseDirection = (_playerTransform.position - transform.position).normalized;

        // 追击时避障
        if (_avoidObstacle.CheckAndStartAvoid(chaseDirection, _playerTransform.position))
        {
            _avoidObstacle.DoAvoidMove();
            return;
        }

        _enemyMove.SetMoveDirection(chaseDirection);
    }

    /// <summary>
    /// 开始追击（由ChaseSensor触发器调用）
    /// </summary>
    public void StartChase()
    {
        if (_playerTransform == null)
        {
            _playerTransform = GameObject.FindWithTag("Player")?.transform;
        }

        _stateManager.SetChase(true);
        _stateManager.SetAttack(false);
    }

    /// <summary>
    /// 停止追击（由ChaseSensor触发器调用）
    /// </summary>
    public void StopChase()
    {
        _stateManager.SetChase(false);
        _stateManager.patrol.ReturnToOrigin(); // 追击结束，返回出生点
        _stateManager.attack.ClearTarget();
    }
}