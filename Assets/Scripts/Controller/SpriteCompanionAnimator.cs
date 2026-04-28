using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemyMove))]
[RequireComponent(typeof(CharacterCore))]
public sealed class SpriteCompanionAnimator : MonoBehaviour
{
    public const string FrontStateName = "Sprite";
    public const string LeftStateName = "SpriteLeft";
    public const string RightStateName = "SpriteR";
    public const string BackStateName = "SpriteB";

    private EnemyMove move;
    private CharacterCore core;
    private Animator animator;
    private string currentStateName;

    public void Bind(EnemyMove moveComponent, CharacterCore coreComponent = null, Animator animatorComponent = null)
    {
        move = moveComponent != null ? moveComponent : GetComponent<EnemyMove>();
        core = coreComponent != null ? coreComponent : GetComponent<CharacterCore>();
        animator = animatorComponent != null ? animatorComponent : GetComponent<Animator>();
        ApplyState(true);
    }

    private void Awake()
    {
        move = GetComponent<EnemyMove>();
        core = GetComponent<CharacterCore>();
        animator = GetComponent<Animator>();
    }

    private void LateUpdate()
    {
        ApplyState(false);
    }

    private void ApplyState(bool force)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        Vector2 facingDirection = move != null && move.moveDirection != Vector2.zero
            ? move.moveDirection
            : (core != null ? core.lastFacingDirection : Vector2.down);

        string nextStateName = ResolveStateName(facingDirection);
        if (!force && nextStateName == currentStateName)
        {
            return;
        }

        animator.Play(nextStateName, 0, 0f);
        if (force)
        {
            animator.Update(0f);
        }

        currentStateName = nextStateName;
    }

    private static string ResolveStateName(Vector2 direction)
    {
        if (direction.x <= -0.1f)
        {
            return LeftStateName;
        }

        if (direction.x >= 0.1f)
        {
            return RightStateName;
        }

        if (direction.y >= 0.1f)
        {
            return BackStateName;
        }

        return FrontStateName;
    }
}
