using UnityEngine;

public class EnemyFootstepEffect : MonoBehaviour
{
    private const int RuntimeCrackSortingOrder = 3;

    [Header("踩地裂纹预设")]
    public GameObject crackEffectPrefab;

    [Header("裂纹显示时长（秒）")]
    public float effectDuration = 0.5f;

    [Header("敌人位置")]
    public Transform enemyTransform;

    [Header("敌人状态")]
    public EnemyStatsManager statsManager;

    [Header("裂纹生成间隔（秒）")]
    public float spawnInterval = 2f; // Spawn once every 2 seconds

    // Cooldown time record
    private float lastSpawnTime = -2f; // Can spawn immediately at the start

    private void Reset()
    {
        ResolveDependencies();
    }

    private void Awake()
    {
        ResolveDependencies();
    }

    private void OnValidate()
    {
        ResolveDependencies();
    }

    /// <summary>
    /// Crack spawning script
    /// </summary>
    public void SpawnFootstepCrack()
    {
        if (!CanSpawnFootstepCrack())
            return;

        // Skip spawning if less than 2 seconds have passed since the last spawn
        if (Time.time < lastSpawnTime + spawnInterval)
            return;

        if (crackEffectPrefab == null || enemyTransform == null)
            return;

        // Update the last spawn time
        lastSpawnTime = Time.time;

        // Spawn crack
        GameObject crack = CombatObjectPool.RentPrefab(
            crackEffectPrefab,
            enemyTransform.position,
            Quaternion.identity
        );
        if (crack == null)
        {
            return;
        }

        SpriteRenderer crackRenderer = crack.GetComponent<SpriteRenderer>();
        if (crackRenderer != null)
        {
            crackRenderer.sortingOrder = RuntimeCrackSortingOrder;
        }

        CrackDamage crackDamage = crack.GetComponent<CrackDamage>();
        if (crackDamage != null)
        {
            crackDamage.BindSource(statsManager);
        }

        CombatObjectPool.ReleaseOrDestroy(crack, effectDuration);
    }

    private bool CanSpawnFootstepCrack()
    {
        ResolveDependencies();
        if (statsManager == null || !statsManager.HasPlayerInRange || statsManager.PlayerTarget == null)
        {
            return false;
        }

        return statsManager.CurrentState == EnemyState.Chase ||
               statsManager.CurrentState == EnemyState.Attack;
    }

    private void ResolveDependencies()
    {
        if (enemyTransform == null)
        {
            enemyTransform = transform;
        }

        if (statsManager == null)
        {
            statsManager = GetComponent<EnemyStatsManager>();
        }
    }
}
