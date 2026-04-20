using System;
using UnityEngine;

/// <summary>
/// 角色管理脚本
/// 负责调整角色血量
/// 每个角色都要挂载
/// </summary>
public class CharacterCore : MonoBehaviour
{
    public CharacterStats stats; // 角色相关变量
    public CharacterStats baseStats; // 记录角色相关变量

    [Header("角色目前血量")]
    public float currentHp; // 角色目前血量

    public event Action OnTakeDamage; // 受击事件
    public event Action OnDeath;  // 死亡事件

    [Header("朝向配置")]
    public Vector2 lastFacingDirection = Vector2.down; // 默认朝下

    private void Awake()
    {
        currentHp = stats.maxHp; // 满血状态
        baseStats = stats; // // 保存原始基础属性
    }

    /// <summary>
    /// 伤害扣血
    /// </summary>
    /// <param name="damage">伤害</param>
    public void TakeDamage(float damage)
    {
        float realDmg = Mathf.Max(0, damage); // 防止血量未负数
        currentHp -= Mathf.Max(0, realDmg - stats.defense); // 受到伤害

        OnTakeDamage?.Invoke(); // 播放相关被攻击逻辑

        if (currentHp <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // 触发相关死亡逻辑
        OnDeath?.Invoke();
    }
}