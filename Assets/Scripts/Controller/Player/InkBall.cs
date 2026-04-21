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
    private bool enableTrailAfterImage;
    private float trailSpawnInterval = 0.05f;
    private float trailLifetime = 0.18f;
    private float trailScaleMultiplier = 0.82f;
    private float trailAlpha = 0.28f;
    private bool enableHeavyShockwave;
    private float heavyShockwaveScale = 1.45f;
    private float heavyShockwaveDurationMultiplier = 1.25f;
    private bool enableSlowResidue;
    private float slowResidueScale = 1.1f;
    private float slowResidueDuration = 0.55f;
    private float nextTrailSpawnTime;
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
        enableTrailAfterImage = config.enableTrailAfterImage;
        trailSpawnInterval = Mathf.Max(0.016f, config.trailSpawnInterval);
        trailLifetime = Mathf.Max(0.05f, config.trailLifetime);
        trailScaleMultiplier = Mathf.Max(0.2f, config.trailScaleMultiplier);
        trailAlpha = Mathf.Clamp01(config.trailAlpha);
        enableHeavyShockwave = config.enableHeavyShockwave;
        heavyShockwaveScale = Mathf.Max(1f, config.heavyShockwaveScale);
        heavyShockwaveDurationMultiplier = Mathf.Max(1f, config.heavyShockwaveDurationMultiplier);
        enableSlowResidue = config.enableSlowResidue;
        slowResidueScale = Mathf.Max(0.2f, config.slowResidueScale);
        slowResidueDuration = Mathf.Max(0.05f, config.slowResidueDuration);
        nextTrailSpawnTime = Time.time;
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

        TrySpawnTrailAfterImage();
        rb.velocity = transform.right * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isHit)
        {
            return;
        }

        bool hitEnemy = false;
        CharacterCore enemyCore = other.GetComponent<CharacterCore>();
        if (enemyCore != null && enemyCore != character && character != null)
        {
            if (hitTargets.Contains(enemyCore))
            {
                return;
            }

            ApplyHit(enemyCore, damage, true);
            SpawnImpactPulse(enemyCore.transform.position, Mathf.Min(impactPulseScale, 0.95f));
            SpawnModifierHitEffects(enemyCore.transform.position);
            hitEnemy = true;

            if (explodeOnHit)
            {
                ApplyExplosion();
            }

            if (!explodeOnHit && hitTargets.Count < maxHitCount)
            {
                return;
            }
        }

        FinishHit(hitEnemy);
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

    private void FinishHit(bool hitEnemy)
    {
        isHit = true;

        if (!explodeOnHit)
        {
            SpawnImpactPulse(transform.position, impactPulseScale * 0.75f);
        }

        if (!hitEnemy)
        {
            SpawnModifierHitEffects(transform.position);
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
        SpawnImpactPulse(position, pulseScale, displayColor, impactPulseDuration, 0.8f);
    }

    private void SpawnImpactPulse(Vector3 position, float pulseScale, Color pulseColor, float pulseDuration, float expansionMultiplier)
    {
        GameObject pulseObject = new GameObject("InkImpactPulse");
        pulseObject.transform.position = position;
        pulseObject.transform.localScale = Vector3.one * Mathf.Max(0.2f, pulseScale);

        SpriteRenderer renderer = pulseObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetRuntimeSprite();
        renderer.color = pulseColor;
        renderer.sortingOrder = 12;

        InkImpactPulse pulse = pulseObject.AddComponent<InkImpactPulse>();
        pulse.Initialize(pulseColor, pulseDuration, expansionMultiplier);
    }

    private void SpawnModifierHitEffects(Vector3 position)
    {
        if (enableHeavyShockwave)
        {
            Color shockwaveColor = Color.Lerp(displayColor, new Color(0.98f, 0.82f, 0.45f, 1f), 0.4f);
            SpawnImpactPulse(
                position,
                impactPulseScale * heavyShockwaveScale,
                shockwaveColor,
                impactPulseDuration * heavyShockwaveDurationMultiplier,
                1.18f);
        }

        if (enableSlowResidue)
        {
            SpawnSlowResidue(position);
        }
    }

    private void TrySpawnTrailAfterImage()
    {
        if (!enableTrailAfterImage || spriteRenderer == null || Time.time < nextTrailSpawnTime)
        {
            return;
        }

        nextTrailSpawnTime = Time.time + trailSpawnInterval;

        GameObject trailObject = new GameObject("InkTrailAfterImage");
        trailObject.transform.position = transform.position - transform.right * 0.08f;
        trailObject.transform.rotation = transform.rotation;
        trailObject.transform.localScale = transform.localScale * trailScaleMultiplier;

        SpriteRenderer renderer = trailObject.AddComponent<SpriteRenderer>();
        renderer.sprite = spriteRenderer.sprite != null ? spriteRenderer.sprite : GetRuntimeSprite();
        Color trailColor = Color.Lerp(displayColor, Color.white, 0.12f);
        trailColor.a = trailAlpha;
        renderer.color = trailColor;
        renderer.sortingOrder = Mathf.Max(1, spriteRenderer.sortingOrder - 1);

        InkTransientSprite transientSprite = trailObject.AddComponent<InkTransientSprite>();
        transientSprite.Initialize(trailColor, trailLifetime, 1.03f, 0.75f);
    }

    private void SpawnSlowResidue(Vector3 position)
    {
        GameObject residueObject = new GameObject("InkSlowResidue");
        residueObject.transform.position = position;
        residueObject.transform.localScale = new Vector3(slowResidueScale, slowResidueScale * 0.62f, 1f);

        SpriteRenderer renderer = residueObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetRuntimeSprite();
        Color residueColor = Color.Lerp(displayColor, new Color(0.56f, 0.74f, 0.42f, 1f), 0.45f);
        residueColor.a = 0.26f;
        renderer.color = residueColor;
        renderer.sortingOrder = 6;

        InkTransientSprite transientSprite = residueObject.AddComponent<InkTransientSprite>();
        transientSprite.Initialize(residueColor, slowResidueDuration, 1.08f, 0.88f);
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
    private float expansionMultiplier = 0.8f;

    public void Initialize(Color color, float pulseDuration, float pulseExpansionMultiplier)
    {
        pulseColor = color;
        duration = Mathf.Max(0.05f, pulseDuration);
        remaining = duration;
        initialScale = transform.localScale;
        spriteRenderer = GetComponent<SpriteRenderer>();
        expansionMultiplier = Mathf.Max(0.2f, pulseExpansionMultiplier);
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
        transform.localScale = initialScale * (1f + progress * expansionMultiplier);

        Color color = pulseColor;
        color.a = 0.55f * (1f - progress);
        spriteRenderer.color = color;

        if (remaining <= 0f)
        {
            Destroy(gameObject);
        }
    }
}

public class InkTransientSprite : MonoBehaviour
{
    private Color spriteColor = Color.white;
    private float duration = 0.18f;
    private float remaining;
    private Vector3 initialScale;
    private float growMultiplier = 1f;
    private float endScaleMultiplier = 0.8f;
    private SpriteRenderer spriteRenderer;

    public void Initialize(Color color, float spriteDuration, float scaleUpMultiplier, float scaleDownMultiplier)
    {
        spriteColor = color;
        duration = Mathf.Max(0.05f, spriteDuration);
        remaining = duration;
        initialScale = transform.localScale;
        growMultiplier = Mathf.Max(0f, scaleUpMultiplier);
        endScaleMultiplier = Mathf.Max(0.01f, scaleDownMultiplier);
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
        float scaleMultiplier = Mathf.Lerp(growMultiplier, endScaleMultiplier, progress);
        transform.localScale = initialScale * scaleMultiplier;

        Color color = spriteColor;
        color.a = spriteColor.a * (1f - progress);
        spriteRenderer.color = color;

        if (remaining <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
