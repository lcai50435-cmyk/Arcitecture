using UnityEngine;

public class FireBall : MonoBehaviour
{
    [Header("基础设置")]
    public float speed = 6f;
    public float autoDestroyTime = 10f; // Auto-destroy after 10 seconds if no hit occurs
    public float hitDestroyDelay = 3f;  // Fallback destroy delay after a hit

    private float damage = 10;
    private Animator anim;
    private Rigidbody2D rb;
    private Collider2D hitCollider;
    private bool isHit = false;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        hitCollider = GetComponent<Collider2D>();

        // Null check to avoid null references
        if (anim == null) Debug.LogError($"[{gameObject.name}] 缺少Animator组件！");
        if (rb == null) Debug.LogError($"[{gameObject.name}] 缺少Rigidbody2D组件！");
    }

    private void OnEnable()
    {
        Initialize();
    }

    public void Initialize()
    {
        isHit = false;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (hitCollider != null)
        {
            hitCollider.enabled = true;
        }

        if (anim != null)
        {
            anim.ResetTrigger("IsHit");
        }

        NightLightingController.EnsureTransientFxLight(
            gameObject,
            0.95f,
            0.12f,
            NightLightingController.GetGameplayFireballLightColor());

        CombatObjectPool.ReleaseOrDestroy(gameObject, autoDestroyTime);
    }

    private void FixedUpdate()
    {      
        if (isHit) return;
        rb.velocity = transform.right * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isHit) return;
        if (!other.CompareTag("Player")) return;

        isHit = true;

        // Stop movement
        rb.velocity = Vector2.zero;

        // Get the player CharacterCore script
        CharacterCore playerCore = other.GetComponent<CharacterCore>();
        if (playerCore != null)
        {
            // Damage the player
            playerCore.TakeDamage(damage);
        }

        // Play hit animation with null protection
        if (anim != null)
            anim.SetTrigger("IsHit");

        // Disable the collider to avoid repeated triggers
        if (hitCollider != null)
            hitCollider.enabled = false;

        // Avoid getting stuck
        CombatObjectPool.ReleaseOrDestroy(gameObject, hitDestroyDelay);
    }

    // Destroy after the hit animation finishes
    public void DestroyAfterHit()
    {
        CombatObjectPool.ReleaseOrDestroy(gameObject);
    }
}
