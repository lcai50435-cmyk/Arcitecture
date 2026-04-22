using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class GameplayStatusHudRuntime
{
    private const string HudFontResourcePath = "Fonts & Materials/LiberationSans SDF";
    private const string CanvasName = "GameplayStatusHudCanvas";
    private const string RootName = "GameplayStatusHudRoot";

    private static Canvas hudCanvas;
    private static CanvasGroup hudCanvasGroup;
    private static TMP_FontAsset hudFontAsset;
    private static RectTransform rootRect;
    private static ValueTrans healthGauge;
    private static ValueTrans weaponGauge;
    private static Graphic weaponFillGraphic;
    private static TextMeshProUGUI healthValueText;
    private static TextMeshProUGUI weaponValueText;
    private static TextMeshProUGUI countdownText;
    private static bool externallyHidden;

    public static ValueTrans EnsureHealthGauge(ValueTrans currentGauge)
    {
        if (currentGauge != null && IsRuntimeGauge(currentGauge))
        {
            EnsureRoot();
            return currentGauge;
        }

        EnsureRoot();

        if (IsRuntimeGauge(healthGauge))
        {
            return healthGauge;
        }

        if (currentGauge != null)
        {
            UseLegacyHealthGauge(currentGauge);
            return currentGauge;
        }

        return healthGauge;
    }

    public static ValueTrans EnsureWeaponGauge(ValueTrans currentGauge)
    {
        if (currentGauge != null && IsRuntimeGauge(currentGauge))
        {
            EnsureRoot();
            return currentGauge;
        }

        EnsureRoot();

        if (IsRuntimeGauge(weaponGauge))
        {
            return weaponGauge;
        }

        if (currentGauge != null)
        {
            UseLegacyWeaponGauge(currentGauge);
            return currentGauge;
        }

        return weaponGauge;
    }

    public static void RefreshHealthText(float current, float max)
    {
        if (healthValueText != null)
        {
            healthValueText.text = $"{current:0}/{max:0}";
        }
    }

    public static void RefreshWeaponText(float current, float max, WeaponType weaponType)
    {
        if (weaponFillGraphic != null)
        {
            weaponFillGraphic.color = InkTypeCatalog.GetDisplayColor(weaponType);
        }

        if (weaponValueText != null)
        {
            weaponValueText.text = $"{current:0}/{max:0}";
        }
    }

    public static TextMeshProUGUI EnsureCountdownText(TextMeshProUGUI currentText)
    {
        if (currentText != null)
        {
            countdownText = currentText;
            return currentText;
        }

        EnsureHudCanvas();

        if (countdownText != null)
        {
            return countdownText;
        }

        GameObject textObject = GameObject.Find("GameplayCountdownText");
        if (textObject == null)
        {
            textObject = new GameObject("GameplayCountdownText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(hudCanvas.transform, false);
        }

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -24f);
        rect.sizeDelta = new Vector2(220f, 52f);
        rect.localScale = Vector3.one;
        rect.SetAsLastSibling();

        countdownText = textObject.GetComponent<TextMeshProUGUI>();
        countdownText.font = ResolveHudFont();
        countdownText.fontSize = 30f;
        countdownText.color = Color.white;
        countdownText.alignment = TextAlignmentOptions.Center;
        countdownText.enableWordWrapping = false;
        countdownText.raycastTarget = false;
        countdownText.gameObject.SetActive(!externallyHidden);

        return countdownText;
    }

    public static void SetVisible(bool shouldShow)
    {
        externallyHidden = !shouldShow;

        if (hudCanvas != null)
        {
            hudCanvas.gameObject.SetActive(shouldShow);
        }

        if (rootRect != null)
        {
            rootRect.gameObject.SetActive(shouldShow);
        }

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(shouldShow);
        }
    }

    public static void SetAlpha(float alpha)
    {
        EnsureHudCanvas();

        if (hudCanvasGroup != null)
        {
            hudCanvasGroup.alpha = Mathf.Clamp01(alpha);
        }
    }

    private static void EnsureRoot()
    {
        RuntimeMiniMapHud.EnsureInstance();

        if (rootRect != null && IsRuntimeGauge(healthGauge) && IsRuntimeGauge(weaponGauge))
        {
            ReattachToHudCanvas();
            rootRect.gameObject.SetActive(true);
            return;
        }

        EnsureHudCanvas();

        GameObject rootObject = GameObject.Find(RootName);
        if (rootObject == null)
        {
            rootObject = new GameObject(RootName, typeof(RectTransform));
        }

        rootRect = rootObject.GetComponent<RectTransform>();
        if (rootRect == null)
        {
            return;
        }

        Image rootBackground = rootObject.GetComponent<Image>();
        if (rootBackground == null)
        {
            rootBackground = rootObject.AddComponent<Image>();
        }

        RuntimeUiSpriteFactory.ApplyRoundedSprite(
            rootBackground,
            new Color(0.08f, 0.06f, 0.04f, 0.74f),
            10,
            12);
        ReattachToHudCanvas();
        rootRect.gameObject.SetActive(!externallyHidden);

        // 旧场景里仍可能保留 legacy ValueTrans 引用；这里统一回收到运行时 HUD，
        // 避免只创建了外层底板、却没有真正生成血量/Ink 两行内容。
        if (!IsRuntimeGauge(healthGauge) || !IsRuntimeGauge(weaponGauge))
        {
            CreateRows(rootObject.transform);
        }
    }

    private static void UseLegacyHealthGauge(ValueTrans gauge)
    {
        healthGauge = gauge;
        HideRuntimeHudIfPresent();
    }

    private static void UseLegacyWeaponGauge(ValueTrans gauge)
    {
        weaponGauge = gauge;
        weaponFillGraphic = ResolveGaugeFillGraphic(gauge);
        HideRuntimeHudIfPresent();
    }

    private static bool IsRuntimeGauge(ValueTrans gauge)
    {
        return gauge != null
            && rootRect != null
            && gauge.transform != null
            && gauge.transform.IsChildOf(rootRect);
    }

    private static void CreateRows(Transform parent)
    {
        ClearRow(parent, "HealthRow");
        ClearRow(parent, "InkRow");

        StatusRow healthRow = CreateRow(parent, "HealthRow", "HP", new Vector2(14f, -10f), new Color(0.87f, 0.27f, 0.24f, 0.95f));
        StatusRow weaponRow = CreateRow(parent, "InkRow", "Ink", new Vector2(14f, -46f), new Color(0.25f, 0.70f, 0.96f, 0.95f));

        healthGauge = healthRow.gauge;
        weaponGauge = weaponRow.gauge;
        weaponFillGraphic = weaponGauge != null && weaponGauge.slider != null && weaponGauge.slider.fillRect != null
            ? weaponGauge.slider.fillRect.GetComponent<Graphic>()
            : null;
        healthValueText = healthRow.valueText;
        weaponValueText = weaponRow.valueText;
    }

    private static void ClearRow(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            Object.Destroy(child.gameObject);
        }
    }

    private static StatusRow CreateRow(Transform parent, string rowName, string title, Vector2 anchoredPosition, Color fillColor)
    {
        GameObject rowObject = CreateUIObject(rowName, parent);
        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(0f, 1f);
        rowRect.pivot = new Vector2(0f, 1f);
        rowRect.anchoredPosition = anchoredPosition;
        rowRect.sizeDelta = new Vector2(364f, 28f);

        Image background = rowObject.AddComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(
            background,
            new Color(0.14f, 0.11f, 0.08f, 0.82f),
            8,
            10);

        TextMeshProUGUI titleText = CreateText("Label", rowObject.transform, title, 17f, new Vector2(10f, -4f), new Vector2(48f, 20f), TextAlignmentOptions.MidlineLeft);
        titleText.fontStyle = FontStyles.Bold;

        GameObject barObject = CreateUIObject("Bar", rowObject.transform);
        RectTransform barRect = barObject.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 0.5f);
        barRect.anchorMax = new Vector2(0f, 0.5f);
        barRect.pivot = new Vector2(0f, 0.5f);
        barRect.anchoredPosition = new Vector2(56f, 0f);
        barRect.sizeDelta = new Vector2(206f, 14f);

        Image barBackground = barObject.AddComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(
            barBackground,
            new Color(0.17f, 0.17f, 0.20f, 0.95f),
            7,
            10);

        Slider slider = barObject.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 100f;
        slider.targetGraphic = barBackground;

        GameObject fillArea = CreateUIObject("FillArea", barObject.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(2f, 2f);
        fillAreaRect.offsetMax = new Vector2(-2f, -2f);

        GameObject fillObject = CreateUIObject("Fill", fillArea.transform);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImage = fillObject.AddComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(fillImage, fillColor, 7, 10);
        slider.fillRect = fillRect;

        ValueTrans gauge = barObject.AddComponent<ValueTrans>();
        gauge.slider = slider;

        TextMeshProUGUI valueText = CreateText("Value", rowObject.transform, "100/100", 15f, new Vector2(268f, -4f), new Vector2(72f, 20f), TextAlignmentOptions.MidlineRight);

        return new StatusRow(gauge, valueText);
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        string text,
        float fontSize,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateUIObject(name, parent);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.enableWordWrapping = false;
        tmp.font = ResolveHudFont();

        return tmp;
    }

    private static TMP_FontAsset ResolveHudFont()
    {
        if (hudFontAsset != null)
        {
            return hudFontAsset;
        }

        hudFontAsset = TmpRuntimeFontFallback.EnsureChineseFallback();
        if (hudFontAsset != null)
        {
            return hudFontAsset;
        }

        hudFontAsset = Resources.Load<TMP_FontAsset>(HudFontResourcePath);
        if (hudFontAsset != null)
        {
            return hudFontAsset;
        }

        hudFontAsset = TMP_Settings.defaultFontAsset;
        return hudFontAsset;
    }

    private static void EnsureHudCanvas()
    {
        if (hudCanvas != null)
        {
            return;
        }

        GameObject canvasObject = GameObject.Find(CanvasName);
        if (canvasObject == null)
        {
            canvasObject = new GameObject(
                CanvasName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
        }

        hudCanvas = canvasObject.GetComponent<Canvas>();
        if (hudCanvas == null)
        {
            hudCanvas = canvasObject.AddComponent<Canvas>();
        }

        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.overrideSorting = true;
        hudCanvas.sortingOrder = 240;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvasObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;
        canvasRect.localScale = Vector3.one;

        GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = false;
        }

        hudCanvasGroup = canvasObject.GetComponent<CanvasGroup>();
        if (hudCanvasGroup == null)
        {
            hudCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
        }

        hudCanvasGroup.interactable = false;
        hudCanvasGroup.blocksRaycasts = false;
        hudCanvasGroup.alpha = externallyHidden ? 0f : 1f;
        canvasObject.SetActive(!externallyHidden);
    }

    private static void ReattachToHudCanvas()
    {
        if (rootRect == null)
        {
            return;
        }

        EnsureHudCanvas();
        if (hudCanvas != null && rootRect.parent != hudCanvas.transform)
        {
            rootRect.SetParent(hudCanvas.transform, false);
        }

        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(24f, -24f);
        rootRect.sizeDelta = new Vector2(392f, 80f);
        rootRect.localScale = Vector3.one;
        rootRect.SetAsLastSibling();
    }

    private static Graphic ResolveGaugeFillGraphic(ValueTrans gauge)
    {
        if (gauge == null)
        {
            return null;
        }

        if (gauge.slider != null && gauge.slider.fillRect != null)
        {
            return gauge.slider.fillRect.GetComponent<Graphic>();
        }

        return gauge.GetComponentInChildren<Graphic>(true);
    }

    private static void HideRuntimeHudIfPresent()
    {
        if (rootRect != null)
        {
            rootRect.gameObject.SetActive(false);
        }

        if (hudCanvas != null)
        {
            hudCanvas.gameObject.SetActive(false);
        }
    }

    private readonly struct StatusRow
    {
        public readonly ValueTrans gauge;
        public readonly TextMeshProUGUI valueText;

        public StatusRow(ValueTrans gauge, TextMeshProUGUI valueText)
        {
            this.gauge = gauge;
            this.valueText = valueText;
        }
    }
}

