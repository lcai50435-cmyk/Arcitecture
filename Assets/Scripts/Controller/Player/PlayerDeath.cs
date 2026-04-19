using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDeath :  CharacterDeathBase
{
      protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnCharacterDie()
    {
        // 触发动画机的死亡Trigger
        OnDestroy();
    }

    public void OnDestroy()
    {
        Destroy(gameObject,1f);
    }
}
