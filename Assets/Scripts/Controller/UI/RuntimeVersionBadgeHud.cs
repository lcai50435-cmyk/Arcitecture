using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class RuntimeVersionBadgeHud : MonoBehaviour
{
    private const string MainSceneName = "MainScene";
    private const string BaseSceneName = "NewBase";
    private const string CanvasName = "RuntimeVersionBadgeCanvas";
    private const int SortingOrder = 360;
    private const float EdgeMargin = 2f;
    private const float MainSceneBottomOffset = 2f;
    private const float BaseAndGameplayBottomOffset = 2f;
    private const float RightInsetRatio = 0.10f;

    private static readonly Color TextColor = new Color(0.92f, 0.94f, 0.97f, 0.96f);

    private static RuntimeVersionBadgeHud instance;

    private Canvas canvas;
    private RectTransform textRect;
    private TextMeshProUGUI versionText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureInstance();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RuntimeVersionBadgeHud hud = EnsureInstance();
        if (hud != null)
        {
            hud.RefreshForScene(scene.name);
        }
    }

    public static RuntimeVersionBadgeHud EnsureInstance()
    {
        if (instance != null)
        {
            instance.RefreshForScene(SceneManager.GetActiveScene().name);
            return instance;
        }

        RuntimeVersionBadgeHud existing = FindObjectOfType<RuntimeVersionBadgeHud>(true);
        if (existing != null)
        {
            instance = existing;
            instance.RefreshForScene(SceneManager.GetActiveScene().name);
            return existing;
        }

        GameObject runtimeObject = new GameObject("RuntimeVersionBadgeHud");
        instance = runtimeObject.AddComponent<RuntimeVersionBadgeHud>();
        instance.RefreshForScene(SceneManager.GetActiveScene().name);
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
        EnsureUi();
        RefreshForScene(SceneManager.GetActiveScene().name);
    }

    private void EnsureUi()
    {
        if (canvas != null && textRect != null && versionText != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject(
            CanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = SortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = false;
        }

        CanvasGroup canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        StretchRect(canvasRect);

        GameObject textObject = new GameObject(
            "RuntimeVersionBadgeText",
            typeof(RectTransform),
            typeof(TextMeshProUGUI),
            typeof(ContentSizeFitter));
        textObject.transform.SetParent(canvasObject.transform, false);

        textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(1f - RightInsetRatio, 0f);
        textRect.anchorMax = new Vector2(1f - RightInsetRatio, 0f);
        textRect.pivot = new Vector2(1f, 0f);
        textRect.anchoredPosition = new Vector2(-EdgeMargin, MainSceneBottomOffset);
        textRect.sizeDelta = Vector2.zero;

        versionText = textObject.GetComponent<TextMeshProUGUI>();
        versionText.font = TmpRuntimeFontFallback.EnsureChineseFallback() ?? TMP_Settings.defaultFontAsset;
        versionText.fontSize = 16f;
        versionText.fontStyle = FontStyles.Normal;
        versionText.color = TextColor;
        versionText.alignment = TextAlignmentOptions.BottomRight;
        versionText.enableWordWrapping = false;
        versionText.overflowMode = TextOverflowModes.Overflow;
        versionText.raycastTarget = false;

        ContentSizeFitter sizeFitter = textObject.GetComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void RefreshForScene(string sceneName)
    {
        EnsureUi();

        bool shouldShow = IsSupportedScene(sceneName);
        if (canvas != null && canvas.gameObject.activeSelf != shouldShow)
        {
            canvas.gameObject.SetActive(shouldShow);
        }

        if (!shouldShow)
        {
            return;
        }

        RuntimeBuildInfoData info = RuntimeBuildInfo.Current;
        versionText.text = BuildDisplayText(info);

        textRect.anchoredPosition = new Vector2(
            -EdgeMargin,
            IsBaseOrGameplayScene(sceneName) ? BaseAndGameplayBottomOffset : MainSceneBottomOffset);
        LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);
    }

    private static bool IsSupportedScene(string sceneName)
    {
        return string.Equals(sceneName, MainSceneName)
            || string.Equals(sceneName, BaseSceneName)
            || GameplayStageCatalog.IsGameplayScene(sceneName);
    }

    private static bool IsBaseOrGameplayScene(string sceneName)
    {
        return string.Equals(sceneName, BaseSceneName) || GameplayStageCatalog.IsGameplayScene(sceneName);
    }

    private static string BuildDisplayText(RuntimeBuildInfoData info)
    {
        if (info == null)
        {
            return "PROD v0.0.0";
        }

        if (string.IsNullOrWhiteSpace(info.DisplaySecondaryText))
        {
            return info.DisplayPrimaryText;
        }

        return $"{info.DisplayPrimaryText} | {info.DisplaySecondaryText}";
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }
}
