using UnityEngine;
using System;

/// <summary>
/// Enemy attack range trigger
/// </summary>
[RequireComponent(typeof(Collider2D))] 
public class EnemyAttackRangeTrigger2D : MonoBehaviour
{
    // Player entered attack range event
    public event Action OnPlayerEnterRange;
    // Player exited attack range event
    public event Action OnPlayerExitRange;

    // Tag check
    [SerializeField] private string playerTag = "Player";

    private void Awake()
    {
        // Ensure the trigger is enabled and non-solid
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