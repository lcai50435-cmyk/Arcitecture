using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class GameplayStatusHudRuntime
{
    private const string HudFontResourcePath = "Fonts & Materials/LiberationSans SDF";
    private const string CanvasName = "GameplayStatusHudCanvas";
    private const string CountdownTextName = "GameplayCountdownText";
    private const string RootName = "GameplayStatusHudRoot";
    private const string ScenePanelName = "ShowPanel";
    private const string HealthSliderName = "HealthSlider";
    private const string InkSliderName = "InkSlider";
    private const string StructureSliderName = "StructureSlider";
    private const string HeartIconAssetPath = "Assets/File/Prop/UIProp/Heart.png";
    private const string HealthTrackAssetPath = "Assets/File/Prop/UIProp/GrayHealth.png";
    private const string HealthFillAssetPath = "Assets/File/Prop/UIProp/RedHealth.png";
    private const string InkTrackAssetPath = "Assets/File/Prop/UIProp/BlackHealth.png";
    private const string StructureIconAssetPath = "Assets/File/Prop/Prop/Prism.png";
    private const string StructureTrackAssetPath = "Assets/File/Prop/UIProp/Bar.png";
    private const float RootWidth = 204f;
    private const float RootHeight = 78f;
    private const float RowWidth = 190f;
    private const float RowHeight = 22f;
    private const int PixelFrameWidth = 96;
    private const int PixelFrameHeight = 36;
    private const int PixelFrameBorder = 6;
    private const int PixelFillWidth = 64;
    private const int PixelFillHeight = 12;
    private static readonly Vector2 BoundCountdownTextSize = new Vector2(104f, 28f);
    private static readonly Vector2 RuntimeCountdownTextSize = new Vector2(220f, 52f);

    private static Canvas hudCanvas;
    private static CanvasGroup hudCanvasGroup;
    private static CanvasGroup rootCanvasGroup;
    private static TMP_FontAsset hudFontAsset;
    private static RectTransform rootRect;
    private static ValueTrans healthGauge;
    private static ValueTrans weaponGauge;
    private static ValueTrans structureGauge;
    private static Graphic weaponFillGraphic;
    private static Graphic structureFillGraphic;
    private static TextMeshProUGUI healthValueText;
    private static TextMeshProUGUI weaponValueText;
    private static TextMeshProUGUI structureValueText;
    private static TextMeshProUGUI countdownText;
    private static bool externallyHidden;
    private static bool usingSceneShowPanel;
    private static readonly Dictionary<string, Sprite> HudSpriteCache = new Dictionary<string, Sprite>();
    private static readonly Color RootPanelColor = new Color(1f, 1f, 1f, 0f);
    private static readonly Color RowPanelColor = new Color(1f, 1f, 1f, 0f);
    private static readonly Color GaugeTrackColor = new Color(0.10f, 0.10f, 0.11f, 0.96f);
    private static readonly Color GoldBorderColor = new Color(0.78f, 0.53f, 0.22f, 1f);
    private static readonly Color GoldHighlightColor = new Color(1f, 0.83f, 0.46f, 1f);
    private static readonly Color DarkOutlineColor = new Color(0.14f, 0.08f, 0.04f, 1f);
    private static readonly Color HealthFillColor = new Color(0.82f, 0.19f, 0.16f, 1f);
    private static readonly Color InkFillColor = new Color(0.20f, 0.58f, 0.86f, 1f);
    private static readonly Color StructureFillColor = new Color(0.20f, 0.78f, 0.82f, 1f);

    public static ValueTrans EnsureHealthGauge(ValueTrans currentGauge)
    {
        EnsureRoot();

        if (currentGauge != null && IsRuntimeGauge(currentGauge))
        {
            return currentGauge;
        }

        HideLegacyGauge(currentGauge);
        if (healthGauge != null)
        {
            return healthGauge;
        }

        return healthGauge;
    }

    public static ValueTrans EnsureWeaponGauge(ValueTrans currentGauge)
    {
        EnsureRoot();

        if (currentGauge != null && IsRuntimeGauge(currentGauge))
        {
            return currentGauge;
        }

        HideLegacyGauge(currentGauge);
        if (weaponGauge != null)
        {
            return weaponGauge;
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

    public static void RefreshStructureProgressText()
    {
        RuntimeProgressState state = RuntimeProgressState.Instance ?? RuntimeProgressState.EnsureInstance();
        float max = state != null ? Mathf.Max(1, state.GetTotalMaxProgress()) : 1f;
        float current = state != null ? Mathf.Clamp(state.GetTotalProgress(), 0f, max) : 0f;

        if (structureGauge != null)
        {
            structureGauge.SetMaxValue(max);
            structureGauge.SetValue(current);
        }

        if (structureFillGraphic != null)
        {
            structureFillGraphic.color = StructureFillColor;
        }

        if (structureValueText != null)
        {
            structureValueText.text = $"{current:0}/{max:0}";
        }
    }

    public static TextMeshProUGUI EnsureCountdownText(TextMeshProUGUI currentText)
    {
        if (currentText != null)
        {
            if (string.Equals(currentText.gameObject.name, CountdownTextName, System.StringComparison.Ordinal))
            {
                ApplyRuntimeCountdownTextLayout(currentText.rectTransform);
                ConfigureCountdownText(currentText, 30f);
                countdownText = currentText;
                return currentText;
            }

            if (!TryNormalizeBoundCountdownText(currentText))
            {
                currentText = null;
            }
            else
            {
                countdownText = currentText;
                return currentText;
            }
        }

        if (currentText == null)
        {
            EnsureHudCanvas();
        }

        if (countdownText != null)
        {
            ApplyRuntimeCountdownTextLayout(countdownText.rectTransform);
            ConfigureCountdownText(countdownText, 30f);
            countdownText.gameObject.SetActive(!externallyHidden);
            return countdownText;
        }

        GameObject textObject = GameObject.Find(CountdownTextName);
        if (textObject == null)
        {
            textObject = new GameObject(CountdownTextName, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(hudCanvas.transform, false);
        }

        countdownText = textObject.GetComponent<TextMeshProUGUI>();
        ApplyRuntimeCountdownTextLayout(textObject.GetComponent<RectTransform>());
        ConfigureCountdownText(countdownText, 30f);
        countdownText.gameObject.SetActive(!externallyHidden);

        return countdownText;
    }

    private static bool TryNormalizeBoundCountdownText(TextMeshProUGUI text)
    {
        if (text == null || text.transform == null || text.transform.parent == null)
        {
            return false;
        }

        RectTransform rect = text.rectTransform;
        if (rect == null)
        {
            return false;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = BoundCountdownTextSize;
        rect.localScale = Vector3.one;
        rect.SetAsLastSibling();

        ConfigureCountdownText(text, 18f);
        text.gameObject.SetActive(!externallyHidden);
        return true;
    }

    private static void ApplyRuntimeCountdownTextLayout(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -24f);
        rect.sizeDelta = RuntimeCountdownTextSize;
        rect.localScale = Vector3.one;
        rect.SetAsLastSibling();
    }

    private static void ConfigureCountdownText(TextMeshProUGUI text, float fontSize)
    {
        if (text == null)
        {
            return;
        }

        text.font = ResolveHudFont();
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
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
        EnsureRoot();
        float clampedAlpha = Mathf.Clamp01(alpha);

        if (hudCanvasGroup != null)
        {
            hudCanvasGroup.alpha = clampedAlpha;
        }

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = clampedAlpha;
        }
    }

    private static void EnsureRoot()
    {
        RuntimeMiniMapHud.EnsureInstance();

        if (TryBindSceneShowPanel())
        {
            RefreshStructureProgressText();
            return;
        }

        if (usingSceneShowPanel
            && rootRect != null
            && IsRuntimeGauge(healthGauge)
            && IsRuntimeGauge(weaponGauge)
            && IsRuntimeGauge(structureGauge))
        {
            rootRect.gameObject.SetActive(!externallyHidden);
            RefreshStructureProgressText();
        }
    }

    private static bool IsRuntimeGauge(ValueTrans gauge)
    {
        return gauge != null
            && rootRect != null
            && gauge.transform != null
            && gauge.transform.IsChildOf(rootRect);
    }

    private static bool TryBindSceneShowPanel()
    {
        RectTransform panel = FindSceneShowPanel() ?? CreateSceneShowPanel();
        if (panel == null)
        {
            return false;
        }

        Slider healthSlider = FindChildSlider(panel, HealthSliderName);
        Slider weaponSlider = FindChildSlider(panel, InkSliderName);
        Slider structureSlider = FindChildSlider(panel, StructureSliderName);
        if (healthSlider == null || weaponSlider == null || structureSlider == null)
        {
            return false;
        }

        rootRect = panel;
        usingSceneShowPanel = true;
        EnsureSceneShowPanelVisible(rootRect);
        rootCanvasGroup = EnsureCanvasGroup(rootRect.gameObject, false);
        healthGauge = EnsureValueTrans(healthSlider);
        weaponGauge = EnsureValueTrans(weaponSlider);
        structureGauge = EnsureValueTrans(structureSlider);
        weaponFillGraphic = ResolveFillGraphic(weaponSlider);
        structureFillGraphic = ResolveFillGraphic(structureSlider);
        healthValueText = null;
        weaponValueText = null;
        structureValueText = null;
        rootRect.gameObject.SetActive(!externallyHidden);
        return true;
    }

    private static RectTransform FindSceneShowPanel()
    {
        RectTransform[] rectTransforms = Object.FindObjectsOfType<RectTransform>(true);
        foreach (RectTransform rectTransform in rectTransforms)
        {
            if (rectTransform == null || !string.Equals(rectTransform.name, ScenePanelName, System.StringComparison.Ordinal))
            {
                continue;
            }

            if (FindChildSlider(rectTransform, HealthSliderName) != null
                && FindChildSlider(rectTransform, InkSliderName) != null
                && FindChildSlider(rectTransform, StructureSliderName) != null)
            {
                return rectTransform;
            }
        }

        return null;
    }

    private static RectTransform CreateSceneShowPanel()
    {
        Canvas canvas = FindGameplayCanvas() ?? CreateShowPanelCanvas();
        if (canvas == null)
        {
            return null;
        }

        GameObject panelObject = CreateUIObject(ScenePanelName, canvas.transform);
        RectTransform panel = panelObject.GetComponent<RectTransform>();
        panel.anchorMin = new Vector2(0f, 1f);
        panel.anchorMax = new Vector2(0f, 1f);
        panel.pivot = new Vector2(0f, 1f);
        panel.anchoredPosition = new Vector2(24f, -24f);
        panel.sizeDelta = new Vector2(260f, 132f);
        panel.localScale = Vector3.one;

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(1f, 1f, 1f, 0f);
        panelImage.raycastTarget = false;

        CreateShowPanelSlider(panel, HealthSliderName, HeartIconAssetPath, HealthTrackAssetPath, HealthFillAssetPath, new Vector2(0f, -4f), HealthFillColor);
        CreateShowPanelSlider(panel, InkSliderName, null, InkTrackAssetPath, null, new Vector2(0f, -44f), InkFillColor);
        CreateShowPanelSlider(panel, StructureSliderName, StructureIconAssetPath, StructureTrackAssetPath, null, new Vector2(0f, -84f), StructureFillColor);

        return panel;
    }

    private static Canvas FindGameplayCanvas()
    {
        Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                continue;
            }

            return canvas;
        }

        return null;
    }

    private static Canvas CreateShowPanelCanvas()
    {
        GameObject canvasObject = new GameObject("ShowPanelCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 240;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
        raycaster.enabled = false;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;
        canvasRect.localScale = Vector3.one;

        return canvas;
    }

    private static void CreateShowPanelSlider(
        Transform parent,
        string sliderName,
        string iconAssetPath,
        string trackAssetPath,
        string fillAssetPath,
        Vector2 anchoredPosition,
        Color fillColor)
    {
        GameObject rowObject = CreateUIObject(sliderName, parent);
        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(0f, 1f);
        rowRect.pivot = new Vector2(0f, 1f);
        rowRect.anchoredPosition = anchoredPosition;
        rowRect.sizeDelta = new Vector2(236f, 32f);

        CreateRowIcon(rowObject.transform, sliderName, iconAssetPath, fillColor);

        GameObject barObject = CreateUIObject("Bar", rowObject.transform);
        RectTransform barRect = barObject.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 0.5f);
        barRect.anchorMax = new Vector2(0f, 0.5f);
        barRect.pivot = new Vector2(0f, 0.5f);
        barRect.anchoredPosition = new Vector2(34f, -1f);
        barRect.sizeDelta = new Vector2(182f, 16f);

        Image barBackground = barObject.AddComponent<Image>();
        Sprite trackSprite = LoadHudSprite(trackAssetPath);
        if (trackSprite != null)
        {
            barBackground.sprite = trackSprite;
            barBackground.type = Image.Type.Simple;
            barBackground.color = Color.white;
            barBackground.raycastTarget = false;
        }
        else
        {
            ApplyPixelFrame(barBackground, sliderName + "_track", GaugeTrackColor, 64, 20, 5);
        }

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
        fillAreaRect.offsetMin = new Vector2(5f, 3f);
        fillAreaRect.offsetMax = new Vector2(-5f, -3f);

        GameObject fillObject = CreateUIObject("Fill", fillArea.transform);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImage = fillObject.AddComponent<Image>();
        ApplyGaugeFill(fillImage, fillColor, fillAssetPath);
        slider.fillRect = fillRect;

        ValueTrans valueTrans = barObject.AddComponent<ValueTrans>();
        valueTrans.slider = slider;
    }

    private static Slider FindChildSlider(Transform parent, string sliderName)
    {
        if (parent == null)
        {
            return null;
        }

        Transform child = parent.Find(sliderName);
        Slider slider = child != null ? child.GetComponent<Slider>() : null;
        if (slider != null)
        {
            return slider;
        }

        return child != null ? child.GetComponentInChildren<Slider>(true) : null;
    }

    private static ValueTrans EnsureValueTrans(Slider slider)
    {
        if (slider == null)
        {
            return null;
        }

        ValueTrans valueTrans = slider.GetComponent<ValueTrans>();
        if (valueTrans == null)
        {
            valueTrans = slider.gameObject.AddComponent<ValueTrans>();
        }

        valueTrans.slider = slider;
        return valueTrans;
    }

    private static Graphic ResolveFillGraphic(Slider slider)
    {
        return slider != null && slider.fillRect != null ? slider.fillRect.GetComponent<Graphic>() : null;
    }

    private static void EnsureSceneShowPanelVisible(RectTransform panel)
    {
        if (panel == null)
        {
            return;
        }

        Canvas canvas = panel.GetComponentInParent<Canvas>(true);
        if (canvas != null)
        {
            canvas.gameObject.SetActive(true);
            canvas.enabled = true;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 240;
        }

        Transform current = panel.transform;
        while (current != null)
        {
            current.gameObject.SetActive(true);
            if (current is RectTransform rectTransform && rectTransform.localScale.sqrMagnitude <= 0.0001f)
            {
                rectTransform.localScale = Vector3.one;
            }

            current = current.parent;
        }
    }

    private static CanvasGroup EnsureCanvasGroup(GameObject target, bool blocksRaycasts)
    {
        if (target == null)
        {
            return null;
        }

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = blocksRaycasts;
        return canvasGroup;
    }

    private static void HideLegacyGauge(ValueTrans gauge)
    {
        if (gauge == null || IsRuntimeGauge(gauge))
        {
            return;
        }

        Transform legacyRoot = FindAncestorByName(gauge.transform, ScenePanelName);
        if (legacyRoot != null)
        {
            legacyRoot.gameObject.SetActive(false);
            return;
        }

        gauge.gameObject.SetActive(false);
    }

    private static void HideLegacyStatusPanels()
    {
        RectTransform[] rectTransforms = Object.FindObjectsOfType<RectTransform>(true);
        foreach (RectTransform rectTransform in rectTransforms)
        {
            if (rectTransform == null || !string.Equals(rectTransform.name, ScenePanelName, System.StringComparison.Ordinal))
            {
                continue;
            }

            if (rootRect != null && rectTransform.IsChildOf(rootRect))
            {
                continue;
            }

            if (rectTransform.Find(HealthSliderName) == null
                || rectTransform.Find(InkSliderName) == null
                || rectTransform.Find(StructureSliderName) == null)
            {
                continue;
            }

            rectTransform.gameObject.SetActive(false);
        }
    }

    private static Transform FindAncestorByName(Transform start, string objectName)
    {
        Transform current = start;
        while (current != null)
        {
            if (string.Equals(current.name, objectName, System.StringComparison.Ordinal))
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }

    private static void CreateRows(Transform parent)
    {
        ClearRow(parent, "HealthRow");
        ClearRow(parent, "InkRow");
        ClearRow(parent, "StructureRow");

        StatusRow healthRow = CreateRow(
            parent,
            "HealthRow",
            HeartIconAssetPath,
            HealthTrackAssetPath,
            HealthFillAssetPath,
            new Vector2(6f, -5f),
            HealthFillColor);
        StatusRow weaponRow = CreateRow(
            parent,
            "InkRow",
            null,
            InkTrackAssetPath,
            null,
            new Vector2(6f, -29f),
            InkFillColor);
        StatusRow structureRow = CreateRow(
            parent,
            "StructureRow",
            StructureIconAssetPath,
            StructureTrackAssetPath,
            null,
            new Vector2(6f, -53f),
            StructureFillColor);

        healthGauge = healthRow.gauge;
        weaponGauge = weaponRow.gauge;
        structureGauge = structureRow.gauge;
        weaponFillGraphic = weaponRow.fillGraphic;
        structureFillGraphic = structureRow.fillGraphic;
        healthValueText = healthRow.valueText;
        weaponValueText = weaponRow.valueText;
        structureValueText = structureRow.valueText;
    }

    private static void ClearRow(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            Object.Destroy(child.gameObject);
        }
    }

    private static StatusRow CreateRow(
        Transform parent,
        string rowName,
        string iconAssetPath,
        string trackAssetPath,
        string fillAssetPath,
        Vector2 anchoredPosition,
        Color fillColor)
    {
        GameObject rowObject = CreateUIObject(rowName, parent);
        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(0f, 1f);
        rowRect.pivot = new Vector2(0f, 1f);
        rowRect.anchoredPosition = anchoredPosition;
        rowRect.sizeDelta = new Vector2(RowWidth, RowHeight);

        Image background = rowObject.AddComponent<Image>();
        background.color = RowPanelColor;
        background.raycastTarget = false;

        CreateRowIcon(rowObject.transform, rowName, iconAssetPath, fillColor);

        GameObject barObject = CreateUIObject("Bar", rowObject.transform);
        RectTransform barRect = barObject.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 0.5f);
        barRect.anchorMax = new Vector2(0f, 0.5f);
        barRect.pivot = new Vector2(0f, 0.5f);
        barRect.anchoredPosition = new Vector2(26f, -1f);
        barRect.sizeDelta = new Vector2(132f, 11f);

        Image barBackground = barObject.AddComponent<Image>();
        Sprite trackSprite = LoadHudSprite(trackAssetPath);
        if (trackSprite != null)
        {
            barBackground.sprite = trackSprite;
            barBackground.type = Image.Type.Simple;
            barBackground.preserveAspect = false;
            barBackground.color = Color.white;
            barBackground.raycastTarget = false;
        }
        else
        {
            ApplyPixelFrame(barBackground, rowName + "_track", GaugeTrackColor, 64, 20, 5);
        }

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
        fillAreaRect.offsetMin = new Vector2(4f, 2f);
        fillAreaRect.offsetMax = new Vector2(-4f, -2f);

        GameObject fillObject = CreateUIObject("Fill", fillArea.transform);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImage = fillObject.AddComponent<Image>();
        ApplyGaugeFill(fillImage, fillColor, fillAssetPath);
        slider.fillRect = fillRect;

        ValueTrans gauge = barObject.AddComponent<ValueTrans>();
        gauge.slider = slider;

        TextMeshProUGUI valueText = CreateText("Value", rowObject.transform, "100/100", 7f, new Vector2(118f, -6f), new Vector2(36f, 11f), TextAlignmentOptions.MidlineRight);
        valueText.fontStyle = FontStyles.Bold;

        return new StatusRow(gauge, valueText, fillImage);
    }

    private static Image CreateRowIcon(Transform parent, string rowName, string iconAssetPath, Color fallbackColor)
    {
        GameObject iconObject = CreateUIObject("Icon", parent);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(13f, -1f);
        iconRect.sizeDelta = new Vector2(20f, 20f);

        Image iconImage = iconObject.AddComponent<Image>();
        Sprite iconSprite = LoadHudSprite(iconAssetPath);
        if (iconSprite != null)
        {
            iconImage.sprite = iconSprite;
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;
        }
        else
        {
            ApplyPixelFrame(iconImage, rowName + "_icon", fallbackColor, 28, 28, 5);
        }

        iconImage.raycastTarget = false;
        return iconImage;
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
        tmp.raycastTarget = false;
        tmp.font = ResolveHudFont();

        return tmp;
    }

    private static void ApplyPixelFrame(Image image, string key, Color faceColor)
    {
        ApplyPixelFrame(image, key, faceColor, PixelFrameWidth, PixelFrameHeight, PixelFrameBorder);
    }

    private static void ApplyPixelFrame(Image image, string key, Color faceColor, int width, int height, int border)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = GetPixelFrameSprite(key, faceColor, width, height, border);
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private static Sprite GetPixelFrameSprite(string key, Color faceColor, int width, int height, int border)
    {
        string cacheKey = $"pixel_frame_{key}_{width}_{height}_{border}";
        if (HudSpriteCache.TryGetValue(cacheKey, out Sprite cachedSprite) && cachedSprite != null)
        {
            return cachedSprite;
        }

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = cacheKey,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        int cut = Mathf.Max(2, border - 1);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool clippedCorner =
                    (x < cut && y < cut && x + y < cut - 1) ||
                    (x >= width - cut && y < cut && width - 1 - x + y < cut - 1) ||
                    (x < cut && y >= height - cut && x + height - 1 - y < cut - 1) ||
                    (x >= width - cut && y >= height - cut && width - 1 - x + height - 1 - y < cut - 1);

                if (clippedCorner)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                bool outer = x <= 1 || y <= 1 || x >= width - 2 || y >= height - 2;
                bool frame = x < border || y < border || x >= width - border || y >= height - border;
                bool highlight = (x == border || y == height - border - 1) && frame;
                bool shadow = (x == width - border - 1 || y == border) && frame;

                Color pixelColor = faceColor;
                if (outer)
                {
                    pixelColor = DarkOutlineColor;
                }
                else if (highlight)
                {
                    pixelColor = GoldHighlightColor;
                }
                else if (shadow)
                {
                    pixelColor = DarkOutlineColor;
                }
                else if (frame)
                {
                    pixelColor = GoldBorderColor;
                }
                else
                {
                    float vertical = height > 1 ? (float)y / (height - 1) : 0f;
                    pixelColor = Color.Lerp(faceColor * 0.82f, faceColor * 1.08f, vertical);
                    pixelColor.a = faceColor.a;
                }

                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.Apply();
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            100f,
            0u,
            SpriteMeshType.FullRect,
            new Vector4(border, border, border, border));
        sprite.name = cacheKey;
        HudSpriteCache[cacheKey] = sprite;
        return sprite;
    }

    private static void ApplyGaugeFill(Image image, Color fillColor, string fillAssetPath = null)
    {
        if (image == null)
        {
            return;
        }

        Sprite fillSprite = LoadHudSprite(fillAssetPath);
        image.sprite = fillSprite ?? GetGaugeFillSprite();
        image.type = fillSprite != null ? Image.Type.Simple : Image.Type.Sliced;
        image.preserveAspect = false;
        image.color = fillSprite != null ? Color.white : fillColor;
        image.raycastTarget = false;
    }

    private static Sprite LoadHudSprite(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return null;
        }

        string cacheKey = $"asset_{assetPath}";
        if (HudSpriteCache.TryGetValue(cacheKey, out Sprite cachedSprite) && cachedSprite != null)
        {
            return cachedSprite;
        }

        Sprite sprite = RuntimeProjectSpriteLoader.LoadSprite(assetPath, true, SpriteMeshType.FullRect);
        if (sprite != null)
        {
            HudSpriteCache[cacheKey] = sprite;
        }

        return sprite;
    }

    private static Sprite GetGaugeFillSprite()
    {
        const string cacheKey = "pixel_gauge_fill";
        if (HudSpriteCache.TryGetValue(cacheKey, out Sprite cachedSprite) && cachedSprite != null)
        {
            return cachedSprite;
        }

        Texture2D texture = new Texture2D(PixelFillWidth, PixelFillHeight, TextureFormat.RGBA32, false)
        {
            name = cacheKey,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int y = 0; y < PixelFillHeight; y++)
        {
            for (int x = 0; x < PixelFillWidth; x++)
            {
                float vertical = PixelFillHeight > 1 ? (float)y / (PixelFillHeight - 1) : 0f;
                Color pixelColor = Color.Lerp(new Color(0.55f, 0.55f, 0.55f, 1f), Color.white, vertical);
                if (y <= 1)
                {
                    pixelColor = new Color(0.36f, 0.36f, 0.36f, 1f);
                }
                else if (y >= PixelFillHeight - 3)
                {
                    pixelColor = Color.white;
                }

                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.Apply();
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, PixelFillWidth, PixelFillHeight),
            new Vector2(0.5f, 0.5f),
            100f,
            0u,
            SpriteMeshType.FullRect,
            new Vector4(4f, 4f, 4f, 4f));
        sprite.name = cacheKey;
        HudSpriteCache[cacheKey] = sprite;
        return sprite;
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
        rootRect.sizeDelta = new Vector2(RootWidth, RootHeight);
        rootRect.localScale = Vector3.one;
        rootRect.SetAsLastSibling();
    }

    private readonly struct StatusRow
    {
        public readonly ValueTrans gauge;
        public readonly TextMeshProUGUI valueText;
        public readonly Graphic fillGraphic;

        public StatusRow(ValueTrans gauge, TextMeshProUGUI valueText, Graphic fillGraphic)
        {
            this.gauge = gauge;
            this.valueText = valueText;
            this.fillGraphic = fillGraphic;
        }
    }
}

