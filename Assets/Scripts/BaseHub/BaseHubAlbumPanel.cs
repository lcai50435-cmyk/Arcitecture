using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class BaseHubAlbumSlotView
{
    public Button button;
    public Image background;
    public RawImage thumbnailImage;
    public AspectRatioFitter thumbnailFitter;
    public TextMeshProUGUI labelText;
}

public class BaseHubAlbumPanel : MonoBehaviour
{
    private const int EntriesPerPage = 8;

    private static readonly Color SlotIdleColor = new Color(0.20f, 0.18f, 0.14f, 0.92f);
    private static readonly Color SlotSelectedColor = new Color(0.46f, 0.33f, 0.17f, 0.98f);
    private static readonly Color SlotDisabledColor = new Color(0.15f, 0.14f, 0.13f, 0.82f);

    [SerializeField] private Button closeButton;
    [SerializeField] private Button previousPageButton;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private TextMeshProUGUI pageIndicatorText;
    [SerializeField] private TextMeshProUGUI previewTitleText;
    [SerializeField] private TextMeshProUGUI previewMetaText;
    [SerializeField] private TextMeshProUGUI emptyStateText;
    [SerializeField] private RawImage previewImage;
    [SerializeField] private AspectRatioFitter previewAspectFitter;

    private readonly List<BaseHubAlbumSlotView> slotViews = new List<BaseHubAlbumSlotView>();
    private readonly List<PhotoAlbumEntry> entries = new List<PhotoAlbumEntry>();
    private readonly List<Texture2D> pageTextures = new List<Texture2D>();

    private BaseHubUIController uiController;
    private Texture2D previewTexture;
    private int currentPageIndex;
    private int selectedEntryIndex = -1;

    public void Configure(
        BaseHubUIController controller,
        Button close,
        Button previousPage,
        Button nextPage,
        TextMeshProUGUI pageIndicator,
        TextMeshProUGUI previewTitle,
        TextMeshProUGUI previewMeta,
        TextMeshProUGUI emptyState,
        RawImage preview,
        AspectRatioFitter previewFitter)
    {
        uiController = controller;
        closeButton = close;
        previousPageButton = previousPage;
        nextPageButton = nextPage;
        pageIndicatorText = pageIndicator;
        previewTitleText = previewTitle;
        previewMetaText = previewMeta;
        emptyStateText = emptyState;
        previewImage = preview;
        previewAspectFitter = previewFitter;

        closeButton?.onClick.RemoveAllListeners();
        closeButton?.onClick.AddListener(() => uiController?.CloseAll());

        previousPageButton?.onClick.RemoveAllListeners();
        previousPageButton?.onClick.AddListener(SelectPreviousPage);

        nextPageButton?.onClick.RemoveAllListeners();
        nextPageButton?.onClick.AddListener(SelectNextPage);
    }

    public void RegisterSlot(BaseHubAlbumSlotView slotView)
    {
        if (slotView == null || slotView.button == null)
        {
            return;
        }

        int slotIndex = slotViews.Count;
        slotViews.Add(slotView);
        slotView.button.onClick.RemoveAllListeners();
        slotView.button.onClick.AddListener(() => SelectPageSlot(slotIndex));
    }

    private void OnEnable()
    {
        Open();
    }

    private void OnDisable()
    {
        ReleaseLoadedTextures();
    }

    private void OnDestroy()
    {
        ReleaseLoadedTextures();
    }

    public void Open()
    {
        string selectedEntryId = GetSelectedEntryId();
        entries.Clear();
        entries.AddRange(PhotoAlbumRepository.LoadEntries());

        if (entries.Count == 0)
        {
            selectedEntryIndex = -1;
            currentPageIndex = 0;
            RefreshView();
            return;
        }

        if (!TryRestoreSelection(selectedEntryId))
        {
            selectedEntryIndex = Mathf.Clamp(selectedEntryIndex, 0, entries.Count - 1);
            if (selectedEntryIndex < 0)
            {
                selectedEntryIndex = 0;
            }
        }

        currentPageIndex = Mathf.Clamp(selectedEntryIndex / EntriesPerPage, 0, Mathf.Max(0, GetPageCount() - 1));
        RefreshView();
    }

    private void SelectPreviousPage()
    {
        if (entries.Count == 0)
        {
            return;
        }

        currentPageIndex = Mathf.Max(0, currentPageIndex - 1);
        selectedEntryIndex = Mathf.Min(entries.Count - 1, currentPageIndex * EntriesPerPage);
        RefreshView();
    }

