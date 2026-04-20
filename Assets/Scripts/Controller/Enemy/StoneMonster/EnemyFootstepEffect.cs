using UnityEngine;

public class EnemyFootstepEffect : MonoBehaviour
{
    [Header("踩地裂纹预设")]
    public GameObject crackEffectPrefab;

    [Header("裂纹显示时长（秒）")]
    public float effectDuration = 0.5f;

    [Header("敌人位置")]
    public Transform enemyTransform;

    [Header("裂纹生成间隔（秒）")]
    public float spawnInterval = 2f; // 每2秒生成一次

    // 冷却时间记录
    private float lastSpawnTime = -2f; // 一开始就能生成

    /// <summary>
    /// 裂纹生成脚本
    /// </summary>
    public void SpawnFootstepCrack()
    {
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
            crackRenderer.sortingOrder = 0;
        }

        // 自动销毁
        Destroy(crack, effectDuration);
    }
}