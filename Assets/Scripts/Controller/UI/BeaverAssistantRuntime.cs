using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
                if (runtimeState.IsBuildingUnlocked(Entries[i].buildingId))
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
            if (!runtimeState.IsBuildingUnlocked(buildingId))
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
            return "我还不能讲太多。先解锁上一处建筑图鉴，相关知识就会在这里开放。";
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
                ? "河狸：解锁建筑图鉴后，下一段路线会更清晰。"
                : "河狸：专用结构齐了，就能直接点亮对应建筑图鉴。";
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
    private const string AvatarResourcePath = "UI/dabb2b31-d671-4717-9918-6d60739a0f10_no_bg";
    private const string LegacyBeaverButtonName = "Beaver";
    private const int SortingOrder = Dialog.TopmostRuntimeDialogSortingOrder - 20;
    private const float AvatarLeftMargin = 48f;
    private const float AvatarBottomMargin = 64f;
    private const float AvatarSize = 88f;
    private const float BubbleVisibleSeconds = 4.8f;
    private const float BubbleResumeMinVisibleSeconds = 2.6f;
    private const float LegacyButtonRescanInterval = 0.5f;
    private static readonly Vector2 BottomLeftAnchor = new Vector2(0f, 0f);

    private static BeaverAssistantHud instance;

    private Canvas canvas;
    private RectTransform canvasRect;
    private Button avatarButton;
    private TextMeshProUGUI bubbleText;
    private CanvasGroup bubbleGroup;
    private Sprite avatarSprite;
    private float bubbleUntilTime;
    private float nextAmbientTime;
    private string lastBubbleMessage;
    private float suspendedBubbleRemainingSeconds;
    private float nextLegacyButtonScanTime;
    private bool wasAssistantBlockedByRuntimeUi;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureInstance().HandleSceneChanged(SceneManager.GetActiveScene().name);
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstance().HandleSceneChanged(ResolveHudSceneName(scene, mode));
    }

    private static string ResolveHudSceneName(Scene loadedScene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive && IllustratedUISceneLoader.IsIllustratedUiScene(loadedScene))
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                return activeScene.name;
            }
        }

        return loadedScene.name;
    }

    public static BeaverAssistantHud EnsureInstance()
    {
        if (instance != null)
        {
            instance.EnsureUi();
            return instance;
        }

        BeaverAssistantHud existing = FindObjectOfType<BeaverAssistantHud>(true);
        if (existing != null)
        {
            instance = existing;
            instance.EnsureUi();
            return instance;
        }

        GameObject hudObject = new GameObject("BeaverAssistantHud");
        instance = hudObject.AddComponent<BeaverAssistantHud>();
        instance.EnsureUi();
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
        RefreshBlockingUiResumeState();
        RefreshUnblockedCanvasState();
        RefreshLegacyBeaverButtonBindingsIfNeeded();

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
        ApplyCanvasSupportState(sceneName);
        RefreshLegacyBeaverButtonBindings();
        ScheduleNextAmbient();
    }

    private void TryShowAmbientFact()
    {
        if (!IsSupportedScene(SceneManager.GetActiveScene().name))
        {
            return;
        }

        if (IsAssistantBlockedByRuntimeUi())
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
        lastBubbleMessage = message;
        bubbleText.text = message;
        bubbleUntilTime = Time.unscaledTime + BubbleVisibleSeconds;
        if (bubbleGroup != null)
        {
            bubbleGroup.alpha = 1f;
        }
    }

    private void RefreshBlockingUiResumeState()
    {
        bool blocked = IsAssistantBlockedByRuntimeUi();
        if (blocked && !wasAssistantBlockedByRuntimeUi)
        {
            suspendedBubbleRemainingSeconds = Mathf.Max(0f, bubbleUntilTime - Time.unscaledTime);
        }
        else if (!blocked && wasAssistantBlockedByRuntimeUi)
        {
            RestoreAfterBlockingUiClosed();
        }

        wasAssistantBlockedByRuntimeUi = blocked;
    }

    private void RefreshUnblockedCanvasState()
    {
        if (IsAssistantBlockedByRuntimeUi())
        {
            if (canvas != null && canvas.gameObject.activeSelf)
            {
                canvas.gameObject.SetActive(false);
            }

            return;
        }

        ApplyCanvasSupportState(SceneManager.GetActiveScene().name);
    }

    private void RestoreAfterBlockingUiClosed()
    {
        ApplyCanvasSupportState(SceneManager.GetActiveScene().name);

        if (suspendedBubbleRemainingSeconds > 0f && !string.IsNullOrWhiteSpace(lastBubbleMessage))
        {
            EnsureUi();
            TmpRuntimeFontFallback.WarmupCharacters(lastBubbleMessage);
            bubbleText.text = lastBubbleMessage;
            bubbleUntilTime = Time.unscaledTime + Mathf.Max(suspendedBubbleRemainingSeconds, BubbleResumeMinVisibleSeconds);
            if (bubbleGroup != null)
            {
                bubbleGroup.alpha = 1f;
            }
        }

        suspendedBubbleRemainingSeconds = 0f;
    }

    private static bool IsAssistantBlockedByRuntimeUi()
    {
        return BeaverAssistantPanel.IsOpen ||
               RuntimePauseMenu.IsPauseOpen ||
               (UIRootManager.Instance != null && UIRootManager.Instance.IsAnyGameplayBlockingUIOpen()) ||
               GameplayStageIntroDirector.IsIntroActive ||
               GameplayFailureController.IsFailureActive;
    }

    private void ApplyCanvasSupportState(string sceneName)
    {
        EnsureUi();
        bool supported = IsSupportedScene(sceneName);
        if (canvas != null && canvas.gameObject.activeSelf != supported)
        {
            canvas.gameObject.SetActive(supported);
        }
    }

    private void EnsureUi()
    {
        RuntimeUiEventSystemBootstrapper.Ensure();

        if (canvas != null && avatarButton != null && bubbleText != null)
        {
            EnsureCanvasSurface();
            EnsureAvatarButtonBinding(avatarButton);
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
        EnsureAvatarButtonBinding(avatarButton);
        RefreshLegacyBeaverButtonBindings();
    }

    private void EnsureCanvasSurface()
    {
        RuntimeUiEventSystemBootstrapper.Ensure();
        if (canvas == null)
        {
            return;
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = SortingOrder;
        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = true;
        }
    }

    private Button CreateAvatarButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("BeaverAvatarButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = BottomLeftAnchor;
        rect.anchorMax = BottomLeftAnchor;
        rect.pivot = BottomLeftAnchor;
        rect.anchoredPosition = new Vector2(AvatarLeftMargin, AvatarBottomMargin);
        rect.sizeDelta = new Vector2(AvatarSize, AvatarSize);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = GetOrCreateAvatarSprite();
        image.preserveAspect = true;
        image.color = Color.white;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        EnsureAvatarButtonBinding(button);
        return button;
    }

    private TextMeshProUGUI CreateBubble(Transform parent, out CanvasGroup group)
    {
        GameObject bubbleObject = new GameObject("BeaverBubble", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        RectTransform rect = bubbleObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = BottomLeftAnchor;
        rect.anchorMax = BottomLeftAnchor;
        rect.pivot = BottomLeftAnchor;
        rect.anchoredPosition = new Vector2(AvatarLeftMargin + AvatarSize + 16f, AvatarBottomMargin + 9f);
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

        avatarSprite = CreateAuthoredAvatarSprite();
        if (avatarSprite != null)
        {
            return avatarSprite;
        }

        avatarSprite = CreateFallbackAvatarSprite();
        return avatarSprite;
    }

    private static Sprite CreateAuthoredAvatarSprite()
    {
        Sprite sourceSprite = Resources.Load<Sprite>(AvatarResourcePath);
        Texture2D texture = sourceSprite != null ? sourceSprite.texture : Resources.Load<Texture2D>(AvatarResourcePath);
        if (texture == null)
        {
            return null;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Sprite sprite = Sprite.Create(
            texture,
            GetAuthoredAvatarSpriteRect(texture),
            new Vector2(0.5f, 0.5f),
            sourceSprite != null ? sourceSprite.pixelsPerUnit : 100f);
        sprite.name = "RuntimeBeaverAssistantAvatarSprite";
        return sprite;
    }

    private static Rect GetAuthoredAvatarSpriteRect(Texture2D texture)
    {
        const float sourceWidth = 2066f;
        const float sourceHeight = 761f;
        const float cropX = 175f;
        const float cropTop = 0f;
        const float cropWidth = 230f;
        const float cropHeight = 205f;

        return new Rect(
            Mathf.Round(texture.width * cropX / sourceWidth),
            Mathf.Round(texture.height * (sourceHeight - cropTop - cropHeight) / sourceHeight),
            Mathf.Round(texture.width * cropWidth / sourceWidth),
            Mathf.Round(texture.height * cropHeight / sourceHeight));
    }

    private static Sprite CreateFallbackAvatarSprite()
    {
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

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = "RuntimeBeaverFallbackAvatar";
        return sprite;
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

    private void RefreshLegacyBeaverButtonBindingsIfNeeded()
    {
        if (Time.unscaledTime < nextLegacyButtonScanTime)
        {
            return;
        }

        nextLegacyButtonScanTime = Time.unscaledTime + LegacyButtonRescanInterval;
        RefreshLegacyBeaverButtonBindings();
    }

    private void RefreshLegacyBeaverButtonBindings()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (!IsLegacyBeaverButton(button))
            {
                continue;
            }

            EnsureLegacyBeaverButtonClickable(button);
            button.onClick.RemoveListener(TogglePanelFromLegacyButton);
            button.onClick.AddListener(TogglePanelFromLegacyButton);
            if (button.GetComponent<BeaverAssistantLegacyButtonBinding>() == null)
            {
                button.gameObject.AddComponent<BeaverAssistantLegacyButtonBinding>();
            }
        }
    }

    private static void EnsureAvatarButtonBinding(Button button)
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
        button.onClick.RemoveListener(TogglePanelFromRuntimeAvatar);
        button.onClick.AddListener(TogglePanelFromRuntimeAvatar);
    }

    private static void EnsureLegacyBeaverButtonClickable(Button button)
    {
        if (button == null)
        {
            return;
        }

        Graphic targetGraphic = button.targetGraphic;
        if (targetGraphic == null)
        {
            targetGraphic = button.GetComponent<Graphic>();
            button.targetGraphic = targetGraphic;
        }

        if (targetGraphic != null)
        {
            targetGraphic.raycastTarget = true;
        }

        button.interactable = true;

        Canvas parentCanvas = button.GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            parentCanvas.overrideSorting = true;
            parentCanvas.sortingOrder = Mathf.Max(parentCanvas.sortingOrder, SortingOrder);
        }

        GraphicRaycaster raycaster = button.GetComponentInParent<GraphicRaycaster>();
        if (raycaster == null && parentCanvas != null)
        {
            raycaster = parentCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        if (raycaster != null)
        {
            raycaster.enabled = true;
        }
    }

    private static bool IsLegacyBeaverButton(Button button)
    {
        return button != null &&
               button.gameObject != null &&
               string.Equals(button.gameObject.name, LegacyBeaverButtonName, StringComparison.Ordinal);
    }

    private static void TogglePanelFromLegacyButton()
    {
        BeaverAssistantPanel.EnsureInstance().Toggle();
    }

    private static void TogglePanelFromRuntimeAvatar()
    {
        BeaverAssistantPanel.EnsureInstance().Toggle();
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

[DisallowMultipleComponent]
public sealed class BeaverAssistantLegacyButtonBinding : MonoBehaviour
{
}

public sealed class BeaverAssistantPanel : MonoBehaviour
{
    private const string PauseReason = "BeaverAssistant";
    private const int SortingOrder = Dialog.TopmostRuntimeDialogSortingOrder - 10;
    private const int MaxHistoryLines = 48;
    private const float HistoryScrollWidth = 796f;
    private const float HistoryScrollHeight = 292f;
    private const float HistoryTextWidth = 744f;
    private const float HistoryViewportHeight = 268f;
    private const float MessageMaxBubbleWidth = 540f;
    private const float MessageTextMaxWidth = 492f;
    private const float MessageMinBubbleWidth = 96f;
    private const float MessageMinHeight = 48f;
    private const float MessageHorizontalPadding = 18f;
    private const float MessageVerticalPadding = 12f;
    private const float MessageGap = 10f;

    private static BeaverAssistantPanel instance;

    private enum HistorySpeaker
    {
        Beaver,
        Player
    }

    private readonly struct HistoryEntry
    {
        public HistoryEntry(HistorySpeaker speaker, string message)
        {
            Speaker = speaker;
            Message = message;
        }

        public HistorySpeaker Speaker { get; }
        public string Message { get; }
    }

    private Canvas canvas;
    private CanvasGroup rootGroup;
    private ScrollRect historyScrollRect;
    private RectTransform historyContentRect;
    private TMP_InputField inputField;
    private Button closeButton;
    private Button askButton;
    private int lastSubmitFrame = -1;
    private readonly List<HistoryEntry> historyEntries = new List<HistoryEntry>();
    private readonly List<GameObject> historyRowObjects = new List<GameObject>();

    public static bool IsOpen => instance != null && instance.gameObject.activeInHierarchy;

    public static BeaverAssistantPanel EnsureInstance()
    {
        if (instance != null)
        {
            instance.EnsureUi();
            return instance;
        }

        BeaverAssistantPanel existing = FindObjectOfType<BeaverAssistantPanel>(true);
        if (existing != null)
        {
            instance = existing;
            instance.EnsureUi();
            return instance;
        }

        GameObject panelObject = new GameObject("BeaverAssistantPanel");
        instance = panelObject.AddComponent<BeaverAssistantPanel>();
        instance.EnsureUi();
        instance.gameObject.SetActive(false);
        return instance;
    }

    public static void HideForSceneTransition()
    {
        if (instance != null)
        {
            instance.HideImmediateForSceneTransition();
        }
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
        EnsureCanvasSurface();
        gameObject.SetActive(true);
        rootGroup.alpha = 1f;
        rootGroup.interactable = true;
        rootGroup.blocksRaycasts = true;
        UIRootManager.Instance?.HideBackpack();
        RuntimeGameplayPauseController.RequestPause(PauseReason);
        SeedHistoryIfNeeded();
        FocusInputField();
    }

    public void Hide()
    {
        DeactivatePanelSurface();
        RuntimeGameplayPauseController.ReleasePause(PauseReason);
        UIRootManager.Instance?.ShowBackpack();
    }

    private void HideImmediateForSceneTransition()
    {
        DeactivatePanelSurface();
        RuntimeGameplayPauseController.ReleasePause(PauseReason);
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
        RuntimeUiEventSystemBootstrapper.Ensure();

        if (canvas != null && historyScrollRect != null && historyContentRect != null && inputField != null)
        {
            EnsureCanvasSurface();
            EnsurePanelControlBindings();
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
        historyContentRect = CreateHistory(panel.transform);
        inputField = CreateInput(panel.transform);
        CreateAskButton(panel.transform);
        CreateTopicButtons(panel.transform);
        EnsurePanelControlBindings();
    }

    private void EnsureCanvasSurface()
    {
        RuntimeUiEventSystemBootstrapper.Ensure();
        if (canvas == null)
        {
            return;
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = SortingOrder;
        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = true;
        }
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

        closeButton = CreateButton(parent, "CloseButton", "×", new Vector2(-34f, -30f), new Vector2(52f, 42f), new Vector2(1f, 1f));
        BindCloseButton(closeButton);
    }

    private void BindCloseButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        EnsureButtonInteractive(button);
        button.onClick.RemoveListener(Hide);
        button.onClick.AddListener(Hide);
        BeaverAssistantPanelCloseButtonFallback fallback = button.gameObject.GetComponent<BeaverAssistantPanelCloseButtonFallback>();
        if (fallback == null)
        {
            fallback = button.gameObject.AddComponent<BeaverAssistantPanelCloseButtonFallback>();
        }

        fallback.Configure(this);
    }

    private RectTransform CreateHistory(Transform parent)
    {
        GameObject scrollObject = new GameObject("HistoryScroll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        scrollRect.SetParent(parent, false);
        SetRect(scrollRect, new Vector2(32f, -92f), new Vector2(HistoryScrollWidth, HistoryScrollHeight), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));

        Image background = scrollObject.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(background, new Color(0.05f, 0.06f, 0.05f, 0.42f), 10, 10);
        background.raycastTarget = true;

        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.SetParent(scrollRect, false);
        SetStretch(viewportRect, 16f, 12f, 36f, 12f);

        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
        viewportImage.raycastTarget = true;

        GameObject contentObject = new GameObject("Content", typeof(RectTransform));
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.SetParent(viewportRect, false);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, HistoryViewportHeight);

        Scrollbar scrollbar = CreateHistoryScrollbar(scrollRect);
        historyScrollRect = scrollObject.GetComponent<ScrollRect>();
        historyScrollRect.viewport = viewportRect;
        historyScrollRect.content = contentRect;
        historyScrollRect.horizontal = false;
        historyScrollRect.vertical = true;
        historyScrollRect.movementType = ScrollRect.MovementType.Clamped;
        historyScrollRect.scrollSensitivity = 42f;
        historyScrollRect.verticalScrollbar = scrollbar;
        historyScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        historyScrollRect.verticalNormalizedPosition = 0f;
        return contentRect;
    }

    private static Scrollbar CreateHistoryScrollbar(Transform parent)
    {
        GameObject scrollbarObject = new GameObject("HistoryScrollbar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
        RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
        scrollbarRect.SetParent(parent, false);
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 0.5f);
        scrollbarRect.anchoredPosition = new Vector2(-14f, 0f);
        scrollbarRect.sizeDelta = new Vector2(8f, -24f);

        Image track = scrollbarObject.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(track, new Color(0.20f, 0.25f, 0.19f, 0.32f), 4, 4);

        GameObject slidingAreaObject = new GameObject("SlidingArea", typeof(RectTransform));
        RectTransform slidingArea = slidingAreaObject.GetComponent<RectTransform>();
        slidingArea.SetParent(scrollbarRect, false);
        SetStretch(slidingArea, 0f, 0f, 0f, 0f);

        GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform handle = handleObject.GetComponent<RectTransform>();
        handle.SetParent(slidingArea, false);
        SetStretch(handle, 0f, 0f, 0f, 0f);

        Image handleImage = handleObject.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(handleImage, new Color(0.82f, 0.74f, 0.52f, 0.86f), 4, 4);

        Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handleImage;
        return scrollbar;
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
        EnsureInputFieldInteractive(field);
        field.onSubmit.AddListener(_ => SubmitQuestion());
        BeaverAssistantPanelInputFocusProxy focusProxy = inputObject.GetComponent<BeaverAssistantPanelInputFocusProxy>();
        if (focusProxy == null)
        {
            focusProxy = inputObject.AddComponent<BeaverAssistantPanelInputFocusProxy>();
        }

        focusProxy.Configure(field);
        return field;
    }

    private void CreateAskButton(Transform parent)
    {
        askButton = CreateButton(parent, "AskButton", "询问", new Vector2(-32f, 90f), new Vector2(158f, 58f), new Vector2(1f, 0f));
        BindAskButton(askButton);
    }

    private void BindAskButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        EnsureButtonInteractive(button);
        button.onClick.RemoveListener(SubmitQuestion);
        button.onClick.AddListener(SubmitQuestion);
        BeaverAssistantPanelAskButtonFallback fallback = button.gameObject.GetComponent<BeaverAssistantPanelAskButtonFallback>();
        if (fallback == null)
        {
            fallback = button.gameObject.AddComponent<BeaverAssistantPanelAskButtonFallback>();
        }

        fallback.Configure(this);
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
        if (lastSubmitFrame == Time.frameCount)
        {
            return;
        }

        lastSubmitFrame = Time.frameCount;
        string question = inputField != null ? inputField.text.Trim() : string.Empty;
        string answer = BuildingKnowledgeLibrary.Answer(
            question,
            SceneManager.GetActiveScene().name,
            RuntimeProgressState.Instance ?? RuntimeProgressState.EnsureInstance());

        AddHistoryEntry(HistorySpeaker.Player, question);
        AddHistoryEntry(HistorySpeaker.Beaver, answer);
        if (inputField != null)
        {
            inputField.text = string.Empty;
            inputField.ActivateInputField();
        }
    }

    internal void SubmitQuestionFromControl()
    {
        SubmitQuestion();
    }

    private void SeedHistoryIfNeeded()
    {
        if (historyEntries.Count > 0)
        {
            return;
        }

        AddHistoryEntry(HistorySpeaker.Beaver, "我会根据已修复的上一关建筑回答问题。");
        AddHistoryEntry(
            HistorySpeaker.Beaver,
            BuildingKnowledgeLibrary.Answer(string.Empty, SceneManager.GetActiveScene().name, RuntimeProgressState.Instance ?? RuntimeProgressState.EnsureInstance()));
    }

    private void AddHistoryLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        if (line.StartsWith("你：", StringComparison.Ordinal))
        {
            AddHistoryEntry(HistorySpeaker.Player, line.Substring(2).TrimStart());
            return;
        }

        if (line.StartsWith("河狸：", StringComparison.Ordinal))
        {
            AddHistoryEntry(HistorySpeaker.Beaver, line.Substring(3).TrimStart());
            return;
        }

        AddHistoryEntry(HistorySpeaker.Beaver, line);
    }

    private void AddHistoryEntry(HistorySpeaker speaker, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        historyEntries.Add(new HistoryEntry(speaker, message.Trim()));
        while (historyEntries.Count > MaxHistoryLines)
        {
            historyEntries.RemoveAt(0);
        }

        RefreshHistoryLayout();
    }

    private void RefreshHistoryLayout()
    {
        if (historyContentRect == null || historyScrollRect == null)
        {
            return;
        }

        ClearHistoryRows();

        float currentY = 0f;
        string warmupText = GetHistoryWarmupText();
        TmpRuntimeFontFallback.WarmupCharacters(warmupText);

        for (int i = 0; i < historyEntries.Count; i++)
        {
            GameObject row = CreateHistoryRow(historyEntries[i], i, currentY, out float rowHeight);
            historyRowObjects.Add(row);
            currentY += rowHeight + MessageGap;
        }

        float contentHeight = Mathf.Max(HistoryViewportHeight, Mathf.Ceil(Mathf.Max(0f, currentY - MessageGap)));
        historyContentRect.sizeDelta = new Vector2(0f, contentHeight);
        Canvas.ForceUpdateCanvases();
        historyScrollRect.verticalNormalizedPosition = 0f;
    }

    private GameObject CreateHistoryRow(HistoryEntry entry, int index, float topOffset, out float rowHeight)
    {
        string rowPrefix = entry.Speaker == HistorySpeaker.Player ? "Player" : "Beaver";
        GameObject rowObject = new GameObject($"{rowPrefix}MessageRow_{index:D2}", typeof(RectTransform));
        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.SetParent(historyContentRect, false);
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, -topOffset);

        GameObject bubbleObject = new GameObject($"{rowPrefix}MessageBubble_{index:D2}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform bubbleRect = bubbleObject.GetComponent<RectTransform>();
        bubbleRect.SetParent(rowRect, false);
        bool player = entry.Speaker == HistorySpeaker.Player;
        Vector2 horizontalAnchor = player ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        bubbleRect.anchorMin = horizontalAnchor;
        bubbleRect.anchorMax = horizontalAnchor;
        bubbleRect.pivot = horizontalAnchor;
        bubbleRect.anchoredPosition = Vector2.zero;

        Image bubbleImage = bubbleObject.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(
            bubbleImage,
            player ? new Color(0.27f, 0.34f, 0.24f, 0.96f) : new Color(0.07f, 0.09f, 0.08f, 0.78f),
            12,
            10);
        bubbleImage.raycastTarget = false;

        TextMeshProUGUI text = CreateText(
            bubbleObject.transform,
            "MessageText",
            entry.Message,
            23f,
            new Color(0.93f, 0.92f, 0.86f, 1f),
            player ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft);
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        SetStretch(text.rectTransform, MessageHorizontalPadding, MessageVerticalPadding, MessageHorizontalPadding, MessageVerticalPadding);

        Vector2 preferred = text.GetPreferredValues(entry.Message, MessageTextMaxWidth, 0f);
        float bubbleWidth = Mathf.Clamp(
            Mathf.Ceil(preferred.x + MessageHorizontalPadding * 2f),
            MessageMinBubbleWidth,
            MessageMaxBubbleWidth);
        float textWidth = Mathf.Max(1f, bubbleWidth - MessageHorizontalPadding * 2f);
        float preferredHeight = text.GetPreferredValues(entry.Message, textWidth, 0f).y;
        rowHeight = Mathf.Max(MessageMinHeight, Mathf.Ceil(preferredHeight + MessageVerticalPadding * 2f));
        rowRect.sizeDelta = new Vector2(0f, rowHeight);
        bubbleRect.sizeDelta = new Vector2(bubbleWidth, rowHeight);
        return rowObject;
    }

    private void ClearHistoryRows()
    {
        for (int i = 0; i < historyRowObjects.Count; i++)
        {
            GameObject row = historyRowObjects[i];
            if (row == null)
            {
                continue;
            }

            row.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(row);
            }
            else
            {
                DestroyImmediate(row);
            }
        }

        historyRowObjects.Clear();
    }

    private string GetHistoryWarmupText()
    {
        List<string> values = new List<string>(historyEntries.Count);
        for (int i = 0; i < historyEntries.Count; i++)
        {
            values.Add(historyEntries[i].Message);
        }

        return string.Join("\n", values);
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
        EnsureButtonInteractive(button);
        return button;
    }

    private void EnsurePanelControlBindings()
    {
        if (closeButton == null)
        {
            closeButton = transform.Find("BeaverAssistantPanelCanvas/Panel/CloseButton")?.GetComponent<Button>();
        }

        if (askButton == null)
        {
            askButton = transform.Find("BeaverAssistantPanelCanvas/Panel/AskButton")?.GetComponent<Button>();
        }

        BindCloseButton(closeButton);
        BindAskButton(askButton);

        if (inputField != null)
        {
            EnsureInputFieldInteractive(inputField);
            BeaverAssistantPanelInputFocusProxy focusProxy = inputField.GetComponent<BeaverAssistantPanelInputFocusProxy>();
            if (focusProxy == null)
            {
                focusProxy = inputField.gameObject.AddComponent<BeaverAssistantPanelInputFocusProxy>();
            }

            focusProxy.Configure(inputField);
        }
    }

    private void FocusInputField()
    {
        if (inputField == null)
        {
            return;
        }

        EnsureInputFieldInteractive(inputField);
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(inputField.gameObject);
        }

        inputField.Select();
        inputField.ActivateInputField();
    }

    private void DeactivatePanelSurface()
    {
        if (inputField != null)
        {
            inputField.DeactivateInputField();
        }

        ClearSelectedObjectIfOwnedByPanel();

        if (rootGroup != null)
        {
            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }

        if (canvas != null)
        {
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = false;
            }
        }

        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    private void ClearSelectedObjectIfOwnedByPanel()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
        {
            return;
        }

        Transform selectedTransform = eventSystem.currentSelectedGameObject.transform;
        if (selectedTransform != null && selectedTransform.IsChildOf(transform))
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }

    internal static void EnsureInputFieldInteractive(TMP_InputField field)
    {
        if (field == null)
        {
            return;
        }

        field.enabled = true;
        field.interactable = true;
        field.readOnly = false;

        Graphic targetGraphic = field.targetGraphic ?? field.GetComponent<Graphic>();
        if (targetGraphic != null)
        {
            targetGraphic.raycastTarget = true;
            field.targetGraphic = targetGraphic;
        }
    }

    internal static void EnsureButtonInteractive(Button button)
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

        TextMeshProUGUI[] labels = button.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] != null)
            {
                labels[i].raycastTarget = false;
            }
        }
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

