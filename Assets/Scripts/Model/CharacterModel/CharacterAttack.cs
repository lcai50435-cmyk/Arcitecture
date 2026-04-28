using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

/// <summary>
/// 封装角色攻击的通用逻辑
/// 攻击期间不得再次攻击
/// 攻击期间不得移动
/// </summary>
public abstract class CharacterAttack : MonoBehaviour
{
    [Header("攻击基础配置 [动画/移动脚本]")]
    public Animator anim;
    public MonoBehaviour moveScript; // 挂载角色移动的脚本

    // 攻击状态判断
    protected bool isAttacking = false;
    protected CharacterCore core;
    private Vector3 initialLocalScale;

    // 攻击扩展事件（可挂载攻击特效/音效等逻辑）
    public event Action OnAttackStarted;
    public event Action OnAttackFinished;

    public delegate void AttackHitEvent(GameObject attacker, GameObject target, float damage);
    public static event AttackHitEvent OnAttackHit; // 攻击命中时触发

    protected PlayerMove playerMove;

    protected virtual bool ShouldMirrorRootForAttack => true;

    protected virtual void Awake()
    {
        initialLocalScale = transform.localScale;
        core = GetComponent<CharacterCore>();
        if (moveScript != null)
        {
            playerMove = moveScript.GetComponent<PlayerMove>();
        }

        if (core == null)
        {
            Debug.LogError($"[{gameObject.name}] 未挂载 CharacterCore 组件！", this);
        }
    }

    protected virtual void OnEnable()
    {
        if (core == null)
        {
            return;
        }

        core.OnDeath -= HandleOwnerDeath;
        core.OnDeath += HandleOwnerDeath;
    }

    #region 角色攻击（复用核心逻辑：面朝方向 + 移动禁用 + 动画触发）
    public virtual void TriggerAttack()
    {
        if (isAttacking || core == null || core.IsDead) return; // 攻击中或者已死亡则拦截

        // 根据最后面朝方向更新攻击朝向
        UpdateAttackFacingDirection();  

        // 通用攻击状态切换
        isAttacking = true;
        AnimatorParameterUtility.SetBoolIfPresent(anim, "IsMoving", false); // 停止移动动画
        if (playerMove != null)
        {   
            // 禁用移动
            playerMove.canMove = false;
            // 速度清零
            if (playerMove.rb != null)
            {
                playerMove.rb.velocity = Vector2.zero;
            }
        }     
        AnimatorParameterUtility.SetBoolIfPresent(anim, "IsAttacking", true); // 触发攻击动画

        // 触发攻击开始事件
        OnAttackStarted?.Invoke();
    }

    /// <summary>
    /// 更新攻击的面朝方向（玩家/敌人通用）
    /// </summary>
    private void UpdateAttackFacingDirection()
    {
        // 获取CharacterCore中维护的「最后面朝方向」
        Vector2 lastFacingDir = core.lastFacingDirection;
        if (lastFacingDir.sqrMagnitude > 0.0001f)
        {
            Vector2 normalizedFacingDir = lastFacingDir.normalized;
            AnimatorParameterUtility.SetFloatIfPresent(anim, "InputX", normalizedFacingDir.x);
            AnimatorParameterUtility.SetFloatIfPresent(anim, "InputY", normalizedFacingDir.y);
        }

        // 更新角色Transform朝向
        if (ShouldMirrorRootForAttack && lastFacingDir.x != 0) // 左右朝向
        {
            float scaleX = Mathf.Abs(initialLocalScale.x);
            if (scaleX <= Mathf.Epsilon)
            {
                scaleX = Mathf.Abs(transform.localScale.x);
            }

            transform.localScale = new Vector3(
                Mathf.Sign(lastFacingDir.x) * scaleX,
                transform.localScale.y,
                transform.localScale.z
            );
        }
        // 若有上下攻击需求，可扩展：
        // else if (lastFacingDir.y != 0) 
        // {
        //     // 上下朝向逻辑（如旋转/动画参数）
        //     anim?.SetFloat("AttackUpDown", lastFacingDir.y);
        // }

        // 可选：给动画层传递朝向参数（便于动画适配不同方向攻击）
        // if (anim != null)
        // {
        //     anim.SetFloat("FacingX", lastFacingDir.x);
        //     anim.SetFloat("FacingY", lastFacingDir.y);
        // }
    }

    /// <summary>
    /// 攻击结束统一逻辑（动画帧事件调用）
    /// </summary>
    public virtual void OnAttackEnd()
    {
        isAttacking = false;

        if (core != null && core.IsDead)
        {
            AnimatorParameterUtility.SetBoolIfPresent(anim, "IsAttacking", false);
            return;
        }

        // 恢复移动能力
        if (playerMove != null) playerMove.canMove = true;
        AnimatorParameterUtility.SetBoolIfPresent(anim, "IsAttacking", false);

        // 触发攻击结束事件（扩展逻辑：如攻击后摇、重置朝向）
        OnAttackFinished?.Invoke();
    }
    #endregion

    // 防止事件内存泄漏
    protected virtual void OnDisable()
    {
        if (core != null)
        {
            core.OnDeath -= HandleOwnerDeath;
        }

        OnAttackStarted = null;
        OnAttackFinished = null;
    }

    public void HandleOwnerDeathImmediate()
    {
        isAttacking = false;

        if (playerMove != null)
        {
            playerMove.canMove = false;
            if (playerMove.rb != null)
            {
                playerMove.rb.velocity = Vector2.zero;
            }
        }

        AnimatorParameterUtility.SetBoolIfPresent(anim, "IsAttacking", false);
        enabled = false;
    }

    /// <summary>
    /// 攻击命中扣血
    /// </summary>
    /// <param name="target">被命中的目标</param>
    public void HitTarget(GameObject target)
    {
        if (core == null) return;
        float dmg = core.stats.attackDamage;
        OnAttackHit?.Invoke(gameObject, target, dmg);
    }

    private void HandleOwnerDeath()
    {
        HandleOwnerDeathImmediate();
    }
}
