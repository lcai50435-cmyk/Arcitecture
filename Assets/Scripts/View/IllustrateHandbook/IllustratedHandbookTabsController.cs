using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum IllustratedHandbookPage
{
    IllustratedHandbook = 0,
    PersonalInformation = 1,
    PhotoAlbum = 2,
    Mission = 3,
    Setting = 4
}

public sealed class IllustratedHandbookTabsController : MonoBehaviour
{
    public const string RootObjectName = "IllustratedHandbookCanvasUI";
    public const string IllustratedHandbookCanvasName = "IllustratedHandbookCanvas";

    private const string BackgroundName = "BackGround";
    private const string TabsRailName = "TabsRail";
    private const string SelectedTabsRailName = "SelectedTabsRail";
    private const string ContentPanelName = "ContentPanel";
    private const string BookScrollViewName = "BookScrollView";
    private const string BookViewportName = "Viewport";
    private const string BookContentName = "Content";
    private const string BookPageContentPrefix = "PageContent_";
    private const string PageTagName = "PageTag";
    private const string TitleName = "Title";
    private const string SubtitleName = "Subtitle";
    private const string BodyName = "Body";
    private const string FooterName = "Footer";

    private static readonly Color PageBackgroundColor = new Color(0.20f, 0.15f, 0.10f, 0.76f);
    private static readonly Color SelectedButtonColor = new Color(0.78f, 0.62f, 0.33f, 0.98f);
    private static readonly Color TitleColor = new Color(0.98f, 0.95f, 0.87f, 1f);
    private static readonly Color SubtitleColor = new Color(0.84f, 0.81f, 0.73f, 1f);
    private static readonly Color BodyColor = new Color(0.92f, 0.90f, 0.86f, 1f);
    private static readonly Color FooterColor = new Color(0.76f, 0.73f, 0.68f, 1f);
    private static readonly Color CloseButtonColor = new Color(0.48f, 0.16f, 0.14f, 0.96f);
    private static readonly Color SelectedBookmarkTint = new Color(0.92f, 0.92f, 0.90f, 1f);
    private static readonly Color InactiveBookmarkTint = new Color(0.72f, 0.72f, 0.70f, 0.96f);

    private const float BookmarkWidth = 232f;
    private const float BookmarkHeight = 96f;
    private const float BookmarkInactiveX = 850f;
    private const float BookmarkSelectedX = 870f;
    private const float BookmarkAnimationDuration = 0.12f;
    private const float BookmarkInactiveLabelX = 10f;
    private const float BookmarkSelectedLabelX = 20f;
    private const float CloseButtonX = 840f;
    private const float CloseButtonY = -330f;

    private const float BookSafeLeft = 116f;
    private const float BookSafeRight = 372f;
    private const float BookSafeTop = 96f;
    private const float BookSafeBottom = 104f;
    private const float BookContentWidth = 1432f;
    private const float BookContentHeight = 980f;
    private const float BookScrollSensitivity = 34f;

    [SerializeField] private UIManager owner;

    [Header("书本根")]
    [SerializeField] private GameObject illustratedHandbookCanvas;

    [Header("配置")]
    [SerializeField] private IllustratedHandbookPage defaultPage = IllustratedHandbookPage.IllustratedHandbook;
    [SerializeField] private Button closeButton;

    private readonly Dictionary<IllustratedHandbookPage, RectTransform> pageContentRoots =
        new Dictionary<IllustratedHandbookPage, RectTransform>();

    private readonly Dictionary<IllustratedHandbookPage, List<Button>> tabButtons =
        new Dictionary<IllustratedHandbookPage, List<Button>>();

    private readonly Dictionary<Button, TMP_Text> buttonTexts =
        new Dictionary<Button, TMP_Text>();

    private readonly Dictionary<Button, Coroutine> buttonMoveAnimations =
        new Dictionary<Button, Coroutine>();

    private static readonly Dictionary<IllustratedHandbookPage, Sprite> BookmarkSpriteCache =
        new Dictionary<IllustratedHandbookPage, Sprite>();
    private static readonly Dictionary<string, Sprite> BookmarkAssetCache =
        new Dictionary<string, Sprite>(StringComparer.Ordinal);

    private ScrollRect sharedBookScrollRect;
    private RectTransform sharedBookContentRoot;
    private bool initialized;

    public static IllustratedHandbookTabsController EnsureInstalled(UIManager manager)
    {
        if (manager == null || manager.illustratedHandbook == null)
        {
            return null;
        }

        GameObject handbookObject = manager.illustratedHandbook;
        GameObject chromeObject = ResolveChromeObject(handbookObject);
        if (chromeObject == null)
        {
            return null;
        }

        IllustratedHandbookTabsController controller = chromeObject.GetComponent<IllustratedHandbookTabsController>();
        if (controller == null)
        {
            controller = chromeObject.AddComponent<IllustratedHandbookTabsController>();
        }

        controller.owner = manager;
        controller.illustratedHandbookCanvas = chromeObject;
        controller.EnsureInitialized();
        manager.illustratedHandbook = chromeObject;
        return controller;
    }

