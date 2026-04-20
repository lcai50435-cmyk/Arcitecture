using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireBall : MonoBehaviour
{
    [Header("基础设置")]
    public float speed = 6f;
    public float autoDestroyTime = 10f; // 未命中10秒自动销毁
    public float hitDestroyDelay = 3f;  // 命中后兜底销毁延迟

    private float damage = 10;
    private Animator anim;
    private Rigidbody2D rb;
    private bool isHit = false;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        // 判空校验，避免空引用
        if (anim == null) Debug.LogError($"[{gameObject.name}] 缺少Animator组件！");
        if (rb == null) Debug.LogError($"[{gameObject.name}] 缺少Rigidbody2D组件！");
    }

    private void Start()
    {
        // 未命中10秒自动销毁
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

        // 停止移动
        rb.velocity = Vector2.zero;

        // 获得玩家脚本CharacterCore
        CharacterCore playerCore = other.GetComponent<CharacterCore>();
        if (playerCore != null)
        {
            // 对玩家造成伤害
            playerCore.TakeDamage(damage);
        }

        // 播放命中动画（判空保护）
        if (anim != null)
            anim.SetTrigger("IsHit");

        // 关闭碰撞器，避免重复触发
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        // 取消原本的10秒自动销毁，避免和动画事件冲突
        CancelInvoke(nameof(Destroy));

        // 避免卡死
        Destroy(gameObject, hitDestroyDelay);
    }

    // 命中动画播完后销毁
    public void DestroyAfterHit()
    {
        Destroy(gameObject);
    }
}
