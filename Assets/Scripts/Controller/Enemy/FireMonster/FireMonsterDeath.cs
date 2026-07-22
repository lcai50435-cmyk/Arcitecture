using UnityEngine;

/// <summary>
/// Fire monster death logic
/// </summary>
public class FireMonsterDeath : CharacterDeathBase
{
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnCharacterDie()
    {
        // Trigger the Animator death trigger
        anim.SetTrigger("IsDeath");
    }

    protected override void DisablePhysicsComponents()
    {
        // Disable collision
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