    private void SelectNextPage()
    {
        if (entries.Count == 0)
        {
            return;
        }

        currentPageIndex = Mathf.Min(GetPageCount() - 1, currentPageIndex + 1);
        selectedEntryIndex = Mathf.Min(entries.Count - 1, currentPageIndex * EntriesPerPage);
        RefreshView();
    }

    private void SelectPageSlot(int slotIndex)
    {
        int globalIndex = currentPageIndex * EntriesPerPage + slotIndex;
        if (globalIndex < 0 || globalIndex >= entries.Count)
        {
            return;
        }

        selectedEntryIndex = globalIndex;
        RefreshSelectionState();
        RefreshPreview();
    }

    private void RefreshView()
    {
        ReleaseLoadedTextures();
        RefreshPageSlots();
        RefreshSelectionState();
        RefreshPreview();
        RefreshFooterState();
    }

    private void RefreshPageSlots()
    {
        int pageStartIndex = currentPageIndex * EntriesPerPage;
        for (int i = 0; i < slotViews.Count; i++)
        {
            BaseHubAlbumSlotView slot = slotViews[i];
            int globalIndex = pageStartIndex + i;
            bool hasEntry = globalIndex >= 0 && globalIndex < entries.Count;

            if (slot.button != null)
            {
                slot.button.gameObject.SetActive(hasEntry);
                slot.button.interactable = hasEntry;
            }

            if (!hasEntry)
            {
                ApplySlotPlaceholder(slot);
                continue;
            }

            PhotoAlbumEntry entry = entries[globalIndex];
            Texture2D thumbnailTexture = PhotoAlbumRepository.LoadTexture(entry);
            if (thumbnailTexture != null)
            {
                pageTextures.Add(thumbnailTexture);
            }

            if (slot.thumbnailImage != null)
            {
                slot.thumbnailImage.texture = thumbnailTexture;
                slot.thumbnailImage.color = thumbnailTexture != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            }

            if (slot.thumbnailFitter != null)
            {
                slot.thumbnailFitter.aspectRatio = thumbnailTexture != null && thumbnailTexture.height > 0
                    ? thumbnailTexture.width / (float)thumbnailTexture.height
                    : 1f;
            }

            if (slot.labelText != null)
            {
                slot.labelText.text = BuildSlotLabel(entry, globalIndex);
            }
        }

        if (emptyStateText != null)
        {
            emptyStateText.gameObject.SetActive(entries.Count == 0);
        }
    }

    private void RefreshSelectionState()
    {
        int pageStartIndex = currentPageIndex * EntriesPerPage;
        for (int i = 0; i < slotViews.Count; i++)
        {
            BaseHubAlbumSlotView slot = slotViews[i];
            if (slot == null || slot.background == null)
            {
                continue;
            }

            int globalIndex = pageStartIndex + i;
            bool hasEntry = globalIndex >= 0 && globalIndex < entries.Count;
            bool selected = hasEntry && globalIndex == selectedEntryIndex;
            slot.background.color = !hasEntry
                ? SlotDisabledColor
                : selected
                    ? SlotSelectedColor
                    : SlotIdleColor;
        }
    }

    private void RefreshPreview()
    {
        if (previewImage == null)
        {
            return;
        }

        if (previewTexture != null)
        {
            Destroy(previewTexture);
            previewTexture = null;
        }

        if (entries.Count == 0 || selectedEntryIndex < 0 || selectedEntryIndex >= entries.Count)
        {
            previewImage.texture = null;
            previewImage.color = new Color(1f, 1f, 1f, 0f);
            if (previewAspectFitter != null)
            {
                previewAspectFitter.aspectRatio = 1.6f;
            }

            if (previewTitleText != null)
            {
                previewTitleText.text = "暂无留念";
            }

            if (previewMetaText != null)
            {
                previewMetaText.text = "回到战斗里按下拍照键，新的留念会自动保存在这里。";
            }

            return;
        }

        PhotoAlbumEntry entry = entries[selectedEntryIndex];
        previewTexture = PhotoAlbumRepository.LoadTexture(entry);
        previewImage.texture = previewTexture;
        previewImage.color = previewTexture != null ? Color.white : new Color(1f, 1f, 1f, 0f);

        if (previewAspectFitter != null)
        {
            previewAspectFitter.aspectRatio = previewTexture != null && previewTexture.height > 0
                ? previewTexture.width / (float)previewTexture.height
                : 1.6f;
        }

        if (previewTitleText != null)
        {
            previewTitleText.text = BuildPreviewTitle(entry, selectedEntryIndex);
        }

        if (previewMetaText != null)
        {
            previewMetaText.text = BuildPreviewMeta(entry, previewTexture != null);
        }
    }