    public void SwitchToPage(IllustratedHandbookPage page)
    {
        page = NormalizePage(page);
        if (!initialized)
        {
            EnsureInitialized();
        }

        RefreshGeneratedPageContent();
        ActivateChromeRoot();
        SetActiveGeneratedPage(page);
        UpdateButtonState(page);
        ResetScrollPosition();
    }

    public void OpenPage(IllustratedHandbookPage page)
    {
        SwitchToPage(page);
    }

    public void ResetToDefaultPage()
    {
        SwitchToPage(defaultPage);
    }

    private void OnDisable()
    {
        foreach (KeyValuePair<Button, Coroutine> entry in buttonMoveAnimations)
        {
            if (entry.Value != null)
            {
                StopCoroutine(entry.Value);
            }
        }

        buttonMoveAnimations.Clear();
    }

    private void EnsureInitialized()
    {
        EnsureRootCanvas();
        NormalizePageRoot(illustratedHandbookCanvas);

        if (initialized)
        {
            RefreshGeneratedPageContent();
            return;
        }

        EnsurePageScaffolding();
        BindPageButtons();
        BindCloseButtons();
        RefreshGeneratedPageContent();
        initialized = true;
        ResetToDefaultPage();
    }

    private static GameObject ResolveChromeObject(GameObject handbookObject)
    {
        if (handbookObject == null)
        {
            return null;
        }

        if (string.Equals(handbookObject.name, IllustratedHandbookCanvasName, StringComparison.Ordinal))
        {
            return handbookObject;
        }

        if (string.Equals(handbookObject.name, RootObjectName, StringComparison.Ordinal))
        {
            Transform chromeChild = FindDirectChild(handbookObject.transform, IllustratedHandbookCanvasName);
            if (chromeChild != null)
            {
                return chromeChild.gameObject;
            }
        }

        Transform handbookChild = FindDirectChild(handbookObject.transform, IllustratedHandbookCanvasName);
        if (handbookChild != null)
        {
            return handbookChild.gameObject;
        }

        return handbookObject;
    }

    private void EnsurePageScaffolding()
    {
        GameObject chromeRoot = GetChromePageRoot();
        if (chromeRoot == null)
        {
            return;
        }

        Transform background = FindOrCreateBackground(chromeRoot.transform);
        Transform contentPanel = EnsureContentPanel(background);
        sharedBookContentRoot = EnsureSharedContent(contentPanel);

        EnsureGeneratedPage(IllustratedHandbookPage.IllustratedHandbook, "图鉴总览", "已解锁结构、当前运行进度与入口导航。");
        EnsureGeneratedPage(IllustratedHandbookPage.PersonalInformation, "角色", "当前角色、装备与图鉴进度摘要。");
        EnsureGeneratedPage(IllustratedHandbookPage.PhotoAlbum, "相册留念", "本地照片数量与最近留念记录。");
        EnsureGeneratedPage(IllustratedHandbookPage.Setting, "设置", "当前配置摘要，保持在书页区域内显示。");

        EnsureTabsRail(chromeRoot.transform, background as RectTransform);
    }

    private Transform FindOrCreateBackground(Transform pageRoot)
    {
        Transform background = FindDirectChild(pageRoot, BackgroundName);
        bool usesPrefabBackground = background != null;
        if (background == null)
        {
            background = CreateUiObject(BackgroundName, pageRoot).transform;
        }

        RectTransform backgroundRect = background as RectTransform;
        if (backgroundRect != null)
        {
            if (!usesPrefabBackground)
            {
                SetStretch(backgroundRect, 48f, 48f, 48f, 48f);
            }

            backgroundRect.localScale = Vector3.one;
            backgroundRect.anchoredPosition = Vector2.zero;
        }

        Image backgroundImage = background.GetComponent<Image>();
        if (backgroundImage == null)
        {
            backgroundImage = background.gameObject.AddComponent<Image>();
        }

        if (!usesPrefabBackground || backgroundImage.sprite == null)
        {
            RuntimeUiSpriteFactory.ApplySpiritPanelFrameSprite(backgroundImage, PageBackgroundColor);
        }

        backgroundImage.raycastTarget = false;
        HideLegacyBackgroundChildren(background);
        return background;
    }

    private static void HideLegacyBackgroundChildren(Transform background)
    {
        if (background == null)
        {
            return;
        }

        for (int i = 0; i < background.childCount; i++)
        {
            Transform child = background.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (string.Equals(child.name, ContentPanelName, StringComparison.Ordinal) ||
                string.Equals(child.name, TabsRailName, StringComparison.Ordinal) ||
                string.Equals(child.name, SelectedTabsRailName, StringComparison.Ordinal))
            {
                child.gameObject.SetActive(true);
                continue;
            }

            child.gameObject.SetActive(false);
        }
    }

    private Transform EnsureContentPanel(Transform background)
    {
        Transform contentPanel = FindDirectChild(background, ContentPanelName);
        if (contentPanel == null)
        {
            contentPanel = CreateUiObject(ContentPanelName, background).transform;
        }

        contentPanel.gameObject.SetActive(true);

        RectTransform contentRect = contentPanel as RectTransform;
        if (contentRect != null)
        {
            SetStretch(contentRect, BookSafeLeft, BookSafeRight, BookSafeTop, BookSafeBottom);
        }

        Image image = contentPanel.GetComponent<Image>();
        if (image == null)
        {
            image = contentPanel.gameObject.AddComponent<Image>();
        }

        image.sprite = null;
        image.color = Color.clear;
        image.raycastTarget = false;
        return contentPanel;
    }

