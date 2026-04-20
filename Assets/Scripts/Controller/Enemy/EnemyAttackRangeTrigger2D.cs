using UnityEngine;
using System;

/// <summary>
/// 敌人攻击范围触发器
/// </summary>
[RequireComponent(typeof(Collider2D))] 
public class EnemyAttackRangeTrigger2D : MonoBehaviour
{
    // 玩家进入攻击范围事件
    public event Action OnPlayerEnterRange;
    // 玩家离开攻击范围事件
    public event Action OnPlayerExitRange;

    // 标签检测
    [SerializeField] private string playerTag = "Player";

    private void Awake()
    {
        // 确保触发器是开启状态且非碰撞体
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            Debug.Log("玩家进入攻击范围");
            OnPlayerEnterRange?.Invoke();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            OnPlayerExitRange?.Invoke();
        }
    }
}