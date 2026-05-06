using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BackpackUI : MonoBehaviour
{
    private static BackpackUI currentRuntimeInstance;
    private static bool AllowManualCollapseToggle => false;
    private const string RuntimeCanvasRootName = "PackBagCanvas";
    private const string RuntimeSurfaceName = "RuntimeBackpackSurface";
    private const string RuntimeSharedPanelName = "Panel";
    private const string RuntimeItemPanelName = "ItemPanel";
    private const string RuntimeAttackPanelName = "AttackPanel";
    private const string RuntimeSlotPrefix = "slot_";
    private const string RuntimeSlotIconName = "ItemIcon";
    private const string RuntimeSlotSelectionName = "SelectedBorder";
    private const string RuntimeSlotHoverShadeName = "HoverShade";
    private const string RuntimeBackpackFrameSpriteResourcePath = "UI/BackpackSlotsHorizontal";
    private const string RuntimeFallbackSlotSpriteResourcePath = "UI/AttackSlot";
    private const int RuntimeAttackSelectionIndex = -1;
    private const int RuntimeSlotCount = 6;
    private const int RuntimeCanvasSortingOrder = 1;
    private const float RuntimeFallbackSurfaceBottom = 24f;
    private const float RuntimeFallbackItemPanelWidth = 550f;
    private const float RuntimeFallbackItemPanelHeight = 112f;
    private const float RuntimeSceneAuthoredLayoutBottom = 12f;
    private const float RuntimeSceneAuthoredLayoutWidth = 920f;
    private const float RuntimeSceneAuthoredLayoutHeight = 148f;
    private const float RuntimeFallbackAttackPanelX = 459f;
    private const float RuntimeFallbackAttackPanelY = 65f;
    private const float RuntimeFallbackAttackPanelSize = 130f;
    private const float RuntimeFallbackSlotSize = 80f;
    private const float RuntimeFallbackSlotGapX = 85.6f;
    private const float RuntimeFallbackMergedSlotWidth = 70f;
    private const float RuntimeFallbackMergedSlotHeight = 64f;
    private const float RuntimeFallbackMergedSlotGapX = 70f;
    private const float RuntimeSelectionBorderThickness = 3f;
    private const float RuntimeSelectionFramePadding = 3f;
    private const float RuntimeSelectionFadeSpeed = 12f;
    private const string TogglePromptId = "backpack_toggle";
    private const float ToggleHotspotY = 18f;
    private const float ToggleHotspotWidth = 138f;
    private const float ToggleHotspotHeight = 44f;
    private const float CollapseSlideDistance = 112f;
    private const float SlideSmoothTime = 0.08f;
    private const float ModalHideSlideDistance = 92f;
    private const float VisibilitySmoothTime = 0.12f;
    private const float CollapsedHintScalePulse = 0.04f;
    private const float CollapsedHintPulseSpeed = 4.4f;
    private static readonly Color ToggleButtonColor = new Color(0.17f, 0.12f, 0.07f, 0.88f);
    private static readonly Color ToggleButtonColorCollapsed = new Color(0.20f, 0.13f, 0.05f, 0.94f);
    private static readonly Color ToggleTextColor = new Color(0.98f, 0.89f, 0.67f, 1f);
    private static readonly Color RuntimeBackpackSelectionBorderColor = new Color(1f, 0.86f, 0.32f, 0.98f);
    private static readonly Color RuntimeAttackSelectionBorderColor = new Color(1f, 0.12f, 0.08f, 0.98f);
    private static readonly Color RuntimeHoverShadeColor = new Color(0f, 0f, 0f, 0.12f);
    private static Sprite runtimeBackpackFrameSprite;
    private static Sprite runtimeFallbackSlotSprite;

    public Image[] backPackGrid;
    private BackpackMananger backpack;
    private bool subscribedToRuntimeState;
    private RectTransform rectTransform;
    private RectTransform toggleHotspotRect;
    private Image toggleHotspotImage;
    private TextMeshProUGUI toggleHintText;
    private CanvasGroup canvasGroup;
    private Vector2 expandedAnchoredPosition;
    private Vector2 currentTargetPosition;
    private Vector2 slideVelocity;
    private float alphaVelocity;
    private Transform cachedPlayerTransform;
    private bool slideInitialized;
    private bool isCollapsed;
    private bool isRuntimeVisible = true;
    private int pickupPresentationLockCount;
    private float pickupToggleSuppressUntilTime;
    private bool shouldRestoreCollapsedAfterPickupPresentation;
    private RectTransform runtimeLayoutRootRect;
    private RectTransform runtimeAttackSlotRect;
    private RectTransform[] runtimeSlotRects;
    private CanvasGroup runtimeAttackSelectionGroup;
    private CanvasGroup[] runtimeSelectionGroups;
    private GameObject[] runtimeHoverShadeObjects;
    private readonly List<RaycastResult> pointerRaycastResults = new List<RaycastResult>(16);
    private int selectedSlotIndex = RuntimeAttackSelectionIndex;

    public int SelectedSlotIndex => selectedSlotIndex;
    public bool IsAttackSlotSelected => selectedSlotIndex == RuntimeAttackSelectionIndex;

    public static bool IsRuntimeAttackSlotSelected()
    {
        return currentRuntimeInstance == null || currentRuntimeInstance.IsAttackSlotSelected;
    }

    public static BackpackUI EnsureRuntimeInstance(bool refreshLayout = true)
    {
        RectTransform runtimeCanvasRoot = FindRuntimeCanvasRoot();
        if (runtimeCanvasRoot == null)
        {
            runtimeCanvasRoot = CreateRuntimeCanvasRoot();
        }

        BackpackUI view = runtimeCanvasRoot != null ? runtimeCanvasRoot.GetComponent<BackpackUI>() : null;
        if (view == null)
        {
            if (runtimeCanvasRoot == null)
            {
                return null;
            }

            view = runtimeCanvasRoot.gameObject.AddComponent<BackpackUI>();
        }

        if (refreshLayout)
        {
            view.ConfigureRuntimeLayout();
        }

        currentRuntimeInstance = view;
        return view;
    }

    private void Start()
    {
        if (!EnsureRuntimeRootOwnership())
        {
            return;
        }

        currentRuntimeInstance = this;
        ConfigureRuntimeLayout();
        ResolveBackpackManager();
        EnsureCanvasGroup();
        EnsureSlideToggle();
        RefreshUI();
    }

    private void OnEnable()
    {
        if (!EnsureRuntimeRootOwnership())
        {
            return;
        }

        currentRuntimeInstance = this;
        ConfigureRuntimeLayout();
        ResolveBackpackManager();
        SubscribeRuntimeEvents();
        EnsureCanvasGroup();
        EnsureSlideToggle();
        RefreshUI();
    }

    private void OnDisable()
    {
        if (currentRuntimeInstance == this)
        {
            currentRuntimeInstance = null;
        }

        HideFollowTogglePrompt();
        UnsubscribeRuntimeEvents();
    }

    public void RefreshUI()
    {
        ConfigureRuntimeLayout();
        ResolveBackpackManager();

        if (backPackGrid == null || backpack == null)
        {
            return;
        }

        for (int i = 0; i < backPackGrid.Length; i++)
        {
            Image image = backPackGrid[i];
            if (image == null)
            {
                continue;
            }

            ArchitecturalCrystal? item = backpack.GetItem(i);
            if (item.HasValue)
            {
                ArchitecturalCrystal crystal = item.Value;
                Sprite displaySprite = crystal.backIcon != null
                    ? crystal.backIcon
                    : (crystal.icon != null ? crystal.icon : RuntimeCrystalDropFactory.ResolveSprite(crystal));
                image.sprite = RuntimeSpriteDisplaySanitizer.GetDisplaySprite(displaySprite);
                image.color = Color.white;
                image.enabled = true;
            }
            else
            {
                image.sprite = null;
                image.color = Color.white;
                image.enabled = false;
            }
        }

        RefreshSelectionVisuals();
    }

    public void ConfigureRuntimeLayout()
    {
        EnsureRuntimeCanvasVisible();
        BindRuntimeBackpackLayout();
    }

    private void Update()
    {
        if (!slideInitialized || rectTransform == null)
        {
            return;
        }

        Vector2 targetPosition = GetAnimatedTargetPosition();
        if ((rectTransform.anchoredPosition - targetPosition).sqrMagnitude < 0.01f)
        {
            rectTransform.anchoredPosition = targetPosition;
        }
        else
        {
            rectTransform.anchoredPosition = Vector2.SmoothDamp(
                rectTransform.anchoredPosition,
                targetPosition,
                ref slideVelocity,
                SlideSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
        }

        if (canvasGroup != null)
        {
            float targetAlpha = isRuntimeVisible ? 1f : 0f;
            canvasGroup.alpha = Mathf.SmoothDamp(
                canvasGroup.alpha,
                targetAlpha,
                ref alphaVelocity,
                VisibilitySmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);

            bool canInteract = isRuntimeVisible && canvasGroup.alpha > 0.98f;
            canvasGroup.interactable = canInteract;
            canvasGroup.blocksRaycasts = canInteract;
        }

        RefreshToggleHintVisual();
        UpdateFollowTogglePrompt();
        HandleSlotSelectionInput();
        UpdateSelectionFade(Time.unscaledDeltaTime);
    }

    public void SelectSlot(int slotIndex)
    {
        int resolvedIndex = slotIndex == RuntimeAttackSelectionIndex
            ? RuntimeAttackSelectionIndex
            : Mathf.Clamp(slotIndex, 0, RuntimeSlotCount - 1);

        if (selectedSlotIndex == resolvedIndex)
        {
            RefreshSelectionVisuals();
            return;
        }

        selectedSlotIndex = resolvedIndex;
        RefreshSelectionVisuals();
        MusicManager.PlaySfx(SfxCueId.SlotSwitch);
    }

    public void SelectAttackSlot()
    {
        SelectSlot(RuntimeAttackSelectionIndex);
    }

    public void SetRuntimeVisible(bool visible, bool immediate = false)
    {
        EnsureCanvasGroup();
        EnsureSlideToggle();
        isRuntimeVisible = visible;

        if (!visible)
        {
            HideFollowTogglePrompt();
        }

        if (immediate)
        {
            ApplyRuntimeVisibilityInstant();
        }
    }

    public void EnsureVisibleForIncomingPickup()
    {
        BeginIncomingPickupPresentation();
    }

    public void BeginIncomingPickupPresentation()
    {
        EnsureCanvasGroup();
        EnsureSlideToggle();

        if (pickupPresentationLockCount == 0)
        {
            shouldRestoreCollapsedAfterPickupPresentation = isCollapsed;
        }

        pickupPresentationLockCount++;
        isRuntimeVisible = true;
        SetCollapsedState(false, true);
        ApplyRuntimeVisibilityInstant();
        RefreshToggleHintVisual();
    }

    public void EndIncomingPickupPresentation()
    {
        if (pickupPresentationLockCount <= 0)
        {
            return;
        }

        pickupPresentationLockCount--;
        if (pickupPresentationLockCount > 0)
        {
            return;
        }

        bool shouldCollapseBack = shouldRestoreCollapsedAfterPickupPresentation;
        shouldRestoreCollapsedAfterPickupPresentation = false;

        if (shouldCollapseBack)
        {
            SetCollapsedState(true);
            pickupToggleSuppressUntilTime = Time.unscaledTime + SlideSmoothTime + 0.03f;
        }

        RefreshToggleHintVisual();
    }

    public bool TryGetSlotScreenPosition(int slotIndex, out Vector2 screenPosition, out Vector2 slotSize)
    {
        screenPosition = default;
        slotSize = default;

        RectTransform slotRect = ResolveCurrentRuntimeSlotRect(slotIndex);
        if (slotRect == null)
        {
            return false;
        }

        Canvas parentCanvas = slotRect.GetComponentInParent<Canvas>();
        Camera canvasCamera = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? parentCanvas.worldCamera
            : null;

        screenPosition = RectTransformUtility.WorldToScreenPoint(
            canvasCamera,
            slotRect.TransformPoint(slotRect.rect.center));
        slotSize = Vector2.Scale(slotRect.rect.size, slotRect.lossyScale);
        return true;
    }

    private RectTransform ResolveCurrentRuntimeSlotRect(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= RuntimeSlotCount)
        {
            return null;
        }

        if (runtimeSlotRects != null &&
            slotIndex < runtimeSlotRects.Length &&
            IsUsableRuntimeSlotRect(runtimeSlotRects[slotIndex]))
        {
            return runtimeSlotRects[slotIndex];
        }

        if (backPackGrid == null || slotIndex >= backPackGrid.Length || backPackGrid[slotIndex] == null)
        {
            return null;
        }

        RectTransform fallbackRect = backPackGrid[slotIndex].rectTransform;
        return IsUsableRuntimeSlotRect(fallbackRect) ? fallbackRect : null;
    }

    private bool IsUsableRuntimeSlotRect(RectTransform slotRect)
    {
        return slotRect != null &&
               slotRect.gameObject.activeInHierarchy &&
               IsBackpackOwnedTransform(slotRect);
    }

    private void ResolveBackpackManager()
    {
        if (backpack == null)
        {
            backpack = BackpackMananger.Instance;
        }

        if (backpack == null)
        {
            GameObject manager = new GameObject("RuntimeBackpackManager");
            backpack = manager.AddComponent<BackpackMananger>();
            Debug.Log("Created runtime BackpackMananger for BackpackUI");
        }
    }

    private bool EnsureRuntimeRootOwnership()
    {
        RectTransform runtimeCanvasRoot = ResolveRuntimeCanvasRoot();
        if (runtimeCanvasRoot == null)
        {
            runtimeCanvasRoot = CreateRuntimeCanvasRoot();
        }

        if (runtimeCanvasRoot == null || runtimeCanvasRoot.gameObject == gameObject)
        {
            return true;
        }

        BackpackUI rootView = runtimeCanvasRoot.GetComponent<BackpackUI>();
        if (rootView == null)
        {
            rootView = runtimeCanvasRoot.gameObject.AddComponent<BackpackUI>();
        }

        rootView.ConfigureRuntimeLayout();
        rootView.RefreshUI();
        enabled = false;
        return false;
    }

    private void EnsureRuntimeCanvasVisible()
    {
        RectTransform runtimeCanvasRoot = ResolveRuntimeCanvasRoot();
        if (runtimeCanvasRoot == null)
        {
            return;
        }

        if (runtimeCanvasRoot.localScale == Vector3.zero)
        {
            runtimeCanvasRoot.localScale = Vector3.one;
        }

        ConfigureRuntimeCanvasRoot(runtimeCanvasRoot);
    }

    private void BindRuntimeBackpackLayout()
    {
        RectTransform runtimeCanvasRoot = ResolveRuntimeCanvasRoot();
        if (runtimeCanvasRoot == null)
        {
            return;
        }

        DisableStaleGeneratedSurface(runtimeCanvasRoot);

        RectTransform itemPanel = FindBackpackPanel(runtimeCanvasRoot, RuntimeItemPanelName);
        if (itemPanel == null)
        {
            itemPanel = CreateFallbackItemPanel(runtimeCanvasRoot);
        }

        RectTransform attackPanel = FindBackpackPanel(runtimeCanvasRoot, RuntimeAttackPanelName);
        if (attackPanel == null)
        {
            attackPanel = FindMergedAttackSlot(itemPanel);
        }

        if (attackPanel == null)
        {
            attackPanel = CreateFallbackAttackPanel(runtimeCanvasRoot);
        }

        runtimeLayoutRootRect = ResolveRuntimeLayoutRoot(runtimeCanvasRoot, itemPanel, attackPanel);
        NormalizeSceneAuthoredRuntimeLayout(runtimeLayoutRootRect, runtimeCanvasRoot, itemPanel, attackPanel);

        runtimeAttackSlotRect = attackPanel;
        ApplyRuntimeSlotFrame(attackPanel, ResolveRuntimeSlotFrameSprite(attackPanel));
        EnsureRuntimeAttackSlotBehaviour(attackPanel);
        runtimeAttackSelectionGroup = EnsureRuntimeSelectionFrame(
            attackPanel,
            ResolveSelectionFrameSize(attackPanel),
            RuntimeAttackSelectionBorderColor);

        Image[] runtimeSlotImages = new Image[RuntimeSlotCount];
        runtimeSlotRects = new RectTransform[RuntimeSlotCount];
        runtimeSelectionGroups = new CanvasGroup[RuntimeSlotCount];
        runtimeHoverShadeObjects = new GameObject[RuntimeSlotCount];

        for (int i = 0; i < RuntimeSlotCount; i++)
        {
            RectTransform slot = FindRuntimeSlot(itemPanel, i);
            if (slot == null)
            {
                slot = CreateFallbackSlot(itemPanel, i);
            }

            runtimeSlotRects[i] = slot;

            ApplyRuntimeSlotFrame(slot, ResolveRuntimeSlotFrameSprite(slot));
            EnsureSlotRaycastSurface(slot);

            runtimeSlotImages[i] = EnsureRuntimeSlotIcon(slot);
            EnsureRuntimeBackpackSlotBehaviour(slot, i, runtimeSlotImages[i]);
            EnsureRuntimeSlotHoverShade(slot, i);
            runtimeSelectionGroups[i] = EnsureRuntimeSelectionFrame(
                slot,
                ResolveSelectionFrameSize(slot),
                RuntimeBackpackSelectionBorderColor);
        }

        if (backPackGrid != runtimeSlotImages)
        {
            backPackGrid = runtimeSlotImages;
        }

        RefreshSelectionVisuals();
    }

    private RectTransform ResolveRuntimeCanvasRoot()
    {
        if (transform is RectTransform selfRect &&
            string.Equals(gameObject.name, RuntimeCanvasRootName) &&
            IsPreferredRuntimeCanvasRoot(selfRect))
        {
            return selfRect;
        }

        return FindRuntimeCanvasRoot();
    }

    private static void ConfigureRuntimeCanvasRoot(RectTransform canvasRoot)
    {
        if (canvasRoot == null)
        {
            return;
        }

        canvasRoot.anchorMin = Vector2.zero;
        canvasRoot.anchorMax = Vector2.zero;
        canvasRoot.pivot = Vector2.zero;
        canvasRoot.anchoredPosition = Vector2.zero;
        canvasRoot.sizeDelta = Vector2.zero;

        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = canvasRoot.gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = RuntimeCanvasSortingOrder;

        CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvasRoot.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        if (canvasRoot.GetComponent<GraphicRaycaster>() == null)
        {
            canvasRoot.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private static RectTransform FindRuntimeCanvasRoot()
    {
        RectTransform[] rectTransforms = FindObjectsOfType<RectTransform>(true);
        Scene activeScene = SceneManager.GetActiveScene();
        RectTransform activeSceneCandidate = null;
        int activeSceneScore = int.MinValue;
        RectTransform fallbackCandidate = null;
        int fallbackScore = int.MinValue;

        for (int i = 0; i < rectTransforms.Length; i++)
        {
            RectTransform rect = rectTransforms[i];
            if (rect != null && string.Equals(rect.gameObject.name, RuntimeCanvasRootName))
            {
                int score = GetRuntimeCanvasRootScore(rect);
                if (rect.gameObject.scene == activeScene)
                {
                    if (score > activeSceneScore)
                    {
                        activeSceneCandidate = rect;
                        activeSceneScore = score;
                    }

                    continue;
                }

                if (!IllustratedUISceneLoader.IsIllustratedUiScene(rect.gameObject.scene) &&
                    score > fallbackScore)
                {
                    fallbackCandidate = rect;
                    fallbackScore = score;
                }
            }
        }

        return activeSceneCandidate != null ? activeSceneCandidate : fallbackCandidate;
    }

    private static int GetRuntimeCanvasRootScore(RectTransform rect)
    {
        int score = 0;
        if (rect == null)
        {
            return score;
        }

        if (rect.gameObject.activeInHierarchy)
        {
            score += 100;
        }
        else if (rect.gameObject.activeSelf)
        {
            score += 40;
        }

        if (FindNamedChildRecursive(rect, RuntimeItemPanelName) != null)
        {
            score += 20;
        }

        if (FindNamedChildRecursive(rect, RuntimeAttackPanelName) != null ||
            FindNamedChildRecursive(rect, $"{RuntimeSlotPrefix}{RuntimeSlotCount + 1}") != null)
        {
            score += 10;
        }

        if (rect.GetComponent<BackpackUI>() != null)
        {
            score += 5;
        }

        return score;
    }

    private static bool IsPreferredRuntimeCanvasRoot(RectTransform rect)
    {
        if (rect == null)
        {
            return false;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (rect.gameObject.scene == activeScene)
        {
            return true;
        }

        return !IllustratedUISceneLoader.IsIllustratedUiScene(rect.gameObject.scene);
    }

    private static RectTransform CreateRuntimeCanvasRoot()
    {
        GameObject rootObject = new GameObject(
            RuntimeCanvasRootName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        rootObject.layer = LayerMask.NameToLayer("UI");

        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        ConfigureRuntimeCanvasRoot(rootRect);
        return rootRect;
    }

    private static RectTransform FindNamedChildRecursive(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (string.Equals(child.name, name))
            {
                return child as RectTransform;
            }

            RectTransform nested = FindNamedChildRecursive(child, name);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static RectTransform EnsureChildRect(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null && existing is RectTransform existingRect)
        {
            return existingRect;
        }

        GameObject childObject = new GameObject(childName, typeof(RectTransform));
        childObject.layer = parent.gameObject.layer;
        RectTransform childRect = childObject.GetComponent<RectTransform>();
        childRect.SetParent(parent, false);
        return childRect;
    }

    private static RectTransform FindBackpackPanel(Transform root, string panelName)
    {
        Transform directChild = root != null ? root.Find(panelName) : null;
        if (directChild is RectTransform directRect)
        {
            directRect.gameObject.SetActive(true);
            return directRect;
        }

        RectTransform nestedRect = FindNamedChildRecursive(root, panelName);
        if (nestedRect != null)
        {
            nestedRect.gameObject.SetActive(true);
        }

        return nestedRect;
    }

    private static RectTransform ResolveRuntimeLayoutRoot(
        RectTransform runtimeCanvasRoot,
        RectTransform itemPanel,
        RectTransform attackPanel)
    {
        if (runtimeCanvasRoot == null)
        {
            return null;
        }

        Transform itemParent = itemPanel != null ? itemPanel.parent : null;
        Transform attackParent = attackPanel != null ? attackPanel.parent : null;
        if (itemParent != null &&
            itemParent == attackParent &&
            itemParent != runtimeCanvasRoot &&
            itemParent is RectTransform sharedParent)
        {
            sharedParent.gameObject.SetActive(true);
            return sharedParent;
        }

        if (itemPanel != null &&
            attackPanel != null &&
            attackPanel.IsChildOf(itemPanel) &&
            itemParent != null &&
            itemParent != runtimeCanvasRoot &&
            itemParent is RectTransform mergedSlotSharedParent)
        {
            mergedSlotSharedParent.gameObject.SetActive(true);
            return mergedSlotSharedParent;
        }

        return runtimeCanvasRoot;
    }

    private static void NormalizeSceneAuthoredRuntimeLayout(
        RectTransform layoutRoot,
        RectTransform runtimeCanvasRoot,
        RectTransform itemPanel,
        RectTransform attackPanel)
    {
        if (layoutRoot == null || runtimeCanvasRoot == null)
        {
            return;
        }

        if (layoutRoot != runtimeCanvasRoot)
        {
            layoutRoot.anchorMin = new Vector2(0.5f, 0f);
            layoutRoot.anchorMax = new Vector2(0.5f, 0f);
            layoutRoot.pivot = new Vector2(0.5f, 0f);
            layoutRoot.anchoredPosition = new Vector2(0f, RuntimeSceneAuthoredLayoutBottom);
            layoutRoot.sizeDelta = ResolveSceneAuthoredLayoutSize(itemPanel, attackPanel);
            layoutRoot.localScale = Vector3.one;
            layoutRoot.localRotation = Quaternion.identity;
            layoutRoot.gameObject.SetActive(true);

            ClearLegacyBackpackBackground(layoutRoot);
        }

        ClearLegacyBackpackBackground(itemPanel);
        NormalizeSceneAuthoredItemPanel(itemPanel);
        NormalizeSceneAuthoredAttackPanel(attackPanel);
        NormalizeMergedSevenSlotRow(itemPanel, attackPanel);
    }

    private static Vector2 ResolveSceneAuthoredLayoutSize(RectTransform itemPanel, RectTransform attackPanel)
    {
        float halfWidth = RuntimeSceneAuthoredLayoutWidth * 0.5f;
        float height = RuntimeSceneAuthoredLayoutHeight;

        ExpandLayoutBounds(itemPanel, ref halfWidth, ref height);
        ExpandLayoutBounds(attackPanel, ref halfWidth, ref height);

        return new Vector2(halfWidth * 2f, height);
    }

    private static void ExpandLayoutBounds(RectTransform rectTransform, ref float halfWidth, ref float height)
    {
        if (rectTransform == null)
        {
            return;
        }

        Vector2 size = ResolveRectSize(rectTransform);
        Vector2 position = rectTransform.anchoredPosition;
        Vector2 pivot = rectTransform.pivot;

        float left = position.x - size.x * pivot.x;
        float right = position.x + size.x * (1f - pivot.x);
        float bottom = position.y - size.y * pivot.y;
        float top = position.y + size.y * (1f - pivot.y);

        halfWidth = Mathf.Max(halfWidth, Mathf.Abs(left), Mathf.Abs(right));
        height = Mathf.Max(height, top, -bottom + RuntimeFallbackSurfaceBottom);
    }

    private static void NormalizeSceneAuthoredItemPanel(RectTransform itemPanel)
    {
        if (itemPanel == null)
        {
            return;
        }

        itemPanel.anchorMin = new Vector2(0.5f, 0f);
        itemPanel.anchorMax = new Vector2(0.5f, 0f);
        itemPanel.pivot = new Vector2(0.5f, 0f);
        itemPanel.anchoredPosition = new Vector2(itemPanel.anchoredPosition.x, 0f);
        itemPanel.sizeDelta = new Vector2(RuntimeFallbackItemPanelWidth, RuntimeFallbackItemPanelHeight);
        itemPanel.localScale = Vector3.one;
        itemPanel.localRotation = Quaternion.identity;
        itemPanel.gameObject.SetActive(true);
        ApplyRuntimeBackpackFrame(itemPanel);
    }

    private static void NormalizeSceneAuthoredAttackPanel(RectTransform attackPanel)
    {
        if (attackPanel == null)
        {
            return;
        }

        Vector2 size = ResolveRectSize(attackPanel);
        attackPanel.anchorMin = new Vector2(0.5f, 0f);
        attackPanel.anchorMax = new Vector2(0.5f, 0f);
        attackPanel.pivot = new Vector2(0.5f, 0.5f);
        attackPanel.anchoredPosition = new Vector2(
            attackPanel.anchoredPosition.x,
            Mathf.Max(attackPanel.anchoredPosition.y, size.y * 0.5f));
        attackPanel.localScale = Vector3.one;
        attackPanel.localRotation = Quaternion.identity;
        attackPanel.gameObject.SetActive(true);
    }

    private static void NormalizeMergedSevenSlotRow(
        RectTransform itemPanel,
        RectTransform attackPanel)
    {
        if (itemPanel == null)
        {
            return;
        }

        if (attackPanel != null && attackPanel != itemPanel && !attackPanel.IsChildOf(itemPanel))
        {
            attackPanel.SetParent(itemPanel, false);
        }

        Sprite slotSprite = HasRuntimeBackpackFrame(itemPanel) ? null : LoadRuntimeFallbackSlotSprite();
        for (int i = 0; i < RuntimeSlotCount + 1; i++)
        {
            RectTransform slot = i < RuntimeSlotCount
                ? FindRuntimeSlot(itemPanel, i) ?? EnsureChildRect(itemPanel, $"{RuntimeSlotPrefix}{i + 1}")
                : (attackPanel != null
                    ? attackPanel
                    : FindMergedAttackSlot(itemPanel) ?? EnsureChildRect(itemPanel, $"{RuntimeSlotPrefix}{RuntimeSlotCount + 1}"));

            if (slot == null)
            {
                continue;
            }

            slot.anchorMin = new Vector2(0.5f, 0.5f);
            slot.anchorMax = new Vector2(0.5f, 0.5f);
            slot.pivot = new Vector2(0.5f, 0.5f);
            slot.anchoredPosition = new Vector2(
                (i - RuntimeSlotCount * 0.5f) * RuntimeFallbackMergedSlotGapX,
                0f);
            slot.sizeDelta = new Vector2(RuntimeFallbackMergedSlotWidth, RuntimeFallbackMergedSlotHeight);
            slot.localScale = Vector3.one;
            slot.localRotation = Quaternion.identity;
            slot.gameObject.SetActive(true);

            ApplyRuntimeSlotFrame(slot, slotSprite);
        }
    }

    private static RectTransform FindRuntimeSlot(Transform itemPanel, int slotIndex)
    {
        string slotName = $"{RuntimeSlotPrefix}{slotIndex + 1}";
        Transform directChild = itemPanel != null ? itemPanel.Find(slotName) : null;
        if (directChild is RectTransform directRect)
        {
            directRect.gameObject.SetActive(true);
            return directRect;
        }

        RectTransform nestedRect = FindNamedChildRecursive(itemPanel, slotName);
        if (nestedRect != null)
        {
            nestedRect.gameObject.SetActive(true);
        }

        return nestedRect;
    }

    private static RectTransform FindMergedAttackSlot(Transform itemPanel)
    {
        if (itemPanel == null)
        {
            return null;
        }

        string slotName = $"{RuntimeSlotPrefix}{RuntimeSlotCount + 1}";
        Transform directChild = itemPanel.Find(slotName);
        if (directChild is RectTransform directRect)
        {
            directRect.gameObject.SetActive(true);
            return directRect;
        }

        RectTransform nestedRect = FindNamedChildRecursive(itemPanel, slotName);
        if (nestedRect != null)
        {
            nestedRect.gameObject.SetActive(true);
        }

        return nestedRect;
    }

    private static RectTransform CreateFallbackItemPanel(Transform runtimeCanvasRoot)
    {
        RectTransform sharedPanel = EnsureFallbackSharedPanel(runtimeCanvasRoot);
        RectTransform panel = EnsureChildRect(sharedPanel, RuntimeItemPanelName);
        panel.anchorMin = new Vector2(0.5f, 0f);
        panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(RuntimeFallbackItemPanelWidth, RuntimeFallbackItemPanelHeight);
        panel.localScale = Vector3.one;
        panel.gameObject.SetActive(true);

        ApplyRuntimeBackpackFrame(panel);
        EnsureFallbackMergedSlots(panel);
        return panel;
    }

    private static RectTransform EnsureFallbackSharedPanel(Transform runtimeCanvasRoot)
    {
        RectTransform sharedPanel = EnsureChildRect(runtimeCanvasRoot, RuntimeSharedPanelName);
        sharedPanel.anchorMin = new Vector2(0.5f, 0f);
        sharedPanel.anchorMax = new Vector2(0.5f, 0f);
        sharedPanel.pivot = new Vector2(0.5f, 0f);
        sharedPanel.anchoredPosition = new Vector2(0f, RuntimeSceneAuthoredLayoutBottom);
        sharedPanel.sizeDelta = new Vector2(RuntimeSceneAuthoredLayoutWidth, RuntimeSceneAuthoredLayoutHeight);
        sharedPanel.localScale = Vector3.one;
        sharedPanel.localRotation = Quaternion.identity;
        sharedPanel.gameObject.SetActive(true);
        return sharedPanel;
    }

    private static void EnsureFallbackMergedSlots(Transform itemPanel)
    {
        RectTransform itemPanelRect = itemPanel as RectTransform;
        Sprite slotSprite = HasRuntimeBackpackFrame(itemPanelRect) ? null : LoadRuntimeFallbackSlotSprite();
        for (int i = 0; i < RuntimeSlotCount + 1; i++)
        {
            RectTransform slot = EnsureChildRect(itemPanel, $"{RuntimeSlotPrefix}{i + 1}");
            slot.anchorMin = new Vector2(0.5f, 0.5f);
            slot.anchorMax = new Vector2(0.5f, 0.5f);
            slot.pivot = new Vector2(0.5f, 0.5f);
            slot.anchoredPosition = new Vector2(
                (i - RuntimeSlotCount * 0.5f) * RuntimeFallbackMergedSlotGapX,
                0f);
            slot.sizeDelta = new Vector2(RuntimeFallbackMergedSlotWidth, RuntimeFallbackMergedSlotHeight);
            slot.localScale = Vector3.one;
            slot.localRotation = Quaternion.identity;
            slot.gameObject.SetActive(true);

            ApplyRuntimeSlotFrame(slot, slotSprite);
        }
    }

    private static Sprite LoadRuntimeFallbackSlotSprite()
    {
        if (runtimeFallbackSlotSprite != null)
        {
            return runtimeFallbackSlotSprite;
        }

        runtimeFallbackSlotSprite = Resources.Load<Sprite>(RuntimeFallbackSlotSpriteResourcePath);
        return runtimeFallbackSlotSprite;
    }

    private static Sprite LoadRuntimeBackpackFrameSprite()
    {
        if (runtimeBackpackFrameSprite != null)
        {
            return runtimeBackpackFrameSprite;
        }

        runtimeBackpackFrameSprite = Resources.Load<Sprite>(RuntimeBackpackFrameSpriteResourcePath);
        return runtimeBackpackFrameSprite;
    }

    private static void ApplyRuntimeBackpackFrame(RectTransform itemPanel)
    {
        if (itemPanel == null)
        {
            return;
        }

        Image image = itemPanel.GetComponent<Image>();
        if (image == null)
        {
            image = itemPanel.gameObject.AddComponent<Image>();
        }

        Sprite frameSprite = LoadRuntimeBackpackFrameSprite();
        if (frameSprite != null)
        {
            image.sprite = frameSprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
        }
        else
        {
            image.sprite = null;
            image.color = Color.clear;
        }

        image.enabled = true;
        image.raycastTarget = false;
        image.preserveAspect = false;
    }

    private static bool HasRuntimeBackpackFrame(RectTransform itemPanel)
    {
        if (itemPanel == null)
        {
            return false;
        }

        Sprite frameSprite = LoadRuntimeBackpackFrameSprite();
        Image image = itemPanel.GetComponent<Image>();
        return frameSprite != null && image != null && image.sprite == frameSprite && image.color.a > 0f;
    }

    private static Sprite ResolveRuntimeSlotFrameSprite(RectTransform slot)
    {
        RectTransform itemPanel = FindOwningItemPanel(slot);
        return HasRuntimeBackpackFrame(itemPanel) ? null : LoadRuntimeFallbackSlotSprite();
    }

    private static RectTransform FindOwningItemPanel(RectTransform slot)
    {
        Transform current = slot;
        while (current != null)
        {
            if (string.Equals(current.name, RuntimeItemPanelName))
            {
                return current as RectTransform;
            }

            current = current.parent;
        }

        return null;
    }

    private static RectTransform CreateFallbackAttackPanel(Transform runtimeCanvasRoot)
    {
        RectTransform panel = EnsureChildRect(runtimeCanvasRoot, RuntimeAttackPanelName);
        panel.anchorMin = new Vector2(0.5f, 0f);
        panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = new Vector2(RuntimeFallbackAttackPanelX, RuntimeFallbackAttackPanelY);
        panel.sizeDelta = new Vector2(RuntimeFallbackAttackPanelSize, RuntimeFallbackAttackPanelSize);
        panel.localScale = Vector3.one;
        panel.gameObject.SetActive(true);

        Image image = panel.GetComponent<Image>();
        if (image == null)
        {
            image = panel.gameObject.AddComponent<Image>();
        }

        if (image.sprite == null)
        {
            image.color = new Color(0.25f, 0.19f, 0.13f, 0.96f);
        }

        image.raycastTarget = true;
        image.preserveAspect = false;
        ApplyRuntimeSlotFrame(panel);
        return panel;
    }

    private static RectTransform CreateFallbackSlot(Transform itemPanel, int slotIndex)
    {
        RectTransform slot = EnsureChildRect(itemPanel, $"{RuntimeSlotPrefix}{slotIndex + 1}");
        slot.anchorMin = new Vector2(0.5f, 0.5f);
        slot.anchorMax = new Vector2(0.5f, 0.5f);
        slot.pivot = new Vector2(0.5f, 0.5f);
        slot.anchoredPosition = new Vector2(
            (slotIndex - (RuntimeSlotCount - 1) * 0.5f) * RuntimeFallbackSlotGapX,
            0f);
        slot.sizeDelta = new Vector2(RuntimeFallbackSlotSize, RuntimeFallbackSlotSize);
        slot.localScale = Vector3.one;
        slot.gameObject.SetActive(true);
        return slot;
    }

    private static void ClearLegacyBackpackBackground(RectTransform target)
    {
        if (target == null)
        {
            return;
        }

        Image image = target.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        image.sprite = null;
        image.color = Color.clear;
        image.raycastTarget = false;
        image.preserveAspect = false;
    }

    private static void ApplyRuntimeSlotFrame(RectTransform slot)
    {
        ApplyRuntimeSlotFrame(slot, LoadRuntimeFallbackSlotSprite());
    }

    private static void ApplyRuntimeSlotFrame(
        RectTransform slot,
        Sprite slotSprite)
    {
        if (slot == null)
        {
            return;
        }

        Image slotImage = slot.GetComponent<Image>();
        if (slotImage == null)
        {
            slotImage = slot.gameObject.AddComponent<Image>();
        }

        if (slotSprite != null)
        {
            slotImage.sprite = slotSprite;
            slotImage.type = Image.Type.Simple;
            slotImage.color = Color.white;
        }
        else
        {
            slotImage.sprite = null;
            slotImage.type = Image.Type.Simple;
            slotImage.color = new Color(1f, 1f, 1f, 0.001f);
        }

        slotImage.enabled = true;
        slotImage.raycastTarget = true;
        slotImage.preserveAspect = false;
    }

    private static void EnsureSlotRaycastSurface(RectTransform slot)
    {
        Image slotImage = slot.GetComponent<Image>();
        if (slotImage == null)
        {
            slotImage = slot.gameObject.AddComponent<Image>();
            slotImage.color = new Color(1f, 1f, 1f, 0.001f);
        }
        else if (slotImage.sprite == null && slotImage.color.a <= 0f)
        {
            slotImage.color = new Color(slotImage.color.r, slotImage.color.g, slotImage.color.b, 0.001f);
        }

        slotImage.enabled = true;
        slotImage.raycastTarget = true;

        DisableLegacySlotClickChildren(slot);
    }

    private static void DisableLegacySlotClickChildren(RectTransform slot)
    {
        for (int i = 0; i < slot.childCount; i++)
        {
            Transform child = slot.GetChild(i);
            if (child == null ||
                string.Equals(child.name, RuntimeSlotIconName) ||
                string.Equals(child.name, RuntimeSlotSelectionName) ||
                string.Equals(child.name, RuntimeSlotHoverShadeName))
            {
                continue;
            }

            Button button = child.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = false;
                button.enabled = false;
            }

            Image image = child.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = false;
            }
        }
    }

    private static Image EnsureRuntimeSlotIcon(RectTransform slot)
    {
        Transform existing = slot.Find(RuntimeSlotIconName);
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
            GameObject iconObject = new GameObject(RuntimeSlotIconName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(slot, false);
            iconImage = iconObject.GetComponent<Image>();
        }

        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        iconRect.SetAsLastSibling();

        iconImage.raycastTarget = false;
        iconImage.preserveAspect = false;
        return iconImage;
    }

    private void EnsureRuntimeBackpackSlotBehaviour(RectTransform slot, int slotIndex, Image iconImage)
    {
        BackpackSlot slotBehaviour = slot.GetComponent<BackpackSlot>();
        if (slotBehaviour == null)
        {
            slotBehaviour = slot.gameObject.AddComponent<BackpackSlot>();
        }

        slotBehaviour.slotIndex = slotIndex;
        slotBehaviour.BindRuntimeVisual(this, iconImage);
    }

    private void EnsureRuntimeAttackSlotBehaviour(RectTransform attackPanel)
    {
        if (attackPanel == null)
        {
            return;
        }

        Image image = attackPanel.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
        }

        Button button = attackPanel.GetComponent<Button>();
        if (button == null)
        {
            button = attackPanel.gameObject.AddComponent<Button>();
        }

        button.transition = Selectable.Transition.None;
        button.onClick.RemoveListener(SelectAttackSlot);
        button.onClick.AddListener(SelectAttackSlot);
    }

    private CanvasGroup EnsureRuntimeSelectionFrame(RectTransform target, Vector2 frameSize, Color borderColor)
    {
        Transform existing = target.Find(RuntimeSlotSelectionName);
        RectTransform selectionRect;

        if (existing != null)
        {
            selectionRect = existing as RectTransform;
        }
        else
        {
            GameObject selectionObject = new GameObject(RuntimeSlotSelectionName, typeof(RectTransform));
            selectionObject.layer = target.gameObject.layer;
            selectionRect = selectionObject.GetComponent<RectTransform>();
            selectionRect.SetParent(target, false);
        }

        selectionRect.anchorMin = new Vector2(0.5f, 0.5f);
        selectionRect.anchorMax = new Vector2(0.5f, 0.5f);
        selectionRect.pivot = new Vector2(0.5f, 0.5f);
        selectionRect.anchoredPosition = Vector2.zero;
        selectionRect.sizeDelta = frameSize;
        selectionRect.SetAsLastSibling();
        selectionRect.gameObject.SetActive(true);

        Image legacyImage = selectionRect.GetComponent<Image>();
        if (legacyImage != null)
        {
            legacyImage.enabled = false;
            legacyImage.raycastTarget = false;
        }

        Outline legacyOutline = selectionRect.GetComponent<Outline>();
        if (legacyOutline != null)
        {
            legacyOutline.enabled = false;
        }

        EnsureSelectionLine(selectionRect, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -RuntimeSelectionBorderThickness), Vector2.zero, borderColor);
        EnsureSelectionLine(selectionRect, "Bottom", Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, RuntimeSelectionBorderThickness), borderColor);
        EnsureSelectionLine(selectionRect, "Left", Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(RuntimeSelectionBorderThickness, 0f), borderColor);
        EnsureSelectionLine(selectionRect, "Right", new Vector2(1f, 0f), Vector2.one, new Vector2(-RuntimeSelectionBorderThickness, 0f), Vector2.zero, borderColor);

        CanvasGroup selectionGroup = selectionRect.GetComponent<CanvasGroup>();
        if (selectionGroup == null)
        {
            selectionGroup = selectionRect.gameObject.AddComponent<CanvasGroup>();
            selectionGroup.alpha = 0f;
        }

        selectionGroup.interactable = false;
        selectionGroup.blocksRaycasts = false;
        return selectionGroup;
    }

    private void EnsureRuntimeSlotHoverShade(RectTransform slot, int slotIndex)
    {
        Transform existing = slot.Find(RuntimeSlotHoverShadeName);
        RectTransform shadeRect;
        Image shadeImage;

        if (existing != null)
        {
            shadeRect = existing as RectTransform;
            shadeImage = existing.GetComponent<Image>();
            if (shadeImage == null)
            {
                shadeImage = existing.gameObject.AddComponent<Image>();
            }
        }
        else
        {
            GameObject shadeObject = new GameObject(RuntimeSlotHoverShadeName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            shadeObject.layer = slot.gameObject.layer;
            shadeRect = shadeObject.GetComponent<RectTransform>();
            shadeRect.SetParent(slot, false);
            shadeImage = shadeObject.GetComponent<Image>();
        }

        shadeRect.anchorMin = new Vector2(0.5f, 0.5f);
        shadeRect.anchorMax = new Vector2(0.5f, 0.5f);
        shadeRect.pivot = new Vector2(0.5f, 0.5f);
        shadeRect.anchoredPosition = Vector2.zero;
        shadeRect.sizeDelta = ResolveRectSize(slot);
        shadeRect.SetAsLastSibling();

        shadeImage.color = RuntimeHoverShadeColor;
        shadeImage.raycastTarget = false;
        shadeImage.preserveAspect = false;
        shadeRect.gameObject.SetActive(false);

        runtimeHoverShadeObjects[slotIndex] = shadeRect.gameObject;
    }

    private static void EnsureSelectionLine(
        RectTransform parent,
        string lineName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        Color borderColor)
    {
        Transform existing = parent.Find(lineName);
        RectTransform lineRect;
        Image lineImage;

        if (existing != null)
        {
            lineRect = existing as RectTransform;
            lineImage = existing.GetComponent<Image>();
            if (lineImage == null)
            {
                lineImage = existing.gameObject.AddComponent<Image>();
            }
        }
        else
        {
            GameObject lineObject = new GameObject(lineName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lineObject.layer = parent.gameObject.layer;
            lineRect = lineObject.GetComponent<RectTransform>();
            lineRect.SetParent(parent, false);
            lineImage = lineObject.GetComponent<Image>();
        }

        lineRect.anchorMin = anchorMin;
        lineRect.anchorMax = anchorMax;
        lineRect.pivot = new Vector2(0.5f, 0.5f);
        lineRect.offsetMin = offsetMin;
        lineRect.offsetMax = offsetMax;
        lineRect.SetAsLastSibling();

        lineImage.color = borderColor;
        lineImage.raycastTarget = false;
    }

    private static Vector2 ResolveSelectionFrameSize(RectTransform target)
    {
        Vector2 size = ResolveRectSize(target);
        return new Vector2(
            size.x + RuntimeSelectionFramePadding * 2f,
            size.y + RuntimeSelectionFramePadding * 2f);
    }

    private static Vector2 ResolveRectSize(RectTransform target)
    {
        if (target == null)
        {
            return new Vector2(RuntimeFallbackSlotSize, RuntimeFallbackSlotSize);
        }

        Vector2 size = target.rect.size;
        if (size.x <= 0f || size.y <= 0f)
        {
            size = target.sizeDelta;
        }

        if (size.x <= 0f || size.y <= 0f)
        {
            size = new Vector2(RuntimeFallbackSlotSize, RuntimeFallbackSlotSize);
        }

        return size;
    }

    private static void DisableStaleGeneratedSurface(Transform runtimeCanvasRoot)
    {
        Transform staleSurface = runtimeCanvasRoot != null ? runtimeCanvasRoot.Find(RuntimeSurfaceName) : null;
        if (staleSurface != null)
        {
            staleSurface.gameObject.SetActive(false);
        }
    }

    private void HandleSlotSelectionInput()
    {
        if (!CanAcceptSlotSelectionInput())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
        {
            if (IsExternalUiReceivingInput())
            {
                return;
            }

            SelectAttackSlot();
            return;
        }

        for (int i = 0; i < RuntimeSlotCount; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)) ||
                Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad1 + i)))
            {
                if (IsExternalUiReceivingInput())
                {
                    return;
                }

                SelectSlot(i);
                return;
            }
        }

        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) <= 0.01f)
        {
            return;
        }

        int direction = wheel > 0f ? -1 : 1;
        if (IsExternalUiReceivingInput())
        {
            return;
        }

        int nextSlotIndex = selectedSlotIndex == RuntimeAttackSelectionIndex
            ? (direction > 0 ? 0 : RuntimeSlotCount - 1)
            : (selectedSlotIndex + direction + RuntimeSlotCount) % RuntimeSlotCount;
        SelectSlot(nextSlotIndex);
    }

    private bool CanAcceptSlotSelectionInput()
    {
        if (!isRuntimeVisible || IsPickupPresentationLocked)
        {
            return false;
        }

        if (RuntimePauseMenu.IsPauseOpen)
        {
            return false;
        }

        return UIRootManager.Instance == null || !UIRootManager.Instance.IsAnyGameplayBlockingUIOpen();
    }

    private void RefreshSelectionVisuals()
    {
        if (runtimeSelectionGroups == null)
        {
            return;
        }

        if (runtimeAttackSlotRect != null)
        {
            runtimeAttackSlotRect.localScale = Vector3.one;
        }

        for (int i = 0; i < RuntimeSlotCount; i++)
        {
            bool selected = i == selectedSlotIndex;
            if (runtimeSlotRects != null && i < runtimeSlotRects.Length && runtimeSlotRects[i] != null)
            {
                runtimeSlotRects[i].localScale = Vector3.one;
            }
        }
    }

    private void UpdateSelectionFade(float deltaTime)
    {
        float step = Mathf.Max(0.001f, RuntimeSelectionFadeSpeed * deltaTime);
        UpdateSelectionGroupFade(runtimeAttackSelectionGroup, selectedSlotIndex == RuntimeAttackSelectionIndex, step);

        if (runtimeSelectionGroups == null)
        {
            return;
        }

        for (int i = 0; i < RuntimeSlotCount; i++)
        {
            UpdateSelectionGroupFade(runtimeSelectionGroups[i], i == selectedSlotIndex, step);
        }
    }

    private static void UpdateSelectionGroupFade(CanvasGroup selectionGroup, bool selected, float step)
    {
        if (selectionGroup == null)
        {
            return;
        }

        float targetAlpha = selected ? 1f : 0f;
        selectionGroup.alpha = Mathf.MoveTowards(selectionGroup.alpha, targetAlpha, step);
    }

    public void SetSlotHover(int slotIndex, bool hovered)
    {
        if (slotIndex < 0 || slotIndex >= RuntimeSlotCount || runtimeHoverShadeObjects == null)
        {
            return;
        }

        if (runtimeHoverShadeObjects[slotIndex] != null)
        {
            runtimeHoverShadeObjects[slotIndex].SetActive(hovered);
        }
    }

    private bool IsExternalUiReceivingInput()
    {
        if (GameDebugPageBootstrapper.IsAnyPanelOpen)
        {
            return true;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return false;
        }

        GameObject selectedObject = eventSystem.currentSelectedGameObject;
        if (selectedObject != null &&
            selectedObject.activeInHierarchy &&
            !IsBackpackOwnedTransform(selectedObject.transform))
        {
            return true;
        }

        pointerRaycastResults.Clear();
        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };
        eventSystem.RaycastAll(pointerData, pointerRaycastResults);

        for (int i = 0; i < pointerRaycastResults.Count; i++)
        {
            GameObject target = pointerRaycastResults[i].gameObject;
            if (target == null || !target.activeInHierarchy)
            {
                continue;
            }

            if (IsBackpackOwnedTransform(target.transform))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool IsBackpackOwnedTransform(Transform target)
    {
        return target != null &&
               (target == transform ||
                target.IsChildOf(transform) ||
                (runtimeLayoutRootRect != null && target.IsChildOf(runtimeLayoutRootRect)) ||
                (toggleHotspotRect != null && target.IsChildOf(toggleHotspotRect)));
    }

    private void EnsureSlideToggle()
    {
        rectTransform ??= transform as RectTransform;
        if (rectTransform == null)
        {
            return;
        }

        if (!slideInitialized)
        {
            expandedAnchoredPosition = rectTransform.anchoredPosition;
            currentTargetPosition = expandedAnchoredPosition;
            slideInitialized = true;
        }

        if (toggleHotspotRect == null)
        {
            Transform existing = transform.Find("BackpackToggleHotspot");
            if (existing != null)
            {
                toggleHotspotRect = existing as RectTransform;
            }
        }

        if (toggleHotspotRect == null)
        {
            GameObject hotspot = new GameObject(
                "BackpackToggleHotspot",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            toggleHotspotRect = hotspot.GetComponent<RectTransform>();
            toggleHotspotRect.SetParent(transform, false);
        }

        toggleHotspotRect.anchorMin = new Vector2(0.5f, 0.5f);
        toggleHotspotRect.anchorMax = new Vector2(0.5f, 0.5f);
        toggleHotspotRect.pivot = new Vector2(0.5f, 0.5f);
        toggleHotspotRect.anchoredPosition = new Vector2(0f, ToggleHotspotY);
        toggleHotspotRect.sizeDelta = new Vector2(ToggleHotspotWidth, ToggleHotspotHeight);
        toggleHotspotRect.SetAsLastSibling();

        toggleHotspotImage = toggleHotspotRect.GetComponent<Image>();
        toggleHotspotImage.raycastTarget = true;

        Button hotspotButton = toggleHotspotRect.GetComponent<Button>();
        hotspotButton.transition = Selectable.Transition.None;
        hotspotButton.onClick.RemoveListener(ToggleCollapsedState);
        hotspotButton.interactable = AllowManualCollapseToggle;
        hotspotButton.enabled = AllowManualCollapseToggle;
        toggleHotspotImage.raycastTarget = AllowManualCollapseToggle;

        if (AllowManualCollapseToggle)
        {
            hotspotButton.onClick.AddListener(ToggleCollapsedState);
        }

        EnsureToggleHintText();

        ApplySlidePositionInstant();
        ApplyRuntimeVisibilityInstant();
        RefreshToggleHintVisual();
    }

    private void ToggleCollapsedState()
    {
        if (IsPickupPresentationLocked)
        {
            return;
        }

        SetCollapsedState(!isCollapsed);
    }

    private void ApplySlidePositionInstant()
    {
        if (rectTransform == null)
        {
            return;
        }

        SyncCurrentTargetPosition();
        slideVelocity = Vector2.zero;
        rectTransform.anchoredPosition = GetAnimatedTargetPosition();
    }

    private void SetCollapsedState(bool collapsed, bool immediate = false)
    {
        isCollapsed = collapsed;
        SyncCurrentTargetPosition();

        if (immediate)
        {
            ApplySlidePositionInstant();
        }
    }

    private void SyncCurrentTargetPosition()
    {
        currentTargetPosition = isCollapsed
            ? expandedAnchoredPosition + new Vector2(0f, -CollapseSlideDistance)
            : expandedAnchoredPosition;
    }

    private void EnsureToggleHintText()
    {
        if (toggleHotspotRect == null)
        {
            return;
        }

        if (toggleHintText == null)
        {
            Transform existing = toggleHotspotRect.Find("HintText");
            if (existing != null)
            {
                toggleHintText = existing.GetComponent<TextMeshProUGUI>();
            }
        }

        if (toggleHintText == null)
        {
            GameObject textObject = new GameObject("HintText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(toggleHotspotRect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            toggleHintText = textObject.GetComponent<TextMeshProUGUI>();
        }

        toggleHintText.font = TmpRuntimeFontFallback.WarmupCharacters("图鉴手册") ?? TMP_Settings.defaultFontAsset;
        toggleHintText.fontSize = 20f;
        toggleHintText.alignment = TextAlignmentOptions.Center;
        toggleHintText.enableWordWrapping = false;
        toggleHintText.raycastTarget = false;
        toggleHintText.text = string.Empty;
    }

    private void RefreshToggleHintVisual()
    {
        if (toggleHotspotRect == null || toggleHotspotImage == null)
        {
            return;
        }

        if (!AllowManualCollapseToggle)
        {
            toggleHotspotRect.localScale = Vector3.one;
            Color hiddenColor = toggleHotspotImage.color;
            hiddenColor.a = 0f;
            toggleHotspotImage.color = hiddenColor;

            if (toggleHintText != null)
            {
                toggleHintText.enabled = false;
            }

            return;
        }

        if (isCollapsed)
        {
            RuntimeUiSpriteFactory.ApplyRoundedSprite(
                toggleHotspotImage,
                ToggleButtonColorCollapsed,
                12,
                14,
                1.4f);

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * CollapsedHintPulseSpeed) * CollapsedHintScalePulse;
            toggleHotspotRect.localScale = new Vector3(pulse, pulse, 1f);

            if (toggleHintText != null)
            {
                toggleHintText.enabled = true;
                toggleHintText.color = ToggleTextColor;
                toggleHintText.text = "展开 [B]";
            }
        }
        else
        {
            RuntimeUiSpriteFactory.ApplyRoundedSprite(
                toggleHotspotImage,
                ToggleButtonColor,
                12,
                14,
                1.3f);

            toggleHotspotRect.localScale = Vector3.one;

            if (toggleHintText != null)
            {
                toggleHintText.enabled = true;
                toggleHintText.color = ToggleTextColor;
                toggleHintText.text = "收起 [B]";
            }
        }

        if (UseFollowTogglePromptStyle())
        {
            // In gameplay scenes, use a compact prompt attached to the player side; the bottom hot zone only keeps click handling.
            toggleHotspotRect.localScale = Vector3.one;
            Color hiddenColor = toggleHotspotImage.color;
            hiddenColor.a = 0f;
            toggleHotspotImage.color = hiddenColor;

            if (toggleHintText != null)
            {
                toggleHintText.enabled = false;
            }
        }
    }

    private void EnsureCanvasGroup()
    {
        if (canvasGroup != null)
        {
            return;
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private Vector2 GetAnimatedTargetPosition()
    {
        return currentTargetPosition + (isRuntimeVisible ? Vector2.zero : new Vector2(0f, -ModalHideSlideDistance));
    }

    private void UpdateFollowTogglePrompt()
    {
        if (!ShouldShowFollowTogglePrompt())
        {
            HideFollowTogglePrompt();
            return;
        }

        Transform playerTransform = ResolvePlayerTransform();
        if (playerTransform == null)
        {
            HideFollowTogglePrompt();
            return;
        }

        RuntimeFollowPromptHud.ShowOrUpdate(
            TogglePromptId,
            playerTransform,
            "B",
            isCollapsed ? "展开背包" : "收起背包",
            1);
    }

    private void HideFollowTogglePrompt()
    {
        RuntimeFollowPromptHud.Hide(TogglePromptId);
    }

    private bool ShouldShowFollowTogglePrompt()
    {
        // Backpack no longer exposes manual collapse/expand controls, so keep the follow prompt closed.
        return false;
    }

    private bool IsPickupPresentationLocked =>
        pickupPresentationLockCount > 0 || Time.unscaledTime < pickupToggleSuppressUntilTime;

    private Transform ResolvePlayerTransform()
    {
        if (cachedPlayerTransform != null)
        {
            return cachedPlayerTransform;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        cachedPlayerTransform = playerObject != null ? playerObject.transform : null;
        return cachedPlayerTransform;
    }

    private static bool UseFollowTogglePromptStyle()
    {
        return GameplayStageCatalog.IsGameplayScene(SceneManager.GetActiveScene().name);
    }

    private void ApplyRuntimeVisibilityInstant()
    {
        if (rectTransform != null)
        {
            slideVelocity = Vector2.zero;
            rectTransform.anchoredPosition = GetAnimatedTargetPosition();
        }

        if (canvasGroup == null)
        {
            return;
        }

        alphaVelocity = 0f;
        canvasGroup.alpha = isRuntimeVisible ? 1f : 0f;
        canvasGroup.interactable = isRuntimeVisible;
        canvasGroup.blocksRaycasts = isRuntimeVisible;
    }

    private void SubscribeRuntimeEvents()
    {
        if (backpack != null)
        {
            backpack.OnInventoryChanged -= RefreshUI;
            backpack.OnInventoryChanged += RefreshUI;
        }

        if (!subscribedToRuntimeState)
        {
            RuntimeProgressState.EnsureInstance().OnStateChanged += RefreshUI;
            subscribedToRuntimeState = true;
        }
    }

    private void UnsubscribeRuntimeEvents()
    {
        if (backpack != null)
        {
            backpack.OnInventoryChanged -= RefreshUI;
        }

        if (subscribedToRuntimeState && RuntimeProgressState.Instance != null)
        {
            RuntimeProgressState.Instance.OnStateChanged -= RefreshUI;
            subscribedToRuntimeState = false;
        }
    }
}
