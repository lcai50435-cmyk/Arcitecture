using UnityEngine;

/// <summary>
/// Stone monster death logic
/// </summary>
public class StoneMonsterDeath : CharacterDeathBase
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

    public void DestroyAfterDeathAnimation()
    {
        CompleteDeathDestroy();
    }
}
