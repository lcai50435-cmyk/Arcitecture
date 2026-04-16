using UnityEngine;

/// <summary>
/// 玩家受击核心逻辑：播放受击动画 + 禁用/启用移动
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
        // healthTrans = GetComponent<HealthTrans>();

        characterCore.OnTakeDamage += PlayHurtAnimation;

        // 安全校验：确保移动脚本引用不为空
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMove>();
        }

        healthTrans.SetMaxValue(characterCore.stats.maxHp);
    }

    /// <summary>
    /// 播放受击动画并禁用移动
    /// </summary>
    private void PlayHurtAnimation()
    {
        if (playerAnim == null || playerMovement == null) return;

        // 受击时禁止移动
        playerMovement.canMove = false;
        // 清空刚体速度，立即停止位移
        if (playerMovement.rb != null)
        {
            playerMovement.rb.velocity = Vector2.zero;
        }

        // 触发受击动画
        playerAnim.SetTrigger(hurtAnimParam);

        // 血条减少
        healthTrans.SetValue(characterCore.currentHp);
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
}