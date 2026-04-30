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
        public Image previewImage;
        public string previewImagePath;
        public Sprite previewSprite;
        public Texture2D previewTexture;
        public Text titleText;
        public Text detailText;
        public Text stateText;
        public Button deleteButton;
        public Text deleteButtonText;
    }

    private readonly struct MainMenuButtonLayout
    {
        public MainMenuButtonLayout(Vector2 size, Vector2 anchoredPosition, int labelFontSize)
        {
            Size = size;
            AnchoredPosition = anchoredPosition;
            LabelFontSize = labelFontSize;
        }

        public Vector2 Size { get; }
        public Vector2 AnchoredPosition { get; }
        public int LabelFontSize { get; }
    }

    private const string MainSceneName = "MainScene";
    private const int UiLayer = 5;
    private const string RuntimeCanvasName = "MainMenuRuntimeCanvas";

    private static readonly Color MenuButtonHitHighlightColor = new Color(1f, 0.92f, 0.68f, 0.18f);
    private static readonly Color MenuButtonHitPressedColor = new Color(1f, 0.76f, 0.38f, 0.28f);
    private static readonly Color MenuButtonArtDisabledColor = new Color(1f, 1f, 1f, 0.36f);
    private static readonly Color TextPrimaryColor = new Color(0.98f, 0.95f, 0.88f, 1f);
    private static readonly Color SaveBackdropColor = new Color(0.04f, 0.035f, 0.03f, 0.54f);
    private static readonly Color SavePanelFallbackColor = new Color(0.84f, 0.64f, 0.37f, 0.98f);
    private static readonly Color SaveCardColor = new Color(0.86f, 0.68f, 0.40f, 0.58f);
    private static readonly Color SaveCardSelectedColor = new Color(0.97f, 0.78f, 0.46f, 0.78f);
    private static readonly Color SaveButtonFallbackColor = new Color(0.78f, 0.56f, 0.28f, 0.98f);
    private static readonly Color SavePreviewColor = new Color(1f, 0.98f, 0.91f, 0.92f);
    private static readonly Color SaveTextPrimaryColor = new Color(0.07f, 0.045f, 0.02f, 1f);
    private static readonly Color SaveTextMutedColor = new Color(0.43f, 0.29f, 0.14f, 1f);
    private static readonly Color SaveDangerTextColor = new Color(0.36f, 0.08f, 0.04f, 1f);

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
    private GameObject confirmDialogObject;
    private Text confirmTitleText;
    private Text confirmMessageText;
    private Text confirmPrimaryButtonText;
    private Action pendingConfirmAction;
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
        ReleaseSlotPreviewSprites();

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
        menuRootRect.sizeDelta = new Vector2(860f, 920f);
        menuRootRect.anchoredPosition = new Vector2(0f, -30f);

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
        overlayImage.color = SaveBackdropColor;

        Button backdropButton = slotOverlayObject.AddComponent<Button>();
        backdropButton.targetGraphic = overlayImage;
        backdropButton.onClick.AddListener(CloseSlotPanel);

        GameObject panelObject = CreateUiObject("SaveBackGround_1", slotOverlayObject.transform);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1000f, 820f);
        panelRect.anchoredPosition = new Vector2(40f, 0f);

        Image panelImage = panelObject.AddComponent<Image>();
        RuntimeUiSpriteFactory.ApplySaveBackgroundSprite(panelImage, SavePanelFallbackColor);

        panelTitleText = CreateText(panelObject.transform, "SavePrompt", "存档管理", 36, SaveTextPrimaryColor, TextAnchor.MiddleCenter, FontStyle.Bold);
        ConfigureRect(panelTitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(220f, 50f), new Vector2(28f, -61f));
        AddTextOutline(panelTitleText);

        Button closeIconButton = CreateIconButton(panelObject.transform, "CloseButton", string.Empty, new Vector2(50f, 50f), CloseSlotPanel);
        ConfigureRect((RectTransform)closeIconButton.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(50f, 50f), new Vector2(-65.5f, -69f));
        Image closeIconImage = closeIconButton.GetComponent<Image>();
        Sprite closeIconSprite = RuntimeUiSpriteFactory.GetSaveCloseIconSprite();
        if (closeIconImage != null && closeIconSprite != null)
        {
            closeIconImage.sprite = closeIconSprite;
            closeIconImage.type = Image.Type.Simple;
            closeIconImage.preserveAspect = true;
            closeIconImage.color = Color.white;
            Text closeIconLabel = closeIconButton.GetComponentInChildren<Text>(true);
            if (closeIconLabel != null)
            {
                closeIconLabel.text = string.Empty;
            }
        }

        GameObject dividerObject = CreateUiObject("Dec", panelObject.transform);
        RectTransform dividerRect = dividerObject.GetComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0.5f, 0.5f);
        dividerRect.anchorMax = new Vector2(0.5f, 0.5f);
        dividerRect.pivot = new Vector2(0.5f, 0.5f);
        dividerRect.sizeDelta = new Vector2(956.1309f, 35.5788f);
        dividerRect.anchoredPosition = new Vector2(39.091812f, 311.7556f);
        Image dividerImage = dividerObject.AddComponent<Image>();
        Sprite dividerSprite = RuntimeUiSpriteFactory.GetSaveDividerSprite();
        if (dividerSprite != null)
        {
            dividerImage.sprite = dividerSprite;
            dividerImage.type = Image.Type.Simple;
            dividerImage.preserveAspect = false;
        }
        dividerImage.color = Color.white;

        panelHintText = CreateText(panelObject.transform, "Hint", string.Empty, 1, Color.clear, TextAnchor.MiddleCenter);
        ConfigureRect(panelHintText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);

        for (int slotId = 1; slotId <= GameProgressPersistence.SlotCount; slotId++)
        {
            slotCardViews.Add(CreateSlotCard(panelObject.transform, slotId));
        }

        panelSelectionText = CreateText(panelObject.transform, "SelectionHint", string.Empty, 1, Color.clear, TextAnchor.MiddleCenter);
        ConfigureRect(panelSelectionText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);

        BuildConfirmDialog(slotOverlayObject.transform);
        slotOverlayObject.transform.SetAsLastSibling();
        slotOverlayObject.SetActive(false);
    }

    private SlotCardView CreateSlotCard(Transform parent, int slotId)
    {
        GameObject cardObject = CreateUiObject($"SlotCard_{slotId}", parent);
        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = new Vector2(0f, -210f * (slotId - 1));
        cardRect.sizeDelta = new Vector2(100f, 100f);

        LayoutElement layoutElement = cardObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 190f;

        GameObject panelObject = CreateUiObject("Save_1Panel", cardObject.transform);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, 247f);
        panelRect.sizeDelta = new Vector2(840f, 190f);

        Image backgroundImage = panelObject.AddComponent<Image>();
        RuntimeUiSpriteFactory.ApplySavePanelFrameSprite(backgroundImage, SaveCardColor);

        Button selectButton = panelObject.AddComponent<Button>();
        selectButton.targetGraphic = backgroundImage;
        ApplyButtonStyle(
            selectButton,
            backgroundImage,
            Color.white,
            Color.Lerp(Color.white, SaveCardSelectedColor, 0.18f),
            Color.Lerp(Color.white, SaveCardSelectedColor, 0.32f),
            new Color(1f, 1f, 1f, 0.55f));
        int capturedSlotId = slotId;
        selectButton.onClick.AddListener(() => HandleSlotSelected(capturedSlotId));

        GameObject contentObject = CreateUiObject("Content", panelObject.transform);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        GameObject previewObject = CreateUiObject("Preview", contentObject.transform);
        RectTransform previewRect = previewObject.GetComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0f, 0.5f);
        previewRect.anchorMax = new Vector2(0f, 0.5f);
        previewRect.pivot = new Vector2(0.5f, 0.5f);
        previewRect.anchoredPosition = new Vector2(126.770355f, -0.35780334f);
        previewRect.sizeDelta = new Vector2(232.3407f, 170.6831f);

        Image previewImage = previewObject.AddComponent<Image>();
        RuntimeUiSpriteFactory.ApplySavePreviewFrameSprite(previewImage, SavePreviewColor);
        previewImage.raycastTarget = false;

        GameObject previewInnerObject = CreateUiObject("Image", previewObject.transform);
        RectTransform previewInnerRect = previewInnerObject.GetComponent<RectTransform>();
        previewInnerRect.anchorMin = new Vector2(0.5f, 0.5f);
        previewInnerRect.anchorMax = new Vector2(0.5f, 0.5f);
        previewInnerRect.pivot = new Vector2(0.5f, 0.5f);
        previewInnerRect.anchoredPosition = Vector2.zero;
        previewInnerRect.sizeDelta = new Vector2(200f, 125f);
        Image previewInnerImage = previewInnerObject.AddComponent<Image>();
        previewInnerImage.color = Color.clear;
        previewInnerImage.enabled = false;
        previewInnerImage.preserveAspect = true;
        previewInnerImage.raycastTarget = false;

        GameObject leftObject = CreateUiObject("Left", contentObject.transform);
        RectTransform leftRect = leftObject.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0.5f, 0.5f);
        leftRect.anchorMax = new Vector2(0.5f, 0.5f);
        leftRect.pivot = new Vector2(0.5f, 0.5f);
        leftRect.anchoredPosition = new Vector2(132f, -2f);
        leftRect.sizeDelta = new Vector2(438f, 150f);
        VerticalLayoutGroup leftGroup = leftObject.AddComponent<VerticalLayoutGroup>();
        leftGroup.spacing = 2f;
        leftGroup.childAlignment = TextAnchor.UpperLeft;
        leftGroup.childControlWidth = true;
        leftGroup.childControlHeight = true;
        leftGroup.childForceExpandWidth = true;
        leftGroup.childForceExpandHeight = false;

        Text stateText = CreateText(leftObject.transform, "State", string.Empty, 18, SaveTextMutedColor, TextAnchor.UpperLeft, FontStyle.Bold);
        ConfigureBestFit(stateText, 13, 18);
        LayoutElement stateLayout = stateText.gameObject.AddComponent<LayoutElement>();
        stateLayout.preferredHeight = 24f;

        Text titleText = CreateText(leftObject.transform, "Title", string.Empty, 32, SaveTextPrimaryColor, TextAnchor.MiddleLeft, FontStyle.Bold);
        ConfigureBestFit(titleText, 24, 32);
        LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 40f;

        Text detailText = CreateText(leftObject.transform, "Detail", string.Empty, 17, SaveTextPrimaryColor, TextAnchor.UpperLeft, FontStyle.Bold);
        detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
        detailText.verticalOverflow = VerticalWrapMode.Truncate;
        detailText.lineSpacing = 0.86f;
        ConfigureBestFit(detailText, 13, 17);
        LayoutElement detailLayout = detailText.gameObject.AddComponent<LayoutElement>();
        detailLayout.preferredHeight = 80f;

        Button deleteButton = CreateIconButton(cardObject.transform, "Dele", string.Empty, new Vector2(50f, 50f), () => HandleDeleteAction(capturedSlotId));
        RectTransform deleteRect = (RectTransform)deleteButton.transform;
        deleteRect.anchorMin = new Vector2(0.5f, 0.5f);
        deleteRect.anchorMax = new Vector2(0.5f, 0.5f);
        deleteRect.pivot = new Vector2(0.5f, 0.5f);
        deleteRect.anchoredPosition = new Vector2(380.2f, 143.3f);
        Image deleteImage = deleteButton.GetComponent<Image>();
        Sprite deleteSprite = RuntimeUiSpriteFactory.GetSaveDeleteIconSprite();
        if (deleteImage != null && deleteSprite != null)
        {
            deleteImage.sprite = deleteSprite;
            deleteImage.type = Image.Type.Simple;
            deleteImage.preserveAspect = true;
            deleteImage.color = Color.white;
        }

        return new SlotCardView
        {
            slotId = slotId,
            selectButton = selectButton,
            backgroundImage = backgroundImage,
            previewImage = previewInnerImage,
            titleText = titleText,
            detailText = detailText,
            stateText = stateText,
            deleteButton = deleteButton,
            deleteButtonText = null
        };
    }

    private void BuildConfirmDialog(Transform parent)
    {
        confirmDialogObject = CreateUiObject("SaveSlotConfirmDialog", parent);
        RectTransform overlayRect = confirmDialogObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = confirmDialogObject.AddComponent<Image>();
        overlayImage.color = new Color(0.04f, 0.03f, 0.02f, 0.28f);

        Button overlayButton = confirmDialogObject.AddComponent<Button>();
        overlayButton.targetGraphic = overlayImage;
        overlayButton.onClick.AddListener(CloseConfirmDialog);

        GameObject panelObject = CreateUiObject("Pop-upPrompt", confirmDialogObject.transform);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(550f, 350f);
        panelRect.anchoredPosition = Vector2.zero;

        Image panelImage = panelObject.AddComponent<Image>();
        RuntimeUiSpriteFactory.ApplySavePanelFrameSprite(panelImage, SavePanelFallbackColor);

        confirmTitleText = CreateText(panelObject.transform, "Information", "Prompt", 36, SaveTextPrimaryColor, TextAnchor.MiddleCenter, FontStyle.Bold);
        ConfigureRect(confirmTitleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(200f, 50f), new Vector2(0f, 130f));
        AddTextOutline(confirmTitleText);

        GameObject dividerObject = CreateUiObject("Image", panelObject.transform);
        RectTransform dividerRect = dividerObject.GetComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0.5f, 0.5f);
        dividerRect.anchorMax = new Vector2(0.5f, 0.5f);
        dividerRect.pivot = new Vector2(0.5f, 0.5f);
        dividerRect.sizeDelta = new Vector2(471.4f, 21.4296f);
        dividerRect.anchoredPosition = new Vector2(6.8781f, 105.667114f);
        Image dividerImage = dividerObject.AddComponent<Image>();
        Sprite lineSprite = RuntimeUiSpriteFactory.GetSavePromptLineSprite();
        if (lineSprite != null)
        {
            dividerImage.sprite = lineSprite;
            dividerImage.type = Image.Type.Simple;
            dividerImage.preserveAspect = false;
        }
        dividerImage.color = Color.white;

        GameObject detailObject = CreateUiObject("DetailInformation", panelObject.transform);
        RectTransform detailRect = detailObject.GetComponent<RectTransform>();
        detailRect.anchorMin = new Vector2(0.5f, 0.5f);
        detailRect.anchorMax = new Vector2(0.5f, 0.5f);
        detailRect.pivot = new Vector2(0.5f, 0.5f);
        detailRect.sizeDelta = new Vector2(471.3979f, 216.9215f);
        detailRect.anchoredPosition = new Vector2(6.8777f, -27.511002f);
        Image detailImage = detailObject.AddComponent<Image>();
        RuntimeUiSpriteFactory.ApplySavePanelFrameSprite(detailImage, SavePanelFallbackColor);

        confirmMessageText = CreateText(detailObject.transform, "Text", string.Empty, 24, SaveTextPrimaryColor, TextAnchor.MiddleCenter, FontStyle.Bold);
        confirmMessageText.horizontalOverflow = HorizontalWrapMode.Wrap;
        confirmMessageText.verticalOverflow = VerticalWrapMode.Truncate;
        ConfigureRect(confirmMessageText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(404.3094f, 91.9097f), new Vector2(6.8103027f, 22.191f));

        Button cancelButton = CreateActionButton(detailObject.transform, "CancelButton", "取消", new Vector2(160f, 40f), CloseConfirmDialog, SaveButtonFallbackColor);
        ConfigureRect((RectTransform)cancelButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(160f, 40f), new Vector2(-114.9f, -53.9f));

        Button confirmButton = CreateActionButton(detailObject.transform, "NotarizeButton", "确认", new Vector2(160f, 40f), ConfirmPendingAction, SaveButtonFallbackColor);
        ConfigureRect((RectTransform)confirmButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(160f, 40f), new Vector2(130.2f, -54.4f));
        confirmPrimaryButtonText = confirmButton.GetComponentInChildren<Text>(true);

        confirmDialogObject.SetActive(false);
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
        CloseConfirmDialog();

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
        HandlePrimaryAction();
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
        }

        OpenConfirmDialog(
            "删除存档",
            $"确认删除存档 {slotId - 1:00}？\n永久进度会被清空，照片相册会保留。",
            "确认",
            () =>
            {
                GameProgressPersistence.DeleteSlot(slotId);
                armedDeleteSlotId = 0;
                armedOverwriteSlotId = 0;
                selectedSlotId = ResolveInitialSelection(currentPanelMode);
                RefreshMenuState();
            });
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
            if (summary.hasSave)
            {
                armedOverwriteSlotId = selectedSlotId;
                armedDeleteSlotId = 0;
                RefreshSlotOverlay();
                OpenConfirmDialog(
                    "覆盖存档",
                    $"确认覆盖存档 {selectedSlotId - 1:00}？\n原有永久进度会被清空，并从基地重新开始。",
                    "确认",
                    () =>
                    {
                        GameProgressPersistence.StartNewGame(selectedSlotId);
                        CloseSlotPanel();
                        EnterBaseScene();
                    });
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

        panelTitleText.text = "存档管理";
        panelHintText.text = string.Empty;

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

        view.titleText.text = $"存档 {view.slotId - 1:00}";
        view.detailText.text = BuildSlotDetail(summary);
        view.stateText.text = BuildSlotState(summary, isSelected);
        view.selectButton.interactable = canSelect;

        if (view.backgroundImage != null)
        {
            view.backgroundImage.color = Color.white;
        }

        RefreshSlotPreview(view, summary);

        if (view.deleteButton != null)
        {
            view.deleteButton.gameObject.SetActive(deleteVisible);
            if (deleteVisible)
            {
                bool isDeleteArmed = armedDeleteSlotId == view.slotId;
                Image deleteButtonImage = view.deleteButton.GetComponent<Image>();
                if (deleteButtonImage != null)
                {
                    deleteButtonImage.color = Color.white;
                }
                if (view.deleteButtonText != null)
                {
                    view.deleteButtonText.text = isDeleteArmed ? "确认" : "删除";
                    view.deleteButtonText.color = SaveDangerTextColor;
                }
            }
        }
    }

    private void UpdatePrimaryActionState(IReadOnlyList<SaveSlotSummary> summaries)
    {
        SaveSlotSummary summary = GetSlotSummary(selectedSlotId, summaries);
        bool canSelect = CanSelectSlot(summary);

        if (panelSelectionText != null)
        {
            panelSelectionText.text = BuildSelectionHint(summary, canSelect);
        }
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
                return $"请在确认弹窗中决定是否覆盖存档 {summary.slotId - 1:00}。";
            }

            return $"存档 {summary.slotId - 1:00} 已有进度。点击右下角按钮会打开覆盖确认，不会立即清空。";
        }

        if (!canSelect)
        {
            return "请选择一个已有存档的槽位继续游戏。";
        }

        if (armedDeleteSlotId == summary.slotId)
        {
            return $"请在确认弹窗中决定是否删除存档 {summary.slotId - 1:00}。照片相册保留。";
        }

        return $"将读取存档 {summary.slotId - 1:00} 的永久进度并进入基地场景，不会恢复战斗中途现场。";
    }

    private string BuildSlotDetail(SaveSlotSummary summary)
    {
        if (summary == null || !summary.hasSave)
        {
            return "最后保存：暂无\n关卡：未开始    进度：0%\n武器：直墨";
        }

        string stageName = ResolveStageName(summary.selectedStageId);
        string weaponName = InkTypeCatalog.GetDisplayName(summary.currentWeaponType);
        int progressValue = Mathf.RoundToInt(summary.progressPercent);
        return $"最后保存：{FormatUtcTimestamp(summary.savedAtUtc)}\n关卡：{stageName}    进度：{progressValue}%\n武器：{weaponName}";
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

    private static void RefreshSlotPreview(SlotCardView view, SaveSlotSummary summary)
    {
        if (view == null || view.previewImage == null)
        {
            return;
        }

        string nextPreviewPath = summary != null && summary.hasSave
            ? summary.previewImagePath ?? string.Empty
            : string.Empty;

        if (!string.Equals(view.previewImagePath, nextPreviewPath, StringComparison.Ordinal))
        {
            ReleaseSlotPreview(view);
            view.previewImagePath = nextPreviewPath;

            Texture2D previewTexture = GameProgressPersistence.LoadSlotPreviewTexture(summary);
            if (previewTexture != null)
            {
                view.previewTexture = previewTexture;
                view.previewSprite = Sprite.Create(
                    previewTexture,
                    new Rect(0f, 0f, previewTexture.width, previewTexture.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0u,
                    SpriteMeshType.FullRect);
                view.previewSprite.name = $"SaveSlotPreviewSprite_{view.slotId:00}";
            }
        }

        bool hasPreview = view.previewSprite != null;
        view.previewImage.sprite = hasPreview ? view.previewSprite : null;
        view.previewImage.color = hasPreview ? Color.white : Color.clear;
        view.previewImage.enabled = hasPreview;
        view.previewImage.preserveAspect = true;
        view.previewImage.raycastTarget = false;
    }

    private void ReleaseSlotPreviewSprites()
    {
        for (int i = 0; i < slotCardViews.Count; i++)
        {
            ReleaseSlotPreview(slotCardViews[i]);
        }
    }

    private static void ReleaseSlotPreview(SlotCardView view)
    {
        if (view == null)
        {
            return;
        }

        DestroyUnityObject(view.previewSprite);
        DestroyUnityObject(view.previewTexture);
        view.previewSprite = null;
        view.previewTexture = null;
        view.previewImagePath = string.Empty;
    }

    private void OpenConfirmDialog(string title, string message, string confirmLabel, Action confirmAction)
    {
        pendingConfirmAction = confirmAction;

        if (confirmDialogObject == null)
        {
            return;
        }

        if (confirmTitleText != null)
        {
            confirmTitleText.text = string.IsNullOrWhiteSpace(title) ? "Prompt" : title;
        }

        if (confirmMessageText != null)
        {
            confirmMessageText.text = message ?? string.Empty;
        }

        if (confirmPrimaryButtonText != null)
        {
            confirmPrimaryButtonText.text = string.IsNullOrWhiteSpace(confirmLabel) ? "确认" : confirmLabel;
            confirmPrimaryButtonText.color = SaveTextPrimaryColor;
        }

        confirmDialogObject.SetActive(true);
        confirmDialogObject.transform.SetAsLastSibling();
    }

    private void CloseConfirmDialog()
    {
        pendingConfirmAction = null;
        bool wasOpen = confirmDialogObject != null && confirmDialogObject.activeSelf;

        if (confirmDialogObject != null)
        {
            confirmDialogObject.SetActive(false);
        }

        if (wasOpen && currentPanelMode != MainMenuSlotPanelMode.None)
        {
            armedDeleteSlotId = 0;
            armedOverwriteSlotId = 0;
            RefreshSlotOverlay();
        }
    }

    private void ConfirmPendingAction()
    {
        Action action = pendingConfirmAction;
        pendingConfirmAction = null;

        if (confirmDialogObject != null)
        {
            confirmDialogObject.SetActive(false);
        }

        action?.Invoke();
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
        MainMenuButtonLayout buttonLayout = ResolveMenuButtonLayout(objectName);

        GameObject buttonObject = CreateUiObject(objectName, parent);
        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = buttonLayout.Size;
        rectTransform.anchoredPosition = buttonLayout.AnchoredPosition;

        LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = buttonLayout.Size.x;
        layoutElement.preferredHeight = buttonLayout.Size.y;

        Image image = buttonObject.AddComponent<Image>();
        RuntimeUiSpriteFactory.ApplySavePanelFrameSprite(image, Color.white);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        Color normal = image.color;
        ApplyButtonStyle(
            button,
            image,
            normal,
            Color.Lerp(normal, MenuButtonHitHighlightColor, 0.24f),
            Color.Lerp(normal, MenuButtonHitPressedColor, 0.32f),
            MenuButtonArtDisabledColor);
        button.onClick.AddListener(onClick);

        Text labelText = CreateText(buttonObject.transform, "Label", label, buttonLayout.LabelFontSize, TextPrimaryColor, TextAnchor.MiddleCenter, FontStyle.Bold);
        ConfigureRect(labelText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), buttonLayout.Size - new Vector2(60f, 34f), new Vector2(0f, -4f));
        AddTextOutline(labelText);

        return button;
    }

    private static MainMenuButtonLayout ResolveMenuButtonLayout(string objectName)
    {
        switch (objectName)
        {
            case "NewGameButton":
                return new MainMenuButtonLayout(new Vector2(500f, 250f), new Vector2(0f, 310f), 42);
            case "ContinueButton":
                return new MainMenuButtonLayout(new Vector2(760f, 140f), new Vector2(0f, 115f), 48);
            case "HandbookButton":
                return new MainMenuButtonLayout(new Vector2(760f, 140f), new Vector2(0f, -80f), 48);
            case "SettingsButton":
                return new MainMenuButtonLayout(new Vector2(420f, 180f), new Vector2(0f, -275f), 42);
            case "ExitButton":
                return new MainMenuButtonLayout(new Vector2(80f, 80f), new Vector2(0f, -405f), 32);
            default:
                return new MainMenuButtonLayout(new Vector2(500f, 114f), Vector2.zero, 42);
        }
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
        RuntimeUiSpriteFactory.ApplySaveButtonFrameSprite(image, buttonColor);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        Color normal = image.color;
        Color highlighted = Color.Lerp(normal, Color.white, 0.10f);
        Color pressed = Color.Lerp(normal, new Color(0.65f, 0.42f, 0.18f, 1f), 0.16f);
        Color disabled = new Color(normal.r, normal.g, normal.b, 0.42f);
        ApplyButtonStyle(button, image, normal, highlighted, pressed, disabled);
        button.onClick.AddListener(onClick);

        int labelFontSize = size.y <= 54f ? 24 : 28;
        Text labelText = CreateText(buttonObject.transform, "Label", label, labelFontSize, SaveTextPrimaryColor, TextAnchor.MiddleCenter, FontStyle.Bold);
        ConfigureRect(labelText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size - new Vector2(20f, 14f), Vector2.zero);

        return button;
    }

    private static Button CreateIconButton(
        Transform parent,
        string objectName,
        string label,
        Vector2 size,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.color = Color.clear;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(onClick);

        Text labelText = CreateText(buttonObject.transform, "Label", label, 38, SaveTextMutedColor, TextAnchor.MiddleCenter, FontStyle.Bold);
        ConfigureRect(labelText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, Vector2.zero);
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
        text.raycastTarget = false;
        text.supportRichText = false;
        RuntimeTextFontRepair.RepairLegacyText(text);
        return text;
    }

    private static void ConfigureBestFit(Text text, int minSize, int maxSize)
    {
        if (text == null)
        {
            return;
        }

        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(1, minSize);
        text.resizeTextMaxSize = Mathf.Max(text.resizeTextMinSize, maxSize);
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

    private static void DestroyUnityObject(UnityEngine.Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
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
