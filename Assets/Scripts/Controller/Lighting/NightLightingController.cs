using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public sealed class SceneNightProfile
{
    public readonly string sceneName;
    public readonly bool useCountdownProgress;
    public readonly float fixedNightProgress;
    public readonly Color overlayTint;
    public readonly float overlayAlphaAtStart;
    public readonly float overlayAlphaAtEnd;
    public readonly Color cameraBackgroundNight;
    public readonly float localLightMultiplierAtStart;
    public readonly float localLightMultiplierAtEnd;
    public readonly Color shadowColor;
    public readonly Vector3 shadowOffset;
    public readonly Vector3 shadowScale;

    public SceneNightProfile(
        string sceneName,
        bool useCountdownProgress,
        float fixedNightProgress,
        Color overlayTint,
        float overlayAlphaAtStart,
        float overlayAlphaAtEnd,
        Color cameraBackgroundNight,
        float localLightMultiplierAtStart,
        float localLightMultiplierAtEnd,
        Color shadowColor,
        Vector3 shadowOffset,
        Vector3 shadowScale)
    {
        this.sceneName = sceneName;
        this.useCountdownProgress = useCountdownProgress;
        this.fixedNightProgress = Mathf.Clamp01(fixedNightProgress);
        this.overlayTint = overlayTint;
        this.overlayAlphaAtStart = Mathf.Clamp01(overlayAlphaAtStart);
        this.overlayAlphaAtEnd = Mathf.Clamp01(overlayAlphaAtEnd);
        this.cameraBackgroundNight = cameraBackgroundNight;
        this.localLightMultiplierAtStart = Mathf.Max(0f, localLightMultiplierAtStart);
        this.localLightMultiplierAtEnd = Mathf.Max(0f, localLightMultiplierAtEnd);
        this.shadowColor = shadowColor;
        this.shadowOffset = shadowOffset;
        this.shadowScale = shadowScale;
    }

    public float EvaluateOverlayAlpha(float progress)
    {
        return Mathf.Lerp(overlayAlphaAtStart, overlayAlphaAtEnd, Mathf.Clamp01(progress));
    }

    public Color EvaluateCameraBackground(Color baseColor, float progress)
    {
        return Color.Lerp(baseColor, cameraBackgroundNight, Mathf.Clamp01(progress));
    }

    public float EvaluateLightMultiplier(float progress)
    {
        return Mathf.Lerp(localLightMultiplierAtStart, localLightMultiplierAtEnd, Mathf.Clamp01(progress));
    }
}

public sealed class NightLightingController : MonoBehaviour
{
    private const string ControllerObjectName = "NightLightingController";
    private const string OverlayObjectName = "NightWorldOverlay";
    private const string AccentRootObjectName = "NightAccentRoot";
    private const string MainSceneName = "MainScene";
    private const string BaseSceneName = "NewBase";
    private const string FirstPassSceneName = "FirstPass_1";
    private const string DeadSceneName = "DeadScene";
    internal const int OverlaySortingOrder = 28000;
    internal const int ShadowSortingOrder = OverlaySortingOrder + 20;
    internal const int LocalLightSortingOrder = OverlaySortingOrder + 200;
    private const int InitialSceneBindingPassCount = 3;
    private const float InitialSceneBindingInterval = 0.25f;
    private const string ProfileResourcePath = "Lighting/NightLightingProfiles";

    private static readonly Color GameplayLightColor = new Color(0.96f, 0.86f, 0.62f, 1f);
    private static readonly Color GameplayEnemyLightColor = new Color(1f, 0.42f, 0.26f, 1f);
    private static readonly Color GameplayFireballLightColor = new Color(1f, 0.56f, 0.28f, 1f);
    private static readonly Color BaseWarmLightColor = new Color(1f, 0.81f, 0.56f, 1f);
    private static readonly Color MainSceneLightColor = new Color(0.80f, 0.90f, 1f, 1f);
    private static readonly Color DeadSceneLightColor = new Color(0.54f, 0.72f, 1f, 1f);
    private static readonly Vector3 GameplayPlayerLightOffset = new Vector3(0f, 0.14f, 0f);
    private static readonly Vector3 GameplayEnemyLightOffset = new Vector3(0f, 0.12f, 0f);
    private static readonly Color ReadableOverlayTint = new Color(0.006f, 0.012f, 0.026f, 1f);
    private static readonly Color ReadableCameraBackgroundNight = new Color(0.004f, 0.008f, 0.018f, 1f);
    private static readonly Color ReadableCharacterShadowColor = new Color(0.004f, 0.005f, 0.008f, 0.58f);
    private static readonly Vector3 ReadableCharacterShadowOffset = new Vector3(0.09f, -0.15f, 0f);
    private static readonly Vector3 ReadableCharacterShadowScale = new Vector3(1.02f, 0.36f, 1f);

