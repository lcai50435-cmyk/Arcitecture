using UnityEngine;

/// <summary>
/// Enemy attack logic
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyStatsManager))]
public class EnemyAttack : CharacterAttack
{
    [Header("初始化")]
    public EnemyStatsManager statsManager;
    public Transform player;
    // Attack range trigger
    [SerializeField] private EnemyAttackRangeTrigger2D attackRangeTrigger;

    [Header("攻击设置")]
    [Min(0f)] public float attackInterval = 2f; // Attack interval

    protected float lastAttackTime;  // Last attack time
    private bool isPlayerInRange;  // Tracks whether the player is in attack range

    private void Reset()
    {
        statsManager = GetComponent<EnemyStatsManager>();
        // Automatically find the trigger on the AttackRange child object
        FindAttackRangeTrigger();
    }

    protected override void Awake()
    {  
        base.Awake();

        if (statsManager == null)
        {
            statsManager = GetComponent<EnemyStatsManager>();
        }

        if (attackRangeTrigger == null)
        {
            FindAttackRangeTrigger();
        }

        // Register trigger events
        if (attackRangeTrigger != null)
        {
            attackRangeTrigger.OnPlayerEnterRange += OnPlayerEnterAttackRange;
            attackRangeTrigger.OnPlayerExitRange += OnPlayerExitAttackRange;
        }
    }

    private void OnDestroy()
    {
        // Unregister events to prevent memory leaks
        if (attackRangeTrigger != null)
        {
            attackRangeTrigger.OnPlayerEnterRange -= OnPlayerEnterAttackRange;
            attackRangeTrigger.OnPlayerExitRange -= OnPlayerExitAttackRange;
        }
    }

    private void OnValidate()
    {
        if (attackInterval < 0f) attackInterval = 0f;
        // Automatically find the trigger
        if (attackRangeTrigger == null)
        {
            FindAttackRangeTrigger();
        }
    }

    private void Update()
    {
        if (core != null && core.IsDead)
        {
            return;
        }

        if (statsManager == null || player == null)
        {
            // Automatically get the player target
            statsManager?.ResolvePlayerTargetIfMissing();
            player = statsManager?.PlayerTarget;
            return;
        }

        // Switch state based on whether the player is in range
        if (statsManager.CurrentState == EnemyState.Chase)
        {
            if (isPlayerInRange)
            {
                statsManager.EnterAttackState();
            }
            return;
        }

        if (statsManager.CurrentState != EnemyState.Attack)
        {
            return;
        }

        if (!isPlayerInRange)
        {
            statsManager.EnterChaseState();
            return;
        }

        // Try attacking when the player is in range and the enemy is in attack state
        TryAttack();
    }

    /// <summary>
    /// Automatically finds the trigger component on the AttackRange child object
    /// </summary>
    private void FindAttackRangeTrigger()
    {
        Transform attackRangeTrans = transform.Find("AttackRange");
        if (attackRangeTrans != null)
        {
            attackRangeTrigger = attackRangeTrans.GetComponent<EnemyAttackRangeTrigger2D>();
            // Add one automatically if missing
            if (attackRangeTrigger == null)
            {
                attackRangeTrigger = attackRangeTrans.gameObject.AddComponent<EnemyAttackRangeTrigger2D>();
            }
        }
    }

    /// <summary>
    /// Callback when the player enters attack range
    /// </summary>
    private void OnPlayerEnterAttackRange()
    {
        isPlayerInRange = true;
    }

    /// <summary>
    /// Callback when the player exits attack range
    /// </summary>
    private void OnPlayerExitAttackRange()
    {
        isPlayerInRange = false;
    }

    protected virtual void TryAttack()
    {
        if (core != null && core.IsDead)
        {
            return;
        }

        // Attack cooldown
        if (attackInterval > 0f && Time.time - lastAttackTime < attackInterval)
        {
            return;
        }

        lastAttackTime = Time.time;
        Debug.Log("敌人对玩家发起进攻");
    }

//#if UNITY_EDITOR
//    // Remove the old attack range Gizmos because trigger visualization is now used
//    // If Gizmos are needed, draw the AttackRange child object bounds instead
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
//            // Extend similarly for 3D triggers
//        }
//    }
//#endif
}
