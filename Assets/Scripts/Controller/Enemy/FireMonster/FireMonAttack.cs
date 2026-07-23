using UnityEngine;

public class FireMonAttack : EnemyAttack
{
    private DirectionTracker directionTracker;
    private Animator animator;

    [Header("火球攻击设置")]
    public GameObject fireballPrefab; // Fireball prefab
    public Transform firePoint;       // Fireball spawn point

    protected override void Awake()
    {
        // Initialize this component's dependencies
        directionTracker = GetComponent<DirectionTracker>();
        animator = GetComponent<Animator>();

        // Run the base class Awake logic
        base.Awake();

        // Add DirectionTracker automatically if it is missing
        if (directionTracker == null)
        {
            directionTracker = gameObject.AddComponent<DirectionTracker>();
            Debug.LogWarning("自动为 FireMon 添加了 DirectionTracker 组件", this);
        }
    }

    // Override EnemyAttack.TryAttack
    protected override void TryAttack()
    {
        // Attack cooldown
        if (attackInterval > 0f && Time.time - lastAttackTime < attackInterval)
        {
            return; // Cooldown is not ready
        }

        // Run the base class attack cooldown check first
        base.TryAttack();

        // Run the fireball attack logic
        TriggerFireballAttack();
    }

    /// <summary>
    /// Trigger the fireball attack (encapsulated attack logic)
    /// </summary>
    private void TriggerFireballAttack()
    {
        if (fireballPrefab == null || firePoint == null)
        {
            Debug.LogError("火球预制体或发射点未配置", this);
            return;
        }

        // Trigger CharacterAttack core attack logic, such as stopping movement and playing animation
        base.TriggerAttack();
        MusicManager.PlaySfx(SfxCueId.FireMonsterCast);

        if (player == null)
        {
            Debug.LogError("玩家对象为空，无法计算火球方向", this);
            return;
        }

        // Calculate the vector from the monster (spawn point) to the player
        Vector2 fireToPlayerDir = player.position - firePoint.position;
        fireToPlayerDir = fireToPlayerDir.normalized;

        //// Get the enemy facing direction recorded by DirectionTracker
        //Vector2 faceDir = directionTracker.LastDirection;

        // Spawn the fireball and set its facing direction
        GameObject fireball = CombatObjectPool.RentPrefab(fireballPrefab, firePoint.position, Quaternion.identity);
        if (fireball == null)
        {
            return;
        }

        fireball.transform.right = fireToPlayerDir; // Fireball facing = enemy facing direction

        FireBall fireBallComponent;
        if (fireball.TryGetComponent(out fireBallComponent))
        {
            fireBallComponent.Initialize();
        }

      

        Debug.Log("火焰怪发射火球，朝向：" + fireToPlayerDir);
    }
}
