using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class BuildingKnowledgeEntry
{
    public CatalogueBuildingId buildingId;
    public string buildingName;
    public string[] keywords;
    public string[] shortFacts;
    public string fallbackAnswer;
}

public static class BuildingKnowledgeLibrary
{
    private static readonly BuildingKnowledgeEntry[] Entries =
    {
        new BuildingKnowledgeEntry
        {
            buildingId = CatalogueBuildingId.Building1,
            buildingName = "福建土楼",
            keywords = new[] { "土楼", "福建", "围合", "夯土", "防御", "宗族", "圆楼" },
            shortFacts = new[]
            {
                "福建土楼常以厚重夯土墙围合，能同时满足居住和防御。",
                "土楼内部共享庭院，体现聚族而居和公共协作的生活秩序。",
                "圆形土楼能减少外墙死角，防御视线更连续。"
            },
            fallbackAnswer = "福建土楼的关键是夯土、围合、防御和聚族而居。它把生活空间和防护结构合在一起。"
        },
        new BuildingKnowledgeEntry
        {
            buildingId = CatalogueBuildingId.Building2,
            buildingName = "赵州桥",
            keywords = new[] { "赵州桥", "桥", "敞肩", "石拱", "拱券", "泄洪", "受力" },
            shortFacts = new[]
            {
                "赵州桥的敞肩小拱能减轻桥身重量，也能帮助洪水通过。",
                "石拱把压力沿弧线传到桥台，是很清晰的结构受力智慧。",
                "赵州桥历经千年，说明材料、受力和排水设计都很可靠。"
            },
            fallbackAnswer = "赵州桥的重点是敞肩石拱。小拱减重并泄洪，主拱把压力稳定传到两侧桥台。"
        },
        new BuildingKnowledgeEntry
        {
            buildingId = CatalogueBuildingId.Building3,
            buildingName = "安徽水乡民居",
            keywords = new[] { "安徽", "水乡", "民居", "白墙", "黛瓦", "天井", "临水" },
            shortFacts = new[]
            {
                "安徽水乡民居常临水布置，生活、交通和排水关系很紧密。",
                "白墙黛瓦不只是审美，也利于识别屋面排水和墙体层次。",
                "天井能组织采光、通风和雨水，是民居空间里的核心节点。"
            },
            fallbackAnswer = "安徽水乡民居的重点是临水布局、白墙黛瓦和天井。它把自然水系和日常生活组织在一起。"
        }
    };

    public static IReadOnlyList<BuildingKnowledgeEntry> GetAccessibleEntries(string sceneName, RuntimeProgressState runtimeState)
    {
        List<BuildingKnowledgeEntry> accessible = new List<BuildingKnowledgeEntry>();
        runtimeState ??= RuntimeProgressState.Instance ?? RuntimeProgressState.EnsureInstance();

        GameplayStageDefinition currentStage = GameplayStageCatalog.GetStageByScene(sceneName);
        if (currentStage == null)
        {
            for (int i = 0; i < Entries.Length; i++)
            {
                if (runtimeState.IsBuildingRepaired(Entries[i].buildingId))
                {
                    accessible.Add(Entries[i]);
                }
            }

            return accessible;
        }

        int currentIndex = GameplayStageCatalog.GetStageIndex(currentStage.stageId);
        IReadOnlyList<GameplayStageDefinition> stages = GameplayStageCatalog.GetAll();
        for (int i = 0; i < stages.Count && i < currentIndex; i++)
        {
            CatalogueBuildingId buildingId = stages[i].stageBuildingId;
            if (!runtimeState.IsBuildingRepaired(buildingId))
            {
                continue;
            }

            BuildingKnowledgeEntry entry = GetEntry(buildingId);
            if (entry != null)
            {
                accessible.Add(entry);
            }
        }

        return accessible;
    }

