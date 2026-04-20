using System.Collections.Generic;
using UnityEngine;

public class InkBall : MonoBehaviour
{
    [Header("基础设置")]
    public float speed = 6f;
    public float autoDestroyTime = 10f; // 未命中10秒自动销毁
    public float hitDestroyDelay = 3f;  // 命中后兜底销毁延迟
    public CharacterCore character;

    private float damage;
    private Animator anim;
    private Rigidbody2D rb;
    private bool isHit = false;
    private int maxHitCount = 1;
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

        // 判空校验，避免空引用
        if (anim == null) Debug.LogError($"[{gameObject.name}] 缺少Animator组件！");
        if (rb == null) Debug.LogError($"[{gameObject.name}] 缺少Rigidbody2D组件！");
        if (character == null) Debug.LogError($"[{gameObject.name}] 未绑定玩家CharacterCore！");
    }

    private void Start()
    {
        // 未命中10秒自动销毁
        Destroy(gameObject, autoDestroyTime);
    }

    public void Init(InkAttackRuntimeConfig config)
    {
        maxHitCount = Mathf.Max(1, config.maxHitCount);
        debuffConfig = config.debuff;
        speed *= Mathf.Max(0.01f, config.speedMultiplier);
        autoDestroyTime *= Mathf.Max(0.01f, config.lifetimeMultiplier);
        transform.localScale *= Mathf.Max(0.01f, config.projectileScale);
    }

    private void FixedUpdate()
    {
        if (character != null)
        {
            damage = character.stats.attackDamage;
        }

        if (isHit) return;
        rb.velocity = transform.right * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isHit) return;

        CharacterCore enemyCore = other.GetComponent<CharacterCore>();
        if (enemyCore != null && enemyCore != character && character != null)
        {
            if (hitTargets.Contains(enemyCore))
            {
                return;
            }

            hitTargets.Add(enemyCore);
            enemyCore.TakeDamage(damage);
            ApplyDebuff(enemyCore);

            if (hitTargets.Count < maxHitCount)
            {
                return;
            }
        }

        FinishHit();
    }

    private void ApplyDebuff(CharacterCore enemyCore)
    {
        if (!debuffConfig.HasSlow && !debuffConfig.HasKnockback)
        {
            return;
        }

        if (enemyCore.GetComponent<EnemyStatsManager>() == null && enemyCore.GetComponent<EnemyMove>() == null)
        {
            return;
        }

        InkDebuffReceiver receiver = enemyCore.GetComponent<InkDebuffReceiver>();
        if (receiver == null)
        {
            receiver = enemyCore.gameObject.AddComponent<InkDebuffReceiver>();
        }

        receiver.Apply(debuffConfig, transform.right);
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

        CancelInvoke(nameof(Destroy));
        Destroy(gameObject, hitDestroyDelay);
    }

    public void DestroyAfterHit()
    {
        Destroy(gameObject);
    }
}