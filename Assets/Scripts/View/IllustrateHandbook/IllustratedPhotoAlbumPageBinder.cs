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
    private const float DeleteButtonShareGap = 8f;

    private readonly Func<IReadOnlyList<PhotoAlbumEntry>> entryLoader;
    private readonly Func<PhotoAlbumEntry, Texture2D> textureLoader;
    private readonly Func<PhotoAlbumEntry, bool> entryDeleter;
    private readonly bool destroyLoadedTextures;
    private readonly List<PhotoAlbumEntry> entries = new List<PhotoAlbumEntry>();
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
    private int currentPageIndex;
    private int selectedEntryIndex = -1;

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

        LoadEntries();
        ClampSelection();
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
        slotRects.Clear();
        slotImages.Clear();
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

        for (int i = 0; i < slotRects.Count; i++)
        {
            RawImage slotImage = EnsureTextureOverlay(slotRects[i]);
            slotImages.Add(slotImage);
            BindSlotClick(slotRects[i], i);
        }

        titleText = FindTextByName(root, "Name");
        descriptionText = FindTextByContent(root, "New Text");
        timeText = FindTextContaining(root, "拍摄时间");
        sceneText = FindTextContaining(root, "拍摄地点");
        unlockText = FindTextContaining(root, "解锁条件");
        pageNumberText = FindTextByName(root, "PageNumber");
        EnsurePagingButtons();
        EnsureDeleteSelectedButton(previewTarget as RectTransform);
    }

    private void LoadEntries()
    {
        entries.Clear();
        IReadOnlyList<PhotoAlbumEntry> loadedEntries = entryLoader();
        if (loadedEntries == null)
        {
            return;
        }

        for (int i = 0; i < loadedEntries.Count; i++)
        {
            if (loadedEntries[i] != null)
            {
                entries.Add(loadedEntries[i]);
            }
        }
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
            bool hasEntry = entryIndex >= 0 && entryIndex < entries.Count;
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
        if (entries.Count == 0 || selectedEntryIndex < 0 || selectedEntryIndex >= entries.Count)
        {
            SetTexture(previewImage, null);
            SetText(titleText, "暂无留念");
            SetText(descriptionText, "进入战斗场景拍照并确认保存后，照片会展示在这里。");
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
        SetText(descriptionText, "本地留念已保存，可在右页选择缩略图切换预览。");
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
            previousPageButton.interactable = entries.Count > 0 && currentPageIndex > 0;
        }

        if (nextPageButton != null)
        {
            nextPageButton.interactable = entries.Count > 0 && currentPageIndex < pageCount - 1;
        }

        if (deleteSelectedButton != null)
        {
            deleteSelectedButton.interactable = selectedEntryIndex >= 0 && selectedEntryIndex < entries.Count;
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
                text.text = $"风景   {entries.Count}/{entries.Count}";
            }
            else if (value.Contains("建筑"))
            {
                text.text = "建筑   0/0";
            }
            else if (value.Contains("000%") || value.Contains("相册收集进度"))
            {
                text.text = entries.Count == 0
                    ? "00/000（000%）"
                    : $"{entries.Count:00}/{entries.Count:000}（100%）";
            }
        }
    }

    private void SelectEntry(int slotIndex)
    {
        int entryIndex = currentPageIndex * EntriesPerPage + slotIndex;
        if (entryIndex < 0 || entryIndex >= entries.Count)
        {
            return;
        }

        selectedEntryIndex = entryIndex;
        Refresh();
    }

    private void SelectPreviousPage()
    {
        if (currentPageIndex <= 0)
        {
            return;
        }

        currentPageIndex--;
        selectedEntryIndex = currentPageIndex * EntriesPerPage;
        Refresh();
    }

    private void SelectNextPage()
    {
        if (currentPageIndex >= GetPageCount() - 1)
        {
            return;
        }

        currentPageIndex++;
        selectedEntryIndex = currentPageIndex * EntriesPerPage;
        Refresh();
    }

    private void DeleteSelectedEntry()
    {
        if (selectedEntryIndex < 0 || selectedEntryIndex >= entries.Count)
        {
            return;
        }

        int deletedIndex = selectedEntryIndex;
        if (!entryDeleter(entries[selectedEntryIndex]))
        {
            return;
        }

        int expectedCount = Mathf.Max(0, entries.Count - 1);
        selectedEntryIndex = expectedCount == 0
            ? -1
            : Mathf.Clamp(deletedIndex, 0, expectedCount - 1);
        currentPageIndex = selectedEntryIndex < 0
            ? 0
            : selectedEntryIndex / EntriesPerPage;
        Refresh();
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
        previousPageButton = EnsurePagingButton(parent, RuntimePreviousButtonName, pageNumberRect, new Vector2(-76f, 0f));
        nextPageButton = EnsurePagingButton(parent, RuntimeNextButtonName, pageNumberRect, new Vector2(76f, 0f));

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
        deleteSelectedButton.onClick.AddListener(DeleteSelectedEntry);
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

        Transform existing = targetRect.Find(RuntimeTextureName);
        GameObject textureObject = existing != null
            ? existing.gameObject
            : new GameObject(RuntimeTextureName, typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        RectTransform textureRect = textureObject.GetComponent<RectTransform>();
        textureRect.SetParent(targetRect, false);
        textureRect.anchorMin = Vector2.zero;
        textureRect.anchorMax = Vector2.one;
        textureRect.offsetMin = Vector2.zero;
        textureRect.offsetMax = Vector2.zero;
        textureRect.pivot = new Vector2(0.5f, 0.5f);
        textureRect.localScale = Vector3.one;
        textureRect.SetAsLastSibling();

        RawImage rawImage = textureObject.GetComponent<RawImage>();
        rawImage.raycastTarget = false;
        rawImage.color = Color.clear;

        AspectRatioFitter fitter = textureObject.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 1.6f;
        return rawImage;
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
            fitter.aspectRatio = texture != null && texture.height > 0
                ? texture.width / (float)texture.height
                : 1.6f;
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
        if (TryCollectRightPanelSlots(root, results))
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

    private static bool TryCollectRightPanelSlots(Transform root, List<RectTransform> results)
    {
        Transform rightPanel = FindTransformByName(root, "RightPanel");
        if (rightPanel == null)
        {
            return false;
        }

        List<RectTransform> candidates = new List<RectTransform>();
        RectTransform[] descendants = rightPanel.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < descendants.Length; i++)
        {
            RectTransform rect = descendants[i];
            Image image = rect != null ? rect.GetComponent<Image>() : null;
            if (rect == null ||
                rect == rightPanel ||
                image == null ||
                !image.enabled ||
                image.color.a < 0.2f ||
                !TryGetSceneSlotOrder(rect.name, out _))
            {
                continue;
            }

            candidates.Add(rect);
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        candidates.Sort((left, right) =>
        {
            TryGetSceneSlotOrder(left.name, out int leftOrder);
            TryGetSceneSlotOrder(right.name, out int rightOrder);
            return leftOrder.CompareTo(rightOrder);
        });

        int count = Mathf.Min(EntriesPerPage, candidates.Count);
        for (int i = 0; i < count; i++)
        {
            results.Add(candidates[i]);
        }

        return true;
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
        if (image == null || rect == null || !image.enabled || image.color.a < 0.2f)
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
                name == "Photo" ||
                name == "BackGround" ||
                name == "Background")
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

    private static string ResolveStageName(PhotoAlbumEntry entry)
    {
        GameplayStageDefinition stage = entry != null ? GameplayStageCatalog.GetStageById(entry.stageId) : null;
        if (stage != null && !string.IsNullOrWhiteSpace(stage.displayName))
        {
            return stage.displayName;
        }

        return entry != null && !string.IsNullOrWhiteSpace(entry.sceneName)
            ? entry.sceneName
            : "未记录场景";
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
