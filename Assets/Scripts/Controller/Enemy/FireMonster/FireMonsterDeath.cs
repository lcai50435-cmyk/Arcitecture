using UnityEngine;

/// <summary>
/// 火灾怪死亡相关逻辑
/// </summary>
public class FireMonsterDeath : CharacterDeathBase
{
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnCharacterDie()
    {
        // 触发动画机的死亡Trigger
        anim.SetTrigger("IsDeath");
    }

    protected override void DisablePhysicsComponents()
    {
        // 关闭碰撞
        if (characterCollider != null)
            characterCollider.enabled = false;

        if (characterRigidbody != null)
        {
            characterRigidbody.velocity = Vector2.zero;
            characterRigidbody.angularVelocity = 0f;
            characterRigidbody.bodyType = RigidbodyType2D.Static;
        }
    }

    public void DestroyAfterDeathAnimation()
    {
        CompleteDeathDestroy();
    }
}
