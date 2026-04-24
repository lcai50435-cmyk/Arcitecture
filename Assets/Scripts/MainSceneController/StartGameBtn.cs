using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartGameBtn : MonoBehaviour
{
    private void Start()
    {
        Button button = GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(OnClick);
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        Debug.Log("开始游戏按钮点击成功！");

        if (MainMenuController.TryOpenNewGameFlow())
        {
            return;
        }

        SceneLoader loader = SceneLoader.EnsureInstance();
        if (loader != null)
        {
            loader.ToBase();
        }
    }
}

public enum MainMenuSlotPanelMode
{
    None = 0,
    NewGame = 1,
    Continue = 2
}

public sealed class MainMenuController : MonoBehaviour
{
    private sealed class SlotCardView
    {
        public int slotId;
        public Button selectButton;
        public Image backgroundImage;
        public Text titleText;
        public Text detailText;
        public Text stateText;
        public Button deleteButton;
        public Text deleteButtonText;
    }

    private const string MainSceneName = "MainScene";
    private const int UiLayer = 5;
    private const string RuntimeCanvasName = "MainMenuRuntimeCanvas";

    private static readonly Color MenuButtonColor = new Color(0.12f, 0.09f, 0.06f, 0.92f);
    private static readonly Color MenuButtonHighlightColor = new Color(0.19f, 0.15f, 0.1f, 0.96f);
    private static readonly Color MenuButtonPressedColor = new Color(0.09f, 0.07f, 0.05f, 0.98f);
    private static readonly Color MenuButtonDisabledColor = new Color(0.13f, 0.13f, 0.13f, 0.55f);
    private static readonly Color AccentColor = new Color(0.84f, 0.71f, 0.47f, 0.96f);
    private static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.09f, 0.95f);
    private static readonly Color CardColor = new Color(0.15f, 0.15f, 0.17f, 0.95f);
    private static readonly Color CardSelectedColor = new Color(0.3f, 0.22f, 0.11f, 0.98f);
    private static readonly Color CardDisabledColor = new Color(0.11f, 0.11f, 0.12f, 0.72f);
    private static readonly Color DeleteButtonColor = new Color(0.41f, 0.12f, 0.11f, 0.96f);
    private static readonly Color DeleteButtonArmedColor = new Color(0.74f, 0.18f, 0.13f, 1f);
    private static readonly Color TextPrimaryColor = new Color(0.98f, 0.95f, 0.88f, 1f);
    private static readonly Color TextSecondaryColor = new Color(0.84f, 0.83f, 0.78f, 1f);

    private static MainMenuController current;

    private readonly List<SlotCardView> slotCardViews = new List<SlotCardView>();

    private Button exitButton;
    private Button continueButton;
    private RectTransform menuCanvasRect;
    private RectTransform menuRootRect;
    private Canvas runtimeCanvas;
    private RectTransform runtimeCanvasRect;
    private GameObject slotOverlayObject;
    private Text panelTitleText;
    private Text panelHintText;
    private Text panelSelectionText;
    private Text primaryButtonText;
    private Button primaryButton;
    private MainMenuSlotPanelMode currentPanelMode;
    private int selectedSlotId;
    private int armedDeleteSlotId;
    private int armedOverwriteSlotId;
    private bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetBootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        current = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryEnsureController(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryEnsureController(scene);
    }

    private static void TryEnsureController(Scene scene)
    {
        if (!scene.IsValid() || !string.Equals(scene.name, MainSceneName, StringComparison.Ordinal))
        {
            return;
        }

        MainMenuController existing = FindObjectOfType<MainMenuController>(true);
        if (existing != null)
        {
            existing.TryInitialize();
            return;
        }

        GameObject controllerObject = new GameObject(nameof(MainMenuController));
        controllerObject.layer = 0;
        controllerObject.AddComponent<MainMenuController>();
    }

    public static bool TryOpenNewGameFlow()
    {
        if (current == null)
        {
            current = FindObjectOfType<MainMenuController>(true);
        }

        if (current == null)
        {
            return false;
        }

        current.OpenSlotPanel(MainMenuSlotPanelMode.NewGame);
        return true;
    }

    private void Awake()
    {
        if (current != null && current != this)
        {
            Destroy(gameObject);
            return;
        }

        current = this;
    }

    private void Start()
    {
        TryInitialize();
    }

    private void Update()
    {
        if (!initialized || menuRootRect == null)
        {
            return;
        }

        bool settingsOpen = RuntimeSettingsPanel.Instance != null && RuntimeSettingsPanel.Instance.IsShown;
        bool handbookOpen = MainSceneHandbookLauncher.IsAnyHandbookOpen();
        bool shouldShowMenu = !settingsOpen && !handbookOpen;

        if (menuRootRect.gameObject.activeSelf != shouldShowMenu)
        {
            menuRootRect.gameObject.SetActive(shouldShowMenu);
        }
    }

    private void OnDestroy()
    {
        if (current == this)
        {
            current = null;
        }
    }

    public void TryInitialize()
    {
        if (initialized)
        {
            RefreshMenuState();
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!string.Equals(activeScene.name, MainSceneName, StringComparison.Ordinal))
        {
            return;
        }

        Button legacyStartButton = FindNamedButton("Start Game");
        Button legacySetupButton = FindNamedButton("Set up");
        exitButton = FindNamedButton("Exit");

        if (legacyStartButton == null || legacySetupButton == null || exitButton == null)
        {
            Debug.LogWarning("MainMenuController 初始化失败：主菜单关键按钮缺失。");
            return;
        }

        menuCanvasRect = legacyStartButton.transform.parent as RectTransform;
        if (menuCanvasRect == null)
        {
            Debug.LogWarning("MainMenuController 初始化失败：未找到主菜单 Canvas。");
            return;
        }

        legacyStartButton.gameObject.SetActive(false);
        legacySetupButton.gameObject.SetActive(false);
        exitButton.gameObject.SetActive(false);

        EnsureRuntimeCanvas();
        BuildMenuButtons();
        BuildSlotOverlay();

        initialized = true;
        RefreshMenuState();
    }

    private void EnsureRuntimeCanvas()
    {
        if (runtimeCanvas != null && runtimeCanvasRect != null)
        {
            return;
        }

        GameObject existingCanvasObject = GameObject.Find(RuntimeCanvasName);
        GameObject canvasObject = existingCanvasObject ?? new GameObject(
            RuntimeCanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        runtimeCanvas = canvasObject.GetComponent<Canvas>();
        runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        runtimeCanvas.overrideSorting = true;
        runtimeCanvas.sortingOrder = 320;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        runtimeCanvasRect = canvasObject.GetComponent<RectTransform>();
        runtimeCanvasRect.anchorMin = Vector2.zero;
        runtimeCanvasRect.anchorMax = Vector2.one;
        runtimeCanvasRect.offsetMin = Vector2.zero;
        runtimeCanvasRect.offsetMax = Vector2.zero;
        runtimeCanvasRect.localScale = Vector3.one;
    }

    private void BuildMenuButtons()
    {
        GameObject menuRoot = CreateUiObject("MainMenuRuntimeRoot", runtimeCanvasRect);
        menuRootRect = menuRoot.GetComponent<RectTransform>();
        menuRootRect.anchorMin = new Vector2(0.5f, 0.5f);
        menuRootRect.anchorMax = new Vector2(0.5f, 0.5f);
        menuRootRect.pivot = new Vector2(0.5f, 0.5f);
        menuRootRect.sizeDelta = new Vector2(560f, 760f);
        menuRootRect.anchoredPosition = new Vector2(0f, -90f);

        VerticalLayoutGroup layoutGroup = menuRoot.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 28f;
        layoutGroup.padding = new RectOffset(0, 0, 0, 0);
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        ContentSizeFitter sizeFitter = menuRoot.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateMenuButton(menuRootRect, "NewGameButton", "新游戏", OpenNewGamePanel);
        continueButton = CreateMenuButton(menuRootRect, "ContinueButton", "继续游戏", OpenContinuePanel);
        CreateMenuButton(menuRootRect, "HandbookButton", "图鉴/手册", OpenHandbookPanel);
        CreateMenuButton(menuRootRect, "SettingsButton", "设置", OpenSettingsPanel);
#if !(UNITY_WEBGL && !UNITY_EDITOR)
        CreateMenuButton(menuRootRect, "ExitButton", "退出", ExitGame);
#endif
    }

    private void BuildSlotOverlay()
    {
        slotOverlayObject = CreateUiObject("SaveSlotOverlay", runtimeCanvasRect);
        RectTransform overlayRect = slotOverlayObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = slotOverlayObject.AddComponent<Image>();
        overlayImage.color = new Color(0.02f, 0.02f, 0.03f, 0.78f);

        Button backdropButton = slotOverlayObject.AddComponent<Button>();
        backdropButton.targetGraphic = overlayImage;
        backdropButton.onClick.AddListener(CloseSlotPanel);

        GameObject panelObject = CreateUiObject("SaveSlotPanel", slotOverlayObject.transform);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(960f, 660f);
        panelRect.anchoredPosition = Vector2.zero;

        Image panelImage = panelObject.AddComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(panelImage, PanelColor, 24, 18, 1.2f);

        GameObject headerAccent = CreateUiObject("HeaderAccent", panelObject.transform);
        RectTransform headerAccentRect = headerAccent.GetComponent<RectTransform>();
        headerAccentRect.anchorMin = new Vector2(0.5f, 1f);
        headerAccentRect.anchorMax = new Vector2(0.5f, 1f);
        headerAccentRect.pivot = new Vector2(0.5f, 1f);
        headerAccentRect.sizeDelta = new Vector2(320f, 6f);
        headerAccentRect.anchoredPosition = new Vector2(0f, -30f);
        Image headerAccentImage = headerAccent.AddComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(headerAccentImage, AccentColor, 6, 4, 1f);

        panelTitleText = CreateText(panelObject.transform, "Title", string.Empty, 44, TextPrimaryColor, TextAnchor.MiddleCenter, FontStyle.Bold);
        ConfigureRect(panelTitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(720f, 60f), new Vector2(0f, -72f));
        AddTextOutline(panelTitleText);

        panelHintText = CreateText(panelObject.transform, "Hint", string.Empty, 24, TextSecondaryColor, TextAnchor.MiddleCenter);
        ConfigureRect(panelHintText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(820f, 70f), new Vector2(0f, -126f));

        GameObject cardsRoot = CreateUiObject("CardsRoot", panelObject.transform);
        RectTransform cardsRootRect = cardsRoot.GetComponent<RectTransform>();
        cardsRootRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardsRootRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardsRootRect.pivot = new Vector2(0.5f, 0.5f);
        cardsRootRect.sizeDelta = new Vector2(840f, 370f);
        cardsRootRect.anchoredPosition = new Vector2(0f, -18f);

        VerticalLayoutGroup cardsLayout = cardsRoot.AddComponent<VerticalLayoutGroup>();
        cardsLayout.spacing = 18f;
        cardsLayout.padding = new RectOffset(0, 0, 0, 0);
        cardsLayout.childAlignment = TextAnchor.UpperCenter;
        cardsLayout.childControlWidth = true;
        cardsLayout.childControlHeight = false;
        cardsLayout.childForceExpandWidth = true;
        cardsLayout.childForceExpandHeight = false;

        for (int slotId = 1; slotId <= GameProgressPersistence.SlotCount; slotId++)
        {
            slotCardViews.Add(CreateSlotCard(cardsRoot.transform, slotId));
        }

        panelSelectionText = CreateText(panelObject.transform, "SelectionHint", string.Empty, 24, TextSecondaryColor, TextAnchor.MiddleCenter);
        ConfigureRect(panelSelectionText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(820f, 80f), new Vector2(0f, 102f));

        Button closeButton = CreateActionButton(panelObject.transform, "CloseButton", "返回", new Vector2(220f, 78f), CloseSlotPanel, MenuButtonColor);
        ConfigureRect((RectTransform)closeButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(220f, 78f), new Vector2(-160f, 32f));

        primaryButton = CreateActionButton(panelObject.transform, "PrimaryButton", "继续游戏", new Vector2(340f, 84f), HandlePrimaryAction, AccentColor);
        ConfigureRect((RectTransform)primaryButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(340f, 84f), new Vector2(170f, 28f));
        primaryButtonText = primaryButton.GetComponentInChildren<Text>(true);

        slotOverlayObject.transform.SetAsLastSibling();
        slotOverlayObject.SetActive(false);
    }

    private SlotCardView CreateSlotCard(Transform parent, int slotId)
    {
        GameObject cardObject = CreateUiObject($"SlotCard_{slotId}", parent);
        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(840f, 110f);

        LayoutElement layoutElement = cardObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 110f;

        Image backgroundImage = cardObject.AddComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(backgroundImage, CardColor, 18, 14, 1.1f);

        Button selectButton = cardObject.AddComponent<Button>();
        selectButton.targetGraphic = backgroundImage;
        ApplyButtonStyle(selectButton, backgroundImage, CardColor, CardSelectedColor, CardSelectedColor, CardDisabledColor);
        int capturedSlotId = slotId;
        selectButton.onClick.AddListener(() => HandleSlotSelected(capturedSlotId));

        GameObject contentObject = CreateUiObject("Content", cardObject.transform);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(22f, 16f);
        contentRect.offsetMax = new Vector2(-22f, -16f);

        HorizontalLayoutGroup contentLayout = contentObject.AddComponent<HorizontalLayoutGroup>();
        contentLayout.spacing = 20f;
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.childAlignment = TextAnchor.MiddleLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = false;
        contentLayout.childForceExpandHeight = true;

        GameObject leftObject = CreateUiObject("Left", contentObject.transform);
        LayoutElement leftLayout = leftObject.AddComponent<LayoutElement>();
        leftLayout.flexibleWidth = 1f;

        VerticalLayoutGroup leftGroup = leftObject.AddComponent<VerticalLayoutGroup>();
        leftGroup.spacing = 10f;
        leftGroup.childAlignment = TextAnchor.MiddleLeft;
        leftGroup.childControlWidth = true;
        leftGroup.childControlHeight = false;
        leftGroup.childForceExpandWidth = true;
        leftGroup.childForceExpandHeight = false;

        Text titleText = CreateText(leftObject.transform, "Title", string.Empty, 30, TextPrimaryColor, TextAnchor.MiddleLeft, FontStyle.Bold);
        LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 34f;

        Text detailText = CreateText(leftObject.transform, "Detail", string.Empty, 21, TextSecondaryColor, TextAnchor.UpperLeft);
        detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
        detailText.verticalOverflow = VerticalWrapMode.Overflow;
        detailText.lineSpacing = 0.92f;

        GameObject rightObject = CreateUiObject("Right", contentObject.transform);
        LayoutElement rightLayout = rightObject.AddComponent<LayoutElement>();
        rightLayout.preferredWidth = 190f;

        VerticalLayoutGroup rightGroup = rightObject.AddComponent<VerticalLayoutGroup>();
        rightGroup.spacing = 12f;
        rightGroup.childAlignment = TextAnchor.MiddleCenter;
        rightGroup.childControlWidth = true;
        rightGroup.childControlHeight = false;
        rightGroup.childForceExpandWidth = true;
        rightGroup.childForceExpandHeight = false;

        Text stateText = CreateText(rightObject.transform, "State", string.Empty, 22, AccentColor, TextAnchor.MiddleCenter, FontStyle.Bold);
        LayoutElement stateLayout = stateText.gameObject.AddComponent<LayoutElement>();
        stateLayout.preferredHeight = 28f;

        Button deleteButton = CreateActionButton(rightObject.transform, "DeleteButton", "删除存档", new Vector2(180f, 48f), () => HandleDeleteAction(capturedSlotId), DeleteButtonColor);
        Text deleteButtonText = deleteButton.GetComponentInChildren<Text>(true);

        return new SlotCardView
        {
            slotId = slotId,
            selectButton = selectButton,
            backgroundImage = backgroundImage,
            titleText = titleText,
            detailText = detailText,
            stateText = stateText,
            deleteButton = deleteButton,
            deleteButtonText = deleteButtonText
        };
    }

    private void RefreshMenuState()
    {
        if (!initialized)
        {
            return;
        }

        bool hasAnySlots = GameProgressPersistence.HasAnySlots();
        if (continueButton != null)
        {
            continueButton.interactable = hasAnySlots;
        }

        if (currentPanelMode != MainMenuSlotPanelMode.None)
        {
            RefreshSlotOverlay();
        }
    }

    private void OpenNewGamePanel()
    {
        OpenSlotPanel(MainMenuSlotPanelMode.NewGame);
    }

    private void OpenContinuePanel()
    {
        if (!GameProgressPersistence.HasAnySlots())
        {
            RefreshMenuState();
            return;
        }

        OpenSlotPanel(MainMenuSlotPanelMode.Continue);
    }

    private void OpenSettingsPanel()
    {
        CloseSlotPanel();
        RuntimeSettingsPanel.EnsureInstance().Show(SettingsPanelContext.MainMenu);
    }

    private void OpenHandbookPanel()
    {
        CloseSlotPanel();

        MainSceneHandbookLauncher launcher = MainSceneHandbookLauncher.Instance != null
            ? MainSceneHandbookLauncher.Instance
            : FindObjectOfType<MainSceneHandbookLauncher>(true);
        if (launcher == null)
        {
            Debug.LogWarning("MainScene 缺少 MainSceneHandbookLauncher，无法打开图鉴。");
            return;
        }

        launcher.TryOpen(menuRootRect != null ? menuRootRect.gameObject : null);
    }

    private void OpenSlotPanel(MainMenuSlotPanelMode mode)
    {
        if (!initialized)
        {
            TryInitialize();
        }

        if (!initialized || slotOverlayObject == null)
        {
            return;
        }

        currentPanelMode = mode;
        armedDeleteSlotId = 0;
        armedOverwriteSlotId = 0;
        selectedSlotId = ResolveInitialSelection(mode);

        slotOverlayObject.SetActive(true);
        slotOverlayObject.transform.SetAsLastSibling();
        RefreshSlotOverlay();
    }

    private void CloseSlotPanel()
    {
        currentPanelMode = MainMenuSlotPanelMode.None;
        selectedSlotId = 0;
        armedDeleteSlotId = 0;
        armedOverwriteSlotId = 0;

        if (slotOverlayObject != null)
        {
            slotOverlayObject.SetActive(false);
        }
    }

    private void HandleSlotSelected(int slotId)
    {
        SaveSlotSummary summary = GetSlotSummary(slotId);
        if (!CanSelectSlot(summary))
        {
            return;
        }

        selectedSlotId = slotId;
        armedDeleteSlotId = 0;
        armedOverwriteSlotId = 0;
        RefreshSlotOverlay();
    }

    private void HandleDeleteAction(int slotId)
    {
        if (currentPanelMode != MainMenuSlotPanelMode.Continue)
        {
            return;
        }

        SaveSlotSummary summary = GetSlotSummary(slotId);
        if (summary == null || !summary.hasSave)
        {
            return;
        }

        if (armedDeleteSlotId != slotId)
        {
            selectedSlotId = slotId;
            armedDeleteSlotId = slotId;
            armedOverwriteSlotId = 0;
            RefreshSlotOverlay();
            return;
        }

        GameProgressPersistence.DeleteSlot(slotId);
        armedDeleteSlotId = 0;
        armedOverwriteSlotId = 0;
        selectedSlotId = ResolveInitialSelection(currentPanelMode);
        RefreshMenuState();
    }

    private void HandlePrimaryAction()
    {
        SaveSlotSummary summary = GetSlotSummary(selectedSlotId);
        if (!CanSelectSlot(summary))
        {
            return;
        }

        if (currentPanelMode == MainMenuSlotPanelMode.NewGame)
        {
            if (summary.hasSave && armedOverwriteSlotId != selectedSlotId)
            {
                armedOverwriteSlotId = selectedSlotId;
                armedDeleteSlotId = 0;
                RefreshSlotOverlay();
                return;
            }

            GameProgressPersistence.StartNewGame(selectedSlotId);
            CloseSlotPanel();
            EnterBaseScene();
            return;
        }

        if (currentPanelMode == MainMenuSlotPanelMode.Continue)
        {
            GameProgressPersistence.LoadSlot(selectedSlotId);
            CloseSlotPanel();
            EnterBaseScene();
        }
    }

    private void RefreshSlotOverlay()
    {
        if (slotOverlayObject == null || currentPanelMode == MainMenuSlotPanelMode.None)
        {
            return;
        }

        IReadOnlyList<SaveSlotSummary> summaries = GameProgressPersistence.ListSlots();
        if (!IsSlotStillSelectable(selectedSlotId, summaries))
        {
            selectedSlotId = ResolveInitialSelection(currentPanelMode, summaries);
        }

        panelTitleText.text = currentPanelMode == MainMenuSlotPanelMode.NewGame ? "新游戏" : "继续游戏";
        panelHintText.text = currentPanelMode == MainMenuSlotPanelMode.NewGame
            ? "请选择要创建新档的槽位。已有进度的槽位需要再次确认才会覆盖。"
            : "请选择要继续的存档。删除存档需要二次确认，照片相册会继续保留。";

        for (int i = 0; i < slotCardViews.Count; i++)
        {
            SlotCardView view = slotCardViews[i];
            SaveSlotSummary summary = i < summaries.Count ? summaries[i] : null;
            RefreshSlotCard(view, summary);
        }

        UpdatePrimaryActionState(summaries);
    }

    private void RefreshSlotCard(SlotCardView view, SaveSlotSummary summary)
    {
        if (view == null)
        {
            return;
        }

        bool hasSave = summary != null && summary.hasSave;
        bool isSelected = summary != null && summary.slotId == selectedSlotId;
        bool canSelect = CanSelectSlot(summary);
        bool deleteVisible = currentPanelMode == MainMenuSlotPanelMode.Continue && hasSave;

        view.titleText.text = $"槽位 {view.slotId}";
        view.detailText.text = BuildSlotDetail(summary);
        view.stateText.text = BuildSlotState(summary, isSelected);
        view.selectButton.interactable = canSelect;

        Color cardColor = CardColor;
        if (!canSelect)
        {
            cardColor = CardDisabledColor;
        }
        else if (isSelected)
        {
            cardColor = CardSelectedColor;
        }

        RuntimeUiSpriteFactory.ApplyRoundedSprite(view.backgroundImage, cardColor, 18, 14, 1.1f);

        if (view.deleteButton != null)
        {
            view.deleteButton.gameObject.SetActive(deleteVisible);
            if (deleteVisible)
            {
                bool isDeleteArmed = armedDeleteSlotId == view.slotId;
                Image deleteButtonImage = view.deleteButton.GetComponent<Image>();
                Color deleteColor = isDeleteArmed ? DeleteButtonArmedColor : DeleteButtonColor;
                RuntimeUiSpriteFactory.ApplyRoundedSprite(deleteButtonImage, deleteColor, 12, 10, 1f);
                view.deleteButtonText.text = isDeleteArmed ? "确认删除" : "删除存档";
            }
        }
    }

    private void UpdatePrimaryActionState(IReadOnlyList<SaveSlotSummary> summaries)
    {
        SaveSlotSummary summary = GetSlotSummary(selectedSlotId, summaries);
        bool canSelect = CanSelectSlot(summary);
        bool canPerformPrimaryAction = false;

        if (currentPanelMode == MainMenuSlotPanelMode.NewGame)
        {
            primaryButtonText.text = summary != null && summary.hasSave && armedOverwriteSlotId == selectedSlotId
                ? "确认覆盖并开始"
                : summary != null && summary.hasSave
                    ? "覆盖并开始"
                    : "开始新游戏";
            canPerformPrimaryAction = summary != null;
        }
        else
        {
            primaryButtonText.text = "继续游戏";
            canPerformPrimaryAction = canSelect;
        }

        primaryButton.interactable = canPerformPrimaryAction;
        panelSelectionText.text = BuildSelectionHint(summary, canSelect);
    }

    private string BuildSelectionHint(SaveSlotSummary summary, bool canSelect)
    {
        if (summary == null)
        {
            return currentPanelMode == MainMenuSlotPanelMode.NewGame
                ? "请选择一个槽位后开始新游戏。"
                : "当前没有可继续的存档。";
        }

        if (currentPanelMode == MainMenuSlotPanelMode.NewGame)
        {
            if (!summary.hasSave)
            {
                return $"将在槽位 {summary.slotId} 创建新档，并直接进入基地场景。";
            }

            if (armedOverwriteSlotId == summary.slotId)
            {
                return $"再次点击右下角按钮后，会清空槽位 {summary.slotId} 的永久进度并开始新游戏。";
            }

            return $"槽位 {summary.slotId} 已有存档。首次点击右下角按钮只会进入覆盖确认，不会立即清空。";
        }

        if (!canSelect)
        {
            return "请选择一个已有存档的槽位继续游戏。";
        }

        return $"将读取槽位 {summary.slotId} 的永久进度并进入基地场景，不会恢复战斗中途现场。";
    }

    private string BuildSlotDetail(SaveSlotSummary summary)
    {
        if (summary == null || !summary.hasSave)
        {
            return "状态：空槽位\n最后保存：暂无\n当前关卡：未开始    总进度：0%    当前武器：直墨";
        }

        string stageName = ResolveStageName(summary.selectedStageId);
        string weaponName = InkTypeCatalog.GetDisplayName(summary.currentWeaponType);
        int progressValue = Mathf.RoundToInt(summary.progressPercent);
        return $"状态：已占用\n最后保存：{FormatUtcTimestamp(summary.savedAtUtc)}\n当前关卡：{stageName}    总进度：{progressValue}%    当前武器：{weaponName}";
    }

    private string BuildSlotState(SaveSlotSummary summary, bool isSelected)
    {
        if (summary == null)
        {
            return "不可用";
        }

        if (currentPanelMode == MainMenuSlotPanelMode.NewGame)
        {
            if (!summary.hasSave)
            {
                return isSelected ? "已选择" : "可新建";
            }

            if (armedOverwriteSlotId == summary.slotId)
            {
                return "等待覆盖确认";
            }

            return isSelected ? "已选择" : "将覆盖";
        }

        if (!summary.hasSave)
        {
            return "无存档";
        }

        if (armedDeleteSlotId == summary.slotId)
        {
            return "等待删除确认";
        }

        return isSelected ? "已选择" : "可继续";
    }

    private int ResolveInitialSelection(MainMenuSlotPanelMode mode)
    {
        return ResolveInitialSelection(mode, GameProgressPersistence.ListSlots());
    }

    private int ResolveInitialSelection(MainMenuSlotPanelMode mode, IReadOnlyList<SaveSlotSummary> summaries)
    {
        if (mode == MainMenuSlotPanelMode.NewGame)
        {
            for (int i = 0; i < summaries.Count; i++)
            {
                if (summaries[i] != null && !summaries[i].hasSave)
                {
                    return summaries[i].slotId;
                }
            }

            return summaries.Count > 0 && summaries[0] != null ? summaries[0].slotId : 0;
        }

        for (int i = 0; i < summaries.Count; i++)
        {
            if (summaries[i] != null && summaries[i].hasSave)
            {
                return summaries[i].slotId;
            }
        }

        return 0;
    }

    private bool IsSlotStillSelectable(int slotId, IReadOnlyList<SaveSlotSummary> summaries)
    {
        SaveSlotSummary summary = GetSlotSummary(slotId, summaries);
        return CanSelectSlot(summary);
    }

    private bool CanSelectSlot(SaveSlotSummary summary)
    {
        if (summary == null)
        {
            return false;
        }

        if (currentPanelMode == MainMenuSlotPanelMode.NewGame)
        {
            return true;
        }

        return summary.hasSave;
    }

    private SaveSlotSummary GetSlotSummary(int slotId)
    {
        return GetSlotSummary(slotId, GameProgressPersistence.ListSlots());
    }

    private static SaveSlotSummary GetSlotSummary(int slotId, IReadOnlyList<SaveSlotSummary> summaries)
    {
        if (slotId <= 0 || summaries == null)
        {
            return null;
        }

        for (int i = 0; i < summaries.Count; i++)
        {
            SaveSlotSummary summary = summaries[i];
            if (summary != null && summary.slotId == slotId)
            {
                return summary;
            }
        }

        return null;
    }

    private void EnterBaseScene()
    {
        SceneLoader loader = SceneLoader.EnsureInstance();
        if (loader != null)
        {
            loader.ToBase();
        }
    }

    private void ExitGame()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return;
#elif UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static Button FindNamedButton(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        return target != null ? target.GetComponent<Button>() : null;
    }

    private static Button CreateMenuButton(Transform parent, string objectName, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(500f, 114f);

        LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 500f;
        layoutElement.preferredHeight = 114f;

        Image image = buttonObject.AddComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(image, MenuButtonColor, 20, 14, 1.1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        ApplyButtonStyle(button, image, MenuButtonColor, MenuButtonHighlightColor, MenuButtonPressedColor, MenuButtonDisabledColor);
        button.onClick.AddListener(onClick);

        GameObject accentObject = CreateUiObject("Accent", buttonObject.transform);
        RectTransform accentRect = accentObject.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0.5f, 1f);
        accentRect.anchorMax = new Vector2(0.5f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.sizeDelta = new Vector2(270f, 6f);
        accentRect.anchoredPosition = new Vector2(0f, -18f);
        Image accentImage = accentObject.AddComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(accentImage, AccentColor, 6, 4, 1f);

        Text labelText = CreateText(buttonObject.transform, "Label", label, 42, TextPrimaryColor, TextAnchor.MiddleCenter, FontStyle.Bold);
        ConfigureRect(labelText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(420f, 60f), new Vector2(0f, -4f));
        AddTextOutline(labelText);

        return button;
    }

    private static Button CreateActionButton(
        Transform parent,
        string objectName,
        string label,
        Vector2 size,
        UnityEngine.Events.UnityAction onClick,
        Color buttonColor)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(image, buttonColor, 14, 10, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        Color highlighted = Color.Lerp(buttonColor, Color.white, 0.08f);
        Color pressed = Color.Lerp(buttonColor, Color.black, 0.12f);
        Color disabled = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.4f);
        ApplyButtonStyle(button, image, buttonColor, highlighted, pressed, disabled);
        button.onClick.AddListener(onClick);

        Text labelText = CreateText(buttonObject.transform, "Label", label, 28, TextPrimaryColor, TextAnchor.MiddleCenter, FontStyle.Bold);
        ConfigureRect(labelText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size - new Vector2(20f, 14f), Vector2.zero);
        AddTextOutline(labelText);

        return button;
    }

    private static void ApplyButtonStyle(Button button, Image image, Color normal, Color highlighted, Color pressed, Color disabled)
    {
        if (button == null || image == null)
        {
            return;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = highlighted;
        colors.selectedColor = highlighted;
        colors.pressedColor = pressed;
        colors.disabledColor = disabled;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = UiLayer;

        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.anchoredPosition3D = Vector3.zero;

        return gameObject;
    }

    private static Text CreateText(
        Transform parent,
        string objectName,
        string value,
        int fontSize,
        Color color,
        TextAnchor anchor,
        FontStyle fontStyle = FontStyle.Normal)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = anchor;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = false;
        RuntimeTextFontRepair.RepairLegacyText(text);
        return text;
    }

    private static void ConfigureRect(
        RectTransform rectTransform,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 sizeDelta,
        Vector2 anchoredPosition)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.anchoredPosition = anchoredPosition;
    }

    private static void AddTextOutline(Text text)
    {
        if (text == null)
        {
            return;
        }

        Outline outline = text.GetComponent<Outline>();
        if (outline == null)
        {
            outline = text.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = new Color(0f, 0f, 0f, 0.35f);
        outline.effectDistance = new Vector2(1.2f, -1.2f);
    }

    private static string ResolveStageName(string stageId)
    {
        GameplayStageDefinition stage = GameplayStageCatalog.GetStageById(stageId);
        return stage != null ? stage.displayName : "未开始";
    }

    private static string FormatUtcTimestamp(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "暂无";
        }

        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsed))
        {
            return value;
        }

        return parsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    }
}
