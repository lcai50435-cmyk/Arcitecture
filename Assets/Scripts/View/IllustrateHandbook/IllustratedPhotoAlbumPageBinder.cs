using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class IllustratedPhotoAlbumPageBinder
{
    public const int EntriesPerPage = 12;

    private const string RuntimeTextureName = "RuntimePhotoTexture";
    private const string RuntimePreviousButtonName = "RuntimePhotoAlbumPreviousPage";
    private const string RuntimeNextButtonName = "RuntimePhotoAlbumNextPage";
    private const string RuntimeDeleteButtonName = "RuntimePhotoAlbumDeleteSelected";
    private const string RuntimeDeleteConfirmRootName = "RuntimePhotoAlbumDeleteConfirm";
    private const string RuntimeConfirmDeleteButtonName = "RuntimePhotoAlbumConfirmDelete";
    private const string RuntimeCancelDeleteButtonName = "RuntimePhotoAlbumCancelDelete";
    private const string RuntimeStageDropdownName = "RuntimePhotoAlbumStageDropdown";
    private const string RuntimeSortDropdownName = "RuntimePhotoAlbumSortDropdown";
    private const string BaseStageFilterValue = "__base__";
    private const string SceneDeleteButtonName = "DeleButton";
    private const float DeleteButtonShareGap = 8f;
    private const float ScenePagingButtonMaxHorizontalDistance = 160f;
    private const float FilterDropdownMinWidth = 92f;
    private const float FilterDropdownMinHeight = 22f;
    private const float FilterDropdownHorizontalGap = 6f;
    private const float FilterDropdownCaptionFontSize = 9.5f;
    private const float FilterDropdownItemFontSize = 9f;
    private const float FilterDropdownTextHorizontalInset = 6f;
    private const float FilterDropdownTextVerticalInset = 1f;
    private const float FilterDropdownArrowInset = 18f;
    private const float PreviewTitleFontSize = 10f;
    private const float PreviewTitleMinFontSize = 6f;
    private const float PreviewDescriptionFontSize = 7f;
    private const float PreviewDescriptionMinFontSize = 5f;
    private const float PreviewTitleBottomPadding = 13f;
    private const float PreviewDescriptionBottomPadding = 36f;
    private static readonly Color DeleteConfirmOverlayColor = new Color(0.05f, 0.04f, 0.03f, 0.54f);
    private static readonly Color DeleteConfirmPanelColor = new Color(0.72f, 0.60f, 0.40f, 0.98f);
    private static readonly Color DeleteConfirmTitleColor = new Color(0.13f, 0.09f, 0.04f, 1f);
    private static readonly Color DeleteConfirmBodyColor = new Color(0.20f, 0.15f, 0.08f, 1f);
    private static readonly Color DeleteConfirmDangerColor = new Color(0.54f, 0.18f, 0.12f, 0.98f);
    private static readonly Color DeleteConfirmCancelColor = new Color(0.39f, 0.31f, 0.19f, 0.94f);

    private readonly Func<IReadOnlyList<PhotoAlbumEntry>> entryLoader;
    private readonly Func<PhotoAlbumEntry, Texture2D> textureLoader;
    private readonly Func<PhotoAlbumEntry, bool> entryDeleter;
    private readonly bool destroyLoadedTextures;
    private readonly List<PhotoAlbumEntry> sourceEntries = new List<PhotoAlbumEntry>();
    private readonly List<PhotoAlbumEntry> entries = new List<PhotoAlbumEntry>();
    private readonly List<Dropdown> sortFilterDropdowns = new List<Dropdown>();
    private readonly List<TMP_Dropdown> tmpSortFilterDropdowns = new List<TMP_Dropdown>();
    private readonly List<RectTransform> slotRects = new List<RectTransform>();
    private readonly List<RawImage> slotImages = new List<RawImage>();
    private readonly List<Texture2D> loadedTextures = new List<Texture2D>();

    private RectTransform root;
    private RawImage previewImage;
    private TMP_Text titleText;
    private TMP_Text descriptionText;
    private TMP_Text timeText;
    private TMP_Text sceneText;
    private TMP_Text unlockText;
    private TMP_Text pageNumberText;
    private Button previousPageButton;
    private Button nextPageButton;
    private Button deleteSelectedButton;
    private RectTransform deleteConfirmRoot;
    private CanvasGroup deleteConfirmCanvasGroup;
    private PhotoAlbumSortMode selectedSortMode = PhotoAlbumSortMode.Time;
    private bool suppressSortDropdownEvents;
    private int currentPageIndex;
    private int selectedEntryIndex = -1;

    private enum PhotoAlbumSortMode
    {
        Time = 0,
        Stage = 1
    }

    public IllustratedPhotoAlbumPageBinder()
        : this(PhotoAlbumRepository.LoadEntries, PhotoAlbumRepository.LoadTexture, PhotoAlbumRepository.DeleteEntry, true)
    {
    }

    public IllustratedPhotoAlbumPageBinder(
        Func<IReadOnlyList<PhotoAlbumEntry>> entryLoader,
        Func<PhotoAlbumEntry, Texture2D> textureLoader,
        bool destroyLoadedTextures)
        : this(entryLoader, textureLoader, PhotoAlbumRepository.DeleteEntry, destroyLoadedTextures)
    {
    }

    public IllustratedPhotoAlbumPageBinder(
        Func<IReadOnlyList<PhotoAlbumEntry>> entryLoader,
        Func<PhotoAlbumEntry, Texture2D> textureLoader,
        Func<PhotoAlbumEntry, bool> entryDeleter,
        bool destroyLoadedTextures)
    {
        this.entryLoader = entryLoader ?? PhotoAlbumRepository.LoadEntries;
        this.textureLoader = textureLoader ?? PhotoAlbumRepository.LoadTexture;
        this.entryDeleter = entryDeleter ?? PhotoAlbumRepository.DeleteEntry;
        this.destroyLoadedTextures = destroyLoadedTextures;
    }

    public void Bind(RectTransform pageRoot)
    {
        if (root == pageRoot)
        {
            return;
        }

        ClearVisuals();
        root = pageRoot;
        ResolveTargets();
    }

    public void Refresh()
    {
        if (root == null)
        {
            return;
        }

        ClearVisuals();
        ResolveTargets();
        LoadEntries();
        ClampSelection();
        RefreshCurrentSnapshot();
    }

    private void RefreshCurrentSnapshot()
    {
        RefreshPageSlots();
        RefreshPreview();
        RefreshPageControls();
        RefreshSummaryCounters();
    }

    public void Release()
    {
        ClearVisuals();
        root = null;
        previewImage = null;
        titleText = null;
        descriptionText = null;
        timeText = null;
        sceneText = null;
        unlockText = null;
        pageNumberText = null;
        previousPageButton = null;
        nextPageButton = null;
        deleteSelectedButton = null;
        deleteConfirmRoot = null;
        deleteConfirmCanvasGroup = null;
        sortFilterDropdowns.Clear();
        tmpSortFilterDropdowns.Clear();
        slotRects.Clear();
        slotImages.Clear();
        sourceEntries.Clear();
        entries.Clear();
        currentPageIndex = 0;
        selectedEntryIndex = -1;
    }

    private void ResolveTargets()
    {
        if (root == null)
        {
            return;
        }

        Transform previewTarget = FindPreviewTarget(root);
        previewImage = EnsureTextureOverlay(previewTarget as RectTransform);

        slotRects.Clear();
        slotImages.Clear();
        CollectSlotTargets(root, previewTarget, slotRects);
        SortSlotTargets(slotRects);
        TrimSlotTargets(slotRects);
        NormalizeSlotGridLayout(slotRects);

        for (int i = 0; i < slotRects.Count; i++)
        {
            RawImage slotImage = EnsureTextureOverlay(slotRects[i]);
            slotImages.Add(slotImage);
            BindSlotClick(slotRects[i], i);
        }

        titleText = FindTextByName(root, "PhotoName") ?? FindTextByName(root, "Name");
        descriptionText = FindTextByName(root, "Introduction") ?? FindTextByContent(root, "New Text");
        NormalizePreviewTextLayout(previewTarget as RectTransform);
        timeText = FindTextContaining(root, "拍摄时间");
        sceneText = FindTextContaining(root, "拍摄地点");
        unlockText = FindTextContaining(root, "解锁条件");
        pageNumberText = FindTextByName(root, "PageNumber");
        EnsureFilterDropdowns();
        EnsurePagingButtons();
        EnsureDeleteSelectedButton(previewTarget as RectTransform);
        EnsureDeleteConfirmDialog();
    }

    private void LoadEntries()
    {
        sourceEntries.Clear();
        entries.Clear();
        IReadOnlyList<PhotoAlbumEntry> loadedEntries = entryLoader();
        if (loadedEntries != null)
        {
            for (int i = 0; i < loadedEntries.Count; i++)
            {
                if (loadedEntries[i] != null)
                {
                    sourceEntries.Add(loadedEntries[i]);
                }
            }
        }

        RefreshFilterDropdownOptions();
        ApplyFiltersAndSort();
    }

    private void ApplyFiltersAndSort()
    {
        entries.Clear();
        for (int i = 0; i < sourceEntries.Count; i++)
        {
            PhotoAlbumEntry entry = sourceEntries[i];
            if (entry != null)
            {
                entries.Add(entry);
            }
        }

        entries.Sort(CompareEntriesForCurrentSort);
    }

    private int CompareEntriesForCurrentSort(PhotoAlbumEntry left, PhotoAlbumEntry right)
    {
        if (selectedSortMode == PhotoAlbumSortMode.Stage)
        {
            int stageCompare = CompareEntryStage(left, right);
            if (stageCompare != 0)
            {
                return stageCompare;
            }
        }

        int timeCompare = CompareEntrySavedTime(left, right);
        if (timeCompare != 0)
        {
            return -timeCompare;
        }

        string leftFile = left != null ? left.fileName : string.Empty;
        string rightFile = right != null ? right.fileName : string.Empty;
        return string.Compare(leftFile, rightFile, StringComparison.Ordinal);
    }

    private void ClampSelection()
    {
        if (entries.Count == 0)
        {
            selectedEntryIndex = -1;
            currentPageIndex = 0;
            return;
        }

        if (selectedEntryIndex < 0 || selectedEntryIndex >= entries.Count)
        {
            selectedEntryIndex = 0;
        }

        int pageCount = GetPageCount();
        currentPageIndex = Mathf.Clamp(currentPageIndex, 0, Mathf.Max(0, pageCount - 1));
        int firstEntryOnPage = currentPageIndex * EntriesPerPage;
        int lastEntryOnPage = Mathf.Min(entries.Count - 1, firstEntryOnPage + EntriesPerPage - 1);
        if (selectedEntryIndex < firstEntryOnPage || selectedEntryIndex > lastEntryOnPage)
        {
            selectedEntryIndex = firstEntryOnPage;
        }
    }

    private void RefreshPageSlots()
    {
        ClearVisuals();
        int pageStartIndex = currentPageIndex * EntriesPerPage;
        for (int i = 0; i < slotImages.Count; i++)
        {
            int entryIndex = pageStartIndex + i;
            bool hasEntry = entryIndex >= 0 && entryIndex < entries.Count && entries[entryIndex] != null;
            RawImage slotImage = slotImages[i];
            if (slotImage != null)
            {
                Texture2D texture = hasEntry ? LoadEntryTexture(entries[entryIndex]) : null;
                slotImage.texture = texture;
                slotImage.color = texture != null ? Color.white : Color.clear;
            }

            Button slotButton = slotRects[i] != null ? slotRects[i].GetComponent<Button>() : null;
            if (slotButton != null)
            {
                slotButton.interactable = hasEntry;
            }
        }
    }

    private void RefreshPreview()
    {
        if (entries.Count == 0 ||
            selectedEntryIndex < 0 ||
            selectedEntryIndex >= entries.Count ||
            entries[selectedEntryIndex] == null)
        {
            SetTexture(previewImage, null);
            SetText(titleText, "暂无留念");
            SetText(descriptionText, "进入战斗场景或基地拍照并确认保存后，照片会展示在这里。");
            SetText(timeText, "拍摄时间 : --");
            SetText(sceneText, "拍摄地点 : --");
            SetText(unlockText, "解锁条件 : 保存一张留念照片");
            return;
        }

        PhotoAlbumEntry entry = entries[selectedEntryIndex];
        Texture2D previewTexture = LoadEntryTexture(entry);
        SetTexture(previewImage, previewTexture);

        string stageName = ResolveStageName(entry);
        SetText(titleText, $"第 {selectedEntryIndex + 1} 张 · {stageName}");
        SetText(descriptionText, "本地留念已保存，可在左页选择缩略图切换预览。");
        SetText(timeText, $"拍摄时间 : {FormatSavedTime(entry.savedAtUtc)}");
        SetText(sceneText, $"拍摄地点 : {ResolveSceneLabel(entry, stageName)}");
        SetText(unlockText, $"解锁条件 : {stageName}");
    }

    private void RefreshPageControls()
    {
        int pageCount = GetPageCount();
        SetText(pageNumberText, entries.Count == 0 ? "0/0" : $"{currentPageIndex + 1}/{pageCount}");

        if (previousPageButton != null)
        {
            previousPageButton.interactable = pageCount > 0 && currentPageIndex > 0;
        }

        if (nextPageButton != null)
        {
            nextPageButton.interactable = pageCount > 0 && currentPageIndex < pageCount - 1;
        }

        if (deleteSelectedButton != null)
        {
            deleteSelectedButton.interactable =
                selectedEntryIndex >= 0 &&
                selectedEntryIndex < entries.Count &&
                entries[selectedEntryIndex] != null;
        }

        if (selectedEntryIndex < 0 || selectedEntryIndex >= entries.Count || entries[selectedEntryIndex] == null)
        {
            SetDeleteConfirmVisible(false);
        }
    }

    private void RefreshSummaryCounters()
    {
        TMP_Text[] texts = root != null ? root.GetComponentsInChildren<TMP_Text>(true) : Array.Empty<TMP_Text>();
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null ||
                text == titleText ||
                text == descriptionText ||
                text == timeText ||
                text == sceneText ||
                text == unlockText ||
                text == pageNumberText)
            {
                continue;
            }

            string value = text.text ?? string.Empty;
            if (value.Contains("风景"))
            {
                int activeEntryCount = GetActiveEntryCount();
                text.text = $"风景   {activeEntryCount}/{activeEntryCount}";
            }
            else if (value.Contains("建筑"))
            {
                text.text = "建筑   0/0";
            }
            else if (IsAlbumProgressValue(value))
            {
                text.text = BuildAlbumProgressValue();
            }
        }
    }

    private string BuildAlbumProgressValue()
    {
        int activeEntryCount = GetActiveEntryCount();
        return activeEntryCount == 0
            ? "00/000（000%）"
            : $"{activeEntryCount:00}/{activeEntryCount:000}（100%）";
    }

    private static bool IsAlbumProgressValue(string value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains("/") &&
               value.Contains("%");
    }

    private void SelectEntry(int slotIndex)
    {
        int entryIndex = currentPageIndex * EntriesPerPage + slotIndex;
        if (entryIndex < 0 || entryIndex >= entries.Count || entries[entryIndex] == null)
        {
            return;
        }

        selectedEntryIndex = entryIndex;
        SetDeleteConfirmVisible(false);
        RefreshCurrentSnapshot();
    }

    private void SelectPreviousPage()
    {
        if (currentPageIndex <= 0)
        {
            return;
        }

        currentPageIndex--;
        selectedEntryIndex = FindFirstEntryIndexOnPage(currentPageIndex);
        SetDeleteConfirmVisible(false);
        RefreshCurrentSnapshot();
    }

    private void SelectNextPage()
    {
        if (currentPageIndex >= GetPageCount() - 1)
        {
            return;
        }

        currentPageIndex++;
        selectedEntryIndex = FindFirstEntryIndexOnPage(currentPageIndex);
        SetDeleteConfirmVisible(false);
        RefreshCurrentSnapshot();
    }

    private void ShowDeleteConfirm()
    {
        if (selectedEntryIndex < 0 || selectedEntryIndex >= entries.Count || entries[selectedEntryIndex] == null)
        {
            return;
        }

        EnsureDeleteConfirmDialog();
        SetDeleteConfirmVisible(true);
    }

    private void ConfirmDeleteSelectedEntry()
    {
        SetDeleteConfirmVisible(false);
        DeleteSelectedEntry();
    }

    private void DeleteSelectedEntry()
    {
        if (selectedEntryIndex < 0 || selectedEntryIndex >= entries.Count)
        {
            return;
        }

        int deletedIndex = selectedEntryIndex;
        PhotoAlbumEntry deletedEntry = entries[selectedEntryIndex];
        if (deletedEntry == null || !entryDeleter(deletedEntry))
        {
            return;
        }

        RemoveEntry(sourceEntries, deletedEntry);
        entries[deletedIndex] = null;
        if (GetActiveEntryCount() == 0)
        {
            entries.Clear();
            selectedEntryIndex = -1;
            currentPageIndex = 0;
        }
        else
        {
            selectedEntryIndex = FindNearestEntryIndexOnPage(currentPageIndex, deletedIndex);
        }

        RefreshCurrentSnapshot();
    }

    private static void RemoveEntry(List<PhotoAlbumEntry> targetEntries, PhotoAlbumEntry entry)
    {
        if (targetEntries == null || entry == null)
        {
            return;
        }

        for (int i = targetEntries.Count - 1; i >= 0; i--)
        {
            if (IsSameEntry(targetEntries[i], entry))
            {
                targetEntries.RemoveAt(i);
            }
        }
    }

    private Texture2D LoadEntryTexture(PhotoAlbumEntry entry)
    {
        Texture2D texture = textureLoader(entry);
        if (texture != null)
        {
            loadedTextures.Add(texture);
        }

        return texture;
    }

    private int GetPageCount()
    {
        return entries.Count == 0
            ? 0
            : Mathf.CeilToInt(entries.Count / (float)EntriesPerPage);
    }

    private int GetActiveEntryCount()
    {
        int count = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private int FindFirstEntryIndexOnPage(int pageIndex)
    {
        int pageStart = Mathf.Max(0, pageIndex) * EntriesPerPage;
        int pageEnd = Mathf.Min(entries.Count, pageStart + EntriesPerPage);
        for (int i = pageStart; i < pageEnd; i++)
        {
            if (entries[i] != null)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindNearestEntryIndexOnPage(int pageIndex, int preferredIndex)
    {
        int pageStart = Mathf.Max(0, pageIndex) * EntriesPerPage;
        int pageEnd = Mathf.Min(entries.Count, pageStart + EntriesPerPage);
        int clampedPreferredIndex = Mathf.Clamp(preferredIndex, pageStart, Mathf.Max(pageStart, pageEnd - 1));
        for (int i = clampedPreferredIndex; i < pageEnd; i++)
        {
            if (entries[i] != null)
            {
                return i;
            }
        }

        for (int i = clampedPreferredIndex - 1; i >= pageStart; i--)
        {
            if (entries[i] != null)
            {
                return i;
            }
        }

        return -1;
    }

    private void ClearVisuals()
    {
        SetTexture(previewImage, null);
        for (int i = 0; i < slotImages.Count; i++)
        {
            SetTexture(slotImages[i], null);
        }

        ReleaseLoadedTextures();
    }

    private void ReleaseLoadedTextures()
    {
        if (destroyLoadedTextures)
        {
            for (int i = 0; i < loadedTextures.Count; i++)
            {
                if (loadedTextures[i] != null)
                {
                    UnityEngine.Object.Destroy(loadedTextures[i]);
                }
            }
        }

        loadedTextures.Clear();
    }

    private void BindSlotClick(RectTransform slotRect, int slotIndex)
    {
        if (slotRect == null)
        {
            return;
        }

        Button button = slotRect.GetComponent<Button>();
        if (button == null)
        {
            button = slotRect.gameObject.AddComponent<Button>();
        }

        Image targetImage = slotRect.GetComponent<Image>();
        if (targetImage != null)
        {
            button.targetGraphic = targetImage;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectEntry(slotIndex));
    }

    private void EnsurePagingButtons()
    {
        if (pageNumberText == null)
        {
            return;
        }

        RectTransform pageNumberRect = pageNumberText.rectTransform;
        Transform parent = pageNumberRect.parent;
        previousPageButton = ResolvePagingButton(parent, RuntimePreviousButtonName, pageNumberRect, false, new Vector2(-76f, 0f));
        nextPageButton = ResolvePagingButton(parent, RuntimeNextButtonName, pageNumberRect, true, new Vector2(76f, 0f));

        previousPageButton.onClick.RemoveAllListeners();
        previousPageButton.onClick.AddListener(SelectPreviousPage);
        nextPageButton.onClick.RemoveAllListeners();
        nextPageButton.onClick.AddListener(SelectNextPage);
    }

    private void EnsureDeleteSelectedButton(RectTransform previewTarget)
    {
        if (root == null)
        {
            return;
        }

        Button sceneDeleteButton = FindSceneAuthoredDeleteButton(root);
        if (sceneDeleteButton != null)
        {
            deleteSelectedButton = sceneDeleteButton;
            ApplyShareButtonInteractionStyle(deleteSelectedButton, root);
            deleteSelectedButton.onClick.RemoveAllListeners();
            deleteSelectedButton.onClick.AddListener(ShowDeleteConfirm);
            return;
        }

        RectTransform shareButtonRect = FindTransformByName(root, "ShareButton") as RectTransform;
        RectTransform referenceRect = shareButtonRect != null
            ? shareButtonRect
            : previewTarget != null
                ? previewTarget
                : pageNumberText != null
                    ? pageNumberText.rectTransform
                    : root;
        Transform parent = referenceRect.parent != null ? referenceRect.parent : root;
        Vector2 referenceSize = GetRectSize(referenceRect);
        Vector2 deleteButtonSize = shareButtonRect != null
            ? referenceSize
            : new Vector2(148f, 44f);
        Vector2 position = shareButtonRect != null
            ? referenceRect.anchoredPosition + new Vector2(-deleteButtonSize.x - DeleteButtonShareGap, 0f)
            : referenceRect.anchoredPosition + new Vector2(
                Mathf.Min(110f, referenceSize.x * 0.34f),
                -referenceSize.y * 0.64f);
        string deleteLabel = shareButtonRect != null ? "删" : "删除选中";

        deleteSelectedButton = EnsureTextButton(
            parent,
            RuntimeDeleteButtonName,
            deleteLabel,
            referenceRect,
            position,
            deleteButtonSize,
            new Color(0.55f, 0.18f, 0.14f, 0.94f));
        TextMeshProUGUI buttonText = deleteSelectedButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (buttonText != null && titleText != null && titleText.font != null)
        {
            buttonText.font = titleText.font;
        }

        deleteSelectedButton.onClick.RemoveAllListeners();
        deleteSelectedButton.onClick.AddListener(ShowDeleteConfirm);
    }

    private void EnsureDeleteConfirmDialog()
    {
        if (root == null)
        {
            return;
        }

        Transform existingRoot = FindTransformByName(root, RuntimeDeleteConfirmRootName);
        GameObject confirmObject = existingRoot != null
            ? existingRoot.gameObject
            : new GameObject(RuntimeDeleteConfirmRootName, typeof(RectTransform), typeof(CanvasGroup));
        deleteConfirmRoot = confirmObject.GetComponent<RectTransform>();
        deleteConfirmRoot.SetParent(root, false);
        deleteConfirmRoot.anchorMin = Vector2.zero;
        deleteConfirmRoot.anchorMax = Vector2.one;
        deleteConfirmRoot.offsetMin = Vector2.zero;
        deleteConfirmRoot.offsetMax = Vector2.zero;
        deleteConfirmRoot.localScale = Vector3.one;
        deleteConfirmRoot.SetAsLastSibling();

        deleteConfirmCanvasGroup = GetOrAddComponent<CanvasGroup>(confirmObject);

        Image overlay = EnsureConfirmImage(deleteConfirmRoot, "Backdrop", DeleteConfirmOverlayColor);
        StretchRect(overlay.rectTransform);

        Image panelImage = EnsureConfirmImage(deleteConfirmRoot, "Panel", DeleteConfirmPanelColor);
        RuntimeUiSpriteFactory.ApplyRoundedSprite(panelImage, DeleteConfirmPanelColor, 16, 14, 1.1f);
        RectTransform panelRect = panelImage.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(430f, 220f);
        panelRect.localScale = Vector3.one;

        TextMeshProUGUI title = EnsureConfirmText(
            panelRect,
            "Title",
            "确认删除这张留念？",
            30f,
            DeleteConfirmTitleColor,
            TextAlignmentOptions.Center,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(28f, -74f),
            new Vector2(-28f, -24f));
        ApplyExistingFont(title);

        TextMeshProUGUI body = EnsureConfirmText(
            panelRect,
            "Body",
            "删除后会从本地相册移除，无法恢复。",
            20f,
            DeleteConfirmBodyColor,
            TextAlignmentOptions.Center,
            new Vector2(0f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(30f, -22f),
            new Vector2(-30f, 38f));
        body.enableWordWrapping = true;
        ApplyExistingFont(body);

        Button cancelButton = EnsureConfirmButton(
            panelRect,
            RuntimeCancelDeleteButtonName,
            "取消",
            DeleteConfirmCancelColor,
            Color.white,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(-88f, 34f),
            new Vector2(128f, 48f));
        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(() => SetDeleteConfirmVisible(false));

        Button confirmButton = EnsureConfirmButton(
            panelRect,
            RuntimeConfirmDeleteButtonName,
            "确认删除",
            DeleteConfirmDangerColor,
            Color.white,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(88f, 34f),
            new Vector2(128f, 48f));
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(ConfirmDeleteSelectedEntry);

        SetDeleteConfirmVisible(false);
    }

    private void NormalizePreviewTextLayout(RectTransform previewTarget)
    {
        if (previewTarget == null || previewTarget.parent == null)
        {
            return;
        }

        RectTransform previewGroup = previewTarget.parent as RectTransform;
        if (previewGroup == null)
        {
            return;
        }

        NormalizePreviewTitleText(titleText, previewTarget, previewGroup);
        NormalizePreviewDescriptionText(descriptionText, previewTarget, previewGroup);
    }

    private static void NormalizePreviewTitleText(TMP_Text text, RectTransform previewTarget, RectTransform previewGroup)
    {
        if (!IsPreviewDetailText(text, previewGroup))
        {
            return;
        }

        RectTransform rect = text.rectTransform;
        Vector2 previewSize = GetRectSize(previewTarget);
        float width = Mathf.Max(GetRectSize(rect).x, previewSize.x * 1.55f, 180f);
        float previewBottom = previewTarget.anchoredPosition.y - previewSize.y * previewTarget.pivot.y;

        rect.anchorMin = previewTarget.anchorMin;
        rect.anchorMax = previewTarget.anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(previewTarget.anchoredPosition.x, previewBottom - PreviewTitleBottomPadding);
        rect.sizeDelta = new Vector2(width, 18f);
        rect.localScale = Vector3.one;

        text.enableAutoSizing = true;
        text.fontSizeMin = PreviewTitleMinFontSize;
        text.fontSizeMax = PreviewTitleFontSize;
        text.fontSize = PreviewTitleFontSize;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
    }

    private static void NormalizePreviewDescriptionText(TMP_Text text, RectTransform previewTarget, RectTransform previewGroup)
    {
        if (!IsPreviewDetailText(text, previewGroup))
        {
            return;
        }

        RectTransform rect = text.rectTransform;
        Vector2 previewSize = GetRectSize(previewTarget);
        float width = Mathf.Max(GetRectSize(rect).x, previewSize.x * 1.65f, 190f);
        float previewBottom = previewTarget.anchoredPosition.y - previewSize.y * previewTarget.pivot.y;

        rect.anchorMin = previewTarget.anchorMin;
        rect.anchorMax = previewTarget.anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(previewTarget.anchoredPosition.x, previewBottom - PreviewDescriptionBottomPadding);
        rect.sizeDelta = new Vector2(width, 32f);
        rect.localScale = Vector3.one;

        text.enableAutoSizing = true;
        text.fontSizeMin = PreviewDescriptionMinFontSize;
        text.fontSizeMax = PreviewDescriptionFontSize;
        text.fontSize = PreviewDescriptionFontSize;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
    }

    private static bool IsPreviewDetailText(TMP_Text text, RectTransform previewGroup)
    {
        return text != null &&
               text.rectTransform != null &&
               previewGroup != null &&
               text.rectTransform.parent == previewGroup;
    }

    private void SetDeleteConfirmVisible(bool visible)
    {
        if (deleteConfirmRoot == null || deleteConfirmCanvasGroup == null)
        {
            return;
        }

        deleteConfirmRoot.gameObject.SetActive(visible);
        deleteConfirmCanvasGroup.alpha = visible ? 1f : 0f;
        deleteConfirmCanvasGroup.interactable = visible;
        deleteConfirmCanvasGroup.blocksRaycasts = visible;
    }

    private static Button FindSceneAuthoredDeleteButton(Transform root)
    {
        Transform deleteTransform = FindTransformByName(root, SceneDeleteButtonName);
        return deleteTransform != null
            ? deleteTransform.GetComponent<Button>()
            : null;
    }

    private static void ApplyShareButtonInteractionStyle(Button targetButton, Transform root)
    {
        if (targetButton == null || root == null)
        {
            return;
        }

        Transform shareTransform = FindTransformByName(root, "ShareButton");
        Button shareButton = shareTransform != null ? shareTransform.GetComponent<Button>() : null;
        if (shareButton == null || shareButton == targetButton)
        {
            return;
        }

        targetButton.transition = shareButton.transition;
        targetButton.colors = shareButton.colors;
        targetButton.spriteState = shareButton.spriteState;
        targetButton.animationTriggers = shareButton.animationTriggers;
    }

    private void EnsureFilterDropdowns()
    {
        sortFilterDropdowns.Clear();
        tmpSortFilterDropdowns.Clear();
        CollectSceneAuthoredFilterDropdowns(root, sortFilterDropdowns, tmpSortFilterDropdowns);
        if (sortFilterDropdowns.Count == 0 && tmpSortFilterDropdowns.Count == 0)
        {
            return;
        }

        List<RectTransform> filterRects = new List<RectTransform>(sortFilterDropdowns.Count + tmpSortFilterDropdowns.Count);
        for (int i = 0; i < sortFilterDropdowns.Count; i++)
        {
            Dropdown dropdown = sortFilterDropdowns[i];
            RectTransform rect = dropdown != null ? dropdown.transform as RectTransform : null;
            if (rect != null)
            {
                filterRects.Add(rect);
            }
        }

        for (int i = 0; i < tmpSortFilterDropdowns.Count; i++)
        {
            TMP_Dropdown dropdown = tmpSortFilterDropdowns[i];
            RectTransform rect = dropdown != null ? dropdown.transform as RectTransform : null;
            if (rect != null)
            {
                filterRects.Add(rect);
            }
        }

        NormalizeFilterDropdownLayout(filterRects);

        for (int i = 0; i < filterRects.Count; i++)
        {
            Transform parent = filterRects[i] != null && filterRects[i].parent != null ? filterRects[i].parent : root;
            RemoveRuntimeFilterDropdown(parent, RuntimeStageDropdownName);
            RemoveRuntimeFilterDropdown(parent, RuntimeSortDropdownName);
        }

        for (int i = 0; i < sortFilterDropdowns.Count; i++)
        {
            ConfigureDropdownRect(sortFilterDropdowns[i]);
        }

        for (int i = 0; i < tmpSortFilterDropdowns.Count; i++)
        {
            ConfigureTmpDropdownRect(tmpSortFilterDropdowns[i]);
        }

        BindSortFilterDropdownEvent();
        RefreshFilterDropdownOptions();
    }

    private void RefreshFilterDropdownOptions()
    {
        suppressSortDropdownEvents = true;
        try
        {
            for (int i = 0; i < sortFilterDropdowns.Count; i++)
            {
                SetDropdownOptions(
                    sortFilterDropdowns[i],
                    new[] { "按时间排序", "按关卡排序" },
                    Mathf.Clamp((int)selectedSortMode, 0, 1));
            }

            for (int i = 0; i < tmpSortFilterDropdowns.Count; i++)
            {
                SetDropdownOptions(
                    tmpSortFilterDropdowns[i],
                    new[] { "按时间排序", "按关卡排序" },
                    Mathf.Clamp((int)selectedSortMode, 0, 1));
            }
        }
        finally
        {
            suppressSortDropdownEvents = false;
        }
    }

    private void BindSortFilterDropdownEvent()
    {
        for (int i = 0; i < sortFilterDropdowns.Count; i++)
        {
            Dropdown dropdown = sortFilterDropdowns[i];
            if (dropdown == null)
            {
                continue;
            }

            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(HandleSortChanged);
        }

        for (int i = 0; i < tmpSortFilterDropdowns.Count; i++)
        {
            TMP_Dropdown dropdown = tmpSortFilterDropdowns[i];
            if (dropdown == null)
            {
                continue;
            }

            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(HandleSortChanged);
        }
    }

    private void HandleSortChanged(int value)
    {
        if (suppressSortDropdownEvents)
        {
            return;
        }

        selectedSortMode = value == 1 ? PhotoAlbumSortMode.Stage : PhotoAlbumSortMode.Time;
        ApplyFilterChange();
    }

    private void ApplyFilterChange()
    {
        currentPageIndex = 0;
        selectedEntryIndex = -1;
        ApplyFiltersAndSort();
        ClampSelection();
        RefreshFilterDropdownOptions();
        SetDeleteConfirmVisible(false);
        RefreshCurrentSnapshot();
    }

    private static void CollectSceneAuthoredFilterDropdowns(
        Transform root,
        List<Dropdown> legacyResults,
        List<TMP_Dropdown> tmpResults)
    {
        if (root == null)
        {
            return;
        }

        if (legacyResults != null)
        {
            Dropdown[] dropdowns = root.GetComponentsInChildren<Dropdown>(true);
            for (int i = 0; i < dropdowns.Length; i++)
            {
                Dropdown dropdown = dropdowns[i];
                if (dropdown != null && IsSceneAuthoredFilterDropdownName(dropdown.name))
                {
                    legacyResults.Add(dropdown);
                }
            }
        }

        if (tmpResults != null)
        {
            TMP_Dropdown[] dropdowns = root.GetComponentsInChildren<TMP_Dropdown>(true);
            for (int i = 0; i < dropdowns.Length; i++)
            {
                TMP_Dropdown dropdown = dropdowns[i];
                if (dropdown != null && IsSceneAuthoredFilterDropdownName(dropdown.name))
                {
                    tmpResults.Add(dropdown);
                }
            }
        }
    }

    private static bool IsSceneAuthoredFilterDropdownName(string name)
    {
        return string.Equals(name, "Dropdown", StringComparison.Ordinal) &&
               !name.StartsWith("RuntimePhotoAlbum", StringComparison.Ordinal);
    }

    private static void RemoveRuntimeFilterDropdown(Transform parent, string name)
    {
        if (parent == null || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        Transform existing = parent.Find(name);
        if (existing == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(existing.gameObject);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }
    }

    private static void ConfigureDropdownRect(Dropdown dropdown)
    {
        RectTransform rect = dropdown != null ? dropdown.transform as RectTransform : null;
        if (rect == null)
        {
            return;
        }

        rect.localScale = Vector3.one;

        Image image = dropdown.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
        }

        ConfigureLegacyDropdownText(dropdown.captionText, true);
        ConfigureLegacyDropdownText(dropdown.itemText, false);
    }

    private static void ConfigureTmpDropdownRect(TMP_Dropdown dropdown)
    {
        RectTransform rect = dropdown != null ? dropdown.transform as RectTransform : null;
        if (rect == null)
        {
            return;
        }

        rect.localScale = Vector3.one;

        Image image = dropdown.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
        }

        ConfigureTmpDropdownText(dropdown.captionText, true);
        ConfigureTmpDropdownText(dropdown.itemText, false);
    }

    private static void NormalizeFilterDropdownLayout(List<RectTransform> dropdownRects)
    {
        if (dropdownRects == null || dropdownRects.Count == 0)
        {
            return;
        }

        dropdownRects.RemoveAll(rect => rect == null);
        if (dropdownRects.Count == 0)
        {
            return;
        }

        dropdownRects.Sort(CompareFilterDropdownRects);
        Vector2 targetSize = ResolveFilterDropdownSize(dropdownRects);
        bool hasSharedParent = FilterDropdownsShareParent(dropdownRects);
        Vector2 startPosition = dropdownRects[0].anchoredPosition;

        for (int i = 0; i < dropdownRects.Count; i++)
        {
            RectTransform rect = dropdownRects[i];
            rect.sizeDelta = targetSize;
            if (hasSharedParent)
            {
                rect.anchoredPosition = new Vector2(
                    startPosition.x + i * (targetSize.x + FilterDropdownHorizontalGap),
                    startPosition.y);
            }

            rect.localScale = Vector3.one;
        }
    }

    private static int CompareFilterDropdownRects(RectTransform left, RectTransform right)
    {
        if (left == right)
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        int yCompare = -left.anchoredPosition.y.CompareTo(right.anchoredPosition.y);
        return yCompare != 0 ? yCompare : left.anchoredPosition.x.CompareTo(right.anchoredPosition.x);
    }

    private static Vector2 ResolveFilterDropdownSize(List<RectTransform> dropdownRects)
    {
        Vector2 size = new Vector2(FilterDropdownMinWidth, FilterDropdownMinHeight);
        for (int i = 0; i < dropdownRects.Count; i++)
        {
            Vector2 candidate = GetRectSize(dropdownRects[i]);
            size.x = Mathf.Max(size.x, candidate.x);
            size.y = Mathf.Max(size.y, candidate.y);
        }

        return size;
    }

    private static bool FilterDropdownsShareParent(List<RectTransform> dropdownRects)
    {
        Transform parent = dropdownRects != null && dropdownRects.Count > 0 && dropdownRects[0] != null
            ? dropdownRects[0].parent
            : null;
        if (parent == null)
        {
            return false;
        }

        for (int i = 1; i < dropdownRects.Count; i++)
        {
            if (dropdownRects[i] == null || dropdownRects[i].parent != parent)
            {
                return false;
            }
        }

        return true;
    }

    private static void ConfigureLegacyDropdownText(Text text, bool reserveArrowSpace)
    {
        if (text == null)
        {
            return;
        }

        ConfigureDropdownTextRect(text.rectTransform, reserveArrowSpace);
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = false;
        text.fontSize = Mathf.RoundToInt(reserveArrowSpace ? FilterDropdownCaptionFontSize : FilterDropdownItemFontSize);
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        RuntimeTextFontRepair.RepairLegacyText(text);
    }

    private static void ConfigureTmpDropdownText(TMP_Text text, bool reserveArrowSpace)
    {
        if (text == null)
        {
            return;
        }

        ConfigureDropdownTextRect(text.rectTransform, reserveArrowSpace);
        text.enableAutoSizing = false;
        text.fontSize = reserveArrowSpace ? FilterDropdownCaptionFontSize : FilterDropdownItemFontSize;
        text.fontSizeMin = FilterDropdownItemFontSize;
        text.fontSizeMax = FilterDropdownCaptionFontSize;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        RuntimeTextFontRepair.RepairTmpText(text);
    }

    private static void ConfigureDropdownTextRect(RectTransform textRect, bool reserveArrowSpace)
    {
        if (textRect == null)
        {
            return;
        }

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(FilterDropdownTextHorizontalInset, FilterDropdownTextVerticalInset);
        textRect.offsetMax = new Vector2(
            reserveArrowSpace ? -FilterDropdownArrowInset : -FilterDropdownTextHorizontalInset,
            -FilterDropdownTextVerticalInset);
        textRect.localScale = Vector3.one;
    }

    private static void SetDropdownOptions(Dropdown dropdown, string[] labels, int selectedIndex)
    {
        if (dropdown == null || labels == null || labels.Length == 0)
        {
            return;
        }

        dropdown.options.Clear();
        for (int i = 0; i < labels.Length; i++)
        {
            dropdown.options.Add(new Dropdown.OptionData(labels[i]));
        }

        dropdown.value = Mathf.Clamp(selectedIndex, 0, labels.Length - 1);
        dropdown.RefreshShownValue();
    }

    private static void SetDropdownOptions(TMP_Dropdown dropdown, string[] labels, int selectedIndex)
    {
        if (dropdown == null || labels == null || labels.Length == 0)
        {
            return;
        }

        dropdown.options.Clear();
        for (int i = 0; i < labels.Length; i++)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData(labels[i]));
        }

        dropdown.value = Mathf.Clamp(selectedIndex, 0, labels.Length - 1);
        dropdown.RefreshShownValue();
    }

    private static Image EnsureConfirmImage(RectTransform parent, string name, Color color)
    {
        Transform existing = parent != null ? parent.Find(name) : null;
        GameObject imageObject = existing != null
            ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        rect.SetAsLastSibling();

        Image image = GetOrAddComponent<Image>(imageObject);
        image.color = color;
        image.raycastTarget = true;
        return image;
    }

    private TextMeshProUGUI EnsureConfirmText(
        RectTransform parent,
        string name,
        string content,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        Transform existing = parent != null ? parent.Find(name) : null;
        GameObject textObject = existing != null
            ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
        rect.SetAsLastSibling();

        TextMeshProUGUI text = GetOrAddComponent<TextMeshProUGUI>(textObject);
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private Button EnsureConfirmButton(
        RectTransform parent,
        string name,
        string label,
        Color backgroundColor,
        Color textColor,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        Transform existing = parent != null ? parent.Find(name) : null;
        GameObject buttonObject = existing != null
            ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.SetAsLastSibling();

        Image image = GetOrAddComponent<Image>(buttonObject);
        RuntimeUiSpriteFactory.ApplyRoundedSprite(image, backgroundColor, 12, 10, 1.1f);
        image.raycastTarget = true;

        Button button = GetOrAddComponent<Button>(buttonObject);
        button.targetGraphic = image;

        Transform existingText = buttonObject.transform.Find("Text");
        GameObject textObject = existingText != null
            ? existingText.gameObject
            : new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(buttonObject.transform, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        textRect.localScale = Vector3.one;

        TextMeshProUGUI text = GetOrAddComponent<TextMeshProUGUI>(textObject);
        text.text = label;
        text.fontSize = 20f;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        ApplyExistingFont(text);
        return button;
    }

    private void ApplyExistingFont(TMP_Text targetText)
    {
        if (targetText != null && titleText != null && titleText.font != null)
        {
            targetText.font = titleText.font;
        }
    }

    private static void StretchRect(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static T GetOrAddComponent<T>(GameObject gameObject)
        where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static Button ResolvePagingButton(
        Transform parent,
        string runtimeName,
        RectTransform pageNumberRect,
        bool next,
        Vector2 fallbackOffset)
    {
        Button sceneButton = FindSceneAuthoredPagingButton(parent, pageNumberRect, next);
        if (sceneButton != null)
        {
            return sceneButton;
        }

        Button runtimeButton = FindDirectButton(parent, runtimeName);
        return runtimeButton != null
            ? runtimeButton
            : EnsurePagingButton(parent, runtimeName, pageNumberRect, fallbackOffset);
    }

    private static Button FindSceneAuthoredPagingButton(Transform parent, RectTransform pageNumberRect, bool next)
    {
        if (parent == null || pageNumberRect == null)
        {
            return null;
        }

        Vector2 pagePosition = pageNumberRect.anchoredPosition;
        Vector2 pageSize = GetRectSize(pageNumberRect);
        float maxVerticalDistance = Mathf.Max(32f, pageSize.y * 1.5f);
        Button bestButton = null;
        float bestHorizontalDistance = float.MaxValue;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            RectTransform rect = child as RectTransform;
            Button button = child != null ? child.GetComponent<Button>() : null;
            if (rect == null ||
                button == null ||
                child == pageNumberRect.transform ||
                IsRuntimePhotoAlbumButtonName(child.name))
            {
                continue;
            }

            float horizontalDistance = rect.anchoredPosition.x - pagePosition.x;
            if ((next && horizontalDistance <= 0f) || (!next && horizontalDistance >= 0f))
            {
                continue;
            }

            if (Mathf.Abs(rect.anchoredPosition.y - pagePosition.y) > maxVerticalDistance ||
                Mathf.Abs(horizontalDistance) > ScenePagingButtonMaxHorizontalDistance)
            {
                continue;
            }

            float absHorizontalDistance = Mathf.Abs(horizontalDistance);
            if (absHorizontalDistance < bestHorizontalDistance)
            {
                bestHorizontalDistance = absHorizontalDistance;
                bestButton = button;
            }
        }

        return bestButton;
    }

    private static Button FindDirectButton(Transform parent, string name)
    {
        if (parent == null || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        Transform child = parent.Find(name);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private static bool IsRuntimePhotoAlbumButtonName(string name)
    {
        return !string.IsNullOrWhiteSpace(name) &&
               name.StartsWith("RuntimePhotoAlbum", StringComparison.Ordinal);
    }

    private static Button EnsurePagingButton(
        Transform parent,
        string name,
        RectTransform pageNumberRect,
        Vector2 offset)
    {
        Transform existing = parent != null ? parent.Find(name) : null;
        GameObject buttonObject = existing != null
            ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.SetParent(parent, false);
        buttonRect.anchorMin = pageNumberRect.anchorMin;
        buttonRect.anchorMax = pageNumberRect.anchorMax;
        buttonRect.pivot = pageNumberRect.pivot;
        buttonRect.anchoredPosition = pageNumberRect.anchoredPosition + offset;
        buttonRect.sizeDelta = new Vector2(58f, 58f);
        buttonRect.localScale = Vector3.one;
        buttonRect.SetAsLastSibling();

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.001f);
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private static Button EnsureTextButton(
        Transform parent,
        string name,
        string label,
        RectTransform referenceRect,
        Vector2 anchoredPosition,
        Vector2 size,
        Color color)
    {
        Transform existing = parent != null ? parent.Find(name) : null;
        GameObject buttonObject = existing != null
            ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.SetParent(parent, false);
        buttonRect.anchorMin = referenceRect.anchorMin;
        buttonRect.anchorMax = referenceRect.anchorMax;
        buttonRect.pivot = referenceRect.pivot;
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = size;
        buttonRect.localScale = Vector3.one;
        buttonRect.SetAsLastSibling();

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        Transform textTransform = buttonObject.transform.Find("Text");
        GameObject textObject = textTransform != null
            ? textTransform.gameObject
            : new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(buttonObject.transform, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        textRect.localScale = Vector3.one;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = textObject.AddComponent<TextMeshProUGUI>();
        }

        text.text = label;
        text.fontSize = Mathf.Min(21f, Mathf.Max(8f, size.y * 0.72f));
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return button;
    }

    private static RawImage EnsureTextureOverlay(RectTransform targetRect)
    {
        if (targetRect == null)
        {
            return null;
        }

        PreparePhotoFrameTarget(targetRect);

        Transform existing = targetRect.Find(RuntimeTextureName);
        GameObject textureObject = existing != null
            ? existing.gameObject
            : new GameObject(RuntimeTextureName, typeof(RectTransform), typeof(RawImage));
        RectTransform textureRect = textureObject.GetComponent<RectTransform>();
        textureRect.SetParent(targetRect, false);
        textureRect.anchorMin = Vector2.zero;
        textureRect.anchorMax = Vector2.one;
        textureRect.offsetMin = Vector2.zero;
        textureRect.offsetMax = Vector2.zero;
        textureRect.pivot = new Vector2(0.5f, 0.5f);
        textureRect.localScale = Vector3.one;
        textureRect.SetAsFirstSibling();

        RawImage rawImage = textureObject.GetComponent<RawImage>();
        rawImage.raycastTarget = false;
        rawImage.color = Color.clear;

        AspectRatioFitter fitter = textureObject.GetComponent<AspectRatioFitter>();
        if (fitter != null)
        {
            fitter.enabled = false;
        }

        return rawImage;
    }

    private static void PreparePhotoFrameTarget(RectTransform targetRect)
    {
        Image frameImage = targetRect != null ? targetRect.GetComponent<Image>() : null;
        if (frameImage == null)
        {
            return;
        }

        frameImage.raycastTarget = true;
        if (frameImage.sprite == null)
        {
            Color color = frameImage.color;
            color.a = 0.001f;
            frameImage.color = color;
        }
    }

    private static void SetTexture(RawImage image, Texture2D texture)
    {
        if (image == null)
        {
            return;
        }

        image.texture = texture;
        image.color = texture != null ? Color.white : Color.clear;
        AspectRatioFitter fitter = image.GetComponent<AspectRatioFitter>();
        if (fitter != null)
        {
            fitter.enabled = false;
        }
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private static Transform FindPreviewTarget(Transform root)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (string.Equals(current.name, "Photo", StringComparison.Ordinal) &&
                current.GetComponent<Image>() != null)
            {
                return current;
            }
        }

        return null;
    }

    private static Transform FindTransformByName(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && string.Equals(transforms[i].name, name, StringComparison.Ordinal))
            {
                return transforms[i];
            }
        }

        return null;
    }

    private static void CollectSlotTargets(Transform root, Transform previewTarget, List<RectTransform> results)
    {
        if (TryCollectNamedPanelSlots(root, "LeftPanel", results) ||
            TryCollectNamedPanelSlots(root, "RightPanel", results))
        {
            return;
        }

        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            RectTransform rect = image != null ? image.transform as RectTransform : null;
            if (IsSlotTarget(image, rect, previewTarget))
            {
                results.Add(rect);
            }
        }
    }

    private static bool TryCollectNamedPanelSlots(Transform root, string panelName, List<RectTransform> results)
    {
        if (root == null || string.IsNullOrWhiteSpace(panelName))
        {
            return false;
        }

        List<RectTransform> bestCandidates = null;
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform panel = transforms[i];
            if (panel == null || !string.Equals(panel.name, panelName, StringComparison.Ordinal))
            {
                continue;
            }

            List<RectTransform> candidates = CollectOrderedPanelSlots(panel);
            if (candidates.Count > (bestCandidates != null ? bestCandidates.Count : 0))
            {
                bestCandidates = candidates;
            }
        }

        if (bestCandidates == null || bestCandidates.Count == 0)
        {
            return false;
        }

        int count = Mathf.Min(EntriesPerPage, bestCandidates.Count);
        for (int i = 0; i < count; i++)
        {
            results.Add(bestCandidates[i]);
        }

        return true;
    }

    private static List<RectTransform> CollectOrderedPanelSlots(Transform panel)
    {
        List<RectTransform> candidates = new List<RectTransform>();
        RectTransform panelRect = panel as RectTransform;
        RectTransform[] descendants = panel.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < descendants.Length; i++)
        {
            RectTransform rect = descendants[i];
            Image image = rect != null ? rect.GetComponent<Image>() : null;
            Vector2 size = rect != null ? GetRectSize(rect) : Vector2.zero;
            if (rect == null ||
                rect == panelRect ||
                image == null ||
                !image.enabled ||
                !TryGetSceneSlotOrder(rect.name, out _) ||
                size.x < 30f ||
                size.y < 20f)
            {
                continue;
            }

            candidates.Add(rect);
        }

        candidates.Sort((left, right) =>
        {
            TryGetSceneSlotOrder(left.name, out int leftOrder);
            TryGetSceneSlotOrder(right.name, out int rightOrder);
            return leftOrder.CompareTo(rightOrder);
        });
        return candidates;
    }

    private static bool TryGetSceneSlotOrder(string name, out int order)
    {
        order = 0;
        const string prefix = "Image_";
        if (string.IsNullOrWhiteSpace(name) ||
            !name.StartsWith(prefix, StringComparison.Ordinal) ||
            !int.TryParse(name.Substring(prefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out order))
        {
            return false;
        }

        return order >= 1 && order <= EntriesPerPage;
    }

    private static bool IsSlotTarget(Image image, RectTransform rect, Transform previewTarget)
    {
        if (image == null || rect == null || !image.enabled)
        {
            return false;
        }

        if (previewTarget != null && (rect == previewTarget || rect.IsChildOf(previewTarget)))
        {
            return false;
        }

        if (rect.GetComponent<Button>() != null)
        {
            return false;
        }

        if (IsExcludedSlotName(rect))
        {
            return false;
        }

        Vector2 size = GetRectSize(rect);
        if (TryGetSceneSlotOrder(rect.name, out _))
        {
            return size.x >= 30f && size.y >= 20f;
        }

        if (image.color.a < 0.2f)
        {
            return false;
        }

        if (size.x < 42f || size.y < 42f)
        {
            return false;
        }

        Color color = image.color;
        return color.r >= 0.88f && color.g >= 0.88f && color.b >= 0.88f;
    }

    private static bool IsExcludedSlotName(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            string name = current.name ?? string.Empty;
            if (name.Contains("BookMark") ||
                name.Contains("HandBook") ||
                name.Contains("PersonalInformation") ||
                name.Contains("Mission") ||
                name.Contains("Setting") ||
                name.Contains("ShareButton") ||
                name.Contains("PageNumber") ||
                name.Contains("RuntimePhotoAlbum") ||
                name.Contains("PhotoPos") ||
                (current == transform && name == "Photo") ||
                (current == transform && (name == "BackGround" || name == "Background")))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static Vector2 GetRectSize(RectTransform rect)
    {
        Vector2 size = rect.rect.size;
        if (size.x <= 0f || size.y <= 0f)
        {
            size = rect.sizeDelta;
        }

        return new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
    }

    private static void SortSlotTargets(List<RectTransform> slots)
    {
        slots.Sort((left, right) =>
        {
            bool leftHasOrder = TryGetSceneSlotOrder(left.name, out int leftOrder);
            bool rightHasOrder = TryGetSceneSlotOrder(right.name, out int rightOrder);
            if (leftHasOrder && rightHasOrder)
            {
                return leftOrder.CompareTo(rightOrder);
            }

            if (leftHasOrder != rightHasOrder)
            {
                return leftHasOrder ? -1 : 1;
            }

            Vector3 leftPosition = left.TransformPoint(left.rect.center);
            Vector3 rightPosition = right.TransformPoint(right.rect.center);
            if (Mathf.Abs(leftPosition.y - rightPosition.y) > 8f)
            {
                return rightPosition.y.CompareTo(leftPosition.y);
            }

            return leftPosition.x.CompareTo(rightPosition.x);
        });
    }

    private static void TrimSlotTargets(List<RectTransform> slots)
    {
        while (slots.Count > EntriesPerPage)
        {
            slots.RemoveAt(slots.Count - 1);
        }
    }

    private static void NormalizeSlotGridLayout(List<RectTransform> slots)
    {
        if (slots == null || slots.Count < 2)
        {
            return;
        }

        Transform parent = slots[0] != null ? slots[0].parent : null;
        if (parent == null)
        {
            return;
        }

        for (int i = 1; i < slots.Count; i++)
        {
            if (slots[i] == null || slots[i].parent != parent)
            {
                return;
            }
        }

        int columnCount = Mathf.Min(3, slots.Count);
        int rowCount = Mathf.CeilToInt(slots.Count / (float)columnCount);
        Vector2 targetSize = ResolveCommonSlotSize(slots);
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = 0; i < slots.Count; i++)
        {
            Vector2 position = slots[i].anchoredPosition;
            minX = Mathf.Min(minX, position.x);
            maxX = Mathf.Max(maxX, position.x);
            minY = Mathf.Min(minY, position.y);
            maxY = Mathf.Max(maxY, position.y);
        }

        float columnStep = columnCount > 1 && maxX > minX
            ? (maxX - minX) / (columnCount - 1)
            : targetSize.x + 24f;
        float rowStep = rowCount > 1 && maxY > minY
            ? (maxY - minY) / (rowCount - 1)
            : targetSize.y + 34f;

        for (int i = 0; i < slots.Count; i++)
        {
            int row = i / columnCount;
            int column = i % columnCount;
            RectTransform slot = slots[i];
            slot.sizeDelta = targetSize;
            slot.anchoredPosition = new Vector2(minX + columnStep * column, maxY - rowStep * row);
        }
    }

    private static Vector2 ResolveCommonSlotSize(List<RectTransform> slots)
    {
        Vector2 size = Vector2.zero;
        for (int i = 0; i < slots.Count; i++)
        {
            Vector2 candidate = GetRectSize(slots[i]);
            if (candidate.x * candidate.y > size.x * size.y)
            {
                size = candidate;
            }
        }

        if (size.x <= 0f || size.y <= 0f)
        {
            return new Vector2(50f, 30f);
        }

        return size;
    }

    private static TMP_Text FindTextByName(Transform root, string name)
    {
        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && string.Equals(texts[i].name, name, StringComparison.Ordinal))
            {
                return texts[i];
            }
        }

        return null;
    }

    private static TMP_Text FindTextByContent(Transform root, string content)
    {
        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && string.Equals(texts[i].text, content, StringComparison.Ordinal))
            {
                return texts[i];
            }
        }

        return null;
    }

    private static TMP_Text FindTextContaining(Transform root, string content)
    {
        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && (texts[i].text ?? string.Empty).Contains(content))
            {
                return texts[i];
            }
        }

        return null;
    }

    private static string ResolveStageFilterValue(PhotoAlbumEntry entry)
    {
        if (entry == null)
        {
            return string.Empty;
        }

        GameplayStageDefinition stage = GameplayStageCatalog.GetStageById(entry.stageId);
        if (stage != null)
        {
            return stage.stageId;
        }

        stage = GameplayStageCatalog.GetStageByScene(entry.sceneName);
        if (stage != null)
        {
            return stage.stageId;
        }

        if (IsBaseScene(entry.sceneName))
        {
            return BaseStageFilterValue;
        }

        return string.IsNullOrWhiteSpace(entry.stageId) ? entry.sceneName : entry.stageId;
    }

    private static int CompareEntryStage(PhotoAlbumEntry left, PhotoAlbumEntry right)
    {
        int leftIndex = ResolveStageSortIndex(left);
        int rightIndex = ResolveStageSortIndex(right);
        if (leftIndex != rightIndex)
        {
            return leftIndex.CompareTo(rightIndex);
        }

        string leftStage = ResolveStageFilterValue(left);
        string rightStage = ResolveStageFilterValue(right);
        return string.Compare(leftStage, rightStage, StringComparison.Ordinal);
    }

    private static int ResolveStageSortIndex(PhotoAlbumEntry entry)
    {
        string stageValue = ResolveStageFilterValue(entry);
        if (string.Equals(stageValue, BaseStageFilterValue, StringComparison.Ordinal))
        {
            return int.MaxValue - 1;
        }

        int stageIndex = GameplayStageCatalog.GetStageIndex(stageValue);
        return stageIndex >= 0 ? stageIndex : int.MaxValue;
    }

    private static string ResolveStageName(PhotoAlbumEntry entry)
    {
        GameplayStageDefinition stage = entry != null ? GameplayStageCatalog.GetStageById(entry.stageId) : null;
        if (stage != null && !string.IsNullOrWhiteSpace(stage.displayName))
        {
            return stage.displayName;
        }

        if (entry != null && IsBaseScene(entry.sceneName))
        {
            return "基地";
        }

        return entry != null && !string.IsNullOrWhiteSpace(entry.sceneName)
            ? entry.sceneName
            : "未记录场景";
    }

    private static bool IsBaseScene(string sceneName)
    {
        return string.Equals(sceneName, "NewBase", StringComparison.Ordinal) ||
               string.Equals(sceneName, "BaseScene", StringComparison.Ordinal);
    }

    private static bool IsSameEntry(PhotoAlbumEntry left, PhotoAlbumEntry right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(left.id) &&
            !string.IsNullOrWhiteSpace(right.id) &&
            string.Equals(left.id, right.id, StringComparison.Ordinal))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(left.fileName) &&
               !string.IsNullOrWhiteSpace(right.fileName) &&
               string.Equals(left.fileName, right.fileName, StringComparison.Ordinal);
    }

    private static int CompareEntrySavedTime(PhotoAlbumEntry left, PhotoAlbumEntry right)
    {
        bool leftParsed = TryParseSavedTime(left, out DateTime leftTime);
        bool rightParsed = TryParseSavedTime(right, out DateTime rightTime);
        if (leftParsed && rightParsed)
        {
            int timeCompare = leftTime.CompareTo(rightTime);
            if (timeCompare != 0)
            {
                return timeCompare;
            }
        }
        else if (leftParsed != rightParsed)
        {
            return leftParsed ? 1 : -1;
        }

        string leftValue = left != null ? left.savedAtUtc : string.Empty;
        string rightValue = right != null ? right.savedAtUtc : string.Empty;
        return string.Compare(leftValue, rightValue, StringComparison.Ordinal);
    }

    private static bool TryParseSavedTime(PhotoAlbumEntry entry, out DateTime savedTime)
    {
        savedTime = default;
        return entry != null &&
               DateTime.TryParse(
                   entry.savedAtUtc,
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.RoundtripKind,
                   out savedTime);
    }

    private static string ResolveSceneLabel(PhotoAlbumEntry entry, string stageName)
    {
        if (entry == null)
        {
            return "--";
        }

        return string.IsNullOrWhiteSpace(entry.sceneName)
            ? stageName
            : entry.sceneName;
    }

    private static string FormatSavedTime(string savedAtUtc)
    {
        if (DateTime.TryParse(
                savedAtUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTime parsed))
        {
            return parsed.ToLocalTime().ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);
        }

        return "时间未记录";
    }
}
