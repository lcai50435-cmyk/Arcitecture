using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class InkDebuffReceiver : MonoBehaviour
{
    private static Sprite runtimeSprite;

    private EnemyMove enemyMove;
    private Rigidbody2D rb;
    private CharacterCore characterCore;
    private Coroutine slowRoutine;
    private Coroutine dotRoutine;
    private Transform dotMarker;
    private SpriteRenderer dotMarkerRenderer;
    private float dotMarkerRemaining;

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
        ShowDotMarker(config.dotDuration);
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
        HideDotMarker();
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
        HideDotMarker();
    }

    private void Update()
    {
        if (dotMarker == null || dotMarkerRenderer == null)
        {
            return;
        }

        float alpha = 0.45f + Mathf.PingPong(Time.time * 1.8f, 0.35f);
        dotMarkerRenderer.color = new Color(0.24f, 0.78f, 0.56f, alpha);

        if (dotMarkerRemaining > 0f)
        {
            dotMarkerRemaining -= Time.deltaTime;
            if (dotMarkerRemaining <= 0f)
            {
                HideDotMarker();
            }
        }
    }

    private void ShowDotMarker(float duration)
    {
        dotMarkerRemaining = Mathf.Max(dotMarkerRemaining, duration);

        if (dotMarker == null)
        {
            dotMarker = new GameObject("FlowInkMarker").transform;
            dotMarker.SetParent(transform, false);
            dotMarker.localPosition = new Vector3(0f, 0.95f, 0f);
            dotMarker.localScale = new Vector3(0.5f, 0.5f, 1f);

            dotMarkerRenderer = dotMarker.gameObject.AddComponent<SpriteRenderer>();
            dotMarkerRenderer.sprite = GetRuntimeSprite();
            dotMarkerRenderer.sortingOrder = 28;
        }

        if (dotMarkerRenderer != null)
        {
            dotMarkerRenderer.enabled = true;
        }
    }

    private void HideDotMarker()
    {
        dotMarkerRemaining = 0f;
        if (dotMarkerRenderer != null)
        {
            dotMarkerRenderer.enabled = false;
        }
    }

    private static Sprite GetRuntimeSprite()
    {
        if (runtimeSprite != null)
        {
            return runtimeSprite;
        }

        Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                texture.SetPixel(x, y, Color.white);
            }
        }

        texture.Apply();
        runtimeSprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        return runtimeSprite;
    }
}
