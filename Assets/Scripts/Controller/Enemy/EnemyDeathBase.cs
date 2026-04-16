using UnityEngine;

public class EnemyDeathBase : MonoBehaviour
{
    protected CharacterCore core;
    protected Animator anim;

    protected virtual void Awake()
    {
        core = GetComponent<CharacterCore>();
        anim = GetComponent<Animator>();
    }

    protected virtual void OnEnable()
    {
        if (core != null)
            core.OnDeath += OnEnemyDie;
    }

    protected virtual void OnDisable()
    {
        if (core != null)
            core.OnDeath -= OnEnemyDie;
    }

    protected virtual void OnEnemyDie()
    {
        // 子类重写，各自实现
    }
}