[DisallowMultipleComponent]
public sealed class BeaverAssistantPanelCloseButtonFallback : MonoBehaviour, IPointerUpHandler, IPointerClickHandler, ISubmitHandler
{
    private BeaverAssistantPanel owner;

    public void Configure(BeaverAssistantPanel panel)
    {
        owner = panel;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        Close();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        Close();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        Close();
    }

    private void Close()
    {
        if (owner == null)
        {
            owner = GetComponentInParent<BeaverAssistantPanel>();
        }

        owner?.Hide();
    }
}

[DisallowMultipleComponent]
public sealed class BeaverAssistantPanelAskButtonFallback : MonoBehaviour, IPointerClickHandler, ISubmitHandler
{
    private BeaverAssistantPanel owner;

    public void Configure(BeaverAssistantPanel panel)
    {
        owner = panel;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        Submit();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        Submit();
    }

    private void Submit()
    {
        if (owner == null)
        {
            owner = GetComponentInParent<BeaverAssistantPanel>();
        }

        owner?.SubmitQuestionFromControl();
    }
}

[DisallowMultipleComponent]
public sealed class BeaverAssistantPanelInputFocusProxy : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
{
    private TMP_InputField field;

    public void Configure(TMP_InputField inputField)
    {
        field = inputField;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Focus(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Focus(eventData);
    }

    private void Focus(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (field == null)
        {
            field = GetComponent<TMP_InputField>();
        }

        if (field == null)
        {
            return;
        }

        BeaverAssistantPanel.EnsureInputFieldInteractive(field);
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(field.gameObject, eventData);
        }

        field.Select();
        field.ActivateInputField();
    }
}

internal static class RuntimeUiEventSystemBootstrapper
{
    public static void Ensure()
    {
        EventSystem eventSystem = EventSystem.current ?? UnityEngine.Object.FindObjectOfType<EventSystem>(true);
        if (eventSystem == null)
        {
            eventSystem = CreateEventSystem();
        }

        if (!eventSystem.gameObject.activeSelf)
        {
            eventSystem.gameObject.SetActive(true);
        }

        if (!eventSystem.gameObject.activeInHierarchy)
        {
            eventSystem = CreateEventSystem();
        }

        eventSystem.enabled = true;
        BaseInputModule inputModule = eventSystem.GetComponent<BaseInputModule>();
        if (inputModule == null)
        {
            inputModule = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }

        inputModule.enabled = true;
    }

    private static EventSystem CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem");
        return eventSystemObject.AddComponent<EventSystem>();
    }
}
