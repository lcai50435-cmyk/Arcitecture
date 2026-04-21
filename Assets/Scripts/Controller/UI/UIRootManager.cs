using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIRootManager : MonoBehaviour
{
    public static UIRootManager Instance;

    [Header("图鉴主页")]
    public CanvasGroup handbookUI;

    [Header("详细信息页")]
    public CanvasGroup detailUIPage1;
    public CanvasGroup detailUIPage2;

    [Header("提交窗口 - 三个建筑分别一个")]
    public CanvasGroup submitSelectionUI1;
    public CanvasGroup submitSelectionUI2;
    public CanvasGroup submitSelectionUI3;

    [Header("Dialog弹窗")]
    public CanvasGroup dialogUI;

    [Header("场景交互提示UI")]
    public CanvasGroup interactTipUI;

    [Header("背包UI（可选）")]
    public CanvasGroup backpackUI;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        HideHandbook();
        HideAllDetail();
        HideAllSubmitSelection();
        HideDialog();

        ShowInteractTip();

        if (backpackUI != null)
        {
            ShowBackpack();
        }
    }

    private void SetUI(CanvasGroup cg, bool active, string name)
    {
        if (cg == null)
        {
            Debug.LogWarning($"{name} 没绑定 CanvasGroup");
            return;
        }

        // 关键：显示时强制确保物体本身激活
        if (active && !cg.gameObject.activeSelf)
        {
            cg.gameObject.SetActive(true);
        }

        cg.alpha = active ? 1f : 0f;
        cg.interactable = active;
        cg.blocksRaycasts = active;

    }

    // ========= 图鉴主页 =========
    public void ShowHandbook() => SetUI(handbookUI, true, "HandbookUI");
    public void HideHandbook() => SetUI(handbookUI, false, "HandbookUI");

    // ========= 详细页 =========
    public void ShowDetailPage1()
    {
        SetUI(detailUIPage1, true, "DetailUIPage1");
        SetUI(detailUIPage2, false, "DetailUIPage2");
    }

    public void ShowDetailPage2()
    {
        SetUI(detailUIPage1, false, "DetailUIPage1");
        SetUI(detailUIPage2, true, "DetailUIPage2");
    }

    public void HideAllDetail()
    {
        SetUI(detailUIPage1, false, "DetailUIPage1");
        SetUI(detailUIPage2, false, "DetailUIPage2");
    }

    // ========= 提交窗口 =========
    public void ShowSubmitSelection(int buildingIndex)
    {
        HideAllSubmitSelection();

        switch (buildingIndex)
        {
            case 0:
                SetUI(submitSelectionUI1, true, "SubmitSelectionUI1");
                break;
            case 1:
                SetUI(submitSelectionUI2, true, "SubmitSelectionUI2");
                break;
            case 2:
                SetUI(submitSelectionUI3, true, "SubmitSelectionUI3");
                break;
            default:
                Debug.LogWarning($"未知的提交窗口索引: {buildingIndex}");
                break;
        }
    }

    public void HideSubmitSelection(int buildingIndex)
    {
        switch (buildingIndex)
        {
            case 0:
                SetUI(submitSelectionUI1, false, "SubmitSelectionUI1");
                break;
            case 1:
                SetUI(submitSelectionUI2, false, "SubmitSelectionUI2");
                break;
            case 2:
                SetUI(submitSelectionUI3, false, "SubmitSelectionUI3");
                break;
            default:
                Debug.LogWarning($"未知的提交窗口索引: {buildingIndex}");
                break;
        }
    }

    public void HideAllSubmitSelection()
    {
        SetUI(submitSelectionUI1, false, "SubmitSelectionUI1");
        SetUI(submitSelectionUI2, false, "SubmitSelectionUI2");
        SetUI(submitSelectionUI3, false, "SubmitSelectionUI3");
    }

    // ========= Dialog =========
    public void ShowDialog() => SetUI(dialogUI, true, "DialogUI");
    public void HideDialog() => SetUI(dialogUI, false, "DialogUI");

    // ========= 交互提示 =========
    public void ShowInteractTip() => SetUI(interactTipUI, true, "InteractTipUI");
    public void HideInteractTip() => SetUI(interactTipUI, false, "InteractTipUI");

    // ========= 背包 =========
    public void ShowBackpack() => SetUI(backpackUI, true, "BackpackUI");
    public void HideBackpack() => SetUI(backpackUI, false, "BackpackUI");

    // ========= 常用组合 =========
    public void OpenHandbookView()
    {
        ShowHandbook();
        HideAllDetail();
        HideAllSubmitSelection();
        HideDialog();
        HideInteractTip();
    }

    public void OpenDetailViewPage1()
    {
        HideHandbook();
        ShowDetailPage1();
        HideAllSubmitSelection();
        HideDialog();
        HideInteractTip();
    }

    public void OpenDetailViewPage2()
    {
        HideHandbook();
        ShowDetailPage2();
        HideAllSubmitSelection();
        HideDialog();
        HideInteractTip();
    }

    public void CloseAllBookUI()
    {
        HideHandbook();
        HideAllDetail();
        HideAllSubmitSelection();
        HideDialog();
        ShowInteractTip();
    }

    public bool IsAnyGameplayBlockingUIOpen()
    {
        return
            IsCanvasGroupOpen(handbookUI) ||
            IsCanvasGroupOpen(detailUIPage1) ||
            IsCanvasGroupOpen(detailUIPage2) ||
            IsCanvasGroupOpen(submitSelectionUI1) ||
            IsCanvasGroupOpen(submitSelectionUI2) ||
            IsCanvasGroupOpen(submitSelectionUI3) ||
            IsCanvasGroupOpen(dialogUI) ||
            RuntimePauseMenu.IsPauseOpen;
    }

    private bool IsCanvasGroupOpen(CanvasGroup cg)
    {
        if (cg == null) return false;

        return cg.alpha > 0.01f && cg.blocksRaycasts;
    }


}

