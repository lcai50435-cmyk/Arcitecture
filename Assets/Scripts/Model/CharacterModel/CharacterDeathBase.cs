using UnityEngine;

public class CharacterDeathBase : MonoBehaviour
{
    protected Collider2D characterCollider;
    protected Rigidbody2D characterRigidbody;
    protected Animator anim;
    protected CharacterCore core;

    protected virtual void Awake()
    {
        characterCollider = GetComponent<Collider2D>();
        characterRigidbody = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        core = GetComponent<CharacterCore>();
    }

    public void TriggerCharacterDie()
    {
        DisablePhysicsComponents(); 
        OnCharacterDie();              
    }

    protected virtual void OnEnable()
    {
        if (core != null)
            core.OnDeath += TriggerCharacterDie;
    }

    protected virtual void OnDisable()
    {
        if (core != null)
            core.OnDeath -= TriggerCharacterDie;
    }

    protected virtual void DisablePhysicsComponents()
    {
        // 关闭碰撞
        if (characterCollider != null)
            characterCollider.enabled = true;

        if (characterRigidbody != null)
        {
            characterRigidbody.velocity = Vector2.zero;      // 清空速度
            characterRigidbody.angularVelocity = 0f;         // 清空旋转速度
            characterRigidbody.bodyType = RigidbodyType2D.Static; // 完全静止
        }
    }

    protected virtual void OnCharacterDie()
    {
        // 特殊怪物的死亡方法
    }
}