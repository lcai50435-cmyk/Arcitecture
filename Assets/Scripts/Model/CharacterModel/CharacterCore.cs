using System;
using UnityEngine;

/// <summary>
/// 角色管理脚本。
/// 负责维护角色基础属性、当前属性与血量。
/// </summary>
public class CharacterCore : MonoBehaviour
{
    public CharacterStats stats;
    public CharacterStats baseStats;

    [Header("角色目前血量")]
    public float currentHp;

    public event Action OnTakeDamage;
    public event Action<float> OnTakeDamageWithValue;
    public event Action OnDeath;

    [Header("朝向配置")]
    public Vector2 lastFacingDirection = Vector2.down;

    public float LastDamageTaken { get; private set; }
    public bool IsDead { get; private set; }

    private void Awake()
    {
        if (stats == null)
        {
            stats = new CharacterStats();
        }

        if (baseStats == null || IsZeroStats(baseStats))
        {
            baseStats = stats.Clone();
        }
        else
        {
            baseStats = baseStats.Clone();
        }

        stats = baseStats.Clone();
        currentHp = stats.maxHp;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead)
        {
            return;
        }

        float realDamage = Mathf.Max(0f, damage - Mathf.Max(0f, stats.defense));
        LastDamageTaken = realDamage;
        currentHp -= realDamage;

        OnTakeDamage?.Invoke();
        OnTakeDamageWithValue?.Invoke(realDamage);

        if (currentHp <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;
        OnDeath?.Invoke();
    }

    private static bool IsZeroStats(CharacterStats candidate)
    {
        if (candidate == null)
        {
            return true;
        }

        return candidate.maxHp <= 0f &&
               candidate.attackDamage <= 0f &&
               candidate.moveSpeed <= 0f &&
               candidate.defense <= 0f;
    }
}
