using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class SpriteCompanionRuntime
{
    private const string CompanionName = "SpriteCompanion";
    private const string AuthoredIdleCompanionName = "SpriteIdle_0";
    private const string CompanionControllerResourcePath = "RuntimeSpriteCompanion";
    private const string FirstStageId = "stage_01";
    private const int DefaultSortingOrder = 4;
    private const float BaseCompanionScaleMultiplier = 0.54f;
    private const float GameplayCompanionScaleMultiplier = BaseCompanionScaleMultiplier * 1.35f;
    private const float FirstStageCompanionScaleMultiplier = GameplayCompanionScaleMultiplier * 0.8f * 0.8f;
    private const float FootColliderWidthFactor = 0.52f;
    private const float FootColliderHeightFactor = 0.28f;
    private const float FootColliderLiftFactor = 0.04f;
    private static readonly Vector3 CompanionShadowOffset = new Vector3(0.08f, -0.14f, 0f);
    private static readonly Vector3 CompanionShadowScale = new Vector3(0.92f, 0.40f, 1f);
    private static readonly Color CompanionShadowColor = new Color(0.035f, 0.04f, 0.06f, 0.40f);
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
                ApplyCompanionScale(activeCompanion.transform, player.scene.name);
                ConfigureCompanionCollider(activeCompanion.GetComponent<BoxCollider2D>(), activeCompanion.GetComponent<SpriteRenderer>());
                EnsureCompanionShadow(activeCompanion.gameObject);
                EnsureAssistantClickProxy(activeCompanion.gameObject);
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

        Scene playerScene = player.scene;
        GameObject companionObject = FindAuthoredIdleCompanion(playerScene);
        bool reusedAuthoredCompanion = companionObject != null;
        if (companionObject == null)
        {
            companionObject = new GameObject(CompanionName);
        }

        companionObject.name = CompanionName;
        companionObject.layer = player.layer;
        if (playerScene.IsValid() && playerScene.isLoaded && companionObject.scene != playerScene)
        {
            SceneManager.MoveGameObjectToScene(companionObject, playerScene);
        }

        if (!reusedAuthoredCompanion)
        {
            companionObject.transform.position = playerTransform.position;
        }

        companionObject.transform.localScale = Vector3.one * ResolveCompanionScale(playerScene.name);

        SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();
        SpriteRenderer companionRenderer = GetOrAddComponent<SpriteRenderer>(companionObject);
        if (playerRenderer != null)
        {
            companionRenderer.sortingLayerID = playerRenderer.sortingLayerID;
            companionRenderer.sortingOrder = playerRenderer.sortingOrder - 1;
        }
        else
        {
            companionRenderer.sortingOrder = DefaultSortingOrder;
        }

        Animator animator = GetOrAddComponent<Animator>(companionObject);
        animator.runtimeAnimatorController = controller;

        Rigidbody2D body = GetOrAddComponent<Rigidbody2D>(companionObject);
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D collider = GetOrAddComponent<BoxCollider2D>(companionObject);
        ConfigureCompanionCollider(collider, companionRenderer);

        CharacterCore companionCore = GetOrAddComponent<CharacterCore>(companionObject);
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

        EnemyMove move = GetOrAddComponent<EnemyMove>(companionObject);
        move.rb = body;
        move.animator = animator;
        move.autoAssignMovementController = false;
        move.movementAnimatorController = controller;

        GetOrAddComponent<EnemyAvoidObstacle>(companionObject);

        SpriteCompanionAnimator animationDriver = GetOrAddComponent<SpriteCompanionAnimator>(companionObject);
        animationDriver.Bind(move, companionCore, animator);

        animator.Play(SpriteCompanionAnimator.FrontStateName, 0, 0f);
        animator.Update(0f);
        ConfigureCompanionCollider(collider, companionRenderer);
        EnsureCompanionShadow(companionObject);
        EnsureAssistantClickProxy(companionObject);

        SpriteCompanionFollowController followController = GetOrAddComponent<SpriteCompanionFollowController>(companionObject);
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

    private static GameObject FindAuthoredIdleCompanion(Scene playerScene)
    {
        if (!playerScene.IsValid() || !playerScene.isLoaded)
        {
            return null;
        }

        GameObject[] roots = playerScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform match = FindChildByName(roots[i].transform, AuthoredIdleCompanionName);
            if (match != null && match.GetComponent<SpriteCompanionFollowController>() == null)
            {
                return match.gameObject;
            }
        }

        return null;
    }

    private static Transform FindChildByName(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindChildByName(root.GetChild(i), objectName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static T GetOrAddComponent<T>(GameObject target)
        where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void ApplyCompanionScale(Transform companionTransform, string sceneName)
    {
        if (companionTransform == null)
        {
            return;
        }

        companionTransform.localScale = Vector3.one * ResolveCompanionScale(sceneName);
    }

    private static void EnsureCompanionShadow(GameObject companionObject)
    {
        NightLightingController.EnsureProjectedShadow(
            companionObject,
            CompanionShadowOffset,
            CompanionShadowScale,
            CompanionShadowColor);
    }

    private static void EnsureAssistantClickProxy(GameObject companionObject)
    {
        if (companionObject == null || companionObject.GetComponent<SpriteCompanionAssistantClickProxy>() != null)
        {
            return;
        }

        companionObject.AddComponent<SpriteCompanionAssistantClickProxy>();
    }

    internal static void ConfigureCompanionCollider(BoxCollider2D collider, SpriteRenderer renderer)
    {
        if (collider == null)
        {
            return;
        }

        collider.isTrigger = false;
        if (renderer == null || renderer.sprite == null)
        {
            collider.size = CompanionColliderSize;
            collider.offset = CompanionColliderOffset;
            return;
        }

        Bounds bounds = renderer.sprite.bounds;
        float width = Mathf.Max(CompanionColliderSize.x, bounds.size.x * FootColliderWidthFactor);
        float height = Mathf.Max(CompanionColliderSize.y, bounds.size.y * FootColliderHeightFactor);
        collider.size = new Vector2(width, height);
        collider.offset = new Vector2(
            bounds.center.x,
            bounds.min.y + height * 0.5f + bounds.size.y * FootColliderLiftFactor);
    }

    private static float ResolveCompanionScale(string sceneName)
    {
        GameplayStageDefinition stage = GameplayStageCatalog.GetStageByScene(sceneName);
        if (stage == null)
        {
            return BaseCompanionScaleMultiplier;
        }

        return stage.stageId == FirstStageId
            ? FirstStageCompanionScaleMultiplier
            : GameplayCompanionScaleMultiplier;
    }
}

public sealed class SpriteCompanionAssistantClickProxy : MonoBehaviour
{
    private const float ClickDebounceSeconds = 0.12f;

    private Collider2D companionCollider;
    private SpriteRenderer companionRenderer;
    private float nextAllowedClickTime;

    private void Awake()
    {
        companionCollider = GetComponent<Collider2D>();
        companionRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (!GameplayStageCatalog.IsGameplayScene(SceneManager.GetActiveScene().name) ||
            RuntimeUiInputGuard.IsBlockingGameplayUiOpen() ||
            GameplayStageIntroDirector.IsIntroActive ||
            GameplayFailureController.IsFailureActive)
        {
            return;
        }

        HandleMouseClick();
        HandleTouchClick();
    }

    public void ToggleAssistantPanel()
    {
        if (Time.unscaledTime < nextAllowedClickTime)
        {
            return;
        }

        nextAllowedClickTime = Time.unscaledTime + ClickDebounceSeconds;
        BeaverAssistantPanel.EnsureInstance().Toggle();
    }

    private void HandleMouseClick()
    {
        if (!Input.GetMouseButtonDown(0) || IsPointerOverUi())
        {
            return;
        }

        TryToggleFromScreenPosition(Input.mousePosition);
    }

    private void HandleTouchClick()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase != TouchPhase.Began || IsPointerOverUi(touch.fingerId))
            {
                continue;
            }

            if (TryToggleFromScreenPosition(touch.position))
            {
                return;
            }
        }
    }

    private bool TryToggleFromScreenPosition(Vector2 screenPosition)
    {
        Camera targetCamera = Camera.main ?? Object.FindObjectOfType<Camera>();
        if (targetCamera == null || !ContainsScreenPoint(screenPosition, targetCamera))
        {
            return false;
        }

        ToggleAssistantPanel();
        return true;
    }

    private bool ContainsScreenPoint(Vector2 screenPosition, Camera targetCamera)
    {
        float depth = Mathf.Abs(transform.position.z - targetCamera.transform.position.z);
        Vector3 worldPoint = targetCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
        Vector2 worldPoint2D = new Vector2(worldPoint.x, worldPoint.y);

        if (companionCollider == null)
        {
            companionCollider = GetComponent<Collider2D>();
        }

        if (companionCollider != null && companionCollider.OverlapPoint(worldPoint2D))
        {
            return true;
        }

        if (companionRenderer == null)
        {
            companionRenderer = GetComponent<SpriteRenderer>();
        }

        return companionRenderer != null &&
               companionRenderer.bounds.Contains(new Vector3(worldPoint.x, worldPoint.y, companionRenderer.bounds.center.z));
    }

    private static bool IsPointerOverUi(int fingerId = -1)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return false;
        }

        return fingerId >= 0
            ? eventSystem.IsPointerOverGameObject(fingerId)
            : eventSystem.IsPointerOverGameObject();
    }
}
