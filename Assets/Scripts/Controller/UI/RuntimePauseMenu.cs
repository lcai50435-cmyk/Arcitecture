using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class RuntimePauseMenu : MonoBehaviour
{
    private enum PausePage
    {
        Menu,
        About,
        ReturnConfirm,
        QuitConfirm
    }

    private const string CanvasName = "RuntimePauseMenuCanvas";
    // 暂停面板必须高于所有运行时弹窗，避免旧 UI 遮罩吞掉 Esc 打开的面板点击。
    private const int SortingOrder = Dialog.TopmostRuntimeDialogSortingOrder + 50;
    private const string PauseReason = "RuntimePauseMenu";
    private const float PanelWidth = 1120f;
    private const float PanelHeight = 720f;
    private const float MenuButtonWidth = 430f;
    private const float MenuButtonHeight = 64f;
    private const float MenuRevealHoldAfterFocus = 0.30f;
    private const float MenuButtonRevealDuration = 0.13f;
    private const float MenuButtonRevealOffsetY = -42f;
    private const float MenuButtonRevealCurveX = 18f;
    private const float MenuButtonRevealStartScale = 0.965f;
    private const float MenuButtonInteractionRevealProgress = 0f;

    private static readonly Color PrimaryButtonColor = Color.white;
    private static readonly Color PrimaryButtonTextColor = new Color(0.14f, 0.11f, 0.08f, 1f);
    private static readonly Color SecondaryButtonColor = new Color(0.90f, 0.77f, 0.55f, 0.92f);
    private static readonly Color SecondaryButtonTextColor = new Color(0.18f, 0.15f, 0.11f, 0.98f);
    private static readonly Color DangerButtonColor = new Color(0.93f, 0.62f, 0.48f, 0.94f);
    private static readonly Color DangerButtonTextColor = new Color(0.24f, 0.08f, 0.06f, 1f);
    private static readonly Color TitleColor = new Color(0.97f, 0.94f, 0.86f, 1f);
    private static readonly Color DescriptionColor = new Color(0.84f, 0.79f, 0.70f, 1f);

    private sealed class MenuButtonRevealItem
    {
        public MenuButtonRevealItem(RectTransform rectTransform, CanvasGroup canvasGroup)
        {
            RectTransform = rectTransform;
            CanvasGroup = canvasGroup;
            VisiblePosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
            VisibleScale = rectTransform != null ? rectTransform.localScale : Vector3.one;
        }

        public RectTransform RectTransform { get; }
        public CanvasGroup CanvasGroup { get; }
        public Vector2 VisiblePosition { get; }
        public Vector3 VisibleScale { get; }
    }

    public static RuntimePauseMenu Instance { get; private set; }
    public static bool IsPauseOpen => Instance != null && Instance.isOpen;

    private RuntimeModalShell modalShell;
    private RuntimeSettingsPanel settingsPanel;
    private Canvas canvas;
    private CanvasGroup panelCanvasGroup;
    private RectTransform menuRoot;
    private RectTransform aboutRoot;
    private RectTransform confirmRoot;
    private TextMeshProUGUI confirmTitleText;
    private TextMeshProUGUI confirmBodyText;
    private Button confirmPrimaryButton;
    private TextMeshProUGUI confirmPrimaryLabel;
    private Image confirmPrimaryImage;

    private readonly List<MenuButtonRevealItem> menuButtonRevealItems = new List<MenuButtonRevealItem>();
    private Coroutine menuRevealCoroutine;
    private bool isOpen;
    private bool visible;
    private bool showingSettings;
    private PausePage currentPage = PausePage.Menu;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureInstance();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstance();
        if (Instance != null)
        {
            Instance.SetVisible(IsSupportedScene(scene.name));
        }
    }

    public static void ConsumeOpenHotkey()
    {
        // 保留给旧调用方；Esc 现在始终优先打开暂停页。
    }

    public static void CloseForSceneTransition()
    {
        if (Instance != null)
        {
            Instance.HideImmediate();
        }
    }

    public static bool TryOpenFromExternal()
    {
        RuntimePauseMenu menu = EnsureInstance();
        if (menu == null || !menu.CanOpenFromExternal())
        {
            return false;
        }

        menu.PauseGame();
        return true;
    }

    public static bool TryOpenFromPauseKey()
    {
        RuntimePauseMenu menu = EnsureInstance();
        if (menu == null || !menu.CanOpenFromPauseKey())
        {
            return false;
        }

        menu.PauseGame();
        return true;
    }

    public static RuntimePauseMenu EnsureInstance()
    {
        bool supportedScene = IsSupportedScene(SceneManager.GetActiveScene().name);

        if (Instance != null)
        {
            Instance.EnsureUi();
            Instance.SetVisible(supportedScene);
            return Instance;
        }

        RuntimePauseMenu existing = FindObjectOfType<RuntimePauseMenu>(true);
        if (existing != null)
        {
            Instance = existing;
            Instance.EnsureUi();
            Instance.SetVisible(supportedScene);
            return existing;
        }

        GameObject runtimeObject = new GameObject("RuntimePauseMenu");
        Instance = runtimeObject.AddComponent<RuntimePauseMenu>();
        Instance.EnsureUi();
        Instance.SetVisible(supportedScene);
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureUi();
        SetVisible(IsSupportedScene(SceneManager.GetActiveScene().name));
        HideImmediate();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            TryOpenForFocusLoss();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            TryOpenForFocusLoss();
        }
    }

    private void Update()
    {
        if (!visible || GameplayStageIntroDirector.IsIntroActive)
        {
            return;
        }

        if (isOpen || showingSettings)
        {
            RuntimeUiEventSystemBootstrapper.Ensure();
        }

        KeyCode pauseKey = GameSettingsStore.GetKeyBinding(GameInputAction.Pause);
        if (pauseKey == KeyCode.None || !Input.GetKeyDown(pauseKey))
        {
            return;
        }

        if (showingSettings)
        {
            if (settingsPanel != null && settingsPanel.IsCapturingBinding)
            {
                return;
            }

            settingsPanel?.RequestContinueGame();
            return;
        }

        if (isOpen)
        {
            ConsumeOpenHotkey();
            if (currentPage == PausePage.Menu)
            {
                ResumeGame();
            }
            else
            {
                ShowMenuPage();
            }

            return;
        }

        TryOpenFromPauseKey();
    }

    private void OnDestroy()
    {
        if (settingsPanel != null)
        {
            settingsPanel.ContinueRequested -= HandleSettingsClosed;
        }

        RuntimeGameplayPauseController.ReleasePause(PauseReason);
        RuntimeCameraController.EnsureInstance().SetPauseFocusActive(false, true);

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void TryOpenForFocusLoss()
    {
        EnsureUi();
        if (CanOpenForFocusLoss())
        {
            PauseGame();
        }
    }

    private bool CanOpenForFocusLoss()
    {
        if (!CanOpenFromExternal())
        {
            return false;
        }

        if (GameplayFailureController.IsFailureActive || GameplayStageIntroDirector.IsIntroActive)
        {
            return false;
        }

        if (settingsPanel != null && (settingsPanel.IsShown || settingsPanel.IsCapturingBinding))
        {
            return false;
        }

        return !RuntimeUiInputGuard.IsBlockingGameplayUiOpen();
    }

    private bool CanOpenFromExternal()
    {
        return visible &&
               !isOpen &&
               !showingSettings &&
               IsSupportedScene(SceneManager.GetActiveScene().name) &&
               (settingsPanel == null || (!settingsPanel.IsShown && !settingsPanel.IsCapturingBinding)) &&
               !RuntimeUiInputGuard.IsBlockingGameplayUiOpen();
    }

    private bool CanOpenFromPauseKey()
    {
        return visible &&
               !isOpen &&
               !showingSettings &&
               IsSupportedScene(SceneManager.GetActiveScene().name) &&
               (settingsPanel == null || (!settingsPanel.IsShown && !settingsPanel.IsCapturingBinding));
    }

    private void PauseGame()
    {
        if (isOpen)
        {
            return;
        }

        EnsureUi();
        isOpen = true;
        showingSettings = false;
        RuntimeGameplayPauseController.RequestPause(PauseReason);
        RuntimeCameraController.EnsureInstance().SetPauseFocusActive(true);
        ShowMenuPage(true);
        ShowShell();
        StartMenuRevealSequence();
    }

    private void ResumeGame()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        showingSettings = false;
        StopMenuReveal(false);
        RuntimeCameraController.EnsureInstance().SetPauseFocusActive(false);
        RuntimeGameplayPauseController.ReleasePause(PauseReason);
        HideShell(false, null);

        if (settingsPanel != null && settingsPanel.IsShown)
        {
            settingsPanel.HideImmediate();
        }
    }

    private void HideImmediate()
    {
        isOpen = false;
        showingSettings = false;
        StopMenuReveal(false);
        RuntimeGameplayPauseController.ReleasePause(PauseReason);
        RuntimeCameraController.EnsureInstance().SetPauseFocusActive(false, true);
        HideShell(true, null);

        if (settingsPanel != null && settingsPanel.IsShown)
        {
            settingsPanel.HideImmediate();
        }
    }

    private void SetVisible(bool shouldShow)
    {
        visible = shouldShow;
        if (shouldShow)
        {
            RuntimeUiEventSystemBootstrapper.Ensure();
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetVisible(shouldShow);
        }

        if (!shouldShow)
        {
            HideImmediate();
        }
    }

    private void OpenSettings()
    {
        StopMenuReveal(true);
        EnsureSettingsPanel();
        showingSettings = true;
        HideShell(false, () => settingsPanel.Show(SettingsPanelContext.PauseMenu));
    }

    private void HandleSettingsClosed()
    {
        if (!isOpen || !showingSettings)
        {
            return;
        }

        showingSettings = false;
        ShowMenuPage();
        ShowShell();
    }

    private void RequestReturnToMenu()
    {
        ShowConfirmPage(
            PausePage.ReturnConfirm,
            "回到主界面？",
            "当前战斗会结束并回到主界面。\n本地存档、图鉴和设置不会被重置。",
            "确认返回");
    }

    private void ConfirmReturnToMenu()
    {
        RuntimeSessionResetService.ResetGameplayTransientState();
        HideImmediate();

        SceneLoader loader = SceneLoader.EnsureInstance();
        if (loader != null)
        {
            loader.ToMenu();
            return;
        }

        SceneManager.LoadScene("MainScene");
    }

    private void RequestQuitGame()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        ShowMenuPage();