public sealed class RuntimeFollowPromptHud : MonoBehaviour
{
    private const string PlayerInteractPromptId = "player_interact";
    private const string CanvasName = "RuntimeFollowPromptCanvas";
    private const int OverlaySortingOrder = 244;
    private const float SideOffset = 48f;
    private const float VerticalOffset = -6f;
    private const float VerticalSpacing = 30f;
    private const float ScreenMargin = 18f;
    private const float AnchorWorldHeightOffset = 0.04f;
    private const float PlayerInteractDistanceMultiplier = 1.4f;
    private const float VisibilityAnimationDuration = 0.18f;
    private const float ShowSlideDistance = 36f;
    private const float HideSlideDistance = 30f;
    private const float HiddenScale = 0.97f;
    private const float MaxBlurDistance = 5f;
    private const float MaxBlurAlpha = 0.22f;

    private static readonly Color PromptBackgroundColor = new Color(0.08f, 0.06f, 0.04f, 0.82f);
    private static readonly Color KeyChipColor = new Color(0.86f, 0.67f, 0.34f, 1f);
    private static readonly Color KeyTextColor = new Color(0.11f, 0.08f, 0.05f, 1f);
    private static readonly Color LabelTextColor = new Color(0.96f, 0.91f, 0.80f, 1f);
    private static readonly Color MotionBlurColor = new Color(0.10f, 0.08f, 0.05f, 1f);