    private static readonly Dictionary<string, SceneNightProfile> Profiles = new Dictionary<string, SceneNightProfile>(StringComparer.Ordinal)
    {
        {
            MainSceneName,
            CreateReadableSceneProfile(MainSceneName, false, 0.52f, 0.18f, 0.36f, 0.82f, 1.00f)
        },
        {
            BaseSceneName,
            CreateReadableSceneProfile(BaseSceneName, false, 0.55f, 0.20f, 0.40f, 0.84f, 1.00f)
        },
        {
            FirstPassSceneName,
            CreateReadableSceneProfile(FirstPassSceneName, true, 0f, 0.16f, 0.58f, 0.82f, 1.02f)
        },
        {
            "GameScene",
            CreateReadableSceneProfile("GameScene", true, 0f, 0.16f, 0.58f, 0.82f, 1.02f)
        },
        {
            "GameScene_02",
            CreateReadableSceneProfile("GameScene_02", true, 0f, 0.16f, 0.58f, 0.82f, 1.02f)
        },
        {
            "GameScene_03",
            CreateReadableSceneProfile("GameScene_03", true, 0f, 0.16f, 0.58f, 0.82f, 1.02f)
        },
        {
            "SecondPassSence",
            CreateReadableSceneProfile("SecondPassSence", true, 0f, 0.16f, 0.58f, 0.82f, 1.02f)
        },
        {
            DeadSceneName,
            new SceneNightProfile(
                DeadSceneName,
                false,
                0.82f,
                new Color(0.004f, 0.008f, 0.018f, 1f),
                0.32f,
                0.70f,
                new Color(0.002f, 0.004f, 0.012f, 1f),
                0.82f,
                1.02f,
                ReadableCharacterShadowColor,
                ReadableCharacterShadowOffset,
                ReadableCharacterShadowScale)
        }
    };

    private static NightLightingController instance;

    private readonly List<ViewportLightBinding> viewportLights = new List<ViewportLightBinding>();

    private SpriteRenderer overlayRenderer;
    private Transform accentRoot;
    private Camera currentCamera;
    private GameCountDownManager boundCountdown;
    private SceneNightProfile currentProfile;
    private string currentSceneName;
    private float currentNightProgress;
    private float bindingRefreshTimer;
    private int pendingSceneBindingPasses;
    private bool hasCapturedCameraBaseColor;
    private Color baseCameraBackgroundColor = Color.black;

    public static float ActiveNightProgress => instance != null ? instance.currentNightProgress : 0f;

    public static float ActiveLightMultiplier
    {
        get
        {
            if (instance == null || instance.currentProfile == null)
            {
                return 1f;
            }

            return instance.currentProfile.EvaluateLightMultiplier(instance.currentNightProgress);
        }
    }

    public static bool HasActiveProfile => instance != null && instance.currentProfile != null;

    private static SceneNightProfile CreateReadableSceneProfile(
        string sceneName,
        bool useCountdownProgress,
        float fixedNightProgress,
        float overlayAlphaAtStart,
        float overlayAlphaAtEnd,
        float localLightMultiplierAtStart,
        float localLightMultiplierAtEnd)
    {
        return new SceneNightProfile(
            sceneName,
            useCountdownProgress,
            fixedNightProgress,
            ReadableOverlayTint,
            overlayAlphaAtStart,
            overlayAlphaAtEnd,
            ReadableCameraBackgroundNight,
            localLightMultiplierAtStart,
            localLightMultiplierAtEnd,
            ReadableCharacterShadowColor,
            ReadableCharacterShadowOffset,
            ReadableCharacterShadowScale);
    }

    public static int ExcludeEffectLayerFromMask(int cullingMask)
    {
        return cullingMask & ~NightLightingLayers.VisualLayerMask;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        NightLightingController controller = EnsureInstance();
        if (controller != null)
        {
            controller.ApplySceneProfile(SceneManager.GetActiveScene().name);
        }
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        NightLightingController controller = EnsureInstance();
        if (controller != null)
        {
            controller.ApplySceneProfile(scene.name);
        }
    }

