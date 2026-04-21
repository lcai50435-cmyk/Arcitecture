using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BackpackUI : MonoBehaviour
{
    private const float ToggleHotspotY = -168f;
    private const float ToggleHotspotWidth = 138f;
    private const float ToggleHotspotHeight = 44f;
    private const float CollapseSlideDistance = 150f;
    private const float SlideSmoothTime = 0.08f;
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
    private Vector2 expandedAnchoredPosition;
    private Vector2 currentTargetPosition;
    private Vector2 slideVelocity;
    private bool slideInitialized;
    private bool isCollapsed;

    private void Start()
    {
        ResolveBackpackManager();
        EnsureSlideToggle();
        RefreshUI();
    }

    private void OnEnable()
    {
        ResolveBackpackManager();
        SubscribeRuntimeEvents();
        EnsureSlideToggle();
        RefreshUI();
    }

    private void OnDisable()
    {
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
                image.sprite = crystal.backIcon != null
                    ? crystal.backIcon
                    : (crystal.icon != null ? crystal.icon : RuntimeCrystalDropFactory.ResolveSprite(crystal));
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

        if ((rectTransform.anchoredPosition - currentTargetPosition).sqrMagnitude < 0.01f)
        {
            rectTransform.anchoredPosition = currentTargetPosition;
            return;
        }

        rectTransform.anchoredPosition = Vector2.SmoothDamp(
            rectTransform.anchoredPosition,
            currentTargetPosition,
            ref slideVelocity,
            SlideSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime);

        RefreshToggleHintVisual();
    }

    private void HandleToggleShortcut()
    {
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
        RefreshToggleHintVisual();
    }

    private void ToggleCollapsedState()
    {
        isCollapsed = !isCollapsed;
        currentTargetPosition = isCollapsed
            ? expandedAnchoredPosition + new Vector2(0f, -CollapseSlideDistance)
            : expandedAnchoredPosition;
    }

    private void ApplySlidePositionInstant()
    {
        if (rectTransform == null)
        {
            return;
        }

        currentTargetPosition = isCollapsed
            ? expandedAnchoredPosition + new Vector2(0f, -CollapseSlideDistance)
            : expandedAnchoredPosition;
        slideVelocity = Vector2.zero;
        rectTransform.anchoredPosition = currentTargetPosition;
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
