using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class RuntimeSubtitleFeedHud : MonoBehaviour
{
    private const string CanvasName = "RuntimeSubtitleFeedHudCanvas";
    private const string RootName = "RuntimeSubtitleFeedHudRoot";
    private const string HudFontResourcePath = "Fonts & Materials/LiberationSans SDF";
    private const int SortingOrder = 236;
    private const int MaxVisibleEntries = 4;
    private const float RootWidth = 520f;
    private const float RootPadding = 4f;
    private const float RowHeight = 28f;
    private const float RowSpacing = 4f;
    private const float EntryAnimationDuration = 0.22f;
    private const float EntrySlideDistance = 12f;

    private static readonly Color[] RowTextColors =
    {
        new Color(0.98f, 0.98f, 0.96f, 1f),
        new Color(0.95f, 0.95f, 0.92f, 0.90f),
        new Color(0.91f, 0.92f, 0.89f, 0.78f),
        new Color(0.86f, 0.87f, 0.84f, 0.66f)
    };
    private static readonly Color[] RowShadowColors =
    {
        new Color(0.02f, 0.03f, 0.04f, 0.96f),
        new Color(0.02f, 0.03f, 0.04f, 0.82f),
        new Color(0.02f, 0.03f, 0.04f, 0.68f),
        new Color(0.02f, 0.03f, 0.04f, 0.56f)
    };

    public static RuntimeSubtitleFeedHud Instance { get; private set; }

    private static bool externallyHidden;

    private readonly List<SubtitleEntry> entries = new List<SubtitleEntry>();
    private readonly List<RectTransform> rowRects = new List<RectTransform>();
    private readonly List<CanvasGroup> rowCanvasGroups = new List<CanvasGroup>();
    private readonly List<TextMeshProUGUI> rowShadowTexts = new List<TextMeshProUGUI>();
    private readonly List<TextMeshProUGUI> rowTexts = new List<TextMeshProUGUI>();

    private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform rootRect;
    private TMP_FontAsset hudFontAsset;
    private BackpackMananger backpack;
    private bool subscribed;
    private bool sceneSupported;
    private Coroutine newestEntryAnimation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        RuntimeSubtitleFeedHud hud = EnsureInstance();
        if (hud != null)
        {
            hud.HandleSceneChanged(SceneManager.GetActiveScene().name);
        }
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RuntimeSubtitleFeedHud hud = EnsureInstance();
        if (hud != null)
        {
            hud.HandleSceneChanged(scene.name);
        }
    }

    public static RuntimeSubtitleFeedHud EnsureInstance()
    {
        if (Instance != null)
        {
            Instance.TrySubscribe();
            return Instance;
        }

        RuntimeSubtitleFeedHud existing = FindObjectOfType<RuntimeSubtitleFeedHud>(true);
        if (existing != null)
        {
            Instance = existing;
            existing.TrySubscribe();
            return existing;
        }

        GameObject runtimeObject = new GameObject("RuntimeSubtitleFeedHud");
        Instance = runtimeObject.AddComponent<RuntimeSubtitleFeedHud>();
        return Instance;
    }

    public static void SetExternallyHidden(bool hidden)
    {
        externallyHidden = hidden;

        if (Instance != null)
        {
            Instance.RefreshVisibility();
        }
    }

    public static void PushMessage(string message)
    {
        RuntimeSubtitleFeedHud hud = EnsureInstance();
        if (hud == null)
        {
            return;
        }

        hud.PushMessageInternal(message);
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
        CleanupLegacySelfCanvas();
        EnsureUi();
        HandleSceneChanged(SceneManager.GetActiveScene().name);
        TrySubscribe();
    }

    private void Update()
    {
        if (!subscribed)
        {
            TrySubscribe();
        }
    }

    private void OnDestroy()
    {
        Unsubscribe();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void HandleSceneChanged(string sceneName)
    {
        sceneSupported = GameplayStageCatalog.IsGameplayScene(sceneName);
        ClearMessages();
        RefreshVisibility();
        TrySubscribe();
    }

    private void TrySubscribe()
    {
        BackpackMananger latestBackpack = BackpackMananger.Instance;
        if (latestBackpack == null)
        {
            return;
        }

        if (backpack == latestBackpack && subscribed)
        {
            return;
        }

        Unsubscribe();
        backpack = latestBackpack;
        backpack.OnItemPicked += HandleItemPicked;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || backpack == null)
        {
            subscribed = false;
            backpack = null;
            return;
        }

        backpack.OnItemPicked -= HandleItemPicked;
        subscribed = false;
        backpack = null;
    }

    private void HandleItemPicked(ArchitecturalCrystal crystal)
    {
        if (!sceneSupported)
        {
            return;
        }

        PushMessageInternal(BuildPickupMessage(crystal));
    }

    private void PushMessageInternal(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        EnsureUi();

        string normalizedMessage = message.Trim();
        int lastIndex = entries.Count - 1;
        if (lastIndex >= 0 && entries[lastIndex].baseText == normalizedMessage)
        {
            SubtitleEntry latestEntry = entries[lastIndex];
            latestEntry.count++;
            entries[lastIndex] = latestEntry;
        }
        else
        {
            entries.Add(new SubtitleEntry(normalizedMessage));
            if (entries.Count > MaxVisibleEntries)
            {
                entries.RemoveAt(0);
            }
        }

        RefreshRows();
        PlayNewestEntryAnimation();
    }

    private void ClearMessages()
    {
        entries.Clear();

        if (newestEntryAnimation != null)
        {
            StopCoroutine(newestEntryAnimation);
            newestEntryAnimation = null;
        }

        EnsureUi();
        RefreshRows();
    }

    private void RefreshVisibility()
    {
        EnsureUi();

        bool shouldShowCanvas = sceneSupported;
        if (canvas != null)
        {
            canvas.gameObject.SetActive(shouldShowCanvas);
        }

        if (rootRect != null)
        {
            rootRect.gameObject.SetActive(shouldShowCanvas && !externallyHidden && entries.Count > 0);
        }
    }

    private void EnsureUi()
    {
        if (canvas != null &&
            canvasRect != null &&
            rootRect != null &&
            rowTexts.Count == MaxVisibleEntries &&
            rowShadowTexts.Count == MaxVisibleEntries)
        {
            CleanupLegacyVisuals();
            return;
        }

        EnsureCanvasInfrastructure();

        if (canvasRect == null)
        {
            return;
        }

        if (rootRect == null)
        {
            GameObject rootObject = new GameObject(RootName, typeof(RectTransform));
            rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.SetParent(canvasRect, false);
            rootRect.anchorMin = new Vector2(0f, 0f);
            rootRect.anchorMax = new Vector2(0f, 0f);
            rootRect.pivot = new Vector2(0f, 0f);
            rootRect.anchoredPosition = new Vector2(22f, 18f);
            rootRect.sizeDelta = new Vector2(
                RootWidth,
                RootPadding * 2f + MaxVisibleEntries * RowHeight + (MaxVisibleEntries - 1) * RowSpacing);
        }
        else if (rootRect.parent != canvasRect)
        {
            rootRect.SetParent(canvasRect, false);
        }

        CleanupLegacyVisuals();
        RebuildRows();
    }

    private void EnsureCanvasInfrastructure()
    {
        if (canvas != null && canvasRect != null)
        {
            return;
        }

        Transform existingCanvas = transform.Find(CanvasName);
        GameObject canvasObject = existingCanvas != null
            ? existingCanvas.gameObject
            : new GameObject(
                CanvasName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = canvasObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = SortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvasObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = false;
        }

        canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;
        canvasRect.localScale = Vector3.one;
    }

    private void CleanupLegacySelfCanvas()
    {
        Canvas legacyCanvas = GetComponent<Canvas>();
        if (legacyCanvas != null)
        {
            Destroy(legacyCanvas);
        }

        CanvasScaler legacyScaler = GetComponent<CanvasScaler>();
        if (legacyScaler != null)
        {
            Destroy(legacyScaler);
        }

        GraphicRaycaster legacyRaycaster = GetComponent<GraphicRaycaster>();
        if (legacyRaycaster != null)
        {
            Destroy(legacyRaycaster);
        }
    }

    private void RebuildRows()
    {
        if (rootRect == null)
        {
            return;
        }

        for (int i = rootRect.childCount - 1; i >= 0; i--)
        {
            Destroy(rootRect.GetChild(i).gameObject);
        }

        rowRects.Clear();
        rowCanvasGroups.Clear();
        rowShadowTexts.Clear();
        rowTexts.Clear();

        for (int i = 0; i < MaxVisibleEntries; i++)
        {
            CreateRow(i);
        }
    }

    private void CleanupLegacyVisuals()
    {
        if (rootRect == null)
        {
            return;
        }

        Image legacyRootImage = rootRect.GetComponent<Image>();
        if (legacyRootImage != null)
        {
            Destroy(legacyRootImage);
        }

        for (int i = 0; i < rootRect.childCount; i++)
        {
            Transform row = rootRect.GetChild(i);
            Image legacyRowImage = row.GetComponent<Image>();
            if (legacyRowImage != null)
            {
                Destroy(legacyRowImage);
            }
        }
    }

    private void CreateRow(int index)
    {
        GameObject rowObject = new GameObject(
            $"SubtitleRow_{index}",
            typeof(RectTransform),
            typeof(CanvasGroup));
        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.SetParent(rootRect, false);
        rowRect.anchorMin = new Vector2(0f, 0f);
        rowRect.anchorMax = new Vector2(0f, 0f);
        rowRect.pivot = new Vector2(0f, 0f);
        rowRect.anchoredPosition = GetRowBasePosition(index);
        rowRect.sizeDelta = new Vector2(RootWidth - RootPadding * 2f, RowHeight);

        CanvasGroup canvasGroup = rowObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        TextMeshProUGUI shadowText = CreateRowText(
            rowRect,
            "ShadowText",
            new Vector2(2f, -2f),
            RowShadowColors[Mathf.Min(index, RowShadowColors.Length - 1)]);
        TextMeshProUGUI text = CreateRowText(
            rowRect,
            "Text",
            Vector2.zero,
            RowTextColors[Mathf.Min(index, RowTextColors.Length - 1)]);

        rowRects.Add(rowRect);
        rowCanvasGroups.Add(canvasGroup);
        rowShadowTexts.Add(shadowText);
        rowTexts.Add(text);
    }

    private TextMeshProUGUI CreateRowText(Transform parent, string objectName, Vector2 offset, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(parent, false);
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = offset;
        textRect.offsetMax = offset;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = ResolveHudFont();
        text.fontSize = 23f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.color = color;
        text.text = string.Empty;
        text.raycastTarget = false;
        return text;
    }

    private void RefreshRows()
    {
        int visibleCount = Mathf.Min(entries.Count, MaxVisibleEntries);

        for (int i = 0; i < MaxVisibleEntries; i++)
        {
            bool shouldShowRow = i < visibleCount;
            RectTransform rowRect = rowRects[i];
            CanvasGroup rowCanvasGroup = rowCanvasGroups[i];
            TextMeshProUGUI rowShadowText = rowShadowTexts[i];
            TextMeshProUGUI rowText = rowTexts[i];

            rowRect.anchoredPosition = GetRowBasePosition(i);
            rowRect.localScale = Vector3.one;

            if (!shouldShowRow)
            {
                rowCanvasGroup.alpha = 0f;
                rowShadowText.text = string.Empty;
                rowText.text = string.Empty;
                continue;
            }

            SubtitleEntry entry = entries[entries.Count - 1 - i];
            TmpRuntimeFontFallback.WarmupCharacters(entry.DisplayText);
            rowCanvasGroup.alpha = 1f;
            rowText.color = RowTextColors[Mathf.Min(i, RowTextColors.Length - 1)];
            rowShadowText.color = RowShadowColors[Mathf.Min(i, RowShadowColors.Length - 1)];
            rowShadowText.text = entry.DisplayText;
            rowText.text = entry.DisplayText;
        }

        RefreshVisibility();
    }

    private void PlayNewestEntryAnimation()
    {
        if (rowRects.Count == 0 || rowCanvasGroups.Count == 0)
        {
            return;
        }

        if (newestEntryAnimation != null)
        {
            StopCoroutine(newestEntryAnimation);
        }

        newestEntryAnimation = StartCoroutine(AnimateNewestEntry());
    }

    private IEnumerator AnimateNewestEntry()
    {
        RectTransform newestRow = rowRects[0];
        CanvasGroup newestCanvasGroup = rowCanvasGroups[0];
        Vector2 targetPosition = GetRowBasePosition(0);
        Vector2 startPosition = targetPosition + Vector2.down * EntrySlideDistance;
        float elapsed = 0f;

        newestCanvasGroup.alpha = 0f;
        newestRow.anchoredPosition = startPosition;
        newestRow.localScale = new Vector3(0.985f, 0.985f, 1f);

        while (elapsed < EntryAnimationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / EntryAnimationDuration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            float pop = Mathf.Sin(eased * Mathf.PI) * 0.015f;

            newestCanvasGroup.alpha = eased;
            newestRow.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, eased);
            newestRow.localScale = Vector3.one * (Mathf.Lerp(0.985f, 1f, eased) + pop);
            yield return null;
        }

        newestCanvasGroup.alpha = 1f;
        newestRow.anchoredPosition = targetPosition;
        newestRow.localScale = Vector3.one;
        newestEntryAnimation = null;
    }

    private TMP_FontAsset ResolveHudFont()
    {
        if (hudFontAsset != null)
        {
            return hudFontAsset;
        }

        hudFontAsset = TmpRuntimeFontFallback.EnsureChineseFallback();
        if (hudFontAsset != null)
        {
            return hudFontAsset;
        }

        hudFontAsset = Resources.Load<TMP_FontAsset>(HudFontResourcePath);
        if (hudFontAsset != null)
        {
            return hudFontAsset;
        }

        hudFontAsset = TMP_Settings.defaultFontAsset;
        return hudFontAsset;
    }

    private static Vector2 GetRowBasePosition(int index)
    {
        return new Vector2(
            RootPadding,
            RootPadding + index * (RowHeight + RowSpacing));
    }

    private static string BuildPickupMessage(ArchitecturalCrystal crystal)
    {
        if (crystal.IsSpecialStructure)
        {
            return "拾取了专用结构材料";
        }

        if (crystal.IsInkSupply)
        {
            return crystal.inkRestoreValue > 0
                ? $"拾取了{crystal.DisplayName}，墨笔耐久 +{crystal.inkRestoreValue}"
                : $"拾取了{crystal.DisplayName}";
        }

        return $"拾取了{crystal.DisplayName}";
    }

    private struct SubtitleEntry
    {
        public string baseText;
        public int count;

        public SubtitleEntry(string baseText)
        {
            this.baseText = baseText;
            count = 1;
        }

        public string DisplayText => count > 1 ? $"{baseText} ×{count}" : baseText;
    }
}
