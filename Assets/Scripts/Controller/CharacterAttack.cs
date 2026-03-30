using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

/// <summary>
/// 封装敌人与玩家相同攻击逻辑
/// </summary>
public abstract class CharacterAttack : MonoBehaviour
{
    [Header("基础攻击配置 [玩家/敌人通用]")]
    public Animator anim;
    public MonoBehaviour moveScript; // 玩家或敌人的移动脚本

    // 攻击状态判定
    protected bool isAttacking = false;
    protected CharacterCore core;

    // 用于扩展个性化逻辑（如播放音效、生成特效）
    public event Action OnAttackStarted;  
    public event Action OnAttackFinished;
  
    public delegate void AttackHitEvent(GameObject attacker, GameObject target, float damage);  // 委托 // 攻击者 + 攻击对象 + 伤害
    public static event AttackHitEvent OnAttackHit; // 攻击命中时触发

    private void Awake()
    {
       core = GetComponent<CharacterCore>();
    }

    #region 角色攻击不能移动且攻击完成时不可再次攻击
    public virtual void TriggerAttack()
    {
        if (isAttacking) return; // 攻击中禁止重复触发
        isAttacking = true;

        // 禁用移动脚本
        if (moveScript != null) moveScript.enabled = false;
        // 触发攻击动画
        if (anim != null) anim.SetBool("IsAttacking", true);

        // 触发攻击开始事件 // 可扩展个性化逻辑（如播音效）
        OnAttackStarted?.Invoke();
    }

    /// <summary>
    /// 攻击动画最后一帧调用该方法
    /// </summary>
    public virtual void OnAttackEnd()
    {
        isAttacking = false;

        // 启用移动脚本
        if (moveScript != null) moveScript.enabled = true;
        // 重置攻击动画
        if (anim != null) anim.SetBool("IsAttacking", false);

        // 触发攻击结束事件 // 扩展个性化逻辑（如停止特效）
        OnAttackFinished?.Invoke();
    }
    #endregion

    // 防止脚本销毁时事件内存泄漏
    protected virtual void OnDisable()
    {
        OnAttackStarted = null;
        OnAttackFinished = null;
    }

    /// <summary>
    /// 执行伤害扣血
    /// </summary>
    /// <param name="target"></param>
    public void HitTarget(GameObject target)
    {
        float dmg = core.stats.attackDamage; 
        OnAttackHit?.Invoke(gameObject, target, dmg);
    }
}
