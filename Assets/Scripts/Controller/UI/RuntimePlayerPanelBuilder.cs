using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class RuntimePlayerPanelBuilder
{
    public static SpiritPanelUI Create(Transform parent, string rootName = "SpiritPanel")
    {
        if (parent == null)
        {
            return null;
        }

        GameObject root = CreateModalRoot(rootName, parent);
        GameObject panel = CreateCenteredPanel("SpiritContent", root.transform, new Vector2(940f, 650f));
        Image panelBackground = panel.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplySpiritPanelFrameSprite(panelBackground, Color.white);

        GameObject avatarPanel = CreateUIObject("AvatarPanel", panel.transform);
        SetCenteredRect(avatarPanel.GetComponent<RectTransform>(), new Vector2(-252f, -14f), new Vector2(246f, 392f));
        Image avatarPanelImage = avatarPanel.AddComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(avatarPanelImage, new Color(0.13f, 0.17f, 0.14f, 0.20f), 18, 16, 1.2f);

        GameObject avatarFrame = CreateUIObject("AvatarFrame", avatarPanel.transform);
        SetCenteredRect(avatarFrame.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(198f, 310f));
        Image avatarFrameImage = avatarFrame.AddComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(avatarFrameImage, new Color(0.97f, 0.97f, 0.95f, 0.88f), 16, 14, 1.2f);

        GameObject avatarObject = CreateUIObject("Avatar", avatarFrame.transform);
        SetStretch(avatarObject.GetComponent<RectTransform>(), 18f, 18f, 18f, 18f);
        Image avatarImage = avatarObject.AddComponent<Image>();
        avatarImage.color = Color.white;
        avatarImage.preserveAspect = true;

        GameObject contentPanel = CreateUIObject("ContentPanel", panel.transform);
        SetCenteredRect(contentPanel.GetComponent<RectTransform>(), new Vector2(150f, -18f), new Vector2(500f, 408f));
        Image contentPanelImage = contentPanel.AddComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(contentPanelImage, new Color(0.15f, 0.18f, 0.14f, 0.16f), 18, 16, 1.2f);

        TextMeshProUGUI title = CreateText(
            "Title",
            panel.transform,
            "精灵 · 玩家属性",
            40f,
            new Color(0.20f, 0.24f, 0.16f, 1f),
            TextAlignmentOptions.Center);
        SetCenteredRect(title.rectTransform, new Vector2(0f, 252f), new Vector2(620f, 66f));

        Button closeButton = CreateButton("CloseButton", panel.transform, "×", new Color(0.19f, 0.24f, 0.17f, 0.92f), new Vector2(58f, 42f));
        RuntimeUiSpriteFactory.ApplyRoundedSprite(closeButton.GetComponent<Image>(), new Color(0.19f, 0.24f, 0.17f, 0.92f), 12, 12, 1.2f);
        closeButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(402f, 258f);

        Button statsTabButton = CreateButton("StatsTabButton", panel.transform, "属性", new Color(0.33f, 0.42f, 0.28f, 0.94f), new Vector2(132f, 44f));
        RuntimeUiSpriteFactory.ApplyRoundedSprite(statsTabButton.GetComponent<Image>(), new Color(0.33f, 0.42f, 0.28f, 0.94f), 12, 12, 1.2f);
        statsTabButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(78f, 188f);

        Button weaponTabButton = CreateButton("WeaponTabButton", panel.transform, "墨水", new Color(0.25f, 0.28f, 0.22f, 0.94f), new Vector2(132f, 44f));
        RuntimeUiSpriteFactory.ApplyRoundedSprite(weaponTabButton.GetComponent<Image>(), new Color(0.25f, 0.28f, 0.22f, 0.94f), 12, 12, 1.2f);
        weaponTabButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(226f, 188f);

        GameObject statsPage = CreateUIObject("StatsPage", contentPanel.transform);
        SetStretch(statsPage.GetComponent<RectTransform>(), 26f, 26f, 26f, 26f);
        PlayerStatsPanelUI statsPanel = statsPage.AddComponent<PlayerStatsPanelUI>();
        BuildStatsPage(statsPage.transform, statsPanel, avatarImage);

        GameObject weaponPage = CreateUIObject("WeaponPage", contentPanel.transform);
        SetStretch(weaponPage.GetComponent<RectTransform>(), 26f, 26f, 26f, 26f);
        WeaponSelectionPanelUI weaponPanel = weaponPage.AddComponent<WeaponSelectionPanelUI>();
        BuildWeaponPage(weaponPage.transform, weaponPanel);

        SpiritPanelUI spiritPanel = root.AddComponent<SpiritPanelUI>();
        spiritPanel.Configure(statsPage, weaponPage, statsTabButton, weaponTabButton, closeButton, title, statsPanel, weaponPanel);

        root.SetActive(false);
        return spiritPanel;
    }

    private static void BuildStatsPage(Transform parent, PlayerStatsPanelUI statsPanel, Image avatarImage)
    {
        GameObject rows = CreateUIObject("StatRows", parent);
        SetStretch(rows.GetComponent<RectTransform>(), 4f, 8f, 4f, 8f);
        VerticalLayoutGroup layout = rows.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 16f;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.padding = new RectOffset(6, 6, 10, 10);

        TextMeshProUGUI health = CreateRowText(rows.transform, "生命：-");
        TextMeshProUGUI maxHealth = CreateRowText(rows.transform, "生命上限：-");
        TextMeshProUGUI durability = CreateRowText(rows.transform, "耐久：-");
        TextMeshProUGUI attack = CreateRowText(rows.transform, "攻击力：-");
        TextMeshProUGUI moveSpeed = CreateRowText(rows.transform, "移动速度：-");
        TextMeshProUGUI defense = CreateRowText(rows.transform, "防御力：-");

        statsPanel.Configure(avatarImage, health, maxHealth, durability, attack, moveSpeed, defense);
    }

    private static void BuildWeaponPage(Transform parent, WeaponSelectionPanelUI weaponPanel)
    {
        Image summaryCard = CreateImage("WeaponSummaryCard", parent, new Color(0.19f, 0.22f, 0.18f, 0.84f), 14, 12);
        RectTransform summaryRect = summaryCard.rectTransform;
        summaryRect.anchorMin = new Vector2(0f, 1f);
        summaryRect.anchorMax = new Vector2(1f, 1f);
        summaryRect.pivot = new Vector2(0.5f, 1f);
        summaryRect.offsetMin = new Vector2(2f, -104f);
        summaryRect.offsetMax = new Vector2(-2f, -4f);

        TextMeshProUGUI summaryTitle = CreateText(
            "SummaryTitle",
            summaryCard.transform,
            "当前构筑",
            21f,
            new Color(0.96f, 0.83f, 0.52f, 1f),
            TextAlignmentOptions.MidlineLeft);
        summaryTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
        summaryTitle.rectTransform.anchorMax = new Vector2(0f, 1f);
        summaryTitle.rectTransform.pivot = new Vector2(0f, 1f);
        summaryTitle.rectTransform.anchoredPosition = new Vector2(20f, -14f);
        summaryTitle.rectTransform.sizeDelta = new Vector2(180f, 26f);
        summaryTitle.enableWordWrapping = false;

        TextMeshProUGUI summaryText = CreateText(
            "SummaryText",
            summaryCard.transform,
            "基础墨水：-\n当前实战墨水：-\n当前攻击按基础墨水生效。",
            17f,
            new Color(0.92f, 0.89f, 0.82f, 1f),
            TextAlignmentOptions.TopLeft);
        summaryText.rectTransform.anchorMin = new Vector2(0f, 0f);
        summaryText.rectTransform.anchorMax = new Vector2(1f, 1f);
        summaryText.rectTransform.offsetMin = new Vector2(20f, 16f);
        summaryText.rectTransform.offsetMax = new Vector2(-20f, -40f);
        summaryText.lineSpacing = 6f;
        weaponPanel.ConfigureSummary(summaryText);

        GameObject list = CreateUIObject("WeaponOptions", parent);
        RectTransform listRect = list.GetComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0f, 0f);
        listRect.anchorMax = new Vector2(1f, 1f);
        listRect.offsetMin = new Vector2(2f, 8f);
        listRect.offsetMax = new Vector2(-2f, -114f);
        VerticalLayoutGroup layout = list.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.padding = new RectOffset(4, 4, 6, 6);

        CreateWeaponOption(list.transform, weaponPanel, WeaponType.DirectInk, "直墨", InkTypeCatalog.GetEffectDescription(WeaponType.DirectInk));
        CreateWeaponOption(list.transform, weaponPanel, WeaponType.BurstInk, "爆墨", InkTypeCatalog.GetEffectDescription(WeaponType.BurstInk));
        CreateWeaponOption(list.transform, weaponPanel, WeaponType.PierceInk, "贯墨", InkTypeCatalog.GetEffectDescription(WeaponType.PierceInk));
        CreateWeaponOption(list.transform, weaponPanel, WeaponType.FlowInk, "流墨", InkTypeCatalog.GetEffectDescription(WeaponType.FlowInk));
    }

    private static void CreateWeaponOption(
        Transform parent,
        WeaponSelectionPanelUI weaponPanel,
        WeaponType type,
        string title,
        string description)
    {
        Button button = CreateButton($"{type}Button", parent, string.Empty, new Color(0.19f, 0.22f, 0.18f, 0.92f), new Vector2(432f, 82f));
        LayoutElement layoutElement = button.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 82f;

        Image background = button.GetComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(background, new Color(0.19f, 0.22f, 0.18f, 0.92f), 14, 12, 1.2f);

        TextMeshProUGUI titleText = CreateText("Title", button.transform, title, 24f, new Color(0.96f, 0.83f, 0.52f, 1f), TextAlignmentOptions.MidlineLeft);
        titleText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        titleText.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        titleText.rectTransform.pivot = new Vector2(0f, 0.5f);
        titleText.rectTransform.anchoredPosition = new Vector2(24f, 17f);
        titleText.rectTransform.sizeDelta = new Vector2(240f, 28f);
        titleText.enableWordWrapping = false;

        TextMeshProUGUI descText = CreateText("Description", button.transform, description, 17f, new Color(0.86f, 0.80f, 0.70f, 1f), TextAlignmentOptions.MidlineLeft);
        descText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        descText.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        descText.rectTransform.pivot = new Vector2(0f, 0.5f);
        descText.rectTransform.anchoredPosition = new Vector2(24f, -15f);
        descText.rectTransform.sizeDelta = new Vector2(276f, 24f);
        descText.enableWordWrapping = false;

        TextMeshProUGUI stateText = CreateText("State", button.transform, "点击装备", 18f, Color.white, TextAlignmentOptions.Center);
        stateText.rectTransform.anchorMin = new Vector2(1f, 0.5f);
        stateText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        stateText.rectTransform.pivot = new Vector2(1f, 0.5f);
        stateText.rectTransform.anchoredPosition = new Vector2(-20f, 0f);
        stateText.rectTransform.sizeDelta = new Vector2(124f, 40f);
        stateText.enableWordWrapping = true;

        WeaponOptionData data = new WeaponOptionData
        {
            weaponType = type,
            displayName = title,
            description = description
        };
        weaponPanel.RegisterOption(data, button, background, stateText);
    }

    private static GameObject CreateModalRoot(string name, Transform parent)
    {
        GameObject root = CreateUIObject(name, parent);
        SetStretch(root.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

        Image overlay = root.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.58f);
        root.AddComponent<CanvasGroup>();
        return root;
    }

    private static GameObject CreateCenteredPanel(string name, Transform parent, Vector2 size)
    {
        GameObject panel = CreateUIObject(name, parent);
        SetCenteredRect(panel.GetComponent<RectTransform>(), Vector2.zero, size);
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.10f, 0.08f, 0.06f, 0.97f);
        return panel;
    }

    private static Button CreateButton(string name, Transform parent, string label, Color color, Vector2 size)
    {
        GameObject buttonObject = CreateUIObject(name, parent);
        SetCenteredRect(buttonObject.GetComponent<RectTransform>(), Vector2.zero, size);

        Image image = buttonObject.AddComponent<Image>();
        image.color = color;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        if (!string.IsNullOrEmpty(label))
        {
            TextMeshProUGUI labelText = CreateText("Label", buttonObject.transform, label, 26f, Color.white, TextAlignmentOptions.Center);
            labelText.enableWordWrapping = false;
        }

        return button;
    }

    private static Image CreateImage(string name, Transform parent, Color color, int radius, int border)
    {
        GameObject imageObject = CreateUIObject(name, parent);
        Image image = imageObject.AddComponent<Image>();
        RuntimeUiSpriteFactory.ApplyRoundedSprite(image, color, radius, border, 1.2f);
        return image;
    }

    private static TextMeshProUGUI CreateRowText(Transform parent, string value)
    {
        TextMeshProUGUI text = CreateText("Row", parent, value, 26f, new Color(0.93f, 0.88f, 0.78f, 1f), TextAlignmentOptions.MidlineLeft);
        LayoutElement layoutElement = text.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 34f;
        return text;
    }

    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        string value,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateUIObject(name, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = !string.IsNullOrEmpty(value)
            ? TmpRuntimeFontFallback.WarmupCharacters(value)
            : TmpRuntimeFontFallback.EnsureChineseFallback() ?? TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        SetStretch(text.rectTransform, 0f, 0f, 0f, 0f);
        return text;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static void SetStretch(RectTransform rect, float left, float top, float right, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void SetCenteredRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }
}
