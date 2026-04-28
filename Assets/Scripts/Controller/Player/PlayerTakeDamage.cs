using UnityEngine;

/// <summary>
/// 玩家播放受击动画
/// 禁用/启用移动
/// </summary>
public class PlayerTakeDamage : MonoBehaviour
{
    [Header("组件引用")]
    public Animator playerAnim;
    public PlayerMove playerMovement; // 拖拽赋值移动脚本
    [Header("受击动画参数")]
    public string hurtAnimParam = "IsHurt";
    [Header("血条脚本")]
    public ValueTrans healthTrans; 

    private CharacterCore characterCore;

    private void Awake()
    {
        characterCore = GetComponent<CharacterCore>();
        healthTrans = GameplayStatusHudRuntime.EnsureHealthGauge(healthTrans);
        PlayerCriticalStateFeedback.Ensure(gameObject);

        if (characterCore != null)
        {
            characterCore.OnTakeDamage += PlayHurtAnimation;
        }

        // 安全校验：确保移动脚本引用不为空
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMove>();
        }

        RefreshHealthUi();
    }

    private void Start()
    {
        healthTrans = GameplayStatusHudRuntime.EnsureHealthGauge(healthTrans);
        RefreshHealthUi();
    }

    /// <summary>
    /// 播放受击动画并禁用移动
    /// </summary>
    private void PlayHurtAnimation()
    {
        RefreshHealthUi();

        if (playerAnim != null)
        {
            if (playerMovement != null)
            {
                // 受击时禁止移动
                playerMovement.canMove = false;
                // 清空刚体速度，立即停止位移
                if (playerMovement.rb != null)
                {
                    playerMovement.rb.velocity = Vector2.zero;
                }
            }

            // 触发受击动画
            playerAnim.SetTrigger(hurtAnimParam);
        }
    }

    /// <summary>
    /// 动画事件回调：受击动画播放完成后启用移动
    /// </summary>
    public void OnHurtAnimationEnd()
    {
        if (playerMovement != null)
        {
            playerMovement.canMove = true; // 只关移动
        }
    }

    private void OnDestroy()
    {
        if (characterCore != null)
        {
            characterCore.OnTakeDamage -= PlayHurtAnimation;
        }
    }

    private void RefreshHealthUi()
    {
        if (characterCore == null || characterCore.stats == null)
        {
            return;
        }

        healthTrans = GameplayStatusHudRuntime.EnsureHealthGauge(healthTrans);
        if (healthTrans == null)
        {
            return;
        }

        healthTrans.SetMaxValue(characterCore.stats.maxHp);
        healthTrans.SetValue(characterCore.currentHp);
        GameplayStatusHudRuntime.RefreshHealthText(characterCore.currentHp, characterCore.stats.maxHp);
    }
}
