using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

/// <summary>
/// Encapsulates common character attack logic
/// Cannot attack again while attacking
/// Cannot move while attacking
/// </summary>
public abstract class CharacterAttack : MonoBehaviour
{
    [Header("攻击基础配置 [动画/移动脚本]")]
    public Animator anim;
    public MonoBehaviour moveScript; // Character movement script attached here

    // Attack state check
    protected bool isAttacking = false;
    protected CharacterCore core;
    private Vector3 initialLocalScale;

    // Attack extension events for effects, audio, and related logic
    public event Action OnAttackStarted;
    public event Action OnAttackFinished;

    public delegate void AttackHitEvent(GameObject attacker, GameObject target, float damage);
    public static event AttackHitEvent OnAttackHit; // Triggered when an attack hits

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
        if (isAttacking || core == null || core.IsDead) return; // Block if already attacking or dead

        // Update attack facing from the last facing direction
        UpdateAttackFacingDirection();  

        // Common attack state transition
        isAttacking = true;
        AnimatorParameterUtility.SetBoolIfPresent(anim, "IsMoving", false); // Stop movement animation
        if (playerMove != null)
        {   
            // Disable movement
            playerMove.canMove = false;
            // Clear velocity
            if (playerMove.rb != null)
            {
                playerMove.rb.velocity = Vector2.zero;
            }
        }     
        AnimatorParameterUtility.SetBoolIfPresent(anim, "IsAttacking", true); // Trigger attack animation

        // Trigger attack start event
        OnAttackStarted?.Invoke();
    }

    /// <summary>
    /// Updates attack facing direction (shared by player/enemy)
    /// </summary>
    private void UpdateAttackFacingDirection()
    {
        // Get the last facing direction maintained by CharacterCore
        Vector2 lastFacingDir = core.lastFacingDirection;
        if (lastFacingDir.sqrMagnitude > 0.0001f)
        {
            Vector2 normalizedFacingDir = lastFacingDir.normalized;
            AnimatorParameterUtility.SetFloatIfPresent(anim, "InputX", normalizedFacingDir.x);
            AnimatorParameterUtility.SetFloatIfPresent(anim, "InputY", normalizedFacingDir.y);
        }

        // Update the character Transform facing
        if (ShouldMirrorRootForAttack && lastFacingDir.x != 0) // Horizontal facing
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
        // Extend here if vertical attacks are needed:
        // else if (lastFacingDir.y != 0) 
        // {
        //     // Vertical facing logic (for example, rotation/animation parameters)
        //     anim?.SetFloat("AttackUpDown", lastFacingDir.y);
        // }

        // Optional: pass facing parameters to the animation layer so animations can adapt to different attack directions
        // if (anim != null)
        // {
        //     anim.SetFloat("FacingX", lastFacingDir.x);
        //     anim.SetFloat("FacingY", lastFacingDir.y);
        // }
    }

    /// <summary>
    /// Unified attack end logic (called by an animation frame event)
    /// </summary>
    public virtual void OnAttackEnd()
    {
        isAttacking = false;

        if (core != null && core.IsDead)
        {
            AnimatorParameterUtility.SetBoolIfPresent(anim, "IsAttacking", false);
            return;
        }

        // Restore movement ability
        if (playerMove != null) playerMove.canMove = true;
        AnimatorParameterUtility.SetBoolIfPresent(anim, "IsAttacking", false);

        // Trigger attack end event for extensions such as recovery frames or facing reset
        OnAttackFinished?.Invoke();
    }
    #endregion

    // Prevent event memory leaks
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
    /// Apply damage on attack hit
    /// </summary>
    /// <param name="target">Hit target</param>
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