public sealed class RuntimeFollowPromptHud : MonoBehaviour
{
    private const string CanvasName = "RuntimeFollowPromptCanvas";
    private const int OverlaySortingOrder = 244;
    private const float SideOffset = 48f;
    private const float VerticalOffset = -6f;
    private const float VerticalSpacing = 30f;
    private const float ScreenMargin = 18f;
    private const float AnchorWorldHeightOffset = 0.04f;

    private static readonly Color PromptBackgroundColor = new Color(0.08f, 0.06f, 0.04f, 0.82f);
    private static readonly Color KeyChipColor = new Color(0.86f, 0.67f, 0.34f, 1f);
    private static readonly Color KeyTextColor = new Color(0.11f, 0.08f, 0.05f, 1f);
    private static readonly Color LabelTextColor = new Color(0.96f, 0.91f, 0.80f, 1f);

    private sealed class PromptBinding
    {
        public string Id;
        public Transform Anchor;
        public RectTransform Root;
        public HorizontalLayoutGroup RootLayout;
        public LayoutElement KeyLayout;
        public LayoutElement LabelLayout;
        public TextMeshProUGUI KeyText;
        public TextMeshProUGUI LabelText;
        public int Priority;
        public bool RequestedVisible;
    }

    private static RuntimeFollowPromptHud instance;

