using System.Collections.Generic;
using UnityEngine;

public class InkBall : MonoBehaviour
{
    [Header("基础设置")]
    public float speed = 6f;
    public float autoDestroyTime = 10f;
    public float hitDestroyDelay = 0.25f;
    public CharacterCore character;

    private float damage;
    private Animator anim;
    private Rigidbody2D rb;
    private bool isHit;
    private int maxHitCount = 1;
    private bool explodeOnHit;
    private float explosionRadius = 1.35f;
    private float explosionDamageMultiplier = 1f;
    private InkDebuffRuntimeConfig debuffConfig;
    private readonly HashSet<CharacterCore> hitTargets = new HashSet<CharacterCore>();

    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            character = player.GetComponent<CharacterCore>();
        }
    }

    private void Start()
    {
        ScheduleDestroyTimer();
    }

    public void Init(InkAttackRuntimeConfig config)
    {
        maxHitCount = Mathf.Max(1, config.maxHitCount);
        debuffConfig = config.debuff;
        explodeOnHit = config.explodeOnHit;
        explosionRadius = Mathf.Max(0.25f, config.explosionRadius);
        explosionDamageMultiplier = Mathf.Max(0f, config.explosionDamageMultiplier);
        speed = Mathf.Max(0.01f, config.baseProjectileSpeed * config.speedMultiplier);
        autoDestroyTime = Mathf.Max(0.05f, config.baseProjectileLifetime * config.lifetimeMultiplier);
        transform.localScale *= Mathf.Max(0.01f, config.projectileScale);
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
}
