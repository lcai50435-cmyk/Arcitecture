using UnityEngine;

/// <summary>
/// 地震怪死亡相关逻辑
/// </summary>
public class StoneMonsterDeath : CharacterDeathBase
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

    public void DestroyAfterDeathAnimation()
    {
        CompleteDeathDestroy();
    }
}