    private static NightLightingController EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        NightLightingController existing = FindObjectOfType<NightLightingController>(true);
        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        GameObject controllerObject = new GameObject(ControllerObjectName);
        instance = controllerObject.AddComponent<NightLightingController>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureOverlayRenderer();
    }

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(currentSceneName))
        {
            ApplySceneProfile(SceneManager.GetActiveScene().name);
        }
    }

    private void LateUpdate()
    {
        if (currentProfile == null)
        {
            SetOverlayVisible(false);
            return;
        }

        ResolveCameraIfNeeded();
        if (currentCamera == null)
        {
            return;
        }

        EnsureCameraIncludesEffectLayer(currentCamera);
        CaptureBaseCameraBackgroundIfNeeded();
        UpdateOverlayVisual();
        UpdateViewportLights();

        if (pendingSceneBindingPasses > 0)
        {
            bindingRefreshTimer -= Time.unscaledDeltaTime;
            if (bindingRefreshTimer <= 0f)
            {
                bindingRefreshTimer = InitialSceneBindingInterval;
                pendingSceneBindingPasses--;
                RefreshSceneBindings();
            }
        }

        if (currentProfile.useCountdownProgress)
        {
            TryBindCountdownManager();
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        UnbindCountdownManager();
    }

    public void ApplySceneProfile(string sceneName)
    {
        currentSceneName = sceneName;
        currentProfile = ResolveProfile(sceneName);
        currentCamera = null;
        hasCapturedCameraBaseColor = false;
        bindingRefreshTimer = InitialSceneBindingInterval;
        pendingSceneBindingPasses = InitialSceneBindingPassCount;

        ClearViewportLights();
        UnbindCountdownManager();

        if (currentProfile == null)
        {
            SetOverlayVisible(false);
            return;
        }

        EnsureOverlayRenderer();
        SetOverlayVisible(true);
        CleanupPersistedLightingArtifacts();
        ConfigureViewportLightsForScene(sceneName);

        if (currentProfile.useCountdownProgress)
        {
            currentNightProgress = 0f;
            TryBindCountdownManager();
        }
        else
        {
            currentNightProgress = currentProfile.fixedNightProgress;
        }

        UpdateOverlayVisual();
        RefreshSceneBindings();
    }

    private void CleanupPersistedLightingArtifacts()
    {
        Transform[] transforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null || candidate == transform || !candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            if (!string.Equals(candidate.gameObject.scene.name, currentSceneName, StringComparison.Ordinal))
            {
                continue;
            }

            if (ShouldDestroyPersistedLightingArtifact(candidate))
            {
                DestroyLightingArtifact(candidate.gameObject);
            }
        }
    }

    private static bool ShouldDestroyPersistedLightingArtifact(Transform candidate)
    {
        string objectName = candidate.name;
        if (string.Equals(objectName, ProjectedShadowFollower.ShadowObjectName, StringComparison.Ordinal))
        {
            return candidate.GetComponentInParent<ProjectedShadowFollower>() == null;
        }

        if (string.Equals(objectName, NightLocalLightSource.VisualObjectName, StringComparison.Ordinal))
        {
            return candidate.GetComponentInParent<NightLocalLightSource>() == null;
        }

        if (string.Equals(objectName, OverlayObjectName, StringComparison.Ordinal) ||
            string.Equals(objectName, AccentRootObjectName, StringComparison.Ordinal))
        {
            return candidate.GetComponentInParent<NightLightingController>() == null;
        }

        return false;
    }

    private static void DestroyLightingArtifact(GameObject artifact)
    {
        if (artifact == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(artifact);
        }
        else
        {
            DestroyImmediate(artifact);
        }
    }

    public void BindCountdown(GameCountDownManager manager)
    {
        if (manager == null || currentProfile == null || !currentProfile.useCountdownProgress)
        {
            return;
        }

        if (boundCountdown == manager)
        {
            SetNightProgress(CalculateCountdownProgress(manager));
            return;
        }

        UnbindCountdownManager();
        boundCountdown = manager;
        boundCountdown.OnRemainingTimeChanged += HandleCountdownTimeChanged;
        SetNightProgress(CalculateCountdownProgress(boundCountdown));
    }

    public void SetNightProgress(float progress)
    {
        currentNightProgress = Mathf.Clamp01(progress);
        UpdateOverlayVisual();
    }

    public static NightLocalLightSource EnsureLocalLight(
        GameObject target,
        float radius = 2.4f,
        float baseIntensity = 0.20f,
        float nightBoost = 0.14f,
        Vector3? localOffset = null,
        Color? lightColor = null,
        bool scaleWithSceneLightMultiplier = true,
        NightLightSortingMode sortingMode = NightLightSortingMode.RelativeToSource,
        int sourceSortingOrderOffset = -1)
    {
        if (target == null)
        {
            return null;
        }

        NightLocalLightSource lightSource = target.GetComponent<NightLocalLightSource>();
        if (lightSource == null)
        {
            lightSource = target.AddComponent<NightLocalLightSource>();
        }

        lightSource.Configure(
            lightColor ?? GameplayLightColor,
            Mathf.Max(0.1f, radius),
            Mathf.Max(0f, baseIntensity),
            Mathf.Max(0f, nightBoost),
            localOffset ?? new Vector3(0f, 0.18f, 0f),
            scaleWithSceneLightMultiplier,
            sortingMode,
            sourceSortingOrderOffset);
        return lightSource;
    }

    public static NightLocalLightSource EnsureGameplayPlayerLight(GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        return EnsureLocalLight(
            target,
            1.65f,
            0.045f,
            0.035f,
            GameplayPlayerLightOffset,
            ResolveGameplayPlayerLightColor(target));
    }

    public static NightLocalLightSource EnsureGameplayEnemyLight(GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        return EnsureLocalLight(
            target,
            1.35f,
            0.035f,
            0.025f,
            GameplayEnemyLightOffset,
            GameplayEnemyLightColor);
    }

    public static NightLocalLightSource EnsureTransientFxLight(
        GameObject target,
        float radius,
        float baseIntensity,
        Color lightColor,
        Vector3? localOffset = null)
    {
        if (target == null)
        {
            return null;
        }

        return EnsureLocalLight(
            target,
            radius,
            baseIntensity,
            0f,
            localOffset ?? Vector3.zero,
            lightColor,
            false);
    }

    public static void RemoveLocalLight(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        NightLocalLightSource lightSource = target.GetComponent<NightLocalLightSource>();
        if (lightSource != null)
        {
            Destroy(lightSource);
        }

        Transform lightVisual = target.transform.Find(NightLocalLightSource.VisualObjectName);
        if (lightVisual != null)
        {
            Destroy(lightVisual.gameObject);
        }
    }

    public static ProjectedShadowFollower EnsureProjectedShadow(
        GameObject target,
        Vector3? localOffset = null,
        Vector3? scaleMultiplier = null,
        Color? shadowColor = null,
        int sortingOrderOffset = -1)
    {
        if (target == null)
        {
            return null;
        }

        SpriteRenderer renderer = ResolvePrimarySpriteRenderer(target);
        if (renderer == null)
        {
            return null;
        }

        ProjectedShadowFollower follower = target.GetComponent<ProjectedShadowFollower>();
        if (follower == null)
        {
            follower = target.AddComponent<ProjectedShadowFollower>();
        }

        Vector3 resolvedOffset = localOffset ?? GetDefaultShadowOffset();
        Vector3 resolvedScale = scaleMultiplier ?? GetDefaultShadowScale();
        Color resolvedColor = shadowColor ?? GetDefaultShadowColor();

        follower.Configure(renderer, resolvedOffset, resolvedScale, resolvedColor, sortingOrderOffset);
        return follower;
    }

    private void ResolveCameraIfNeeded()
    {
        if (currentCamera != null)
        {
            return;
        }

        currentCamera = Camera.main;
        if (currentCamera != null)
        {
            return;
        }

        Camera[] cameras = FindObjectsOfType<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera candidate = cameras[i];
            if (candidate == null || !candidate.isActiveAndEnabled)
            {
                continue;
            }

            currentCamera = candidate;
            return;
        }
    }

    private void CaptureBaseCameraBackgroundIfNeeded()
    {
        if (hasCapturedCameraBaseColor || currentCamera == null)
        {
            return;
        }

        baseCameraBackgroundColor = currentCamera.backgroundColor;
        hasCapturedCameraBaseColor = true;
    }

    private void EnsureOverlayRenderer()
    {
        if (overlayRenderer != null)
        {
            return;
        }

        Transform overlayTransform = transform.Find(OverlayObjectName);
        GameObject overlayObject = overlayTransform != null ? overlayTransform.gameObject : new GameObject(OverlayObjectName);
        overlayObject.transform.SetParent(transform, false);
        overlayObject.layer = NightLightingLayers.VisualLayer;
        overlayObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

        overlayRenderer = overlayObject.GetComponent<SpriteRenderer>();
        if (overlayRenderer == null)
        {
            overlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
        }

        overlayRenderer.sprite = NightLightingVisualFactory.GetUnitSprite();
        overlayRenderer.sortingLayerID = NightLightingVisualFactory.GetTopSortingLayerId();
        overlayRenderer.sortingOrder = OverlaySortingOrder;
    }

    private void UpdateOverlayVisual()
    {
        if (overlayRenderer == null || currentProfile == null || currentCamera == null)
        {
            return;
        }

        overlayRenderer.enabled = true;
        overlayRenderer.transform.position = new Vector3(
            currentCamera.transform.position.x,
            currentCamera.transform.position.y,
            0f);
        overlayRenderer.transform.localScale = new Vector3(
            GetCameraWorldWidth(currentCamera),
            GetCameraWorldHeight(currentCamera),
            1f);

        Color overlayColor = currentProfile.overlayTint;
        overlayColor.a = currentProfile.EvaluateOverlayAlpha(currentNightProgress);
        overlayRenderer.color = overlayColor;
        overlayRenderer.sortingLayerID = NightLightingVisualFactory.GetTopSortingLayerId();
        overlayRenderer.sortingOrder = OverlaySortingOrder;

        currentCamera.backgroundColor = currentProfile.EvaluateCameraBackground(baseCameraBackgroundColor, currentNightProgress);
    }

    private void UpdateViewportLights()
    {
        if (currentCamera == null || viewportLights.Count == 0)
        {
            return;
        }

        float depth = Mathf.Abs(currentCamera.transform.position.z);
        for (int i = 0; i < viewportLights.Count; i++)
        {
            ViewportLightBinding binding = viewportLights[i];
            if (binding == null || binding.anchor == null)
            {
                continue;
            }

            Vector3 worldPoint = currentCamera.ViewportToWorldPoint(new Vector3(binding.viewportPosition.x, binding.viewportPosition.y, depth));
            worldPoint.z = 0f;
            binding.anchor.position = worldPoint + binding.worldOffset;
        }
    }

    private void RefreshSceneBindings()
    {
        if (currentProfile == null)
        {
            return;
        }

        RefreshAnchorBindings();

        if (GameplayStageCatalog.IsGameplayScene(currentSceneName))
        {
            RefreshGameplayBindings();
            return;
        }

        if (string.Equals(currentSceneName, BaseSceneName, StringComparison.Ordinal))
        {
            RefreshBaseSceneBindings();
            return;
        }

        if (string.Equals(currentSceneName, MainSceneName, StringComparison.Ordinal))
        {
            RefreshMainSceneBindings();
        }
    }

    private void RefreshAnchorBindings()
    {
        NightLightingAnchor[] anchors = FindObjectsOfType<NightLightingAnchor>(true);
        for (int i = 0; i < anchors.Length; i++)
        {
            NightLightingAnchor anchor = anchors[i];
            if (anchor == null || !anchor.gameObject.scene.IsValid())
            {
                continue;
            }

            if (!string.Equals(anchor.gameObject.scene.name, currentSceneName, StringComparison.Ordinal))
            {
                continue;
            }

            anchor.ApplyLighting();
        }
    }

    private void RefreshGameplayBindings()
    {
        GameObject player = FindByTagSafe("Player");
        if (player != null)
        {
            EnsureProjectedShadow(player);
            EnsureGameplayPlayerLight(player);
        }

        GameObject catalogue = FindByTagSafe("Catalogue");
        if (catalogue != null)
        {
            EnsureLocalLight(catalogue, 2.0f, 0.10f, 0.10f, new Vector3(0f, 0.30f, 0f), BaseWarmLightColor);
        }

        EnemyStatsManager[] enemies = FindObjectsOfType<EnemyStatsManager>(true);
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyStatsManager enemy = enemies[i];
            if (enemy == null)
            {
                continue;
            }

            EnsureProjectedShadow(enemy.gameObject);
            EnsureGameplayEnemyLight(enemy.gameObject);
        }
    }

    private void RefreshBaseSceneBindings()
    {
        GameObject player = FindByTagSafe("Player");
        if (player != null)
        {
            EnsureProjectedShadow(player);
            RemoveLocalLight(player);
        }

        TryAttachNamedLight("BookInteractable", 1.4f, 0.06f, 0.02f, new Vector3(0f, 0.32f, 0f), BaseWarmLightColor, false, false);
        TryAttachNamedLight("SpiritInteractable", 1.4f, 0.06f, 0.02f, new Vector3(0f, 0.32f, 0f), new Color(0.74f, 0.89f, 1f, 1f), false, false);
        TryAttachNamedLight("AlbumInteractable", 1.3f, 0.08f, 0.03f, new Vector3(0f, 0.28f, 0f), new Color(1f, 0.82f, 0.62f, 1f), false, true);
        TryAttachNamedLight("GameSceneGateInteractable", 1.7f, 0.08f, 0.03f, new Vector3(0f, 0.36f, 0f), BaseWarmLightColor, false, false);
        TryAttachNamedLight("TrainingDummy_Left", 1.2f, 0.04f, 0.02f, new Vector3(0f, 0.28f, 0f), new Color(1f, 0.88f, 0.72f, 1f), false, true);
        TryAttachNamedLight("TrainingDummy_Right", 1.2f, 0.04f, 0.02f, new Vector3(0f, 0.28f, 0f), new Color(1f, 0.88f, 0.72f, 1f), false, true);
    }

    private void RefreshMainSceneBindings()
    {
        TryAttachNamedLight("GameTitle", 3.6f, 0.10f, 0.04f, new Vector3(0f, 0f, 0f), MainSceneLightColor, false, true, NightLightSortingMode.AccentOverlay);
        TryAttachNamedLight("Title", 2.8f, 0.08f, 0.04f, new Vector3(0f, 0f, 0f), MainSceneLightColor, false, true, NightLightSortingMode.AccentOverlay);
        TryAttachNamedLightContains("HomeButton", 2.4f, 0.08f, 0.04f, new Vector3(0f, 0f, 0f), new Color(0.92f, 0.96f, 1f, 1f), NightLightSortingMode.AccentOverlay);
    }

    private void TryAttachNamedLight(
        string objectName,
        float radius,
        float baseIntensity,
        float nightBoost,
        Vector3 localOffset,
        Color lightColor,
        bool withShadow = false,
        bool allowAnchorOnly = true,
        NightLightSortingMode sortingMode = NightLightSortingMode.RelativeToSource)
    {
        GameObject target = FindSceneObjectByName(objectName);
        if (target == null)
        {
            return;
        }

        if (!allowAnchorOnly && ResolvePrimarySpriteRenderer(target) == null)
        {
            return;
        }

        EnsureLocalLight(target, radius, baseIntensity, nightBoost, localOffset, lightColor, true, sortingMode);
        if (withShadow)
        {
            EnsureProjectedShadow(target);
        }
    }

    private void TryAttachNamedLightContains(
        string objectNameFragment,
        float radius,
        float baseIntensity,
        float nightBoost,
        Vector3 localOffset,
        Color lightColor,
        NightLightSortingMode sortingMode = NightLightSortingMode.RelativeToSource)
    {
        GameObject target = FindSceneObjectByContains(objectNameFragment);
        if (target == null)
        {
            return;
        }

        EnsureLocalLight(target, radius, baseIntensity, nightBoost, localOffset, lightColor, true, sortingMode);
    }

    private void ConfigureViewportLightsForScene(string sceneName)
    {
        if (GameplayStageCatalog.IsGameplayScene(sceneName))
        {
            return;
        }

        if (string.Equals(sceneName, MainSceneName, StringComparison.Ordinal))
        {
            AddViewportLight("MainSceneTitleGlow", new Vector2(0.50f, 0.72f), new Vector3(0f, 0.2f, 0f), 4.8f, 0.08f, 0.05f, MainSceneLightColor);
            AddViewportLight("MainSceneActionGlow", new Vector2(0.50f, 0.38f), Vector3.zero, 3.2f, 0.06f, 0.06f, new Color(0.88f, 0.94f, 1f, 1f));
            return;
        }

        if (string.Equals(sceneName, DeadSceneName, StringComparison.Ordinal))
        {
            AddViewportLight("DeadSceneTopGlow", new Vector2(0.50f, 0.64f), Vector3.zero, 3.8f, 0.12f, 0.06f, DeadSceneLightColor);
            AddViewportLight("DeadSceneActionGlow", new Vector2(0.50f, 0.42f), Vector3.zero, 2.8f, 0.10f, 0.05f, DeadSceneLightColor);
        }
    }

    private void AddViewportLight(
        string lightName,
        Vector2 viewportPosition,
        Vector3 worldOffset,
        float radius,
        float baseIntensity,
        float nightBoost,
        Color lightColor)
    {
        EnsureAccentRoot();

        GameObject anchorObject = new GameObject(lightName);
        anchorObject.transform.SetParent(accentRoot, false);
        anchorObject.layer = NightLightingLayers.VisualLayer;
        anchorObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

        NightLocalLightSource lightSource = EnsureLocalLight(
            anchorObject,
            radius,
            baseIntensity,
            nightBoost,
            Vector3.zero,
            lightColor,
            false,
            NightLightSortingMode.AccentOverlay);
        viewportLights.Add(new ViewportLightBinding(anchorObject.transform, lightSource, viewportPosition, worldOffset));
    }

    private void EnsureAccentRoot()
    {
        if (accentRoot != null)
        {
            return;
        }

        Transform accentTransform = transform.Find(AccentRootObjectName);
        if (accentTransform != null)
        {
            accentRoot = accentTransform;
            return;
        }

        GameObject accentObject = new GameObject(AccentRootObjectName);
        accentObject.transform.SetParent(transform, false);
        accentObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        accentRoot = accentObject.transform;
    }

    private void ClearViewportLights()
    {
        for (int i = 0; i < viewportLights.Count; i++)
        {
            ViewportLightBinding binding = viewportLights[i];
            if (binding != null && binding.anchor != null)
            {
                Destroy(binding.anchor.gameObject);
            }
        }

        viewportLights.Clear();

        if (accentRoot != null)
        {
            for (int i = accentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(accentRoot.GetChild(i).gameObject);
            }
        }
    }

    private void TryBindCountdownManager()
    {
        if (currentProfile == null || !currentProfile.useCountdownProgress)
        {
            return;
        }

        if (boundCountdown != null)
        {
            if (boundCountdown.gameObject != null &&
                boundCountdown.gameObject.scene.IsValid() &&
                string.Equals(boundCountdown.gameObject.scene.name, currentSceneName, StringComparison.Ordinal))
            {
                return;
            }

            UnbindCountdownManager();
        }

        GameCountDownManager manager = GameCountDownManager.Instance != null
            ? GameCountDownManager.Instance
            : FindObjectOfType<GameCountDownManager>();

        if (manager != null)
        {
            BindCountdown(manager);
        }
    }

    private void UnbindCountdownManager()
    {
        if (boundCountdown != null)
        {
            boundCountdown.OnRemainingTimeChanged -= HandleCountdownTimeChanged;
            boundCountdown = null;
        }
    }

    private void HandleCountdownTimeChanged(float remainingTime)
    {
        if (boundCountdown == null)
        {
            return;
        }

        SetNightProgress(CalculateCountdownProgress(boundCountdown));
    }

    private static float CalculateCountdownProgress(GameCountDownManager manager)
    {
        if (manager == null)
        {
            return 0f;
        }

        float total = Mathf.Max(0.01f, manager.totalTime);
        return 1f - Mathf.Clamp01(manager.GetRemainTime() / total);
    }

    private static SceneNightProfile ResolveProfile(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return null;
        }

        SceneNightProfile assetProfile = ResolveProfileAsset(sceneName);
        if (assetProfile != null)
        {
            return assetProfile;
        }

        Profiles.TryGetValue(sceneName, out SceneNightProfile profile);
        return profile;
    }

    private static SceneNightProfile ResolveProfileAsset(string sceneName)
    {
        NightLightingProfileAsset[] assets = Resources.LoadAll<NightLightingProfileAsset>(ProfileResourcePath);
        for (int i = 0; i < assets.Length; i++)
        {
            NightLightingProfileAsset asset = assets[i];
            if (asset == null)
            {
                continue;
            }

            if (string.Equals(asset.TargetSceneName, sceneName, StringComparison.Ordinal))
            {
                return asset.CreateProfile(sceneName);
            }
        }

        return null;
    }

    private static void EnsureCameraIncludesEffectLayer(Camera camera)
    {
        if (camera == null)
        {
            return;
        }

        int requiredMask = camera.cullingMask | NightLightingLayers.VisualLayerMask;
        if (requiredMask != camera.cullingMask)
        {
            camera.cullingMask = requiredMask;
        }
    }

    public static SpriteRenderer ResolvePrimarySpriteRenderer(GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        SpriteRenderer directRenderer = target.GetComponent<SpriteRenderer>();
        if (directRenderer != null && !IsGeneratedLightingRenderer(directRenderer))
        {
            return directRenderer;
        }

        SpriteRenderer[] childRenderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < childRenderers.Length; i++)
        {
            SpriteRenderer candidate = childRenderers[i];
            if (candidate == null)
            {
                continue;
            }

            if (IsGeneratedLightingRenderer(candidate))
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private static bool IsGeneratedLightingRenderer(SpriteRenderer candidate)
    {
        if (candidate == null)
        {
            return true;
        }

        string objectName = candidate.gameObject.name;
        return string.Equals(objectName, ProjectedShadowFollower.ShadowObjectName, StringComparison.Ordinal) ||
               string.Equals(objectName, NightLocalLightSource.VisualObjectName, StringComparison.Ordinal) ||
               string.Equals(objectName, RuntimeWaterReflectionCaster.ShadowObjectName, StringComparison.Ordinal) ||
               string.Equals(objectName, RuntimeWaterReflectionCaster.LegacyReflectionObjectName, StringComparison.Ordinal) ||
               string.Equals(objectName, OverlayObjectName, StringComparison.Ordinal);
    }

    private static GameObject FindByTagSafe(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return null;
        }

        try
        {
            return GameObject.FindGameObjectWithTag(tag);
        }
        catch
        {
            return null;
        }
    }

    private static Color ResolveGameplayPlayerLightColor(GameObject playerObject)
    {
        WeaponType effectiveWeaponType = PlayerLoadoutRuntime.CurrentWeaponType;

        if (playerObject != null)
        {
            PlayerProfileData profileData = playerObject.GetComponent<PlayerProfileData>();
            if (profileData != null)
            {
                effectiveWeaponType = profileData.effectiveWeaponType;
            }
        }

        if (BackpackMananger.Instance != null)
        {
            effectiveWeaponType = RuntimeWeaponTypeResolver.ResolveEffectiveWeaponType(
                BackpackMananger.Instance,
                effectiveWeaponType);
        }

        return InkTypeCatalog.GetDisplayColor(effectiveWeaponType);
    }

    public static Color GetGameplayFireballLightColor()
    {
        return GameplayFireballLightColor;
    }

    private static GameObject FindSceneObjectByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        GameObject exact = GameObject.Find(name);
        if (exact != null)
        {
            return exact;
        }

        Transform[] transforms = FindObjectsOfType<Transform>(true);
        string trimmedName = name.Trim();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null)
            {
                continue;
            }

            if (string.Equals(candidate.name.Trim(), trimmedName, StringComparison.OrdinalIgnoreCase))
            {
                return candidate.gameObject;
            }
        }

        return null;
    }

    private static GameObject FindSceneObjectByContains(string fragment)
    {
        if (string.IsNullOrWhiteSpace(fragment))
        {
            return null;
        }

        Transform[] transforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null)
            {
                continue;
            }

            if (candidate.name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return candidate.gameObject;
            }
        }

        return null;
    }

    private static float GetCameraWorldHeight(Camera camera)
    {
        if (camera == null)
        {
            return 0f;
        }

        if (camera.orthographic)
        {
            return camera.orthographicSize * 2f;
        }

        return 10f;
    }

    private static float GetCameraWorldWidth(Camera camera)
    {
        if (camera == null)
        {
            return 0f;
        }

        return GetCameraWorldHeight(camera) * Mathf.Max(0.01f, camera.aspect);
    }

    private void SetOverlayVisible(bool visible)
    {
        if (overlayRenderer != null)
        {
            overlayRenderer.enabled = visible;
        }
    }

    private static Color GetDefaultShadowColor()
    {
        if (instance != null && instance.currentProfile != null)
        {
            return instance.currentProfile.shadowColor;
        }

        return new Color(0.04f, 0.05f, 0.08f, 0.42f);
    }

    private static Vector3 GetDefaultShadowOffset()
    {
        if (instance != null && instance.currentProfile != null)
        {
            return instance.currentProfile.shadowOffset;
        }

        return new Vector3(0.18f, -0.28f, 0f);
    }

    private static Vector3 GetDefaultShadowScale()
    {
        if (instance != null && instance.currentProfile != null)
        {
            return instance.currentProfile.shadowScale;
        }

        return new Vector3(1.10f, 0.42f, 1f);
    }

    private sealed class ViewportLightBinding
    {
        public readonly Transform anchor;
        public readonly NightLocalLightSource lightSource;
        public readonly Vector2 viewportPosition;
        public readonly Vector3 worldOffset;

        public ViewportLightBinding(
            Transform anchor,
            NightLocalLightSource lightSource,
            Vector2 viewportPosition,
            Vector3 worldOffset)
        {
            this.anchor = anchor;
            this.lightSource = lightSource;
            this.viewportPosition = viewportPosition;
            this.worldOffset = worldOffset;
        }
    }
}