    private void RefreshFooterState()
    {
        int pageCount = GetPageCount();
        if (pageIndicatorText != null)
        {
            pageIndicatorText.text = entries.Count == 0
                ? "0 / 0"
                : $"{currentPageIndex + 1} / {pageCount}";
        }

        if (previousPageButton != null)
        {
            previousPageButton.interactable = currentPageIndex > 0;
        }

        if (nextPageButton != null)
        {
            nextPageButton.interactable = currentPageIndex < pageCount - 1;
        }
    }

    private void ApplySlotPlaceholder(BaseHubAlbumSlotView slot)
    {
        if (slot == null)
        {
            return;
        }

        if (slot.thumbnailImage != null)
        {
            slot.thumbnailImage.texture = null;
            slot.thumbnailImage.color = new Color(1f, 1f, 1f, 0f);
        }

        if (slot.thumbnailFitter != null)
        {
            slot.thumbnailFitter.aspectRatio = 1f;
        }

        if (slot.labelText != null)
        {
            slot.labelText.text = string.Empty;
        }
    }

    private string GetSelectedEntryId()
    {
        return selectedEntryIndex >= 0 && selectedEntryIndex < entries.Count
            ? entries[selectedEntryIndex].id
            : null;
    }

    private bool TryRestoreSelection(string selectedEntryId)
    {
        if (string.IsNullOrWhiteSpace(selectedEntryId))
        {
            return false;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            PhotoAlbumEntry entry = entries[i];
            if (entry != null && entry.id == selectedEntryId)
            {
                selectedEntryIndex = i;
                return true;
            }
        }

        return false;
    }

    private int GetPageCount()
    {
        return entries.Count <= 0
            ? 1
            : Mathf.CeilToInt(entries.Count / (float)EntriesPerPage);
    }

    private void ReleaseLoadedTextures()
    {
        for (int i = 0; i < pageTextures.Count; i++)
        {
            Texture2D texture = pageTextures[i];
            if (texture != null)
            {
                Destroy(texture);
            }
        }

        pageTextures.Clear();

        if (previewTexture != null)
        {
            Destroy(previewTexture);
            previewTexture = null;
        }
    }

    private static string BuildSlotLabel(PhotoAlbumEntry entry, int globalIndex)
    {
        string savedTime = FormatSavedTime(entry != null ? entry.savedAtUtc : null, "MM-dd HH:mm");
        return string.IsNullOrEmpty(savedTime)
            ? $"留念 {globalIndex + 1}"
            : savedTime;
    }

    private static string BuildPreviewTitle(PhotoAlbumEntry entry, int globalIndex)
    {
        if (entry == null)
        {
            return "留念";
        }

        GameplayStageDefinition stageDefinition = GameplayStageCatalog.GetStageById(entry.stageId);
        string stageLabel = stageDefinition != null
            ? stageDefinition.displayName
            : (string.IsNullOrWhiteSpace(entry.stageId) ? entry.sceneName : entry.stageId);
        if (string.IsNullOrWhiteSpace(stageLabel))
        {
            stageLabel = $"留念 {globalIndex + 1}";
        }

        return $"留念 {globalIndex + 1} · {stageLabel}";
    }

    private static string BuildPreviewMeta(PhotoAlbumEntry entry, bool hasTexture)
    {
        if (entry == null)
        {
            return "暂无可展示的留念数据。";
        }

        string savedTime = FormatSavedTime(entry.savedAtUtc, "yyyy-MM-dd HH:mm:ss");
        GameplayStageDefinition stageDefinition = GameplayStageCatalog.GetStageById(entry.stageId);
        string stageText = stageDefinition != null
            ? stageDefinition.displayName
            : (string.IsNullOrWhiteSpace(entry.stageId) ? "未记录" : entry.stageId);
        string sceneText = string.IsNullOrWhiteSpace(entry.sceneName) ? "未记录" : entry.sceneName;
        string resolutionText = entry.width > 0 && entry.height > 0
            ? $"{entry.width} x {entry.height}"
            : "未知";
        string statusText = hasTexture ? "文件状态：正常" : "文件状态：读取失败";
        return $"保存时间：{savedTime}\n场景：{sceneText}\n关卡：{stageText}\n分辨率：{resolutionText}\n{statusText}";
    }

    private static string FormatSavedTime(string savedAtUtc, string format)
    {
        if (string.IsNullOrWhiteSpace(savedAtUtc))
        {
            return "未知时间";
        }

        if (!DateTime.TryParse(savedAtUtc, out DateTime parsedTime))
        {
            return "未知时间";
        }

        return parsedTime.ToLocalTime().ToString(format);
    }
}
