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
        // Trigger the Animator death trigger
        OnDestroy();
    }

    public void OnDestroy()
    {
        Destroy(gameObject,1f);
    }
}
