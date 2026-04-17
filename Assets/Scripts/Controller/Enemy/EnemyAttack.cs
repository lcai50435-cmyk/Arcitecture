using UnityEngine;

/// <summary>
<<<<<<< HEAD
/// 敌人相关攻击逻辑
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyStatsManager))]
public class EnemyAttack : CharacterAttack
{
    [Header("初始化")]
    public EnemyStatsManager statsManager;
    public Transform player;
    // 攻击范围触发器
    [SerializeField] private EnemyAttackRangeTrigger2D attackRangeTrigger;

    [Header("攻击设置")]
    [Min(0f)] public float attackInterval = 2f; // 攻击间隔

    protected float lastAttackTime;  // 上次攻击时间
    private bool isPlayerInRange;  // 标记玩家是否在攻击范围内
=======
/// Switches between chase and attack states and simulates attack output.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyStatsManager))]
public class EnemyAttack : MonoBehaviour
{
    [Header("References")]
    public EnemyStatsManager statsManager;
    public Transform player;

    [Header("Attack Settings")]
    [Min(0f)] public float attackRange = 1.5f;
    [Min(0f)] public float attackInterval = 1.2f;

    private float lastAttackTime;
>>>>>>> 149ea8bf52f63a4570e3c4931af65fd141369a58

    private void Reset()
    {
        statsManager = GetComponent<EnemyStatsManager>();
<<<<<<< HEAD
        // 自动查找子物体AttackRange上的触发器
        FindAttackRangeTrigger();
    }

    protected override void Awake()
    {  
        base.Awake();

=======
    }

    private void Awake()
    {
>>>>>>> 149ea8bf52f63a4570e3c4931af65fd141369a58
        if (statsManager == null)
        {
            statsManager = GetComponent<EnemyStatsManager>();
        }
<<<<<<< HEAD

        if (attackRangeTrigger == null)
        {
            FindAttackRangeTrigger();
        }

        // 注册触发器事件
        if (attackRangeTrigger != null)
        {
            attackRangeTrigger.OnPlayerEnterRange += OnPlayerEnterAttackRange;
            attackRangeTrigger.OnPlayerExitRange += OnPlayerExitAttackRange;
        }
    }

    private void OnDestroy()
    {
        // 解注册事件，防止内存泄漏
        if (attackRangeTrigger != null)
        {
            attackRangeTrigger.OnPlayerEnterRange -= OnPlayerEnterAttackRange;
            attackRangeTrigger.OnPlayerExitRange -= OnPlayerExitAttackRange;
        }
=======
>>>>>>> 149ea8bf52f63a4570e3c4931af65fd141369a58
    }

    private void OnValidate()
    {
<<<<<<< HEAD
        if (attackInterval < 0f) attackInterval = 0f;
        // 自动查找触发器
        if (attackRangeTrigger == null)
        {
            FindAttackRangeTrigger();
        }
=======
        if (attackRange < 0f) attackRange = 0f;
        if (attackInterval < 0f) attackInterval = 0f;
>>>>>>> 149ea8bf52f63a4570e3c4931af65fd141369a58
    }

    private void Update()
    {
<<<<<<< HEAD
        if (statsManager == null || player == null)
        {
            // 自动获取玩家目标
            statsManager?.ResolvePlayerTargetIfMissing();
            player = statsManager?.PlayerTarget;
            return;
        }

        // 根据玩家是否在范围切换状态
        if (statsManager.CurrentState == EnemyState.Chase)
        {
            if (isPlayerInRange)
            {
                statsManager.EnterAttackState();
            }
=======
        if (statsManager == null)
        {
            return;
        }

        if (player == null)
        {
            statsManager.ResolvePlayerTargetIfMissing();
            player = statsManager.PlayerTarget;
        }

        if (player == null)
        {
            return;
        }

        float sqrDistance = (player.position - transform.position).sqrMagnitude;
        float sqrRange = attackRange * attackRange;

        if (statsManager.CurrentState == EnemyState.Chase)
        {
            if (sqrDistance <= sqrRange)
            {
                statsManager.EnterAttackState();
            }

>>>>>>> 149ea8bf52f63a4570e3c4931af65fd141369a58
            return;
        }

        if (statsManager.CurrentState != EnemyState.Attack)
        {
            return;
        }

<<<<<<< HEAD
        if (!isPlayerInRange)
=======
        if (sqrDistance > sqrRange)
>>>>>>> 149ea8bf52f63a4570e3c4931af65fd141369a58
        {
            statsManager.EnterChaseState();
            return;
        }

<<<<<<< HEAD
        // 玩家在攻击范围内且处于攻击状态，尝试攻击
        TryAttack();
    }

    /// <summary>
    /// 自动查找AttackRange子物体上的触发器组件
    /// </summary>
    private void FindAttackRangeTrigger()
    {
        Transform attackRangeTrans = transform.Find("AttackRange");
        if (attackRangeTrans != null)
        {
            attackRangeTrigger = attackRangeTrans.GetComponent<EnemyAttackRangeTrigger2D>();
            // 如果没有则自动添加
            if (attackRangeTrigger == null)
            {
                attackRangeTrigger = attackRangeTrans.gameObject.AddComponent<EnemyAttackRangeTrigger2D>();
            }
        }
    }

    /// <summary>
    /// 玩家进入攻击范围回调
    /// </summary>
    private void OnPlayerEnterAttackRange()
    {
        isPlayerInRange = true;
    }

    /// <summary>
    /// 玩家离开攻击范围回调
    /// </summary>
    private void OnPlayerExitAttackRange()
    {
        isPlayerInRange = false;
    }

    protected virtual void TryAttack()
    {
        // 攻击时间冷却
=======
        TryAttack();
    }

    private void TryAttack()
    {
>>>>>>> 149ea8bf52f63a4570e3c4931af65fd141369a58
        if (attackInterval > 0f && Time.time - lastAttackTime < attackInterval)
        {
            return;
        }

        lastAttackTime = Time.time;
        Debug.Log("敌人对玩家发起进攻");
    }

<<<<<<< HEAD
//#if UNITY_EDITOR
//    // 移除原有攻击范围Gizmos（因为改用触发器可视化）
//    // 如果需要保留Gizmos，可改为绘制AttackRange子物体的范围
//    private void OnDrawGizmosSelected()
//    {
//        if (attackRangeTrigger != null)
//        {
//            UnityEditor.Handles.color = Color.red;
//            Collider2D col = attackRangeTrigger.GetComponent<Collider2D>();
//            if (col is CircleCollider2D circleCol)
//            {
//                UnityEditor.Handles.DrawWireDisc(attackRangeTrigger.transform.position,
//                    Vector3.forward, circleCol.radius);
//            }
//            else if (col is BoxCollider2D boxCol)
//            {
//                UnityEditor.Handles.DrawWireCube(attackRangeTrigger.transform.position + (Vector3)boxCol.offset,
//                    boxCol.size);
//            }
//            // 3D触发器同理扩展
//        }
//    }
//#endif
}
=======
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (attackRange <= 0f)
        {
            return;
        }

        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, attackRange);
    }
#endif
}
>>>>>>> 149ea8bf52f63a4570e3c4931af65fd141369a58
