using UnityEngine;

public class CharacterDeathBase : MonoBehaviour
{
    protected Collider2D characterCollider;
    protected Rigidbody2D characterRigidbody;
    protected Animator anim;
    protected CharacterCore core;
    private bool deathTriggered;

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
        // 关闭碰撞
        if (characterCollider != null)
            characterCollider.enabled = false;

        if (characterRigidbody != null)
        {
            characterRigidbody.velocity = Vector2.zero;
            characterRigidbody.angularVelocity = 0f;
            characterRigidbody.bodyType = RigidbodyType2D.Static;
        }
    }

    protected virtual void OnCharacterDie()
    {
        // 交由子类播放死亡表现
    }

    protected virtual float GetDeathFallbackDelay()
    {
        return 1.5f;
    }

    protected void CompleteDeathDestroy()
    {
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