    private RectTransform EnsureSharedContent(Transform contentPanel)
    {
        Transform scrollRoot = FindDirectChild(contentPanel, BookScrollViewName);
        if (scrollRoot == null)
        {
            scrollRoot = CreateUiObject(BookScrollViewName, contentPanel).transform;
        }

        RectTransform scrollRectTransform = scrollRoot as RectTransform;
        SetStretch(scrollRectTransform, 0f, 0f, 0f, 0f);

        Image scrollImage = scrollRoot.GetComponent<Image>();
        if (scrollImage == null)
        {
            scrollImage = scrollRoot.gameObject.AddComponent<Image>();
        }

        scrollImage.color = new Color(1f, 1f, 1f, 0.001f);
        scrollImage.raycastTarget = true;

        ScrollRect scrollRect = scrollRoot.GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
        }

        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = BookScrollSensitivity;

        Transform viewport = FindDirectChild(scrollRoot, BookViewportName);
        if (viewport == null)
        {
            viewport = CreateUiObject(BookViewportName, scrollRoot).transform;
        }

        RectTransform viewportRect = viewport as RectTransform;
        SetStretch(viewportRect, 0f, 0f, 0f, 0f);

        Image viewportImage = viewport.GetComponent<Image>();
        if (viewportImage == null)
        {
            viewportImage = viewport.gameObject.AddComponent<Image>();
        }

        viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
        viewportImage.raycastTarget = true;

        if (viewport.GetComponent<RectMask2D>() == null)
        {
            viewport.gameObject.AddComponent<RectMask2D>();
        }

        Transform content = FindDirectChild(viewport, BookContentName);
        if (content == null)
        {
            content = CreateUiObject(BookContentName, viewport).transform;
        }

