using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public enum SettingsPanelContext
{
    MainMenu = 0,
    BaseHub = 1,
    Gameplay = 2,
    PauseMenu = 3
}

public sealed class RuntimeSettingsPanel : MonoBehaviour
{
    private enum SettingsTab
    {
        Audio,
        Display,
        Controls,
        Data
    }

    private const string CanvasName = "RuntimeSettingsPanelCanvas";
    private const int SortingOrder = 940;
    private const float PanelWidth = 1440f;
    private const float PanelHeight = 840f;
    private const float CardWidth = 1220f;
    private const float FooterButtonWidth = 172f;
    private const float FooterButtonHeight = 56f;
    private const float ShowAnimationDuration = RuntimeModalStyle.TransitionDuration;
    private const int DisplayRefreshFrameBudget = 12;
    private const float PageBottomPadding = 36f;
    private const float ManualScrollStep = 0.12f;

    private const string RequiredCharacters =
        "设置游戏已暂停暂停页返回暂停页当前没有未应用更改存在未应用更改继续游戏会自动应用关闭时会自动应用关闭设置会自动应用声音画面按键存档恢复默认取消应用总音量音乐音量音效音量控制全部游戏声音背景音乐攻击与发射声音单独强度实时预览屏幕大小显示模式视野缩放影响游戏相机可见范围待应用窗口尺寸窗口模式与全屏切换当前生效信息以下内容来自当前运行中的实际状态攻击交互地图暂停拍照留念纪念截图点击右侧按钮后按任意键或鼠标键进行绑定等待输入正在为当前暂停键重置存档清空本地建筑进度关卡选择武器状态和留念相册设置项会保留没有可重置的数据再次点击将立即清空本地进度与相册截图，并返回主菜单此操作不可恢复完成后重置结束后会返回主菜单便于从干净状态重新开始重置结束后会留在主菜单返回主菜单图形音量键位等设置不会被重置保留设置确认重置关闭应用并继续继续游戏应用并关闭关闭返回主界面应用并返回不保存返回确认返回当前战斗会直接结束并回到主界面未应用改动会先写入设置不会重置存档或相册会放弃当前未应用改动并回到主界面返回取消留在当前页面，。“”0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-+/():.% ";

    private static readonly string[] RuntimeFontNames =
    {
        "Noto Sans SC",
        "PingFang SC",
        "Hiragino Sans GB",
        "Songti SC",
        "Arial Unicode MS"
    };

    private static readonly KeyCode[] BindableMouseKeys =
    {
        KeyCode.Mouse0,
        KeyCode.Mouse1,
        KeyCode.Mouse2
    };

    private static readonly Color PanelColor = new Color(0.09f, 0.11f, 0.16f, 0.82f);
    private static readonly Color BorderColor = new Color(0.31f, 0.45f, 0.57f, 0.88f);
    private static readonly Color SectionColor = new Color(0.14f, 0.17f, 0.24f, 0.56f);
    private static readonly Color TabIdleColor = new Color(0.21f, 0.26f, 0.34f, 0.72f);
    private static readonly Color TabActiveColor = new Color(0.84f, 0.67f, 0.36f, 0.92f);
    private static readonly Color TabIdleTextColor = new Color(0.78f, 0.84f, 0.92f, 1f);
    private static readonly Color TabActiveTextColor = new Color(0.17f, 0.12f, 0.08f, 1f);
    private static readonly Color PrimaryButtonColor = new Color(0.84f, 0.67f, 0.36f, 0.94f);
    private static readonly Color PrimaryButtonTextColor = new Color(0.17f, 0.12f, 0.08f, 1f);
    private static readonly Color SecondaryButtonColor = new Color(0.27f, 0.33f, 0.43f, 0.76f);
    private static readonly Color SecondaryButtonTextColor = new Color(0.92f, 0.96f, 1f, 1f);
    private static readonly Color DisabledButtonColor = new Color(0.19f, 0.22f, 0.30f, 0.48f);
    private static readonly Color DisabledButtonTextColor = new Color(0.58f, 0.64f, 0.72f, 0.92f);
    private static readonly Color DisabledPrimaryButtonColor = new Color(0.44f, 0.37f, 0.28f, 0.72f);
    private static readonly Color DisabledPrimaryButtonTextColor = new Color(0.95f, 0.90f, 0.80f, 0.82f);
    private static readonly Color DangerButtonColor = new Color(0.55f, 0.25f, 0.22f, 0.88f);
    private static readonly Color DangerButtonArmedColor = new Color(0.84f, 0.30f, 0.26f, 0.96f);
    private static readonly Color DangerButtonTextColor = new Color(0.99f, 0.95f, 0.92f, 1f);
    private static readonly Color ConfirmOverlayColor = new Color(0.03f, 0.04f, 0.06f, 0.82f);
    private static readonly Color ConfirmPanelColor = new Color(0.11f, 0.13f, 0.18f, 0.96f);
    private static readonly Color TitleColor = new Color(0.96f, 0.98f, 1f, 1f);
    private static readonly Color DescriptionColor = new Color(0.70f, 0.78f, 0.88f, 1f);
    private static readonly Color ValueChipColor = new Color(0.16f, 0.20f, 0.28f, 0.78f);
    private static readonly Color ValueTextColor = new Color(0.98f, 0.99f, 1f, 1f);
    private static readonly Color EmphasisColor = new Color(0.98f, 0.89f, 0.66f, 1f);

    private static RuntimeSettingsPanel instance;
    private static TMP_FontAsset runtimeFontAsset;
    private static KeyCode[] cachedKeyboardKeyCodes;

    public static RuntimeSettingsPanel Instance => instance;

    public event Action ContinueRequested;

    public bool IsCapturingBinding => pendingBindingAction.HasValue;
    public bool IsShown => IsPanelShown();

    private readonly Dictionary<SettingsTab, RectTransform> pageRoots = new Dictionary<SettingsTab, RectTransform>();
    private readonly Dictionary<SettingsTab, ScrollRect> pageScrollRects = new Dictionary<SettingsTab, ScrollRect>();
    private readonly Dictionary<SettingsTab, Image> tabImages = new Dictionary<SettingsTab, Image>();
    private readonly Dictionary<SettingsTab, TextMeshProUGUI> tabLabels = new Dictionary<SettingsTab, TextMeshProUGUI>();
    private readonly Dictionary<GameInputAction, TextMeshProUGUI> bindingValueTexts = new Dictionary<GameInputAction, TextMeshProUGUI>();

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RawImage blurBackdropImage;
    private Image blurTintImage;
    private Image overlayImage;
    private Outline panelOutline;
    private RectTransform panelRectTransform;
    private CanvasGroup panelCanvasGroup;
    private Coroutine showAnimationCoroutine;
    private Coroutine hideAnimationCoroutine;
    private Coroutine showSequenceCoroutine;
    private Coroutine backdropRefreshCoroutine;
    private Coroutine liveBackdropCoroutine;
    private Vector2 panelVisibleAnchoredPosition;
    private SettingsTab currentTab = SettingsTab.Audio;
    private SettingsPanelContext currentContext = SettingsPanelContext.Gameplay;
    private GameInputAction? pendingBindingAction;
    private float captureReadyAt;
    private bool resetSaveArmed;
    private bool returnToMenuConfirmOpen;
    private Texture2D capturedBackdropTexture;
    private RenderTexture blurredBackdropTexture;
    private float animationProgress;
    private Action pendingHideCallback;

    private GameSettingsDraft savedSettings;
    private GameSettingsDraft draftSettings;

    private TextMeshProUGUI subtitleText;
    private TextMeshProUGUI captureHintText;
    private TextMeshProUGUI masterVolumeValueText;
    private TextMeshProUGUI musicVolumeValueText;
    private TextMeshProUGUI sfxVolumeValueText;
    private TextMeshProUGUI resolutionValueText;
    private TextMeshProUGUI displayModeValueText;
    private TextMeshProUGUI viewZoomValueText;
    private TextMeshProUGUI runtimeInfoValueText;
    private TextMeshProUGUI saveResetSummaryText;
    private TextMeshProUGUI dataResetDescriptionText;
    private TextMeshProUGUI dataCompletionDescriptionText;
    private TextMeshProUGUI dataCompletionValueText;
    private Button applyButton;
    private TextMeshProUGUI applyButtonLabel;
    private Image applyButtonImage;
    private Button continueButton;
    private TextMeshProUGUI continueButtonLabel;
    private Image continueButtonImage;
    private Button returnToMenuButton;
    private TextMeshProUGUI returnToMenuButtonLabel;
    private Image returnToMenuButtonImage;
    private Button resetSaveButton;
    private TextMeshProUGUI resetSaveButtonLabel;
    private Image resetSaveButtonImage;
    private RectTransform returnToMenuConfirmRoot;
    private CanvasGroup returnToMenuConfirmCanvasGroup;
    private TextMeshProUGUI returnToMenuConfirmTitleText;
    private TextMeshProUGUI returnToMenuConfirmBodyText;
    private Button returnToMenuConfirmPrimaryButton;
    private TextMeshProUGUI returnToMenuConfirmPrimaryButtonLabel;
    private Image returnToMenuConfirmPrimaryButtonImage;
    private Button returnToMenuConfirmSecondaryButton;
    private TextMeshProUGUI returnToMenuConfirmSecondaryButtonLabel;
    private Image returnToMenuConfirmSecondaryButtonImage;
    private Button returnToMenuConfirmCancelButton;
    private TextMeshProUGUI returnToMenuConfirmCancelButtonLabel;
    private Image returnToMenuConfirmCancelButtonImage;

