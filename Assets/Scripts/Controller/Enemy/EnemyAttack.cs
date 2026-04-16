using UnityEngine;

/// <summary>
/// 敌人相关攻击逻辑
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyStatsManager))]
public class EnemyAttack : CharacterAttack
{
    [Header("References")]
    public EnemyStatsManager statsManager;
    public Transform player;

    [Header("Attack Settings")]
    [Min(0f)] public float attackRange = 1.5f;
    [Min(0f)] public float attackInterval = 1.2f;

    private float lastAttackTime;

    private void Reset()
    {
        statsManager = GetComponent<EnemyStatsManager>();
    }

    private void Awake()
    {
        if (statsManager == null)
        {
            statsManager = GetComponent<EnemyStatsManager>();
        }
    }

    private void OnValidate()
    {
        if (attackRange < 0f) attackRange = 0f;
        if (attackInterval < 0f) attackInterval = 0f;
    }

    private void Update()
    {
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

            return;
        }

        if (statsManager.CurrentState != EnemyState.Attack)
        {
            return;
        }

        if (sqrDistance > sqrRange)
        {
            statsManager.EnterChaseState();
            return;
        }

        TryAttack();
    }

    private void TryAttack()
    {
        if (attackInterval > 0f && Time.time - lastAttackTime < attackInterval)
        {
            return;
        }

        lastAttackTime = Time.time;
        Debug.Log("敌人对玩家发起进攻");
    }

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
