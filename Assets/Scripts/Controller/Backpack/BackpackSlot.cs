using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BackpackSlot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    private const string RuntimeSlotIconName = "ItemIcon";

    [Header("格子编号 0~5")]
    public int slotIndex;

    [Header("长按几秒丢弃")]
    public float needHoldTime = 1f;

    private BackpackMananger backpack;
    private Image slotImage;
    private Image itemIconImage;
    private BackpackUI backpackUI;
    private Outline legacyHoverOutline;
    private bool isHolding;
    private float holdTimer;
    private bool isHovered;

    private void Start()
    {
        slotImage = GetComponent<Image>();
        if (slotImage == null)
        {
            slotImage = gameObject.AddComponent<Image>();
            Debug.Log($"自动为格子{slotIndex}添加了Image组件");
        }

        if (itemIconImage == null)
        {
            Transform iconTransform = transform.Find(RuntimeSlotIconName);
            if (iconTransform != null)
            {
                itemIconImage = iconTransform.GetComponent<Image>();
            }
        }

        legacyHoverOutline = GetComponent<Outline>();
        if (legacyHoverOutline != null)
        {
            legacyHoverOutline.enabled = false;
        }

        backpack = BackpackMananger.Instance;
        if (backpackUI == null)
        {
            backpackUI = BackpackUI.EnsureRuntimeInstance();
        }

        if (backpack == null)
        {
            Debug.LogError($"BackpackSlot {slotIndex}: 未找到BackpackMananger实例！");
        }
    }

    public void BindRuntimeVisual(BackpackUI owner, Image runtimeItemIcon)
    {
        backpackUI = owner;
        itemIconImage = runtimeItemIcon;
    }

    public bool TryGetScreenCenter(out Vector2 screenCenter, out Vector2 size)
    {
        screenCenter = default;
        size = default;

        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null)
        {
            return false;
        }

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        Camera canvasCamera = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? parentCanvas.worldCamera
            : null;

        screenCenter = RectTransformUtility.WorldToScreenPoint(
            canvasCamera,
            rectTransform.TransformPoint(rectTransform.rect.center));
        size = Vector2.Scale(rectTransform.rect.size, rectTransform.lossyScale);
        return true;
    }

    private void Update()
    {
        if (isHovered)
        {
            RefreshHoverState();
        }

        if (!isHolding)
        {
            return;
        }

        holdTimer += Time.unscaledDeltaTime;
        if (holdTimer >= needHoldTime)
        {
            DropSingleItem();
            StopHold();
        }
    }

    private void OnDisable()
    {
        StopHold();
        isHovered = false;
        HideHoverState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            backpackUI?.SelectSlot(slotIndex);
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            StartSingleHold();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            StopHold();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        backpackUI?.SetSlotHover(slotIndex, true);
        RefreshHoverState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        HideHoverState();
    }

    private void StartSingleHold()
    {
        if (backpack == null)
        {
            Debug.LogError("BackpackManager未找到！");
            return;
        }

        ArchitecturalCrystal? item = backpack.GetItem(slotIndex);
        if (!item.HasValue)
        {
            Debug.Log($"格子{slotIndex}没有物品，无法丢弃");
            return;
        }

        if (!HasVisibleItem())
        {
            Debug.Log("格子无显示图片");
            return;
        }

        Debug.Log($"开始长按格子{slotIndex}，物品：{item.Value.type}");
        isHolding = true;
        holdTimer = 0f;
    }

    private void StopHold()
    {
        isHolding = false;
        holdTimer = 0f;
    }

    private void RefreshHoverState()
    {
        backpackUI?.SetSlotHover(slotIndex, true);

        if (backpack == null || !HasVisibleItem())
        {
            HideHoverTooltip();
            return;
        }

        ArchitecturalCrystal? item = backpack.GetItem(slotIndex);
        if (!item.HasValue)
        {
            HideHoverTooltip();
            return;
        }

        RuntimeBackpackHoverHud.EnsureInstance().ShowOrUpdate(this, item.Value, Input.mousePosition);
    }

    private void HideHoverState()
    {
        backpackUI?.SetSlotHover(slotIndex, false);
        if (legacyHoverOutline != null)
        {
            legacyHoverOutline.enabled = false;
        }

        HideHoverTooltip();
    }

    private void HideHoverTooltip()
    {
        if (RuntimeBackpackHoverHud.Instance != null)
        {
            RuntimeBackpackHoverHud.Instance.HideForSlot(this);
        }
    }

    private bool HasVisibleItem()
    {
        Image visibleItemImage = itemIconImage != null ? itemIconImage : slotImage;
        return visibleItemImage != null && visibleItemImage.enabled && visibleItemImage.sprite != null;
    }

    private void DropSingleItem()
    {
        if (backpack == null)
        {
            return;
        }

        ArchitecturalCrystal? item = backpack.GetItem(slotIndex);
        if (!item.HasValue)
        {
            return;
        }

        ArchitecturalCrystal crystal = item.Value;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("未找到Player对象！");
            return;
        }

        float faceDir = player.transform.localScale.x > 0f ? 1f : -1f;
        Vector3 dropPosition = new Vector2(
            player.transform.position.x + faceDir * 0.2f,
            player.transform.position.y);
        RuntimeCrystalDropFactory.CreateInteractiveDrop(
            crystal,
            dropPosition,
            0.3f,
            0,
            null,
            $"Drop_{crystal.type}");

        backpack.RemoveItem(slotIndex);
        if (backpackUI != null)
        {
            backpackUI.RefreshUI();
        }
        else
        {
            Debug.LogError("BackpackUI未找到！");
        }

        HideHoverState();
        Debug.Log($"格子{slotIndex}的物品{crystal.type}已丢弃");
    }
}

