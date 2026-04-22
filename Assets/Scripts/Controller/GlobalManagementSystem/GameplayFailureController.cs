using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameplayFailureReason
{
    PlayerDeath,
    TimeExpired
}

public class GameplayFailureController : MonoBehaviour
{
    private const string DefaultGameOverSceneName = "DeadScene";
    private const float DropScatterDuration = 0.72f;
    private const float DropScatterDelayStep = 0.04f;
    private const float DropScatterMinDistance = 0.7f;
    private const float DropScatterMaxDistance = 1.45f;
    private const float DropScatterArcHeight = 0.55f;
    private const float SceneTransitionDelay = 0.16f;
    private const string FailureOverlayCanvasName = "FailureSpotlightCanvas";
    private const string FailureOverlayGraphicName = "FailureSpotlightGraphic";
    private const int FailureOverlaySortingOrder = 12000;
    private const float SpotlightFadeInDuration = 0.18f;
    private const float SpotlightFocusDuration = 1.1f;
    private const float SpotlightHoldDuration = 0.5f;
    private const float SpotlightStartAlpha = 0.9f;
    private const float SpotlightEndAlpha = 1f;
    private const float SpotlightStartRadius = 980f;
    private const float SpotlightFallbackEndRadius = 180f;
    private const float SpotlightRadiusPadding = 72f;
    private const float SpotlightFeatherRadius = 42f;
    private static readonly Color FailureOverlayColor = Color.black;

    public static GameplayFailureController Instance { get; private set; }
    public static bool IsFailureActive => Instance != null && Instance.isFailureActive;

    private bool isFailureActive;
    private Canvas failureOverlayCanvas;
    private RectTransform failureOverlayRect;
    private DeathSpotlightGraphic failureSpotlightGraphic;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureInstance(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstance(scene);
    }

    public static bool TryTriggerFailure(
        GameplayFailureReason reason,
        string gameOverSceneName = DefaultGameOverSceneName)
    {
        GameplayFailureController controller = EnsureInstance(SceneManager.GetActiveScene());
        return controller != null && controller.TryStartFailure(reason, gameOverSceneName);
    }

    private static GameplayFailureController EnsureInstance(Scene scene)
    {
        if (!GameplayStageCatalog.IsGameplayScene(scene.name))
        {
            return null;
        }

        if (Instance != null)
        {
            return Instance;
        }

        GameplayFailureController existing = FindObjectOfType<GameplayFailureController>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject runtimeObject = new GameObject("GameplayFailureController");
        Instance = runtimeObject.AddComponent<GameplayFailureController>();
        return Instance;
    }

