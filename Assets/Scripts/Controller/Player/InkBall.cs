using System.Collections.Generic;
using UnityEngine;

public class InkBall : MonoBehaviour
{
    private static Sprite runtimeSprite;

    [Header("基础设置")]
    public float speed = 6f;
    public float autoDestroyTime = 10f;
    public float hitDestroyDelay = 0.25f;
    public CharacterCore character;

    private float damage;
    private Animator anim;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool isHit;
    private int maxHitCount = 1;
    private bool explodeOnHit;
    private float explosionRadius = 1.35f;
    private float explosionDamageMultiplier = 1f;
    private InkType inkType = InkType.DirectInk;
    private Color displayColor = Color.white;
    private float impactPulseScale = 0.9f;
    private float impactPulseDuration = 0.16f;
    private InkDebuffRuntimeConfig debuffConfig;
    private readonly HashSet<CharacterCore> hitTargets = new HashSet<CharacterCore>();

    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            character = player.GetComponent<CharacterCore>();
        }

        EnsureSpriteRenderer();
    }

    private void Start()
    {
        ScheduleDestroyTimer();
    }

    public void Init(InkAttackRuntimeConfig config)
    {
        inkType = config.inkType;
        displayColor = config.displayColor;
        maxHitCount = Mathf.Max(1, config.maxHitCount);
        debuffConfig = config.debuff;
        explodeOnHit = config.explodeOnHit;
        explosionRadius = Mathf.Max(0.25f, config.explosionRadius);
        explosionDamageMultiplier = Mathf.Max(0f, config.explosionDamageMultiplier);
        impactPulseScale = Mathf.Max(0.2f, config.impactPulseScale);
        impactPulseDuration = Mathf.Max(0.05f, config.impactPulseDuration);
        speed = Mathf.Max(0.01f, config.baseProjectileSpeed * config.speedMultiplier);
        autoDestroyTime = Mathf.Max(0.05f, config.baseProjectileLifetime * config.lifetimeMultiplier);
        Vector3 nextScale = transform.localScale;
        nextScale.x *= Mathf.Max(0.01f, config.projectileScale * config.projectileStretch.x);
        nextScale.y *= Mathf.Max(0.01f, config.projectileScale * config.projectileStretch.y);
        transform.localScale = nextScale;
        ApplyVisualStyle();
        ScheduleDestroyTimer();
    }

    private void FixedUpdate()
    {
        if (character != null)
        {
            damage = character.stats.attackDamage;
        }

        if (isHit || rb == null)
        {
            return;
        }

        rb.velocity = transform.right * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isHit)
        {
            return;
        }

        CharacterCore enemyCore = other.GetComponent<CharacterCore>();
        if (enemyCore != null && enemyCore != character && character != null)
        {
            if (hitTargets.Contains(enemyCore))
            {
                return;
            }

            ApplyHit(enemyCore, damage, true);
            SpawnImpactPulse(enemyCore.transform.position, Mathf.Min(impactPulseScale, 0.95f));

            if (explodeOnHit)
            {
                ApplyExplosion();
            }

            if (!explodeOnHit && hitTargets.Count < maxHitCount)
            {
                return;
            }
        }

        FinishHit();
    }

    private void ApplyExplosion()
    {
        SpawnImpactPulse(transform.position, impactPulseScale);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            CharacterCore enemyCore = hits[i].GetComponent<CharacterCore>();
            if (enemyCore == null || enemyCore == character || hitTargets.Contains(enemyCore))
            {
                continue;
            }

            ApplyHit(enemyCore, damage * explosionDamageMultiplier, false);
        }
    }

    private void ApplyHit(CharacterCore enemyCore, float hitDamage, bool countHit)
    {
        if (enemyCore == null)
        {
            return;
        }

        if (countHit)
        {
            hitTargets.Add(enemyCore);
        }

        enemyCore.TakeDamage(hitDamage);
        ApplyDebuff(enemyCore, hitDamage);
    }

    private void ApplyDebuff(CharacterCore enemyCore, float baseDamage)
    {
        if (!debuffConfig.HasSlow && !debuffConfig.HasKnockback && !debuffConfig.HasDamageOverTime)
        {
            return;
        }

        InkDebuffReceiver receiver = enemyCore.GetComponent<InkDebuffReceiver>();
        if (receiver == null)
        {
            receiver = enemyCore.gameObject.AddComponent<InkDebuffReceiver>();
        }

        receiver.Apply(debuffConfig, transform.right, baseDamage);
    }

    private void FinishHit()
    {
        isHit = true;

        if (!explodeOnHit)
        {
            SpawnImpactPulse(transform.position, impactPulseScale * 0.75f);
        }

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        if (anim != null)
        {
            anim.SetTrigger("IsHit");
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        Destroy(gameObject, hitDestroyDelay);
    }

    public void DestroyAfterHit()
    {
        Destroy(gameObject);
    }

    private void ScheduleDestroyTimer()
    {
        CancelInvoke(nameof(DestroyAfterTime));
        Invoke(nameof(DestroyAfterTime), autoDestroyTime);
    }

    private void DestroyAfterTime()
    {
        Destroy(gameObject);
    }

    private void EnsureSpriteRenderer()
    {
        if (spriteRenderer != null)
        {
            return;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = GetRuntimeSprite();
        }

        spriteRenderer.sortingOrder = Mathf.Max(spriteRenderer.sortingOrder, 8);
    }

    private void ApplyVisualStyle()
    {
        EnsureSpriteRenderer();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = displayColor;
        }
    }

    private void SpawnImpactPulse(Vector3 position, float pulseScale)
    {
        GameObject pulseObject = new GameObject("InkImpactPulse");
        pulseObject.transform.position = position;
        pulseObject.transform.localScale = Vector3.one * Mathf.Max(0.2f, pulseScale);

        SpriteRenderer renderer = pulseObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetRuntimeSprite();
        renderer.color = displayColor;
        renderer.sortingOrder = 12;

        InkImpactPulse pulse = pulseObject.AddComponent<InkImpactPulse>();
        pulse.Initialize(displayColor, impactPulseDuration);
    }

    private static Sprite GetRuntimeSprite()
    {
        if (runtimeSprite != null)
        {
            return runtimeSprite;
        }

        Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Vector2 center = new Vector2(3.5f, 3.5f);

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= 3.3f ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        runtimeSprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
        return runtimeSprite;
    }
}

public class InkImpactPulse : MonoBehaviour
{
    private Color pulseColor = Color.white;
    private float duration = 0.16f;
    private float remaining;
    private Vector3 initialScale;
    private SpriteRenderer spriteRenderer;

    public void Initialize(Color color, float pulseDuration)
    {
        pulseColor = color;
        duration = Mathf.Max(0.05f, pulseDuration);
        remaining = duration;
        initialScale = transform.localScale;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (spriteRenderer == null)
        {
            Destroy(gameObject);
            return;
        }

        remaining -= Time.deltaTime;
        float progress = 1f - Mathf.Clamp01(remaining / duration);
        transform.localScale = initialScale * (1f + progress * 0.8f);

        Color color = pulseColor;
        color.a = 0.55f * (1f - progress);
        spriteRenderer.color = color;

        if (remaining <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
