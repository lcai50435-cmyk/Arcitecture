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
    public float spawnInterval = 2f; // 每2秒生成一次

    // 冷却时间记录
    private float lastSpawnTime = -2f; // 一开始就能生成

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
    /// 裂纹生成脚本
    /// </summary>
    public void SpawnFootstepCrack()
    {
        if (!CanSpawnFootstepCrack())
            return;

        // 距离上次生成不足2秒，直接跳过，不生成
        if (Time.time < lastSpawnTime + spawnInterval)
            return;

        if (crackEffectPrefab == null || enemyTransform == null)
            return;

        // 更新最后生成时间
        lastSpawnTime = Time.time;

        // 生成裂纹
        GameObject crack = Instantiate(
            crackEffectPrefab,
            enemyTransform.position,
            Quaternion.identity
        );

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

        // 自动销毁
        Destroy(crack, effectDuration);
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