    private void OnDestroy()
    {
        if (failureOverlayCanvas != null)
        {
            Destroy(failureOverlayCanvas.gameObject);
            failureOverlayCanvas = null;
            failureOverlayRect = null;
            failureSpotlightGraphic = null;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private bool TryStartFailure(GameplayFailureReason reason, string gameOverSceneName)
    {
        if (isFailureActive)
        {
            return true;
        }

        isFailureActive = true;
        StartCoroutine(HandleFailureRoutine(reason, string.IsNullOrWhiteSpace(gameOverSceneName) ? DefaultGameOverSceneName : gameOverSceneName));
        return true;
    }

    private IEnumerator HandleFailureRoutine(GameplayFailureReason reason, string gameOverSceneName)
    {
        Time.timeScale = 0f;
        HideGameplayUi();
        DisablePlayerControls();

        GameCountDownManager countdownManager = GameCountDownManager.Instance != null
            ? GameCountDownManager.Instance
            : FindObjectOfType<GameCountDownManager>();
        countdownManager?.SetInBaseState(true);

        RunStageDirector director = FindObjectOfType<RunStageDirector>();
        director?.SuspendRuntime();

        BackpackMananger backpack = BackpackMananger.Instance;
        List<ArchitecturalCrystal> droppedItems = SnapshotBackpackItems(backpack);
        Vector3 dropOrigin = ResolveDropOrigin();
        Vector3 failureFocusOrigin = ResolveFailureFocusOrigin(dropOrigin);
        float spotlightDuration = PlayDeathSpotlightTransition(failureFocusOrigin);
        float waitDuration = PlayDropScatterAnimation(droppedItems, dropOrigin);

        float combinedWaitDuration = Mathf.Max(waitDuration, spotlightDuration);
        if (combinedWaitDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(combinedWaitDuration);
        }

        if (backpack != null)
        {
            backpack.ClearAllItems();
        }

        yield return new WaitForSecondsRealtime(SceneTransitionDelay);

        Time.timeScale = 1f;

        SceneLoader loader = SceneLoader.EnsureInstance();
        if (loader != null)
        {
            loader.ToScene(gameOverSceneName);
            yield break;
        }

        SceneManager.LoadScene(gameOverSceneName);
    }

    private static void HideGameplayUi()
    {
        if (UIRootManager.Instance == null)
        {
            return;
        }

        UIRootManager.Instance.HideHandbook();
        UIRootManager.Instance.HideAllDetail();
        UIRootManager.Instance.HideAllSubmitSelection();
        UIRootManager.Instance.HideDialog();
        UIRootManager.Instance.HideInteractTip();
        UIRootManager.Instance.HideBackpack(true);
    }

    private static void DisablePlayerControls()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            return;
        }

        PlayerMove move = playerObject.GetComponent<PlayerMove>();
        if (move != null)
        {
            move.canMove = false;
            if (move.rb != null)
            {
                move.rb.velocity = Vector2.zero;
            }
        }

        PlayerAttack attack = playerObject.GetComponent<PlayerAttack>();
        if (attack != null)
        {
            attack.enabled = false;
        }

        PlayerInteraction interaction = playerObject.GetComponent<PlayerInteraction>();
        if (interaction != null)
        {
            interaction.ClearCurrentInteractable();
            interaction.enabled = false;
        }
    }

    private static List<ArchitecturalCrystal> SnapshotBackpackItems(BackpackMananger backpack)
    {
        List<ArchitecturalCrystal> crystals = new List<ArchitecturalCrystal>();
        if (backpack == null || backpack.backpackItems == null)
        {
            return crystals;
        }

        for (int i = 0; i < backpack.backpackItems.Count; i++)
        {
            ArchitecturalCrystal? nullableItem = backpack.backpackItems[i];
            if (nullableItem.HasValue)
            {
                crystals.Add(nullableItem.Value);
            }
        }

        return crystals;
    }

