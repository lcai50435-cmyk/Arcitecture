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
    private const string FailureOverlayGraphicName = "FailureSpotlightOverlay";
    private const int FailureOverlaySortingOrder = 12000;
    private const float SpotlightFadeInDuration = 0.18f;
    private const float SpotlightFocusDuration = 1.1f;
    private const float SpotlightHoldDuration = 0.3f;
    private const float SpotlightStartAlpha = 0.9f;
    private const float SpotlightEndAlpha = 1f;
    private static readonly Vector2 SpotlightStartClearRadii = new Vector2(260f, 180f);
    private static readonly Vector2 SpotlightEndClearRadii = new Vector2(44f, 64f);
    private static readonly Vector2 SpotlightStartFeatherRadii = new Vector2(96f, 84f);
    private static readonly Vector2 SpotlightEndFeatherRadii = new Vector2(28f, 24f);
    private static readonly Color FailureOverlayColor = Color.black;

    public static GameplayFailureController Instance { get; private set; }
    public static bool IsFailureActive => Instance != null && Instance.isFailureActive;

    private bool isFailureActive;
    private Canvas failureOverlayCanvas;
    private RectTransform failureOverlayRect;
    private StageIntroRevealGraphic failureRevealGraphic;

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
            failureRevealGraphic = null;
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
        if (failureRevealGraphic == null)
        {
            return 0f;
        }

        StartCoroutine(AnimateFailureSpotlight(focusWorldPosition));
        return SpotlightFadeInDuration + SpotlightFocusDuration + SpotlightHoldDuration;
    }

    private void EnsureFailureOverlay()
    {
        if (failureOverlayCanvas != null && failureRevealGraphic != null)
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

        GameObject revealObject = new GameObject(
            FailureOverlayGraphicName,
            typeof(RectTransform),
            typeof(StageIntroRevealGraphic));
        revealObject.transform.SetParent(failureOverlayRect, false);

        RectTransform revealRect = revealObject.GetComponent<RectTransform>();
        revealRect.anchorMin = Vector2.zero;
        revealRect.anchorMax = Vector2.one;
        revealRect.offsetMin = Vector2.zero;
        revealRect.offsetMax = Vector2.zero;
        revealRect.localScale = Vector3.one;

        failureRevealGraphic = revealObject.GetComponent<StageIntroRevealGraphic>();
        failureRevealGraphic.raycastTarget = false;
        failureRevealGraphic.SetBaseColor(FailureOverlayColor);
        failureRevealGraphic.SetReveal(SpotlightStartClearRadii, SpotlightStartFeatherRadii, 0f);
    }

    private IEnumerator AnimateFailureSpotlight(Vector3 focusWorldPosition)
    {
        if (failureRevealGraphic == null)
        {
            yield break;
        }

        Vector2 focusViewport = ResolveFocusViewport(focusWorldPosition);

        float elapsed = 0f;
        while (elapsed < SpotlightFadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, SpotlightFadeInDuration));
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            float alpha = Mathf.Lerp(0f, SpotlightStartAlpha, easedT);
            failureRevealGraphic.SetReveal(
                SpotlightStartClearRadii,
                SpotlightStartFeatherRadii,
                alpha,
                focusViewport);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < SpotlightFocusDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, SpotlightFocusDuration));
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            Vector2 clearRadii = Vector2.Lerp(SpotlightStartClearRadii, SpotlightEndClearRadii, easedT);
            Vector2 featherRadii = Vector2.Lerp(SpotlightStartFeatherRadii, SpotlightEndFeatherRadii, easedT);
            float alpha = Mathf.Lerp(SpotlightStartAlpha, SpotlightEndAlpha, easedT);
            failureRevealGraphic.SetReveal(clearRadii, featherRadii, alpha, focusViewport);
            yield return null;
        }

        failureRevealGraphic.SetReveal(
            SpotlightEndClearRadii,
            SpotlightEndFeatherRadii,
            SpotlightEndAlpha,
            focusViewport);

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
