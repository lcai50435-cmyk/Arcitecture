using System;
using UnityEngine;

/// <summary>
/// 管理角色相关变量。
/// </summary>
[Serializable]
public class CharacterStats
{
    [Header("生命")]
    public float maxHp = 100f;

    [Header("战斗")]
    public float attackDamage = 20f;

    [Header("速度")]
    public float moveSpeed = 4f;

    [Header("防御")]
    public float defense = 0f;

    public CharacterStats Clone()
    {
        return new CharacterStats
        {
            maxHp = maxHp,
            attackDamage = attackDamage,
            moveSpeed = moveSpeed,
            defense = defense
        };
    }
}