    private static Vector3 ResolveDropOrigin()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            Vector3 position = playerObject.transform.position;
            position.z = 0f;
            return position;
        }

        return Vector3.zero;
    }

    private static Vector3 ResolveFailureFocusOrigin(Vector3 fallbackOrigin)
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            return fallbackOrigin;
        }

        Renderer playerRenderer = playerObject.GetComponentInChildren<Renderer>();
        if (playerRenderer != null)
        {
            Vector3 center = playerRenderer.bounds.center;
            center.z = 0f;
            return center;
        }

        Vector3 position = playerObject.transform.position;
        position.z = 0f;
        return position;
    }

    private float PlayDeathSpotlightTransition(Vector3 focusWorldPosition)
    {
        EnsureFailureOverlay();
        if (failureSpotlightGraphic == null)
        {
            return 0f;
        }

        StartCoroutine(AnimateFailureSpotlight(focusWorldPosition));
        return SpotlightFadeInDuration + SpotlightFocusDuration + SpotlightHoldDuration;
    }

    private void EnsureFailureOverlay()
    {
        if (failureOverlayCanvas != null && failureSpotlightGraphic != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject(
            FailureOverlayCanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        failureOverlayCanvas = canvasObject.GetComponent<Canvas>();
        failureOverlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        failureOverlayCanvas.overrideSorting = true;
        failureOverlayCanvas.sortingOrder = FailureOverlaySortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
        raycaster.enabled = false;

        failureOverlayRect = canvasObject.GetComponent<RectTransform>();
        failureOverlayRect.anchorMin = Vector2.zero;
        failureOverlayRect.anchorMax = Vector2.one;
        failureOverlayRect.offsetMin = Vector2.zero;
        failureOverlayRect.offsetMax = Vector2.zero;
        failureOverlayRect.localScale = Vector3.one;

        GameObject spotlightObject = new GameObject(
            FailureOverlayGraphicName,
            typeof(RectTransform),
            typeof(DeathSpotlightGraphic));
        spotlightObject.transform.SetParent(failureOverlayRect, false);

        RectTransform spotlightRect = spotlightObject.GetComponent<RectTransform>();
        spotlightRect.anchorMin = Vector2.zero;
        spotlightRect.anchorMax = Vector2.one;
        spotlightRect.offsetMin = Vector2.zero;
        spotlightRect.offsetMax = Vector2.zero;
        spotlightRect.localScale = Vector3.one;

        failureSpotlightGraphic = spotlightObject.GetComponent<DeathSpotlightGraphic>();
        failureSpotlightGraphic.raycastTarget = false;
        failureSpotlightGraphic.SetBaseColor(FailureOverlayColor);

        Canvas.ForceUpdateCanvases();
        failureSpotlightGraphic.SetSpotlight(new Vector2(0.5f, 0.5f), SpotlightStartRadius, SpotlightFeatherRadius, 0f);
    }

    private IEnumerator AnimateFailureSpotlight(Vector3 focusWorldPosition)
    {
        if (failureSpotlightGraphic == null)
        {
            yield break;
        }

        Vector2 focusViewport = ResolveFocusViewport(focusWorldPosition);
        float targetRadius = ResolveFocusRadius(focusViewport);

        float elapsed = 0f;
        while (elapsed < SpotlightFadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, SpotlightFadeInDuration));
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            float alpha = Mathf.Lerp(0f, SpotlightStartAlpha, easedT);
            failureSpotlightGraphic.SetSpotlight(
                focusViewport,
                SpotlightStartRadius,
                SpotlightFeatherRadius,
                alpha);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < SpotlightFocusDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, SpotlightFocusDuration));
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            float radius = Mathf.Lerp(SpotlightStartRadius, targetRadius, easedT);
            float alpha = Mathf.Lerp(SpotlightStartAlpha, SpotlightEndAlpha, easedT);
            failureSpotlightGraphic.SetSpotlight(
                focusViewport,
                radius,
                SpotlightFeatherRadius,
                alpha);
            yield return null;
        }

        failureSpotlightGraphic.SetSpotlight(
            focusViewport,
            targetRadius,
            SpotlightFeatherRadius,
            SpotlightEndAlpha);

        if (SpotlightHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(SpotlightHoldDuration);
        }
    }

    private static Vector2 ResolveFocusViewport(Vector3 focusWorldPosition)
    {
        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            targetCamera = FindObjectOfType<Camera>();
        }

        if (targetCamera == null)
        {
            return new Vector2(0.5f, 0.5f);
        }

        Vector3 viewportPoint = targetCamera.WorldToViewportPoint(focusWorldPosition);
        if (viewportPoint.z < 0f)
        {
            return new Vector2(0.5f, 0.5f);
        }

        return new Vector2(
            Mathf.Clamp01(viewportPoint.x),
            Mathf.Clamp01(viewportPoint.y));
    }

    private Vector2 ResolveFocusAnchoredPosition(Vector2 focusViewport)
    {
        if (failureOverlayRect == null)
        {
            return Vector2.zero;
        }

        Rect rect = failureOverlayRect.rect;
        float width = rect.width > 1f ? rect.width : Screen.width;
        float height = rect.height > 1f ? rect.height : Screen.height;
        return new Vector2(
            (focusViewport.x - 0.5f) * width,
            (focusViewport.y - 0.5f) * height);
    }

    private float ResolveFocusRadius(Vector2 focusViewport)
    {
        if (failureOverlayRect == null)
        {
            return SpotlightFallbackEndRadius;
        }

        Rect rect = failureOverlayRect.rect;
        float canvasWidth = rect.width > 1f ? rect.width : Screen.width;
        float canvasHeight = rect.height > 1f ? rect.height : Screen.height;

        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            targetCamera = FindObjectOfType<Camera>();
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        Renderer playerRenderer = playerObject != null ? playerObject.GetComponentInChildren<Renderer>() : null;
        if (targetCamera == null || playerRenderer == null)
        {
            return SpotlightFallbackEndRadius;
        }

        Bounds bounds = playerRenderer.bounds;
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;
        Vector2 focusAnchoredPosition = ResolveFocusAnchoredPosition(focusViewport);
        Vector3[] corners =
        {
            center + new Vector3(-extents.x, -extents.y, 0f),
            center + new Vector3(-extents.x, extents.y, 0f),
            center + new Vector3(extents.x, -extents.y, 0f),
            center + new Vector3(extents.x, extents.y, 0f)
        };

        float maxDistance = 0f;
        bool hasValidCorner = false;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 viewportPoint = targetCamera.WorldToViewportPoint(corners[i]);
            if (viewportPoint.z < 0f)
            {
                continue;
            }

            hasValidCorner = true;
            Vector2 cornerAnchoredPosition = ResolveFocusAnchoredPosition(
                new Vector2(
                    Mathf.Clamp01(viewportPoint.x),
                    Mathf.Clamp01(viewportPoint.y)));
            float distance = Vector2.Distance(focusAnchoredPosition, cornerAnchoredPosition);
            maxDistance = Mathf.Max(maxDistance, distance);
        }

        if (!hasValidCorner)
        {
            return SpotlightFallbackEndRadius;
        }

        float radius = maxDistance + SpotlightRadiusPadding;
        float maxRadius = Mathf.Sqrt(canvasWidth * canvasWidth + canvasHeight * canvasHeight);
        return Mathf.Clamp(radius, SpotlightFallbackEndRadius, maxRadius);
    }

    private float PlayDropScatterAnimation(List<ArchitecturalCrystal> droppedItems, Vector3 origin)
    {
        if (droppedItems == null || droppedItems.Count == 0)
        {
            return 0f;
        }

        float maxDuration = 0f;
        float angleStep = 360f / Mathf.Max(1, droppedItems.Count);

        for (int i = 0; i < droppedItems.Count; i++)
        {
            ArchitecturalCrystal crystal = droppedItems[i];
            float angle = angleStep * i + Random.Range(-18f, 18f);
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            float distance = Random.Range(DropScatterMinDistance, DropScatterMaxDistance);
            Vector3 targetPosition = origin + (Vector3)(direction.normalized * distance);
            float startDelay = i * DropScatterDelayStep;
            float duration = DropScatterDuration + startDelay;

            GameObject dropObject = RuntimeCrystalDropFactory.CreateVisualDrop(
                crystal,
                origin,
                0.3f,
                8,
                transform,
                $"FailureDrop_{crystal.DisplayName}_{i}");

            StartCoroutine(AnimateVisualDrop(dropObject, origin, targetPosition, startDelay));
            maxDuration = Mathf.Max(maxDuration, duration);
        }

        return maxDuration;
    }

    private IEnumerator AnimateVisualDrop(GameObject dropObject, Vector3 startPosition, Vector3 targetPosition, float startDelay)
    {
        if (dropObject == null)
        {
            yield break;
        }

        if (startDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(startDelay);
        }

        SpriteRenderer renderer = dropObject.GetComponent<SpriteRenderer>();
        Transform dropTransform = dropObject.transform;
        Vector3 baseScale = dropTransform.localScale;
        float elapsed = 0f;

        while (elapsed < DropScatterDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / DropScatterDuration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);

            Vector3 position = Vector3.Lerp(startPosition, targetPosition, easedProgress);
            position.y += Mathf.Sin(easedProgress * Mathf.PI) * DropScatterArcHeight;
            dropTransform.position = position;
            dropTransform.localScale = baseScale * (1f + Mathf.Sin(easedProgress * Mathf.PI) * 0.12f);

            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = progress < 0.6f
                    ? 1f
                    : Mathf.Lerp(1f, 0f, (progress - 0.6f) / 0.4f);
                renderer.color = color;
            }

            yield return null;
        }

        Destroy(dropObject);
    }
}