    private sealed class PromptBinding
    {
        public string Id;
        public Transform Anchor;
        public RectTransform Root;
        public CanvasGroup RootCanvasGroup;
        public Image RootBackground;
        public HorizontalLayoutGroup RootLayout;
        public Image KeyBackground;
        public LayoutElement KeyLayout;
        public LayoutElement LabelLayout;
        public TextMeshProUGUI KeyText;
        public TextMeshProUGUI LabelText;
        public Shadow RootShadow;
        public Shadow KeyShadow;
        public Shadow KeyTextShadow;
        public Shadow LabelTextShadow;
        public int Priority;
        public bool RequestedVisible;
        public float VisibilityProgress;
        public Vector2 LastResolvedLocalPoint;
        public bool HasResolvedLocalPoint;
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
            if (binding.Root == null)
            {
                continue;
            }

            bool canResolveTarget = binding.RequestedVisible &&
                                    binding.Anchor != null &&
                                    mainCamera != null;

            if (canResolveTarget)
            {
                activeBindings.Add(binding);
                continue;
            }

            UpdatePromptPresentation(
                binding,
                binding.HasResolvedLocalPoint ? binding.LastResolvedLocalPoint : binding.Root.anchoredPosition,
                false);
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
            typeof(CanvasGroup),
            typeof(HorizontalLayoutGroup),
            typeof(ContentSizeFitter),
            typeof(Shadow));
        rootObject.transform.SetParent(canvasRect, false);

        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(0f, 0f);

