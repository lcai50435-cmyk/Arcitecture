using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理角色相关变量
/// </summary>
[System.Serializable]
public class CharacterStats
{
    [Header("生命")]
    public float maxHp = 100f;

    [Header("战斗")]
    public float attackDamage = 10f;

    [Header("速度")]
    public float moveSpeed = 4f;

    [Header("防御")]
    public float defense = 0f;
}