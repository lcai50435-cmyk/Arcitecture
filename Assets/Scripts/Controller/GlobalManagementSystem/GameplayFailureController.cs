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
    private const int SpotlightTextureResolution = 384;
    private static readonly Color FailureOverlayColor = Color.black;

    public static GameplayFailureController Instance { get; private set; }
    public static bool IsFailureActive => Instance != null && Instance.isFailureActive;

    private bool isFailureActive;
    private Canvas failureOverlayCanvas;
    private RectTransform failureOverlayRect;
    private RawImage failureSpotlightImage;
    private Texture2D failureSpotlightTexture;
    private Color32[] failureSpotlightPixels;

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
            failureSpotlightImage = null;
        }

        if (failureSpotlightTexture != null)
        {
            Destroy(failureSpotlightTexture);
            failureSpotlightTexture = null;
            failureSpotlightPixels = null;
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
        if (failureSpotlightImage == null)
        {
            return 0f;
        }

        StartCoroutine(AnimateFailureSpotlight(focusWorldPosition));
        return SpotlightFadeInDuration + SpotlightFocusDuration + SpotlightHoldDuration;
    }

    private void EnsureFailureOverlay()
    {
        if (failureOverlayCanvas != null && failureSpotlightImage != null)
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
            typeof(CanvasRenderer),
            typeof(RawImage));
        spotlightObject.transform.SetParent(failureOverlayRect, false);

        RectTransform spotlightRect = spotlightObject.GetComponent<RectTransform>();
        spotlightRect.anchorMin = Vector2.zero;
        spotlightRect.anchorMax = Vector2.one;
        spotlightRect.offsetMin = Vector2.zero;
        spotlightRect.offsetMax = Vector2.zero;
        spotlightRect.localScale = Vector3.one;

        failureSpotlightImage = spotlightObject.GetComponent<RawImage>();
        failureSpotlightImage.raycastTarget = false;
        failureSpotlightImage.color = Color.white;

        EnsureSpotlightTexture();

        Canvas.ForceUpdateCanvases();
        SetSpotlightVisual(new Vector2(0.5f, 0.5f), SpotlightStartRadius, SpotlightFeatherRadius, 0f);
    }

    private IEnumerator AnimateFailureSpotlight(Vector3 focusWorldPosition)
    {
        if (failureSpotlightImage == null)
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
            SetSpotlightVisual(focusViewport, SpotlightStartRadius, SpotlightFeatherRadius, alpha);
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
            SetSpotlightVisual(focusViewport, radius, SpotlightFeatherRadius, alpha);
            yield return null;
        }

        SetSpotlightVisual(focusViewport, targetRadius, SpotlightFeatherRadius, SpotlightEndAlpha);

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

    private void EnsureSpotlightTexture()
    {
        if (failureSpotlightTexture != null && failureSpotlightPixels != null)
        {
            if (failureSpotlightImage != null)
            {
                failureSpotlightImage.texture = failureSpotlightTexture;
            }

            return;
        }

        failureSpotlightTexture = new Texture2D(
            SpotlightTextureResolution,
            SpotlightTextureResolution,
            TextureFormat.RGBA32,
            false);
        failureSpotlightTexture.name = "FailureSpotlightTexture";
        failureSpotlightTexture.filterMode = FilterMode.Bilinear;
        failureSpotlightTexture.wrapMode = TextureWrapMode.Clamp;
        failureSpotlightPixels = new Color32[SpotlightTextureResolution * SpotlightTextureResolution];

        if (failureSpotlightImage != null)
        {
            failureSpotlightImage.texture = failureSpotlightTexture;
        }
    }

    private void SetSpotlightVisual(Vector2 focusViewport, float radius, float featherRadius, float alpha)
    {
        if (failureSpotlightImage == null || failureOverlayRect == null)
        {
            return;
        }

        EnsureSpotlightTexture();
        if (failureSpotlightTexture == null || failureSpotlightPixels == null)
        {
            return;
        }

        Rect rect = failureOverlayRect.rect;
        float canvasWidth = rect.width > 1f ? rect.width : Screen.width;
        float canvasHeight = rect.height > 1f ? rect.height : Screen.height;
        float centerX = Mathf.Clamp01(focusViewport.x) * canvasWidth;
        float centerY = Mathf.Clamp01(focusViewport.y) * canvasHeight;
        float clampedRadius = Mathf.Max(1f, radius);
        float clampedFeather = Mathf.Max(1f, featherRadius);
        float overlayAlpha = Mathf.Clamp01(alpha);
        byte blackR = (byte)Mathf.RoundToInt(FailureOverlayColor.r * 255f);
        byte blackG = (byte)Mathf.RoundToInt(FailureOverlayColor.g * 255f);
        byte blackB = (byte)Mathf.RoundToInt(FailureOverlayColor.b * 255f);

        for (int y = 0; y < SpotlightTextureResolution; y++)
        {
            float sampleY = y / (float)(SpotlightTextureResolution - 1) * canvasHeight;
            for (int x = 0; x < SpotlightTextureResolution; x++)
            {
                float sampleX = x / (float)(SpotlightTextureResolution - 1) * canvasWidth;
                float distance = Vector2.Distance(new Vector2(sampleX, sampleY), new Vector2(centerX, centerY));
                float t = Mathf.InverseLerp(clampedRadius, clampedRadius + clampedFeather, distance);
                byte alphaByte = (byte)Mathf.RoundToInt(Mathf.Clamp01(t) * overlayAlpha * 255f);
                failureSpotlightPixels[y * SpotlightTextureResolution + x] = new Color32(
                    blackR,
                    blackG,
                    blackB,
                    alphaByte);
            }
        }

        failureSpotlightTexture.SetPixels32(failureSpotlightPixels);
        failureSpotlightTexture.Apply(false, false);
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
