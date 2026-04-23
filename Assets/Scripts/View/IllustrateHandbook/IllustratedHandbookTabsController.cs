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
    public const string PersonalInformationCanvasName = "PersonalInformationCanvas";
    public const string PhotoAlbumCanvasName = "PhotoAlbumCanvas";
    public const string MissionCanvasName = "MissionCanvas";
    public const string SettingCanvasName = "SettingCanvas";

    private static readonly string LegacyPersonalInformationCanvasName = string.Concat("Personal", "nformationCanvas");
    private const string BackgroundName = "BackGround";
    private const string TabsRailName = "TabsRail";
    private const string SelectedTabsRailName = "SelectedTabsRail";
    private const string ContentPanelName = "ContentPanel";
    private const string PageTagName = "PageTag";
    private const string TitleName = "Title";
    private const string SubtitleName = "Subtitle";
    private const string BodyName = "Body";
    private const string FooterName = "Footer";

    private static readonly Color PageBackgroundColor = new Color(0.20f, 0.15f, 0.10f, 0.76f);
    private static readonly Color PagePanelColor = new Color(0.16f, 0.12f, 0.08f, 0.78f);
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
    private const float CloseButtonY = -390f;

    [SerializeField] private UIManager owner;

    [Header("标准页根")]
    [SerializeField] private GameObject illustratedHandbookCanvas;
    [SerializeField] private GameObject personalInformationCanvas;
    [SerializeField] private GameObject photoAlbumCanvas;
    [SerializeField] private GameObject missionCanvas;
    [SerializeField] private GameObject settingCanvas;

    [Header("页签按钮")]
    [SerializeField] private Button illustratedHandbookButton;
    [SerializeField] private Button personalInformationButton;
    [SerializeField] private Button photoAlbumButton;
    [SerializeField] private Button missionButton;
    [SerializeField] private Button settingButton;

    [Header("配置")]
    [SerializeField] private IllustratedHandbookPage defaultPage = IllustratedHandbookPage.IllustratedHandbook;
    [SerializeField] private Button closeButton;

    private readonly Dictionary<IllustratedHandbookPage, GameObject> pageRoots =
        new Dictionary<IllustratedHandbookPage, GameObject>();

    private readonly Dictionary<IllustratedHandbookPage, List<Button>> tabButtons =
        new Dictionary<IllustratedHandbookPage, List<Button>>();

    private readonly Dictionary<Button, TMP_Text> buttonTexts =
        new Dictionary<Button, TMP_Text>();

    private readonly Dictionary<Button, Coroutine> buttonMoveAnimations =
        new Dictionary<Button, Coroutine>();

    private static readonly Dictionary<IllustratedHandbookPage, Sprite> BookmarkSpriteCache =
        new Dictionary<IllustratedHandbookPage, Sprite>();

    private bool initialized;

    public static IllustratedHandbookTabsController EnsureInstalled(UIManager manager)
    {
        if (manager == null || manager.illustratedHandbook == null)
        {
            return null;
        }

        GameObject handbookPage = manager.illustratedHandbook;
        GameObject standardRoot = FindExistingStandardRoot(handbookPage);
        GameObject rootObject = standardRoot != null
            ? standardRoot
            : string.Equals(handbookPage.name, RootObjectName, StringComparison.Ordinal)
            ? handbookPage
            : ResolveOrCreateRoot(handbookPage);
        IllustratedHandbookTabsController controller = rootObject.GetComponent<IllustratedHandbookTabsController>();
        if (controller == null)
        {
            controller = rootObject.AddComponent<IllustratedHandbookTabsController>();
        }

        controller.owner = manager;
        controller.illustratedHandbookCanvas = rootObject == handbookPage || rootObject == standardRoot
            ? controller.ResolveOrCreatePageRoot(IllustratedHandbookCanvasName)
            : string.Equals(handbookPage.name, RootObjectName, StringComparison.Ordinal)
            ? controller.ResolveOrCreatePageRoot(IllustratedHandbookCanvasName)
            : ResolveOrAttachPage(rootObject.transform, handbookPage, IllustratedHandbookCanvasName);
        controller.personalInformationCanvas = controller.ResolveOrCreatePageRoot(PersonalInformationCanvasName);
        controller.photoAlbumCanvas = controller.ResolveOrCreatePageRoot(PhotoAlbumCanvasName);
        controller.missionCanvas = controller.ResolveOrCreatePageRoot(MissionCanvasName);
        controller.settingCanvas = controller.ResolveOrCreatePageRoot(SettingCanvasName);
        controller.EnsureInitialized();

        manager.illustratedHandbook = controller.gameObject;
        return controller;
    }

    public void SwitchToPage(IllustratedHandbookPage page)
    {
        if (!initialized)
        {
            EnsureInitialized();
        }

        RefreshGeneratedPageContent();

        foreach (KeyValuePair<IllustratedHandbookPage, GameObject> entry in pageRoots)
        {
            if (entry.Value == null)
            {
                continue;
            }

            bool visible = entry.Key == page;
            entry.Value.SetActive(visible);
        }

        UpdateButtonState(page);
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

        if (initialized)
        {
            RefreshGeneratedPageContent();
            return;
        }

        RegisterPageRoots();
        EnsurePageScaffolding();
        BindPageButtons();
        BindCloseButtons();
        RefreshGeneratedPageContent();
        initialized = true;
        ResetToDefaultPage();
    }

    private void RegisterPageRoots()
    {
        pageRoots.Clear();
        pageRoots[IllustratedHandbookPage.IllustratedHandbook] = illustratedHandbookCanvas;
        pageRoots[IllustratedHandbookPage.PersonalInformation] = personalInformationCanvas;
        pageRoots[IllustratedHandbookPage.PhotoAlbum] = photoAlbumCanvas;
        pageRoots[IllustratedHandbookPage.Mission] = missionCanvas;
        pageRoots[IllustratedHandbookPage.Setting] = settingCanvas;

        foreach (KeyValuePair<IllustratedHandbookPage, GameObject> entry in pageRoots)
        {
            NormalizePageRoot(entry.Value);
        }
    }

    private void EnsurePageScaffolding()
    {
        EnsureGeneratedPage(illustratedHandbookCanvas, "图鉴总览", "已解锁结构、当前运行进度与入口导航。");
        EnsureGeneratedPage(personalInformationCanvas, "角色", "当前角色、装备与图鉴进度摘要。");
        EnsureGeneratedPage(photoAlbumCanvas, "相册留念", "本地照片数量与最近留念记录。");
        EnsureGeneratedPage(missionCanvas, "任务", "关卡开放状态与当前场景进度。");
        EnsureGeneratedPage(settingCanvas, "设置", "当前配置摘要，完整设置仍走原入口。");

        EnsureTabsRail(IllustratedHandbookPage.IllustratedHandbook, illustratedHandbookCanvas);
        EnsureTabsRail(IllustratedHandbookPage.PersonalInformation, personalInformationCanvas);
        EnsureTabsRail(IllustratedHandbookPage.PhotoAlbum, photoAlbumCanvas);
        EnsureTabsRail(IllustratedHandbookPage.Mission, missionCanvas);
        EnsureTabsRail(IllustratedHandbookPage.Setting, settingCanvas);
    }

    private void BindPageButtons()
    {
        tabButtons.Clear();
        buttonTexts.Clear();

        RegisterTabsForPage(IllustratedHandbookPage.IllustratedHandbook, illustratedHandbookCanvas);
        RegisterTabsForPage(IllustratedHandbookPage.PersonalInformation, personalInformationCanvas);
        RegisterTabsForPage(IllustratedHandbookPage.PhotoAlbum, photoAlbumCanvas);
        RegisterTabsForPage(IllustratedHandbookPage.Mission, missionCanvas);
        RegisterTabsForPage(IllustratedHandbookPage.Setting, settingCanvas);

        illustratedHandbookButton = GetPrimaryButton(IllustratedHandbookPage.IllustratedHandbook);
        personalInformationButton = GetPrimaryButton(IllustratedHandbookPage.PersonalInformation);
        photoAlbumButton = GetPrimaryButton(IllustratedHandbookPage.PhotoAlbum);
        missionButton = GetPrimaryButton(IllustratedHandbookPage.Mission);
        settingButton = GetPrimaryButton(IllustratedHandbookPage.Setting);
    }

    private void BindCloseButtons()
    {
        closeButton = null;

        foreach (KeyValuePair<IllustratedHandbookPage, GameObject> entry in pageRoots)
        {
            GameObject pageRoot = entry.Value;
            if (pageRoot == null)
            {
                continue;
            }

            Button pageCloseButton = FindButtonByName(pageRoot.transform, "CloseButton");
            if (pageCloseButton == null)
            {
                continue;
            }

            pageCloseButton.onClick.RemoveListener(HandleCloseRequested);
            pageCloseButton.onClick.AddListener(HandleCloseRequested);
            if (closeButton == null)
            {
                closeButton = pageCloseButton;
            }
        }
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

    private void EnsureGeneratedPage(GameObject pageRoot, string title, string subtitle)
    {
        if (pageRoot == null)
        {
            return;
        }

        Transform background = FindDirectChild(pageRoot.transform, BackgroundName);
        bool usesPrefabBackground = background != null;
        if (background == null)
        {
            background = CreateUiObject(BackgroundName, pageRoot.transform).transform;
        }

        RectTransform backgroundRect = background as RectTransform;
        if (backgroundRect != null && !usesPrefabBackground)
        {
            SetStretch(backgroundRect, 48f, 48f, 48f, 48f);
            backgroundRect.localScale = Vector3.one;
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

        Transform contentPanel = FindDirectChild(background, ContentPanelName);
        if (contentPanel == null)
        {
            contentPanel = CreateUiObject(ContentPanelName, background).transform;
        }

        contentPanel.gameObject.SetActive(true);
        HideLegacyBackgroundChildren(background);

        RectTransform contentRect = contentPanel as RectTransform;
        if (contentRect != null)
        {
            if (usesPrefabBackground)
            {
                SetStretch(contentRect, 96f, 760f, 92f, 112f);
            }
            else
            {
                SetStretch(contentRect, 72f, 280f, 72f, 72f);
            }
        }

        TMP_Text pageTag = EnsureContentText(contentPanel, PageTagName, "统一多页签图鉴", 20f, SubtitleColor, TextAlignmentOptions.Left);
        ConfigureAnchoredRect(pageTag.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(420f, 34f), new Vector2(0f, -8f), new Vector2(0f, 1f));

        TMP_Text titleText = EnsureContentText(contentPanel, TitleName, title, 42f, TitleColor, TextAlignmentOptions.Left, FontStyles.Bold);
        ConfigureAnchoredRect(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(620f, 56f), new Vector2(0f, -56f), new Vector2(0f, 1f));

        TMP_Text subtitleText = EnsureContentText(contentPanel, SubtitleName, subtitle, 24f, SubtitleColor, TextAlignmentOptions.Left);
        ConfigureAnchoredRect(subtitleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(880f, 36f), new Vector2(0f, -114f), new Vector2(0f, 1f));

        TMP_Text bodyText = EnsureContentText(contentPanel, BodyName, string.Empty, 28f, BodyColor, TextAlignmentOptions.TopLeft);
        SetStretch(bodyText.rectTransform, 24f, 24f, 156f, 92f);
        bodyText.rectTransform.pivot = new Vector2(0f, 1f);

        TMP_Text footerText = EnsureContentText(contentPanel, FooterName, string.Empty, 21f, FooterColor, TextAlignmentOptions.BottomLeft);
        ConfigureAnchoredRect(footerText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 52f), new Vector2(0f, 16f), new Vector2(0f, 0f));

        Image contentPanelImage = contentPanel.GetComponent<Image>();
        if (contentPanelImage == null)
        {
            contentPanelImage = contentPanel.gameObject.AddComponent<Image>();
        }

        if (usesPrefabBackground)
        {
            contentPanelImage.sprite = null;
            contentPanelImage.color = Color.clear;
        }
        else
        {
            RuntimeUiSpriteFactory.ApplyRoundedSprite(contentPanelImage, PagePanelColor, 20, 18, 1.2f);
        }

        contentPanelImage.raycastTarget = false;
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

    private void EnsureTabsRail(IllustratedHandbookPage hostPage, GameObject pageRoot)
    {
        if (pageRoot == null)
        {
            return;
        }

        Transform background = FindDirectChild(pageRoot.transform, BackgroundName);
        Transform chromeParent = pageRoot.transform;
        Transform existingRail = FindDirectChild(chromeParent, TabsRailName);
        if (existingRail == null && background != null)
        {
            existingRail = FindDirectChild(background, TabsRailName);
        }

        GameObject railObject = existingRail != null ? existingRail.gameObject : CreateUiObject(TabsRailName, chromeParent);
        if (railObject.transform.parent != chromeParent)
        {
            railObject.transform.SetParent(chromeParent, false);
        }

        railObject.SetActive(true);

        VerticalLayoutGroup legacyLayoutGroup = railObject.GetComponent<VerticalLayoutGroup>();
        if (legacyLayoutGroup != null)
        {
            legacyLayoutGroup.enabled = false;
        }

        RectTransform railRect = railObject.GetComponent<RectTransform>();
        RectTransform backgroundRect = background as RectTransform;
        if (backgroundRect != null)
        {
            CopyRectTransform(railRect, backgroundRect);
        }
        else
        {
            SetStretch(railRect, 0f, 0f, 0f, 0f);
        }

        if (background != null)
        {
            railRect.SetSiblingIndex(background.GetSiblingIndex());
        }

        Transform selectedRail = EnsureSelectedTabsRail(chromeParent, background, backgroundRect);

        CreateTabButton(railObject.transform, hostPage, IllustratedHandbookPage.IllustratedHandbook, "图鉴");
        CreateTabButton(railObject.transform, hostPage, IllustratedHandbookPage.PersonalInformation, "角色");
        CreateTabButton(railObject.transform, hostPage, IllustratedHandbookPage.PhotoAlbum, "相册");
        CreateTabButton(railObject.transform, hostPage, IllustratedHandbookPage.Setting, "设置");

        CreateCloseButton(railObject.transform, hostPage);
        HideUnusedTabButton(railObject.transform, IllustratedHandbookPage.Mission);
        HideUnusedTabButton(selectedRail, IllustratedHandbookPage.Mission);
    }

    private static Transform EnsureSelectedTabsRail(Transform chromeParent, Transform background, RectTransform backgroundRect)
    {
        Transform selectedRail = FindDirectChild(chromeParent, SelectedTabsRailName);
        if (selectedRail == null)
        {
            selectedRail = CreateUiObject(SelectedTabsRailName, chromeParent).transform;
        }

        selectedRail.gameObject.SetActive(true);
        RectTransform selectedRailRect = selectedRail as RectTransform;
        if (backgroundRect != null)
        {
            CopyRectTransform(selectedRailRect, backgroundRect);
        }
        else
        {
            SetStretch(selectedRailRect, 0f, 0f, 0f, 0f);
        }

        if (selectedRailRect == null)
        {
            return selectedRail;
        }

        if (background != null)
        {
            selectedRailRect.SetSiblingIndex(background.GetSiblingIndex() + 1);
        }
        else
        {
            selectedRailRect.SetAsLastSibling();
        }

        return selectedRail;
    }

    private void CreateTabButton(Transform parent, IllustratedHandbookPage hostPage, IllustratedHandbookPage targetPage, string label)
    {
        string buttonName = $"{targetPage}TabButton";
        Transform existingButton = FindDirectChild(parent, buttonName);
        GameObject buttonObject = existingButton != null ? existingButton.gameObject : CreateUiObject(buttonName, parent);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        ConfigureBookmarkRect(buttonRect, targetPage, false, null);

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = buttonObject.AddComponent<LayoutElement>();
        }

        layoutElement.ignoreLayout = true;
        layoutElement.preferredWidth = BookmarkWidth;
        layoutElement.preferredHeight = BookmarkHeight;

        Image background = buttonObject.GetComponent<Image>();
        if (background == null)
        {
            background = buttonObject.AddComponent<Image>();
        }

        ApplyBookmarkSprite(background, targetPage);
        Vector2 bookmarkSize = GetBookmarkSize(background.sprite);
        layoutElement.preferredWidth = bookmarkSize.x;
        layoutElement.preferredHeight = bookmarkSize.y;
        ConfigureBookmarkRect(buttonRect, targetPage, false, background.sprite);

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            button = buttonObject.AddComponent<Button>();
        }

        button.targetGraphic = background;
        button.transition = Selectable.Transition.None;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => HandleBookmarkClicked(targetPage));

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.disabledColor = new Color(0.25f, 0.24f, 0.22f, 0.55f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        Transform existingLabel = FindDirectChild(buttonObject.transform, "Label");
        TextMeshProUGUI text = existingLabel != null ? existingLabel.GetComponent<TextMeshProUGUI>() : null;
        if (text == null)
        {
            text = CreateTmpText("Label", buttonObject.transform, label, 28f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        }

        text.text = label;
        text.fontSize = 28f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        RectTransform textRect = text.rectTransform;
        ConfigureBookmarkLabelRect(textRect, false);
    }

    private void HandleBookmarkClicked(IllustratedHandbookPage targetPage)
    {
        MusicManager.PlaySfx(SfxCueId.HandbookBookmark);
        SwitchToPage(targetPage);
    }

    private void CreateCloseButton(Transform parent, IllustratedHandbookPage hostPage)
    {
        Transform legacySpacer = FindDirectChild(parent, "Spacer");
        if (legacySpacer != null)
        {
            legacySpacer.gameObject.SetActive(false);
        }

        Transform existingButton = FindDirectChild(parent, "CloseButton");
        GameObject buttonObject = existingButton != null ? existingButton.gameObject : CreateUiObject("CloseButton", parent);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        ConfigureAnchoredRect(buttonRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(BookmarkWidth, BookmarkHeight), new Vector2(CloseButtonX, CloseButtonY), new Vector2(0.5f, 0.5f));

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = buttonObject.AddComponent<LayoutElement>();
        }

        layoutElement.ignoreLayout = true;
        layoutElement.preferredWidth = BookmarkWidth;
        layoutElement.preferredHeight = BookmarkHeight;

        Image background = buttonObject.GetComponent<Image>();
        if (background == null)
        {
            background = buttonObject.AddComponent<Image>();
        }

        ApplyPlainBookmarkSprite(background, "UI_2");
        Vector2 closeButtonSize = GetBookmarkSize(background.sprite);
        layoutElement.preferredWidth = closeButtonSize.x;
        layoutElement.preferredHeight = closeButtonSize.y;
        ConfigureAnchoredRect(buttonRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), closeButtonSize, new Vector2(CloseButtonX, CloseButtonY), new Vector2(0.5f, 0.5f));

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            button = buttonObject.AddComponent<Button>();
        }

        button.targetGraphic = background;
        button.transition = Selectable.Transition.None;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(HandleCloseRequested);

        Transform existingLabel = FindDirectChild(buttonObject.transform, "Label");
        TextMeshProUGUI text = existingLabel != null ? existingLabel.GetComponent<TextMeshProUGUI>() : null;
        if (text == null)
        {
            text = CreateTmpText("Label", buttonObject.transform, "关闭", 28f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        }

        text.text = "关闭";
        text.fontSize = 28f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        RectTransform textRect = text.rectTransform;
        ConfigureAnchoredRect(textRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(132f, 76f), new Vector2(20f, 0f), new Vector2(0.5f, 0.5f));
    }

    private static void HideUnusedTabButton(Transform parent, IllustratedHandbookPage page)
    {
        Transform button = FindDirectChild(parent, $"{page}TabButton");
        if (button != null)
        {
            button.gameObject.SetActive(false);
        }
    }

    private void RegisterTabsForPage(IllustratedHandbookPage hostPage, GameObject pageRoot)
    {
        if (pageRoot == null)
        {
            return;
        }

        foreach (IllustratedHandbookPage page in Enum.GetValues(typeof(IllustratedHandbookPage)))
        {
            if (page == IllustratedHandbookPage.Mission)
            {
                continue;
            }

            Button button = FindButtonByName(pageRoot.transform, $"{page}TabButton");
            if (button == null)
            {
                continue;
            }

            if (!tabButtons.TryGetValue(page, out List<Button> buttons))
            {
                buttons = new List<Button>();
                tabButtons[page] = buttons;
            }

            buttons.Add(button);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                buttonTexts[button] = label;
            }
        }
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
        Transform pageRoot = currentRail.parent;
        if (pageRoot == null)
        {
            return;
        }

        string targetRailName = selected ? SelectedTabsRailName : TabsRailName;
        Transform targetRail = FindDirectChild(pageRoot, targetRailName);
        if (targetRail == null || currentRail == targetRail)
        {
            return;
        }

        button.transform.SetParent(targetRail, false);
        button.transform.SetAsLastSibling();
    }

    private static void ApplyBookmarkSprite(Image image, IllustratedHandbookPage page)
    {
        if (image == null)
        {
            return;
        }

        Sprite bookmarkSprite = LoadBookmarkSprite(page);
        if (bookmarkSprite != null)
        {
            image.sprite = bookmarkSprite;
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

        Sprite bookmarkSprite = LoadBookmarkSprite(assetName);
        if (bookmarkSprite != null)
        {
            image.sprite = bookmarkSprite;
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
        if (BookmarkSpriteCache.TryGetValue(page, out Sprite cachedSprite) && cachedSprite != null)
        {
            return cachedSprite;
        }

        Sprite sprite = LoadBookmarkSprite(GetBookmarkAssetName(page));
        if (sprite != null)
        {
            BookmarkSpriteCache[page] = sprite;
        }

        return sprite;
    }

    private static Sprite LoadBookmarkSprite(string assetName)
    {
        Sprite sprite = FindLoadedBookmarkSprite(assetName);
        if (sprite == null)
        {
            sprite = Resources.Load<Sprite>($"UI/NewUI/{assetName}");
        }

#if UNITY_EDITOR
        if (sprite == null)
        {
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/File/Prop/UIProp/NewUI/{assetName}.png");
        }
#endif

        return sprite;
    }

    private static Sprite FindLoadedBookmarkSprite(string assetName)
    {
        Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite != null && string.Equals(sprite.name, assetName, StringComparison.Ordinal))
            {
                return sprite;
            }
        }

        return null;
    }

    private static void ConfigureBookmarkRect(RectTransform rectTransform, IllustratedHandbookPage page, bool selected, Sprite sprite)
    {
        if (rectTransform == null)
        {
            return;
        }

        Vector2 bookmarkSize = GetBookmarkSize(sprite);
        ConfigureAnchoredRect(
            rectTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            bookmarkSize,
            GetBookmarkAnchoredPosition(page, selected),
            new Vector2(0.5f, 0.5f));
        rectTransform.localScale = Vector3.one;
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
        if (buttonMoveAnimations.TryGetValue(button, out Coroutine runningAnimation) && runningAnimation != null)
        {
            StopCoroutine(runningAnimation);
            buttonMoveAnimations.Remove(button);
        }

        if (!isActiveAndEnabled || !button.gameObject.activeInHierarchy)
        {
            ConfigureBookmarkRect(rectTransform, page, selected, sprite);
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
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, easedProgress);
            rectTransform.sizeDelta = Vector2.LerpUnclamped(startSize, targetSize, easedProgress);
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
                return 250f;
            case IllustratedHandbookPage.PersonalInformation:
                return 125f;
            case IllustratedHandbookPage.PhotoAlbum:
                return 0f;
            case IllustratedHandbookPage.Setting:
                return -125f;
            case IllustratedHandbookPage.Mission:
                return -125f;
            default:
                return 250f;
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
            case IllustratedHandbookPage.Mission:
                return "UI_1";
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
            case IllustratedHandbookPage.Mission:
                color = new Color(0.72f, 0.47f, 0.24f, 0.98f);
                break;
            case IllustratedHandbookPage.Setting:
                color = new Color(0.72f, 0.24f, 0.22f, 0.98f);
                break;
            default:
                color = SelectedButtonColor;
                break;
        }

        if (selected)
        {
            return Color.Lerp(color, new Color(0.98f, 0.86f, 0.48f, 1f), 0.45f);
        }

        return Color.Lerp(color, Color.black, 0.16f);
    }

    private void RefreshGeneratedPageContent()
    {
        UpdateTextPage(
            illustratedHandbookCanvas,
            BuildIllustratedHandbookBody(),
            BuildIllustratedHandbookFooter());

        UpdateTextPage(
            personalInformationCanvas,
            BuildPersonalInformationBody(),
            BuildPersonalInformationFooter());

        UpdateTextPage(
            photoAlbumCanvas,
            BuildPhotoAlbumBody(),
            BuildPhotoAlbumFooter());

        UpdateTextPage(
            missionCanvas,
            BuildMissionBody(),
            BuildMissionFooter());

        UpdateTextPage(
            settingCanvas,
            BuildSettingBody(),
            BuildSettingFooter());
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
        lines.Add("这一页作为统一入口，负责承接各场景的图鉴/手册打开行为。");
        return string.Join("\n", lines);
    }

    private string BuildIllustratedHandbookFooter()
    {
        return "默认页固定为图鉴总览；右侧切页时内容会同步切换，不再依赖旧场景里那套散装显隐状态。";
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
            lines.Add($"特殊结构库存：{runtimeState.AvailableSpecialStructureInventory}");
        }
        else
        {
            lines.Add("图鉴总进度：进入基地或战斗后同步本轮运行时进度。");
        }

        return string.Join("\n", lines);
    }

    private string BuildPersonalInformationFooter()
    {
        return "这一页只做多页签统一入口，完整的精灵 / 属性 / 武器面板仍保持原有行为。";
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
        return $"完整相册系统与拍照确认流程保持原实现；战斗内按 {captureKey} 可继续保存本地留念。";
    }

    private string BuildMissionBody()
    {
        RuntimeProgressState runtimeState = ResolveRuntimeProgressState();
        string currentSceneName = SceneManager.GetActiveScene().name;
        List<string> lines = new List<string>();
        IReadOnlyList<GameplayStageDefinition> stages = GameplayStageCatalog.GetAll();

        for (int i = 0; i < stages.Count; i++)
        {
            GameplayStageDefinition stage = stages[i];
            bool unlocked = runtimeState != null
                ? GameplayStageCatalog.IsStageUnlocked(stage, runtimeState)
                : i == 0;
            string currentTag = string.Equals(stage.sceneName, currentSceneName, StringComparison.Ordinal)
                ? "当前场景"
                : "可前往";
            string stateText = unlocked ? currentTag : stage.lockedHint;
            lines.Add($"{stage.displayName}\n{stateText}");
        }

        if (lines.Count == 0)
        {
            lines.Add("暂无可用关卡信息。");
        }

        return string.Join("\n\n", lines);
    }

    private string BuildMissionFooter()
    {
        return "这一页只统一展示关卡状态；基地独立的关卡入口与选择流程保持原行为。";
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
        return "完整设置页仍走原系统入口；这里的目标只是把图鉴多页签在所有场景里统一起来。";
    }

    private void UpdateTextPage(GameObject pageRoot, string body, string footer)
    {
        if (pageRoot == null)
        {
            return;
        }

        TMP_Text bodyText = FindTmpText(pageRoot.transform, BodyName);
        if (bodyText != null)
        {
            bodyText.text = body;
        }

        TMP_Text footerText = FindTmpText(pageRoot.transform, FooterName);
        if (footerText != null)
        {
            footerText.text = footer;
        }
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

    private GameObject ResolveOrCreatePageRoot(string pageName)
    {
        GameObject existing = FindPageRoot(pageName);
        if (existing != null)
        {
            return existing;
        }

        return CreatePageRoot(pageName);
    }

    private GameObject FindPageRoot(string pageName)
    {
        if (string.Equals(pageName, PersonalInformationCanvasName, StringComparison.Ordinal))
        {
            Transform legacy = FindDirectChild(transform, LegacyPersonalInformationCanvasName);
            if (legacy != null)
            {
                legacy.name = PersonalInformationCanvasName;
                return legacy.gameObject;
            }
        }

        Transform target = FindDirectChild(transform, pageName);
        return target != null ? target.gameObject : null;
    }

    private GameObject CreatePageRoot(string pageName)
    {
        GameObject pageRoot = new GameObject(
            pageName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));
        pageRoot.transform.SetParent(transform, false);

        NormalizePageRoot(pageRoot);

        Canvas canvas = pageRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;

        CanvasScaler scaler = pageRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return pageRoot;
    }

    private void EnsureRootCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;

        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform != null)
        {
            SetStretch(rectTransform, 0f, 0f, 0f, 0f);
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }
        else
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
        }

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private static GameObject ResolveOrCreateRoot(GameObject handbookPage)
    {
        Transform currentParent = handbookPage.transform.parent;
        if (currentParent != null && string.Equals(currentParent.name, RootObjectName, StringComparison.Ordinal))
        {
            return currentParent.gameObject;
        }

        GameObject rootObject = new GameObject(
            RootObjectName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));
        Transform originalParent = handbookPage.transform.parent;
        if (originalParent != null)
        {
            rootObject.transform.SetParent(originalParent, false);
        }

        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        if (rootRect != null)
        {
            SetStretch(rootRect, 0f, 0f, 0f, 0f);
            rootRect.localScale = Vector3.one;
        }

        Canvas canvas = rootObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;

        CanvasScaler scaler = rootObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        handbookPage.transform.SetParent(rootObject.transform, false);
        NormalizePageRoot(handbookPage);
        rootObject.SetActive(handbookPage.activeSelf);
        return rootObject;
    }

    private static GameObject FindExistingStandardRoot(GameObject handbookPage)
    {
        if (handbookPage == null)
        {
            return null;
        }

        if (string.Equals(handbookPage.name, RootObjectName, StringComparison.Ordinal) &&
            HasStandardPages(handbookPage.transform))
        {
            return handbookPage;
        }

        Transform[] transforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null ||
                !string.Equals(candidate.name, RootObjectName, StringComparison.Ordinal) ||
                candidate.gameObject == handbookPage)
            {
                continue;
            }

            if (candidate.gameObject.scene != handbookPage.scene)
            {
                continue;
            }

            if (HasStandardPages(candidate))
            {
                return candidate.gameObject;
            }
        }

        return null;
    }

    private static bool HasStandardPages(Transform root)
    {
        return FindDirectChild(root, IllustratedHandbookCanvasName) != null &&
               (FindDirectChild(root, PersonalInformationCanvasName) != null ||
                FindDirectChild(root, LegacyPersonalInformationCanvasName) != null) &&
               FindDirectChild(root, PhotoAlbumCanvasName) != null &&
               FindDirectChild(root, MissionCanvasName) != null &&
               FindDirectChild(root, SettingCanvasName) != null;
    }

    private static GameObject ResolveOrAttachPage(Transform root, GameObject handbookPage, string expectedName)
    {
        if (handbookPage == null)
        {
            return null;
        }

        handbookPage.name = expectedName;
        if (handbookPage.transform.parent != root)
        {
            handbookPage.transform.SetParent(root, false);
        }

        NormalizePageRoot(handbookPage);
        return handbookPage;
    }

    private static void NormalizePageRoot(GameObject pageRoot)
    {
        if (pageRoot == null)
        {
            return;
        }

        RectTransform rectTransform = pageRoot.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            SetStretch(rectTransform, 0f, 0f, 0f, 0f);
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        Canvas canvas = pageRoot.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = RuntimeModalStyle.ModalSortingOrder + 1;
        }
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

    private static Button FindButtonByName(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (!string.Equals(child.name, childName, StringComparison.Ordinal))
            {
                continue;
            }

            Button button = child.GetComponent<Button>();
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
            Transform child = children[i];
            if (!string.Equals(child.name, childName, StringComparison.Ordinal))
            {
                continue;
            }

            return child.GetComponent<TMP_Text>();
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

    private Button GetPrimaryButton(IllustratedHandbookPage page)
    {
        if (!tabButtons.TryGetValue(page, out List<Button> buttons) || buttons.Count == 0)
        {
            return null;
        }

        return buttons[0];
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