public sealed class DeathSpotlightGraphic : MaskableGraphic
{
    [SerializeField] private Vector2 spotlightCenterNormalized = new Vector2(0.5f, 0.5f);
    [SerializeField] private float clearRadius = 200f;
    [SerializeField] private float featherRadius = 36f;
    [SerializeField] private int segments = 96;

    public void SetBaseColor(Color overlayColor)
    {
        color = overlayColor;
        SetVerticesDirty();
    }

    public void SetSpotlight(Vector2 normalizedCenter, float radius, float feather, float alpha)
    {
        spotlightCenterNormalized = new Vector2(
            Mathf.Clamp01(normalizedCenter.x),
            Mathf.Clamp01(normalizedCenter.y));
        clearRadius = Mathf.Max(1f, radius);
        featherRadius = Mathf.Max(1f, feather);

        Color overlayColor = color;
        overlayColor.a = Mathf.Clamp01(alpha);
        color = overlayColor;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        if (rect.width <= 0.001f || rect.height <= 0.001f || color.a <= 0.001f)
        {
            return;
        }

        Vector2 center = new Vector2(
            Mathf.Lerp(rect.xMin, rect.xMax, spotlightCenterNormalized.x),
            Mathf.Lerp(rect.yMin, rect.yMax, spotlightCenterNormalized.y));
        float outerRadius = clearRadius + featherRadius;
        int clampedSegments = Mathf.Clamp(segments, 24, 180);
        Color32 opaque = color;
        Color32 transparent = new Color(color.r, color.g, color.b, 0f);

        for (int i = 0; i < clampedSegments; i++)
        {
            float angle0 = i / (float)clampedSegments * Mathf.PI * 2f;
            float angle1 = (i + 1) / (float)clampedSegments * Mathf.PI * 2f;

            Vector2 direction0 = new Vector2(Mathf.Cos(angle0), Mathf.Sin(angle0));
            Vector2 direction1 = new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1));

