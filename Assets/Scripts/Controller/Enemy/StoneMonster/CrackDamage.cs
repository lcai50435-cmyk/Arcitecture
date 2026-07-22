using UnityEngine;

public class CrackDamage : MonoBehaviour
{
    private float damage = 20f; // Attack damage

    private EnemyStatsManager sourceStatsManager;

    public void BindSource(EnemyStatsManager statsManager)
    {
        sourceStatsManager = statsManager;
    }

    // The collision matrix already filters this, so anything entering here must be the player
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
