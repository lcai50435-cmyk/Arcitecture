using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class GameplayStatusHudRuntime
{
    private const string HudFontResourcePath = "Fonts & Materials/LiberationSans SDF";
    private const string CanvasName = "GameplayStatusHudCanvas";
    private const string RootName = "GameplayStatusHudRoot";

    private static Canvas hudCanvas;
    private static TMP_FontAsset hudFontAsset;
    private static RectTransform rootRect;
    private static ValueTrans healthGauge;
    private static ValueTrans weaponGauge;
    private static Graphic weaponFillGraphic;
    private static TextMeshProUGUI healthValueText;
    private static TextMeshProUGUI weaponValueText;
    private static TextMeshProUGUI countdownText;

    public static ValueTrans EnsureHealthGauge(ValueTrans currentGauge)
    {
        if (currentGauge != null)
        {
            if (IsRuntimeGauge(currentGauge))
            {
                EnsureRoot();
                return currentGauge;
            }

            UseLegacyHealthGauge(currentGauge);
            return currentGauge;
        }

        EnsureRoot();
        return healthGauge;
    }

    public static ValueTrans EnsureWeaponGauge(ValueTrans currentGauge)
    {
        if (currentGauge != null)
        {
            if (IsRuntimeGauge(currentGauge))
            {
                EnsureRoot();
                return currentGauge;
            }

            UseLegacyWeaponGauge(currentGauge);
            return currentGauge;
        }

        EnsureRoot();
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

        return countdownText;
    }

    private static void EnsureRoot()
    {
        RuntimeMiniMapHud.EnsureInstance();

        if (rootRect != null && healthGauge != null && weaponGauge != null)
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
        rootRect.gameObject.SetActive(true);

        if (healthGauge == null || weaponGauge == null)
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

        CanvasGroup canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
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
