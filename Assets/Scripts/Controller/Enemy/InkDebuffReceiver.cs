using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class InkDebuffReceiver : MonoBehaviour
{
    private EnemyMove enemyMove;
    private Rigidbody2D rb;
    private CharacterCore characterCore;
    private Coroutine slowRoutine;
    private Coroutine dotRoutine;

    private void Awake()
    {
        enemyMove = GetComponent<EnemyMove>();
        rb = GetComponent<Rigidbody2D>();
        characterCore = GetComponent<CharacterCore>();
    }

    public void Apply(InkDebuffRuntimeConfig config, Vector2 hitDirection, float baseDamage)
    {
        if (config.HasSlow)
        {
            ApplySlow(config.slowRatio, config.slowDuration);
        }

        if (config.HasKnockback)
        {
            ApplyKnockback(hitDirection, config.knockbackForce);
        }

        if (config.HasDamageOverTime)
        {
            ApplyDamageOverTime(config, baseDamage);
        }
    }

    private void ApplySlow(float slowRatio, float duration)
    {
        if (enemyMove == null)
        {
            return;
        }

        if (slowRoutine != null)
        {
            StopCoroutine(slowRoutine);
        }

        slowRoutine = StartCoroutine(SlowRoutine(slowRatio, duration));
    }

    private IEnumerator SlowRoutine(float slowRatio, float duration)
    {
        float speedMultiplier = Mathf.Clamp(1f - slowRatio, 0.05f, 1f);
        enemyMove.SetExternalSpeedMultiplier(speedMultiplier);

        yield return new WaitForSeconds(duration);

        enemyMove.SetExternalSpeedMultiplier(1f);
        slowRoutine = null;
    }

    private void ApplyDamageOverTime(InkDebuffRuntimeConfig config, float baseDamage)
    {
        if (characterCore == null)
        {
            return;
        }

        if (dotRoutine != null)
        {
            StopCoroutine(dotRoutine);
        }

        float tickDamage = Mathf.Max(0.5f, baseDamage * config.dotDamageMultiplier);
        dotRoutine = StartCoroutine(DamageOverTimeRoutine(config.dotDuration, config.dotTickInterval, tickDamage));
    }

    private IEnumerator DamageOverTimeRoutine(float duration, float tickInterval, float tickDamage)
    {
        float remaining = duration;
        float interval = Mathf.Max(0.1f, tickInterval);

        while (remaining > 0f)
        {
            yield return new WaitForSeconds(interval);

            if (characterCore == null)
            {
                yield break;
            }

            characterCore.TakeDamage(tickDamage);
            remaining -= interval;
        }

        dotRoutine = null;
    }

    private void ApplyKnockback(Vector2 hitDirection, float force)
    {
        if (rb == null)
        {
            return;
        }

        Vector2 direction = hitDirection.sqrMagnitude > 0.0001f ? hitDirection.normalized : Vector2.right;

        if (rb.bodyType == RigidbodyType2D.Dynamic)
        {
            rb.AddForce(direction * force, ForceMode2D.Impulse);
        }
        else
        {
            rb.MovePosition(rb.position + direction * force);
        }
    }

    private void OnDisable()
    {
        if (enemyMove != null)
        {
            enemyMove.SetExternalSpeedMultiplier(1f);
        }

        slowRoutine = null;
        dotRoutine = null;
    }
}
