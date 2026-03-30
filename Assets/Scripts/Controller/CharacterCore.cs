using UnityEngine;

/// <summary>
/// 角色管理脚本
/// 负责调整角色血量
/// 每个角色都要挂载
/// </summary>
public class CharacterCore : MonoBehaviour
{
    public CharacterStats stats; // 角色相关变量

    [HideInInspector] public float currentHp; // 角色目前血量

    private void Awake()
    {
        currentHp = stats.maxHp; // 满血状态
    }

    /// <summary>
    /// 伤害扣血
    /// </summary>
    /// <param name="damage"></param>
    public void TakeDamage(float damage)
    {
        float realDmg = Mathf.Max(0, damage); // 防止血量未负数
        currentHp -= realDmg;

        if (currentHp <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }
}