internal static class NightLightingLayers
{
    private const int FallbackVisualLayer = 2;
    private static int cachedVisualLayer = -1;

    public static int VisualLayer
    {
        get
        {
            if (cachedVisualLayer >= 0)
            {
                return cachedVisualLayer;
            }

            int resolved = LayerMask.NameToLayer("Ignore Raycast");
            cachedVisualLayer = resolved >= 0 ? resolved : FallbackVisualLayer;
            return cachedVisualLayer;
        }
    }

    public static int VisualLayerMask => 1 << VisualLayer;
}

internal static class NightLightingVisualFactory
{
    private static Sprite unitSprite;
    private static Sprite radialGlowSprite;
    private static Material additiveGlowMaterial;

    public static Sprite GetUnitSprite()
    {
        if (unitSprite != null)
        {
            return unitSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = "NightLightingUnitTexture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        unitSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        unitSprite.name = "NightLightingUnitSprite";
        return unitSprite;
    }

    public static Sprite GetRadialGlowSprite()
    {
        if (radialGlowSprite != null)
        {
            return radialGlowSprite;
        }

        const int textureSize = 128;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            name = "NightLightingRadialGlowTexture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Vector2 center = new Vector2(textureSize * 0.5f, textureSize * 0.5f);
        float maxDistance = textureSize * 0.5f;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float t = Mathf.Clamp01(distance / maxDistance);
                float alpha = Mathf.Pow(1f - t, 2.2f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();

        radialGlowSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            textureSize);
        radialGlowSprite.name = "NightLightingRadialGlowSprite";
        return radialGlowSprite;
    }

    public static Material GetAdditiveGlowMaterial()
    {
        if (additiveGlowMaterial != null)
        {
            return additiveGlowMaterial;
        }

        Shader shader = Shader.Find("Arcitecture/NightLightingAdditive");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Additive");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            return null;
        }

        additiveGlowMaterial = new Material(shader)
        {
            name = "RuntimeNightLightingAdditiveGlow",
            hideFlags = HideFlags.HideAndDontSave
        };
        return additiveGlowMaterial;
    }

    public static int GetTopSortingLayerId()
    {
        SortingLayer[] layers = SortingLayer.layers;
        if (layers == null || layers.Length == 0)
        {
            return 0;
        }

        return layers[layers.Length - 1].id;
    }
}