public sealed class RuntimeBackpackHoverHud : MonoBehaviour
{
    private const string CanvasName = "RuntimeBackpackHoverHudCanvas";
    private const int SortingOrder = RuntimeModalStyle.ModalSortingOrder + 80;
    private const float PanelWidth = 320f;
    private const float PanelMinHeight = 132f;
    private const float MouseOffsetX = 26f;
    private const float MouseOffsetY = -20f;
    private const float ScreenPadding = 20f;
    private const float BackdropRefreshInterval = 0.04f;

    public static RuntimeBackpackHoverHud Instance { get; private set; }

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform panelRect;
    private Image panelImage;
    private RawImage blurBackdropImage;
    private Image iconImage;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI descriptionText;
    private Texture2D capturedBackdropTexture;
    private RenderTexture blurredBackdropTexture;
    private int capturedScreenWidth;
    private int capturedScreenHeight;
    private float nextBackdropRefreshAt;
    private MonoBehaviour currentOwner;
    private string cachedTitle;
    private string cachedDescription;
    private Sprite cachedIcon;

    public static RuntimeBackpackHoverHud EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        RuntimeBackpackHoverHud existing = FindObjectOfType<RuntimeBackpackHoverHud>(true);
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject hudObject = new GameObject("RuntimeBackpackHoverHud");
        Instance = hudObject.AddComponent<RuntimeBackpackHoverHud>();
        return Instance;
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
        EnsureUi();
        HideImmediate();
    }

    public void ShowOrUpdate(BackpackSlot owner, ArchitecturalCrystal crystal, Vector2 screenPosition)
    {
        ShowOrUpdate(owner as MonoBehaviour, crystal, screenPosition);
    }

    public void ShowOrUpdate(MonoBehaviour owner, ArchitecturalCrystal crystal, Vector2 screenPosition)
    {
        if (owner == null)
        {
            return;
        }

        EnsureUi();
        bool needsBackdropRefresh = blurredBackdropTexture == null
            || capturedScreenWidth != Screen.width
            || capturedScreenHeight != Screen.height
            || canvasGroup == null
            || canvasGroup.alpha <= 0.001f
            || Time.unscaledTime >= nextBackdropRefreshAt;

        currentOwner = owner;
        ApplyContent(crystal);
        UpdatePosition(screenPosition);

        if (needsBackdropRefresh)
        {
            CaptureBlurBackdrop();
            nextBackdropRefreshAt = Time.unscaledTime + BackdropRefreshInterval;
        }

        UpdateBackdropUv();
        SetVisible(true);
    }

    public void HideForSlot(BackpackSlot owner)
    {
        HideForOwner(owner);
    }

    public void HideForOwner(MonoBehaviour owner)
    {
        if (owner == null || currentOwner != owner)
        {
            return;
        }

        HideImmediate();
    }

    private void OnDestroy()
    {
        ReleaseBlurBackdrop();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void EnsureUi()
    {
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;
            return;
        }

        TmpRuntimeFontFallback.EnsureChineseFallback();

        GameObject canvasObject = new GameObject(
            CanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = SortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
        raycaster.enabled = false;

        canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        StretchRect(canvasRect);

        panelImage = CreateImage("Panel", canvasObject.transform, new Color(1f, 1f, 1f, 0.08f), 16, 14);
        panelRect = panelImage.rectTransform;
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelMinHeight);
        panelImage.raycastTarget = false;

        Mask panelMask = panelImage.gameObject.AddComponent<Mask>();
        panelMask.showMaskGraphic = true;

        Outline panelOutline = panelImage.gameObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.98f, 0.85f, 0.54f, 0.92f);
        panelOutline.effectDistance = new Vector2(1.5f, -1.5f);
        panelOutline.useGraphicAlpha = false;

        blurBackdropImage = CreateRawImage("BlurBackdrop", panelImage.transform);
        StretchRect(blurBackdropImage.rectTransform);
        blurBackdropImage.color = new Color(1f, 1f, 1f, 0.94f);
        blurBackdropImage.uvRect = new Rect(0f, 0f, 1f, 1f);

        Image blurTintImage = CreateImage("BlurTint", panelImage.transform, new Color(0.15f, 0.11f, 0.09f, 0.54f), 16, 14);
        StretchRect(blurTintImage.rectTransform);

        iconImage = CreateImage("Icon", panelImage.transform, Color.white, 10, 10);
        RectTransform iconRect = iconImage.rectTransform;
        iconRect.anchorMin = new Vector2(0f, 1f);
        iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(0f, 1f);
        iconRect.anchoredPosition = new Vector2(18f, -18f);
        iconRect.sizeDelta = new Vector2(42f, 42f);
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        titleText = CreateText("Title", panelImage.transform, 24f, new Color(0.98f, 0.94f, 0.83f, 1f), FontStyles.Bold);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.offsetMin = new Vector2(72f, -48f);
        titleRect.offsetMax = new Vector2(-18f, -14f);

        descriptionText = CreateText("Description", panelImage.transform, 18f, new Color(0.90f, 0.92f, 0.97f, 1f), FontStyles.Normal);
        RectTransform descriptionRect = descriptionText.rectTransform;
        descriptionRect.anchorMin = new Vector2(0f, 0f);
        descriptionRect.anchorMax = new Vector2(1f, 1f);
        descriptionRect.offsetMin = new Vector2(18f, 18f);
        descriptionRect.offsetMax = new Vector2(-18f, -70f);
        descriptionText.enableWordWrapping = true;
        descriptionText.overflowMode = TextOverflowModes.Overflow;
    }

    private void ApplyContent(ArchitecturalCrystal crystal)
    {
        string title = crystal.DisplayName;
        string description = InkModifierRuntimeConfig.BuildCrystalActivationText(
            crystal,
            BackpackMananger.Instance,
            RuntimeWeaponTypeResolver.ResolveEffectiveWeaponType(BackpackMananger.Instance));

        if (string.IsNullOrWhiteSpace(description))
        {
            description = string.IsNullOrWhiteSpace(crystal.textDescription)
                ? $"{title} 已加入背包。"
                : crystal.textDescription;
        }

        Sprite resolvedIcon = crystal.backIcon != null
            ? crystal.backIcon
            : (crystal.icon != null ? crystal.icon : RuntimeCrystalDropFactory.ResolveSprite(crystal));
        resolvedIcon = RuntimeSpriteDisplaySanitizer.GetDisplaySprite(resolvedIcon);
        bool contentChanged = cachedTitle != title
            || cachedDescription != description
            || cachedIcon != resolvedIcon;

        if (!contentChanged)
        {
            return;
        }

        cachedTitle = title;
        cachedDescription = description;
        cachedIcon = resolvedIcon;

        TMP_FontAsset runtimeFont = TmpRuntimeFontFallback.WarmupCharacters($"{title}\n{description}")
            ?? TmpRuntimeFontFallback.EnsureChineseFallback()
            ?? TMP_Settings.defaultFontAsset;

        if (iconImage != null)
        {
            iconImage.sprite = resolvedIcon;
            iconImage.enabled = iconImage.sprite != null;
        }

        if (titleText != null)
        {
            titleText.font = runtimeFont;
            titleText.text = title;
        }

        if (descriptionText != null)
        {
            descriptionText.font = runtimeFont;
            descriptionText.text = description;
            descriptionText.ForceMeshUpdate();
        }

        if (panelRect != null)
        {
            float preferredHeight = 122f;
            if (descriptionText != null)
            {
                preferredHeight += descriptionText.preferredHeight;
            }

            panelRect.sizeDelta = new Vector2(PanelWidth, Mathf.Max(PanelMinHeight, preferredHeight));
        }
    }

    private void UpdatePosition(Vector2 screenPosition)
    {
        if (panelRect == null || canvas == null)
        {
            return;
        }

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out Vector2 localPoint);

        Vector2 targetPosition = new Vector2(
            localPoint.x + canvasRect.rect.width * 0.5f + MouseOffsetX,
            localPoint.y - canvasRect.rect.height * 0.5f + MouseOffsetY);

        float minX = ScreenPadding;
        float maxX = canvasRect.rect.width - panelRect.sizeDelta.x - ScreenPadding;
        float maxY = -ScreenPadding;
        float minY = -canvasRect.rect.height + panelRect.sizeDelta.y + ScreenPadding;

        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        panelRect.anchoredPosition = targetPosition;
    }

    private void UpdateBackdropUv()
    {
        if (blurBackdropImage == null || blurBackdropImage.texture == null || panelRect == null)
        {
            return;
        }

        Vector3[] corners = new Vector3[4];
        panelRect.GetWorldCorners(corners);

        float screenWidth = Mathf.Max(Screen.width, 1);
        float screenHeight = Mathf.Max(Screen.height, 1);
        float minX = Mathf.Clamp(corners[0].x, 0f, screenWidth);
        float minY = Mathf.Clamp(corners[0].y, 0f, screenHeight);
        float maxX = Mathf.Clamp(corners[2].x, 0f, screenWidth);
        float maxY = Mathf.Clamp(corners[2].y, 0f, screenHeight);

        blurBackdropImage.uvRect = new Rect(
            minX / screenWidth,
            minY / screenHeight,
            Mathf.Max(maxX - minX, 1f) / screenWidth,
            Mathf.Max(maxY - minY, 1f) / screenHeight);
    }

    private void CaptureBlurBackdrop()
    {
        if (blurBackdropImage == null)
        {
            return;
        }

        ReleaseBlurBackdrop();

        Texture2D screenshot = CaptureBackdropTexture();
        if (screenshot == null)
        {
            blurBackdropImage.texture = null;
            return;
        }

        screenshot.filterMode = FilterMode.Bilinear;
        screenshot.wrapMode = TextureWrapMode.Clamp;
        capturedBackdropTexture = screenshot;
        capturedScreenWidth = Screen.width;
        capturedScreenHeight = Screen.height;

        int halfWidth = Mathf.Max(480, Screen.width / 2);
        int halfHeight = Mathf.Max(270, Screen.height / 2);
        int quarterWidth = Mathf.Max(320, Screen.width / 4);
        int quarterHeight = Mathf.Max(180, Screen.height / 4);
        int finalWidth = Mathf.Max(240, Screen.width / 8);
        int finalHeight = Mathf.Max(135, Screen.height / 8);

        RenderTexture halfTexture = RenderTexture.GetTemporary(halfWidth, halfHeight, 0, RenderTextureFormat.ARGB32);
        RenderTexture quarterTexture = RenderTexture.GetTemporary(quarterWidth, quarterHeight, 0, RenderTextureFormat.ARGB32);
        RenderTexture tempTexture = RenderTexture.GetTemporary(finalWidth, finalHeight, 0, RenderTextureFormat.ARGB32);
        halfTexture.filterMode = FilterMode.Bilinear;
        quarterTexture.filterMode = FilterMode.Bilinear;
        tempTexture.filterMode = FilterMode.Bilinear;

        blurredBackdropTexture = new RenderTexture(finalWidth, finalHeight, 0, RenderTextureFormat.ARGB32)
        {
            name = "RuntimeBackpackHoverBlurBackdrop",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        blurredBackdropTexture.Create();

        Graphics.Blit(capturedBackdropTexture, halfTexture);
        Graphics.Blit(halfTexture, quarterTexture);
        Graphics.Blit(quarterTexture, tempTexture);
        Graphics.Blit(tempTexture, quarterTexture);
        Graphics.Blit(quarterTexture, blurredBackdropTexture);

        RenderTexture.ReleaseTemporary(halfTexture);
        RenderTexture.ReleaseTemporary(quarterTexture);
        RenderTexture.ReleaseTemporary(tempTexture);

        blurBackdropImage.texture = blurredBackdropTexture;
    }

    private Texture2D CaptureBackdropTexture()
    {
        Camera captureCamera = ResolveBackdropCamera();
        if (captureCamera == null)
        {
            return ScreenCapture.CaptureScreenshotAsTexture();
        }

        int captureWidth = Mathf.Max(Screen.width, 1);
        int captureHeight = Mathf.Max(Screen.height, 1);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = captureCamera.targetTexture;
        RenderTexture captureTexture = RenderTexture.GetTemporary(captureWidth, captureHeight, 24, RenderTextureFormat.ARGB32);
        captureTexture.filterMode = FilterMode.Bilinear;
        captureTexture.wrapMode = TextureWrapMode.Clamp;

        try
        {
            captureCamera.targetTexture = captureTexture;
            captureCamera.Render();
            RenderTexture.active = captureTexture;

            Texture2D result = new Texture2D(captureWidth, captureHeight, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0f, 0f, captureWidth, captureHeight), 0, 0, false);
            result.Apply(false, false);
            result.filterMode = FilterMode.Bilinear;
            result.wrapMode = TextureWrapMode.Clamp;
            return result;
        }
        finally
        {
            captureCamera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(captureTexture);
        }
    }

    private static Camera ResolveBackdropCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.isActiveAndEnabled)
        {
            return mainCamera;
        }

        Camera[] cameras = FindObjectsOfType<Camera>(true);
        Camera fallback = null;
        float highestDepth = float.MinValue;
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera candidate = cameras[i];
            if (candidate == null || !candidate.isActiveAndEnabled || !candidate.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (candidate.depth < highestDepth)
            {
                continue;
            }

            highestDepth = candidate.depth;
            fallback = candidate;
        }

        return fallback;
    }

    private void ReleaseBlurBackdrop()
    {
        if (blurBackdropImage != null)
        {
            blurBackdropImage.texture = null;
            blurBackdropImage.uvRect = new Rect(0f, 0f, 1f, 1f);
        }

        if (blurredBackdropTexture != null)
        {
            if (blurredBackdropTexture.IsCreated())
            {
                blurredBackdropTexture.Release();
            }

            Destroy(blurredBackdropTexture);
            blurredBackdropTexture = null;
        }

        if (capturedBackdropTexture != null)
        {
            Destroy(capturedBackdropTexture);
            capturedBackdropTexture = null;
        }

        capturedScreenWidth = 0;
        capturedScreenHeight = 0;
        nextBackdropRefreshAt = 0f;
    }

    private void HideImmediate()
    {
        currentOwner = null;
        cachedTitle = null;
        cachedDescription = null;
        cachedIcon = null;
        ReleaseBlurBackdrop();
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (canvas != null)
        {
            canvas.gameObject.SetActive(true);
        }
    }

    private static Image CreateImage(string name, Transform parent, Color color, int radius, int border)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(image, color, radius, border, 1.2f);
        image.raycastTarget = false;
        return image;
    }

    private static RawImage CreateRawImage(string name, Transform parent)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        imageObject.transform.SetParent(parent, false);
        RawImage image = imageObject.GetComponent<RawImage>();
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, float fontSize, Color color, FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TmpRuntimeFontFallback.EnsureChineseFallback() ?? TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.color = color;
        text.fontStyle = fontStyle;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
        return text;
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