    public static RuntimeSettingsPanel EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        RuntimeSettingsPanel existing = FindObjectOfType<RuntimeSettingsPanel>(true);
        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        GameObject panelObject = new GameObject("RuntimeSettingsPanel");
        instance = panelObject.AddComponent<RuntimeSettingsPanel>();
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
        HideImmediate();
    }

    private void Update()
    {
        HandlePageScrollInput();

        if (!IsPanelShown() || !pendingBindingAction.HasValue || Time.unscaledTime < captureReadyAt || draftSettings == null)
        {
            return;
        }

        if (!TryCapturePressedKey(out KeyCode capturedKey))
        {
            return;
        }

        draftSettings.SetBinding(pendingBindingAction.Value, capturedKey);
        pendingBindingAction = null;
        RefreshControlsPage();
        RefreshFooterState();
    }

    private void HandlePageScrollInput()
    {
        if (!IsPanelShown() || returnToMenuConfirmOpen || panelRectTransform == null)
        {
            return;
        }

        float scrollDelta = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(scrollDelta, 0f))
        {
            return;
        }

        if (!RectTransformUtility.RectangleContainsScreenPoint(panelRectTransform, Input.mousePosition, null))
        {
            return;
        }

        if (!pageScrollRects.TryGetValue(currentTab, out ScrollRect scrollRect) || !CanScroll(scrollRect))
        {
            return;
        }

        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
            scrollRect.verticalNormalizedPosition + scrollDelta * ManualScrollStep);
    }

    private static bool CanScroll(ScrollRect scrollRect)
    {
        return scrollRect != null &&
               scrollRect.content != null &&
               scrollRect.viewport != null &&
               scrollRect.content.rect.height > scrollRect.viewport.rect.height + 1f;
    }

    private void OnDestroy()
    {
        StopShowSequence();
        StopShowAnimation();
        StopHideAnimation();
        StopBackdropRefresh();
        StopLiveBackdropRefresh();
        ReleaseBlurBackdrop();
    }

    public void SetVisible(bool shouldShow)
    {
        if (canvas != null)
        {
            canvas.gameObject.SetActive(shouldShow);
        }

        if (!shouldShow)
        {
            HideImmediate();
        }
    }

    public void Show(SettingsPanelContext context)
    {
        currentContext = context;
        EnsureUi();
        LoadDraftState();
        RefreshAll();

        if (canvas != null)
        {
            canvas.gameObject.SetActive(true);
        }

        StartLiveBackdropRefresh();
        ResetPageScrollPosition(currentTab);
        SetPanelVisibility(true);
        ApplyAnimationState(0f);
        StartShowSequence();
    }

    public void HideImmediate()
    {
        CompleteHideState(true, true);
    }

    private void CompleteHideState(bool stopHideAnimation, bool clearPendingHideCallback)
    {
        StopShowSequence();
        StopShowAnimation();
        if (stopHideAnimation)
        {
            StopHideAnimation();
        }

        StopBackdropRefresh();
        StopLiveBackdropRefresh();
        pendingBindingAction = null;
        if (clearPendingHideCallback)
        {
            pendingHideCallback = null;
        }

        if (savedSettings != null)
        {
            GameSettingsStore.PreviewDraftAudio(savedSettings);
        }

        savedSettings = null;
        draftSettings = null;
        resetSaveArmed = false;
        returnToMenuConfirmOpen = false;
        SetReturnToMenuConfirmVisible(false);
        ReleaseBlurBackdrop();
        ApplyAnimationState(0f);
        SetPanelVisibility(false);
    }

    public void Hide(Action onHidden)
    {
        if (!IsPanelShown())
        {
            HideImmediate();
            onHidden?.Invoke();
            return;
        }

        pendingHideCallback = onHidden;
        StopShowSequence();
        StopShowAnimation();
        StopBackdropRefresh();
        StopHideAnimation();
        hideAnimationCoroutine = StartCoroutine(AnimateHideRoutine());
    }

    public void RequestContinueGame()
    {
        HandleContinueRequested();
    }

    private void LoadDraftState()
    {
        savedSettings = GameSettingsStore.LoadSavedSettings();
        draftSettings = GameSettingsStore.CreateDraftFromSaved();
        pendingBindingAction = null;
        resetSaveArmed = false;
        returnToMenuConfirmOpen = false;
    }

    private void EnsureUi()
    {
        if (canvas != null)
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

        canvasGroup = canvasObject.GetComponent<CanvasGroup>();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        StretchRect(canvasRect);

        GameObject blurObject = new GameObject("BlurBackdrop", typeof(RectTransform), typeof(RawImage));
        blurObject.transform.SetParent(canvasObject.transform, false);
        blurBackdropImage = blurObject.GetComponent<RawImage>();
        blurBackdropImage.color = Color.white;
        blurBackdropImage.raycastTarget = false;
        StretchRect(blurBackdropImage.rectTransform);

        blurTintImage = CreateImage("BlurTint", canvasObject.transform, RuntimeModalStyle.BlurTintColor, 0, 0);
        StretchRect(blurTintImage.rectTransform);
        blurTintImage.raycastTarget = false;

        overlayImage = CreateImage("Overlay", canvasObject.transform, RuntimeModalStyle.OverlayColor, 0, 0);
        StretchRect(overlayImage.rectTransform);
        overlayImage.raycastTarget = true;

        Image panel = CreateImage("Panel", canvasObject.transform, PanelColor, 28, 20);
        panelRectTransform = panel.rectTransform;
        panelRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        panelRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        panelRectTransform.pivot = new Vector2(0.5f, 0.5f);
        panelRectTransform.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        panelVisibleAnchoredPosition = Vector2.zero;

        panelCanvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
        panelOutline = panel.gameObject.AddComponent<Outline>();
        panelOutline.effectColor = BorderColor;
        panelOutline.effectDistance = new Vector2(1f, -1f);

        BuildHeader(panel.transform);
        BuildTabs(panel.transform);
        BuildPages(panel.transform);
        BuildFooter(panel.transform);
        BuildReturnToMenuConfirm(panel.transform);

        ApplyAnimationState(0f);
    }

    private void BuildHeader(Transform parent)
    {
        RectTransform headerRoot = CreateContainer("Header", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f));
        headerRoot.offsetMin = new Vector2(36f, -124f);
        headerRoot.offsetMax = new Vector2(-36f, -28f);

        TextMeshProUGUI title = CreateText("Title", headerRoot, "设置", 42f, TitleColor, TextAlignmentOptions.Left);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.sizeDelta = new Vector2(240f, 52f);
        titleRect.anchoredPosition = Vector2.zero;

        subtitleText = CreateText("Subtitle", headerRoot, string.Empty, 22f, DescriptionColor, TextAlignmentOptions.Left);
        RectTransform subtitleRect = subtitleText.rectTransform;
        subtitleRect.anchorMin = new Vector2(0f, 1f);
        subtitleRect.anchorMax = new Vector2(0f, 1f);
        subtitleRect.pivot = new Vector2(0f, 1f);
        subtitleRect.sizeDelta = new Vector2(1240f, 64f);
        subtitleRect.anchoredPosition = new Vector2(0f, -58f);
    }

    private void BuildTabs(Transform parent)
    {
        RectTransform tabRoot = CreateContainer("TabRoot", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f));
        tabRoot.offsetMin = new Vector2(36f, -194f);
        tabRoot.offsetMax = new Vector2(-36f, -138f);

        CreateTabButton(tabRoot, SettingsTab.Audio, "声音", 0f);
        CreateTabButton(tabRoot, SettingsTab.Display, "画面", 188f);
        CreateTabButton(tabRoot, SettingsTab.Controls, "按键", 376f);
        CreateTabButton(tabRoot, SettingsTab.Data, "存档", 564f);
    }

    private void BuildPages(Transform parent)
    {
        RectTransform contentRoot = CreateContainer("ContentRoot", parent, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f));
        contentRoot.offsetMin = new Vector2(36f, 112f);
        contentRoot.offsetMax = new Vector2(-36f, -206f);

        CreateAudioPage(contentRoot);
        CreateDisplayPage(contentRoot);
        CreateControlsPage(contentRoot);
        CreateDataPage(contentRoot);
    }

    private void BuildFooter(Transform parent)
    {
        RectTransform footerRoot = CreateContainer("Footer", parent, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f));
        footerRoot.offsetMin = new Vector2(36f, 28f);
        footerRoot.offsetMax = new Vector2(-36f, 92f);

        Button resetButton = CreateButton(
            "ResetButton",
            footerRoot,
            "恢复默认",
            SecondaryButtonColor,
            SecondaryButtonTextColor,
            new Vector2(FooterButtonWidth, FooterButtonHeight));
        RectTransform resetRect = resetButton.GetComponent<RectTransform>();
        resetRect.anchorMin = new Vector2(0f, 0.5f);
        resetRect.anchorMax = new Vector2(0f, 0.5f);
        resetRect.pivot = new Vector2(0f, 0.5f);
        resetRect.anchoredPosition = Vector2.zero;
        resetButton.onClick.AddListener(HandleResetAll);

        Button cancelButton = CreateButton(
            "CancelButton",
            footerRoot,
            "取消",
            SecondaryButtonColor,
            SecondaryButtonTextColor,
            new Vector2(FooterButtonWidth, FooterButtonHeight));
        RectTransform cancelRect = cancelButton.GetComponent<RectTransform>();
        cancelRect.anchorMin = new Vector2(0f, 0.5f);
        cancelRect.anchorMax = new Vector2(0f, 0.5f);
        cancelRect.pivot = new Vector2(0f, 0.5f);
        cancelRect.anchoredPosition = new Vector2(FooterButtonWidth + 20f, 0f);
        cancelButton.onClick.AddListener(HandleCancelRequested);

        applyButton = CreateButton(
            "ApplyButton",
            footerRoot,
            "应用",
            PrimaryButtonColor,
            PrimaryButtonTextColor,
            new Vector2(FooterButtonWidth, FooterButtonHeight));
        RectTransform applyRect = applyButton.GetComponent<RectTransform>();
        applyRect.anchorMin = new Vector2(1f, 0.5f);
        applyRect.anchorMax = new Vector2(1f, 0.5f);
        applyRect.pivot = new Vector2(1f, 0.5f);
        applyRect.anchoredPosition = new Vector2(-(FooterButtonWidth + 20f), 0f);
        applyButton.onClick.AddListener(HandleApplyRequested);
        applyButtonImage = applyButton.GetComponent<Image>();
        applyButtonLabel = applyButton.GetComponentInChildren<TextMeshProUGUI>();

        returnToMenuButton = CreateButton(
            "ReturnToMenuButton",
            footerRoot,
            "返回主界面",
            SecondaryButtonColor,
            SecondaryButtonTextColor,
            new Vector2(196f, FooterButtonHeight));
        RectTransform returnRect = returnToMenuButton.GetComponent<RectTransform>();
        returnRect.anchorMin = new Vector2(1f, 0.5f);
        returnRect.anchorMax = new Vector2(1f, 0.5f);
        returnRect.pivot = new Vector2(1f, 0.5f);
        returnRect.anchoredPosition = new Vector2(-(FooterButtonWidth * 2f + 48f), 0f);
        returnToMenuButton.onClick.AddListener(HandleReturnToMenuRequested);
        returnToMenuButtonImage = returnToMenuButton.GetComponent<Image>();
        returnToMenuButtonLabel = returnToMenuButton.GetComponentInChildren<TextMeshProUGUI>();

        continueButton = CreateButton(
            "FooterContinueButton",
            footerRoot,
            "应用并继续",
            PrimaryButtonColor,
            PrimaryButtonTextColor,
            new Vector2(FooterButtonWidth, FooterButtonHeight));
        RectTransform continueRect = continueButton.GetComponent<RectTransform>();
        continueRect.anchorMin = new Vector2(1f, 0.5f);
        continueRect.anchorMax = new Vector2(1f, 0.5f);
        continueRect.pivot = new Vector2(1f, 0.5f);
        continueRect.anchoredPosition = Vector2.zero;
        continueButton.onClick.AddListener(HandleContinueRequested);
        continueButtonLabel = continueButton.GetComponentInChildren<TextMeshProUGUI>();
        continueButtonImage = continueButton.GetComponent<Image>();
    }

    private void BuildReturnToMenuConfirm(Transform parent)
    {
        returnToMenuConfirmRoot = CreateContainer("ReturnToMenuConfirmRoot", parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        StretchRect(returnToMenuConfirmRoot);
        returnToMenuConfirmCanvasGroup = returnToMenuConfirmRoot.gameObject.AddComponent<CanvasGroup>();

        Image overlay = CreateImage("ConfirmOverlay", returnToMenuConfirmRoot, ConfirmOverlayColor, 0, 0);
        StretchRect(overlay.rectTransform);
        overlay.raycastTarget = true;

        Image panel = CreateImage("ConfirmPanel", returnToMenuConfirmRoot, ConfirmPanelColor, 24, 18);
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(760f, 304f);
        panelRect.anchoredPosition = new Vector2(0f, 18f);

        returnToMenuConfirmTitleText = CreateText("Title", panelRect, string.Empty, 34f, TitleColor, TextAlignmentOptions.Left);
        RectTransform titleRect = returnToMenuConfirmTitleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.sizeDelta = new Vector2(600f, 44f);
        titleRect.anchoredPosition = new Vector2(34f, -26f);

        returnToMenuConfirmBodyText = CreateText("Body", panelRect, string.Empty, 22f, DescriptionColor, TextAlignmentOptions.Left);
        RectTransform bodyRect = returnToMenuConfirmBodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0f, 1f);
        bodyRect.offsetMin = new Vector2(34f, -168f);
        bodyRect.offsetMax = new Vector2(-34f, -88f);
        returnToMenuConfirmBodyText.enableWordWrapping = true;

        returnToMenuConfirmPrimaryButton = CreateButton(
            "PrimaryButton",
            panelRect,
            "应用并返回",
            PrimaryButtonColor,
            PrimaryButtonTextColor,
            new Vector2(188f, 56f));
        RectTransform primaryRect = returnToMenuConfirmPrimaryButton.GetComponent<RectTransform>();
        primaryRect.anchorMin = new Vector2(1f, 0f);
        primaryRect.anchorMax = new Vector2(1f, 0f);
        primaryRect.pivot = new Vector2(1f, 0f);
        primaryRect.anchoredPosition = new Vector2(-34f, 30f);
        returnToMenuConfirmPrimaryButton.onClick.AddListener(() => ExecuteReturnToMainMenu(true));
        returnToMenuConfirmPrimaryButtonImage = returnToMenuConfirmPrimaryButton.GetComponent<Image>();
        returnToMenuConfirmPrimaryButtonLabel = returnToMenuConfirmPrimaryButton.GetComponentInChildren<TextMeshProUGUI>();

        returnToMenuConfirmSecondaryButton = CreateButton(
            "SecondaryButton",
            panelRect,
            "不保存返回",
            SecondaryButtonColor,
            SecondaryButtonTextColor,
            new Vector2(188f, 56f));
        RectTransform secondaryRect = returnToMenuConfirmSecondaryButton.GetComponent<RectTransform>();
        secondaryRect.anchorMin = new Vector2(1f, 0f);
        secondaryRect.anchorMax = new Vector2(1f, 0f);
        secondaryRect.pivot = new Vector2(1f, 0f);
        secondaryRect.anchoredPosition = new Vector2(-234f, 30f);
        returnToMenuConfirmSecondaryButton.onClick.AddListener(() => ExecuteReturnToMainMenu(false));
        returnToMenuConfirmSecondaryButtonImage = returnToMenuConfirmSecondaryButton.GetComponent<Image>();
        returnToMenuConfirmSecondaryButtonLabel = returnToMenuConfirmSecondaryButton.GetComponentInChildren<TextMeshProUGUI>();

        returnToMenuConfirmCancelButton = CreateButton(
            "CancelButton",
            panelRect,
            "取消",
            SecondaryButtonColor,
            SecondaryButtonTextColor,
            new Vector2(156f, 56f));
        RectTransform confirmCancelRect = returnToMenuConfirmCancelButton.GetComponent<RectTransform>();
        confirmCancelRect.anchorMin = new Vector2(0f, 0f);
        confirmCancelRect.anchorMax = new Vector2(0f, 0f);
        confirmCancelRect.pivot = new Vector2(0f, 0f);
        confirmCancelRect.anchoredPosition = new Vector2(34f, 30f);
        returnToMenuConfirmCancelButton.onClick.AddListener(HideReturnToMenuConfirm);
        returnToMenuConfirmCancelButtonImage = returnToMenuConfirmCancelButton.GetComponent<Image>();
        returnToMenuConfirmCancelButtonLabel = returnToMenuConfirmCancelButton.GetComponentInChildren<TextMeshProUGUI>();

        SetReturnToMenuConfirmVisible(false);
    }

    private void CreateAudioPage(Transform parent)
    {
        RectTransform pageRoot = CreatePageRoot("AudioPage", parent, SettingsTab.Audio, 392f);
        masterVolumeValueText = CreateStepperCard(
            pageRoot,
            "总音量",
            "控制全部游戏声音，调整时会实时预览",
            32f,
            () => ChangeMasterVolume(-0.05f),
            () => ChangeMasterVolume(0.05f));
        musicVolumeValueText = CreateStepperCard(
            pageRoot,
            "音乐音量",
            "背景音乐单独强度，调整时会实时预览",
            164f,
            () => ChangeMusicVolume(-0.05f),
            () => ChangeMusicVolume(0.05f));
        sfxVolumeValueText = CreateStepperCard(
            pageRoot,
            "音效音量",
            "攻击与发射声音单独强度，调整时会实时预览",
            296f,
            () => ChangeSfxVolume(-0.05f),
            () => ChangeSfxVolume(0.05f));
    }

    private void CreateDisplayPage(Transform parent)
    {
        RectTransform pageRoot = CreatePageRoot("DisplayPage", parent, SettingsTab.Display, 512f);
        resolutionValueText = CreateStepperCard(
            pageRoot,
            "屏幕大小",
            "调整待应用的窗口尺寸",
            24f,
            () => ChangeResolution(-1),
            () => ChangeResolution(1));
        displayModeValueText = CreateStepperCard(
            pageRoot,
            "显示模式",
            "窗口模式与全屏切换",
            148f,
            () => CycleDisplayMode(-1),
            () => CycleDisplayMode(1));
        viewZoomValueText = CreateStepperCard(
            pageRoot,
            "视野缩放",
            "影响游戏相机可见范围",
            272f,
            () => ChangeViewZoom(-1),
            () => ChangeViewZoom(1));
        runtimeInfoValueText = CreateStatusCard(pageRoot, 396f);
    }

    private void CreateControlsPage(Transform parent)
    {
        RectTransform pageRoot = CreatePageRoot("ControlsPage", parent, SettingsTab.Controls, 620f);

        captureHintText = CreateText(
            "CaptureHint",
            pageRoot,
            "点击右侧按钮后，按任意键或鼠标键进行绑定",
            20f,
            DescriptionColor,
            TextAlignmentOptions.Left);
        RectTransform hintRect = captureHintText.rectTransform;
        hintRect.anchorMin = new Vector2(0.5f, 1f);
        hintRect.anchorMax = new Vector2(0.5f, 1f);
        hintRect.pivot = new Vector2(0.5f, 1f);
        hintRect.sizeDelta = new Vector2(CardWidth, 30f);
        hintRect.anchoredPosition = new Vector2(0f, -6f);

        CreateBindingCard(pageRoot, GameInputAction.Attack, "攻击", "战斗中发射墨球", 44f);
        CreateBindingCard(pageRoot, GameInputAction.Interact, "交互", "靠近对象时触发交互", 164f);
        CreateBindingCard(pageRoot, GameInputAction.OpenMap, "地图", "查看和展开地图", 284f);
        CreateBindingCard(pageRoot, GameInputAction.Pause, "暂停", "打开或关闭暂停菜单", 404f);
        CreateBindingCard(pageRoot, GameInputAction.PhotoCapture, "拍照", "定格当前画面并保存留念", 524f);
    }

    private void CreateDataPage(Transform parent)
    {
        RectTransform pageRoot = CreatePageRoot("DataPage", parent, SettingsTab.Data, 456f);

        Image card = CreateImage("SaveResetCard", pageRoot, SectionColor, 20, 16);
        RectTransform cardRect = card.rectTransform;
        cardRect.anchorMin = new Vector2(0.5f, 1f);
        cardRect.anchorMax = new Vector2(0.5f, 1f);
        cardRect.pivot = new Vector2(0.5f, 1f);
        cardRect.sizeDelta = new Vector2(CardWidth, 184f);
        cardRect.anchoredPosition = new Vector2(0f, -28f);

        TextMeshProUGUI titleText = CreateText("Title", cardRect, "重置存档", 28f, TitleColor, TextAlignmentOptions.Left);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.sizeDelta = new Vector2(360f, 34f);
        titleRect.anchoredPosition = new Vector2(30f, -18f);

        dataResetDescriptionText = CreateText(
            "Description",
            cardRect,
            "清空本地建筑进度、关卡选择、武器状态和留念相册，设置项会保留。",
            20f,
            DescriptionColor,
            TextAlignmentOptions.Left);
        RectTransform descriptionRect = dataResetDescriptionText.rectTransform;
        descriptionRect.anchorMin = new Vector2(0f, 1f);
        descriptionRect.anchorMax = new Vector2(0f, 1f);
        descriptionRect.pivot = new Vector2(0f, 1f);
        descriptionRect.sizeDelta = new Vector2(680f, 48f);
        descriptionRect.anchoredPosition = new Vector2(30f, -56f);

        saveResetSummaryText = CreateText("Summary", cardRect, string.Empty, 18f, EmphasisColor, TextAlignmentOptions.Left);
        RectTransform summaryRect = saveResetSummaryText.rectTransform;
        summaryRect.anchorMin = new Vector2(0f, 0f);
        summaryRect.anchorMax = new Vector2(0f, 0f);
        summaryRect.pivot = new Vector2(0f, 0f);
        summaryRect.sizeDelta = new Vector2(700f, 58f);
        summaryRect.anchoredPosition = new Vector2(30f, 24f);

        resetSaveButton = CreateButton(
            "ResetSaveButton",
            cardRect,
            "重置存档",
            DangerButtonColor,
            DangerButtonTextColor,
            new Vector2(236f, 56f));
        RectTransform buttonRect = resetSaveButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0.5f);
        buttonRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRect.pivot = new Vector2(1f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(-30f, -6f);
        resetSaveButton.onClick.AddListener(HandleResetSaveRequested);
        resetSaveButtonImage = resetSaveButton.GetComponent<Image>();
        resetSaveButtonLabel = resetSaveButton.GetComponentInChildren<TextMeshProUGUI>();

        CreateStaticInfoCard(
            pageRoot,
            "完成后",
            "重置结束后会返回主菜单，便于从干净状态重新开始",
            236f,
            "返回主菜单",
            out dataCompletionDescriptionText,
            out dataCompletionValueText);
        CreateStaticInfoCard(pageRoot, "保留项", "图形、音量、键位等设置不会被重置", 360f, "保留设置");
    }

    private RectTransform CreatePageRoot(string name, Transform parent, SettingsTab tab, float contentHeight)
    {
        RectTransform scrollRoot = CreateContainer(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        StretchRect(scrollRoot);

        ScrollRect scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = 0f;

        Image viewportImage = CreateImage("Viewport", scrollRoot, Color.clear, 0, 0);
        viewportImage.raycastTarget = true;
        RectTransform viewport = viewportImage.rectTransform;
        StretchRect(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform content = CreateContainer("Content", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
        float paddedContentHeight = Mathf.Max(0f, contentHeight + PageBottomPadding);
        content.offsetMin = new Vector2(0f, -paddedContentHeight);
        content.offsetMax = Vector2.zero;

        scrollRect.viewport = viewport;
        scrollRect.content = content;

        pageRoots[tab] = scrollRoot;
        pageScrollRects[tab] = scrollRect;
        return content;
    }

    private TextMeshProUGUI CreateStepperCard(
        Transform parent,
        string title,
        string description,
        float topOffset,
        Action onDecrease,
        Action onIncrease)
    {
        RectTransform cardRoot = CreateCardRoot(parent, title, description, topOffset);

        RectTransform controlsRoot = CreateContainer("Controls", cardRoot, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
        controlsRoot.sizeDelta = new Vector2(340f, 56f);
        controlsRoot.anchoredPosition = new Vector2(-30f, 0f);

        Button decreaseButton = CreateButton("Decrease", controlsRoot, "-", SecondaryButtonColor, SecondaryButtonTextColor, new Vector2(56f, 56f));
        RectTransform decreaseRect = decreaseButton.GetComponent<RectTransform>();
        decreaseRect.anchorMin = new Vector2(0f, 0.5f);
        decreaseRect.anchorMax = new Vector2(0f, 0.5f);
        decreaseRect.pivot = new Vector2(0f, 0.5f);
        decreaseRect.anchoredPosition = Vector2.zero;
        decreaseButton.onClick.AddListener(() => onDecrease?.Invoke());

        Image valueChip = CreateImage("ValueChip", controlsRoot, ValueChipColor, 18, 14);
        RectTransform chipRect = valueChip.rectTransform;
        chipRect.anchorMin = new Vector2(0.5f, 0.5f);
        chipRect.anchorMax = new Vector2(0.5f, 0.5f);
        chipRect.pivot = new Vector2(0.5f, 0.5f);
        chipRect.sizeDelta = new Vector2(180f, 56f);
        chipRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI valueText = CreateText("Value", valueChip.transform, string.Empty, 24f, ValueTextColor, TextAlignmentOptions.Center);
        StretchRect(valueText.rectTransform);

        Button increaseButton = CreateButton("Increase", controlsRoot, "+", PrimaryButtonColor, PrimaryButtonTextColor, new Vector2(56f, 56f));
        RectTransform increaseRect = increaseButton.GetComponent<RectTransform>();
        increaseRect.anchorMin = new Vector2(1f, 0.5f);
        increaseRect.anchorMax = new Vector2(1f, 0.5f);
        increaseRect.pivot = new Vector2(1f, 0.5f);
        increaseRect.anchoredPosition = Vector2.zero;
        increaseButton.onClick.AddListener(() => onIncrease?.Invoke());

        return valueText;
    }

    private TextMeshProUGUI CreateStatusCard(Transform parent, float topOffset)
    {
        Image card = CreateImage("RuntimeInfoCard", parent, SectionColor, 20, 16);
        RectTransform cardRect = card.rectTransform;
        cardRect.anchorMin = new Vector2(0.5f, 1f);
        cardRect.anchorMax = new Vector2(0.5f, 1f);
        cardRect.pivot = new Vector2(0.5f, 1f);
        cardRect.sizeDelta = new Vector2(CardWidth, 116f);
        cardRect.anchoredPosition = new Vector2(0f, -topOffset);

        TextMeshProUGUI titleText = CreateText("Title", cardRect, "当前生效信息", 28f, TitleColor, TextAlignmentOptions.Left);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.sizeDelta = new Vector2(320f, 34f);
        titleRect.anchoredPosition = new Vector2(30f, -16f);

        TextMeshProUGUI descriptionText = CreateText(
            "Description",
            cardRect,
            "以下内容来自当前运行中的实际状态",
            18f,
            DescriptionColor,
            TextAlignmentOptions.Left);
        RectTransform descriptionRect = descriptionText.rectTransform;
        descriptionRect.anchorMin = new Vector2(0f, 1f);
        descriptionRect.anchorMax = new Vector2(0f, 1f);
        descriptionRect.pivot = new Vector2(0f, 1f);
        descriptionRect.sizeDelta = new Vector2(520f, 28f);
        descriptionRect.anchoredPosition = new Vector2(30f, -48f);

        TextMeshProUGUI valueText = CreateText("RuntimeInfo", cardRect, string.Empty, 18f, EmphasisColor, TextAlignmentOptions.Left);
        RectTransform valueRect = valueText.rectTransform;
        valueRect.anchorMin = new Vector2(0f, 0f);
        valueRect.anchorMax = new Vector2(1f, 0f);
        valueRect.pivot = new Vector2(0f, 0f);
        valueRect.offsetMin = new Vector2(30f, 14f);
        valueRect.offsetMax = new Vector2(-30f, 52f);
        valueText.enableWordWrapping = false;
        return valueText;
    }

    private void CreateBindingCard(
        Transform parent,
        GameInputAction action,
        string title,
        string description,
        float topOffset)
    {
        RectTransform cardRoot = CreateCardRoot(parent, title, description, topOffset);

        Button bindingButton = CreateButton("BindingButton", cardRoot, string.Empty, SecondaryButtonColor, SecondaryButtonTextColor, new Vector2(236f, 56f));
        RectTransform buttonRect = bindingButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0.5f);
        buttonRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRect.pivot = new Vector2(1f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(-30f, 0f);
        bindingButton.onClick.AddListener(() => BeginBindingCapture(action));

        TextMeshProUGUI label = bindingButton.GetComponentInChildren<TextMeshProUGUI>();
        bindingValueTexts[action] = label;
    }

    private void CreateStaticInfoCard(
        Transform parent,
        string title,
        string description,
        float topOffset,
        string value)
    {
        CreateStaticInfoCard(parent, title, description, topOffset, value, out _, out _);
    }

    private void CreateStaticInfoCard(
        Transform parent,
        string title,
        string description,
        float topOffset,
        string value,
        out TextMeshProUGUI descriptionText,
        out TextMeshProUGUI valueText)
    {
        RectTransform cardRoot = CreateCardRoot(parent, title, description, topOffset);
        descriptionText = FindCardDescription(cardRoot);

        Image valueChip = CreateImage("StaticChip", cardRoot, ValueChipColor, 18, 14);
        RectTransform chipRect = valueChip.rectTransform;
        chipRect.anchorMin = new Vector2(1f, 0.5f);
        chipRect.anchorMax = new Vector2(1f, 0.5f);
        chipRect.pivot = new Vector2(1f, 0.5f);
        chipRect.sizeDelta = new Vector2(260f, 56f);
        chipRect.anchoredPosition = new Vector2(-30f, 0f);

        valueText = CreateText("Value", valueChip.transform, value, 22f, ValueTextColor, TextAlignmentOptions.Center);
        StretchRect(valueText.rectTransform);
    }

    private RectTransform CreateCardRoot(Transform parent, string title, string description, float topOffset)
    {
        Image card = CreateImage($"Card_{title}", parent, SectionColor, 20, 16);
        RectTransform cardRect = card.rectTransform;
        cardRect.anchorMin = new Vector2(0.5f, 1f);
        cardRect.anchorMax = new Vector2(0.5f, 1f);
        cardRect.pivot = new Vector2(0.5f, 1f);
        cardRect.sizeDelta = new Vector2(CardWidth, 96f);
        cardRect.anchoredPosition = new Vector2(0f, -topOffset);

        TextMeshProUGUI titleText = CreateText("Title", cardRect, title, 28f, TitleColor, TextAlignmentOptions.Left);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.sizeDelta = new Vector2(360f, 34f);
        titleRect.anchoredPosition = new Vector2(30f, -18f);

        TextMeshProUGUI descriptionText = CreateText("Description", cardRect, description, 20f, DescriptionColor, TextAlignmentOptions.Left);
        RectTransform descriptionRect = descriptionText.rectTransform;
        descriptionRect.anchorMin = new Vector2(0f, 0f);
        descriptionRect.anchorMax = new Vector2(0f, 0f);
        descriptionRect.pivot = new Vector2(0f, 0f);
        descriptionRect.sizeDelta = new Vector2(520f, 28f);
        descriptionRect.anchoredPosition = new Vector2(30f, 18f);

        return cardRect;
    }

    private static TextMeshProUGUI FindCardDescription(RectTransform cardRoot)
    {
        if (cardRoot == null)
        {
            return null;
        }

        Transform description = cardRoot.Find("Description");
        return description != null ? description.GetComponent<TextMeshProUGUI>() : null;
    }

    private void CreateTabButton(Transform parent, SettingsTab tab, string label, float xOffset)
    {
        Button button = CreateButton($"Tab_{label}", parent, label, TabIdleColor, TabIdleTextColor, new Vector2(168f, 50f));
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 0.5f);
        buttonRect.anchorMax = new Vector2(0f, 0.5f);
        buttonRect.pivot = new Vector2(0f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(xOffset, 0f);

        button.onClick.AddListener(() => SelectTab(tab));

        tabImages[tab] = button.GetComponent<Image>();
        tabLabels[tab] = button.GetComponentInChildren<TextMeshProUGUI>();
    }

    private void SelectTab(SettingsTab tab)
    {
        if (tab != SettingsTab.Data && resetSaveArmed)
        {
            resetSaveArmed = false;
            RefreshDataPage();
        }

        currentTab = tab;
        RefreshTabState();
        ResetPageScrollPosition(tab);
    }

    private void BeginBindingCapture(GameInputAction action)
    {
        if (draftSettings == null)
        {
            return;
        }

        pendingBindingAction = action;
        captureReadyAt = Time.unscaledTime + 0.1f;
        RefreshControlsPage();
        RefreshFooterState();
    }

    private void HandleContinueRequested()
    {
        pendingBindingAction = null;
        resetSaveArmed = false;
        returnToMenuConfirmOpen = false;
        if (draftSettings != null && GameSettingsStore.IsDirty(savedSettings, draftSettings))
        {
            ApplyDraftAndRefreshState();
        }

        Hide(() => ContinueRequested?.Invoke());
    }

    private void HandleCancelRequested()
    {
        if (returnToMenuConfirmOpen)
        {
            HideReturnToMenuConfirm();
            return;
        }

        pendingBindingAction = null;
        resetSaveArmed = false;
        if (savedSettings != null)
        {
            GameSettingsStore.PreviewDraftAudio(savedSettings);
            draftSettings = GameSettingsStore.DiscardDraft(savedSettings);
        }

        Hide(() => ContinueRequested?.Invoke());
    }

    private void HandleApplyRequested()
    {
        pendingBindingAction = null;
        resetSaveArmed = false;
        returnToMenuConfirmOpen = false;
        if (draftSettings == null || !GameSettingsStore.IsDirty(savedSettings, draftSettings))
        {
            return;
        }

        bool shouldRefreshBackdrop = HasDisplayChanges(savedSettings, draftSettings);
        ApplyDraftAndRefreshState();
        RefreshAll();

        if (shouldRefreshBackdrop)
        {
            StartBackdropRefresh();
        }
    }

    private void HandleResetAll()
    {
        pendingBindingAction = null;
        resetSaveArmed = false;
        returnToMenuConfirmOpen = false;
        draftSettings = GameSettingsStore.CreateDefaultDraft();
        GameSettingsStore.PreviewDraftAudio(draftSettings);
        RefreshAll();
    }

    private void HandleReturnToMenuRequested()
    {
        if (!SupportsReturnToMainMenu())
        {
            return;
        }

        pendingBindingAction = null;
        resetSaveArmed = false;
        returnToMenuConfirmOpen = true;
        RefreshAll();
    }

    private void ExecuteReturnToMainMenu(bool applyPendingChanges)
    {
        bool isDirty = GameSettingsStore.IsDirty(savedSettings, draftSettings);
        bool isGameplayContext = currentContext == SettingsPanelContext.Gameplay || currentContext == SettingsPanelContext.PauseMenu;
        bool isPauseMenuContext = currentContext == SettingsPanelContext.PauseMenu;
        returnToMenuConfirmOpen = false;

        if (applyPendingChanges)
        {
            if (isDirty)
            {
                ApplyDraftAndRefreshState();
            }
        }
        else if (savedSettings != null)
        {
            GameSettingsStore.PreviewDraftAudio(savedSettings);
            draftSettings = GameSettingsStore.DiscardDraft(savedSettings);
        }

        Hide(() =>
        {
            if (isPauseMenuContext)
            {
                RuntimePauseMenu.CloseForSceneTransition();
            }
            else
            {
                ContinueRequested?.Invoke();
            }

            if (isGameplayContext)
            {
                RuntimeSessionResetService.ResetGameplayTransientState();
            }

            NavigateToMainMenu();
        });
    }

    private void HandleResetSaveRequested()
    {
        pendingBindingAction = null;
        returnToMenuConfirmOpen = false;

        if (!GameSaveResetService.HasAnySaveData())
        {
            resetSaveArmed = false;
            RefreshDataPage();
            return;
        }

        if (!resetSaveArmed)
        {
            resetSaveArmed = true;
            RefreshDataPage();
            return;
        }

        resetSaveArmed = false;
        GameSaveResetService.ResetAllSaveData();
        bool isPauseMenuContext = currentContext == SettingsPanelContext.PauseMenu;
        HideImmediate();
        if (isPauseMenuContext)
        {
            RuntimePauseMenu.CloseForSceneTransition();
        }
        else
        {
            ContinueRequested?.Invoke();
        }

        NavigateToMainMenu();
    }

    private void ApplyDraftAndRefreshState()
    {
        if (draftSettings == null)
        {
            return;
        }

        GameSettingsStore.ApplyDraft(draftSettings);
        savedSettings = GameSettingsStore.LoadSavedSettings();
        draftSettings = GameSettingsStore.CreateDraftFromSaved();
    }

    private void ChangeMasterVolume(float delta)
    {
        if (draftSettings == null)
        {
            return;
        }

        draftSettings.masterVolume = Mathf.Clamp01(draftSettings.masterVolume + delta);
        GameSettingsStore.PreviewDraftAudio(draftSettings);
        RefreshAudioPage();
        RefreshSubtitle();
        RefreshFooterState();
    }

    private void ChangeMusicVolume(float delta)
    {
        if (draftSettings == null)
        {
            return;
        }

        draftSettings.musicVolume = Mathf.Clamp01(draftSettings.musicVolume + delta);
        GameSettingsStore.PreviewDraftAudio(draftSettings);
        RefreshAudioPage();
        RefreshSubtitle();
        RefreshFooterState();
    }

    private void ChangeSfxVolume(float delta)
    {
        if (draftSettings == null)
        {
            return;
        }

        draftSettings.sfxVolume = Mathf.Clamp01(draftSettings.sfxVolume + delta);
        GameSettingsStore.PreviewDraftAudio(draftSettings);
        RefreshAudioPage();
        RefreshSubtitle();
        RefreshFooterState();
    }

    private void ChangeResolution(int delta)
    {
        if (draftSettings == null)
        {
            return;
        }

        draftSettings.resolutionIndex = (draftSettings.resolutionIndex + delta + GameSettingsStore.ResolutionOptionCount) % GameSettingsStore.ResolutionOptionCount;
        RefreshDisplayPage();
        RefreshSubtitle();
        RefreshFooterState();
    }

    private void CycleDisplayMode(int delta)
    {
        if (draftSettings == null)
        {
            return;
        }

        int raw = ((int)draftSettings.displayMode + delta + 2) % 2;
        draftSettings.displayMode = (GameDisplayMode)raw;
        RefreshDisplayPage();
        RefreshSubtitle();
        RefreshFooterState();
    }

    private void ChangeViewZoom(int delta)
    {
        if (draftSettings == null)
        {
            return;
        }

        draftSettings.viewZoomIndex = (draftSettings.viewZoomIndex + delta + GameSettingsStore.ViewZoomOptionCount) % GameSettingsStore.ViewZoomOptionCount;
        RefreshDisplayPage();
        RefreshSubtitle();
        RefreshFooterState();
    }

    private void RefreshAll()
    {
        RefreshSubtitle();
        RefreshAudioPage();
        RefreshDisplayPage();
        RefreshControlsPage();
        RefreshDataPage();
        RefreshTabState();
        RefreshFooterState();
        RefreshReturnToMenuConfirmState();
    }

    private void RefreshSubtitle()
    {
        if (subtitleText == null)
        {
            return;
        }

        bool isDirty = GameSettingsStore.IsDirty(savedSettings, draftSettings);
        switch (currentContext)
        {
            case SettingsPanelContext.MainMenu:
                subtitleText.text = isDirty
                    ? "存在未应用更改，关闭时会自动应用。"
                    : "当前没有未应用更改。";
                break;
            case SettingsPanelContext.BaseHub:
            {
                GameSettingsDraft source = draftSettings ?? savedSettings ?? GameSettingsStore.LoadSavedSettings();
                string pauseKey = GameSettingsStore.GetKeyDisplayName(source.GetBinding(GameInputAction.Pause));
                subtitleText.text = isDirty
                    ? $"存在未应用更改，关闭设置会自动应用。当前暂停键：{pauseKey}"
                    : $"当前没有未应用更改。当前暂停键：{pauseKey}";
                break;
            }
            case SettingsPanelContext.PauseMenu:
            {
                GameSettingsDraft source = draftSettings ?? savedSettings ?? GameSettingsStore.LoadSavedSettings();
                string pauseKey = GameSettingsStore.GetKeyDisplayName(source.GetBinding(GameInputAction.Pause));
                subtitleText.text = isDirty
                    ? $"存在未应用更改，返回暂停页前会自动应用。当前暂停键：{pauseKey}"
                    : $"当前没有未应用更改。当前暂停键：{pauseKey}";
                break;
            }
            default:
            {
                GameSettingsDraft source = draftSettings ?? savedSettings ?? GameSettingsStore.LoadSavedSettings();
                string pauseKey = GameSettingsStore.GetKeyDisplayName(source.GetBinding(GameInputAction.Pause));
                subtitleText.text = isDirty
                    ? $"存在未应用更改，继续游戏会自动应用。当前暂停键：{pauseKey}"
                    : $"当前没有未应用更改。当前暂停键：{pauseKey}";
                break;
            }
        }
    }

    private void RefreshAudioPage()
    {
        if (draftSettings == null)
        {
            return;
        }

        if (masterVolumeValueText != null)
        {
            masterVolumeValueText.text = $"{Mathf.RoundToInt(draftSettings.masterVolume * 100f)}%";
        }

        if (musicVolumeValueText != null)
        {
            musicVolumeValueText.text = $"{Mathf.RoundToInt(draftSettings.musicVolume * 100f)}%";
        }

        if (sfxVolumeValueText != null)
        {
            sfxVolumeValueText.text = $"{Mathf.RoundToInt(draftSettings.sfxVolume * 100f)}%";
        }
    }

    private void RefreshDisplayPage()
    {
        if (draftSettings == null)
        {
            return;
        }

        if (resolutionValueText != null)
        {
            resolutionValueText.text = GameSettingsStore.GetResolutionLabel(draftSettings.resolutionIndex);
        }

        if (displayModeValueText != null)
        {
            displayModeValueText.text = GameSettingsStore.GetDisplayModeLabel(draftSettings.displayMode);
        }

        if (viewZoomValueText != null)
        {
            viewZoomValueText.text = GameSettingsStore.GetViewZoomLabel(draftSettings.viewZoomIndex);
        }

        if (runtimeInfoValueText != null)
        {
            StringBuilder builder = new StringBuilder(160);
            builder.Append("待应用值：")
                .Append(GameSettingsStore.GetResolutionLabel(draftSettings.resolutionIndex))
                .Append(" / ")
                .Append(GameSettingsStore.GetDisplayModeLabel(draftSettings.displayMode))
                .Append(" / ")
                .Append(GameSettingsStore.GetViewZoomLabel(draftSettings.viewZoomIndex))
                .Append('\n')
                .Append("实际分辨率：")
                .Append(GameSettingsStore.GetCurrentRuntimeResolutionLabel())
                .Append("    实际模式：")
                .Append(GameSettingsStore.GetCurrentRuntimeDisplayModeLabel())
                .Append('\n')
                .Append("实际比例：")
                .Append(GameSettingsStore.GetCurrentRuntimeAspectLabel())
                .Append("    当前视野：")
                .Append(GameSettingsStore.GetCurrentRuntimeViewZoomLabel());
            runtimeInfoValueText.text = builder.ToString();
        }
    }

    private void RefreshControlsPage()
    {
        if (draftSettings == null)
        {
            return;
        }

        foreach (KeyValuePair<GameInputAction, TextMeshProUGUI> entry in bindingValueTexts)
        {
            if (entry.Value == null)
            {
                continue;
            }

            if (pendingBindingAction.HasValue && pendingBindingAction.Value == entry.Key)
            {
                entry.Value.text = "等待输入...";
            }
            else
            {
                entry.Value.text = GameSettingsStore.GetKeyDisplayName(draftSettings.GetBinding(entry.Key));
            }
        }

        if (captureHintText != null)
        {
            captureHintText.text = pendingBindingAction.HasValue
                ? $"正在为“{GameSettingsStore.GetActionDisplayName(pendingBindingAction.Value)}”等待输入..."
                : "点击右侧按钮后，按任意键或鼠标键进行绑定";
        }
    }

    private void RefreshDataPage()
    {
        bool hasSaveData = GameSaveResetService.HasAnySaveData();

        if (dataResetDescriptionText != null)
        {
            dataResetDescriptionText.text = currentContext == SettingsPanelContext.MainMenu
                ? "清空本地建筑进度、关卡选择、武器状态和留念相册，设置项会保留。"
                : "清空本地建筑进度、关卡选择、武器状态和留念相册，设置项会保留。";
        }

        if (dataCompletionDescriptionText != null)
        {
            dataCompletionDescriptionText.text = currentContext == SettingsPanelContext.MainMenu
                ? "重置结束后会留在主菜单，便于从干净状态重新开始"
                : "重置结束后会返回主菜单，便于从干净状态重新开始";
        }

        if (dataCompletionValueText != null)
        {
            dataCompletionValueText.text = currentContext == SettingsPanelContext.MainMenu
                ? "留在主菜单"
                : "返回主菜单";
        }

        if (saveResetSummaryText != null)
        {
            if (!hasSaveData)
            {
                saveResetSummaryText.text = "当前没有可重置的数据。";
            }
            else if (resetSaveArmed)
            {
                saveResetSummaryText.text = "再次点击将立即清空本地进度与相册截图，并返回主菜单。此操作不可恢复。";
            }
            else
            {
                saveResetSummaryText.text = "会清空本地进度和相册截图，设置项会保留。";
            }
        }

        if (resetSaveButtonLabel != null)
        {
            resetSaveButtonLabel.text = !hasSaveData
                ? "没有存档"
                : resetSaveArmed
                    ? "确认重置"
                    : "重置存档";
        }

        if (resetSaveButton != null)
        {
            SetButtonState(
                resetSaveButton,
                resetSaveButtonImage,
                resetSaveButtonLabel,
                hasSaveData,
                !hasSaveData
                    ? DisabledButtonColor
                    : resetSaveArmed
                        ? DangerButtonArmedColor
                        : DangerButtonColor,
                hasSaveData ? DangerButtonTextColor : DisabledButtonTextColor);
        }
    }

    private void RefreshTabState()
    {
        foreach (KeyValuePair<SettingsTab, RectTransform> entry in pageRoots)
        {
            entry.Value.gameObject.SetActive(entry.Key == currentTab);
        }

        foreach (KeyValuePair<SettingsTab, Image> entry in tabImages)
        {
            bool selected = entry.Key == currentTab;
            entry.Value.color = selected ? TabActiveColor : TabIdleColor;

            if (tabLabels.TryGetValue(entry.Key, out TextMeshProUGUI label))
            {
                label.color = selected ? TabActiveTextColor : TabIdleTextColor;
            }
        }
    }

    private void ResetPageScrollPosition(SettingsTab tab)
    {
        if (!pageScrollRects.TryGetValue(tab, out ScrollRect scrollRect) || scrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private void RefreshFooterState()
    {
        bool isDirty = GameSettingsStore.IsDirty(savedSettings, draftSettings);

        if (applyButton != null)
        {
            SetButtonState(
                applyButton,
                applyButtonImage,
                applyButtonLabel,
                isDirty,
                isDirty ? PrimaryButtonColor : DisabledButtonColor,
                isDirty ? PrimaryButtonTextColor : DisabledButtonTextColor);
        }

        string continueLabel = ResolveContinueButtonLabel(isDirty);
        Color continueColor = isDirty ? PrimaryButtonColor : SecondaryButtonColor;
        Color continueTextColor = isDirty ? PrimaryButtonTextColor : SecondaryButtonTextColor;
        SetButtonState(
            continueButton,
            continueButtonImage,
            continueButtonLabel,
            true,
            continueColor,
            continueTextColor,
            continueLabel);

        bool showReturnToMenu = SupportsReturnToMainMenu();
        if (returnToMenuButton != null)
        {
            returnToMenuButton.gameObject.SetActive(showReturnToMenu);
        }

        if (showReturnToMenu)
        {
            SetButtonState(
                returnToMenuButton,
                returnToMenuButtonImage,
                returnToMenuButtonLabel,
                true,
                SecondaryButtonColor,
                SecondaryButtonTextColor,
                "返回主界面");
        }
    }

    private void RefreshReturnToMenuConfirmState()
    {
        bool isDirty = GameSettingsStore.IsDirty(savedSettings, draftSettings);
        SetReturnToMenuConfirmVisible(returnToMenuConfirmOpen && SupportsReturnToMainMenu());

        if (returnToMenuConfirmTitleText == null || returnToMenuConfirmBodyText == null)
        {
            return;
        }

        if (currentContext == SettingsPanelContext.Gameplay || currentContext == SettingsPanelContext.PauseMenu)
        {
            returnToMenuConfirmTitleText.text = "确认返回主界面";
            returnToMenuConfirmBodyText.text = isDirty
                ? "当前战斗会直接结束并回到主界面。\n未应用改动会先写入设置，不会重置存档或相册。"
                : "当前战斗会直接结束并回到主界面。\n不会重置存档或相册。";
        }
        else
        {
            returnToMenuConfirmTitleText.text = "确认返回主界面";
            returnToMenuConfirmBodyText.text = isDirty
                ? "会先处理当前未应用改动，然后回到主界面。\n不会重置存档或相册。"
                : "会直接回到主界面。\n不会重置存档或相册。";
        }

        SetButtonState(
            returnToMenuConfirmPrimaryButton,
            returnToMenuConfirmPrimaryButtonImage,
            returnToMenuConfirmPrimaryButtonLabel,
            true,
            PrimaryButtonColor,
            PrimaryButtonTextColor,
            isDirty ? "应用并返回" : "返回主界面");

        bool showDiscardButton = isDirty;
        if (returnToMenuConfirmSecondaryButton != null)
        {
            returnToMenuConfirmSecondaryButton.gameObject.SetActive(showDiscardButton);
        }

        if (showDiscardButton)
        {
            SetButtonState(
                returnToMenuConfirmSecondaryButton,
                returnToMenuConfirmSecondaryButtonImage,
                returnToMenuConfirmSecondaryButtonLabel,
                true,
                SecondaryButtonColor,
                SecondaryButtonTextColor,
                "不保存返回");
        }

        SetButtonState(
            returnToMenuConfirmCancelButton,
            returnToMenuConfirmCancelButtonImage,
            returnToMenuConfirmCancelButtonLabel,
            true,
            SecondaryButtonColor,
            SecondaryButtonTextColor,
            "取消");
    }

    private string ResolveContinueButtonLabel(bool isDirty)
    {
        switch (currentContext)
        {
            case SettingsPanelContext.MainMenu:
                return "关闭";
            case SettingsPanelContext.BaseHub:
                return isDirty ? "应用并关闭" : "关闭设置";
            case SettingsPanelContext.PauseMenu:
                return isDirty ? "应用并返回" : "返回暂停页";
            default:
                return isDirty ? "应用并继续" : "继续游戏";
        }
    }

    private bool SupportsReturnToMainMenu()
    {
        return currentContext != SettingsPanelContext.MainMenu;
    }

    private void HideReturnToMenuConfirm()
    {
        if (!returnToMenuConfirmOpen)
        {
            return;
        }

        returnToMenuConfirmOpen = false;
        RefreshReturnToMenuConfirmState();
    }

    private void SetReturnToMenuConfirmVisible(bool visible)
    {
        if (returnToMenuConfirmRoot == null || returnToMenuConfirmCanvasGroup == null)
        {
            return;
        }

        returnToMenuConfirmRoot.gameObject.SetActive(visible);
        returnToMenuConfirmCanvasGroup.alpha = visible ? 1f : 0f;
        returnToMenuConfirmCanvasGroup.interactable = visible;
        returnToMenuConfirmCanvasGroup.blocksRaycasts = visible;
    }

    private void NavigateToMainMenu()
    {
        SceneLoader loader = SceneLoader.EnsureInstance();
        if (loader != null)
        {
            loader.ToMenu();
            return;
        }

        SceneManager.LoadScene("MainScene");
    }

    private static void SetButtonState(
        Button button,
        Image image,
        TextMeshProUGUI label,
        bool interactable,
        Color backgroundColor,
        Color textColor)
    {
        SetButtonState(button, image, label, interactable, backgroundColor, textColor, null);
    }

    private static void SetButtonState(
        Button button,
        Image image,
        TextMeshProUGUI label,
        bool interactable,
        Color backgroundColor,
        Color textColor,
        string overrideText)
    {
        if (button == null || image == null || label == null)
        {
            return;
        }

        button.interactable = interactable;
        image.color = backgroundColor;
        label.color = textColor;
        if (!string.IsNullOrEmpty(overrideText))
        {
            label.text = overrideText;
        }
    }

    private static bool HasDisplayChanges(GameSettingsDraft saved, GameSettingsDraft draft)
    {
        if (saved == null || draft == null)
        {
            return false;
        }

        return saved.resolutionIndex != draft.resolutionIndex ||
               saved.displayMode != draft.displayMode ||
               saved.viewZoomIndex != draft.viewZoomIndex;
    }

    private bool TryCapturePressedKey(out KeyCode keyCode)
    {
        for (int i = 0; i < BindableMouseKeys.Length; i++)
        {
            KeyCode mouseKey = BindableMouseKeys[i];
            int mouseIndex = (int)mouseKey - (int)KeyCode.Mouse0;
            if (Input.GetMouseButtonDown(mouseIndex))
            {
                keyCode = mouseKey;
                return true;
            }
        }

        KeyCode[] keyboardKeys = GetKeyboardKeys();
        for (int i = 0; i < keyboardKeys.Length; i++)
        {
            if (Input.GetKeyDown(keyboardKeys[i]))
            {
                keyCode = keyboardKeys[i];
                return true;
            }
        }

        keyCode = KeyCode.None;
        return false;
    }

    private bool IsPanelShown()
    {
        return canvas != null &&
               canvas.gameObject.activeInHierarchy &&
               canvasGroup != null &&
               canvasGroup.alpha > 0.01f;
    }

    private void SetPanelVisibility(bool show)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = show ? 1f : 0f;
        canvasGroup.interactable = show;
        canvasGroup.blocksRaycasts = show;
    }

    private void StartShowSequence()
    {
        StopShowSequence();
        StopShowAnimation();
        StopHideAnimation();
        pendingHideCallback = null;
        showSequenceCoroutine = StartCoroutine(ShowSequenceRoutine());
    }

    private void StopShowSequence()
    {
        if (showSequenceCoroutine == null)
        {
            return;
        }

        StopCoroutine(showSequenceCoroutine);
        showSequenceCoroutine = null;
    }

    private void StartBackdropRefresh()
    {
        StopBackdropRefresh();
        backdropRefreshCoroutine = StartCoroutine(RefreshBackdropRoutine());
    }

    private void StartLiveBackdropRefresh()
    {
        StopLiveBackdropRefresh();
        liveBackdropCoroutine = StartCoroutine(LiveBackdropRoutine());
    }

    private void StopBackdropRefresh()
    {
        if (backdropRefreshCoroutine == null)
        {
            return;
        }

        StopCoroutine(backdropRefreshCoroutine);
        backdropRefreshCoroutine = null;
    }

    private void StopLiveBackdropRefresh()
    {
        if (liveBackdropCoroutine == null)
        {
            return;
        }

        StopCoroutine(liveBackdropCoroutine);
        liveBackdropCoroutine = null;
    }

    private IEnumerator ShowSequenceRoutine()
    {
        yield return new WaitForEndOfFrame();
        CaptureBlurBackdrop();
        ApplyAnimationState(0f);
        PlayShowAnimation();
        showSequenceCoroutine = null;
    }

    private IEnumerator RefreshBackdropRoutine()
    {
        for (int frame = 0; frame < DisplayRefreshFrameBudget; frame++)
        {
            yield return new WaitForEndOfFrame();
            ScreenAdaptationManager.RefreshNow();
            RefreshDisplayPage();

            if (MatchesCurrentRuntimeDisplay(savedSettings))
            {
                break;
            }
        }

        CaptureBlurBackdrop();
        RefreshDisplayPage();
        ApplyAnimationState(1f);
        backdropRefreshCoroutine = null;
    }

    private IEnumerator LiveBackdropRoutine()
    {
        while (IsPanelShown())
        {
            yield return new WaitForEndOfFrame();
            CaptureBlurBackdrop();
        }

        liveBackdropCoroutine = null;
    }

    private static bool MatchesCurrentRuntimeDisplay(GameSettingsDraft appliedSettings)
    {
        if (appliedSettings == null)
        {
            return true;
        }

        Vector2Int resolution = GameSettingsStore.GetResolutionOption(appliedSettings.resolutionIndex);
        bool resolutionMatches = Screen.width == resolution.x && Screen.height == resolution.y;
        bool modeMatches = Screen.fullScreen == (appliedSettings.displayMode == GameDisplayMode.Fullscreen);
        return resolutionMatches && modeMatches;
    }

    private void PlayShowAnimation()
    {
        StopShowAnimation();
        StopHideAnimation();
        ApplyAnimationState(0f);
        showAnimationCoroutine = StartCoroutine(AnimateShowRoutine());
    }

    private void StopShowAnimation()
    {
        if (showAnimationCoroutine == null)
        {
            return;
        }

        StopCoroutine(showAnimationCoroutine);
        showAnimationCoroutine = null;
    }

    private void StopHideAnimation()
    {
        if (hideAnimationCoroutine == null)
        {
            return;
        }

        StopCoroutine(hideAnimationCoroutine);
        hideAnimationCoroutine = null;
    }

    private IEnumerator AnimateShowRoutine()
    {
        float elapsed = 0f;
        while (elapsed < ShowAnimationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / ShowAnimationDuration);
            ApplyAnimationState(progress);
            yield return null;
        }

        ApplyAnimationState(1f);
        showAnimationCoroutine = null;
    }

    private IEnumerator AnimateHideRoutine()
    {
        float elapsed = 0f;
        float startProgress = animationProgress;
        while (elapsed < ShowAnimationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / ShowAnimationDuration);
            float progress = Mathf.Lerp(startProgress, 0f, t);
            ApplyAnimationState(progress);
            yield return null;
        }

        Action callback = pendingHideCallback;
        pendingHideCallback = null;
        hideAnimationCoroutine = null;
        CompleteHideState(false, false);
        callback?.Invoke();
    }

    private void ApplyAnimationState(float progress)
    {
        animationProgress = Mathf.Clamp01(progress);
        RuntimeModalStyle.ApplyBackdropState(blurBackdropImage, blurTintImage, overlayImage, animationProgress);
        RuntimeModalStyle.ApplyPanelState(
            panelCanvasGroup,
            panelRectTransform,
            panelVisibleAnchoredPosition,
            Vector3.one,
            animationProgress);

        if (panelOutline != null)
        {
            float easedProgress = RuntimeModalStyle.EaseOutCubic(animationProgress);
            panelOutline.effectColor = RuntimeModalStyle.WithAlpha(BorderColor, BorderColor.a * easedProgress);
        }
    }

    private void CaptureBlurBackdrop()
    {
        if (blurBackdropImage == null)
        {
            return;
        }

        blurredBackdropTexture = RuntimeModalStyle.RefreshRealtimeBlurBackdrop(blurredBackdropTexture);
        if (blurBackdropImage.texture != blurredBackdropTexture)
        {
            blurBackdropImage.texture = blurredBackdropTexture;
        }
    }

    private Texture2D CaptureBackdropTexture()
    {
        Camera captureCamera = ResolveBackdropCamera();
        if (captureCamera == null)
        {
            return ScreenCapture.CaptureScreenshotAsTexture();
        }

        int captureWidth = Mathf.Max(Screen.width, 1);
        int captureHeight = Mathf.Max(Screen.height, 1);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = captureCamera.targetTexture;
        RenderTexture captureTexture = RenderTexture.GetTemporary(captureWidth, captureHeight, 24, RenderTextureFormat.ARGB32);
        captureTexture.filterMode = FilterMode.Bilinear;
        captureTexture.wrapMode = TextureWrapMode.Clamp;

        try
        {
            captureCamera.targetTexture = captureTexture;
            captureCamera.Render();
            RenderTexture.active = captureTexture;

            Texture2D result = new Texture2D(captureWidth, captureHeight, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0f, 0f, captureWidth, captureHeight), 0, 0, false);
            result.Apply(false, false);
            result.filterMode = FilterMode.Bilinear;
            result.wrapMode = TextureWrapMode.Clamp;
            return result;
        }
        finally
        {
            captureCamera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(captureTexture);
        }
    }

    private static Camera ResolveBackdropCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.isActiveAndEnabled)
        {
            return mainCamera;
        }

        Camera[] cameras = FindObjectsOfType<Camera>(true);
        Camera fallback = null;
        float highestDepth = float.MinValue;
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera candidate = cameras[i];
            if (candidate == null || !candidate.isActiveAndEnabled || !candidate.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (candidate.depth < highestDepth)
            {
                continue;
            }

            highestDepth = candidate.depth;
            fallback = candidate;
        }

        return fallback;
    }

    private void ReleaseBlurBackdrop()
    {
        RuntimeModalStyle.ReleaseBlurBackdrop(blurBackdropImage, ref capturedBackdropTexture, ref blurredBackdropTexture);
    }

    private static float EaseOutCubic(float t)
    {
        float inverse = 1f - Mathf.Clamp01(t);
        return 1f - inverse * inverse * inverse;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static RectTransform CreateContainer(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        GameObject container = new GameObject(name, typeof(RectTransform));
        container.transform.SetParent(parent, false);
        RectTransform rect = container.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        return rect;
    }

    private static Image CreateImage(string name, Transform parent, Color color, int radius, int border)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();

        if (radius <= 0 || border <= 0)
        {
            image.color = color;
        }
        else
        {
            RuntimeUiSpriteFactory.ApplyRoundedSprite(image, color, radius, border, 1.2f);
        }

        return image;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        string label,
        Color backgroundColor,
        Color textColor,
        Vector2 size)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image buttonImage = buttonObject.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(buttonImage, backgroundColor, 16, 14, 1.2f);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Button button = buttonObject.GetComponent<Button>();

        TextMeshProUGUI text = CreateText("Label", buttonObject.transform, label, 24f, textColor, TextAlignmentOptions.Center);
        StretchRect(text.rectTransform);
        return button;
    }

    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        string value,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = GetRuntimeFont();
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        return text;
    }

    private static TMP_FontAsset GetRuntimeFont()
    {
        if (runtimeFontAsset != null)
        {
            return runtimeFontAsset;
        }

        TMP_FontAsset defaultFontAsset = TMP_Settings.defaultFontAsset;
        if (defaultFontAsset != null)
        {
            TryWarmupFontCharacters(defaultFontAsset);
            if (defaultFontAsset.HasCharacters(RequiredCharacters))
            {
                runtimeFontAsset = defaultFontAsset;
                return runtimeFontAsset;
            }
        }

        TMP_FontAsset[] loadedFontAssets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < loadedFontAssets.Length; i++)
        {
            TMP_FontAsset fontAsset = loadedFontAssets[i];
            if (fontAsset == null || !fontAsset.name.Contains("NotoSansSC"))
            {
                continue;
            }

            TryWarmupFontCharacters(fontAsset);
            if (!fontAsset.HasCharacters(RequiredCharacters))
            {
                continue;
            }

            runtimeFontAsset = fontAsset;
            return runtimeFontAsset;
        }

        Font[] loadedFonts = Resources.FindObjectsOfTypeAll<Font>();
        for (int i = 0; i < loadedFonts.Length; i++)
        {
            Font font = loadedFonts[i];
            if (font == null)
            {
                continue;
            }

            string fontName = font.name ?? string.Empty;
            if (!fontName.Contains("NotoSansSC") && !fontName.Contains("Noto Sans SC"))
            {
                continue;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);
            if (fontAsset == null)
            {
                continue;
            }

            fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset>();
            TryWarmupFontCharacters(fontAsset);
            if (!fontAsset.HasCharacters(RequiredCharacters))
            {
                continue;
            }

            runtimeFontAsset = fontAsset;
            return runtimeFontAsset;
        }

        for (int i = 0; i < RuntimeFontNames.Length; i++)
        {
            Font font;
            try
            {
                font = Font.CreateDynamicFontFromOSFont(RuntimeFontNames[i], 90);
            }
            catch (Exception)
            {
                continue;
            }

            if (font == null)
            {
                continue;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);
            if (fontAsset == null)
            {
                continue;
            }

            fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset>();
            TryWarmupFontCharacters(fontAsset);
            if (!fontAsset.HasCharacters(RequiredCharacters))
            {
                continue;
            }

            runtimeFontAsset = fontAsset;
            return runtimeFontAsset;
        }

        runtimeFontAsset = TMP_Settings.defaultFontAsset;
        return runtimeFontAsset;
    }

    private static void TryWarmupFontCharacters(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null)
        {
            return;
        }

        if (fontAsset.atlasPopulationMode == AtlasPopulationMode.Dynamic)
        {
            fontAsset.TryAddCharacters(RequiredCharacters);
        }
    }

    private static KeyCode[] GetKeyboardKeys()
    {
        if (cachedKeyboardKeyCodes != null)
        {
            return cachedKeyboardKeyCodes;
        }

        Array values = Enum.GetValues(typeof(KeyCode));
        List<KeyCode> keys = new List<KeyCode>(values.Length);
        for (int i = 0; i < values.Length; i++)
        {
            KeyCode key = (KeyCode)values.GetValue(i);
            if (key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6)
            {
                continue;
            }

            if (key == KeyCode.None)
            {
                continue;
            }

            keys.Add(key);
        }

        cachedKeyboardKeyCodes = keys.ToArray();
        return cachedKeyboardKeyCodes;
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