    public static string Answer(string query, string sceneName, RuntimeProgressState runtimeState)
    {
        if (BeaverQuoteLibrary.TryAnswerStructureQuestion(query, out string structureAnswer))
        {
            return structureAnswer;
        }

        IReadOnlyList<BuildingKnowledgeEntry> accessible = GetAccessibleEntries(sceneName, runtimeState);
        if (accessible.Count == 0)
        {
            return "我还不能讲太多。先把上一处古建筑修复完整，相关知识就会在这里解锁。";
        }

        BuildingKnowledgeEntry selected = ResolveEntry(query, accessible);
        if (selected == null)
        {
            return BuildTopicList(accessible);
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return $"{selected.buildingName}可以问：{string.Join("、", selected.keywords)}。";
        }

        int factIndex = Mathf.Abs(query.Trim().GetHashCode()) % selected.shortFacts.Length;
        string fact = selected.shortFacts[factIndex];
        return $"{selected.buildingName}：{fact}";
    }

    public static string GetAmbientFact(string sceneName, RuntimeProgressState runtimeState)
    {
        IReadOnlyList<BuildingKnowledgeEntry> accessible = GetAccessibleEntries(sceneName, runtimeState);
        string quote = BeaverQuoteLibrary.GetAmbientQuote(sceneName, accessible.Count > 0);
        if (!string.IsNullOrWhiteSpace(quote))
        {
            return quote;
        }

        if (accessible.Count == 0)
        {
            return UnityEngine.Random.value > 0.5f
                ? "河狸：把建筑修好，世界会更稳定。"
                : "河狸：专用结构齐了，就能带材料回去修复。";
        }

        BuildingKnowledgeEntry entry = accessible[UnityEngine.Random.Range(0, accessible.Count)];
        return $"河狸：{entry.shortFacts[UnityEngine.Random.Range(0, entry.shortFacts.Length)]}";
    }

