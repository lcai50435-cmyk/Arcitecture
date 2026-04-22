using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BackpackUI : MonoBehaviour
{
    private const string TogglePromptId = "backpack_toggle";
    private const float ToggleHotspotY = -168f;
    private const float ToggleHotspotWidth = 138f;
    private const float ToggleHotspotHeight = 44f;
    private const float CollapseSlideDistance = 150f;
    private const float SlideSmoothTime = 0.08f;
    private const float ModalHideSlideDistance = 92f;
    private const float VisibilitySmoothTime = 0.12f;
    private const float CollapsedHintScalePulse = 0.04f;
    private const float CollapsedHintPulseSpeed = 4.4f;
    private static readonly Color ToggleButtonColor = new Color(0.17f, 0.12f, 0.07f, 0.88f);
    private static readonly Color ToggleButtonColorCollapsed = new Color(0.20f, 0.13f, 0.05f, 0.94f);
    private static readonly Color ToggleTextColor = new Color(0.98f, 0.89f, 0.67f, 1f);

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

    private void Start()
    {
        ResolveBackpackManager();
        EnsureCanvasGroup();
        EnsureSlideToggle();
        RefreshUI();
    }

    private void OnEnable()
    {
        ResolveBackpackManager();
        SubscribeRuntimeEvents();
        EnsureCanvasGroup();
        EnsureSlideToggle();
        RefreshUI();
    }

    private void OnDisable()
    {
        HideFollowTogglePrompt();
        UnsubscribeRuntimeEvents();
    }

    public void RefreshUI()
    {
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
    }

    private void Update()
    {
        HandleToggleShortcut();

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
    }

    private void HandleToggleShortcut()
    {
        if (!isRuntimeVisible || IsPickupPresentationLocked)
        {
            return;
        }

        if (!Input.GetKeyDown(KeyCode.B))
        {
            return;
        }

        if (RuntimePauseMenu.IsPauseOpen)
        {
            return;
        }

        if (RuntimeMiniMapHud.Instance != null && RuntimeMiniMapHud.Instance.IsExpandedViewVisible)
        {
            return;
        }

        if (UIRootManager.Instance != null && UIRootManager.Instance.IsAnyGameplayBlockingUIOpen())
        {
            return;
        }

        ToggleCollapsedState();
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

        BackpackSlot[] slots = FindObjectsOfType<BackpackSlot>(true);
        for (int i = 0; i < slots.Length; i++)
        {
            BackpackSlot slot = slots[i];
            if (slot != null &&
                slot.slotIndex == slotIndex &&
                slot.transform.IsChildOf(transform) &&
                slot.TryGetScreenCenter(out screenPosition, out slotSize))
            {
                return true;
            }
        }

        if (backPackGrid == null || slotIndex < 0 || slotIndex >= backPackGrid.Length)
        {
            return false;
        }

        Image slotImage = backPackGrid[slotIndex];
        if (slotImage == null)
        {
            return false;
        }

        RectTransform slotRect = slotImage.rectTransform;
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        Camera canvasCamera = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? parentCanvas.worldCamera
            : null;

        screenPosition = RectTransformUtility.WorldToScreenPoint(
            canvasCamera,
            slotRect.TransformPoint(slotRect.rect.center));
        slotSize = Vector2.Scale(slotRect.rect.size, slotRect.lossyScale);
        return true;
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
        hotspotButton.onClick.AddListener(ToggleCollapsedState);

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

        toggleHintText.font = TmpRuntimeFontFallback.WarmupCharacters("背包展开收起[B]") ?? TMP_Settings.defaultFontAsset;
        toggleHintText.fontSize = 20f;
        toggleHintText.alignment = TextAlignmentOptions.Center;
        toggleHintText.enableWordWrapping = false;
        toggleHintText.raycastTarget = false;
        toggleHintText.text = "收起 [B]";
    }

    private void RefreshToggleHintVisual()
    {
        if (toggleHotspotRect == null || toggleHotspotImage == null)
        {
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
            // gameplay 场景里统一改成贴玩家侧边的小提示，底部热区仅保留点击能力。
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
        // 玩法场景里不再显示 B 的展开/收起跟随提示，只保留原有切换能力。
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
