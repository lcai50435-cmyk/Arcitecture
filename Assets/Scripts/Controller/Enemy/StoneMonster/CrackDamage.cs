using UnityEngine;

public class CrackDamage : MonoBehaviour
{
    private float damage = 20f; // 攻击伤害

    private EnemyStatsManager sourceStatsManager;

    public void BindSource(EnemyStatsManager statsManager)
    {
        sourceStatsManager = statsManager;
    }

    // 因为碰撞矩阵已经过滤，这里进来的一定是玩家
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryApplyDamage(other);
    }

    private void TryApplyDamage(Collider2D other)
    {
        if (other == null || !other.CompareTag("Player") || !CanDealDamage())
        {
            return;
        }

        CharacterCore player = other.GetComponent<CharacterCore>();
        if (player != null)
        {
            player.TakeDamage(damage);
        }
    }

    private bool CanDealDamage()
    {
        if (sourceStatsManager == null)
        {
            return true;
        }

        if (!sourceStatsManager.HasPlayerInRange || sourceStatsManager.PlayerTarget == null)
        {
            return false;
        }

        return sourceStatsManager.CurrentState == EnemyState.Chase ||
               sourceStatsManager.CurrentState == EnemyState.Attack;
    }
}