#else
        ShowConfirmPage(
            PausePage.QuitConfirm,
            "退出游戏？",
            "将关闭当前游戏进程，未保存的战斗状态不会保留。",
            "确认退出");
#endif
    }

    private void ConfirmQuitGame()
    {
        HideImmediate();
#if UNITY_WEBGL && !UNITY_EDITOR
        return;
#elif UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ShowShell()
    {
        EnsureUi();
        RuntimeUiEventSystemBootstrapper.Ensure();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;
            canvas.gameObject.SetActive(true);
        }

        panelCanvasGroup.gameObject.SetActive(true);
        panelCanvasGroup.alpha = 1f;
        panelCanvasGroup.interactable = true;
        panelCanvasGroup.blocksRaycasts = true;
        modalShell.Show(panelCanvasGroup);
    }

    private void HideShell(bool immediate, Action afterHidden)
    {
        if (modalShell == null)
        {
            afterHidden?.Invoke();
            return;
        }

        modalShell.Hide(immediate, () =>
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = false;
                panelCanvasGroup.gameObject.SetActive(false);
            }

            if (canvas != null)
            {
                canvas.gameObject.SetActive(false);
            }

            afterHidden?.Invoke();
        });
    }

    private void ShowMenuPage()
    {
        ShowMenuPage(false);
    }

    private void ShowMenuPage(bool prepareReveal)
    {
        SetPage(PausePage.Menu);
        if (prepareReveal)
        {
            ResetMenuButtonsForReveal();
        }
        else
        {
            RevealMenuButtonsImmediate();
        }
    }

    private void ShowAboutPage()
    {
        StopMenuReveal(true);
        SetPage(PausePage.About);
    }

    private void ShowConfirmPage(PausePage page, string title, string body, string confirmLabel)
    {
        StopMenuReveal(true);
        SetPage(page);
        if (confirmTitleText != null)
        {
            confirmTitleText.text = title;
        }

        if (confirmBodyText != null)
        {
            confirmBodyText.text = body;
        }

        if (confirmPrimaryLabel != null)
        {
            confirmPrimaryLabel.text = confirmLabel;
        }

        if (confirmPrimaryButton != null)
        {
            confirmPrimaryButton.onClick.RemoveAllListeners();
            confirmPrimaryButton.onClick.AddListener(page == PausePage.ReturnConfirm ? ConfirmReturnToMenu : ConfirmQuitGame);
            EnsureButtonInputReady(confirmPrimaryButton);
        }

        if (confirmPrimaryImage != null)
        {
            Color buttonColor = page == PausePage.QuitConfirm ? DangerButtonColor : PrimaryButtonColor;
            confirmPrimaryImage.color = buttonColor;
            PauseMenuButtonFlowEffect flowEffect = confirmPrimaryImage.GetComponent<PauseMenuButtonFlowEffect>();
            if (flowEffect != null)
            {
                flowEffect.SetAccentFromBackground(buttonColor);
            }
        }

        if (confirmPrimaryLabel != null)
        {
            confirmPrimaryLabel.color = page == PausePage.QuitConfirm ? DangerButtonTextColor : PrimaryButtonTextColor;
        }
    }

    private void StartMenuRevealSequence()
    {
        StopMenuReveal(false);
        ResetMenuButtonsForReveal();
        menuRevealCoroutine = StartCoroutine(PlayMenuRevealSequence());
    }

    private void StopMenuReveal(bool revealImmediate)
    {
        if (menuRevealCoroutine != null)
        {
            StopCoroutine(menuRevealCoroutine);
            menuRevealCoroutine = null;
        }

        if (revealImmediate)
        {
            RevealMenuButtonsImmediate();
        }
    }

    private IEnumerator PlayMenuRevealSequence()
    {
        float delay = RuntimeCameraController.PauseFocusEnterDurationSeconds + MenuRevealHoldAfterFocus;
        float elapsedDelay = 0f;
        while (elapsedDelay < delay)
        {
            if (!CanContinueMenuReveal())
            {
                menuRevealCoroutine = null;
                yield break;
            }

            elapsedDelay += Time.unscaledDeltaTime;
            yield return null;
        }

        for (int i = 0; i < menuButtonRevealItems.Count; i++)
        {
            MenuButtonRevealItem item = menuButtonRevealItems[i];
            float elapsed = 0f;
            while (elapsed < MenuButtonRevealDuration)
            {
                if (!CanContinueMenuReveal())
                {
                    menuRevealCoroutine = null;
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                ApplyMenuButtonRevealState(item, Mathf.Clamp01(elapsed / MenuButtonRevealDuration));
                yield return null;
            }

            ApplyMenuButtonRevealState(item, 1f);
        }

        menuRevealCoroutine = null;
    }

    private bool CanContinueMenuReveal()
    {
        return isOpen && !showingSettings && currentPage == PausePage.Menu;
    }

    private void ResetMenuButtonsForReveal()
    {
        for (int i = 0; i < menuButtonRevealItems.Count; i++)
        {
            ApplyMenuButtonRevealState(menuButtonRevealItems[i], 0f);
        }
    }

    private void RevealMenuButtonsImmediate()
    {
        for (int i = 0; i < menuButtonRevealItems.Count; i++)
        {
            ApplyMenuButtonRevealState(menuButtonRevealItems[i], 1f);
        }
    }

    private static void ApplyMenuButtonRevealState(MenuButtonRevealItem item, float progress)
    {
        if (item == null)
        {
            return;
        }

        float easedProgress = RuntimeModalStyle.EaseOutCubic(progress);
        float motionProgress = EaseOutBack(progress);
        if (item.CanvasGroup != null)
        {
            bool canInteract = progress >= MenuButtonInteractionRevealProgress;
            item.CanvasGroup.alpha = easedProgress;
            item.CanvasGroup.interactable = canInteract;
            item.CanvasGroup.blocksRaycasts = canInteract;
        }

        if (item.RectTransform != null)
        {
            float curveOffsetX = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI) * MenuButtonRevealCurveX;
            item.RectTransform.anchoredPosition = item.VisiblePosition + new Vector2(
                curveOffsetX,
                Mathf.LerpUnclamped(MenuButtonRevealOffsetY, 0f, motionProgress));
            item.RectTransform.localScale = Vector3.LerpUnclamped(
                item.VisibleScale * MenuButtonRevealStartScale,
                item.VisibleScale,
                motionProgress);
        }
    }

    private static float EaseOutBack(float progress)
    {
        float clampedProgress = Mathf.Clamp01(progress);
        const float overshoot = 1.42f;
        float shifted = clampedProgress - 1f;
        return 1f + shifted * shifted * ((overshoot + 1f) * shifted + overshoot);
    }

    private void SetPage(PausePage page)
    {
        currentPage = page;
        SetRootVisible(menuRoot, page == PausePage.Menu);
        SetRootVisible(aboutRoot, page == PausePage.About);
        SetRootVisible(confirmRoot, page == PausePage.ReturnConfirm || page == PausePage.QuitConfirm);
        EnsureVisiblePageButtonsInputReady();
    }

    private void EnsureSettingsPanel()
    {
        if (settingsPanel != null)
        {
            return;
        }

        settingsPanel = RuntimeSettingsPanel.EnsureInstance();
        settingsPanel.ContinueRequested -= HandleSettingsClosed;
        settingsPanel.ContinueRequested += HandleSettingsClosed;
        settingsPanel.SetVisible(visible);
        settingsPanel.HideImmediate();
    }

    private void EnsureUi()
    {
        RuntimeUiEventSystemBootstrapper.Ensure();

        if (canvas != null)
        {
            return;
        }

        modalShell = GetComponent<RuntimeModalShell>();
        if (modalShell == null)
        {
            modalShell = gameObject.AddComponent<RuntimeModalShell>();
        }

        GameObject canvasObject = new GameObject(
            CanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
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

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        StretchRect(canvasRect);

        GameObject panelObject = new GameObject("PauseContentRoot", typeof(RectTransform), typeof(CanvasGroup));
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        panelRect.anchoredPosition = Vector2.zero;

        panelCanvasGroup = panelObject.GetComponent<CanvasGroup>();
        BuildMenuPage(panelRect);
        BuildAboutPage(panelRect);
        BuildConfirmPage(panelRect);
        SetPage(PausePage.Menu);
        canvas.gameObject.SetActive(false);
    }

    private void BuildMenuPage(RectTransform parent)
    {
        menuButtonRevealItems.Clear();
        menuRoot = CreateContainer("MenuRoot", parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        StretchRect(menuRoot);

        RectTransform buttonRoot = CreateContainer("ButtonRoot", menuRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        buttonRoot.sizeDelta = new Vector2(520f, 440f);
        buttonRoot.anchoredPosition = Vector2.zero;

        CreateMenuButton(buttonRoot, "返回游戏", 168f, PrimaryButtonColor, PrimaryButtonTextColor, ResumeGame, true);
        CreateMenuButton(buttonRoot, "回到主界面", 84f, SecondaryButtonColor, SecondaryButtonTextColor, RequestReturnToMenu, true);
        CreateMenuButton(buttonRoot, "游戏设置", 0f, SecondaryButtonColor, SecondaryButtonTextColor, OpenSettings, true);
        CreateMenuButton(buttonRoot, "关于我们", -84f, SecondaryButtonColor, SecondaryButtonTextColor, ShowAboutPage, true);
#if !(UNITY_WEBGL && !UNITY_EDITOR)
        CreateMenuButton(buttonRoot, "退出游戏", -168f, DangerButtonColor, DangerButtonTextColor, RequestQuitGame, true);
#endif
    }

    private void BuildAboutPage(RectTransform parent)
    {
        aboutRoot = CreateContainer("AboutRoot", parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        StretchRect(aboutRoot);

        TextMeshProUGUI title = CreateText("Title", aboutRoot, "关于我们", 46f, TitleColor, TextAlignmentOptions.Left);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.offsetMin = new Vector2(72f, -112f);
        titleRect.offsetMax = new Vector2(-72f, -44f);

        RuntimeBuildInfoData buildInfo = RuntimeBuildInfo.Current;
        string buildText = buildInfo == null
            ? "版本信息：未生成"
            : $"版本信息：{buildInfo.DisplayPrimaryText}" +
              (string.IsNullOrWhiteSpace(buildInfo.DisplaySecondaryText) ? string.Empty : $" / {buildInfo.DisplaySecondaryText}");

        TextMeshProUGUI body = CreateText(
            "Body",
            aboutRoot,
            $"Arcitecture 是一个围绕建筑图鉴、战斗收集和基地成长构建的实验性游戏项目。\n\n{buildText}\n\n感谢体验当前版本。",
            26f,
            DescriptionColor,
            TextAlignmentOptions.Left);
        RectTransform bodyRect = body.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(72f, 172f);
        bodyRect.offsetMax = new Vector2(-72f, -146f);
        body.enableWordWrapping = true;

        CreateMenuButton(aboutRoot, "返回暂停页", -250f, PrimaryButtonColor, PrimaryButtonTextColor, ShowMenuPage);
    }

    private void BuildConfirmPage(RectTransform parent)
    {
        confirmRoot = CreateContainer("ConfirmRoot", parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        StretchRect(confirmRoot);

        confirmTitleText = CreateText("Title", confirmRoot, string.Empty, 36f, TitleColor, TextAlignmentOptions.Center);
        RectTransform titleRect = confirmTitleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(720f, 56f);
        titleRect.anchoredPosition = new Vector2(0f, 94f);

        confirmBodyText = CreateText("Body", confirmRoot, string.Empty, 23f, DescriptionColor, TextAlignmentOptions.Center);
        RectTransform bodyRect = confirmBodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
        bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.sizeDelta = new Vector2(760f, 88f);
        bodyRect.anchoredPosition = new Vector2(0f, 18f);
        confirmBodyText.enableWordWrapping = true;

        confirmPrimaryButton = CreateButton("Confirm", confirmRoot, "确认", PrimaryButtonColor, PrimaryButtonTextColor, new Vector2(190f, 56f));
        RectTransform primaryRect = confirmPrimaryButton.GetComponent<RectTransform>();
        primaryRect.anchorMin = new Vector2(0.5f, 0.5f);
        primaryRect.anchorMax = new Vector2(0.5f, 0.5f);
        primaryRect.pivot = new Vector2(0.5f, 0.5f);
        primaryRect.anchoredPosition = new Vector2(110f, -96f);
        confirmPrimaryImage = confirmPrimaryButton.GetComponent<Image>();
        confirmPrimaryLabel = confirmPrimaryButton.GetComponentInChildren<TextMeshProUGUI>();

        Button cancelButton = CreateButton("Cancel", confirmRoot, "取消", SecondaryButtonColor, SecondaryButtonTextColor, new Vector2(160f, 56f));
        RectTransform cancelRect = cancelButton.GetComponent<RectTransform>();
        cancelRect.anchorMin = new Vector2(0.5f, 0.5f);
        cancelRect.anchorMax = new Vector2(0.5f, 0.5f);
        cancelRect.pivot = new Vector2(0.5f, 0.5f);
        cancelRect.anchoredPosition = new Vector2(-110f, -96f);
        cancelButton.onClick.AddListener(ShowMenuPage);
        EnsureButtonInputReady(cancelButton);
    }

    private void CreateMenuButton(
        Transform parent,
        string label,
        float y,
        Color backgroundColor,
        Color textColor,
        UnityEngine.Events.UnityAction onClick,
        bool revealWithMenu = false)
    {
        Button button = CreateButton(label, parent, label, backgroundColor, textColor, new Vector2(MenuButtonWidth, MenuButtonHeight));
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        button.onClick.AddListener(onClick);
        EnsureButtonInputReady(button);

        if (revealWithMenu)
        {
            CanvasGroup buttonCanvasGroup = button.GetComponent<CanvasGroup>();
            if (buttonCanvasGroup == null)
            {
                buttonCanvasGroup = button.gameObject.AddComponent<CanvasGroup>();
            }

            menuButtonRevealItems.Add(new MenuButtonRevealItem(rect, buttonCanvasGroup));
        }
    }

    private static Image CreateImage(string name, Transform parent, Color color, int radius, int border)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(image, color, radius, border);
        return image;
    }

    private static Button CreateButton(string name, Transform parent, string label, Color backgroundColor, Color textColor, Vector2 size)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(RectMask2D), typeof(CanvasGroup));
        buttonObject.transform.SetParent(parent, false);

        Image buttonImage = buttonObject.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplySettingButtonFrameSprite(buttonImage, backgroundColor);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        buttonImage.raycastTarget = true;
        button.interactable = true;

        PauseMenuButtonFlowEffect flowEffect = buttonObject.AddComponent<PauseMenuButtonFlowEffect>();
        flowEffect.SetAccentFromBackground(backgroundColor);

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TmpRuntimeFontFallback.EnsureChineseFallback() ?? TMP_Settings.defaultFontAsset;
        text.text = label;
        text.fontSize = 26f;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        StretchRect(text.rectTransform);
        EnsureButtonInputReady(button);
        return button;
    }

    private void EnsureVisiblePageButtonsInputReady()
    {
        switch (currentPage)
        {
            case PausePage.Menu:
                EnsureButtonsInputReady(menuRoot);
                break;
            case PausePage.About:
                EnsureButtonsInputReady(aboutRoot);
                break;
            case PausePage.ReturnConfirm:
            case PausePage.QuitConfirm:
                EnsureButtonsInputReady(confirmRoot);
                break;
        }
    }

    private static void EnsureButtonsInputReady(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            EnsureButtonInputReady(buttons[i]);
        }
    }

    private static void EnsureButtonInputReady(Button button)
    {
        if (button == null)
        {
            return;
        }

        Graphic targetGraphic = button.targetGraphic ?? button.GetComponent<Graphic>();
        if (targetGraphic != null)
        {
            targetGraphic.raycastTarget = true;
            button.targetGraphic = targetGraphic;
        }

        button.interactable = true;

        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        TextMeshProUGUI[] labels = button.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] != null)
            {
                labels[i].raycastTarget = false;
            }
        }
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
        text.font = TmpRuntimeFontFallback.EnsureChineseFallback() ?? TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        return text;
    }

    private static RectTransform CreateContainer(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static void SetRootVisible(RectTransform root, bool visible)
    {
        if (root != null)
        {
            root.gameObject.SetActive(visible);
        }
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private static bool IsSupportedScene(string sceneName)
    {
        return RuntimeGameplayPauseController.IsRuntimePausableScene(sceneName);
    }
}

internal sealed class PauseMenuButtonFlowEffect : MonoBehaviour
{
    private const float FlowSpeed = 0.72f;
    private const float FlowWidthMin = 92f;
    private const float FlowWidthMax = 148f;
    private const float FlowLineHeight = 3f;

    private RectTransform rectTransform;
    private Outline outline;
    private RectTransform topFlowRect;
    private RectTransform bottomFlowRect;
    private Image topFlowImage;
    private Image bottomFlowImage;
    private Color accentColor = Color.white;
    private float phase;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        outline = GetComponent<Outline>();
        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
        }

        outline.effectDistance = new Vector2(1.5f, -1.5f);
        outline.useGraphicAlpha = true;
        phase = Mathf.Repeat(GetInstanceID() * 0.173f, 1f);

        topFlowImage = CreateFlowLine("TopFlow");
        topFlowRect = topFlowImage.rectTransform;
        bottomFlowImage = CreateFlowLine("BottomFlow");
        bottomFlowRect = bottomFlowImage.rectTransform;
    }

    private void Update()
    {
        if (rectTransform == null)
        {
            return;
        }

        Rect rect = rectTransform.rect;
        if (rect.width <= 0.01f || rect.height <= 0.01f)
        {
            return;
        }

        float time = Time.unscaledTime;
        float pulse = 0.5f + 0.5f * Mathf.Sin((time + phase) * 4.6f);
        Color outlineColor = accentColor;
        outlineColor.a = Mathf.Lerp(0.34f, 0.72f, pulse);
        outline.effectColor = outlineColor;

        float progress = Mathf.Repeat(time * FlowSpeed + phase, 1f);
        float inverseProgress = 1f - progress;
        float width = Mathf.Clamp(rect.width * 0.30f, FlowWidthMin, FlowWidthMax);
        float y = rect.height * 0.5f - 2.6f;
        ApplyFlowLine(topFlowRect, topFlowImage, progress, width, y, rect.width, pulse);
        ApplyFlowLine(bottomFlowRect, bottomFlowImage, inverseProgress, width, -y, rect.width, 1f - pulse);
    }

    public void SetAccentFromBackground(Color backgroundColor)
    {
        accentColor = Color.Lerp(backgroundColor, Color.white, 0.62f);
        accentColor.a = 1f;
    }

    private Image CreateFlowLine(string objectName)
    {
        GameObject flowObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        flowObject.transform.SetParent(transform, false);

        Image image = flowObject.GetComponent<Image>();
        image.raycastTarget = false;
        RuntimeUiSpriteFactory.ApplyRoundedSprite(image, Color.white, 4, 4, 1f);

        RectTransform flowRect = image.rectTransform;
        flowRect.anchorMin = new Vector2(0.5f, 0.5f);
        flowRect.anchorMax = new Vector2(0.5f, 0.5f);
        flowRect.pivot = new Vector2(0.5f, 0.5f);
        flowRect.sizeDelta = new Vector2(FlowWidthMin, FlowLineHeight);
        return image;
    }

    private void ApplyFlowLine(
        RectTransform flowRect,
        Image flowImage,
        float progress,
        float width,
        float y,
        float parentWidth,
        float pulse)
    {
        if (flowRect == null || flowImage == null)
        {
            return;
        }

        float x = Mathf.Lerp(-parentWidth * 0.58f, parentWidth * 0.58f, RuntimeModalStyle.EaseOutCubic(progress));
        flowRect.sizeDelta = new Vector2(width, FlowLineHeight);
        flowRect.anchoredPosition = new Vector2(x, y);

        Color color = accentColor;
        color.a = Mathf.Lerp(0.32f, 0.68f, pulse);
        flowImage.color = color;
    }
}