    private static BuildingKnowledgeEntry ResolveEntry(string query, IReadOnlyList<BuildingKnowledgeEntry> accessible)
    {
        if (accessible.Count == 1)
        {
            return accessible[0];
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        string normalized = query.Trim();
        for (int i = 0; i < accessible.Count; i++)
        {
            BuildingKnowledgeEntry entry = accessible[i];
            if (normalized.IndexOf(entry.buildingName, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return entry;
            }

            for (int keywordIndex = 0; keywordIndex < entry.keywords.Length; keywordIndex++)
            {
                if (normalized.IndexOf(entry.keywords[keywordIndex], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return entry;
                }
            }
        }

        return null;
    }

    private static BuildingKnowledgeEntry GetEntry(CatalogueBuildingId buildingId)
    {
        for (int i = 0; i < Entries.Length; i++)
        {
            if (Entries[i].buildingId == buildingId)
            {
                return Entries[i];
            }
        }

        return null;
    }

    private static string BuildTopicList(IReadOnlyList<BuildingKnowledgeEntry> accessible)
    {
        List<string> names = new List<string>();
        for (int i = 0; i < accessible.Count; i++)
        {
            names.Add(accessible[i].buildingName);
        }

        return $"目前可以问：{string.Join("、", names)}。请说出建筑名或结构关键词。";
    }
}

public sealed class BeaverAssistantHud : MonoBehaviour
{
    private const string CanvasName = "BeaverAssistantCanvas";
    private const int SortingOrder = 238;
    private const float BubbleVisibleSeconds = 4.8f;

    private static BeaverAssistantHud instance;

    private Canvas canvas;
    private RectTransform canvasRect;
    private Button avatarButton;
    private TextMeshProUGUI bubbleText;
    private CanvasGroup bubbleGroup;
    private Sprite avatarSprite;
    private float bubbleUntilTime;
    private float nextAmbientTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureInstance().HandleSceneChanged(SceneManager.GetActiveScene().name);
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstance().HandleSceneChanged(scene.name);
    }

    public static BeaverAssistantHud EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        BeaverAssistantHud existing = FindObjectOfType<BeaverAssistantHud>(true);
        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        GameObject hudObject = new GameObject("BeaverAssistantHud");
        instance = hudObject.AddComponent<BeaverAssistantHud>();
        return instance;
    }

    public static void ShowBubble(string message)
    {
        BeaverAssistantHud hud = EnsureInstance();
        hud.ShowBubbleInternal(message);
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
        ScheduleNextAmbient();
    }

    private void Update()
    {
        if (bubbleGroup != null)
        {
            float targetAlpha = Time.unscaledTime <= bubbleUntilTime ? 1f : 0f;
            bubbleGroup.alpha = Mathf.MoveTowards(bubbleGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 5f);
        }

        if (Time.unscaledTime >= nextAmbientTime)
        {
            TryShowAmbientFact();
            ScheduleNextAmbient();
        }
    }

    private void HandleSceneChanged(string sceneName)
    {
        EnsureUi();
        bool supported = IsSupportedScene(sceneName);
        if (canvas != null)
        {
            canvas.gameObject.SetActive(supported);
        }

        ScheduleNextAmbient();
    }

    private void TryShowAmbientFact()
    {
        if (!IsSupportedScene(SceneManager.GetActiveScene().name))
        {
            return;
        }

        if (BeaverAssistantPanel.IsOpen ||
            RuntimePauseMenu.IsPauseOpen ||
            (UIRootManager.Instance != null && UIRootManager.Instance.IsAnyGameplayBlockingUIOpen()) ||
            GameplayStageIntroDirector.IsIntroActive ||
            GameplayFailureController.IsFailureActive)
        {
            return;
        }

        string message = BuildingKnowledgeLibrary.GetAmbientFact(
            SceneManager.GetActiveScene().name,
            RuntimeProgressState.Instance ?? RuntimeProgressState.EnsureInstance());
        ShowBubbleInternal(ClampAmbientMessage(message));
    }

    private void ScheduleNextAmbient()
    {
        nextAmbientTime = Time.unscaledTime + UnityEngine.Random.Range(45f, 90f);
    }

    private void ShowBubbleInternal(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        EnsureUi();
        TmpRuntimeFontFallback.WarmupCharacters(message);
        bubbleText.text = message;
        bubbleUntilTime = Time.unscaledTime + BubbleVisibleSeconds;
        if (bubbleGroup != null)
        {
            bubbleGroup.alpha = 1f;
        }
    }

    private void EnsureUi()
    {
        if (canvas != null && avatarButton != null && bubbleText != null)
        {
            return;
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

        canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        avatarButton = CreateAvatarButton(canvasRect);
        bubbleText = CreateBubble(canvasRect, out bubbleGroup);
    }

    private Button CreateAvatarButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("BeaverAvatarButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-28f, -26f);
        rect.sizeDelta = new Vector2(82f, 82f);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = GetOrCreateAvatarSprite();
        image.preserveAspect = true;
        image.color = Color.white;

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(() => BeaverAssistantPanel.EnsureInstance().Toggle());
        return button;
    }

    private TextMeshProUGUI CreateBubble(Transform parent, out CanvasGroup group)
    {
        GameObject bubbleObject = new GameObject("BeaverBubble", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        RectTransform rect = bubbleObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-120f, -26f);
        rect.sizeDelta = new Vector2(430f, 66f);

        Image background = bubbleObject.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(background, new Color(0.06f, 0.08f, 0.07f, 0.84f), 14, 12);

        group = bubbleObject.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 8f);
        textRect.offsetMax = new Vector2(-18f, -8f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TmpRuntimeFontFallback.EnsureChineseFallback() ?? TMP_Settings.defaultFontAsset;
        text.fontSize = 22f;
        text.color = new Color(0.94f, 0.92f, 0.84f, 1f);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private Sprite GetOrCreateAvatarSprite()
    {
        if (avatarSprite != null)
        {
            return avatarSprite;
        }

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color fur = new Color32(142, 86, 42, 255);
        Color face = new Color32(204, 151, 86, 255);
        Color ear = new Color32(95, 55, 34, 255);
        Color tooth = new Color32(246, 238, 202, 255);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }

        FillCircle(texture, 20, 46, 10, ear);
        FillCircle(texture, 44, 46, 10, ear);
        FillCircle(texture, 32, 31, 24, fur);
        FillCircle(texture, 32, 26, 18, face);
        FillCircle(texture, 24, 34, 3, Color.black);
        FillCircle(texture, 40, 34, 3, Color.black);
        FillCircle(texture, 32, 27, 4, new Color32(54, 32, 24, 255));
        FillRect(texture, 26, 14, 6, 9, tooth);
        FillRect(texture, 33, 14, 6, 9, tooth);
        texture.Apply();

        avatarSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        avatarSprite.name = "RuntimeBeaverAvatar";
        return avatarSprite;
    }

    private static bool IsSupportedScene(string sceneName)
    {
        return GameplayStageCatalog.IsGameplayScene(sceneName) ||
               string.Equals(sceneName, "NewBase", StringComparison.Ordinal) ||
               string.Equals(sceneName, "BaseScene", StringComparison.Ordinal);
    }

    private static string ClampAmbientMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || message.Length <= 42)
        {
            return message;
        }

        return message.Substring(0, 40) + "…";
    }

