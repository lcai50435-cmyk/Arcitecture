using System;
using UnityEngine;

public class CharacterDeathBase : MonoBehaviour
{
    protected Collider2D characterCollider;
    protected Rigidbody2D characterRigidbody;
    protected Animator anim;
    protected CharacterCore core;
    private bool deathTriggered;
    private bool deathCompleted;

    public event Action OnDeathSequenceCompleted;

    protected virtual void Awake()
    {
        characterCollider = GetComponent<Collider2D>();
        characterRigidbody = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        core = GetComponent<CharacterCore>();
    }

    public void TriggerCharacterDie()
    {
        if (deathTriggered)
        {
            return;
        }

        deathTriggered = true;
        DisableAliveOnlyComponents();
        DisablePhysicsComponents();
        OnCharacterDie();
        StartCoroutine(DestroyAfterDelayRoutine());
    }

    protected virtual void OnEnable()
    {
        if (core != null)
            core.OnDeath += TriggerCharacterDie;
    }

    protected virtual void OnDisable()
    {
        if (core != null)
            core.OnDeath -= TriggerCharacterDie;
    }

    protected virtual void DisablePhysicsComponents()
    {
        // Disable collision
        if (characterCollider != null)
            characterCollider.enabled = false;

        if (characterRigidbody != null)
        {
            characterRigidbody.velocity = Vector2.zero;
            characterRigidbody.angularVelocity = 0f;
            characterRigidbody.bodyType = RigidbodyType2D.Static;
        }
    }

    protected virtual void DisableAliveOnlyComponents()
    {
        CharacterAttack attackBehaviour = GetComponent<CharacterAttack>();
        if (attackBehaviour != null)
        {
            attackBehaviour.HandleOwnerDeathImmediate();
        }

        EnemyChase enemyChase = GetComponent<EnemyChase>();
        if (enemyChase != null)
        {
            enemyChase.enabled = false;
        }

        EnemyMove enemyMove = GetComponent<EnemyMove>();
        if (enemyMove != null)
        {
            enemyMove.StopMovement();
            enemyMove.enabled = false;
        }

        EnemyStatsManager enemyStats = GetComponent<EnemyStatsManager>();
        if (enemyStats != null)
        {
            enemyStats.enabled = false;
        }
    }

    protected virtual void OnCharacterDie()
    {
        // Let subclasses play the death presentation
    }

    protected virtual float GetDeathFallbackDelay()
    {
        return 1.5f;
    }

    protected void CompleteDeathDestroy()
    {
        if (deathCompleted)
        {
            return;
        }

        deathCompleted = true;
        OnDeathSequenceCompleted?.Invoke();
        Destroy(gameObject);
    }

    private System.Collections.IEnumerator DestroyAfterDelayRoutine()
    {
        yield return new WaitForSecondsRealtime(GetDeathFallbackDelay());
        if (this != null)
        {
            CompleteDeathDestroy();
        }
    }
}
