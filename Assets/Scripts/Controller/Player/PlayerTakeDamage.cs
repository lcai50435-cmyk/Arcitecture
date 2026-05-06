using UnityEngine;

/// <summary>
/// Player hit reaction animation
/// Disable/enable movement
/// </summary>
public class PlayerTakeDamage : MonoBehaviour
{
    private const float HurtMoveLockDuration = 0.12f;

    [Header("组件引用")]
    public Animator playerAnim;
    public PlayerMove playerMovement; // Assign the movement script by dragging it here
    [Header("受击动画参数")]
    public string hurtAnimParam = "IsHurt";
    [Header("血条脚本")]
    public ValueTrans healthTrans; 

    private CharacterCore characterCore;
    private float hurtRecoveryTimer;
    private bool movementLockedByHurt;

    private void Awake()
    {
        characterCore = GetComponent<CharacterCore>();
        healthTrans = GameplayStatusHudRuntime.EnsureHealthGauge(healthTrans);
        PlayerCriticalStateFeedback.Ensure(gameObject);

        if (characterCore != null)
        {
            characterCore.OnTakeDamage += PlayHurtAnimation;
        }

        // Safety check: ensure the movement script reference is not null
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

    private void Update()
    {
        TickHurtRecovery(Time.deltaTime);
    }

    /// <summary>
    /// Play the hit reaction animation and disable movement
    /// </summary>
    private void PlayHurtAnimation()
    {
        RefreshHealthUi();

        if (playerAnim != null)
        {
            if (playerMovement != null)
            {
                bool shouldRestoreMovement = movementLockedByHurt || playerMovement.canMove;
                // Disable movement while taking damage
                playerMovement.canMove = false;
                movementLockedByHurt = shouldRestoreMovement;
                hurtRecoveryTimer = shouldRestoreMovement ? HurtMoveLockDuration : 0f;
                // Clear rigidbody velocity to stop movement immediately
                if (playerMovement.rb != null)
                {
                    playerMovement.rb.velocity = Vector2.zero;
                }
            }

            // Trigger the hit reaction animation
            playerAnim.SetTrigger(hurtAnimParam);
        }
    }

    /// <summary>
    /// Animation event callback: re-enable movement after the hit reaction animation finishes
    /// </summary>
    public void OnHurtAnimationEnd()
    {
        ReleaseHurtMovementLock();
    }

    private void TickHurtRecovery(float deltaTime)
    {
        if (hurtRecoveryTimer <= 0f || playerMovement == null)
        {
            return;
        }

        hurtRecoveryTimer -= Mathf.Max(0f, deltaTime);
        if (hurtRecoveryTimer <= 0f)
        {
            ReleaseHurtMovementLock();
        }
    }

    private void ReleaseHurtMovementLock()
    {
        hurtRecoveryTimer = 0f;
        if (!movementLockedByHurt)
        {
            return;
        }

        movementLockedByHurt = false;
        if (playerMovement != null)
        {
            playerMovement.canMove = true; // Only clear the hit reaction lock
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