    private static void FillCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
    {
        int radiusSquared = radius * radius;
        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                if (x >= 0 && y >= 0 && x < texture.width && y < texture.height && dx * dx + dy * dy <= radiusSquared)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }

    private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        for (int yy = y; yy < y + height; yy++)
        {
            for (int xx = x; xx < x + width; xx++)
            {
                if (xx >= 0 && yy >= 0 && xx < texture.width && yy < texture.height)
                {
                    texture.SetPixel(xx, yy, color);
                }
            }
        }
    }
}

public sealed class BeaverAssistantPanel : MonoBehaviour
{
    private const string PauseReason = "BeaverAssistant";
    private const int SortingOrder = 239;

    private static BeaverAssistantPanel instance;

    private Canvas canvas;
    private CanvasGroup rootGroup;
    private TextMeshProUGUI historyText;
    private TMP_InputField inputField;
    private readonly List<string> historyLines = new List<string>();

    public static bool IsOpen => instance != null && instance.gameObject.activeInHierarchy;

    public static BeaverAssistantPanel EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        BeaverAssistantPanel existing = FindObjectOfType<BeaverAssistantPanel>(true);
        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        GameObject panelObject = new GameObject("BeaverAssistantPanel");
        instance = panelObject.AddComponent<BeaverAssistantPanel>();
        return instance;
    }

    public void Toggle()
    {
        if (gameObject.activeInHierarchy)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    public void Show()
    {
        EnsureUi();
        gameObject.SetActive(true);
        rootGroup.alpha = 1f;
        rootGroup.interactable = true;
        rootGroup.blocksRaycasts = true;
        UIRootManager.Instance?.HideBackpack();
        RuntimeGameplayPauseController.RequestPause(PauseReason);
        SeedHistoryIfNeeded();
        inputField?.ActivateInputField();
    }

    public void Hide()
    {
        RuntimeGameplayPauseController.ReleasePause(PauseReason);
        UIRootManager.Instance?.ShowBackpack();
        gameObject.SetActive(false);
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
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
        }
    }

    private void EnsureUi()
    {
        if (canvas != null && historyText != null && inputField != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("BeaverAssistantPanelCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
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

        rootGroup = canvasObject.GetComponent<CanvasGroup>();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        Image overlay = canvasObject.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.46f);

        GameObject panel = CreatePanel(canvasRect);
        CreateHeader(panel.transform);
        historyText = CreateHistory(panel.transform);
        inputField = CreateInput(panel.transform);
        CreateAskButton(panel.transform);
        CreateTopicButtons(panel.transform);
    }

    private GameObject CreatePanel(Transform parent)
    {
        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(860f, 560f);

        Image background = panel.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(background, new Color(0.09f, 0.11f, 0.10f, 0.96f), 14, 12);
        return panel;
    }

    private void CreateHeader(Transform parent)
    {
        TextMeshProUGUI title = CreateText(parent, "Title", "河狸 · 建筑知识", 34f, new Color(0.94f, 0.82f, 0.56f, 1f), TextAlignmentOptions.MidlineLeft);
        SetRect(title.rectTransform, new Vector2(32f, -26f), new Vector2(520f, 54f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));

        Button close = CreateButton(parent, "CloseButton", "×", new Vector2(-34f, -30f), new Vector2(52f, 42f), new Vector2(1f, 1f));
        close.onClick.AddListener(Hide);
    }

    private TextMeshProUGUI CreateHistory(Transform parent)
    {
        TextMeshProUGUI history = CreateText(parent, "History", string.Empty, 24f, new Color(0.91f, 0.90f, 0.84f, 1f), TextAlignmentOptions.TopLeft);
        SetRect(history.rectTransform, new Vector2(32f, -92f), new Vector2(796f, 292f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        history.enableWordWrapping = true;
        history.overflowMode = TextOverflowModes.Ellipsis;
        return history;
    }

    private TMP_InputField CreateInput(Transform parent)
    {
        GameObject inputObject = new GameObject("Input", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        RectTransform rect = inputObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetRect(rect, new Vector2(32f, 90f), new Vector2(620f, 58f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));

        Image image = inputObject.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(image, new Color(0.16f, 0.18f, 0.16f, 0.98f), 12, 10);

        TextMeshProUGUI text = CreateText(inputObject.transform, "Text", string.Empty, 24f, Color.white, TextAlignmentOptions.MidlineLeft);
        SetStretch(text.rectTransform, 18f, 8f, 18f, 8f);
        text.enableWordWrapping = false;

        TextMeshProUGUI placeholder = CreateText(inputObject.transform, "Placeholder", "问福建土楼的防御、赵州桥的受力……", 22f, new Color(0.72f, 0.72f, 0.66f, 0.85f), TextAlignmentOptions.MidlineLeft);
        SetStretch(placeholder.rectTransform, 18f, 8f, 18f, 8f);
        placeholder.enableWordWrapping = false;

        TMP_InputField field = inputObject.GetComponent<TMP_InputField>();
        field.textViewport = rect;
        field.textComponent = text;
        field.placeholder = placeholder;
        field.lineType = TMP_InputField.LineType.SingleLine;
        field.onSubmit.AddListener(_ => SubmitQuestion());
        return field;
    }

    private void CreateAskButton(Transform parent)
    {
        Button ask = CreateButton(parent, "AskButton", "询问", new Vector2(-32f, 90f), new Vector2(158f, 58f), new Vector2(1f, 0f));
        ask.onClick.AddListener(SubmitQuestion);
    }

    private void CreateTopicButtons(Transform parent)
    {
        CreateTopicButton(parent, "福建土楼", new Vector2(32f, 24f));
        CreateTopicButton(parent, "赵州桥", new Vector2(168f, 24f));
        CreateTopicButton(parent, "安徽水乡", new Vector2(304f, 24f));
    }

    private void CreateTopicButton(Transform parent, string label, Vector2 position)
    {
        Button button = CreateButton(parent, $"Topic_{label}", label, position, new Vector2(118f, 42f), new Vector2(0f, 0f));
        button.onClick.AddListener(() =>
        {
            inputField.text = label;
            SubmitQuestion();
        });
    }

    private void SubmitQuestion()
    {
        string question = inputField != null ? inputField.text.Trim() : string.Empty;
        string answer = BuildingKnowledgeLibrary.Answer(
            question,
            SceneManager.GetActiveScene().name,
            RuntimeProgressState.Instance ?? RuntimeProgressState.EnsureInstance());

        if (!string.IsNullOrWhiteSpace(question))
        {
            AddHistoryLine($"你：{question}");
        }

        AddHistoryLine($"河狸：{answer}");
        if (inputField != null)
        {
            inputField.text = string.Empty;
            inputField.ActivateInputField();
        }
    }

    private void SeedHistoryIfNeeded()
    {
        if (historyLines.Count > 0)
        {
            return;
        }

        AddHistoryLine("河狸：我会根据已修复的上一关建筑回答问题。");
        AddHistoryLine(BuildingKnowledgeLibrary.Answer(string.Empty, SceneManager.GetActiveScene().name, RuntimeProgressState.Instance ?? RuntimeProgressState.EnsureInstance()));
    }

    private void AddHistoryLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        historyLines.Add(line);
        while (historyLines.Count > 8)
        {
            historyLines.RemoveAt(0);
        }

        TmpRuntimeFontFallback.WarmupCharacters(string.Join("\n", historyLines));
        historyText.text = string.Join("\n", historyLines);
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Vector2 anchor)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetRect(rect, position, size, anchor, anchor, anchor);

        Image image = buttonObject.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(image, new Color(0.27f, 0.34f, 0.24f, 0.95f), 12, 10);

        TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", label, 24f, Color.white, TextAlignmentOptions.Center);
        text.enableWordWrapping = false;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string value, float size, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = TmpRuntimeFontFallback.WarmupCharacters(value) ?? TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        SetStretch(rect, 0f, 0f, 0f, 0f);
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetStretch(RectTransform rect, float left, float top, float right, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }
}
