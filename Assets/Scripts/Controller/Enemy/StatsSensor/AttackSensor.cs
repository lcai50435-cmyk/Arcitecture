using UnityEngine;

public class AttackSensor : MonoBehaviour
{
    EnemyStateManager state;
    EnemyAttack attack;

    void Awake()
    {
        state = GetComponentInParent<EnemyStateManager>();
        attack = GetComponentInParent<EnemyAttack>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            state.SetAttack(true);
            attack.SetTarget(other.transform);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            state.SetAttack(false);
            attack.ClearTarget();
        }
    }
}