public class RuntimePauseMenu : MonoBehaviour
{
    private const string GameSceneName = "GameScene";
    private const string CanvasName = "RuntimePauseMenuCanvas";
    private const int SortingOrder = 280;

    private static readonly Color OverlayColor = new Color(0.02f, 0.03f, 0.05f, 0.76f);
    private static readonly Color PanelColor = new Color(0.10f, 0.12f, 0.16f, 0.96f);
    private static readonly Color BorderColor = new Color(0.33f, 0.45f, 0.55f, 1f);
    private static readonly Color ButtonColor = new Color(0.86f, 0.67f, 0.34f, 1f);
    private static readonly Color ButtonTextColor = new Color(0.14f, 0.09f, 0.05f, 1f);
    private static readonly Color TitleColor = new Color(0.95f, 0.97f, 1f, 1f);
    private static readonly Color HintColor = new Color(0.78f, 0.83f, 0.90f, 1f);

    public static RuntimePauseMenu Instance { get; private set; }
    public static bool IsPauseOpen => Instance != null && Instance.isOpen;

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Button continueButton;
    private bool isOpen;
    private bool visible;
    private float timeScaleBeforePause = 1f;

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
        if (Instance != null && !Instance.visible)
        {
            Instance.HideImmediate(true);
        }
    }

    public static RuntimePauseMenu EnsureInstance()
    {
        bool supportedScene = SceneManager.GetActiveScene().name == GameSceneName;

        if (Instance != null)
        {
            Instance.SetVisible(supportedScene);
            return Instance;
        }

        RuntimePauseMenu existing = FindObjectOfType<RuntimePauseMenu>(true);
        if (existing != null)
        {
            Instance = existing;
            Instance.SetVisible(supportedScene);
            return existing;
        }

        GameObject runtimeObject = new GameObject("RuntimePauseMenu");
        Instance = runtimeObject.AddComponent<RuntimePauseMenu>();
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
        SetVisible(SceneManager.GetActiveScene().name == GameSceneName);
        HideImmediate(true);
    }

    private void Update()
    {
        if (!visible || !Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (RuntimeMiniMapHud.Instance != null && RuntimeMiniMapHud.Instance.IsExpandedViewVisible)
        {
            return;
        }

        if (isOpen)
        {
            ResumeGame();
            return;
        }

        if (UIRootManager.Instance != null && UIRootManager.Instance.IsAnyGameplayBlockingUIOpen())
        {
            return;
        }

        PauseGame();
    }

    private void OnDestroy()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(ResumeGame);
        }

        if (Instance == this)
        {
            Time.timeScale = 1f;
            Instance = null;
        }
    }

    private void PauseGame()
    {
        if (isOpen)
        {
            return;
        }

        timeScaleBeforePause = Time.timeScale <= 0f ? 1f : Time.timeScale;
        Time.timeScale = 0f;
        isOpen = true;
        ApplyVisibility(true);
    }

    private void ResumeGame()
    {
        if (!isOpen)
        {
            return;
        }

        HideImmediate(false);
    }

    private void HideImmediate(bool resetTimeScale)
    {
        isOpen = false;
        ApplyVisibility(false);

        if (resetTimeScale)
        {
            Time.timeScale = 1f;
        }
        else
        {
            Time.timeScale = timeScaleBeforePause <= 0f ? 1f : timeScaleBeforePause;
        }
    }

    private void SetVisible(bool shouldShow)
    {
        visible = shouldShow;

        if (canvas != null)
        {
            canvas.gameObject.SetActive(shouldShow);
        }

        if (!shouldShow)
        {
            HideImmediate(true);
        }
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

        Image overlay = CreateImage("Overlay", canvasObject.transform, OverlayColor, 18, 18);
        StretchRect(overlay.rectTransform);

        Image panel = CreateImage("Panel", canvasObject.transform, PanelColor, 18, 18);
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(420f, 220f);

        Outline panelOutline = panel.gameObject.AddComponent<Outline>();
        panelOutline.effectColor = BorderColor;
        panelOutline.effectDistance = new Vector2(1f, -1f);

        TextMeshProUGUI title = CreateText(
            "Title",
            panel.transform,
            "游戏已暂停",
            34f,
            TitleColor,
            TextAlignmentOptions.Center);
        title.rectTransform.anchoredPosition = new Vector2(0f, 56f);
        title.rectTransform.sizeDelta = new Vector2(300f, 42f);

        TextMeshProUGUI hint = CreateText(
            "Hint",
            panel.transform,
            "按 Esc 或点击下方按钮继续游戏",
            22f,
            HintColor,
            TextAlignmentOptions.Center);
        hint.rectTransform.anchoredPosition = new Vector2(0f, 10f);
        hint.rectTransform.sizeDelta = new Vector2(320f, 30f);

        continueButton = CreateButton(
            "ContinueButton",
            panel.transform,
            "继续游戏",
            ButtonColor,
            ButtonTextColor,
            new Vector2(220f, 58f));
        RectTransform buttonRect = continueButton.GetComponent<RectTransform>();
        buttonRect.anchoredPosition = new Vector2(0f, -58f);
        continueButton.onClick.AddListener(ResumeGame);

        ApplyVisibility(false);
    }

    private void ApplyVisibility(bool show)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = show ? 1f : 0f;
        canvasGroup.interactable = show;
        canvasGroup.blocksRaycasts = show;
    }

    private static Image CreateImage(string name, Transform parent, Color color, int radius, int border)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(image, color, radius, border);
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
        RuntimeUiSpriteFactory.ApplyRoundedSprite(buttonImage, backgroundColor, 14, 14);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Button button = buttonObject.GetComponent<Button>();

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = label;
        text.fontSize = 28f;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = text.GetComponent<RectTransform>();
        StretchRect(textRect);

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
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        return text;
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
