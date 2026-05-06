using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    private const string BookScrollViewName = "BookScrollView";
    private const string BookViewportName = "Viewport";
    private const string BookContentName = "Content";
    private const string BookPageContentPrefix = "PageContent_";
    private const string PageTagName = "PageTag";
    // The fourth bookmark in scene assets keeps the Mission name but displays as Settings; the Setting name maps to the red close button.
    private const string SceneAuthoredSettingTabName = "Mission";
    private const string SceneAuthoredCloseButtonName = "Setting";
    private const string SceneBookmarkHitAreaName = "SceneBookmarkHitArea";
    private const string PersonalPortraitNodeName = "PersonalImage";
    private const string PersonalBackpackSlotNamePrefix = "Slot_";
    private const string PersonalBackpackSlotIconName = "ItemIcon";
    private const string PersonalBackpackSelectionVisualName = "Image";
    private const string PersonalInkRootName = "Weapon";
    private const string PersonalInkOptionNamePrefix = "Image_";
    private const string PersonalInkSelectionVisualName = "Circle";
    private const string PersonalInkSelectionBadgeName = "Designation";
    private const string PersonalInkUsedName = "Used";
    private const string PersonalInkButtonName = "Button";
    private const string PersonalInkDescriptionPanelName = "Image";
    private const string PersonalSelectionBorderName = "SelectedBorder";
    private const string TitleName = "Title";
    private const string SubtitleName = "Subtitle";
    private const string BodyName = "Body";
    private const string FooterName = "Footer";
    private const string SceneAuthoredBookmarkRootName = "BookMark";

    private static readonly Color PageBackgroundColor = new Color(0.20f, 0.15f, 0.10f, 0.76f);
    private static readonly Color SelectedButtonColor = new Color(0.78f, 0.62f, 0.33f, 0.98f);
    private static readonly Color TitleColor = new Color(0.27f, 0.18f, 0.09f, 1f);
    private static readonly Color SubtitleColor = new Color(0.40f, 0.28f, 0.16f, 1f);
    private static readonly Color BodyColor = new Color(0.24f, 0.18f, 0.12f, 1f);
    private static readonly Color FooterColor = new Color(0.48f, 0.38f, 0.25f, 1f);
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
    private const float SceneAuthoredBookmarkSelectedOffsetX = 18f;
    private static readonly Vector2 SceneAuthoredBookmarkSize = new Vector2(200f, 80f);
    private static readonly Vector3 SceneAuthoredBookmarkScale = Vector3.one;
    private const float CloseButtonX = 840f;
    private const float CloseButtonY = -330f;
    private const float PersonalBackpackSlotIconScale = 0.72f;
    private const float SceneHandbookBackpackSlotSize = 80f;
    private const float SceneHandbookBackpackSlotGap = 5.6f;
    private const float SceneHandbookBackpackTrayBottom = 12f;
    private const float SceneHandbookBackpackTrayPaddingX = 12f;
    private const float SceneHandbookBackpackSlotIconScale = 0.72f;
    private const float PersonalInkNormalScale = 1f;
    private const float PersonalInkHoverScale = 1.08f;

    private const float BookSafeLeft = 96f;
    private const float BookSafeRight = 760f;
    private const float BookSafeTop = 92f;
    private const float BookSafeBottom = 112f;
    private const float SceneBookSafeLeft = 56f;
    private const float SceneBookSafeRight = 88f;
    private const float SceneBookSafeTop = 34f;
    private const float SceneBookSafeBottom = 42f;
    private const float BookContentHeight = 920f;
    private const float BookScrollSensitivity = 34f;
    private const string LegacyHandbookCardsContainerName = "LegacyHandbookCards";
    private const int HandbookCardTargetCount = 3;
    private const float HandbookCardCenterX = 230f;
    private const float HandbookCardStartY = 140f;
    private const float HandbookCardVerticalSpacing = 226f;
    private const float SceneAuthoredHandbookCardGap = 14f;
    private const float SceneAuthoredHandbookCardMinSpacing = 64f;
    private const float SceneAuthoredHandbookCardMaxSpacing = 92f;
    private const float SceneAuthoredHandbookPointerTravelX = 3f;
    private const float SceneAuthoredHandbookPointerCycleDuration = 0.72f;
    private const float HandbookRightPageTextX = 790f;
    private const float HandbookRightPageTextWidth = 600f;
    private const string HandbookCardNamePrefix = "ArcitectureImage_";
    private const string LegacyFujianTulouButtonName = "FuJianTuLouButton";
    private const string LegacyHandbookLeftPanelName = "LeftPanel";
    private const string SceneAuthoredRightIntroductionName = "RightIntroduction";
    private const string SceneAuthoredBuildingImageName = "BuildingImage";
    private const string SceneAuthoredBuildingDetailButtonName = "GotoButton";
    private const string SceneAuthoredProprietaryMaterialName = "ProprietaryMaterial";
    private const string SceneAuthoredGeneralMaterialName = "MaterialForGeneralPurpose";
    private const string SceneAuthoredFujianDetailCanvasName = "DetailInformationFuJianCanvas";
    private const string SceneAuthoredShuiXiangDetailCanvasName = "DetailInformationShuiXiangCanvas";
    private const string SceneHandbookBackpackTrayName = "HandbookBackpackTray";
    private const string SceneHandbookBackpackSlotNamePrefix = "HandbookBackpackSlot_";
    private const string SceneHandbookSpecialStackName = "SpecialMaterialStack";
    private const string SceneHandbookSubmitCommonButtonName = "SubmitCommonMaterialButton";
    private const string SceneHandbookItemIconName = "ItemIcon";
    private const int SceneHandbookBackpackSlotCount = 6;
    private const int SceneHandbookBackpackLaneCount = SceneHandbookBackpackSlotCount;
    private const float SceneHandbookProprietarySlotContentInset = 4f;
    private const float SceneHandbookProprietarySlotSpacing = 61f;

    [SerializeField] private UIManager owner;

    [Header("书本根")]
    [SerializeField] private GameObject illustratedHandbookCanvas;
    [SerializeField] private GameObject personalInformationCanvas;
    [SerializeField] private GameObject photoAlbumCanvas;
    [SerializeField] private GameObject missionCanvas;
    [SerializeField] private GameObject settingCanvas;

    [Header("配置")]
    [SerializeField] private IllustratedHandbookPage defaultPage = IllustratedHandbookPage.IllustratedHandbook;
    [SerializeField] private Button closeButton;
    [SerializeField] private RectTransform bookContentPanel;

    private readonly Dictionary<IllustratedHandbookPage, RectTransform> pageContentRoots =
        new Dictionary<IllustratedHandbookPage, RectTransform>();
    private readonly Dictionary<IllustratedHandbookPage, GameObject> scenePageRoots =
        new Dictionary<IllustratedHandbookPage, GameObject>();

    private readonly Dictionary<IllustratedHandbookPage, List<Button>> tabButtons =
        new Dictionary<IllustratedHandbookPage, List<Button>>();

    private readonly Dictionary<Button, TMP_Text> buttonTexts =
        new Dictionary<Button, TMP_Text>();

    private readonly Dictionary<Button, Coroutine> buttonMoveAnimations =
        new Dictionary<Button, Coroutine>();
    private readonly Dictionary<RectTransform, Vector2> sceneBookmarkBasePositions =
        new Dictionary<RectTransform, Vector2>();
    private readonly Dictionary<Transform, int> sceneBookmarkBaseSiblingIndices =
        new Dictionary<Transform, int>();
    private readonly Dictionary<RectTransform, Coroutine> sceneBookmarkAnimations =
        new Dictionary<RectTransform, Coroutine>();
    private readonly List<GameObject> legacyHandbookContentObjects =
        new List<GameObject>();
    private readonly List<SceneAuthoredHandbookCardBinding> sceneHandbookCardBindings =
        new List<SceneAuthoredHandbookCardBinding>();

    private static readonly Dictionary<IllustratedHandbookPage, Sprite> BookmarkSpriteCache =
        new Dictionary<IllustratedHandbookPage, Sprite>();
    private static readonly Dictionary<string, Sprite> BookmarkAssetCache =
        new Dictionary<string, Sprite>(StringComparer.Ordinal);
    private static readonly Dictionary<string, Sprite> SceneHandbookSlotSpriteCache =
        new Dictionary<string, Sprite>(StringComparer.Ordinal);
    private static readonly WeaponType[] PersonalInkOptionWeaponTypes =
    {
        WeaponType.DirectInk,
        WeaponType.BurstInk,
        WeaponType.PierceInk,
        WeaponType.FlowInk
    };
    private static readonly Color SceneHandbookSlotUnlockedColor = new Color(1f, 1f, 1f, 1f);
    private static readonly Color SceneHandbookSlotLockedColor = new Color(0.74f, 0.70f, 0.62f, 0.72f);

    private sealed class SceneAuthoredHandbookCardBinding
    {
        public GameObject cardObject;
        public CatalogueBuildingId buildingId;
        public GameObject selectionFrame;
        public RectTransform selectionPointer;
        public Vector2 selectionPointerBasePosition;
        public BuildingDetailData detailData;
        public Image previewImage;
        public TMP_Text titleText;
        public TMP_Text descriptionText;
    }

    private ScrollRect sharedBookScrollRect;
    private RectTransform sharedBookContentRoot;
    private IllustratedPhotoAlbumPageBinder scenePhotoAlbumBinder;
    private LegacySettingsToggleBinder sceneSettingsToggleBinder;
    private BackpackMananger subscribedBackpack;
    private int selectedPersonalBackpackSlotIndex;
    private GameObject selectedSceneHandbookCardObject;
    private float sceneHandbookPointerTime;
    private bool hasHoveredPersonalInkWeapon;
    private WeaponType hoveredPersonalInkWeaponType;
    private bool initialized;
    private bool usesSceneAuthoredPages;
    private bool personalInformationPageAvailable = true;
    private RuntimeProgressState subscribedRuntimeProgressState;

    public static IllustratedHandbookTabsController EnsureInstalled(UIManager manager)
    {
        if (manager == null || manager.illustratedHandbook == null)
        {
            return null;
        }

        GameObject handbookObject = manager.illustratedHandbook;
        GameObject sceneRoot = ResolveSceneAuthoredRoot(handbookObject);
        if (sceneRoot != null)
        {
            IllustratedHandbookTabsController sceneController = sceneRoot.GetComponent<IllustratedHandbookTabsController>();
            if (sceneController == null)
            {
                sceneController = sceneRoot.AddComponent<IllustratedHandbookTabsController>();
            }

            sceneController.owner = manager;
            sceneController.illustratedHandbookCanvas = ResolveScenePageRoot(sceneRoot.transform, IllustratedHandbookPage.IllustratedHandbook);
            sceneController.personalInformationCanvas = ResolveScenePageRoot(sceneRoot.transform, IllustratedHandbookPage.PersonalInformation);
            sceneController.photoAlbumCanvas = ResolveScenePageRoot(sceneRoot.transform, IllustratedHandbookPage.PhotoAlbum);
            sceneController.missionCanvas = ResolveScenePageRoot(sceneRoot.transform, IllustratedHandbookPage.Mission);
            sceneController.settingCanvas = ResolveScenePageRoot(sceneRoot.transform, IllustratedHandbookPage.Setting);
            sceneController.EnsureInitialized();
            manager.illustratedHandbook = sceneRoot;
            return sceneController;
        }

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

        if (usesSceneAuthoredPages)
        {
            IllustratedHandbookPage resolvedPage = ResolveAvailableSceneAuthoredPage(page);
            ActivateChromeRoot();
            SetActiveSceneAuthoredPage(resolvedPage);
            if (sharedBookContentRoot == null || bookContentPanel == null || pageContentRoots.Count == 0)
            {
                EnsurePageScaffolding();
            }

            RefreshGeneratedPageContent();
            SetActiveGeneratedPage(resolvedPage);
            RefreshSceneAuthoredPhotoAlbum(resolvedPage);
            RefreshSceneAuthoredSettings(resolvedPage);
            if (resolvedPage == IllustratedHandbookPage.IllustratedHandbook)
            {
                RefreshSceneAuthoredHandbookSelection();
                RefreshSceneAuthoredRightIntroduction();
            }

            UpdateSceneAuthoredBookmarkState(resolvedPage);
            ResetScrollPosition();
            return;
        }

        RefreshGeneratedPageContent();
        ActivateChromeRoot();
        page = ResolveAvailableGeneratedPage(page);
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

    public void SetPersonalInformationPageAvailable(bool available)
    {
        if (personalInformationPageAvailable == available)
        {
            RefreshScenePageAvailability();
            return;
        }

        personalInformationPageAvailable = available;
        if (initialized)
        {
            BindPageButtons();
            BindCloseButtons();
        }

        RefreshScenePageAvailability();
        if (!available)
        {
            SwitchToPage(IllustratedHandbookPage.IllustratedHandbook);
        }
    }

    private void Update()
    {
        UpdateSceneAuthoredSelectionPointerAnimation(Time.unscaledDeltaTime);
    }

    private void OnEnable()
    {
        EnsureRuntimeProgressStateSubscription(false);
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

        foreach (KeyValuePair<RectTransform, Coroutine> entry in sceneBookmarkAnimations)
        {
            if (entry.Value != null)
            {
                StopCoroutine(entry.Value);
            }
        }

        sceneBookmarkAnimations.Clear();
        scenePhotoAlbumBinder?.Release();
        sceneSettingsToggleBinder?.Release();
        hasHoveredPersonalInkWeapon = false;
        UnsubscribeBackpackInventoryEvents();
        UnsubscribeRuntimeProgressState();
    }

    private void OnDestroy()
    {
        scenePhotoAlbumBinder?.Release();
        sceneSettingsToggleBinder?.Release();
        UnsubscribeBackpackInventoryEvents();
        UnsubscribeRuntimeProgressState();
    }

    private void EnsureInitialized()
    {
        EnsureRuntimeProgressStateSubscription();

        if (TryUseSceneAuthoredPages())
        {
            if (initialized)
            {
                SetActiveSceneAuthoredPage(defaultPage);
                RefreshGeneratedPageContent();
                SetActiveGeneratedPage(defaultPage);
                UpdateSceneAuthoredBookmarkState(defaultPage);
                return;
            }

            EnsureRootCanvas();
            NormalizePageRoot(illustratedHandbookCanvas);
            SetActiveSceneAuthoredPage(defaultPage);
            EnsurePageScaffolding();
            BindPageButtons();
            BindCloseButtons();
            RefreshGeneratedPageContent();
            initialized = true;
            ResetToDefaultPage();
            return;
        }

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

    private void EnsureRuntimeProgressStateSubscription(bool createIfMissing = true)
    {
        RuntimeProgressState progressState = RuntimeProgressState.Instance;
        if (progressState == null && createIfMissing)
        {
            progressState = RuntimeProgressState.EnsureInstance();
        }

        if (ReferenceEquals(subscribedRuntimeProgressState, progressState))
        {
            return;
        }

        UnsubscribeRuntimeProgressState();
        if (progressState == null)
        {
            return;
        }

        subscribedRuntimeProgressState = progressState;
        subscribedRuntimeProgressState.OnStateChanged += HandleRuntimeProgressStateChanged;
    }

    private void UnsubscribeRuntimeProgressState()
    {
        if (subscribedRuntimeProgressState == null)
        {
            return;
        }

        subscribedRuntimeProgressState.OnStateChanged -= HandleRuntimeProgressStateChanged;
        subscribedRuntimeProgressState = null;
    }

    private void HandleRuntimeProgressStateChanged()
    {
        if (!initialized)
        {
            return;
        }

        if (usesSceneAuthoredPages)
        {
            RefreshSceneAuthoredHandbookSelection();
            RefreshSceneAuthoredRightIntroduction();
            RefreshSceneAuthoredBackpackSurfaces();
            return;
        }

        RefreshGeneratedPageContent();
    }

    private bool TryUseSceneAuthoredPages()
    {
        usesSceneAuthoredPages = illustratedHandbookCanvas != null &&
                                (HasSceneAuthoredPages(illustratedHandbookCanvas.transform) ||
                                 HasSceneAuthoredBookmarkTabs(illustratedHandbookCanvas.transform));
        if (!usesSceneAuthoredPages)
        {
            scenePageRoots.Clear();
            return false;
        }

        scenePageRoots.Clear();
        scenePageRoots[IllustratedHandbookPage.IllustratedHandbook] = illustratedHandbookCanvas;
        scenePageRoots[IllustratedHandbookPage.PersonalInformation] = personalInformationCanvas;
        scenePageRoots[IllustratedHandbookPage.PhotoAlbum] = photoAlbumCanvas;
        scenePageRoots[IllustratedHandbookPage.Mission] = missionCanvas;
        scenePageRoots[IllustratedHandbookPage.Setting] = settingCanvas;

        foreach (KeyValuePair<IllustratedHandbookPage, GameObject> entry in scenePageRoots)
        {
            NormalizePageRoot(entry.Value);
            DisableTransparentGraphicRaycasts(entry.Value);
        }

        return true;
    }

    private void SetActiveSceneAuthoredPage(IllustratedHandbookPage activePage)
    {
        if (!usesSceneAuthoredPages)
        {
            return;
        }

        GameObject activePageRoot = ResolveSceneAuthoredPageRoot(activePage);
        foreach (KeyValuePair<IllustratedHandbookPage, GameObject> entry in scenePageRoots)
        {
            if (entry.Value == null)
            {
                continue;
            }

            entry.Value.SetActive(IsPageAvailable(entry.Key) && ReferenceEquals(entry.Value, activePageRoot));
        }
    }

    private IllustratedHandbookPage ResolveAvailableSceneAuthoredPage(IllustratedHandbookPage page)
    {
        if (!usesSceneAuthoredPages)
        {
            return page;
        }

        return IsPageAvailable(page) &&
               scenePageRoots.TryGetValue(page, out GameObject pageRoot) &&
               pageRoot != null
            ? page
            : IllustratedHandbookPage.IllustratedHandbook;
    }

    private IllustratedHandbookPage ResolveAvailableGeneratedPage(IllustratedHandbookPage page)
    {
        return IsPageAvailable(page) ? page : IllustratedHandbookPage.IllustratedHandbook;
    }

    private bool IsPageAvailable(IllustratedHandbookPage page)
    {
        return page != IllustratedHandbookPage.PersonalInformation || personalInformationPageAvailable;
    }

    private GameObject ResolveSceneAuthoredPageRoot(IllustratedHandbookPage page)
    {
        if (scenePageRoots.TryGetValue(page, out GameObject pageRoot) && pageRoot != null)
        {
            return pageRoot;
        }

        return illustratedHandbookCanvas;
    }

    private List<GameObject> CollectUniqueScenePageRoots()
    {
        List<GameObject> roots = new List<GameObject>();
        foreach (KeyValuePair<IllustratedHandbookPage, GameObject> entry in scenePageRoots)
        {
            AddUniqueSceneBookmarkRoot(roots, entry.Value);
        }

        AddUniqueSceneCloseBookmarkRoot(roots, ResolveSceneAuthoredDetailCanvas(SceneAuthoredFujianDetailCanvasName)?.gameObject);
        AddUniqueSceneCloseBookmarkRoot(roots, ResolveSceneAuthoredDetailCanvas(SceneAuthoredShuiXiangDetailCanvasName)?.gameObject);
        return roots;
    }

    private static void AddUniqueSceneBookmarkRoot(List<GameObject> roots, GameObject pageRoot)
    {
        if (roots == null ||
            pageRoot == null ||
            roots.Contains(pageRoot) ||
            !HasSceneAuthoredBookmarkTabs(pageRoot.transform))
        {
            return;
        }

        roots.Add(pageRoot);
    }

    private static void AddUniqueSceneCloseBookmarkRoot(List<GameObject> roots, GameObject pageRoot)
    {
        if (roots == null ||
            pageRoot == null ||
            roots.Contains(pageRoot) ||
            FindSceneBookmarkVisual(pageRoot.transform, SceneAuthoredCloseButtonName) == null)
        {
            return;
        }

        roots.Add(pageRoot);
    }

    private static GameObject ResolveSceneAuthoredRoot(GameObject handbookObject)
    {
        if (handbookObject == null)
        {
            return null;
        }

        if (string.Equals(handbookObject.name, RootObjectName, StringComparison.Ordinal) &&
            HasSceneAuthoredUi(handbookObject.transform))
        {
            return handbookObject;
        }

        Transform current = handbookObject.transform.parent;
        while (current != null)
        {
            if (string.Equals(current.name, RootObjectName, StringComparison.Ordinal) &&
                HasSceneAuthoredUi(current))
            {
                return current.gameObject;
            }

            current = current.parent;
        }

        return null;
    }

    private static bool HasSceneAuthoredPages(Transform root)
    {
        return ResolveScenePageRoot(root, IllustratedHandbookPage.IllustratedHandbook) != null &&
               ResolveScenePageRoot(root, IllustratedHandbookPage.PersonalInformation) != null &&
               ResolveScenePageRoot(root, IllustratedHandbookPage.PhotoAlbum) != null &&
               ResolveScenePageRoot(root, IllustratedHandbookPage.Setting) != null;
    }

    private static bool HasSceneAuthoredUi(Transform root)
    {
        return HasSceneAuthoredPages(root) || HasSceneAuthoredBookmarkTabs(root);
    }

    private static bool HasSceneAuthoredBookmarkTabs(Transform root)
    {
        return root != null &&
               FindSceneBookmarkVisual(root, IllustratedHandbookPage.IllustratedHandbook) != null &&
               FindSceneBookmarkVisual(root, IllustratedHandbookPage.PersonalInformation) != null &&
               FindSceneBookmarkVisual(root, IllustratedHandbookPage.PhotoAlbum) != null &&
               FindSceneBookmarkVisual(root, IllustratedHandbookPage.Setting) != null &&
               FindSceneBookmarkVisual(root, SceneAuthoredCloseButtonName) != null;
    }

    private static GameObject ResolveScenePageRoot(Transform root, IllustratedHandbookPage page)
    {
        if (root == null)
        {
            return null;
        }

        switch (page)
        {
            case IllustratedHandbookPage.IllustratedHandbook:
                return FindDirectChild(root, IllustratedHandbookCanvasName)?.gameObject;
            case IllustratedHandbookPage.PersonalInformation:
                return (FindDirectChild(root, PersonalInformationCanvasName) ??
                        FindDirectChild(root, LegacyPersonalInformationCanvasName))?.gameObject;
            case IllustratedHandbookPage.PhotoAlbum:
                return FindDirectChild(root, PhotoAlbumCanvasName)?.gameObject;
            case IllustratedHandbookPage.Mission:
                return FindDirectChild(root, MissionCanvasName)?.gameObject;
            case IllustratedHandbookPage.Setting:
                return FindDirectChild(root, SettingCanvasName)?.gameObject;
            default:
                return null;
        }
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

        if (usesSceneAuthoredPages)
        {
            EnsureSceneAuthoredHandbookCardsLayout();
            return;
        }

        Transform background = FindOrCreateBackground(chromeRoot.transform);
        Transform contentPanel = EnsureContentPanel(background);
        sharedBookContentRoot = EnsureSharedContent(contentPanel);

        EnsureGeneratedPage(IllustratedHandbookPage.IllustratedHandbook, "图鉴总览", "已解锁结构、当前运行进度与入口导航。");
        EnsureGeneratedPage(IllustratedHandbookPage.PersonalInformation, "角色", "当前角色、装备与图鉴进度摘要。");
        EnsureGeneratedPage(IllustratedHandbookPage.PhotoAlbum, "相册留念", "本地照片数量与最近留念记录。");
        EnsureGeneratedPage(IllustratedHandbookPage.Mission, "任务", "当前任务、目标与进度摘要。");
        EnsureGeneratedPage(IllustratedHandbookPage.Setting, "设置", "当前配置摘要，保持在书页区域内显示。");
        EnsureHandbookLegacyCardsLayout();

        if (!usesSceneAuthoredPages)
        {
            EnsureTabsRail(chromeRoot.transform, background as RectTransform);
        }
    }

    private Transform FindOrCreateBackground(Transform pageRoot)
    {
        Transform background = FindDirectChild(pageRoot, BackgroundName);
        bool usesPrefabBackground = background != null;
        if (background == null)
        {
            Transform panel = FindDirectChild(pageRoot, "Panel");
            if (panel != null)
            {
                background = panel;
                usesPrefabBackground = true;
            }
            else
            {
                background = CreateUiObject(BackgroundName, pageRoot).transform;
            }
        }

        RectTransform backgroundRect = background as RectTransform;
        if (backgroundRect != null)
        {
            if (!usesPrefabBackground)
            {
                SetStretch(backgroundRect, 48f, 48f, 48f, 48f);
                backgroundRect.localScale = Vector3.one;
                backgroundRect.anchoredPosition = Vector2.zero;
            }
        }

        Image backgroundImage = background.GetComponent<Image>();
        if (backgroundImage == null && !usesPrefabBackground)
        {
            backgroundImage = background.gameObject.AddComponent<Image>();
        }

        if (!usesPrefabBackground && backgroundImage != null && backgroundImage.sprite == null)
        {
            RuntimeUiSpriteFactory.ApplySpiritPanelFrameSprite(backgroundImage, PageBackgroundColor);
        }

        if (backgroundImage != null)
        {
            backgroundImage.raycastTarget = false;
        }

        CacheLegacyBackgroundChildren(background);
        return background;
    }

    private void CacheLegacyBackgroundChildren(Transform background)
    {
        if (background == null)
        {
            return;
        }

        legacyHandbookContentObjects.Clear();

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
                continue;
            }

            if (string.Equals(child.name, "CloseButton", StringComparison.Ordinal) ||
                IsLegacyHandbookContentLayer(child))
            {
                legacyHandbookContentObjects.Add(child.gameObject);
                continue;
            }

            CollectLegacyHandbookContentObjects(child);
        }
    }

    private void CollectLegacyHandbookContentObjects(Transform root)
    {
        if (root == null)
        {
            return;
        }

        if (IsLegacyHandbookContentLayer(root))
        {
            legacyHandbookContentObjects.Add(root.gameObject);
            return;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            CollectLegacyHandbookContentObjects(root.GetChild(i));
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
            if (usesSceneAuthoredPages)
            {
                SetStretch(contentRect, SceneBookSafeLeft, SceneBookSafeRight, SceneBookSafeTop, SceneBookSafeBottom);
            }
            else
            {
                SetStretch(contentRect, BookSafeLeft, BookSafeRight, BookSafeTop, BookSafeBottom);
            }

            contentRect.localScale = Vector3.one;
            bookContentPanel = contentRect;
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

        bool allowScrollRaycasts = !usesSceneAuthoredPages;
        scrollImage.color = new Color(1f, 1f, 1f, 0.001f);
        scrollImage.raycastTarget = allowScrollRaycasts;

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
        viewportImage.raycastTarget = allowScrollRaycasts;

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
            new Vector2(1f, 1f),
            new Vector2(0f, BookContentHeight),
            Vector2.zero,
            new Vector2(0.5f, 1f));

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
            new Vector2(1f, 1f),
            new Vector2(0f, BookContentHeight),
            Vector2.zero,
            new Vector2(0.5f, 1f));

        TMP_Text pageTag = EnsureContentText(pageTransform, PageTagName, "统一多页签图鉴书", 12f, SubtitleColor, TextAlignmentOptions.Left);
        ConfigureAnchoredRect(
            pageTag.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(HandbookRightPageTextWidth, 24f),
            new Vector2(HandbookRightPageTextX, -8f),
            new Vector2(0f, 1f));

        TMP_Text titleText = EnsureContentText(pageTransform, TitleName, title, 24f, TitleColor, TextAlignmentOptions.Left, FontStyles.Bold);
        ConfigureAnchoredRect(
            titleText.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(HandbookRightPageTextWidth, 36f),
            new Vector2(HandbookRightPageTextX, -42f),
            new Vector2(0f, 1f));

        TMP_Text subtitleText = EnsureContentText(pageTransform, SubtitleName, subtitle, 13f, SubtitleColor, TextAlignmentOptions.Left);
        ConfigureAnchoredRect(
            subtitleText.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(HandbookRightPageTextWidth, 44f),
            new Vector2(HandbookRightPageTextX, -84f),
            new Vector2(0f, 1f));

        TMP_Text bodyText = EnsureContentText(pageTransform, BodyName, string.Empty, 15f, BodyColor, TextAlignmentOptions.TopLeft);
        ConfigureAnchoredRect(
            bodyText.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(HandbookRightPageTextWidth, 700f),
            new Vector2(HandbookRightPageTextX, -140f),
            new Vector2(0f, 1f));
        bodyText.rectTransform.pivot = new Vector2(0f, 1f);
        bodyText.lineSpacing = 2f;

        TMP_Text footerText = EnsureContentText(pageTransform, FooterName, string.Empty, 12f, FooterColor, TextAlignmentOptions.BottomLeft);
        ConfigureAnchoredRect(
            footerText.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(HandbookRightPageTextWidth, 44f),
            new Vector2(HandbookRightPageTextX, 16f),
            new Vector2(0f, 0f));

        pageContentRoots[page] = pageRect;
    }

    private void EnsureHandbookLegacyCardsLayout()
    {
        if (!pageContentRoots.TryGetValue(IllustratedHandbookPage.IllustratedHandbook, out RectTransform handbookPage) ||
            handbookPage == null)
        {
            return;
        }

        Transform container = FindDirectChild(handbookPage, LegacyHandbookCardsContainerName);
        if (container == null)
        {
            container = CreateUiObject(LegacyHandbookCardsContainerName, handbookPage).transform;
        }

        RectTransform containerRect = container as RectTransform;
        SetStretch(containerRect, 0f, 0f, 0f, 0f);

        List<RectTransform> cardRects = new List<RectTransform>();
        CollectExistingHandbookCards(container, cardRects);

        for (int i = 0; i < legacyHandbookContentObjects.Count; i++)
        {
            GameObject contentObject = legacyHandbookContentObjects[i];
            if (contentObject == null ||
                !IsLegacyHandbookContentLayer(contentObject.transform))
            {
                continue;
            }

            RectTransform cardRect = contentObject.transform as RectTransform;
            if (cardRect == null)
            {
                continue;
            }

            if (cardRect.parent != container)
            {
                cardRect.SetParent(container, false);
            }

            cardRect.localScale = Vector3.one;
            cardRect.localRotation = Quaternion.identity;
            AddUniqueCardRect(cardRects, cardRect);
        }

        NormalizeHandbookCardIdentities(cardRects);
        EnsureMinimumHandbookCards(cardRects, container);
        SortHandbookCardsForSceneLayout(cardRects);
        for (int i = 0; i < cardRects.Count; i++)
        {
            RectTransform cardRect = cardRects[i];
            bool visible = i < HandbookCardTargetCount;
            ConfigureAnchoredRect(
                cardRect,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                cardRect.sizeDelta,
                new Vector2(HandbookCardCenterX, -(HandbookCardStartY + i * HandbookCardVerticalSpacing)),
                new Vector2(0.5f, 0.5f));
            cardRect.gameObject.SetActive(visible);
        }

        SetGeneratedTextVisibility(handbookPage, false);

        legacyHandbookContentObjects.RemoveAll(item =>
            item != null &&
            item.transform.parent == container &&
            IsLegacyHandbookContentLayer(item.transform));
    }

    private static void CollectExistingHandbookCards(Transform container, List<RectTransform> cardRects)
    {
        if (container == null || cardRects == null)
        {
            return;
        }

        for (int i = 0; i < container.childCount; i++)
        {
            Transform child = container.GetChild(i);
            if (child == null || !IsLegacyHandbookContentLayer(child))
            {
                continue;
            }

            AddUniqueCardRect(cardRects, child as RectTransform);
        }
    }

    private static void AddUniqueCardRect(List<RectTransform> cardRects, RectTransform cardRect)
    {
        if (cardRects == null || cardRect == null || cardRects.Contains(cardRect))
        {
            return;
        }

        cardRects.Add(cardRect);
    }

    private static void NormalizeHandbookCardIdentities(List<RectTransform> cardRects)
    {
        if (cardRects == null)
        {
            return;
        }

        cardRects.Sort((left, right) => GetHandbookCardOrder(left).CompareTo(GetHandbookCardOrder(right)));
        for (int i = 0; i < cardRects.Count; i++)
        {
            RectTransform cardRect = cardRects[i];
            if (cardRect == null)
            {
                continue;
            }

            cardRect.name = $"{HandbookCardNamePrefix}{i + 1}";
            ApplyCatalogueBuildingId(cardRect.gameObject, i);
        }
    }

    private void EnsureSceneAuthoredHandbookCardsLayout()
    {
        ResetSceneAuthoredSelectionPointers();
        sceneHandbookCardBindings.Clear();

        if (illustratedHandbookCanvas == null)
        {
            return;
        }

        Transform leftPanel = FindTransformByName(illustratedHandbookCanvas.transform, LegacyHandbookLeftPanelName);
        if (leftPanel == null)
        {
            return;
        }

        List<RectTransform> cardRects = new List<RectTransform>();
        CollectSceneHandbookCards(leftPanel, cardRects);
        if (cardRects.Count == 0)
        {
            return;
        }

        SortHandbookCardsForSceneLayout(cardRects);
        RectTransform template = cardRects[0];
        Vector2 templateAnchorMin = template.anchorMin;
        Vector2 templateAnchorMax = template.anchorMax;
        Vector2 templateSize = template.sizeDelta;
        Vector2 templatePivot = template.pivot;
        Vector2 startPosition = template.anchoredPosition;
        float verticalSpacing = Mathf.Clamp(
            templateSize.y + SceneAuthoredHandbookCardGap,
            SceneAuthoredHandbookCardMinSpacing,
            SceneAuthoredHandbookCardMaxSpacing);

        NormalizeHandbookCardIdentities(cardRects);
        for (int i = cardRects.Count; i < HandbookCardTargetCount; i++)
        {
            GameObject cardObject = Instantiate(template.gameObject, leftPanel, false);
            cardObject.name = $"{HandbookCardNamePrefix}{i + 1}";
            RectTransform cardRect = cardObject.transform as RectTransform;
            ApplyCatalogueBuildingId(cardObject, i, false);
            AddUniqueCardRect(cardRects, cardRect);
        }

        SortHandbookCardsForSceneLayout(cardRects);
        for (int i = 0; i < cardRects.Count; i++)
        {
            RectTransform cardRect = cardRects[i];
            if (cardRect == null)
            {
                continue;
            }

            bool visible = i < HandbookCardTargetCount;
            ConfigureAnchoredRect(
                cardRect,
                templateAnchorMin,
                templateAnchorMax,
                templateSize,
                new Vector2(startPosition.x, startPosition.y - i * verticalSpacing),
                templatePivot);
            cardRect.localScale = Vector3.one;
            cardRect.localRotation = Quaternion.identity;
            cardRect.gameObject.SetActive(visible);
            EnsureSceneAuthoredCardStatusVisuals(cardRect, template);
            ConfigureSceneAuthoredBuildingCard(cardRect.gameObject, i);
            ConfigureSceneAuthoredSelectionCard(cardRect.gameObject, i);
        }

        HideLooseSceneAuthoredStatusVisuals(leftPanel);
        EnsureSelectedSceneAuthoredHandbookCard();
        RefreshSceneAuthoredHandbookSelection();
        RefreshSceneAuthoredRightIntroduction();
    }

    private static void EnsureSceneAuthoredCardStatusVisuals(RectTransform cardRect, RectTransform template)
    {
        if (cardRect == null || template == null)
        {
            return;
        }

        EnsureSceneAuthoredCardStatusVisual(cardRect, template, "Lock");
        EnsureSceneAuthoredCardStatusVisual(cardRect, template, "Unlock");
    }

    private static void EnsureSceneAuthoredCardStatusVisual(RectTransform cardRect, RectTransform template, string childName)
    {
        if (FindDirectChild(cardRect, childName) != null)
        {
            return;
        }

        Transform templateVisual = FindDirectChild(template, childName);
        if (templateVisual == null)
        {
            return;
        }

        GameObject visualObject = Instantiate(templateVisual.gameObject, cardRect, false);
        visualObject.name = childName;
    }

    private static void ConfigureSceneAuthoredBuildingCard(GameObject cardObject, int zeroBasedIndex)
    {
        if (cardObject == null)
        {
            return;
        }

        CatalogueBuildingId buildingId = ResolveCatalogueBuildingId(cardObject, zeroBasedIndex);

        CatalogueBuildingUnlockState unlockState = cardObject.GetComponent<CatalogueBuildingUnlockState>();
        if (unlockState == null)
        {
            unlockState = cardObject.AddComponent<CatalogueBuildingUnlockState>();
        }

        unlockState.buildingId = buildingId;
        unlockState.buildingSlider = ResolveSceneAuthoredBuildingSlider(cardObject.transform);
        ConfigureSliderReadOnlyDisplay(unlockState.buildingSlider);
        unlockState.lockedBuildingVisual = ResolveSceneAuthoredStatusVisual(cardObject.transform, "Lock");
        unlockState.unlockedBuildingVisual = ResolveSceneAuthoredStatusVisual(cardObject.transform, "Unlock");
        unlockState.lockedBuildingButton = unlockState.lockedBuildingVisual != null
            ? unlockState.lockedBuildingVisual.GetComponent<Button>()
            : null;
        unlockState.RefreshState();
    }

    private void ConfigureSceneAuthoredSelectionCard(GameObject cardObject, int zeroBasedIndex)
    {
        if (cardObject == null)
        {
            return;
        }

        CatalogueBuildingId buildingId = ResolveCatalogueBuildingId(cardObject, zeroBasedIndex);
        SceneAuthoredHandbookCardSelection selection = cardObject.GetComponent<SceneAuthoredHandbookCardSelection>();
        if (selection == null)
        {
            selection = cardObject.AddComponent<SceneAuthoredHandbookCardSelection>();
        }

        selection.Bind(this);

        Button button = cardObject.GetComponent<Button>();
        if (button != null)
        {
            EnsureButtonRaycastTarget(button);
        }

        GameObject selectionFrame = ResolveSceneAuthoredSelectionFrame(cardObject.transform);
        if (selectionFrame != null)
        {
            Graphic frameGraphic = selectionFrame.GetComponent<Graphic>();
            if (frameGraphic != null)
            {
                frameGraphic.raycastTarget = false;
            }
        }

        Image previewImage = ResolveSceneAuthoredCardPreviewImage(cardObject.transform, selectionFrame);
        RectTransform selectionPointer = ResolveSceneAuthoredSelectionPointer(
            cardObject.transform,
            selectionFrame,
            previewImage);
        if (selectionPointer != null)
        {
            Graphic pointerGraphic = selectionPointer.GetComponent<Graphic>();
            if (pointerGraphic != null)
            {
                pointerGraphic.raycastTarget = false;
            }
        }

        sceneHandbookCardBindings.Add(new SceneAuthoredHandbookCardBinding
        {
            cardObject = cardObject,
            buildingId = buildingId,
            selectionFrame = selectionFrame,
            selectionPointer = selectionPointer,
            selectionPointerBasePosition = selectionPointer != null
                ? selectionPointer.anchoredPosition
                : Vector2.zero,
            detailData = cardObject.GetComponent<BuildingDetailData>() ??
                         cardObject.GetComponentInChildren<BuildingDetailData>(true),
            previewImage = previewImage,
            titleText = ResolveSceneAuthoredCardTitleText(cardObject.transform),
            descriptionText = ResolveSceneAuthoredCardDescriptionText(cardObject.transform)
        });
    }

    public void SelectSceneAuthoredHandbookCard(GameObject cardObject)
    {
        if (cardObject == null)
        {
            return;
        }

        for (int i = 0; i < sceneHandbookCardBindings.Count; i++)
        {
            SceneAuthoredHandbookCardBinding binding = sceneHandbookCardBindings[i];
            if (binding == null || binding.cardObject != cardObject)
            {
                continue;
            }

            if (selectedSceneHandbookCardObject != cardObject)
            {
                sceneHandbookPointerTime = 0f;
            }

            selectedSceneHandbookCardObject = cardObject;
            RefreshSceneAuthoredHandbookSelection();
            RefreshSceneAuthoredRightIntroduction();
            return;
        }
    }

    private void EnsureSelectedSceneAuthoredHandbookCard()
    {
        if (sceneHandbookCardBindings.Count == 0)
        {
            selectedSceneHandbookCardObject = null;
            return;
        }

        if (selectedSceneHandbookCardObject != null)
        {
            for (int i = 0; i < sceneHandbookCardBindings.Count; i++)
            {
                SceneAuthoredHandbookCardBinding binding = sceneHandbookCardBindings[i];
                if (binding != null &&
                    binding.cardObject == selectedSceneHandbookCardObject &&
                    binding.cardObject.activeInHierarchy)
                {
                    return;
                }
            }
        }

        for (int i = 0; i < sceneHandbookCardBindings.Count; i++)
        {
            SceneAuthoredHandbookCardBinding binding = sceneHandbookCardBindings[i];
            if (binding?.cardObject == null || !binding.cardObject.activeInHierarchy)
            {
                continue;
            }

            selectedSceneHandbookCardObject = binding.cardObject;
            return;
        }

        selectedSceneHandbookCardObject = null;
    }

    private void RefreshSceneAuthoredHandbookSelection()
    {
        EnsureSelectedSceneAuthoredHandbookCard();

        for (int i = 0; i < sceneHandbookCardBindings.Count; i++)
        {
            SceneAuthoredHandbookCardBinding binding = sceneHandbookCardBindings[i];
            if (binding == null)
            {
                continue;
            }

            if (binding.selectionFrame != null)
            {
                binding.selectionFrame.SetActive(binding.cardObject == selectedSceneHandbookCardObject);
            }

            RefreshSceneAuthoredSelectionPointer(binding);
        }
    }

    private void RefreshSceneAuthoredSelectionPointer(SceneAuthoredHandbookCardBinding binding)
    {
        if (binding?.selectionPointer == null)
        {
            return;
        }

        bool selected = binding.cardObject == selectedSceneHandbookCardObject;
        binding.selectionPointer.gameObject.SetActive(selected);
        if (!selected)
        {
            binding.selectionPointer.anchoredPosition = binding.selectionPointerBasePosition;
        }
    }

    private void ResetSceneAuthoredSelectionPointers()
    {
        for (int i = 0; i < sceneHandbookCardBindings.Count; i++)
        {
            SceneAuthoredHandbookCardBinding binding = sceneHandbookCardBindings[i];
            if (binding?.selectionPointer == null)
            {
                continue;
            }

            binding.selectionPointer.anchoredPosition = binding.selectionPointerBasePosition;
        }
    }

    private void UpdateSceneAuthoredSelectionPointerAnimation(float deltaTime)
    {
        if (!usesSceneAuthoredPages || sceneHandbookCardBindings.Count == 0)
        {
            return;
        }

        SceneAuthoredHandbookCardBinding binding = ResolveSelectedSceneAuthoredHandbookBinding();
        if (binding?.selectionPointer == null ||
            binding.cardObject == null ||
            !binding.cardObject.activeInHierarchy)
        {
            return;
        }

        sceneHandbookPointerTime += Mathf.Max(0f, deltaTime);
        float normalizedTime = Mathf.PingPong(
            sceneHandbookPointerTime / SceneAuthoredHandbookPointerCycleDuration,
            1f);
        float curvedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
        float offsetX = Mathf.Lerp(
            -SceneAuthoredHandbookPointerTravelX,
            SceneAuthoredHandbookPointerTravelX,
            curvedTime);

        binding.selectionPointer.anchoredPosition =
            binding.selectionPointerBasePosition + new Vector2(offsetX, 0f);
    }

    private void RefreshSceneAuthoredRightIntroduction()
    {
        if (!usesSceneAuthoredPages || illustratedHandbookCanvas == null)
        {
            return;
        }

        SceneAuthoredHandbookCardBinding binding = ResolveSelectedSceneAuthoredHandbookBinding();
        if (binding == null)
        {
            return;
        }

        Transform rightRoot = FindTransformByName(illustratedHandbookCanvas.transform, SceneAuthoredRightIntroductionName);
        if (rightRoot == null)
        {
            return;
        }

        BuildingDefinition definition = BuildingDefinitionLibrary.Get(binding.buildingId);
        TMP_Text titleText = FindTmpText(rightRoot, "Name");
        if (titleText != null)
        {
            titleText.text = ResolveSceneAuthoredRightTitle(binding, definition);
        }

        Transform buildingImageRoot = FindTransformByName(rightRoot, SceneAuthoredBuildingImageName);
        Image buildingImage = buildingImageRoot != null ? buildingImageRoot.GetComponent<Image>() : null;
        Sprite previewSprite = ResolveSceneAuthoredRightSprite(binding);
        if (buildingImage != null)
        {
            buildingImage.sprite = previewSprite;
            buildingImage.enabled = previewSprite != null;
            buildingImage.preserveAspect = true;
            BindSceneAuthoredBuildingDetailButton(buildingImage.gameObject, buildingImage, binding, definition, previewSprite);
        }

        Button detailButton = FindSceneAuthoredDetailButton(
            rightRoot,
            false,
            SceneAuthoredBuildingDetailButtonName,
            "Detail",
            "详细",
            "详情",
            "查看");
        Transform detailButtonRoot = detailButton != null
            ? detailButton.transform
            : FindTransformByName(rightRoot, SceneAuthoredBuildingDetailButtonName);
        if (detailButtonRoot != null)
        {
            BindSceneAuthoredBuildingDetailButton(
                detailButtonRoot.gameObject,
                detailButtonRoot.GetComponent<Graphic>(),
                binding,
                definition,
                previewSprite);
        }

        TMP_Text introductionText = buildingImageRoot != null
            ? buildingImageRoot.GetComponentInChildren<TMP_Text>(true)
            : null;
        if (introductionText != null)
        {
            introductionText.text = ResolveSceneAuthoredRightDescription(binding, definition);
            introductionText.color = BodyColor;
            introductionText.fontSize = Mathf.Min(introductionText.fontSize, 20f);
            introductionText.enableWordWrapping = true;
            introductionText.overflowMode = TextOverflowModes.Ellipsis;
            introductionText.alignment = TextAlignmentOptions.TopLeft;
            introductionText.raycastTarget = false;
        }

        RefreshSceneAuthoredRightProgress(binding.buildingId, rightRoot, definition);
    }

    private SceneAuthoredHandbookCardBinding ResolveSelectedSceneAuthoredHandbookBinding()
    {
        EnsureSelectedSceneAuthoredHandbookCard();
        if (selectedSceneHandbookCardObject == null)
        {
            return null;
        }

        for (int i = 0; i < sceneHandbookCardBindings.Count; i++)
        {
            SceneAuthoredHandbookCardBinding binding = sceneHandbookCardBindings[i];
            if (binding?.cardObject == selectedSceneHandbookCardObject)
            {
                return binding;
            }
        }

        return null;
    }

    private static string ResolveSceneAuthoredRightTitle(
        SceneAuthoredHandbookCardBinding binding,
        BuildingDefinition definition)
    {
        if (binding?.detailData != null && !string.IsNullOrWhiteSpace(binding.detailData.buildingName))
        {
            return binding.detailData.buildingName;
        }

        if (binding?.titleText != null && !string.IsNullOrWhiteSpace(binding.titleText.text))
        {
            return binding.titleText.text;
        }

        if (!string.IsNullOrWhiteSpace(definition.detailTitle))
        {
            return definition.detailTitle;
        }

        return definition.displayName;
    }

    private static Sprite ResolveSceneAuthoredRightSprite(SceneAuthoredHandbookCardBinding binding)
    {
        if (binding?.detailData != null && binding.detailData.detailSprite1 != null)
        {
            return binding.detailData.detailSprite1;
        }

        return binding?.previewImage != null ? binding.previewImage.sprite : null;
    }

    private static string ResolveSceneAuthoredRightDescription(
        SceneAuthoredHandbookCardBinding binding,
        BuildingDefinition definition)
    {
        if (binding?.detailData != null && !string.IsNullOrWhiteSpace(binding.detailData.introduction1))
        {
            return binding.detailData.introduction1;
        }

        if (!string.IsNullOrWhiteSpace(definition.detailDescription))
        {
            return definition.detailDescription;
        }

        return binding?.descriptionText != null ? binding.descriptionText.text : string.Empty;
    }

    private void BindSceneAuthoredBuildingDetailButton(
        GameObject targetObject,
        Graphic targetGraphic,
        SceneAuthoredHandbookCardBinding binding,
        BuildingDefinition definition,
        Sprite previewSprite)
    {
        if (targetObject == null || binding == null || definition == null)
        {
            return;
        }

        if (targetGraphic != null)
        {
            targetGraphic.raycastTarget = true;
        }

        Button button = targetObject.GetComponent<Button>();
        if (button == null)
        {
            button = targetObject.AddComponent<Button>();
        }

        if (button.targetGraphic == null && targetGraphic != null)
        {
            button.targetGraphic = targetGraphic;
        }

        button.interactable = RuntimeProgressState.EnsureInstance().IsBuildingUnlocked(binding.buildingId);

        SceneHandbookBuildingDetailButtonHandler handler =
            targetObject.GetComponent<SceneHandbookBuildingDetailButtonHandler>();
        if (handler == null)
        {
            handler = targetObject.AddComponent<SceneHandbookBuildingDetailButtonHandler>();
        }

        handler.Bind(this, binding.buildingId, binding.detailData, definition, previewSprite);
    }

    private void RefreshSceneAuthoredRightProgress(
        CatalogueBuildingId buildingId,
        Transform rightRoot,
        BuildingDefinition definition)
    {
        if (rightRoot == null || definition == null)
        {
            return;
        }

        RuntimeProgressState runtimeState = RuntimeProgressState.Instance;
        int slotCount = definition.slotDefinitions != null ? definition.slotDefinitions.Length : 0;
        int unlockedSlotCount = runtimeState != null ? runtimeState.GetUnlockedSlotCount(buildingId) : 0;

        TMP_Text specialProgressText = FindTmpTextContaining(rightRoot, "专用进度");
        if (specialProgressText != null)
        {
            specialProgressText.text = $"专用进度（{unlockedSlotCount}/{slotCount}）";
        }

        Transform proprietaryRoot = FindTransformByName(rightRoot, SceneAuthoredProprietaryMaterialName);
        if (proprietaryRoot != null)
        {
            List<Transform> slotRoots = CollectSceneHandbookProprietarySlotRoots(proprietaryRoot);
            ArrangeSceneHandbookProprietarySlots(slotRoots, slotCount);
            for (int i = 0; i < slotRoots.Count && i < slotCount; i++)
            {
                BuildingSlotDefinition slotDefinition = definition.slotDefinitions[i];
                bool unlocked = runtimeState != null && runtimeState.IsSlotUnlocked(buildingId, i);
                ApplySceneHandbookProprietarySlotContent(slotRoots[i], slotDefinition);
                BindSceneHandbookProprietaryDropTarget(slotRoots[i], buildingId, i);
                ApplySceneHandbookProprietarySlotVisual(slotRoots[i], unlocked);
            }
        }

        Transform generalRoot = FindTransformByName(rightRoot, SceneAuthoredGeneralMaterialName);
        SanitizeSceneHandbookGeneralMaterialRaycasts(generalRoot);
        EnsureSceneHandbookProprietaryRootAboveGeneral(proprietaryRoot, generalRoot);
        BindSceneHandbookSubmitCommonButton(generalRoot, buildingId);
        Slider generalSlider = generalRoot != null ? generalRoot.GetComponentInChildren<Slider>(true) : null;
        if (generalSlider == null)
        {
            RefreshSceneAuthoredHandbookBackpack(rightRoot);
            return;
        }

        int progress = runtimeState != null ? runtimeState.GetBuildingProgress(buildingId) : 0;
        generalSlider.minValue = 0f;
        generalSlider.maxValue = Mathf.Max(1, definition.requiredProgress);
        generalSlider.wholeNumbers = true;
        generalSlider.SetValueWithoutNotify(progress);
        ConfigureSliderReadOnlyDisplay(generalSlider);
        RefreshSceneAuthoredHandbookBackpack(rightRoot);
    }

    private static void SanitizeSceneHandbookGeneralMaterialRaycasts(Transform generalRoot)
    {
        if (generalRoot == null)
        {
            return;
        }

        Graphic[] graphics = generalRoot.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null)
            {
                continue;
            }

            if (ShouldKeepSceneHandbookGeneralGraphicInteractive(graphic))
            {
                graphic.raycastTarget = true;
                continue;
            }

            graphic.raycastTarget = false;
        }
    }

    private static bool ShouldKeepSceneHandbookGeneralGraphicInteractive(Graphic graphic)
    {
        if (graphic == null ||
            graphic is TMP_Text ||
            graphic is TMP_SubMeshUI)
        {
            return false;
        }

        Button button = graphic.GetComponentInParent<Button>(true);
        return button != null && button.targetGraphic == graphic;
    }

    private static void EnsureSceneHandbookProprietaryRootAboveGeneral(
        Transform proprietaryRoot,
        Transform generalRoot)
    {
        if (proprietaryRoot == null ||
            generalRoot == null ||
            proprietaryRoot.parent == null ||
            proprietaryRoot.parent != generalRoot.parent ||
            proprietaryRoot.GetSiblingIndex() > generalRoot.GetSiblingIndex())
        {
            return;
        }

        proprietaryRoot.SetSiblingIndex(generalRoot.GetSiblingIndex());
    }

    private static List<Transform> CollectSceneHandbookProprietarySlotRoots(Transform proprietaryRoot)
    {
        List<Transform> slotRoots = new List<Transform>();
        if (proprietaryRoot == null)
        {
            return slotRoots;
        }

        CollectNamedSceneHandbookProprietarySlotRoots(proprietaryRoot, slotRoots, false);
        if (slotRoots.Count == 0)
        {
            CollectNamedSceneHandbookProprietarySlotRoots(proprietaryRoot, slotRoots, true);
        }

        if (slotRoots.Count == 0)
        {
            for (int i = 0; i < proprietaryRoot.childCount; i++)
            {
                Transform child = proprietaryRoot.GetChild(i);
                if (IsSceneHandbookProprietarySlotRoot(child))
                {
                    AddSceneHandbookSlotRoot(slotRoots, child);
                }
            }

            Button[] slotButtons = proprietaryRoot.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < slotButtons.Length; i++)
            {
                if (slotButtons[i] != null)
                {
                    AddSceneHandbookSlotRoot(slotRoots, slotButtons[i].transform);
                }
            }
        }

        slotRoots.Sort(CompareSceneHandbookSlotTransforms);
        return slotRoots;
    }

    private static void ArrangeSceneHandbookProprietarySlots(List<Transform> slotRoots, int slotCount)
    {
        if (slotRoots == null || slotRoots.Count == 0)
        {
            return;
        }

        int visibleCount = Mathf.Min(slotRoots.Count, Mathf.Max(0, slotCount));
        if (visibleCount <= 0)
        {
            return;
        }

        float centerX = ResolveSceneHandbookProprietarySlotCenterX(slotRoots, visibleCount);
        float startX = centerX - SceneHandbookProprietarySlotSpacing * (visibleCount - 1) * 0.5f;
        for (int i = 0; i < slotRoots.Count; i++)
        {
            RectTransform slotRect = slotRoots[i] as RectTransform;
            if (slotRect == null)
            {
                continue;
            }

            bool visible = i < visibleCount;
            slotRect.gameObject.SetActive(visible);
            if (!visible)
            {
                continue;
            }

            Vector2 slotSize = ResolveRectSize(slotRect);
            if (slotSize.x <= 1f || slotSize.y <= 1f)
            {
                slotSize = new Vector2(35f, 35f);
            }

            ConfigureAnchoredRect(
                slotRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                slotSize,
                new Vector2(startX + i * SceneHandbookProprietarySlotSpacing, slotRect.anchoredPosition.y),
                new Vector2(0.5f, 0.5f));
            slotRect.localScale = Vector3.one;
            slotRect.localRotation = Quaternion.identity;
        }
    }

    private static float ResolveSceneHandbookProprietarySlotCenterX(List<Transform> slotRoots, int visibleCount)
    {
        if (slotRoots == null || visibleCount <= 0)
        {
            return 0f;
        }

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        for (int i = 0; i < slotRoots.Count && i < visibleCount; i++)
        {
            RectTransform slotRect = slotRoots[i] as RectTransform;
            if (slotRect == null)
            {
                continue;
            }

            minX = Mathf.Min(minX, slotRect.anchoredPosition.x);
            maxX = Mathf.Max(maxX, slotRect.anchoredPosition.x);
        }

        if (float.IsInfinity(minX) || float.IsInfinity(maxX))
        {
            return 0f;
        }

        return (minX + maxX) * 0.5f;
    }

    private static void CollectNamedSceneHandbookProprietarySlotRoots(
        Transform root,
        List<Transform> slotRoots,
        bool includeButtonNames)
    {
        if (root == null || slotRoots == null)
        {
            return;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (IsNamedSceneHandbookProprietarySlotRoot(child, includeButtonNames))
            {
                AddSceneHandbookSlotRoot(slotRoots, child);
            }

            CollectNamedSceneHandbookProprietarySlotRoots(child, slotRoots, includeButtonNames);
        }
    }

    private static void AddSceneHandbookSlotRoot(List<Transform> slotRoots, Transform candidate)
    {
        if (slotRoots == null || candidate == null)
        {
            return;
        }

        if (!TryGetSceneHandbookSlotOrder(candidate, out int candidateOrder))
        {
            candidateOrder = int.MinValue;
        }

        for (int i = slotRoots.Count - 1; i >= 0; i--)
        {
            Transform existing = slotRoots[i];
            if (existing == null)
            {
                slotRoots.RemoveAt(i);
                continue;
            }

            if (!TryGetSceneHandbookSlotOrder(existing, out int existingOrder))
            {
                existingOrder = int.MinValue;
            }

            if (existing == candidate)
            {
                return;
            }

            if (candidateOrder == existingOrder && IsAncestorOf(existing, candidate))
            {
                return;
            }

            if (candidateOrder == existingOrder && IsAncestorOf(candidate, existing))
            {
                slotRoots.RemoveAt(i);
            }
        }

        slotRoots.Add(candidate);
    }

    private static bool IsSceneHandbookProprietarySlotRoot(Transform candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        if (candidate.GetComponent<Button>() != null)
        {
            return true;
        }

        string candidateName = candidate.name;
        if (!string.IsNullOrEmpty(candidateName) &&
            (candidateName.StartsWith("Material_", StringComparison.OrdinalIgnoreCase) ||
             candidateName.StartsWith("ProprietarySlot_", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return candidate.GetComponentInChildren<Button>(true) != null;
    }

    private static bool IsNamedSceneHandbookProprietarySlotRoot(Transform candidate, bool includeButtonNames)
    {
        if (candidate == null || string.IsNullOrEmpty(candidate.name))
        {
            return false;
        }

        string candidateName = candidate.name;
        return candidateName.StartsWith("Material_", StringComparison.OrdinalIgnoreCase) ||
               candidateName.StartsWith("ProprietarySlot_", StringComparison.OrdinalIgnoreCase) ||
               candidateName.StartsWith("Slot_", StringComparison.OrdinalIgnoreCase) ||
               (includeButtonNames && candidateName.StartsWith("Button_", StringComparison.OrdinalIgnoreCase));
    }

    private static int CompareSceneHandbookSlotTransforms(Transform left, Transform right)
    {
        bool hasLeftOrder = TryGetSceneHandbookSlotOrder(left, out int leftOrder);
        bool hasRightOrder = TryGetSceneHandbookSlotOrder(right, out int rightOrder);
        if (hasLeftOrder && hasRightOrder && leftOrder != rightOrder)
        {
            return leftOrder.CompareTo(rightOrder);
        }

        RectTransform leftRect = left as RectTransform;
        RectTransform rightRect = right as RectTransform;
        if (leftRect != null && rightRect != null)
        {
            int yCompare = -leftRect.anchoredPosition.y.CompareTo(rightRect.anchoredPosition.y);
            if (Mathf.Abs(leftRect.anchoredPosition.y - rightRect.anchoredPosition.y) > 1f && yCompare != 0)
            {
                return yCompare;
            }

            int xCompare = leftRect.anchoredPosition.x.CompareTo(rightRect.anchoredPosition.x);
            if (Mathf.Abs(leftRect.anchoredPosition.x - rightRect.anchoredPosition.x) > 1f && xCompare != 0)
            {
                return xCompare;
            }
        }

        int siblingCompare = (left != null ? left.GetSiblingIndex() : int.MaxValue)
            .CompareTo(right != null ? right.GetSiblingIndex() : int.MaxValue);
        return siblingCompare != 0
            ? siblingCompare
            : string.CompareOrdinal(left != null ? left.name : string.Empty, right != null ? right.name : string.Empty);
    }

    private static bool TryGetSceneHandbookSlotOrder(Transform target, out int order)
    {
        order = 0;
        if (target == null)
        {
            return false;
        }

        string targetName = target.name;
        if (string.IsNullOrWhiteSpace(targetName))
        {
            return false;
        }

        int underscoreIndex = targetName.LastIndexOf('_');
        if (underscoreIndex < 0 || underscoreIndex >= targetName.Length - 1)
        {
            return false;
        }

        return int.TryParse(
            targetName.Substring(underscoreIndex + 1),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out order);
    }

    private static bool IsAncestorOf(Transform ancestor, Transform descendant)
    {
        if (ancestor == null || descendant == null)
        {
            return false;
        }

        Transform current = descendant.parent;
        while (current != null)
        {
            if (current == ancestor)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static void ApplySceneHandbookProprietarySlotContent(
        Transform slotRoot,
        BuildingSlotDefinition slotDefinition)
    {
        if (slotRoot == null || slotDefinition == null)
        {
            return;
        }

        Image contentImage = ResolveSceneHandbookProprietarySlotContentImage(slotRoot);
        Sprite slotSprite = LoadSceneHandbookSlotSprite(slotDefinition.iconAssetPath);
        if (contentImage != null && slotSprite != null)
        {
            NormalizeSceneHandbookProprietarySlotSurface(slotRoot, contentImage);
            DisableSceneHandbookExtraProprietaryContentImages(slotRoot, contentImage);
            contentImage.sprite = slotSprite;
            contentImage.enabled = true;
            contentImage.preserveAspect = true;
            contentImage.color = Color.white;
            contentImage.raycastTarget = false;
            FitSceneHandbookProprietarySlotContent(contentImage.rectTransform, slotRoot as RectTransform);
        }

        TMP_Text[] slotTexts = slotRoot.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < slotTexts.Length; i++)
        {
            TMP_Text slotText = slotTexts[i];
            if (slotText == null || string.IsNullOrWhiteSpace(slotDefinition.slotName))
            {
                continue;
            }

            slotText.text = slotDefinition.slotName;
            slotText.raycastTarget = false;
        }
    }

    private static Image ResolveSceneHandbookProprietarySlotContentImage(Transform slotRoot)
    {
        if (slotRoot == null)
        {
            return null;
        }

        Transform namedImage = FindDirectChild(slotRoot, "Image");
        Image namedImageComponent = namedImage != null ? namedImage.GetComponent<Image>() : null;
        if (namedImageComponent != null)
        {
            return namedImageComponent;
        }

        Image[] images = slotRoot.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null)
            {
                continue;
            }

            if (image.transform != slotRoot && !IsSceneHandbookSlotHitAreaImage(slotRoot, image))
            {
                return image;
            }
        }

        return slotRoot.GetComponent<Image>();
    }

    private static void DisableSceneHandbookExtraProprietaryContentImages(Transform slotRoot, Image contentImage)
    {
        if (slotRoot == null || contentImage == null)
        {
            return;
        }

        Image[] images = slotRoot.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null ||
                image == contentImage ||
                image.transform == slotRoot ||
                IsSceneHandbookSlotHitAreaImage(slotRoot, image))
            {
                continue;
            }

            image.enabled = false;
            image.raycastTarget = false;
        }
    }

    private static bool IsSceneHandbookSlotHitAreaImage(Transform slotRoot, Image image)
    {
        if (slotRoot == null || image == null)
        {
            return false;
        }

        Button button = image.GetComponent<Button>();
        if (button != null && button.targetGraphic == image)
        {
            return true;
        }

        Button parentButton = image.GetComponentInParent<Button>(true);
        return parentButton != null &&
               parentButton.transform == image.transform &&
               IsAncestorOf(slotRoot, parentButton.transform);
    }

    private static void NormalizeSceneHandbookProprietarySlotSurface(Transform slotRoot, Image contentImage)
    {
        if (slotRoot == null || contentImage == null)
        {
            return;
        }

        Image slotSurface = slotRoot.GetComponent<Image>();
        if (slotSurface == null || slotSurface == contentImage)
        {
            return;
        }

        slotSurface.color = Color.clear;
        slotSurface.raycastTarget = true;
    }

    private static void FitSceneHandbookProprietarySlotContent(RectTransform contentRect, RectTransform slotRect)
    {
        if (contentRect == null || slotRect == null || contentRect == slotRect)
        {
            return;
        }

        Vector2 slotSize = ResolveRectSize(slotRect);
        float targetSize = Mathf.Max(
            1f,
            Mathf.Min(slotSize.x, slotSize.y) - SceneHandbookProprietarySlotContentInset * 2f);

        ConfigureAnchoredRect(
            contentRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(targetSize, targetSize),
            Vector2.zero,
            new Vector2(0.5f, 0.5f));
        contentRect.localScale = Vector3.one;
        contentRect.localRotation = Quaternion.identity;
    }

    private static Sprite LoadSceneHandbookSlotSprite(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return null;
        }

        if (SceneHandbookSlotSpriteCache.TryGetValue(assetPath, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite sprite = RuntimeProjectSpriteLoader.LoadSprite(assetPath, true, SpriteMeshType.FullRect) ??
                        CreateFallbackSceneHandbookSlotSprite(assetPath);
        SceneHandbookSlotSpriteCache[assetPath] = sprite;
        return sprite;
    }

    private static Sprite CreateFallbackSceneHandbookSlotSprite(string assetPath)
    {
        const int size = 48;
        string spriteName = ResolveAssetStem(assetPath);
        int hash = assetPath.GetHashCode() & int.MaxValue;
        Color32 primary = new Color32(
            (byte)(96 + hash % 112),
            (byte)(96 + (hash / 7) % 112),
            (byte)(96 + (hash / 17) % 112),
            255);
        Color32 shadow = new Color32(
            (byte)Mathf.Max(28, primary.r - 64),
            (byte)Mathf.Max(28, primary.g - 64),
            (byte)Mathf.Max(28, primary.b - 64),
            255);

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - center.x);
                float dy = Mathf.Abs(y - center.y);
                float diamond = dx + dy * 1.08f;
                Color color = Color.clear;
                if (diamond <= 19f)
                {
                    color = shadow;
                }

                if (diamond <= 15f)
                {
                    color = primary;
                }

                if (diamond <= 11f && (x + y + hash) % 5 == 0)
                {
                    color = Color.Lerp(primary, Color.white, 0.28f);
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        texture.name = spriteName;
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = spriteName;
        return sprite;
    }

    private static string ResolveAssetStem(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return "SceneHandbookSlot";
        }

        int slashIndex = assetPath.LastIndexOf('/');
        int startIndex = slashIndex >= 0 ? slashIndex + 1 : 0;
        int dotIndex = assetPath.LastIndexOf('.');
        if (dotIndex <= startIndex)
        {
            dotIndex = assetPath.Length;
        }

        return assetPath.Substring(startIndex, dotIndex - startIndex);
    }

    private void BindSceneHandbookProprietaryDropTarget(Transform slotRoot, CatalogueBuildingId buildingId, int slotIndex)
    {
        if (slotRoot == null)
        {
            return;
        }

        BindSceneHandbookProprietaryDropTarget(slotRoot.gameObject, buildingId, slotIndex);

        Image contentImage = ResolveSceneHandbookProprietarySlotContentImage(slotRoot);
        if (contentImage != null && contentImage.transform != slotRoot)
        {
            BindSceneHandbookProprietaryDropTarget(contentImage.gameObject, buildingId, slotIndex, false);
        }

        Button[] buttons = slotRoot.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
            {
                continue;
            }

            EnsureButtonRaycastTarget(buttons[i]);
            BindSceneHandbookProprietaryDropTarget(buttons[i].gameObject, buildingId, slotIndex);
        }

        Graphic[] graphics = slotRoot.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null || !graphic.raycastTarget)
            {
                continue;
            }

            BindSceneHandbookProprietaryDropTarget(graphic.gameObject, buildingId, slotIndex);
        }
    }

    private void BindSceneHandbookProprietaryDropTarget(
        GameObject targetObject,
        CatalogueBuildingId buildingId,
        int slotIndex,
        bool enableRaycastTarget = true)
    {
        if (targetObject == null)
        {
            return;
        }

        Image targetImage = targetObject.GetComponent<Image>();
        if (enableRaycastTarget && targetImage != null)
        {
            targetImage.raycastTarget = true;
        }

        SceneHandbookProprietarySlotDropHandler handler =
            targetObject.GetComponent<SceneHandbookProprietarySlotDropHandler>();
        if (handler == null)
        {
            handler = targetObject.AddComponent<SceneHandbookProprietarySlotDropHandler>();
        }

        handler.Bind(this, buildingId, slotIndex);
    }

    private static void ApplySceneHandbookProprietarySlotVisual(Transform slotRoot, bool unlocked)
    {
        if (slotRoot == null)
        {
            return;
        }

        Image[] images = slotRoot.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || !ShouldTintSceneHandbookProprietarySlotImage(image))
            {
                continue;
            }

            image.color = unlocked ? SceneHandbookSlotUnlockedColor : SceneHandbookSlotLockedColor;
        }
    }

    private static bool ShouldTintSceneHandbookProprietarySlotImage(Image image)
    {
        if (image == null || image.sprite == null)
        {
            return false;
        }

        return image.color.a > 0.01f;
    }

    private void BindSceneHandbookSubmitCommonButton(Transform generalRoot, CatalogueBuildingId buildingId)
    {
        Button submitButton = ResolveSceneHandbookSubmitCommonButton(generalRoot);
        if (submitButton == null)
        {
            return;
        }

        EnsureButtonRaycastTarget(submitButton);
        SceneHandbookCommonSubmitButtonHandler handler =
            submitButton.GetComponent<SceneHandbookCommonSubmitButtonHandler>();
        if (handler == null)
        {
            handler = submitButton.gameObject.AddComponent<SceneHandbookCommonSubmitButtonHandler>();
        }

        handler.Bind(this, submitButton, buildingId);
    }

    private static Button ResolveSceneHandbookSubmitCommonButton(Transform generalRoot)
    {
        if (generalRoot == null)
        {
            return null;
        }

        Transform namedButton = FindTransformByName(generalRoot, SceneHandbookSubmitCommonButtonName);
        Button submitButton = namedButton != null ? namedButton.GetComponent<Button>() : null;
        if (submitButton != null)
        {
            return submitButton;
        }

        Button[] buttons = generalRoot.GetComponentsInChildren<Button>(true);
        Button fallback = null;
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            fallback = button;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null && label.text != null && label.text.Contains("提交"))
            {
                return button;
            }
        }

        return fallback;
    }

    public bool TryDropSpecialMaterialOnProprietarySlot(CatalogueBuildingId buildingId, int slotIndex, int sourceSlotIndex)
    {
        RuntimeProgressState runtimeState = RuntimeProgressState.EnsureInstance();
        BackpackMananger backpack = ResolveRuntimeBackpackManager();
        BuildingDefinition definition = BuildingDefinitionLibrary.Get(buildingId);
        int slotCount = definition.slotDefinitions != null ? definition.slotDefinitions.Length : 0;
        if (backpack == null ||
            slotIndex < 0 ||
            slotIndex >= slotCount ||
            runtimeState.IsSlotUnlocked(buildingId, slotIndex) ||
            !backpack.TryConsumeSpecialStructureMaterial(sourceSlotIndex))
        {
            RefreshSceneAuthoredRightIntroduction();
            RefreshSceneAuthoredBackpackSurfaces();
            RuntimeSubtitleFeedHud.PushMessage("需要从背包拖动专用结构，才能点亮槽位。");
            return false;
        }

        bool success = runtimeState.TryUnlockSlot(
            buildingId,
            slotIndex,
            out BuildingRewardDefinition slotReward,
            out BuildingRewardDefinition completionReward);
        if (success && runtimeState.CanUnlockBuilding(buildingId))
        {
            runtimeState.TryUnlockBuilding(buildingId, out completionReward);
        }

        RefreshSceneAuthoredRightIntroduction();
        RefreshSceneAuthoredBackpackSurfaces();
        if (!success)
        {
            RuntimeSubtitleFeedHud.PushMessage("该专用槽位无法点亮。");
            return false;
        }

        string rewardText = slotReward != null && !string.IsNullOrWhiteSpace(slotReward.title)
            ? $"点亮成功：{slotReward.title}"
            : "专用槽位已点亮。";
        if (completionReward != null && !string.IsNullOrWhiteSpace(completionReward.title))
        {
            rewardText += $" {completionReward.title}";
        }

        RuntimeSubtitleFeedHud.PushMessage(rewardText);
        MusicManager.PlaySfx(SfxCueId.ButtonClick);
        return true;
    }

    private bool TryClickProprietarySlot(CatalogueBuildingId buildingId, int slotIndex)
    {
        RuntimeProgressState runtimeState = RuntimeProgressState.EnsureInstance();
        if (runtimeState.IsSlotUnlocked(buildingId, slotIndex))
        {
            return false;
        }

        BackpackMananger backpack = ResolveRuntimeBackpackManager();
        if (backpack != null)
        {
            for (int i = 0; i < SceneHandbookBackpackSlotCount; i++)
            {
                ArchitecturalCrystal? item = backpack.GetItem(i);
                if (item.HasValue && item.Value.IsSpecialStructure)
                {
                    return TryDropSpecialMaterialOnProprietarySlot(buildingId, slotIndex, i);
                }
            }
        }

        RefreshSceneAuthoredRightIntroduction();
        RefreshSceneAuthoredBackpackSurfaces();
        RuntimeSubtitleFeedHud.PushMessage("需要从背包拖动专用结构，才能点亮槽位。");
        return false;
    }

    public bool SubmitCommonMaterialsToBuilding(CatalogueBuildingId buildingId)
    {
        BackpackMananger backpack = ResolveRuntimeBackpackManager();
        CatalogueSubmitCommonStructuresResult result =
            CatalogueSubmissionService.SubmitAllCommonStructures(backpack, buildingId);

        RefreshSceneAuthoredRightIntroduction();
        RefreshSceneAuthoredBackpackSurfaces();
        if (!result.success)
        {
            RuntimeSubtitleFeedHud.PushMessage("背包中没有可提交的通用材料。");
            return false;
        }

        BuildingDefinition definition = BuildingDefinitionLibrary.Get(buildingId);
        string progressLabel = result.appliedProgress == result.requestedProgress ? "进度" : "有效进度";
        RuntimeSubtitleFeedHud.PushMessage(
            $"提交成功：{definition.displayName} {progressLabel} +{result.appliedProgress}");

        if (result.completionReward != null)
        {
            Dialog dialog = Dialog.EnsureTopmostRuntimeInstance();
            if (dialog != null)
            {
                dialog.ShowClickCloseDialog($"{result.completionReward.title}\n{result.completionReward.description}");
            }
        }

        MusicManager.PlaySfx(SfxCueId.ButtonClick);
        return true;
    }

    private static CatalogueBuildingId ResolveCatalogueBuildingId(GameObject cardObject, int zeroBasedIndex)
    {
        CatalogueBuildingUnlockState unlockState = cardObject != null
            ? cardObject.GetComponent<CatalogueBuildingUnlockState>()
            : null;
        if (unlockState != null)
        {
            return unlockState.buildingId;
        }

        if (cardObject != null &&
            TryResolveCatalogueBuildingIdFromTitle(
                ResolveSceneAuthoredCardTitleText(cardObject.transform)?.text,
                out CatalogueBuildingId titleBuildingId))
        {
            return titleBuildingId;
        }

        return (CatalogueBuildingId)Mathf.Clamp(
            zeroBasedIndex,
            0,
            Enum.GetValues(typeof(CatalogueBuildingId)).Length - 1);
    }

    private static bool TryResolveCatalogueBuildingIdFromTitle(string title, out CatalogueBuildingId buildingId)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            if (title.Contains("土楼") || title.Contains("福建"))
            {
                buildingId = CatalogueBuildingId.Building1;
                return true;
            }

            if (title.Contains("赵州") || title.Contains("桥"))
            {
                buildingId = CatalogueBuildingId.Building2;
                return true;
            }

            if (title.Contains("水乡") || title.Contains("民居") || title.Contains("安徽") || title.Contains("苏浙"))
            {
                buildingId = CatalogueBuildingId.Building3;
                return true;
            }
        }

        buildingId = CatalogueBuildingId.Building1;
        return false;
    }

    private static Slider ResolveSceneAuthoredBuildingSlider(Transform cardTransform)
    {
        Transform sliderTransform = FindDirectChild(cardTransform, "Slider");
        return sliderTransform != null
            ? sliderTransform.GetComponent<Slider>()
            : cardTransform.GetComponentInChildren<Slider>(true);
    }

    private static GameObject ResolveSceneAuthoredStatusVisual(Transform cardTransform, string childName)
    {
        Transform child = FindDirectChild(cardTransform, childName);
        return child != null ? child.gameObject : null;
    }

    private static GameObject ResolveSceneAuthoredSelectionFrame(Transform cardTransform)
    {
        RectTransform cardRect = cardTransform as RectTransform;
        if (cardRect == null)
        {
            return null;
        }

        Vector2 cardSize = ResolveRectSize(cardRect);
        for (int i = 0; i < cardTransform.childCount; i++)
        {
            Transform child = cardTransform.GetChild(i);
            Image image = child != null ? child.GetComponent<Image>() : null;
            RectTransform imageRect = child as RectTransform;
            if (image == null || imageRect == null)
            {
                continue;
            }

            Vector2 imageSize = ResolveRectSize(imageRect);
            if (imageSize.x >= cardSize.x * 0.78f && imageSize.y >= cardSize.y * 0.78f)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private static Image ResolveSceneAuthoredCardPreviewImage(Transform cardTransform, GameObject selectionFrame)
    {
        Transform picture = FindDirectChild(cardTransform, "Picture");
        Image pictureImage = picture != null ? picture.GetComponent<Image>() : null;
        if (pictureImage != null)
        {
            return pictureImage;
        }

        RectTransform cardRect = cardTransform as RectTransform;
        Vector2 cardSize = cardRect != null ? ResolveRectSize(cardRect) : Vector2.zero;
        for (int i = 0; i < cardTransform.childCount; i++)
        {
            Transform child = cardTransform.GetChild(i);
            if (child == null || child.gameObject == selectionFrame)
            {
                continue;
            }

            Image image = child.GetComponent<Image>();
            RectTransform imageRect = child as RectTransform;
            if (image == null || imageRect == null)
            {
                continue;
            }

            Vector2 imageSize = ResolveRectSize(imageRect);
            if (imageSize.x >= 20f && imageSize.y >= 20f &&
                (cardSize == Vector2.zero || imageSize.x < cardSize.x * 0.78f))
            {
                return image;
            }
        }

        return null;
    }

    private static RectTransform ResolveSceneAuthoredSelectionPointer(
        Transform cardTransform,
        GameObject selectionFrame,
        Image previewImage)
    {
        RectTransform cardRect = cardTransform as RectTransform;
        if (cardRect == null)
        {
            return null;
        }

        Vector2 cardSize = ResolveRectSize(cardRect);
        RectTransform pointer = null;
        float pointerX = float.MaxValue;
        for (int i = 0; i < cardTransform.childCount; i++)
        {
            Transform child = cardTransform.GetChild(i);
            if (child == null ||
                child.gameObject == selectionFrame ||
                IsSceneAuthoredStatusVisualName(child.name) ||
                (previewImage != null && child.gameObject == previewImage.gameObject))
            {
                continue;
            }

            Image image = child.GetComponent<Image>();
            RectTransform imageRect = child as RectTransform;
            if (image == null || imageRect == null)
            {
                continue;
            }

            Vector2 imageSize = ResolveRectSize(imageRect);
            bool isSmallPointer = imageSize.x <= Mathf.Max(32f, cardSize.x * 0.22f) &&
                                  imageSize.y <= Mathf.Max(32f, cardSize.y * 0.65f);
            if (!isSmallPointer || imageRect.anchoredPosition.x >= pointerX)
            {
                continue;
            }

            pointer = imageRect;
            pointerX = imageRect.anchoredPosition.x;
        }

        return pointer;
    }

    private static TMP_Text ResolveSceneAuthoredCardTitleText(Transform cardTransform)
    {
        Transform title = FindDirectChild(cardTransform, "Name");
        return title != null ? title.GetComponent<TMP_Text>() : null;
    }

    private static TMP_Text ResolveSceneAuthoredCardDescriptionText(Transform cardTransform)
    {
        if (cardTransform == null)
        {
            return null;
        }

        for (int i = 0; i < cardTransform.childCount; i++)
        {
            Transform child = cardTransform.GetChild(i);
            TMP_Text text = child != null ? child.GetComponent<TMP_Text>() : null;
            if (text == null || string.Equals(child.name, "Name", StringComparison.Ordinal))
            {
                continue;
            }

            return text;
        }

        return null;
    }

    private static Vector2 ResolveRectSize(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return Vector2.zero;
        }

        Vector2 size = rectTransform.rect.size;
        if (size.x <= 0.001f || size.y <= 0.001f)
        {
            size = rectTransform.sizeDelta;
        }

        return new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
    }

    private static void HideLooseSceneAuthoredStatusVisuals(Transform leftPanel)
    {
        if (leftPanel == null)
        {
            return;
        }

        for (int i = 0; i < leftPanel.childCount; i++)
        {
            Transform child = leftPanel.GetChild(i);
            if (child == null || !IsSceneAuthoredStatusVisualName(child.name))
            {
                continue;
            }

            child.gameObject.SetActive(false);
        }
    }

    private static void CollectSceneHandbookCards(Transform root, List<RectTransform> cardRects)
    {
        if (root == null || cardRects == null)
        {
            return;
        }

        if (IsLegacyHandbookContentLayer(root))
        {
            AddUniqueCardRect(cardRects, root as RectTransform);
            return;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            CollectSceneHandbookCards(root.GetChild(i), cardRects);
        }
    }

    private static void SortHandbookCardsForSceneLayout(List<RectTransform> cardRects)
    {
        if (cardRects == null)
        {
            return;
        }

        cardRects.Sort((left, right) =>
        {
            int leftOrder = GetHandbookCardOrder(left);
            int rightOrder = GetHandbookCardOrder(right);
            if (leftOrder != rightOrder)
            {
                return leftOrder.CompareTo(rightOrder);
            }

            int leftSibling = left != null ? left.GetSiblingIndex() : int.MaxValue;
            int rightSibling = right != null ? right.GetSiblingIndex() : int.MaxValue;
            return leftSibling.CompareTo(rightSibling);
        });
    }

    private static void EnsureMinimumHandbookCards(List<RectTransform> cardRects, Transform container)
    {
        if (cardRects == null || container == null || cardRects.Count == 0)
        {
            return;
        }

        cardRects.Sort((left, right) => GetHandbookCardOrder(left).CompareTo(GetHandbookCardOrder(right)));
        RectTransform template = cardRects[0];
        for (int i = cardRects.Count; i < HandbookCardTargetCount; i++)
        {
            GameObject cardObject = Instantiate(template.gameObject, container, false);
            cardObject.name = $"{HandbookCardNamePrefix}{i + 1}";

            RectTransform cardRect = cardObject.transform as RectTransform;
            ApplyCatalogueBuildingId(cardObject, i, false);
            AddUniqueCardRect(cardRects, cardRect);
        }
    }

    private static void ApplyCatalogueBuildingId(GameObject cardObject, int zeroBasedIndex, bool preferTitle = true)
    {
        if (cardObject == null)
        {
            return;
        }

        CatalogueBuildingId buildingId = (CatalogueBuildingId)Mathf.Clamp(
            zeroBasedIndex,
            0,
            Enum.GetValues(typeof(CatalogueBuildingId)).Length - 1);
        TMP_Text titleText = ResolveSceneAuthoredCardTitleText(cardObject.transform);
        if (preferTitle &&
            TryResolveCatalogueBuildingIdFromTitle(titleText?.text, out CatalogueBuildingId titleBuildingId))
        {
            buildingId = titleBuildingId;
        }

        CatalogueBuildingUnlockState unlockState = cardObject.GetComponent<CatalogueBuildingUnlockState>();
        if (unlockState == null)
        {
            unlockState = cardObject.AddComponent<CatalogueBuildingUnlockState>();
        }

        BuildingDefinition definition = BuildingDefinitionLibrary.Get(buildingId);
        if (!preferTitle && titleText != null && definition != null)
        {
            titleText.text = definition.displayName;
        }

        if (unlockState != null)
        {
            unlockState.buildingId = buildingId;
        }

        BuildingProgressController progressController = cardObject.GetComponent<BuildingProgressController>();
        if (progressController != null)
        {
            progressController.buildingId = buildingId;
        }
    }

    private static int GetHandbookCardOrder(RectTransform cardRect)
    {
        if (cardRect == null || string.IsNullOrWhiteSpace(cardRect.name))
        {
            return int.MaxValue;
        }

        if (!cardRect.name.StartsWith(HandbookCardNamePrefix, StringComparison.Ordinal))
        {
            return int.MaxValue;
        }

        string suffix = cardRect.name.Substring(HandbookCardNamePrefix.Length);
        return int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out int order)
            ? order
            : int.MaxValue;
    }

    private static void SetGeneratedTextVisibility(RectTransform pageRoot, bool visible)
    {
        if (pageRoot == null)
        {
            return;
        }

        SetChildActive(pageRoot, PageTagName, visible);
        SetChildActive(pageRoot, TitleName, visible);
        SetChildActive(pageRoot, SubtitleName, visible);
        SetChildActive(pageRoot, BodyName, visible);
        SetChildActive(pageRoot, FooterName, visible);
    }

    private static void SetChildActive(Transform parent, string childName, bool active)
    {
        Transform child = FindDirectChild(parent, childName);
        if (child != null)
        {
            child.gameObject.SetActive(active);
        }
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
        if (IsPageAvailable(IllustratedHandbookPage.PersonalInformation))
        {
            CreateTabButton(rail, IllustratedHandbookPage.PersonalInformation, "角色");
        }
        CreateTabButton(rail, IllustratedHandbookPage.PhotoAlbum, "相册");
        CreateTabButton(rail, IllustratedHandbookPage.Setting, "设置");

        if (FindButtonByName(chromeRoot, "CloseButton") == null)
        {
            CreateCloseButton(rail);
        }
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
        EnsureButtonRaycastTarget(button);
        button.transition = Selectable.Transition.None;
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
        EnsureButtonRaycastTarget(button);
        button.transition = Selectable.Transition.None;
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

        if (usesSceneAuthoredPages)
        {
            List<GameObject> sceneRoots = CollectUniqueScenePageRoots();
            for (int i = 0; i < sceneRoots.Count; i++)
            {
                RegisterScenePageButtons(sceneRoots[i]);
            }

            return;
        }

        Transform chromeRoot = GetChromePageRoot() != null ? GetChromePageRoot().transform : null;
        if (chromeRoot == null)
        {
            return;
        }

        foreach (IllustratedHandbookPage page in Enum.GetValues(typeof(IllustratedHandbookPage)))
        {
            if (page == IllustratedHandbookPage.Mission || !IsPageAvailable(page))
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

        if (usesSceneAuthoredPages)
        {
            List<GameObject> sceneRoots = CollectUniqueScenePageRoots();
            for (int i = 0; i < sceneRoots.Count; i++)
            {
                Button sceneCloseButton = BindSceneCloseButton(sceneRoots[i]);
                if (closeButton == null)
                {
                    closeButton = sceneCloseButton;
                }
            }

            return;
        }

        Transform chromeRoot = GetChromePageRoot() != null ? GetChromePageRoot().transform : null;
        if (chromeRoot == null)
        {
            return;
        }

        closeButton = FindButtonByName(chromeRoot, "CloseButton");
    }

    private Button BindSceneCloseButton(GameObject pageRoot)
    {
        if (pageRoot == null)
        {
            return null;
        }

        DisableSceneBookmarkTransparentBlockers(pageRoot.transform);

        Transform bookmarkRoot = FindDirectChild(pageRoot.transform, SceneAuthoredBookmarkRootName) ??
                                 FindTransformByName(pageRoot.transform, SceneAuthoredBookmarkRootName);
        bookmarkRoot?.SetAsLastSibling();

        Transform closeVisualRoot = FindSceneBookmarkVisual(pageRoot.transform, SceneAuthoredCloseButtonName);
        if (closeVisualRoot != null)
        {
            Graphic closeGraphic = closeVisualRoot.GetComponent<Graphic>();
            if (closeGraphic != null)
            {
                closeGraphic.raycastTarget = false;
            }

            TMP_Text[] closeTexts = closeVisualRoot.GetComponentsInChildren<TMP_Text>(true);
            for (int textIndex = 0; textIndex < closeTexts.Length; textIndex++)
            {
                if (closeTexts[textIndex] != null)
                {
                    closeTexts[textIndex].raycastTarget = false;
                }
            }
        }

        Button authoredCloseButton = FindSceneAuthoredBookmarkButton(closeVisualRoot);
        bool isDetailCloseButton = IsSceneAuthoredDetailCanvas(pageRoot);
        if (authoredCloseButton != null)
        {
            ConfigureSceneBookmarkButton(authoredCloseButton);
            BindSceneCloseAction(authoredCloseButton, isDetailCloseButton);
        }

        Button sceneCloseButton = GetOrCreateBookmarkHitAreaButton(closeVisualRoot);
        if (sceneCloseButton == null)
        {
            closeVisualRoot?.SetAsLastSibling();
            return authoredCloseButton;
        }

        ConfigureSceneBookmarkHitArea(sceneCloseButton, closeVisualRoot as RectTransform);
        EnsureButtonRaycastTarget(sceneCloseButton);
        BindSceneCloseAction(sceneCloseButton, isDetailCloseButton);
        closeVisualRoot.SetAsLastSibling();
        return sceneCloseButton;
    }

    private void BindSceneCloseAction(Button button, bool isDetailCloseButton)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(HandleCloseRequested);
        button.onClick.RemoveListener(HandleDetailCloseRequested);
        button.onClick.AddListener(isDetailCloseButton ? HandleDetailCloseRequested : HandleCloseRequested);
    }

    private void RegisterScenePageButtons(GameObject pageRoot)
    {
        if (pageRoot == null)
        {
            return;
        }

        DisableSceneBookmarkTransparentBlockers(pageRoot.transform);

        foreach (IllustratedHandbookPage targetPage in Enum.GetValues(typeof(IllustratedHandbookPage)))
        {
            if (!ShouldBindScenePage(targetPage))
            {
                continue;
            }

            string sceneTabName = GetSceneTabButtonName(targetPage);
            Transform visualRoot = FindSceneBookmarkVisual(pageRoot.transform, sceneTabName);
            ApplySceneBookmarkAvailability(visualRoot, targetPage);
            if (!IsPageAvailable(targetPage))
            {
                continue;
            }

            if (visualRoot != null)
            {
                RectTransform visualRect = visualRoot as RectTransform;
                if (visualRect != null)
                {
                    NormalizeSceneBookmarkVisualRect(visualRect);
                    if (!sceneBookmarkBasePositions.ContainsKey(visualRect))
                    {
                        sceneBookmarkBasePositions[visualRect] = visualRect.anchoredPosition;
                    }
                }

                if (!sceneBookmarkBaseSiblingIndices.ContainsKey(visualRoot))
                {
                    sceneBookmarkBaseSiblingIndices[visualRoot] = visualRoot.GetSiblingIndex();
                }

                Graphic visualGraphic = visualRoot.GetComponent<Graphic>();
                if (visualGraphic != null)
                {
                    visualGraphic.raycastTarget = false;
                }

                TMP_Text[] tabTexts = visualRoot.GetComponentsInChildren<TMP_Text>(true);
                for (int textIndex = 0; textIndex < tabTexts.Length; textIndex++)
                {
                    if (tabTexts[textIndex] != null)
                    {
                        tabTexts[textIndex].raycastTarget = false;
                    }
                }
            }

            BindSceneBookmarkButton(FindSceneAuthoredBookmarkButton(visualRoot), targetPage);

            Button button = GetOrCreateBookmarkHitAreaButton(visualRoot);
            if (button == null)
            {
                continue;
            }

            ConfigureSceneBookmarkHitArea(button, visualRoot as RectTransform);
            EnsureButtonRaycastTarget(button);
            BindSceneBookmarkButton(button, targetPage);

            if (!tabButtons.TryGetValue(targetPage, out List<Button> buttons))
            {
                buttons = new List<Button>();
                tabButtons[targetPage] = buttons;
            }

            buttons.Add(button);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                buttonTexts[button] = label;
            }
        }
    }

    private static string GetSceneTabButtonName(IllustratedHandbookPage page)
    {
        switch (page)
        {
            case IllustratedHandbookPage.IllustratedHandbook:
                return "HandBook";
            case IllustratedHandbookPage.PersonalInformation:
                return "PersonalInformation";
            case IllustratedHandbookPage.PhotoAlbum:
                return "PhotoAlbum";
            case IllustratedHandbookPage.Mission:
                return "Mission";
            case IllustratedHandbookPage.Setting:
                return SceneAuthoredSettingTabName;
            default:
                return string.Empty;
        }
    }

    private void HandleBookmarkClicked(IllustratedHandbookPage targetPage)
    {
        MusicManager.PlaySfx(SfxCueId.HandbookBookmark);
        SwitchToPage(targetPage);
    }

    private void BindSceneBookmarkButton(Button button, IllustratedHandbookPage targetPage)
    {
        if (button == null)
        {
            return;
        }

        ConfigureSceneBookmarkButton(button);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => HandleBookmarkClicked(targetPage));
    }

    private void RefreshScenePageAvailability()
    {
        if (personalInformationCanvas != null && !personalInformationPageAvailable)
        {
            personalInformationCanvas.SetActive(false);
        }

        if (usesSceneAuthoredPages)
        {
            List<GameObject> sceneRoots = CollectUniqueScenePageRoots();
            for (int i = 0; i < sceneRoots.Count; i++)
            {
                GameObject pageRoot = sceneRoots[i];
                if (pageRoot == null)
                {
                    continue;
                }

                foreach (IllustratedHandbookPage page in Enum.GetValues(typeof(IllustratedHandbookPage)))
                {
                    if (!ShouldBindScenePage(page))
                    {
                        continue;
                    }

                    ApplySceneBookmarkAvailability(FindSceneBookmarkVisual(pageRoot.transform, page), page);
                }
            }
        }

        foreach (KeyValuePair<IllustratedHandbookPage, List<Button>> entry in tabButtons)
        {
            bool available = IsPageAvailable(entry.Key);
            for (int i = 0; i < entry.Value.Count; i++)
            {
                if (entry.Value[i] != null)
                {
                    entry.Value[i].gameObject.SetActive(available);
                }
            }
        }
    }

    private void ApplySceneBookmarkAvailability(Transform visualRoot, IllustratedHandbookPage page)
    {
        if (visualRoot == null)
        {
            return;
        }

        bool available = IsPageAvailable(page);
        visualRoot.gameObject.SetActive(available);

        Button authoredButton = FindSceneAuthoredBookmarkButton(visualRoot);
        if (authoredButton != null)
        {
            authoredButton.interactable = available;
            if (!available)
            {
                authoredButton.onClick.RemoveAllListeners();
            }
        }

        Transform hitArea = FindDirectChild(visualRoot, SceneBookmarkHitAreaName);
        Button hitAreaButton = hitArea != null ? hitArea.GetComponent<Button>() : null;
        if (hitAreaButton != null)
        {
            hitAreaButton.interactable = available;
            if (!available)
            {
                hitAreaButton.onClick.RemoveAllListeners();
            }
        }
    }

    private void UpdateSceneAuthoredBookmarkState(IllustratedHandbookPage activePage)
    {
        if (!usesSceneAuthoredPages)
        {
            return;
        }

        List<GameObject> sceneRoots = CollectUniqueScenePageRoots();
        for (int i = 0; i < sceneRoots.Count; i++)
        {
            GameObject pageRoot = sceneRoots[i];
            if (pageRoot == null)
            {
                continue;
            }

            if (pageRoot.activeInHierarchy)
            {
                UpdateSceneBookmarkSiblingState(pageRoot.transform, activePage);
            }

            UpdateSceneBookmarkVisualState(pageRoot.transform, activePage);
        }
    }

    private void UpdateSceneBookmarkVisualState(Transform pageRoot, IllustratedHandbookPage activePage)
    {
        if (pageRoot == null)
        {
            return;
        }

        foreach (IllustratedHandbookPage page in Enum.GetValues(typeof(IllustratedHandbookPage)))
        {
            if (!ShouldBindScenePage(page))
            {
                continue;
            }

            Transform visualRoot = FindSceneBookmarkVisual(pageRoot, page);
            ApplySceneBookmarkAvailability(visualRoot, page);
            if (!IsPageAvailable(page))
            {
                continue;
            }

            RectTransform visualRect = visualRoot as RectTransform;
            if (visualRect == null)
            {
                continue;
            }

            NormalizeSceneBookmarkVisualRect(visualRect);
            if (!sceneBookmarkBasePositions.TryGetValue(visualRect, out Vector2 basePosition))
            {
                basePosition = visualRect.anchoredPosition;
                sceneBookmarkBasePositions[visualRect] = basePosition;
            }

            bool selected = page == activePage;
            Vector2 targetPosition = basePosition + new Vector2(selected ? SceneAuthoredBookmarkSelectedOffsetX : 0f, 0f);
            Graphic visualGraphic = visualRoot.GetComponent<Graphic>();
            TMP_Text[] labels = visualRoot.GetComponentsInChildren<TMP_Text>(true);
            Color targetTint = selected ? Color.white : new Color(1f, 1f, 1f, 0.96f);
            if (!isActiveAndEnabled || !visualRoot.gameObject.activeInHierarchy)
            {
                ApplySceneBookmarkVisualState(visualRect, visualGraphic, labels, targetPosition, targetTint, SceneAuthoredBookmarkScale);
                continue;
            }

            if (sceneBookmarkAnimations.TryGetValue(visualRect, out Coroutine existingAnimation) && existingAnimation != null)
            {
                StopCoroutine(existingAnimation);
            }

            sceneBookmarkAnimations[visualRect] = StartCoroutine(AnimateSceneBookmarkState(visualRect, visualGraphic, labels, targetPosition, targetTint));
        }
    }

    private void UpdateSceneBookmarkSiblingState(Transform chromeRoot, IllustratedHandbookPage activePage)
    {
        if (chromeRoot == null)
        {
            return;
        }

        List<KeyValuePair<IllustratedHandbookPage, int>> restoreOrder = new List<KeyValuePair<IllustratedHandbookPage, int>>();
        Transform selectedRoot = null;

        foreach (IllustratedHandbookPage page in Enum.GetValues(typeof(IllustratedHandbookPage)))
        {
            if (!ShouldBindScenePage(page))
            {
                continue;
            }

            if (!IsPageAvailable(page))
            {
                continue;
            }

            Transform visualRoot = FindSceneBookmarkVisual(chromeRoot, page);
            if (visualRoot == null)
            {
                continue;
            }

            if (page == activePage)
            {
                selectedRoot = visualRoot;
                continue;
            }

            int siblingIndex = sceneBookmarkBaseSiblingIndices.TryGetValue(visualRoot, out int storedIndex)
                ? storedIndex
                : visualRoot.GetSiblingIndex();
            restoreOrder.Add(new KeyValuePair<IllustratedHandbookPage, int>(page, siblingIndex));
        }

        restoreOrder.Sort((left, right) => left.Value.CompareTo(right.Value));
        for (int i = 0; i < restoreOrder.Count; i++)
        {
            Transform visualRoot = FindSceneBookmarkVisual(chromeRoot, restoreOrder[i].Key);
            if (visualRoot == null || visualRoot.parent == null)
            {
                continue;
            }

            int clampedIndex = Mathf.Clamp(restoreOrder[i].Value, 0, visualRoot.parent.childCount - 1);
            visualRoot.SetSiblingIndex(clampedIndex);
        }

        if (selectedRoot != null)
        {
            selectedRoot.SetAsLastSibling();
        }

        BringSceneBookmarkRootsToFront(chromeRoot, activePage);
    }

    private static void BringSceneBookmarkRootsToFront(Transform chromeRoot, IllustratedHandbookPage activePage)
    {
        if (chromeRoot == null)
        {
            return;
        }

        foreach (IllustratedHandbookPage page in Enum.GetValues(typeof(IllustratedHandbookPage)))
        {
            if (page == activePage || !IsSceneBookmarkVisualRoot(page))
            {
                continue;
            }

            Transform visualRoot = FindSceneBookmarkVisual(chromeRoot, page);
            if (visualRoot != null)
            {
                visualRoot.SetAsLastSibling();
            }
        }

        Transform selectedRoot = FindSceneBookmarkVisual(chromeRoot, activePage);
        if (selectedRoot != null)
        {
            selectedRoot.SetAsLastSibling();
        }

        Transform closeRoot = FindSceneBookmarkVisual(chromeRoot, SceneAuthoredCloseButtonName);
        if (closeRoot != null)
        {
            closeRoot.SetAsLastSibling();
        }
    }

    private static bool IsSceneBookmarkVisualRoot(IllustratedHandbookPage page)
    {
        return ShouldBindScenePage(page) || page == IllustratedHandbookPage.Setting;
    }

    private IEnumerator AnimateSceneBookmarkState(
        RectTransform visualRect,
        Graphic visualGraphic,
        TMP_Text[] labels,
        Vector2 targetPosition,
        Color targetTint)
    {
        Vector2 startPosition = visualRect != null ? visualRect.anchoredPosition : Vector2.zero;
        Vector3 startScale = visualRect != null ? visualRect.localScale : Vector3.one;
        Vector3 targetScale = SceneAuthoredBookmarkScale;
        Color startTint = visualGraphic != null ? visualGraphic.color : targetTint;
        float duration = BookmarkAnimationDuration;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (visualRect == null)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            Vector2 currentPosition = Vector2.LerpUnclamped(startPosition, targetPosition, eased);
            Vector3 currentScale = Vector3.LerpUnclamped(startScale, targetScale, eased);
            Color currentTint = Color.LerpUnclamped(startTint, targetTint, eased);
            ApplySceneBookmarkVisualState(visualRect, visualGraphic, labels, currentPosition, currentTint, currentScale);
            yield return null;
        }

        ApplySceneBookmarkVisualState(visualRect, visualGraphic, labels, targetPosition, targetTint, targetScale);
        sceneBookmarkAnimations.Remove(visualRect);
    }

    private static void ApplySceneBookmarkVisualState(
        RectTransform visualRect,
        Graphic visualGraphic,
        TMP_Text[] labels,
        Vector2 anchoredPosition,
        Color tint,
        Vector3 localScale)
    {
        if (visualRect != null)
        {
            visualRect.anchoredPosition = anchoredPosition;
            visualRect.localScale = localScale;
        }

        if (visualGraphic != null)
        {
            visualGraphic.color = tint;
        }

        if (labels == null)
        {
            return;
        }

        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] != null)
            {
                labels[i].color = Color.white;
            }
        }
    }

    private void HandleCloseRequested()
    {
        UIManager targetManager = owner;
        if (targetManager == null)
        {
            if (!IllustratedUISceneLoader.TryGetUIManager(out targetManager))
            {
                targetManager = FindObjectOfType<UIManager>(true);
            }
        }

        if (targetManager != null)
        {
            targetManager.CloseIllustratedHandbook();
            return;
        }

        gameObject.SetActive(false);
    }

    private void HandleDetailCloseRequested()
    {
        DetailedInformationUI visibleDetailUi = ResolveVisibleDetailUi();
        if (visibleDetailUi != null)
        {
            visibleDetailUi.CloseDetailOnlyReturnHandbook();
            return;
        }

        HandleCloseRequested();
    }

    private DetailedInformationUI ResolveVisibleDetailUi()
    {
        DetailedInformationUI detailUi = GetComponent<DetailedInformationUI>();
        if (detailUi != null)
        {
            return detailUi;
        }

        return GetComponentInChildren<DetailedInformationUI>(true);
    }

    private static bool IsSceneAuthoredDetailCanvas(GameObject pageRoot)
    {
        return pageRoot != null &&
               (string.Equals(pageRoot.name, SceneAuthoredFujianDetailCanvasName, StringComparison.Ordinal) ||
                string.Equals(pageRoot.name, SceneAuthoredShuiXiangDetailCanvasName, StringComparison.Ordinal) ||
                pageRoot.GetComponent<DetailedInformationUI>() != null);
    }

    private void ActivateChromeRoot()
    {
        GameObject chromeRoot = usesSceneAuthoredPages ? gameObject : GetChromePageRoot();
        if (chromeRoot != null && !chromeRoot.activeSelf)
        {
            chromeRoot.SetActive(true);
        }
    }

    private void SetActiveGeneratedPage(IllustratedHandbookPage activePage)
    {
        bool showGeneratedContent = true;
        if (bookContentPanel != null)
        {
            bookContentPanel.gameObject.SetActive(showGeneratedContent);
        }

        foreach (KeyValuePair<IllustratedHandbookPage, RectTransform> entry in pageContentRoots)
        {
            if (entry.Value != null)
            {
                entry.Value.gameObject.SetActive(showGeneratedContent && entry.Key == activePage);
            }
        }

        UpdateLegacyHandbookContentVisibility(activePage);
    }

    private void UpdateLegacyHandbookContentVisibility(IllustratedHandbookPage activePage)
    {
        for (int i = 0; i < legacyHandbookContentObjects.Count; i++)
        {
            GameObject contentObject = legacyHandbookContentObjects[i];
            if (contentObject == null)
            {
                continue;
            }

            if (string.Equals(contentObject.name, "CloseButton", StringComparison.Ordinal))
            {
                contentObject.SetActive(true);
                continue;
            }

            contentObject.SetActive(activePage == IllustratedHandbookPage.IllustratedHandbook);
        }
    }

    private static bool IsLegacyHandbookContentLayer(Transform transform)
    {
        if (transform == null)
        {
            return false;
        }

        if (string.Equals(transform.name, LegacyFujianTulouButtonName, StringComparison.Ordinal) ||
            transform.name.StartsWith(HandbookCardNamePrefix, StringComparison.Ordinal) ||
            IsSceneAuthoredHandbookCard(transform))
        {
            return true;
        }

        return transform.GetComponent<CatalogueBuildingUnlockState>() != null ||
               transform.GetComponent<BuildingProgressController>() != null;
    }

    private static bool IsSceneAuthoredHandbookCard(Transform transform)
    {
        if (transform == null ||
            !transform.name.EndsWith("Button", StringComparison.Ordinal))
        {
            return false;
        }

        return FindDirectChild(transform, "Name") != null &&
               FindDirectChild(transform, "Slider") != null &&
               (FindDirectChild(transform, "Picture") != null || FindDirectChild(transform, "Image") != null);
    }

    private static bool IsSceneAuthoredStatusVisualName(string objectName)
    {
        return string.Equals(objectName, "Lock", StringComparison.Ordinal) ||
               string.Equals(objectName, "Unlock", StringComparison.Ordinal);
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
        if (usesSceneAuthoredPages)
        {
            return;
        }

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
        RefreshSceneAuthoredPersonalPortrait();
        RefreshSceneAuthoredPersonalAttributes();
        RefreshSceneAuthoredBackpackSurfaces();
        RefreshSceneAuthoredPersonalInkOptions();
        UpdateTextPage(IllustratedHandbookPage.IllustratedHandbook, BuildIllustratedHandbookBody(), BuildIllustratedHandbookFooter());
        UpdateTextPage(IllustratedHandbookPage.PersonalInformation, BuildPersonalInformationBody(), BuildPersonalInformationFooter());
        UpdateTextPage(IllustratedHandbookPage.PhotoAlbum, BuildPhotoAlbumBody(), BuildPhotoAlbumFooter());
        UpdateTextPage(IllustratedHandbookPage.Mission, BuildMissionBody(), BuildMissionFooter());
        UpdateTextPage(IllustratedHandbookPage.Setting, BuildSettingBody(), BuildSettingFooter());
    }

    private void RefreshSceneAuthoredPhotoAlbum(IllustratedHandbookPage activePage)
    {
        if (scenePhotoAlbumBinder != null && activePage != IllustratedHandbookPage.PhotoAlbum)
        {
            scenePhotoAlbumBinder.Release();
        }

        if (!usesSceneAuthoredPages ||
            activePage != IllustratedHandbookPage.PhotoAlbum ||
            photoAlbumCanvas == null)
        {
            return;
        }

        if (scenePhotoAlbumBinder == null)
        {
            scenePhotoAlbumBinder = new IllustratedPhotoAlbumPageBinder();
        }

        scenePhotoAlbumBinder.Bind(photoAlbumCanvas.transform as RectTransform);
        scenePhotoAlbumBinder.Refresh();
    }

    private void RefreshSceneAuthoredSettings(IllustratedHandbookPage activePage)
    {
        if (sceneSettingsToggleBinder != null && activePage != IllustratedHandbookPage.Setting)
        {
            sceneSettingsToggleBinder.Release();
        }

        if (!usesSceneAuthoredPages ||
            activePage != IllustratedHandbookPage.Setting ||
            settingCanvas == null)
        {
            return;
        }

        if (sceneSettingsToggleBinder == null)
        {
            sceneSettingsToggleBinder = settingCanvas.GetComponent<LegacySettingsToggleBinder>();
            if (sceneSettingsToggleBinder == null)
            {
                sceneSettingsToggleBinder = settingCanvas.AddComponent<LegacySettingsToggleBinder>();
            }
        }

        sceneSettingsToggleBinder.Bind();
        sceneSettingsToggleBinder.Refresh();
    }

    private void RefreshSceneAuthoredPersonalPortrait()
    {
        if (!usesSceneAuthoredPages || personalInformationCanvas == null)
        {
            return;
        }

        Transform portraitTransform = FindTransformByName(personalInformationCanvas.transform, PersonalPortraitNodeName);
        Image portraitImage = portraitTransform != null ? portraitTransform.GetComponent<Image>() : null;
        if (portraitImage == null)
        {
            return;
        }

        Sprite portraitSprite = ResolveRuntimePlayerPortraitSprite();
        portraitImage.sprite = portraitSprite;
        portraitImage.type = Image.Type.Simple;
        portraitImage.preserveAspect = true;
        portraitImage.raycastTarget = false;
        portraitImage.color = portraitSprite != null ? Color.white : Color.clear;
        portraitImage.enabled = portraitSprite != null;
    }

    private void RefreshSceneAuthoredPersonalAttributes()
    {
        if (!usesSceneAuthoredPages || personalInformationCanvas == null)
        {
            return;
        }

        Slider[] sliders = personalInformationCanvas.GetComponentsInChildren<Slider>(true);
        for (int i = 0; i < sliders.Length; i++)
        {
            ConfigureSliderReadOnlyDisplay(sliders[i]);
        }

        NormalizePersonalAttributeCurrentValueTexts(personalInformationCanvas.transform);
    }

    private static void ConfigureSliderReadOnlyDisplay(Slider slider)
    {
        if (slider == null)
        {
            return;
        }

        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };

        Graphic[] graphics = slider.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic != null)
            {
                graphic.raycastTarget = false;
            }
        }

        SliderFillGeometryUtility.ApplyExactFill(slider, true);
    }

    private static void NormalizePersonalAttributeCurrentValueTexts(Transform root)
    {
        if (root == null)
        {
            return;
        }

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null || !TryExtractCurrentValue(text.text, out string currentValue))
            {
                continue;
            }

            text.text = currentValue;
        }
    }

    private static bool TryExtractCurrentValue(string source, out string currentValue)
    {
        currentValue = string.Empty;
        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        int slashIndex = source.IndexOf('/');
        if (slashIndex <= 0 || slashIndex >= source.Length - 1)
        {
            return false;
        }

        string left = source.Substring(0, slashIndex).Trim();
        string right = source.Substring(slashIndex + 1).Trim();
        if (!float.TryParse(left, NumberStyles.Float, CultureInfo.InvariantCulture, out _) ||
            !float.TryParse(right, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            return false;
        }

        currentValue = left;
        return true;
    }

    private static Sprite ResolveRuntimePlayerPortraitSprite()
    {
        PlayerProfileData profile = FindObjectOfType<PlayerProfileData>(true);
        GameObject playerObject = ResolveRuntimePlayerObject(profile);
        Sprite frontSprite = ResolveFrontFacingPlayerSprite(playerObject);
        if (frontSprite != null)
        {
            if (profile != null)
            {
                profile.avatar = frontSprite;
            }

            return frontSprite;
        }

        return profile != null ? profile.avatar : null;
    }

    private static Sprite ResolveFrontFacingPlayerSprite(GameObject playerObject)
    {
        SpriteRenderer spriteRenderer = playerObject != null ? playerObject.GetComponent<SpriteRenderer>() : null;
        if (spriteRenderer == null && playerObject != null)
        {
            spriteRenderer = playerObject.GetComponentInChildren<SpriteRenderer>(true);
        }

        Sprite currentSprite = spriteRenderer != null ? spriteRenderer.sprite : null;
        return ResolveLoadedFrontFacingSprite(currentSprite) ?? currentSprite;
    }

    private static Sprite ResolveLoadedFrontFacingSprite(Sprite currentSprite)
    {
        if (currentSprite == null || currentSprite.texture == null)
        {
            return null;
        }

        if (IsLikelyFrontFacingPlayerSprite(currentSprite))
        {
            return currentSprite;
        }

        Sprite[] loadedSprites = Resources.FindObjectsOfTypeAll<Sprite>();
        for (int i = 0; i < loadedSprites.Length; i++)
        {
            Sprite candidate = loadedSprites[i];
            if (candidate == null ||
                candidate.texture != currentSprite.texture ||
                !IsLikelyFrontFacingPlayerSprite(candidate))
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private static bool IsLikelyFrontFacingPlayerSprite(Sprite sprite)
    {
        return sprite != null &&
               !string.IsNullOrWhiteSpace(sprite.name) &&
               sprite.name.EndsWith("_1", StringComparison.Ordinal);
    }

    private static GameObject ResolveRuntimePlayerObject(PlayerProfileData profile)
    {
        if (profile != null)
        {
            return profile.gameObject;
        }

        PlayerMove playerMove = FindObjectOfType<PlayerMove>(true);
        if (playerMove != null)
        {
            return playerMove.gameObject;
        }

        PlayerAttack playerAttack = FindObjectOfType<PlayerAttack>(true);
        if (playerAttack != null)
        {
            return playerAttack.gameObject;
        }

        return GameObject.FindGameObjectWithTag("Player");
    }

    private void RefreshSceneAuthoredBackpackSurfaces()
    {
        RefreshSceneAuthoredPersonalBackpack();
        RefreshSceneAuthoredHandbookBackpack();
    }

    private void RefreshSceneAuthoredPersonalBackpack()
    {
        if (!usesSceneAuthoredPages || personalInformationCanvas == null)
        {
            return;
        }

        BackpackMananger backpack = ResolveRuntimeBackpackManager();
        EnsureBackpackInventorySubscription(backpack);

        List<RectTransform> slotRects = new List<RectTransform>();
        CollectPersonalBackpackSlotRects(personalInformationCanvas.transform, slotRects);
        for (int i = 0; i < slotRects.Count; i++)
        {
            RectTransform slotRect = slotRects[i];
            if (slotRect == null || !TryResolvePersonalBackpackSlotIndex(slotRect.name, out int slotIndex))
            {
                continue;
            }

            PersonalBackpackSlotHoverHandler hoverHandler = EnsurePersonalBackpackSlotHover(slotRect, slotIndex);
            Image iconImage = EnsurePersonalBackpackSlotIcon(slotRect);
            ArchitecturalCrystal? item = backpack != null ? backpack.GetItem(slotIndex) : null;
            ApplyTransparentBackpackSlotSurface(slotRect.GetComponent<Image>());
            ApplyPersonalBackpackSlotIcon(iconImage, item);
            SetSceneAuthoredSelectionVisual(
                slotRect,
                PersonalBackpackSelectionVisualName,
                item.HasValue && slotIndex == selectedPersonalBackpackSlotIndex);
            hoverHandler.RefreshHover();
        }
    }

    private void RefreshSceneAuthoredHandbookBackpack()
    {
        if (!usesSceneAuthoredPages || illustratedHandbookCanvas == null)
        {
            return;
        }

        Transform rightRoot = FindTransformByName(illustratedHandbookCanvas.transform, SceneAuthoredRightIntroductionName);
        RefreshSceneAuthoredHandbookBackpack(rightRoot);
    }

    private void RefreshSceneAuthoredHandbookBackpack(Transform rightRoot)
    {
        if (!usesSceneAuthoredPages || rightRoot == null)
        {
            return;
        }

        BackpackMananger backpack = ResolveRuntimeBackpackManager();
        EnsureBackpackInventorySubscription(backpack);

        RectTransform trayRect = EnsureSceneHandbookBackpackTray(rightRoot);
        if (trayRect == null)
        {
            return;
        }

        for (int i = 0; i < SceneHandbookBackpackSlotCount; i++)
        {
            RectTransform slotRect = EnsureSceneHandbookBackpackSlot(trayRect, i);
            if (slotRect == null)
            {
                continue;
            }

            Image iconImage = EnsureSceneHandbookItemIcon(slotRect);
            ArchitecturalCrystal? item = backpack != null ? backpack.GetItem(i) : null;
            ApplyPersonalBackpackSlotIcon(iconImage, item);

            SceneHandbookBackpackSlotDragHandler dragHandler =
                slotRect.GetComponent<SceneHandbookBackpackSlotDragHandler>();
            if (dragHandler == null)
            {
                dragHandler = slotRect.gameObject.AddComponent<SceneHandbookBackpackSlotDragHandler>();
            }

            dragHandler.Bind(this, i, iconImage);
        }

        RefreshSceneHandbookSpecialMaterialStack(trayRect);
    }

    private RectTransform EnsureSceneHandbookBackpackTray(Transform rightRoot)
    {
        if (rightRoot == null)
        {
            return null;
        }

        RectTransform trayParent = ResolveSceneHandbookBackpackTrayParent(rightRoot);
        Transform existing = FindExistingSceneHandbookBackpackTray(trayParent, rightRoot);
        RectTransform trayRect;
        if (existing != null)
        {
            trayRect = existing as RectTransform;
            if (trayRect != null && trayRect.parent != trayParent)
            {
                trayRect.SetParent(trayParent, false);
            }
        }
        else
        {
            GameObject trayObject = new GameObject(SceneHandbookBackpackTrayName, typeof(RectTransform), typeof(Image));
            trayRect = trayObject.GetComponent<RectTransform>();
            trayRect.SetParent(trayParent, false);
            Image background = trayObject.GetComponent<Image>();
            background.color = new Color(0.28f, 0.20f, 0.12f, 0.18f);
            background.raycastTarget = false;
        }

        float width = SceneHandbookBackpackLaneCount * SceneHandbookBackpackSlotSize +
                      (SceneHandbookBackpackLaneCount - 1) * SceneHandbookBackpackSlotGap +
                      SceneHandbookBackpackTrayPaddingX * 2f;
        ConfigureAnchoredRect(
            trayRect,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(width, SceneHandbookBackpackSlotSize + 16f),
            new Vector2(0f, SceneHandbookBackpackTrayBottom),
            new Vector2(0.5f, 0f));
        trayRect.SetAsLastSibling();
        return trayRect;
    }

    private static RectTransform ResolveSceneHandbookBackpackTrayParent(Transform rightRoot)
    {
        RectTransform fallback = rightRoot as RectTransform;
        Transform current = rightRoot;
        while (current != null)
        {
            if (current is RectTransform currentRect)
            {
                fallback = currentRect;
            }

            if (string.Equals(current.name, IllustratedHandbookCanvasName, StringComparison.Ordinal) ||
                current.GetComponent<Canvas>() != null)
            {
                return current as RectTransform ?? fallback;
            }

            current = current.parent;
        }

        return fallback;
    }

    private static Transform FindExistingSceneHandbookBackpackTray(RectTransform trayParent, Transform rightRoot)
    {
        Transform current = rightRoot;
        while (current != null)
        {
            Transform existing = FindDirectChild(current, SceneHandbookBackpackTrayName);
            if (existing != null)
            {
                return existing;
            }

            if (current == trayParent)
            {
                break;
            }

            current = current.parent;
        }

        return null;
    }

    private RectTransform EnsureSceneHandbookBackpackSlot(RectTransform trayRect, int slotIndex)
    {
        if (trayRect == null)
        {
            return null;
        }

        string slotName = $"{SceneHandbookBackpackSlotNamePrefix}{slotIndex + 1}";
        Transform existing = FindDirectChild(trayRect, slotName);
        RectTransform slotRect;
        Image slotImage;
        if (existing != null)
        {
            slotRect = existing as RectTransform;
            slotImage = existing.GetComponent<Image>();
            if (slotImage == null)
            {
                slotImage = existing.gameObject.AddComponent<Image>();
            }
        }
        else
        {
            GameObject slotObject = new GameObject(slotName, typeof(RectTransform), typeof(Image));
            slotRect = slotObject.GetComponent<RectTransform>();
            slotRect.SetParent(trayRect, false);
            slotImage = slotObject.GetComponent<Image>();
        }

        ConfigureAnchoredRect(
            slotRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(SceneHandbookBackpackSlotSize, SceneHandbookBackpackSlotSize),
            new Vector2(ResolveSceneHandbookBackpackLaneX(slotIndex), 0f),
            new Vector2(0.5f, 0.5f));
        ApplyTransparentBackpackSlotSurface(slotImage);
        return slotRect;
    }

    private static float ResolveSceneHandbookBackpackLaneX(int laneIndex)
    {
        float laneWidth = SceneHandbookBackpackLaneCount * SceneHandbookBackpackSlotSize +
                          (SceneHandbookBackpackLaneCount - 1) * SceneHandbookBackpackSlotGap;
        return -laneWidth * 0.5f +
               SceneHandbookBackpackSlotSize * 0.5f +
               laneIndex * (SceneHandbookBackpackSlotSize + SceneHandbookBackpackSlotGap);
    }

    private static Image EnsureSceneHandbookItemIcon(RectTransform slotRect)
    {
        Transform existing = FindDirectChild(slotRect, SceneHandbookItemIconName);
        RectTransform iconRect;
        Image iconImage;
        if (existing != null)
        {
            iconRect = existing as RectTransform;
            iconImage = existing.GetComponent<Image>();
            if (iconImage == null)
            {
                iconImage = existing.gameObject.AddComponent<Image>();
            }
        }
        else
        {
            GameObject iconObject = new GameObject(SceneHandbookItemIconName, typeof(RectTransform), typeof(Image));
            iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(slotRect, false);
            iconImage = iconObject.GetComponent<Image>();
        }

        ConfigureAnchoredRect(
            iconRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(
                SceneHandbookBackpackSlotSize * SceneHandbookBackpackSlotIconScale,
                SceneHandbookBackpackSlotSize * SceneHandbookBackpackSlotIconScale),
            Vector2.zero,
            new Vector2(0.5f, 0.5f));
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;
        return iconImage;
    }

    private static void ApplyTransparentBackpackSlotSurface(Image slotImage)
    {
        if (slotImage == null)
        {
            return;
        }

        slotImage.color = Color.clear;
        slotImage.raycastTarget = true;
    }

    private void RefreshSceneHandbookSpecialMaterialStack(RectTransform trayRect)
    {
        Transform existing = trayRect != null ? FindDirectChild(trayRect, SceneHandbookSpecialStackName) : null;
        if (existing != null)
        {
            existing.gameObject.SetActive(false);
        }
    }

    private void SelectPersonalBackpackSlot(int slotIndex)
    {
        int resolvedIndex = Mathf.Clamp(slotIndex, 0, 5);
        if (selectedPersonalBackpackSlotIndex == resolvedIndex)
        {
            RefreshSceneAuthoredPersonalBackpack();
            return;
        }

        selectedPersonalBackpackSlotIndex = resolvedIndex;
        BackpackUI backpackUI = FindObjectOfType<BackpackUI>(true);
        if (backpackUI != null)
        {
            backpackUI.SelectSlot(resolvedIndex);
        }

        RefreshSceneAuthoredPersonalBackpack();
        MusicManager.PlaySfx(SfxCueId.SlotSwitch);
    }

    private void EnsureBackpackInventorySubscription(BackpackMananger backpack)
    {
        if (subscribedBackpack == backpack)
        {
            return;
        }

        UnsubscribeBackpackInventoryEvents();
        subscribedBackpack = backpack;
        if (subscribedBackpack != null)
        {
            subscribedBackpack.OnInventoryChanged += RefreshSceneAuthoredBackpackSurfaces;
        }
    }

    private void UnsubscribeBackpackInventoryEvents()
    {
        if (subscribedBackpack == null)
        {
            return;
        }

        subscribedBackpack.OnInventoryChanged -= RefreshSceneAuthoredBackpackSurfaces;
        subscribedBackpack = null;
    }

    private static BackpackMananger ResolveRuntimeBackpackManager()
    {
        return BackpackMananger.Instance != null
            ? BackpackMananger.Instance
            : FindObjectOfType<BackpackMananger>(true);
    }

    private static void CollectPersonalBackpackSlotRects(Transform root, List<RectTransform> slotRects)
    {
        if (root == null || slotRects == null)
        {
            return;
        }

        RectTransform rect = root as RectTransform;
        if (rect != null && TryResolvePersonalBackpackSlotIndex(root.name, out _))
        {
            slotRects.Add(rect);
            return;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            CollectPersonalBackpackSlotRects(root.GetChild(i), slotRects);
        }
    }

    private static bool TryResolvePersonalBackpackSlotIndex(string slotName, out int slotIndex)
    {
        slotIndex = -1;
        if (string.IsNullOrWhiteSpace(slotName) ||
            !slotName.StartsWith(PersonalBackpackSlotNamePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string indexText = slotName.Substring(PersonalBackpackSlotNamePrefix.Length);
        if (!int.TryParse(indexText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int oneBasedIndex) ||
            oneBasedIndex < 1 ||
            oneBasedIndex > 6)
        {
            return false;
        }

        slotIndex = oneBasedIndex - 1;
        return true;
    }

    private PersonalBackpackSlotHoverHandler EnsurePersonalBackpackSlotHover(RectTransform slotRect, int slotIndex)
    {
        Graphic slotGraphic = slotRect != null ? slotRect.GetComponent<Graphic>() : null;
        if (slotGraphic != null)
        {
            slotGraphic.raycastTarget = true;
        }

        PersonalBackpackSlotHoverHandler hoverHandler = EnsurePersonalBackpackSlotHoverHandler(slotRect.gameObject, this, slotIndex);
        Button[] childButtons = slotRect.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < childButtons.Length; i++)
        {
            Button button = childButtons[i];
            if (button == null)
            {
                continue;
            }

            Graphic targetGraphic = button.targetGraphic != null ? button.targetGraphic : button.GetComponent<Graphic>();
            if (targetGraphic != null)
            {
                targetGraphic.raycastTarget = true;
            }

            EnsurePersonalBackpackSlotHoverHandler(button.gameObject, this, slotIndex);
        }

        return hoverHandler;
    }

    private static PersonalBackpackSlotHoverHandler EnsurePersonalBackpackSlotHoverHandler(
        GameObject targetObject,
        IllustratedHandbookTabsController owner,
        int slotIndex)
    {
        PersonalBackpackSlotHoverHandler hoverHandler = targetObject.GetComponent<PersonalBackpackSlotHoverHandler>();
        if (hoverHandler == null)
        {
            hoverHandler = targetObject.AddComponent<PersonalBackpackSlotHoverHandler>();
        }

        hoverHandler.Bind(owner, slotIndex);
        return hoverHandler;
    }

    private static Image EnsurePersonalBackpackSlotIcon(RectTransform slotRect)
    {
        Transform existing = FindDirectChild(slotRect, PersonalBackpackSlotIconName);
        RectTransform iconRect;
        Image iconImage;
        if (existing != null)
        {
            iconRect = existing as RectTransform;
            iconImage = existing.GetComponent<Image>();
            if (iconImage == null)
            {
                iconImage = existing.gameObject.AddComponent<Image>();
            }
        }
        else
        {
            GameObject iconObject = new GameObject(PersonalBackpackSlotIconName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(slotRect, false);
            iconImage = iconObject.GetComponent<Image>();
        }

        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = ResolvePersonalBackpackSlotIconSize(slotRect);
        iconRect.localScale = Vector3.one;
        iconRect.localRotation = Quaternion.identity;
        iconRect.SetAsLastSibling();

        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;
        return iconImage;
    }

    private static Vector2 ResolvePersonalBackpackSlotIconSize(RectTransform slotRect)
    {
        if (slotRect == null)
        {
            return Vector2.zero;
        }

        Vector2 slotSize = slotRect.rect.size;
        if (slotSize.x <= 1f || slotSize.y <= 1f)
        {
            slotSize = slotRect.sizeDelta;
        }

        float iconSize = Mathf.Max(1f, Mathf.Min(slotSize.x, slotSize.y) * PersonalBackpackSlotIconScale);
        return new Vector2(iconSize, iconSize);
    }

    private static void ApplyPersonalBackpackSlotIcon(Image iconImage, ArchitecturalCrystal? item)
    {
        if (iconImage == null)
        {
            return;
        }

        if (!item.HasValue)
        {
            iconImage.sprite = null;
            iconImage.color = Color.white;
            iconImage.enabled = false;
            return;
        }

        Sprite sprite = ResolveBackpackItemSprite(item.Value);
        iconImage.sprite = sprite;
        iconImage.color = Color.white;
        iconImage.enabled = sprite != null;
    }

    private static Sprite ResolveBackpackItemSprite(ArchitecturalCrystal item)
    {
        Sprite displaySprite = item.backIcon != null
            ? item.backIcon
            : (item.icon != null ? item.icon : RuntimeCrystalDropFactory.ResolveSprite(item));
        return RuntimeSpriteDisplaySanitizer.GetDisplaySprite(displaySprite);
    }

    private void RefreshSceneAuthoredPersonalInkOptions()
    {
        if (!usesSceneAuthoredPages || personalInformationCanvas == null)
        {
            return;
        }

        List<RectTransform> optionRects = new List<RectTransform>();
        CollectPersonalInkOptionRects(personalInformationCanvas.transform, optionRects);
        if (optionRects.Count == 0)
        {
            return;
        }

        optionRects.Sort(ComparePersonalInkOptionRects);

        WeaponType selectedWeaponType = RuntimeWeaponTypeResolver.ResolveEffectiveWeaponType(
            BackpackMananger.Instance,
            PlayerLoadoutRuntime.CurrentWeaponType);
        if (!IsPersonalInkOptionWeaponType(selectedWeaponType))
        {
            selectedWeaponType = WeaponType.DirectInk;
        }

        int optionCount = Mathf.Min(optionRects.Count, PersonalInkOptionWeaponTypes.Length);
        for (int i = 0; i < optionCount; i++)
        {
            RectTransform optionRect = optionRects[i];
            WeaponType optionWeaponType = PersonalInkOptionWeaponTypes[i];
            EnsurePersonalInkOptionBehaviour(optionRect, optionWeaponType);
            bool selected = optionWeaponType == selectedWeaponType;
            bool hovered = hasHoveredPersonalInkWeapon && hoveredPersonalInkWeaponType == optionWeaponType;
            SetPersonalInkOptionVisual(optionRect, selected, hovered);
        }

        RefreshPersonalInkDescription(selectedWeaponType);
    }

    private void SelectPersonalInkWeapon(WeaponType weaponType)
    {
        if (!PlayerLoadoutRuntime.IsWeaponUnlocked(weaponType))
        {
            RuntimeSubtitleFeedHud.PushMessage("该墨水基型尚未解锁。解锁对应建筑图鉴后即可装备。");
            RefreshSceneAuthoredPersonalInkOptions();
            return;
        }

        PlayerLoadoutRuntime.ClearRuntimeWeaponOverride();
        PlayerLoadoutRuntime.CurrentWeaponType = weaponType;

        PlayerProfileData profile = FindObjectOfType<PlayerProfileData>(true);
        if (profile != null)
        {
            profile.currentWeaponType = weaponType;
            profile.currentInkType = weaponType.ToInkType();
            profile.SetEffectiveWeapon(weaponType);
        }

        PlayerAttack playerAttack = FindObjectOfType<PlayerAttack>(true);
        if (playerAttack != null)
        {
            playerAttack.RefreshInkUI();
        }

        GameProgressPersistence.SaveIfReady();

        RefreshSceneAuthoredPersonalInkOptions();
        MusicManager.PlaySfx(SfxCueId.SlotSwitch);
    }

    private static void CollectPersonalInkOptionRects(Transform root, List<RectTransform> optionRects)
    {
        if (root == null || optionRects == null)
        {
            return;
        }

        Transform weaponRoot = FindTransformByName(root, PersonalInkRootName);
        if (weaponRoot == null)
        {
            return;
        }

        for (int i = 0; i < weaponRoot.childCount; i++)
        {
            Transform child = weaponRoot.GetChild(i);
            if (child is not RectTransform optionRect ||
                !child.name.StartsWith(PersonalInkOptionNamePrefix, StringComparison.Ordinal) ||
                FindDirectChild(child, PersonalInkButtonName) == null ||
                FindDirectChild(child, PersonalInkUsedName) == null)
            {
                continue;
            }

            optionRects.Add(optionRect);
        }
    }

    private static int ComparePersonalInkOptionRects(RectTransform left, RectTransform right)
    {
        bool hasLeftIndex = TryGetPersonalInkOptionIndex(left, out int leftIndex);
        bool hasRightIndex = TryGetPersonalInkOptionIndex(right, out int rightIndex);
        if (hasLeftIndex && hasRightIndex && leftIndex != rightIndex)
        {
            return leftIndex.CompareTo(rightIndex);
        }

        if (hasLeftIndex != hasRightIndex)
        {
            return hasLeftIndex ? -1 : 1;
        }

        int yCompare = -GetAnchoredPositionY(left).CompareTo(GetAnchoredPositionY(right));
        if (yCompare != 0)
        {
            return yCompare;
        }

        int xCompare = GetAnchoredPositionX(left).CompareTo(GetAnchoredPositionX(right));
        if (xCompare != 0)
        {
            return xCompare;
        }

        int siblingCompare = (left != null ? left.GetSiblingIndex() : int.MaxValue)
            .CompareTo(right != null ? right.GetSiblingIndex() : int.MaxValue);
        return siblingCompare != 0
            ? siblingCompare
            : string.CompareOrdinal(left != null ? left.name : string.Empty, right != null ? right.name : string.Empty);
    }

    private static bool TryGetPersonalInkOptionIndex(RectTransform optionRect, out int index)
    {
        index = -1;
        if (optionRect == null ||
            string.IsNullOrEmpty(optionRect.name) ||
            !optionRect.name.StartsWith(PersonalInkOptionNamePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        string suffix = optionRect.name.Substring(PersonalInkOptionNamePrefix.Length);
        if (!int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out int oneBasedIndex))
        {
            return false;
        }

        index = oneBasedIndex - 1;
        return index >= 0;
    }

    private static float GetAnchoredPositionX(RectTransform rectTransform)
    {
        return rectTransform != null ? rectTransform.anchoredPosition.x : float.MaxValue;
    }

    private static float GetAnchoredPositionY(RectTransform rectTransform)
    {
        return rectTransform != null ? rectTransform.anchoredPosition.y : float.MinValue;
    }

    private void EnsurePersonalInkOptionBehaviour(RectTransform optionRect, WeaponType weaponType)
    {
        if (optionRect == null)
        {
            return;
        }

        Graphic optionGraphic = optionRect.GetComponent<Graphic>();
        if (optionGraphic != null)
        {
            optionGraphic.raycastTarget = true;
        }

        EnsurePersonalInkInteractionHandler(optionRect.gameObject, weaponType);

        Graphic[] childGraphics = optionRect.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < childGraphics.Length; i++)
        {
            Graphic childGraphic = childGraphics[i];
            if (childGraphic == null || childGraphic.transform == optionRect)
            {
                continue;
            }

            childGraphic.raycastTarget = false;
        }

        Transform buttonTransform = FindDirectChild(optionRect, PersonalInkButtonName);
        Button button = buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
        if (button != null)
        {
            button.transition = Selectable.Transition.None;
            EnsureButtonRaycastTarget(button);
            EnsurePersonalInkInteractionHandler(button.gameObject, weaponType);
        }
    }

    private void EnsurePersonalInkInteractionHandler(GameObject targetObject, WeaponType weaponType)
    {
        if (targetObject == null)
        {
            return;
        }

        PersonalInkOptionInteractionHandler handler = targetObject.GetComponent<PersonalInkOptionInteractionHandler>();
        if (handler == null)
        {
            handler = targetObject.AddComponent<PersonalInkOptionInteractionHandler>();
        }

        handler.Bind(this, weaponType);
    }

    private static void SetPersonalInkUsedVisual(RectTransform optionRect, bool selected)
    {
        Transform usedTransform = FindDirectChild(optionRect, PersonalInkUsedName);
        if (usedTransform == null)
        {
            return;
        }

        usedTransform.gameObject.SetActive(selected);
    }

    private void SetPersonalInkHoverState(WeaponType weaponType, bool hovered)
    {
        if (hovered)
        {
            hasHoveredPersonalInkWeapon = true;
            hoveredPersonalInkWeaponType = weaponType;
        }
        else if (hasHoveredPersonalInkWeapon && hoveredPersonalInkWeaponType == weaponType)
        {
            hasHoveredPersonalInkWeapon = false;
        }

        RefreshSceneAuthoredPersonalInkOptions();
    }

    private static void SetPersonalInkOptionVisual(RectTransform optionRect, bool selected, bool hovered)
    {
        SetSceneAuthoredSelectionVisual(optionRect, PersonalInkSelectionVisualName, selected);
        SetSceneAuthoredSelectionVisual(optionRect, PersonalInkSelectionBadgeName, selected);
        SetPersonalInkUsedVisual(optionRect, selected);
        SetPersonalInkHoverVisual(optionRect, hovered);
    }

    private static void SetPersonalInkHoverVisual(RectTransform optionRect, bool hovered)
    {
        if (optionRect == null)
        {
            return;
        }

        float targetScale = hovered ? PersonalInkHoverScale : PersonalInkNormalScale;
        optionRect.localScale = new Vector3(targetScale, targetScale, 1f);

        Graphic graphic = optionRect.GetComponent<Graphic>();
        if (graphic != null)
        {
            graphic.color = Color.white;
        }
    }

    private void RefreshPersonalInkDescription(WeaponType selectedWeaponType)
    {
        if (personalInformationCanvas == null)
        {
            return;
        }

        Transform weaponRoot = FindTransformByName(personalInformationCanvas.transform, PersonalInkRootName);
        Transform descriptionPanel = FindDirectChild(weaponRoot, PersonalInkDescriptionPanelName);
        TMP_Text descriptionText = descriptionPanel != null
            ? descriptionPanel.GetComponentInChildren<TMP_Text>(true)
            : null;
        if (descriptionText == null)
        {
            return;
        }

        descriptionText.text = GetPersonalInkDescription(selectedWeaponType);
        TmpRuntimeFontFallback.WarmupCharacters(descriptionText.text);
        descriptionText.enableWordWrapping = true;
        descriptionText.overflowMode = TextOverflowModes.Ellipsis;
        descriptionText.alignment = TextAlignmentOptions.TopLeft;
    }

    private static string GetPersonalInkDescription(WeaponType weaponType)
    {
        return InkTypeCatalog.GetEffectDescription(weaponType);
    }

    private static void SetSceneAuthoredSelectionVisual(RectTransform target, string visualName, bool selected)
    {
        if (target == null)
        {
            return;
        }

        Transform generatedBorder = FindDirectChild(target, PersonalSelectionBorderName);
        if (generatedBorder != null)
        {
            generatedBorder.gameObject.SetActive(false);
        }

        Transform selectionVisual = FindDirectChild(target, visualName);
        if (selectionVisual == null)
        {
            return;
        }

        selectionVisual.gameObject.SetActive(selected);
        Graphic graphic = selectionVisual.GetComponent<Graphic>();
        if (graphic != null)
        {
            graphic.raycastTarget = false;
        }

        CanvasGroup canvasGroup = selectionVisual.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = selected ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (selected)
        {
            selectionVisual.SetAsLastSibling();
        }
    }

    private static bool IsPersonalInkOptionWeaponType(WeaponType weaponType)
    {
        for (int i = 0; i < PersonalInkOptionWeaponTypes.Length; i++)
        {
            if (PersonalInkOptionWeaponTypes[i] == weaponType)
            {
                return true;
            }
        }

        return false;
    }

    private DetailedInformationUI ResolveSceneAuthoredDetailUi(CatalogueBuildingId buildingId)
    {
        DetailedInformationUI sceneDetailUi = EnsureSceneAuthoredDetailUi(ResolveSceneAuthoredDetailCanvas(buildingId));
        if (sceneDetailUi != null)
        {
            return sceneDetailUi;
        }

        return FindObjectOfType<DetailedInformationUI>(true);
    }

    private Transform ResolveSceneAuthoredDetailCanvas(CatalogueBuildingId buildingId)
    {
        string canvasName = ResolveSceneAuthoredDetailCanvasName(buildingId);
        if (string.IsNullOrEmpty(canvasName))
        {
            return null;
        }

        return ResolveSceneAuthoredDetailCanvas(canvasName);
    }

    private Transform ResolveSceneAuthoredDetailCanvas(string canvasName)
    {
        if (string.IsNullOrEmpty(canvasName))
        {
            return null;
        }

        Transform ownerRoot = owner != null ? owner.transform : null;
        Transform detailCanvas = FindTransformByName(ownerRoot, canvasName);
        if (detailCanvas != null)
        {
            return detailCanvas;
        }

        GameObject sceneRoot = owner != null ? ResolveSceneAuthoredRoot(owner.illustratedHandbook) : null;
        if (sceneRoot != null)
        {
            detailCanvas = FindTransformByName(sceneRoot.transform, canvasName);
            if (detailCanvas != null)
            {
                return detailCanvas;
            }
        }

        return FindTransformByName(transform.root, canvasName);
    }

    private static string ResolveSceneAuthoredDetailCanvasName(CatalogueBuildingId buildingId)
    {
        switch (buildingId)
        {
            case CatalogueBuildingId.Building1:
                return SceneAuthoredFujianDetailCanvasName;
            case CatalogueBuildingId.Building3:
                return SceneAuthoredShuiXiangDetailCanvasName;
            default:
                return null;
        }
    }

    private DetailedInformationUI EnsureSceneAuthoredDetailUi(Transform detailCanvas)
    {
        if (detailCanvas == null)
        {
            return null;
        }

        DetailedInformationUI detailUi = detailCanvas.GetComponent<DetailedInformationUI>();
        if (detailUi == null)
        {
            detailUi = detailCanvas.gameObject.AddComponent<DetailedInformationUI>();
        }

        if (detailUi.illustratedHandbookPanel == null)
        {
            detailUi.illustratedHandbookPanel = illustratedHandbookCanvas != null
                ? illustratedHandbookCanvas
                : owner != null
                    ? owner.illustratedHandbook
                    : null;
        }

        detailUi.detailedInformationPanel = detailCanvas.gameObject;
        BindSceneAuthoredDetailFields(detailUi, detailCanvas);

        if (owner != null)
        {
            owner.detailedInformation = detailCanvas.gameObject;
        }

        UIRootManager.Instance?.RefreshRuntimeBindings();
        return detailUi;
    }

    private static void BindSceneAuthoredDetailFields(DetailedInformationUI detailUi, Transform detailCanvas)
    {
        if (detailUi == null || detailCanvas == null)
        {
            return;
        }

        Transform firstBackground = FindTransformByName(detailCanvas, "BackGround");
        if (detailUi.backGround1 == null && firstBackground != null)
        {
            detailUi.backGround1 = firstBackground.gameObject;
        }

        if (detailUi.page1NameText == null)
        {
            detailUi.page1NameText = FindSceneAuthoredDetailText(detailCanvas, "Name", "Title") ??
                                     FindFirstSceneAuthoredDetailText(detailCanvas);
        }

        if (detailUi.page1IntroductionText == null)
        {
            detailUi.page1IntroductionText = FindSceneAuthoredDetailText(detailCanvas, "Introduction", "Content", "Body");
        }

        if (detailUi.page2FinallyIntroductionText == null)
        {
            detailUi.page2FinallyIntroductionText = FindSceneAuthoredDetailText(detailCanvas, "Finally", "Final");
        }

        if (detailUi.closeButton1 == null)
        {
            detailUi.closeButton1 = FindSceneAuthoredDetailButton(detailCanvas, "Close", "Setting", "关闭");
        }
    }

    private static Text FindSceneAuthoredDetailText(Transform root, params string[] nameFragments)
    {
        if (root == null)
        {
            return null;
        }

        Text[] texts = root.GetComponentsInChildren<Text>(true);
        for (int fragmentIndex = 0; fragmentIndex < nameFragments.Length; fragmentIndex++)
        {
            string fragment = nameFragments[fragmentIndex];
            if (string.IsNullOrEmpty(fragment))
            {
                continue;
            }

            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text != null &&
                    text.name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return text;
                }
            }
        }

        return null;
    }

    private static Text FindFirstSceneAuthoredDetailText(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Text[] texts = root.GetComponentsInChildren<Text>(true);
        return texts.Length > 0 ? texts[0] : null;
    }

    private static Button FindSceneAuthoredDetailButton(Transform root, params string[] nameFragments)
    {
        return FindSceneAuthoredDetailButton(root, true, nameFragments);
    }

    private static Button FindSceneAuthoredDetailButton(
        Transform root,
        bool allowFallback,
        params string[] nameFragments)
    {
        if (root == null)
        {
            return null;
        }

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int fragmentIndex = 0; fragmentIndex < nameFragments.Length; fragmentIndex++)
        {
            string fragment = nameFragments[fragmentIndex];
            if (string.IsNullOrEmpty(fragment))
            {
                continue;
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                {
                    continue;
                }

                Text label = button.GetComponentInChildren<Text>(true);
                TMP_Text tmpLabel = button.GetComponentInChildren<TMP_Text>(true);
                bool matchesName = button.name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
                bool matchesLabel = label != null &&
                                    label.text != null &&
                                    label.text.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
                bool matchesTmpLabel = tmpLabel != null &&
                                       tmpLabel.text != null &&
                                       tmpLabel.text.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
                if (matchesName || matchesLabel || matchesTmpLabel)
                {
                    return button;
                }
            }
        }

        return allowFallback && buttons.Length > 0 ? buttons[0] : null;
    }

    private sealed class SceneHandbookCommonSubmitButtonHandler : MonoBehaviour
    {
        private IllustratedHandbookTabsController owner;
        private Button button;
        private CatalogueBuildingId buildingId;

        public void Bind(IllustratedHandbookTabsController controller, Button targetButton, CatalogueBuildingId targetBuildingId)
        {
            if (button != targetButton && button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
            }

            owner = controller;
            button = targetButton;
            buildingId = targetBuildingId;
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(HandleClicked);
            button.onClick.AddListener(HandleClicked);
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
            }
        }

        private void HandleClicked()
        {
            owner?.SubmitCommonMaterialsToBuilding(buildingId);
        }
    }

    private sealed class SceneHandbookProprietarySlotDropHandler : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        private IllustratedHandbookTabsController owner;
        private CatalogueBuildingId buildingId;
        private int slotIndex = -1;

        public void Bind(IllustratedHandbookTabsController controller, CatalogueBuildingId targetBuildingId, int targetSlotIndex)
        {
            owner = controller;
            buildingId = targetBuildingId;
            slotIndex = targetSlotIndex;
        }

        public void OnDrop(PointerEventData eventData)
        {
            GameObject pointerDrag = eventData?.pointerDrag;
            if (!SceneHandbookBackpackSlotDragHandler.TryGetDraggedSpecialStructureSlot(pointerDrag, out int sourceSlotIndex) &&
                !BackpackSlot.TryGetDraggedSpecialStructureSlot(pointerDrag, out sourceSlotIndex))
            {
                return;
            }

            owner?.TryDropSpecialMaterialOnProprietarySlot(buildingId, slotIndex, sourceSlotIndex);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            owner?.TryClickProprietarySlot(buildingId, slotIndex);
        }
    }

    private sealed class SceneHandbookBuildingDetailButtonHandler : MonoBehaviour
    {
        private IllustratedHandbookTabsController owner;
        private Button button;
        private CatalogueBuildingId buildingId;
        private BuildingDetailData detailData;
        private BuildingDefinition definition;
        private Sprite previewSprite;

        public void Bind(
            IllustratedHandbookTabsController controller,
            CatalogueBuildingId targetBuildingId,
            BuildingDetailData targetDetailData,
            BuildingDefinition targetDefinition,
            Sprite targetPreviewSprite)
        {
            owner = controller;
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button != null)
            {
                button.onClick.RemoveListener(OpenDetail);
                button.onClick.AddListener(OpenDetail);
            }

            buildingId = targetBuildingId;
            detailData = targetDetailData;
            definition = targetDefinition;
            previewSprite = targetPreviewSprite;
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OpenDetail);
            }
        }

        private void OpenDetail()
        {
            RuntimeProgressState runtimeState = RuntimeProgressState.Instance ?? RuntimeProgressState.EnsureInstance();
            if (!runtimeState.IsBuildingUnlocked(buildingId))
            {
                return;
            }

            DetailedInformationUI detailUi = owner != null
                ? owner.ResolveSceneAuthoredDetailUi(buildingId)
                : FindObjectOfType<DetailedInformationUI>(true);
            if (detailUi == null)
            {
                RuntimeSubtitleFeedHud.PushMessage("详情界面暂未接入。");
                return;
            }

            detailUi.ShowDetail(ResolveDetailData());
        }

        private BuildingDetailData ResolveDetailData()
        {
            if (detailData != null)
            {
                return detailData;
            }

            detailData = GetComponent<BuildingDetailData>();
            if (detailData == null)
            {
                detailData = gameObject.AddComponent<BuildingDetailData>();
            }

            detailData.buildingName = !string.IsNullOrWhiteSpace(definition.detailTitle)
                ? definition.detailTitle
                : definition.displayName;
            detailData.detailSprite1 = previewSprite;
            detailData.introduction1 = definition.detailDescription;
            detailData.finalIntroduction = definition.detailDescription;
            return detailData;
        }
    }

    private sealed class SceneHandbookBackpackSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private IllustratedHandbookTabsController owner;
        private int slotIndex = -1;
        private Image sourceIcon;
        private RectTransform dragGhost;

        public void Bind(IllustratedHandbookTabsController controller, int index, Image icon)
        {
            owner = controller;
            slotIndex = index;
            sourceIcon = icon;
        }

        public static bool TryGetDraggedSpecialStructureSlot(GameObject pointerDrag, out int sourceSlotIndex)
        {
            sourceSlotIndex = -1;
            SceneHandbookBackpackSlotDragHandler handler =
                pointerDrag != null
                    ? pointerDrag.GetComponent<SceneHandbookBackpackSlotDragHandler>() ??
                      pointerDrag.GetComponentInParent<SceneHandbookBackpackSlotDragHandler>()
                    : null;

            int resolvedSlotIndex;
            if (handler != null)
            {
                resolvedSlotIndex = handler.slotIndex;
            }
            else if (!DraggedSpecialStructureSlotSource.TryResolve(pointerDrag, out resolvedSlotIndex))
            {
                return false;
            }

            BackpackMananger backpack = ResolveRuntimeBackpackManager();
            ArchitecturalCrystal? item = backpack != null ? backpack.GetItem(resolvedSlotIndex) : null;
            if (!item.HasValue || !item.Value.IsSpecialStructure)
            {
                return false;
            }

            sourceSlotIndex = resolvedSlotIndex;
            return true;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            BackpackMananger backpack = ResolveRuntimeBackpackManager();
            ArchitecturalCrystal? item = backpack != null ? backpack.GetItem(slotIndex) : null;
            if (!item.HasValue)
            {
                return;
            }

            dragGhost = CreateSceneHandbookDragGhost(
                sourceIcon,
                slotIndex,
                eventData != null ? eventData.position : (Vector2)Input.mousePosition);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateSceneHandbookDragGhost(dragGhost, eventData != null ? eventData.position : (Vector2)Input.mousePosition);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            DestroySceneHandbookDragGhost(dragGhost);
            dragGhost = null;
            owner?.RefreshSceneAuthoredBackpackSurfaces();
        }
    }

    private static RectTransform CreateSceneHandbookDragGhost(Image sourceIcon, int sourceSlotIndex, Vector2 screenPosition)
    {
        Canvas canvas = ResolveSceneHandbookDragCanvas();
        if (canvas == null)
        {
            return null;
        }

        GameObject ghostObject = new GameObject("HandbookDragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        RectTransform ghostRect = ghostObject.GetComponent<RectTransform>();
        ghostRect.SetParent(canvas.transform, false);
        ghostRect.sizeDelta = new Vector2(42f, 42f);
        ghostRect.pivot = new Vector2(0.5f, 0.5f);
        ghostRect.SetAsLastSibling();

        DraggedSpecialStructureSlotSource dragSource = ghostObject.AddComponent<DraggedSpecialStructureSlotSource>();
        dragSource.Bind(sourceSlotIndex);

        CanvasGroup canvasGroup = ghostObject.GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.9f;

        Image ghostImage = ghostObject.GetComponent<Image>();
        ghostImage.sprite = sourceIcon != null ? sourceIcon.sprite : null;
        ghostImage.color = sourceIcon != null ? sourceIcon.color : Color.white;
        ghostImage.preserveAspect = true;
        ghostImage.raycastTarget = false;

        UpdateSceneHandbookDragGhost(ghostRect, screenPosition);
        return ghostRect;
    }

    private static void UpdateSceneHandbookDragGhost(RectTransform dragGhost, Vector2 screenPosition)
    {
        if (dragGhost == null)
        {
            return;
        }

        Canvas canvas = dragGhost.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        if (canvasRect == null)
        {
            dragGhost.position = screenPosition;
            return;
        }

        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, eventCamera, out Vector2 localPoint))
        {
            dragGhost.anchoredPosition = localPoint;
        }
    }

    private static void DestroySceneHandbookDragGhost(RectTransform dragGhost)
    {
        if (dragGhost != null)
        {
            Destroy(dragGhost.gameObject);
        }
    }

    private static Canvas ResolveSceneHandbookDragCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        Canvas bestCanvas = null;
        int bestSortingOrder = int.MinValue;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || !canvas.gameObject.activeInHierarchy)
            {
                continue;
            }

            int sortingOrder = canvas.overrideSorting ? canvas.sortingOrder : 0;
            if (bestCanvas != null && sortingOrder < bestSortingOrder)
            {
                continue;
            }

            bestCanvas = canvas;
            bestSortingOrder = sortingOrder;
        }

        if (bestCanvas != null)
        {
            return bestCanvas;
        }

        GameObject canvasObject = new GameObject("HandbookDragCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvasComponent = canvasObject.GetComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasComponent.overrideSorting = true;
        canvasComponent.sortingOrder = RuntimeModalStyle.ModalSortingOrder + 100;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        return canvasComponent;
    }

    private sealed class PersonalBackpackSlotHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IPointerClickHandler
    {
        private IllustratedHandbookTabsController owner;
        private int slotIndex = -1;
        private bool isHovered;
        private Vector2 lastScreenPosition;

        public void Bind(IllustratedHandbookTabsController controller, int index)
        {
            owner = controller;
            slotIndex = index;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            owner?.SelectPersonalBackpackSlot(slotIndex);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            lastScreenPosition = eventData != null ? eventData.position : (Vector2)Input.mousePosition;
            RefreshHover();
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            isHovered = true;
            lastScreenPosition = eventData != null ? eventData.position : (Vector2)Input.mousePosition;
            RefreshHover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HideHover();
        }

        private void OnDisable()
        {
            HideHover();
        }

        private void OnDestroy()
        {
            HideHover();
        }

        public void RefreshHover()
        {
            if (!isHovered)
            {
                return;
            }

            BackpackMananger backpack = ResolveRuntimeBackpackManager();
            ArchitecturalCrystal? item = backpack != null ? backpack.GetItem(slotIndex) : null;
            if (!item.HasValue)
            {
                HideHover();
                return;
            }

            RuntimeBackpackHoverHud.EnsureInstance().ShowOrUpdate(this, item.Value, lastScreenPosition);
        }

        private void HideHover()
        {
            isHovered = false;
            if (RuntimeBackpackHoverHud.Instance != null)
            {
                RuntimeBackpackHoverHud.Instance.HideForOwner(this);
            }
        }
    }

    private sealed class PersonalInkOptionInteractionHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private IllustratedHandbookTabsController owner;
        private WeaponType weaponType = WeaponType.DirectInk;

        public void Bind(IllustratedHandbookTabsController controller, WeaponType type)
        {
            owner = controller;
            weaponType = type;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            owner?.SelectPersonalInkWeapon(weaponType);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            owner?.SetPersonalInkHoverState(weaponType, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            owner?.SetPersonalInkHoverState(weaponType, false);
        }
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
            BackpackMananger backpack = ResolveRuntimeBackpackManager();
            int specialStructureCount = backpack != null ? backpack.GetSpecialStructureMaterialCount() : 0;
            lines.Add($"图鉴总进度：{runtimeState.GetTotalProgress()}/{runtimeState.GetTotalMaxProgress()}");
            lines.Add($"背包专用结构：{specialStructureCount}/{BackpackMananger.MaxSpecialStructureMaterialCount}");
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
            lines.Add("暂无留念照片。在基地或关卡里按拍照键保存到本地相册。");
            return string.Join("\n", lines);
        }

        int previewCount = Mathf.Min(4, entries.Count);
        for (int i = 0; i < previewCount; i++)
        {
            PhotoAlbumEntry entry = entries[i];
            GameplayStageDefinition stage = GameplayStageCatalog.GetStageById(entry.stageId);
            string stageName = stage != null
                ? stage.displayName
                : ResolvePhotoAlbumEntrySceneLabel(entry.sceneName);
            lines.Add($"{i + 1}. {stageName}    {FormatSavedTime(entry.savedAtUtc)}");
        }

        return string.Join("\n", lines);
    }

    private string BuildPhotoAlbumFooter()
    {
        KeyCode captureKey = GameSettingsStore.GetKeyBinding(GameInputAction.PhotoCapture);
        return $"相册入口已统一走书本；基地或关卡内按 {captureKey} 可继续保存本地留念。";
    }

    private static string ResolvePhotoAlbumEntrySceneLabel(string sceneName)
    {
        if (string.Equals(sceneName, "NewBase", StringComparison.Ordinal))
        {
            return "基地";
        }

        return string.IsNullOrWhiteSpace(sceneName) ? "未记录场景" : sceneName;
    }

    private string BuildMissionBody()
    {
        RuntimeProgressState runtimeState = ResolveRuntimeProgressState();
        GameplayStageDefinition selectedStage = GameplayStageRuntime.SelectedStage;
        IReadOnlyList<GameplayStageDefinition> stages = GameplayStageCatalog.GetAll();
        int unlockedStageCount = 0;
        if (stages != null)
        {
            for (int i = 0; i < stages.Count; i++)
            {
                if (GameplayStageCatalog.IsStageUnlocked(stages[i], runtimeState))
                {
                    unlockedStageCount++;
                }
            }
        }

        List<string> lines = new List<string>
        {
            $"当前场景：{SceneManager.GetActiveScene().name}"
        };

        if (runtimeState == null)
        {
            lines.Add("当前场景未挂接任务运行态，进入基地或战斗后会自动同步。");
            return string.Join("\n", lines);
        }

        string currentStageName = selectedStage != null ? selectedStage.displayName : "未进入关卡";
        BackpackMananger backpack = ResolveRuntimeBackpackManager();
        int specialStructureCount = backpack != null ? backpack.GetSpecialStructureMaterialCount() : 0;
        lines.Add($"当前阶段：{(string.IsNullOrWhiteSpace(currentStageName) ? "未进入关卡" : currentStageName)}");
        lines.Add($"已开放关卡：{unlockedStageCount}/{Mathf.Max(1, stages != null ? stages.Count : 0)}");
        lines.Add($"图鉴总进度：{runtimeState.GetTotalProgress()}/{runtimeState.GetTotalMaxProgress()}");
        lines.Add($"背包专用结构：{specialStructureCount}/{BackpackMananger.MaxSpecialStructureMaterialCount}");
        return string.Join("\n", lines);
    }

    private string BuildMissionFooter()
    {
        return "任务页当前先展示运行时阶段与总进度摘要，后续再接完整任务内容。";
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
            $"声音开关：静音 {FormatAudioToggle(settings.muteMode)} / 淡入淡出 {FormatAudioToggle(settings.musicCrossfade)} / 动态范围 {FormatAudioToggle(settings.sfxDynamicRange)} / 空间音效 {FormatAudioToggle(settings.spatialAudio)}",
            $"攻击：{settings.attackKey}    交互：{settings.interactKey}",
            $"地图：{settings.openMapKey}    暂停：{settings.pauseKey}    拍照：{settings.photoCaptureKey}"
        };

        return string.Join("\n", lines);
    }

    private static string FormatAudioToggle(bool enabled)
    {
        return enabled ? "开" : "关";
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
        scaler.matchWidthOrHeight = 0.5f;

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
        return page;
    }

    private static bool ShouldBindScenePage(IllustratedHandbookPage page)
    {
        return page == IllustratedHandbookPage.IllustratedHandbook ||
               page == IllustratedHandbookPage.PersonalInformation ||
               page == IllustratedHandbookPage.PhotoAlbum ||
               page == IllustratedHandbookPage.Setting;
    }

    private static void NormalizePageRoot(GameObject pageRoot)
    {
        if (pageRoot == null)
        {
            return;
        }

        RectTransform rectTransform = pageRoot.GetComponent<RectTransform>();
        if (rectTransform != null && rectTransform.localScale == Vector3.zero)
        {
            rectTransform.localScale = Vector3.one;
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

    private static void DisableTransparentGraphicRaycasts(GameObject pageRoot)
    {
        if (pageRoot == null)
        {
            return;
        }

        Graphic[] graphics = pageRoot.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null || !graphic.raycastTarget)
            {
                continue;
            }

            if (graphic.color.a > 0.001f)
            {
                continue;
            }

            if (graphic.GetComponent<Selectable>() != null)
            {
                continue;
            }

            graphic.raycastTarget = false;
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

    private static void NormalizeSceneBookmarkVisualRect(RectTransform visualRect)
    {
        if (visualRect == null)
        {
            return;
        }

        visualRect.sizeDelta = SceneAuthoredBookmarkSize;
        visualRect.localScale = SceneAuthoredBookmarkScale;
    }

    private static Transform FindSceneBookmarkVisual(Transform pageRoot, IllustratedHandbookPage page)
    {
        return FindSceneBookmarkVisual(pageRoot, GetSceneTabButtonName(page));
    }

    private static Transform FindSceneBookmarkVisual(Transform pageRoot, string bookmarkName)
    {
        if (pageRoot == null || string.IsNullOrEmpty(bookmarkName))
        {
            return null;
        }

        Transform bookmarkRoot = FindDirectChild(pageRoot, SceneAuthoredBookmarkRootName) ??
                                 FindTransformByName(pageRoot, SceneAuthoredBookmarkRootName);
        if (bookmarkRoot == null)
        {
            return null;
        }

        return FindDirectChild(bookmarkRoot, bookmarkName) ??
               FindTransformByName(bookmarkRoot, bookmarkName);
    }

    private static Button GetOrCreateBookmarkHitAreaButton(Transform visualRoot)
    {
        if (visualRoot == null)
        {
            return null;
        }

        RectTransform visualRect = visualRoot as RectTransform;
        if (visualRect == null)
        {
            return null;
        }

        Transform existing = FindDirectChild(visualRoot, SceneBookmarkHitAreaName);
        GameObject hitObject;
        RectTransform hitRect;
        Image hitImage;
        Button hitButton;

        if (existing != null)
        {
            hitObject = existing.gameObject;
            hitRect = existing as RectTransform;
            hitImage = hitObject.GetComponent<Image>();
            hitButton = hitObject.GetComponent<Button>();
        }
        else
        {
            hitObject = new GameObject(SceneBookmarkHitAreaName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            hitObject.layer = visualRoot.gameObject.layer;
            hitRect = hitObject.GetComponent<RectTransform>();
            hitRect.SetParent(visualRoot, false);
            hitImage = hitObject.GetComponent<Image>();
            hitButton = hitObject.GetComponent<Button>();
        }

        if (hitRect == null)
        {
            return null;
        }

        hitRect.anchorMin = Vector2.zero;
        hitRect.anchorMax = Vector2.one;
        hitRect.offsetMin = new Vector2(-12f, -8f);
        hitRect.offsetMax = new Vector2(12f, 8f);
        hitRect.pivot = new Vector2(0.5f, 0.5f);
        hitRect.localScale = Vector3.one;
        hitRect.SetAsLastSibling();

        if (hitImage == null)
        {
            hitImage = hitObject.AddComponent<Image>();
        }

        hitImage.color = new Color(1f, 1f, 1f, 0.001f);
        hitImage.raycastTarget = true;
        hitImage.canvasRenderer.cullTransparentMesh = false;

        if (hitButton == null)
        {
            hitButton = hitObject.AddComponent<Button>();
        }

        hitButton.targetGraphic = hitImage;
        hitButton.transition = Selectable.Transition.None;
        hitButton.interactable = true;
        return hitButton;
    }

    private static Button FindSceneAuthoredBookmarkButton(Transform visualRoot)
    {
        if (visualRoot == null)
        {
            return null;
        }

        Button rootButton = visualRoot.GetComponent<Button>();
        if (rootButton != null)
        {
            return rootButton;
        }

        Button[] buttons = visualRoot.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null ||
                string.Equals(button.gameObject.name, SceneBookmarkHitAreaName, StringComparison.Ordinal))
            {
                continue;
            }

            return button;
        }

        return null;
    }

    private static void ConfigureSceneBookmarkButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        EnsureButtonRaycastTarget(button);
        button.transition = Selectable.Transition.None;
        button.interactable = true;
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

            button = children[i].GetComponentInChildren<Button>(true);
            if (button != null)
            {
                return button;
            }
        }

        return null;
    }

    private static void EnsureButtonRaycastTarget(Button button)
    {
        if (button == null)
        {
            return;
        }

        Graphic targetGraphic = button.targetGraphic;
        if (targetGraphic == null)
        {
            targetGraphic = button.GetComponent<Graphic>();
            if (targetGraphic != null)
            {
                button.targetGraphic = targetGraphic;
            }
        }

        if (targetGraphic != null)
        {
            Color targetColor = targetGraphic.color;
            if (targetColor.a <= 0.001f)
            {
                targetColor.a = 0.001f;
                targetGraphic.color = targetColor;
            }

            targetGraphic.raycastTarget = true;
            targetGraphic.canvasRenderer.cullTransparentMesh = false;
        }
    }

    private static void DisableSceneBookmarkTransparentBlockers(Transform pageRoot)
    {
        Transform bookmarkRoot = FindDirectChild(pageRoot, SceneAuthoredBookmarkRootName) ??
                                 FindTransformByName(pageRoot, SceneAuthoredBookmarkRootName);
        if (bookmarkRoot == null)
        {
            return;
        }

        Graphic[] graphics = bookmarkRoot.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null ||
                !graphic.raycastTarget ||
                graphic.color.a > 0.001f ||
                graphic.GetComponent<Selectable>() != null)
            {
                continue;
            }

            graphic.raycastTarget = false;
        }
    }

    private static void ConfigureSceneBookmarkHitArea(Button button, RectTransform visualRoot)
    {
        if (button == null || visualRoot == null)
        {
            return;
        }

        RectTransform hitRect = button.transform as RectTransform;
        if (hitRect == null || hitRect == visualRoot || hitRect.parent != visualRoot)
        {
            return;
        }

        hitRect.anchorMin = Vector2.zero;
        hitRect.anchorMax = Vector2.one;
        hitRect.offsetMin = new Vector2(-12f, -8f);
        hitRect.offsetMax = new Vector2(12f, 8f);
        hitRect.pivot = new Vector2(0.5f, 0.5f);
        hitRect.localScale = Vector3.one;
    }

    private static Transform FindTransformByName(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (string.Equals(children[i].name, childName, StringComparison.Ordinal))
            {
                return children[i];
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

    private static TMP_Text FindTmpTextContaining(Transform parent, string text)
    {
        if (parent == null || string.IsNullOrEmpty(text))
        {
            return null;
        }

        TMP_Text[] texts = parent.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text candidate = texts[i];
            if (candidate != null && candidate.text != null && candidate.text.Contains(text))
            {
                return candidate;
            }
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
        label.raycastTarget = false;
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
        label.raycastTarget = false;
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

    private static void ConfigureTopStretchRect(RectTransform rectTransform, float left, float right, float top, float height)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.offsetMin = new Vector2(left, -(top + height));
        rectTransform.offsetMax = new Vector2(-right, -top);
        rectTransform.pivot = new Vector2(0.5f, 1f);
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

[RequireComponent(typeof(Button))]
internal sealed class SceneAuthoredHandbookCardSelection : MonoBehaviour
{
    private IllustratedHandbookTabsController owner;
    private Button button;
    private bool subscribed;

    public void Bind(IllustratedHandbookTabsController nextOwner)
    {
        owner = nextOwner;
        EnsureButton();
        Subscribe();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void EnsureButton()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    private void Subscribe()
    {
        EnsureButton();
        if (button == null || subscribed)
        {
            return;
        }

        button.onClick.AddListener(HandleClick);
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (button != null && subscribed)
        {
            button.onClick.RemoveListener(HandleClick);
        }

        subscribed = false;
    }

    private void HandleClick()
    {
        owner?.SelectSceneAuthoredHandbookCard(gameObject);
    }
}
