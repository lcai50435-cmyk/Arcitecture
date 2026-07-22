using System;
using UnityEngine;

/// <summary>
/// Manages character-related variables.
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
