using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class InkDebuffReceiver : MonoBehaviour
{
    private EnemyMove enemyMove;
    private Rigidbody2D rb;
    private Coroutine slowRoutine;

    private void Awake()
    {
        enemyMove = GetComponent<EnemyMove>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void Apply(InkDebuffRuntimeConfig config, Vector2 hitDirection)
    {
        if (config.HasSlow)
        {
            ApplySlow(config.slowRatio, config.slowDuration);
        }

        if (config.HasKnockback)
        {
            ApplyKnockback(hitDirection, config.knockbackForce);
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
    }
}
