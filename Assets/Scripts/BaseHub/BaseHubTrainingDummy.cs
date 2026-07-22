using UnityEngine;

public class BaseHubTrainingDummy : MonoBehaviour
{
    private const float ResetHpDelay = 0.35f;

    private CharacterCore core;
    private SpriteRenderer spriteRenderer;
    private Color baseColor;
    private float flashTimer;
    private float resetTimer;

    private void Awake()
    {
        core = GetComponent<CharacterCore>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
    }

    private void OnEnable()
    {
        if (core == null) return;

        core.OnTakeDamage += HandleTakeDamage;
        core.OnDeath += ScheduleReset;
    }

    private void OnDisable()
    {
        if (core == null) return;

        core.OnTakeDamage -= HandleTakeDamage;
        core.OnDeath -= ScheduleReset;
    }

    private void Update()
    {
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(baseColor, Color.white, flashTimer / 0.18f);
            }
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.color = baseColor;
        }

        if (resetTimer > 0f)
        {
            resetTimer -= Time.deltaTime;
            if (resetTimer <= 0f)
            {
                ResetHp();
            }
        }
    }

    private void HandleTakeDamage()
    {
        flashTimer = 0.18f;
    }

    private void ScheduleReset()
    {
        resetTimer = ResetHpDelay;
    }

    private void ResetHp()
    {
        if (core == null || core.stats == null) return;

        core.currentHp = core.stats.maxHp;
    }
}
