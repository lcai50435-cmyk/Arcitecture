using UnityEngine;

public class FireMonAttack : EnemyAttack
{
    private DirectionTracker directionTracker;
    private Animator animator;

    [Header("火球攻击设置")]
    public GameObject fireballPrefab; // 火球预制体
    public Transform firePoint;       // 火球发射点

    protected override void Awake()
    {
        // 初始化自身依赖的组件
        directionTracker = GetComponent<DirectionTracker>();
        animator = GetComponent<Animator>();

        // 执行父类的 Awake 逻辑
        base.Awake();

        // 防止没挂 DirectionTracker，自动添加
        if (directionTracker == null)
        {
            directionTracker = gameObject.AddComponent<DirectionTracker>();
            Debug.LogWarning("自动为 FireMon 添加了 DirectionTracker 组件", this);
        }
    }

    // 重写 EnemyAttack 的 TryAttack 方法
    protected override void TryAttack()
    {
        // 攻击时间冷却
        if (attackInterval > 0f && Time.time - lastAttackTime < attackInterval)
        {
            return; // 冷却没好
        }

        // 先执行父类的攻击冷却检测
        base.TryAttack();

        // 执行火球攻击逻辑
        TriggerFireballAttack();
    }

    /// <summary>
    /// 触发火球攻击（封装攻击逻辑）
    /// </summary>
    private void TriggerFireballAttack()
    {
        if (fireballPrefab == null || firePoint == null)
        {
            Debug.LogError("火球预制体或发射点未配置", this);
            return;
        }

        // 触发 CharacterAttack 的核心攻击逻辑（停止移动、播放动画等）
        base.TriggerAttack();

        if (player == null)
        {
            Debug.LogError("玩家对象为空，无法计算火球方向", this);
            return;
        }

        // 计算怪物（发射点）到玩家的向量
        Vector2 fireToPlayerDir = player.position - firePoint.position;
        fireToPlayerDir = fireToPlayerDir.normalized;

        //// 获取敌人面朝方向（DirectionTracker 记录的方向）
        //Vector2 faceDir = directionTracker.LastDirection;

        // 生成火球并设置朝向
        GameObject fireball = Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);
        fireball.transform.right = fireToPlayerDir; // 火球朝向 = 敌人面朝方向

      

        Debug.Log("火焰怪发射火球，朝向：" + fireToPlayerDir);
    }
}