            Vector2 boundary0 = ResolveRectBoundaryPoint(rect, center, direction0);
            Vector2 boundary1 = ResolveRectBoundaryPoint(rect, center, direction1);
            Vector2 outer0 = center + direction0 * outerRadius;
            Vector2 outer1 = center + direction1 * outerRadius;
            Vector2 inner0 = center + direction0 * clearRadius;
            Vector2 inner1 = center + direction1 * clearRadius;

            AddQuad(vh, boundary0, boundary1, outer1, outer0, opaque, opaque, opaque, opaque);
            AddQuad(vh, outer0, outer1, inner1, inner0, opaque, opaque, transparent, transparent);
        }
    }

    private static Vector2 ResolveRectBoundaryPoint(Rect rect, Vector2 center, Vector2 direction)
    {
        float tX = float.PositiveInfinity;
        float tY = float.PositiveInfinity;

        if (Mathf.Abs(direction.x) > 0.0001f)
        {
            float xEdge = direction.x > 0f ? rect.xMax : rect.xMin;
            tX = (xEdge - center.x) / direction.x;
        }

        if (Mathf.Abs(direction.y) > 0.0001f)
        {
            float yEdge = direction.y > 0f ? rect.yMax : rect.yMin;
            tY = (yEdge - center.y) / direction.y;
        }

        float t = Mathf.Min(Mathf.Abs(tX), Mathf.Abs(tY));
        return center + direction * t;
    }

    private static void AddQuad(
        VertexHelper vh,
        Vector2 bottomLeft,
        Vector2 topLeft,
        Vector2 topRight,
        Vector2 bottomRight,
        Color32 bottomLeftColor,
        Color32 topLeftColor,
        Color32 topRightColor,
        Color32 bottomRightColor)
    {
        int startIndex = vh.currentVertCount;

        vh.AddVert(bottomLeft, bottomLeftColor, Vector2.zero);
        vh.AddVert(topLeft, topLeftColor, Vector2.zero);
        vh.AddVert(topRight, topRightColor, Vector2.zero);
        vh.AddVert(bottomRight, bottomRightColor, Vector2.zero);

        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
    }
}