        Image background = rootObject.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(background, PromptBackgroundColor, 6, 8, 1.2f);
        background.raycastTarget = false;

        CanvasGroup canvasGroup = rootObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Shadow rootShadow = rootObject.GetComponent<Shadow>();
        ConfigurePromptShadow(rootShadow);

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
            typeof(LayoutElement),
            typeof(Shadow));
        keyChip.transform.SetParent(rootObject.transform, false);

        Image keyBackground = keyChip.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(keyBackground, KeyChipColor, 4, 6, 1.2f);
        keyBackground.raycastTarget = false;

        Shadow keyShadow = keyChip.GetComponent<Shadow>();
        ConfigurePromptShadow(keyShadow);

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
        Shadow keyTextShadow = keyText.gameObject.AddComponent<Shadow>();
        ConfigurePromptShadow(keyTextShadow);

        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(LayoutElement),
            typeof(TextMeshProUGUI),
            typeof(Shadow));
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

        Shadow labelTextShadow = labelObject.GetComponent<Shadow>();
        ConfigurePromptShadow(labelTextShadow);

        PromptBinding binding = new PromptBinding
        {
            Id = promptId,
            Root = rootRect,
            RootCanvasGroup = canvasGroup,
            RootBackground = background,
            RootLayout = layout,
            KeyBackground = keyBackground,
            KeyLayout = keyLayout,
            LabelLayout = labelLayout,
            KeyText = keyText,
            LabelText = labelText,
            RootShadow = rootShadow,
            KeyShadow = keyShadow,
            KeyTextShadow = keyTextShadow,
            LabelTextShadow = labelTextShadow
        };

        ApplyPromptMetrics(binding);
        ApplyPromptMotionBlur(binding, 0f, true);
        rootRect.localScale = new Vector3(HiddenScale, HiddenScale, 1f);
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

        binding.RootCanvasGroup ??= binding.Root.GetComponent<CanvasGroup>();
        binding.RootBackground ??= binding.Root.GetComponent<Image>();
        binding.RootLayout ??= binding.Root.GetComponent<HorizontalLayoutGroup>();
        binding.RootShadow ??= binding.Root.GetComponent<Shadow>();
        Transform keyChip = binding.Root.Find("KeyChip");
        Transform label = binding.Root.Find("Label");
        binding.KeyBackground ??= keyChip?.GetComponent<Image>();
        binding.KeyLayout ??= binding.Root.Find("KeyChip")?.GetComponent<LayoutElement>();
        binding.LabelLayout ??= binding.Root.Find("Label")?.GetComponent<LayoutElement>();
        binding.KeyShadow ??= keyChip?.GetComponent<Shadow>();
        binding.KeyText ??= binding.Root.Find("KeyChip/KeyText")?.GetComponent<TextMeshProUGUI>();
        binding.KeyTextShadow ??= binding.Root.Find("KeyChip/KeyText")?.GetComponent<Shadow>();
        binding.LabelText ??= label?.GetComponent<TextMeshProUGUI>();
        binding.LabelTextShadow ??= label?.GetComponent<Shadow>();
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

        ConfigurePromptShadow(binding.RootShadow);
        ConfigurePromptShadow(binding.KeyShadow);
        ConfigurePromptShadow(binding.KeyTextShadow);
        ConfigurePromptShadow(binding.LabelTextShadow);
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

        LayoutRebuilder.ForceRebuildLayoutImmediate(binding.Root);

        float promptWidth = Mathf.Max(binding.Root.rect.width, 112f);
        float promptHeight = Mathf.Max(binding.Root.rect.height, 26f);
        bool preferRight = screenPoint.x <= Screen.width * 0.68f;
        float promptSideOffset = GetPromptSideOffset(binding);
        float targetScreenX = preferRight
            ? screenPoint.x + promptSideOffset + promptWidth * 0.5f
            : screenPoint.x - promptSideOffset - promptWidth * 0.5f;
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
            binding.LastResolvedLocalPoint = localPoint;
            binding.HasResolvedLocalPoint = true;
            UpdatePromptPresentation(binding, localPoint, true);
        }
    }

    private void UpdatePromptPresentation(PromptBinding binding, Vector2 targetLocalPoint, bool visible)
    {
        if (binding?.Root == null)
        {
            return;
        }

        float duration = Mathf.Max(0.0001f, VisibilityAnimationDuration);
        float progressStep = Time.unscaledDeltaTime / duration;
        float targetProgress = visible ? 1f : 0f;

        binding.VisibilityProgress = Mathf.MoveTowards(binding.VisibilityProgress, targetProgress, progressStep);

        if (!visible && binding.VisibilityProgress <= 0.0001f)
        {
            if (binding.RootCanvasGroup != null)
            {
                binding.RootCanvasGroup.alpha = 0f;
            }

            binding.Root.localScale = new Vector3(HiddenScale, HiddenScale, 1f);
            ApplyPromptMotionBlur(binding, 0f, false);
            if (binding.Root.gameObject.activeSelf)
            {
                binding.Root.gameObject.SetActive(false);
            }

            return;
        }

        if (!binding.Root.gameObject.activeSelf)
        {
            binding.Root.gameObject.SetActive(true);
        }

        float easedProgress = Mathf.SmoothStep(0f, 1f, binding.VisibilityProgress);
        float blurAmount = 1f - easedProgress;
        float slideDistance = visible ? ShowSlideDistance : HideSlideDistance;
        float slideOffsetX = slideDistance * blurAmount;

        binding.Root.anchoredPosition = targetLocalPoint + new Vector2(slideOffsetX, 0f);
        binding.Root.localScale = Vector3.one * Mathf.Lerp(HiddenScale, 1f, easedProgress);

        if (binding.RootCanvasGroup != null)
        {
            binding.RootCanvasGroup.alpha = easedProgress;
        }

        ApplyPromptMotionBlur(binding, blurAmount, visible);
    }

    private static float GetPromptSideOffset(PromptBinding binding)
    {
        if (binding != null && binding.Id == PlayerInteractPromptId)
        {
            return SideOffset * PlayerInteractDistanceMultiplier;
        }

        return SideOffset;
    }

    private static void ConfigurePromptShadow(Shadow shadow)
    {
        if (shadow == null)
        {
            return;
        }

        shadow.effectColor = Color.clear;
        shadow.effectDistance = Vector2.zero;
        shadow.useGraphicAlpha = true;
        shadow.enabled = false;
    }

    private static void ApplyPromptMotionBlur(PromptBinding binding, float blurAmount, bool visible)
    {
        if (binding == null)
        {
            return;
        }

        float clampedBlur = Mathf.Clamp01(blurAmount);
        float blurDistance = MaxBlurDistance * clampedBlur;
        float blurAlpha = MaxBlurAlpha * clampedBlur;
        float direction = visible ? 1f : -1f;

        ApplyShadowState(binding.RootShadow, blurDistance * direction, blurAlpha * 0.9f);
        ApplyShadowState(binding.KeyShadow, blurDistance * 0.85f * direction, blurAlpha * 0.85f);
        ApplyShadowState(binding.KeyTextShadow, blurDistance * 0.75f * direction, blurAlpha);
        ApplyShadowState(binding.LabelTextShadow, blurDistance * direction, blurAlpha);
    }

    private static void ApplyShadowState(Shadow shadow, float horizontalOffset, float alpha)
    {
        if (shadow == null)
        {
            return;
        }

        if (alpha <= 0.001f)
        {
            shadow.enabled = false;
            shadow.effectColor = Color.clear;
            shadow.effectDistance = Vector2.zero;
            return;
        }

        shadow.enabled = true;
        shadow.effectColor = new Color(MotionBlurColor.r, MotionBlurColor.g, MotionBlurColor.b, alpha);
        shadow.effectDistance = new Vector2(horizontalOffset, 0f);
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
