using System.Collections;
using System.Collections.Generic;
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
    private bool isHit = false;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        // Null check to avoid null references
        if (anim == null) Debug.LogError($"[{gameObject.name}] 缺少Animator组件！");
        if (rb == null) Debug.LogError($"[{gameObject.name}] 缺少Rigidbody2D组件！");
    }

    private void Start()
    {
        NightLightingController.EnsureTransientFxLight(
            gameObject,
            0.95f,
            0.12f,
            NightLightingController.GetGameplayFireballLightColor());

        // Auto-destroy after 10 seconds if no hit occurs
        Destroy(gameObject, autoDestroyTime);
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
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        // Cancel the original 10-second auto-destroy to avoid conflicts with animation events
        CancelInvoke(nameof(Destroy));

        // Avoid getting stuck
        Destroy(gameObject, hitDestroyDelay);
    }

    // Destroy after the hit animation finishes
    public void DestroyAfterHit()
    {
        Destroy(gameObject);
    }
}
