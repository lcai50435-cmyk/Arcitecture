using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 伤害管理系统
/// </summary>
public class DamageManager : MonoBehaviour
{
    private void Awake()
    {
        CharacterAttack.OnAttackHit += ResolveDamage;
    }

    void ResolveDamage(GameObject attacker, GameObject target, float damage)
    {
        CharacterCore targetCore = target.GetComponent<CharacterCore>();
        if (targetCore != null)
        {
            targetCore.TakeDamage(damage);
        }
    }
}