    private readonly Dictionary<string, PromptBinding> promptBindings = new Dictionary<string, PromptBinding>();
    private readonly List<PromptBinding> activeBindings = new List<PromptBinding>();

    private Canvas overlayCanvas;
    private RectTransform canvasRect;
    private TMP_FontAsset promptFont;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static RuntimeFollowPromptHud EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        RuntimeFollowPromptHud existing = FindObjectOfType<RuntimeFollowPromptHud>(true);
        if (existing != null)
        {
            instance = existing;
            instance.EnsureInfrastructure();
            return existing;
        }

        GameObject hudObject = new GameObject("RuntimeFollowPromptHud");
        instance = hudObject.AddComponent<RuntimeFollowPromptHud>();
        return instance;
    }

    public static void ShowOrUpdate(string promptId, Transform anchor, string keyText, string label, int priority = 0)
    {
        if (string.IsNullOrWhiteSpace(promptId) || anchor == null)
        {
            return;
        }

        RuntimeFollowPromptHud hud = EnsureInstance();
        if (hud == null)
        {
            return;
        }

        hud.ShowOrUpdateInternal(promptId, anchor, keyText, label, priority);
    }

    public static void Hide(string promptId)
    {
        if (instance == null || string.IsNullOrWhiteSpace(promptId))
        {
            return;
        }

        if (!instance.promptBindings.TryGetValue(promptId, out PromptBinding binding))
        {
            return;
        }

        binding.RequestedVisible = false;
        if (binding.Root != null)
        {
            binding.Root.gameObject.SetActive(false);
        }
    }

    public static string FormatCompactKey(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.None:
                return string.Empty;
            case KeyCode.Mouse0:
                return "LMB";
            case KeyCode.Mouse1:
                return "RMB";
            case KeyCode.Mouse2:
                return "MMB";
            case KeyCode.LeftShift:
            case KeyCode.RightShift:
                return "Shift";
            case KeyCode.LeftControl:
            case KeyCode.RightControl:
                return "Ctrl";
            case KeyCode.LeftAlt:
            case KeyCode.RightAlt:
                return "Alt";
            case KeyCode.Space:
                return "Space";
            case KeyCode.Return:
                return "Enter";
            case KeyCode.Escape:
                return "Esc";
            case KeyCode.UpArrow:
                return "Up";
            case KeyCode.DownArrow:
                return "Down";
            case KeyCode.LeftArrow:
                return "Left";
            case KeyCode.RightArrow:
                return "Right";
        }

        string rawName = keyCode.ToString();
        if (rawName.StartsWith("Alpha"))
        {
            return rawName.Substring("Alpha".Length);
        }

        if (rawName.StartsWith("Keypad"))
        {
            return rawName.Replace("Keypad", "Num");
        }

        return rawName.ToUpperInvariant();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureInfrastructure();
    }

    private void LateUpdate()
    {
        EnsureInfrastructure();

        Camera mainCamera = Camera.main;
        activeBindings.Clear();

        foreach (PromptBinding binding in promptBindings.Values)
        {
            bool shouldShow = binding.RequestedVisible &&
                              binding.Anchor != null &&
                              binding.Root != null &&
                              mainCamera != null;

            if (!shouldShow)
            {
                if (binding.Root != null && binding.Root.gameObject.activeSelf)
                {
                    binding.Root.gameObject.SetActive(false);
                }

                continue;
            }

            activeBindings.Add(binding);
        }

        activeBindings.Sort((left, right) => left.Priority.CompareTo(right.Priority));
        for (int i = 0; i < activeBindings.Count; i++)
        {
            UpdatePromptPosition(activeBindings[i], mainCamera, i);
        }
    }

    private void ShowOrUpdateInternal(string promptId, Transform anchor, string keyText, string label, int priority)
    {
        EnsureInfrastructure();

        PromptBinding binding = GetOrCreateBinding(promptId);
        ApplyPromptMetrics(binding);
        binding.Anchor = anchor;
        binding.Priority = priority;
        binding.RequestedVisible = true;

        string compactKey = string.IsNullOrWhiteSpace(keyText) ? "?" : keyText;
        string promptLabel = string.IsNullOrWhiteSpace(label) ? "交互" : label;

        if (binding.KeyText != null)
        {
            binding.KeyText.text = compactKey;
        }

        if (binding.LabelText != null)
        {
            binding.LabelText.text = promptLabel;
        }

        if (binding.Root != null)
        {
            binding.Root.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(binding.Root);
        }
    }

    private void EnsureInfrastructure()
    {
        promptFont ??= TmpRuntimeFontFallback.EnsureChineseFallback() ?? TMP_Settings.defaultFontAsset;

        if (overlayCanvas != null && canvasRect != null)
        {
            return;
        }

        Transform existingCanvas = transform.Find(CanvasName);
        GameObject canvasObject = existingCanvas != null
            ? existingCanvas.gameObject
            : new GameObject(
                CanvasName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

        canvasObject.transform.SetParent(transform, false);

        overlayCanvas = canvasObject.GetComponent<Canvas>();
        if (overlayCanvas == null)
        {
            overlayCanvas = canvasObject.AddComponent<Canvas>();
        }

        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = OverlaySortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvasObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = false;
        }

        canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;
        canvasRect.localScale = Vector3.one;
    }

    private PromptBinding GetOrCreateBinding(string promptId)
    {
        if (promptBindings.TryGetValue(promptId, out PromptBinding existing))
        {
            BindPromptReferences(existing);
            ApplyPromptMetrics(existing);
            return existing;
        }

        GameObject rootObject = new GameObject(
            $"Prompt_{promptId}",
            typeof(RectTransform),
            typeof(Image),
            typeof(HorizontalLayoutGroup),
            typeof(ContentSizeFitter));
        rootObject.transform.SetParent(canvasRect, false);

        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(0f, 0f);

        Image background = rootObject.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(background, PromptBackgroundColor, 6, 8, 1.2f);
        background.raycastTarget = false;

        HorizontalLayoutGroup layout = rootObject.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(7, 11, 4, 4);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = rootObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject keyChip = new GameObject(
            "KeyChip",
            typeof(RectTransform),
            typeof(Image),
            typeof(LayoutElement));
        keyChip.transform.SetParent(rootObject.transform, false);

        Image keyBackground = keyChip.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(keyBackground, KeyChipColor, 4, 6, 1.2f);
        keyBackground.raycastTarget = false;

        LayoutElement keyLayout = keyChip.GetComponent<LayoutElement>();
        keyLayout.preferredWidth = 23f;
        keyLayout.preferredHeight = 23f;

        TextMeshProUGUI keyText = CreatePromptText(
            "KeyText",
            keyChip.transform,
            "F",
            14f,
            KeyTextColor,
            TextAlignmentOptions.Center);
        StretchRect(keyText.rectTransform);
        keyText.fontStyle = FontStyles.Bold;

        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(LayoutElement),
            typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(rootObject.transform, false);

        LayoutElement labelLayout = labelObject.GetComponent<LayoutElement>();
        labelLayout.minWidth = 62f;
        labelLayout.preferredHeight = 22f;

        TextMeshProUGUI labelText = labelObject.GetComponent<TextMeshProUGUI>();
        labelText.font = promptFont;
        labelText.fontSize = 14f;
        labelText.color = LabelTextColor;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.enableWordWrapping = false;
        labelText.raycastTarget = false;
        labelText.text = "交互";

        PromptBinding binding = new PromptBinding
        {
            Id = promptId,
            Root = rootRect,
            RootLayout = layout,
            KeyLayout = keyLayout,
            LabelLayout = labelLayout,
            KeyText = keyText,
            LabelText = labelText
        };

        ApplyPromptMetrics(binding);
        rootObject.SetActive(false);
        promptBindings[promptId] = binding;
        return binding;
    }

    private static void BindPromptReferences(PromptBinding binding)
    {
        if (binding == null || binding.Root == null)
        {
            return;
        }

        binding.RootLayout ??= binding.Root.GetComponent<HorizontalLayoutGroup>();
        binding.KeyLayout ??= binding.Root.Find("KeyChip")?.GetComponent<LayoutElement>();
        binding.LabelLayout ??= binding.Root.Find("Label")?.GetComponent<LayoutElement>();
        binding.KeyText ??= binding.Root.Find("KeyChip/KeyText")?.GetComponent<TextMeshProUGUI>();
        binding.LabelText ??= binding.Root.Find("Label")?.GetComponent<TextMeshProUGUI>();
    }

    private static void ApplyPromptMetrics(PromptBinding binding)
    {
        if (binding == null)
        {
            return;
        }

        if (binding.RootLayout != null)
        {
            binding.RootLayout.padding = new RectOffset(7, 11, 4, 4);
            binding.RootLayout.spacing = 6f;
            binding.RootLayout.childControlWidth = true;
            binding.RootLayout.childControlHeight = true;
            binding.RootLayout.childForceExpandWidth = false;
            binding.RootLayout.childForceExpandHeight = false;
        }

        if (binding.KeyLayout != null)
        {
            binding.KeyLayout.minWidth = 23f;
            binding.KeyLayout.minHeight = 23f;
            binding.KeyLayout.preferredWidth = 23f;
            binding.KeyLayout.preferredHeight = 23f;
            binding.KeyLayout.flexibleWidth = 0f;
            binding.KeyLayout.flexibleHeight = 0f;
        }

        if (binding.LabelLayout != null)
        {
            binding.LabelLayout.minWidth = 62f;
            binding.LabelLayout.minHeight = 22f;
            binding.LabelLayout.preferredHeight = 22f;
            binding.LabelLayout.flexibleWidth = 0f;
            binding.LabelLayout.flexibleHeight = 0f;
        }

        if (binding.KeyText != null)
        {
            binding.KeyText.fontSize = 14f;
        }

        if (binding.LabelText != null)
        {
            binding.LabelText.fontSize = 14f;
        }
    }

    private void UpdatePromptPosition(PromptBinding binding, Camera mainCamera, int stackIndex)
    {
        if (binding.Root == null || binding.Anchor == null)
        {
            return;
        }

        Vector3 worldPoint = binding.Anchor.position + new Vector3(0f, AnchorWorldHeightOffset, 0f);
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldPoint);

        if (screenPoint.z <= 0f)
        {
            binding.Root.gameObject.SetActive(false);
            return;
        }

        if (!binding.Root.gameObject.activeSelf)
        {
            binding.Root.gameObject.SetActive(true);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(binding.Root);

        float promptWidth = Mathf.Max(binding.Root.rect.width, 112f);
        float promptHeight = Mathf.Max(binding.Root.rect.height, 26f);
        bool preferRight = screenPoint.x <= Screen.width * 0.68f;
        float targetScreenX = preferRight
            ? screenPoint.x + SideOffset + promptWidth * 0.5f
            : screenPoint.x - SideOffset - promptWidth * 0.5f;
        targetScreenX = Mathf.Clamp(
            targetScreenX,
            ScreenMargin + promptWidth * 0.5f,
            Screen.width - ScreenMargin - promptWidth * 0.5f);
        float targetScreenY = Mathf.Clamp(
            screenPoint.y + VerticalOffset - stackIndex * VerticalSpacing,
            ScreenMargin + promptHeight * 0.5f,
            Screen.height - ScreenMargin - promptHeight * 0.5f);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                new Vector2(targetScreenX, targetScreenY),
                null,
                out Vector2 localPoint))
        {
            binding.Root.anchoredPosition = localPoint;
        }
    }

    private TextMeshProUGUI CreatePromptText(
        string name,
        Transform parent,
        string text,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();
        textComponent.font = promptFont;
        textComponent.fontSize = fontSize;
        textComponent.color = color;
        textComponent.alignment = alignment;
        textComponent.enableWordWrapping = false;
        textComponent.raycastTarget = false;
        textComponent.text = text;
        return textComponent;
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
