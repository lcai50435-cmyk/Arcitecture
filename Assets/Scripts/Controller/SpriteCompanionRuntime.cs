using UnityEngine;
using UnityEngine.SceneManagement;

public static class SpriteCompanionRuntime
{
    private const string CompanionName = "SpriteCompanion";
    private const string CompanionControllerResourcePath = "RuntimeSpriteCompanion";
    private const int DefaultSortingOrder = 4;
    private const float CompanionScaleMultiplier = 0.54f;
    private static readonly Vector2 CompanionColliderSize = new Vector2(0.28f, 0.32f);
    private static readonly Vector2 CompanionColliderOffset = new Vector2(0f, -0.02f);

    private static SpriteCompanionFollowController activeCompanion;

    public static SpriteCompanionFollowController EnsureForPlayer(GameObject player)
    {
        if (player == null)
        {
            return null;
        }

        Transform playerTransform = player.transform;
        CharacterCore playerCore = player.GetComponent<CharacterCore>();

        if (activeCompanion == null)
        {
            activeCompanion = Object.FindObjectOfType<SpriteCompanionFollowController>(true);
        }

        if (activeCompanion != null)
        {
            if (activeCompanion.IsBoundTo(playerTransform))
            {
                activeCompanion.Bind(playerTransform, playerCore);
                return activeCompanion;
            }

            Object.Destroy(activeCompanion.gameObject);
            activeCompanion = null;
        }

        RuntimeAnimatorController controller = Resources.Load<RuntimeAnimatorController>(CompanionControllerResourcePath);
        if (controller == null)
        {
            Debug.LogWarning("未找到 Sprite 伴生体动画控制器 RuntimeSpriteCompanion，伴生体不会创建。", player);
            return null;
        }

        GameObject companionObject = new GameObject(CompanionName);
        companionObject.layer = player.layer;
        Scene playerScene = player.scene;
        if (playerScene.IsValid() && playerScene.isLoaded && companionObject.scene != playerScene)
        {
            SceneManager.MoveGameObjectToScene(companionObject, playerScene);
        }

        companionObject.transform.position = playerTransform.position;
        companionObject.transform.localScale = Vector3.one * CompanionScaleMultiplier;

        SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();
        SpriteRenderer companionRenderer = companionObject.AddComponent<SpriteRenderer>();
        if (playerRenderer != null)
        {
            companionRenderer.sortingLayerID = playerRenderer.sortingLayerID;
            companionRenderer.sortingOrder = playerRenderer.sortingOrder - 1;
        }
        else
        {
            companionRenderer.sortingOrder = DefaultSortingOrder;
        }

        Animator animator = companionObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        Rigidbody2D body = companionObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D collider = companionObject.AddComponent<BoxCollider2D>();
        collider.size = CompanionColliderSize;
        collider.offset = CompanionColliderOffset;

        CharacterCore companionCore = companionObject.AddComponent<CharacterCore>();
        CharacterStats stats = new CharacterStats
        {
            maxHp = 1f,
            attackDamage = 0f,
            moveSpeed = ResolveMoveSpeed(playerCore),
            defense = 0f
        };
        companionCore.baseStats = stats.Clone();
        companionCore.stats = stats.Clone();
        companionCore.currentHp = stats.maxHp;
        companionCore.lastFacingDirection = Vector2.down;

        EnemyMove move = companionObject.AddComponent<EnemyMove>();
        move.rb = body;
        move.animator = animator;
        move.autoAssignMovementController = false;
        move.movementAnimatorController = controller;

        companionObject.AddComponent<EnemyAvoidObstacle>();

        SpriteCompanionAnimator animationDriver = companionObject.AddComponent<SpriteCompanionAnimator>();
        animationDriver.Bind(move, companionCore, animator);

        animator.Play(SpriteCompanionAnimator.FrontStateName, 0, 0f);
        animator.Update(0f);

        SpriteCompanionFollowController followController = companionObject.AddComponent<SpriteCompanionFollowController>();
        followController.Bind(playerTransform, playerCore, collider);

        activeCompanion = followController;
        return followController;
    }

    internal static void NotifyDestroyed(SpriteCompanionFollowController companion)
    {
        if (activeCompanion == companion)
        {
            activeCompanion = null;
        }
    }

    private static float ResolveMoveSpeed(CharacterCore playerCore)
    {
        if (playerCore == null || playerCore.stats == null)
        {
            return 4.8f;
        }

        return Mathf.Max(4.8f, playerCore.stats.moveSpeed + 0.75f);
    }
}