        RectTransform contentRect = content as RectTransform;
        ConfigureAnchoredRect(
            contentRect,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(BookContentWidth, BookContentHeight),
            Vector2.zero,
            new Vector2(0f, 1f));

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        sharedBookScrollRect = scrollRect;
        return contentRect;
    }

    private void EnsureGeneratedPage(IllustratedHandbookPage page, string title, string subtitle)
    {
        string pageName = $"{BookPageContentPrefix}{page}";
        Transform pageTransform = FindDirectChild(sharedBookContentRoot, pageName);
        if (pageTransform == null)
        {
            pageTransform = CreateUiObject(pageName, sharedBookContentRoot).transform;
        }

        RectTransform pageRect = pageTransform as RectTransform;
        ConfigureAnchoredRect(
            pageRect,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(BookContentWidth, BookContentHeight),
            Vector2.zero,
            new Vector2(0f, 1f));

        TMP_Text pageTag = EnsureContentText(pageTransform, PageTagName, "统一多页签图鉴书", 20f, SubtitleColor, TextAlignmentOptions.Left);
        ConfigureTopLeftRect(pageTag.rectTransform, 0f, 4f, 420f, 34f);

        TMP_Text titleText = EnsureContentText(pageTransform, TitleName, title, 42f, TitleColor, TextAlignmentOptions.Left, FontStyles.Bold);
        ConfigureTopLeftRect(titleText.rectTransform, 0f, 48f, 620f, 56f);

        TMP_Text subtitleText = EnsureContentText(pageTransform, SubtitleName, subtitle, 24f, SubtitleColor, TextAlignmentOptions.Left);
        ConfigureTopLeftRect(subtitleText.rectTransform, 0f, 108f, 920f, 42f);

        TMP_Text bodyText = EnsureContentText(pageTransform, BodyName, string.Empty, 27f, BodyColor, TextAlignmentOptions.TopLeft);
        ConfigureTopLeftRect(bodyText.rectTransform, 0f, 170f, 980f, 540f);
        bodyText.lineSpacing = 8f;

        TMP_Text footerText = EnsureContentText(pageTransform, FooterName, string.Empty, 20f, FooterColor, TextAlignmentOptions.TopLeft);
        ConfigureTopLeftRect(footerText.rectTransform, 0f, 760f, 1240f, 72f);

        pageContentRoots[page] = pageRect;
    }

    private void EnsureTabsRail(Transform chromeRoot, RectTransform backgroundRect)
    {
        Transform rail = FindDirectChild(chromeRoot, TabsRailName);
        if (rail == null)
        {
            rail = CreateUiObject(TabsRailName, chromeRoot).transform;
        }

        rail.gameObject.SetActive(true);
        RectTransform railRect = rail as RectTransform;
        if (backgroundRect != null)
        {
            CopyRectTransform(railRect, backgroundRect);
        }
        else
        {
            SetStretch(railRect, 0f, 0f, 0f, 0f);
        }

        Transform selectedRail = FindDirectChild(chromeRoot, SelectedTabsRailName);
        if (selectedRail == null)
        {
            selectedRail = CreateUiObject(SelectedTabsRailName, chromeRoot).transform;
        }

        selectedRail.gameObject.SetActive(true);
        RectTransform selectedRect = selectedRail as RectTransform;
        if (backgroundRect != null)
        {
            CopyRectTransform(selectedRect, backgroundRect);
        }
        else
        {
            SetStretch(selectedRect, 0f, 0f, 0f, 0f);
        }

        if (backgroundRect != null)
        {
            railRect.SetSiblingIndex(backgroundRect.GetSiblingIndex());
            selectedRect.SetSiblingIndex(backgroundRect.GetSiblingIndex() + 1);
        }

        CreateTabButton(rail, IllustratedHandbookPage.IllustratedHandbook, "图鉴");
        CreateTabButton(rail, IllustratedHandbookPage.PersonalInformation, "角色");
        CreateTabButton(rail, IllustratedHandbookPage.PhotoAlbum, "相册");
        CreateTabButton(rail, IllustratedHandbookPage.Setting, "设置");
        CreateCloseButton(rail);
    }

    private void CreateTabButton(Transform parent, IllustratedHandbookPage targetPage, string label)
    {
        string buttonName = $"{targetPage}TabButton";
        Transform existing = FindDirectChild(parent, buttonName);
        GameObject buttonObject = existing != null ? existing.gameObject : CreateUiObject(buttonName, parent);
        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();

        Image background = buttonObject.GetComponent<Image>();
        if (background == null)
        {
            background = buttonObject.AddComponent<Image>();
        }

        ApplyBookmarkSprite(background, targetPage);
        Vector2 size = GetBookmarkSize(background.sprite);
        ConfigureAnchoredRect(
            rectTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            size,
            GetBookmarkAnchoredPosition(targetPage, false),
            new Vector2(0.5f, 0.5f));

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            button = buttonObject.AddComponent<Button>();
        }

        button.targetGraphic = background;
        button.transition = Selectable.Transition.None;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => HandleBookmarkClicked(targetPage));

        TMP_Text text = EnsureButtonLabel(buttonObject.transform, label);
        ConfigureBookmarkLabelRect(text.rectTransform, false);
    }

    private void CreateCloseButton(Transform parent)
    {
        Transform existing = FindDirectChild(parent, "CloseButton");
        GameObject buttonObject = existing != null ? existing.gameObject : CreateUiObject("CloseButton", parent);
        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();

        Image background = buttonObject.GetComponent<Image>();
        if (background == null)
        {
            background = buttonObject.AddComponent<Image>();
        }

        ApplyPlainBookmarkSprite(background, "UI_2");
        Vector2 size = GetBookmarkSize(background.sprite);
        ConfigureAnchoredRect(
            rectTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            size,
            new Vector2(CloseButtonX, CloseButtonY),
            new Vector2(0.5f, 0.5f));

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            button = buttonObject.AddComponent<Button>();
        }

        button.targetGraphic = background;
        button.transition = Selectable.Transition.None;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(HandleCloseRequested);

        TMP_Text text = EnsureButtonLabel(buttonObject.transform, "关闭");
        ConfigureAnchoredRect(
            text.rectTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(132f, 76f),
            new Vector2(20f, 0f),
            new Vector2(0.5f, 0.5f));
    }

    private TMP_Text EnsureButtonLabel(Transform buttonTransform, string label)
    {
        Transform existingLabel = FindDirectChild(buttonTransform, "Label");
        TextMeshProUGUI text = existingLabel != null ? existingLabel.GetComponent<TextMeshProUGUI>() : null;
        if (text == null)
        {
            text = CreateTmpText("Label", buttonTransform, label, 28f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        }

        text.text = label;
        text.fontSize = 28f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        return text;
    }

    private void BindPageButtons()
    {
        tabButtons.Clear();
        buttonTexts.Clear();

        Transform chromeRoot = GetChromePageRoot() != null ? GetChromePageRoot().transform : null;
        if (chromeRoot == null)
        {
            return;
        }

        foreach (IllustratedHandbookPage page in Enum.GetValues(typeof(IllustratedHandbookPage)))
        {
            if (page == IllustratedHandbookPage.Mission)
            {
                continue;
            }

            Button button = FindButtonByName(chromeRoot, $"{page}TabButton");
            if (button == null)
            {
                continue;
            }

            tabButtons[page] = new List<Button> { button };
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                buttonTexts[button] = label;
            }
        }
    }

    private void BindCloseButtons()
    {
        closeButton = null;
        Transform chromeRoot = GetChromePageRoot() != null ? GetChromePageRoot().transform : null;
        if (chromeRoot == null)
        {
            return;
        }

        closeButton = FindButtonByName(chromeRoot, "CloseButton");
    }

    private void HandleBookmarkClicked(IllustratedHandbookPage targetPage)
    {
        MusicManager.PlaySfx(SfxCueId.HandbookBookmark);
        SwitchToPage(targetPage);
    }

    private void HandleCloseRequested()
    {
        UIManager targetManager = owner != null ? owner : FindObjectOfType<UIManager>(true);
        if (targetManager != null)
        {
            targetManager.CloseIllustratedHandbook();
            return;
        }

        gameObject.SetActive(false);
    }

    private void ActivateChromeRoot()
    {
        GameObject chromeRoot = GetChromePageRoot();
        if (chromeRoot != null && !chromeRoot.activeSelf)
        {
            chromeRoot.SetActive(true);
        }
    }

    private void SetActiveGeneratedPage(IllustratedHandbookPage activePage)
    {
        foreach (KeyValuePair<IllustratedHandbookPage, RectTransform> entry in pageContentRoots)
        {
            if (entry.Value != null)
            {
                entry.Value.gameObject.SetActive(entry.Key == activePage);
            }
        }
    }

    private void ResetScrollPosition()
    {
        if (sharedBookScrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        sharedBookScrollRect.verticalNormalizedPosition = 1f;
    }

    private void UpdateButtonState(IllustratedHandbookPage activePage)
    {
        foreach (KeyValuePair<IllustratedHandbookPage, List<Button>> entry in tabButtons)
        {
            bool selected = entry.Key == activePage;
            Color targetColor = GetBookmarkColor(entry.Key, selected);

            for (int i = 0; i < entry.Value.Count; i++)
            {
                Button button = entry.Value[i];
                if (button == null)
                {
                    continue;
                }

                Image background = button.targetGraphic as Image;
                if (background != null)
                {
                    background.color = IsBookmarkSprite(background.sprite)
                        ? (selected ? SelectedBookmarkTint : InactiveBookmarkTint)
                        : targetColor;
                }

                MoveBookmarkToLayer(button, selected);
                AnimateBookmarkRect(button, entry.Key, selected, background != null ? background.sprite : null);

                if (buttonTexts.TryGetValue(button, out TMP_Text label))
                {
                    label.color = Color.white;
                    ConfigureBookmarkLabelRect(label.rectTransform, selected);
                }
            }
        }
    }

    private static void MoveBookmarkToLayer(Button button, bool selected)
    {
        if (button == null || button.transform.parent == null)
        {
            return;
        }

        Transform currentRail = button.transform.parent;
        Transform chromeRoot = currentRail.parent;
        if (chromeRoot == null)
        {
            return;
        }

        string targetRailName = selected ? SelectedTabsRailName : TabsRailName;
        Transform targetRail = FindDirectChild(chromeRoot, targetRailName);
        if (targetRail == null || currentRail == targetRail)
        {
            return;
        }

        button.transform.SetParent(targetRail, false);
        button.transform.SetAsLastSibling();
    }

    private void RefreshGeneratedPageContent()
    {
        UpdateTextPage(IllustratedHandbookPage.IllustratedHandbook, BuildIllustratedHandbookBody(), BuildIllustratedHandbookFooter());
        UpdateTextPage(IllustratedHandbookPage.PersonalInformation, BuildPersonalInformationBody(), BuildPersonalInformationFooter());
        UpdateTextPage(IllustratedHandbookPage.PhotoAlbum, BuildPhotoAlbumBody(), BuildPhotoAlbumFooter());
        UpdateTextPage(IllustratedHandbookPage.Setting, BuildSettingBody(), BuildSettingFooter());
    }

    private void UpdateTextPage(IllustratedHandbookPage page, string body, string footer)
    {
        if (!pageContentRoots.TryGetValue(page, out RectTransform root) || root == null)
        {
            return;
        }

        TMP_Text bodyText = FindTmpText(root, BodyName);
        if (bodyText != null)
        {
            bodyText.text = body;
        }

        TMP_Text footerText = FindTmpText(root, FooterName);
        if (footerText != null)
        {
            footerText.text = footer;
        }
    }

    private string BuildIllustratedHandbookBody()
    {
        RuntimeProgressState runtimeState = ResolveRuntimeProgressState();
        IReadOnlyList<GameplayStageDefinition> stages = GameplayStageCatalog.GetAll();
        int unlockedStageCount = 0;
        int totalStageCount = stages != null ? stages.Count : 0;

        if (stages != null)
        {
            for (int i = 0; i < stages.Count; i++)
            {
                if (runtimeState == null)
                {
                    if (i == 0)
                    {
                        unlockedStageCount++;
                    }

                    continue;
                }

                if (GameplayStageCatalog.IsStageUnlocked(stages[i], runtimeState))
                {
                    unlockedStageCount++;
                }
            }
        }

        List<string> lines = new List<string>
        {
            $"当前场景：{SceneManager.GetActiveScene().name}",
            $"已开放关卡：{unlockedStageCount}/{Mathf.Max(1, totalStageCount)}"
        };

        if (runtimeState != null)
        {
            lines.Add($"图鉴总进度：{runtimeState.GetTotalProgress()}/{runtimeState.GetTotalMaxProgress()}");
            lines.Add($"特殊结构库存：{runtimeState.AvailableSpecialStructureInventory}");
        }
        else
        {
            lines.Add("图鉴总进度：当前场景未挂接运行时进度，进入基地或战斗后自动同步。");
        }

        lines.Add("右侧页签可切到角色、相册、设置。");
        lines.Add("当前实现已回到轻量稳定结构，避免运行时再拼装过多书页 UI。");
        return string.Join("\n", lines);
    }

    private string BuildIllustratedHandbookFooter()
    {
        return "当前书本只保留一套书签和一个黄色内容区，减少运行时生成对象数量。";
    }

    private string BuildPersonalInformationBody()
    {
        PlayerMove playerMove = FindObjectOfType<PlayerMove>(true);
        CharacterCore playerCore = playerMove != null ? playerMove.GetComponent<CharacterCore>() : null;
        if (playerCore == null)
        {
            PlayerAttack playerAttack = FindObjectOfType<PlayerAttack>(true);
            playerCore = playerAttack != null ? playerAttack.GetComponent<CharacterCore>() : null;
        }

        PlayerProfileData profile = FindObjectOfType<PlayerProfileData>(true);
        RuntimeProgressState runtimeState = ResolveRuntimeProgressState();
        WeaponType activeWeapon = profile != null ? profile.effectiveWeaponType : PlayerLoadoutRuntime.CurrentWeaponType;

        List<string> lines = new List<string>
        {
            $"当前场景：{SceneManager.GetActiveScene().name}",
            $"当前墨水基型：{InkTypeCatalog.GetDisplayName(activeWeapon)}"
        };

        if (playerCore != null && playerCore.stats != null)
        {
            lines.Add($"生命：{playerCore.currentHp:0}/{playerCore.stats.maxHp:0}");
            lines.Add($"攻击：{playerCore.stats.attackDamage:0}    防御：{playerCore.stats.defense:0}");
            lines.Add($"移动速度：{playerCore.stats.moveSpeed:0.##}");
        }
        else
        {
            lines.Add("角色属性：当前场景未挂接可读角色，进入基地或战斗后会自动同步。");
        }

        if (profile != null)
        {
            lines.Add($"墨笔耐久：{profile.currentDurability:0}/{Mathf.Max(1f, profile.maxDurability):0}");
        }
        else
        {
            lines.Add("墨笔耐久：当前场景未挂接角色档案。");
        }

        if (runtimeState != null)
        {
            lines.Add($"图鉴总进度：{runtimeState.GetTotalProgress()}/{runtimeState.GetTotalMaxProgress()}");
        }

        return string.Join("\n", lines);
    }

    private string BuildPersonalInformationFooter()
    {
        return "角色入口仍统一走书本，但页面内容保持轻量，不再在这里运行时拼完整独立面板。";
    }

    private string BuildPhotoAlbumBody()
    {
        IReadOnlyList<PhotoAlbumEntry> entries = PhotoAlbumRepository.LoadEntries();
        List<string> lines = new List<string>
        {
            $"本地照片数量：{entries.Count}"
        };

        if (entries.Count == 0)
        {
            lines.Add("暂无留念照片。进入战斗场景后按拍照键保存到本地相册。");
            return string.Join("\n", lines);
        }

        int previewCount = Mathf.Min(4, entries.Count);
        for (int i = 0; i < previewCount; i++)
        {
            PhotoAlbumEntry entry = entries[i];
            GameplayStageDefinition stage = GameplayStageCatalog.GetStageById(entry.stageId);
            string stageName = stage != null
                ? stage.displayName
                : (string.IsNullOrWhiteSpace(entry.sceneName) ? "未记录场景" : entry.sceneName);
            lines.Add($"{i + 1}. {stageName}    {FormatSavedTime(entry.savedAtUtc)}");
        }

        return string.Join("\n", lines);
    }

    private string BuildPhotoAlbumFooter()
    {
        KeyCode captureKey = GameSettingsStore.GetKeyBinding(GameInputAction.PhotoCapture);
        return $"相册入口已统一走书本；战斗内按 {captureKey} 可继续保存本地留念。";
    }

    private string BuildSettingBody()
    {
        GameSettingsDraft settings = GameSettingsStore.LoadSavedSettings();
        Vector2Int resolution = GameSettingsStore.GetResolutionOption(settings.resolutionIndex);
        List<string> lines = new List<string>
        {
            $"分辨率：{resolution.x} x {resolution.y}",
            $"显示模式：{(settings.displayMode == GameDisplayMode.Fullscreen ? "全屏" : "窗口")}",
            $"视野缩放：{GameSettingsStore.GetViewZoomLabel(settings.viewZoomIndex)}",
            $"音量：主 {Mathf.RoundToInt(settings.masterVolume * 100f)}% / 音乐 {Mathf.RoundToInt(settings.musicVolume * 100f)}% / 音效 {Mathf.RoundToInt(settings.sfxVolume * 100f)}%",
            $"攻击：{settings.attackKey}    交互：{settings.interactKey}",
            $"地图：{settings.openMapKey}    暂停：{settings.pauseKey}    拍照：{settings.photoCaptureKey}"
        };

        return string.Join("\n", lines);
    }

    private string BuildSettingFooter()
    {
        return "设置入口统一走书本；完整设置业务仍交给原设置系统处理，避免在图鉴里复制一整套逻辑。";
    }

    private RuntimeProgressState ResolveRuntimeProgressState()
    {
        if (RuntimeProgressState.Instance != null)
        {
            return RuntimeProgressState.Instance;
        }

        return string.Equals(SceneManager.GetActiveScene().name, "MainScene", StringComparison.Ordinal)
            ? null
            : RuntimeProgressState.EnsureInstance();
    }

    private void EnsureRootCanvas()
    {
        GameObject rootObject = GetChromePageRoot();
        if (rootObject == null)
        {
            return;
        }

        Canvas canvas = rootObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = rootObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;

        RectTransform rectTransform = rootObject.GetComponent<RectTransform>();
        if (rectTransform != null && rectTransform.localScale == Vector3.zero)
        {
            rectTransform.localScale = Vector3.one;
        }

        CanvasScaler scaler = rootObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = rootObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        if (rootObject.GetComponent<GraphicRaycaster>() == null)
        {
            rootObject.AddComponent<GraphicRaycaster>();
        }

        CanvasGroup canvasGroup = rootObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = rootObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private GameObject GetChromePageRoot()
    {
        return illustratedHandbookCanvas != null ? illustratedHandbookCanvas : gameObject;
    }

    private static IllustratedHandbookPage NormalizePage(IllustratedHandbookPage page)
    {
        return page == IllustratedHandbookPage.Mission
            ? IllustratedHandbookPage.IllustratedHandbook
            : page;
    }

    private static void NormalizePageRoot(GameObject pageRoot)
    {
        if (pageRoot == null)
        {
            return;
        }

        Canvas canvas = pageRoot.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = RuntimeModalStyle.ModalSortingOrder + 1;
        }

        CanvasGroup canvasGroup = pageRoot.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    private static void ApplyBookmarkSprite(Image image, IllustratedHandbookPage page)
    {
        if (image == null)
        {
            return;
        }

        Sprite sprite = LoadBookmarkSprite(page);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = true;
            return;
        }

        RuntimeUiSpriteFactory.ApplyRoundedSprite(image, GetBookmarkColor(page, false), 10, 10, 1.1f);
    }

    private static void ApplyPlainBookmarkSprite(Image image, string assetName)
    {
        if (image == null)
        {
            return;
        }

        Sprite sprite = LoadBookmarkSprite(assetName);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = true;
            return;
        }

        RuntimeUiSpriteFactory.ApplyRoundedSprite(image, CloseButtonColor, 12, 10, 1.1f);
    }

    private static Sprite LoadBookmarkSprite(IllustratedHandbookPage page)
    {
        if (BookmarkSpriteCache.TryGetValue(page, out Sprite sprite) && sprite != null)
        {
            return sprite;
        }

        sprite = LoadBookmarkSprite(GetBookmarkAssetName(page));
        if (sprite != null)
        {
            BookmarkSpriteCache[page] = sprite;
        }

        return sprite;
    }

    private static Sprite LoadBookmarkSprite(string assetName)
    {
        if (string.IsNullOrWhiteSpace(assetName))
        {
            return null;
        }

        if (BookmarkAssetCache.TryGetValue(assetName, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite sprite = Resources.Load<Sprite>($"UI/NewUI/{assetName}");
        if (sprite != null)
        {
            BookmarkAssetCache[assetName] = sprite;
            return sprite;
        }

#if UNITY_EDITOR
        sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/File/Prop/UIProp/NewUI/{assetName}.png");
        if (sprite != null)
        {
            BookmarkAssetCache[assetName] = sprite;
            return sprite;
        }
#else
        return null;
#endif

        Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null && string.Equals(sprites[i].name, assetName, StringComparison.Ordinal))
            {
                BookmarkAssetCache[assetName] = sprites[i];
                return sprites[i];
            }
        }

        BookmarkAssetCache[assetName] = null;
        return null;
    }

    private static Vector2 GetBookmarkSize(Sprite sprite)
    {
        if (sprite == null || sprite.rect.height <= 0f)
        {
            return new Vector2(BookmarkWidth, BookmarkHeight);
        }

        float aspect = sprite.rect.width / sprite.rect.height;
        return new Vector2(BookmarkHeight * aspect, BookmarkHeight);
    }

    private static Vector2 GetBookmarkAnchoredPosition(IllustratedHandbookPage page, bool selected)
    {
        return new Vector2(selected ? BookmarkSelectedX : BookmarkInactiveX, GetBookmarkAnchoredY(page));
    }

    private static float GetBookmarkAnchoredY(IllustratedHandbookPage page)
    {
        switch (page)
        {
            case IllustratedHandbookPage.IllustratedHandbook:
                return 310f;
            case IllustratedHandbookPage.PersonalInformation:
                return 185f;
            case IllustratedHandbookPage.PhotoAlbum:
                return 60f;
            case IllustratedHandbookPage.Setting:
                return -65f;
            default:
                return 310f;
        }
    }

    private static string GetBookmarkAssetName(IllustratedHandbookPage page)
    {
        switch (page)
        {
            case IllustratedHandbookPage.IllustratedHandbook:
                return "UI_3";
            case IllustratedHandbookPage.PersonalInformation:
                return "UI_4";
            case IllustratedHandbookPage.PhotoAlbum:
                return "UI_5";
            case IllustratedHandbookPage.Setting:
                return "UI_1";
            default:
                return "UI_3";
        }
    }

    private static bool IsBookmarkSprite(Sprite sprite)
    {
        return sprite != null &&
               !string.IsNullOrEmpty(sprite.name) &&
               sprite.name.StartsWith("UI_", StringComparison.Ordinal);
    }

    private static Color GetBookmarkColor(IllustratedHandbookPage page, bool selected)
    {
        Color color;
        switch (page)
        {
            case IllustratedHandbookPage.IllustratedHandbook:
                color = new Color(0.47f, 0.74f, 0.33f, 0.98f);
                break;
            case IllustratedHandbookPage.PersonalInformation:
                color = new Color(0.35f, 0.62f, 0.77f, 0.98f);
                break;
            case IllustratedHandbookPage.PhotoAlbum:
                color = new Color(0.48f, 0.34f, 0.66f, 0.98f);
                break;
            case IllustratedHandbookPage.Setting:
                color = new Color(0.72f, 0.24f, 0.22f, 0.98f);
                break;
            default:
                color = SelectedButtonColor;
                break;
        }

        return selected
            ? Color.Lerp(color, new Color(0.98f, 0.86f, 0.48f, 1f), 0.45f)
            : Color.Lerp(color, Color.black, 0.16f);
    }

    private void AnimateBookmarkRect(Button button, IllustratedHandbookPage page, bool selected, Sprite sprite)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rectTransform = button.transform as RectTransform;
        if (rectTransform == null)
        {
            return;
        }

        Vector2 targetSize = GetBookmarkSize(sprite);
        Vector2 targetPosition = GetBookmarkAnchoredPosition(page, selected);
        if (buttonMoveAnimations.TryGetValue(button, out Coroutine animation) && animation != null)
        {
            StopCoroutine(animation);
            buttonMoveAnimations.Remove(button);
        }

        if (!isActiveAndEnabled || !button.gameObject.activeInHierarchy)
        {
            rectTransform.anchoredPosition = targetPosition;
            rectTransform.sizeDelta = targetSize;
            return;
        }

        buttonMoveAnimations[button] = StartCoroutine(AnimateBookmarkRectRoutine(button, rectTransform, targetSize, targetPosition));
    }

    private IEnumerator AnimateBookmarkRectRoutine(Button button, RectTransform rectTransform, Vector2 targetSize, Vector2 targetPosition)
    {
        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector2 startSize = rectTransform.sizeDelta;
        float elapsed = 0f;

        while (elapsed < BookmarkAnimationDuration)
        {
            if (rectTransform == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / BookmarkAnimationDuration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, eased);
            rectTransform.sizeDelta = Vector2.LerpUnclamped(startSize, targetSize, eased);
            yield return null;
        }

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = targetPosition;
            rectTransform.sizeDelta = targetSize;
        }

        if (button != null)
        {
            buttonMoveAnimations.Remove(button);
        }
    }

    private static void ConfigureBookmarkLabelRect(RectTransform rectTransform, bool selected)
    {
        ConfigureAnchoredRect(
            rectTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(132f, 76f),
            new Vector2(selected ? BookmarkSelectedLabelX : BookmarkInactiveLabelX, 0f),
            new Vector2(0.5f, 0.5f));
    }

    private static Button FindButtonByName(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (!string.Equals(children[i].name, childName, StringComparison.Ordinal))
            {
                continue;
            }

            Button button = children[i].GetComponent<Button>();
            if (button != null)
            {
                return button;
            }
        }

        return null;
    }

    private static TMP_Text FindTmpText(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (!string.Equals(children[i].name, childName, StringComparison.Ordinal))
            {
                continue;
            }

            return children[i].GetComponent<TMP_Text>();
        }

        return null;
    }

    private static TMP_Text EnsureContentText(
        Transform parent,
        string name,
        string text,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment,
        FontStyles fontStyle = FontStyles.Normal)
    {
        Transform existing = FindDirectChild(parent, name);
        TextMeshProUGUI label = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
        if (label == null)
        {
            label = CreateTmpText(name, parent, text, fontSize, color, alignment, fontStyle);
        }

        label.text = text;
        label.color = color;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.fontStyle = fontStyle;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Overflow;
        return label;
    }

    private static TextMeshProUGUI CreateTmpText(
        string name,
        Transform parent,
        string text,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment,
        FontStyles fontStyle = FontStyles.Normal)
    {
        GameObject textObject = CreateUiObject(name, parent);
        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.fontStyle = fontStyle;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Overflow;
        if (TMP_Settings.defaultFontAsset != null)
        {
            label.font = TMP_Settings.defaultFontAsset;
        }

        return label;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = 5;
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (string.Equals(child.name, childName, StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }

    private static void SetStretch(RectTransform rectTransform, float left, float right, float top, float bottom)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(left, bottom);
        rectTransform.offsetMax = new Vector2(-right, -top);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void CopyRectTransform(RectTransform target, RectTransform source)
    {
        if (target == null || source == null)
        {
            return;
        }

        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.offsetMin = source.offsetMin;
        target.offsetMax = source.offsetMax;
        target.sizeDelta = source.sizeDelta;
        target.anchoredPosition = source.anchoredPosition;
        target.pivot = source.pivot;
        target.localScale = source.localScale;
        target.localRotation = source.localRotation;
    }

    private static void ConfigureAnchoredRect(
        RectTransform rectTransform,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 sizeDelta,
        Vector2 anchoredPosition,
        Vector2 pivot)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.pivot = pivot;
    }

    private static void ConfigureTopLeftRect(RectTransform rectTransform, float x, float y, float width, float height)
    {
        ConfigureAnchoredRect(
            rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(width, height),
            new Vector2(x, -y),
            new Vector2(0f, 1f));
    }

    private static string FormatSavedTime(string savedAtUtc)
    {
        if (DateTime.TryParse(
                savedAtUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTime parsed))
        {
            return parsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        }

        return "时间未记录";
    }
}
