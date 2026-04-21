using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BaseHubBootstrapper : MonoBehaviour
{
    [Header("运行时生成")]
    [SerializeField] private bool buildOnStart = true;

    [Header("可选美术资源")]
    [SerializeField] private Sprite playerSprite;
    [SerializeField] private Sprite avatarSprite;
    [SerializeField] private Sprite bookSprite;
    [SerializeField] private Sprite spiritSprite;

    private Sprite generatedPlayerSprite;
    private Sprite generatedBookSprite;
    private Sprite generatedSpiritSprite;
    private Sprite generatedGateSprite;
    private Sprite generatedFloorSprite;

    private void Start()
    {
        if (!buildOnStart) return;
        if (FindObjectOfType<BaseHubUIController>() != null) return;

        BuildBaseHub();
    }

    private void BuildBaseHub()
    {
        EnsureCamera();
        EnsureEventSystem();

        CreateFloor();

        Canvas canvas = CreateCanvas();
        InteractPrompt prompt = CreateInteractPrompt(canvas.transform);
        Button bookCloseButton;
        GameObject handbookPanel = CreateHandbookPanel(canvas.transform, out bookCloseButton);
        SpiritPanelUI spiritPanel = CreateSpiritPanel(canvas.transform);

        BaseHubUIController uiController = new GameObject("BaseHubUIController").AddComponent<BaseHubUIController>();
        GameObject player = CreatePlayer(prompt);
        CharacterCore characterCore = player.GetComponent<CharacterCore>();
        PlayerProfileData profileData = player.GetComponent<PlayerProfileData>();

        spiritPanel.Bind(characterCore, profileData);
        uiController.Configure(player, handbookPanel, spiritPanel, prompt.Root);
        spiritPanel.SetCloseAction(uiController.CloseAll);
        bookCloseButton.onClick.AddListener(uiController.CloseAll);

        CreateBookInteractable(uiController);
        CreateSpiritInteractable(uiController);
        CreateGameSceneInteractable();
    }

    private void EnsureCamera()
    {
        if (Camera.main != null) return;

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 5.5f;
        camera.backgroundColor = new Color(0.12f, 0.16f, 0.14f, 1f);
        camera.transform.position = new Vector3(0f, 0f, -10f);
        cameraObject.AddComponent<AudioListener>();
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private void CreateFloor()
    {
        GameObject floor = new GameObject("BaseGround");
        SpriteRenderer renderer = floor.AddComponent<SpriteRenderer>();
        renderer.sprite = GetOrCreateGeneratedSprite(ref generatedFloorSprite, new Color(0.20f, 0.28f, 0.20f, 1f));
        renderer.size = new Vector2(14f, 9f);
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.sortingOrder = -10;
        floor.transform.localScale = Vector3.one;
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("BaseHubCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private InteractPrompt CreateInteractPrompt(Transform parent)
    {
        GameObject root = CreateUIObject("InteractPrompt", parent);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0f);
        rootRect.anchorMax = new Vector2(0.5f, 0f);
        rootRect.pivot = new Vector2(0.5f, 0f);
        rootRect.anchoredPosition = new Vector2(0f, 54f);
        rootRect.sizeDelta = new Vector2(420f, 72f);

        Image background = root.AddComponent<Image>();
        background.color = new Color(0.05f, 0.04f, 0.03f, 0.84f);

        GameObject keyObject = CreateUIObject("FKey", root.transform);
        RectTransform keyRect = keyObject.GetComponent<RectTransform>();
        keyRect.anchorMin = new Vector2(0f, 0.5f);
        keyRect.anchorMax = new Vector2(0f, 0.5f);
        keyRect.pivot = new Vector2(0f, 0.5f);
        keyRect.anchoredPosition = new Vector2(26f, 0f);
        keyRect.sizeDelta = new Vector2(52f, 52f);
        Image keyImage = keyObject.AddComponent<Image>();
        keyImage.color = new Color(0.86f, 0.67f, 0.34f, 1f);
        CreateText("FKeyText", keyObject.transform, "F", 30, Color.black, TextAlignmentOptions.Center);

        TextMeshProUGUI tipText = CreateText(
            "TipText",
            root.transform,
            "交互",
            28,
            new Color(0.96f, 0.91f, 0.80f, 1f),
            TextAlignmentOptions.MidlineLeft);
        RectTransform tipRect = tipText.rectTransform;
        tipRect.anchorMin = new Vector2(0f, 0f);
        tipRect.anchorMax = new Vector2(1f, 1f);
        tipRect.offsetMin = new Vector2(96f, 0f);
        tipRect.offsetMax = new Vector2(-24f, 0f);

        root.SetActive(false);
        return new InteractPrompt(root, keyObject, tipText);
    }

    private GameObject CreateHandbookPanel(Transform parent, out Button closeButton)
    {
        GameObject root = CreateModalRoot("HandbookPanel", parent);
        GameObject panel = CreateCenteredPanel("HandbookContent", root.transform, new Vector2(780f, 520f));

        TextMeshProUGUI title = CreateText("Title", panel.transform, "建筑图鉴", 44, new Color(0.96f, 0.83f, 0.52f, 1f), TextAlignmentOptions.Center);
        SetCenteredRect(title.rectTransform, new Vector2(0f, 180f), new Vector2(600f, 70f));

        TextMeshProUGUI body = CreateText(
            "Body",
            panel.transform,
            "这里记录已发现的建筑、结构材料与解锁信息。\n靠近基地中的图鉴并按 F 即可随时查看。",
            28,
            new Color(0.93f, 0.88f, 0.78f, 1f),
            TextAlignmentOptions.Center);
        SetCenteredRect(body.rectTransform, new Vector2(0f, 20f), new Vector2(640f, 180f));

        closeButton = CreateButton("CloseButton", panel.transform, "关闭", new Color(0.53f, 0.24f, 0.16f, 1f));
        closeButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -190f);

        root.SetActive(false);
        return root;
    }

    private SpiritPanelUI CreateSpiritPanel(Transform parent)
    {
        GameObject root = CreateModalRoot("SpiritPanel", parent);
        GameObject panel = CreateCenteredPanel("SpiritContent", root.transform, new Vector2(860f, 600f));

        TextMeshProUGUI title = CreateText(
            "Title",
            panel.transform,
            "精灵 · 玩家属性",
            40,
            new Color(0.96f, 0.83f, 0.52f, 1f),
            TextAlignmentOptions.Center);
        SetCenteredRect(title.rectTransform, new Vector2(0f, 234f), new Vector2(600f, 66f));

        Button closeButton = CreateButton("CloseButton", panel.transform, "×", new Color(0.42f, 0.16f, 0.12f, 1f), new Vector2(64f, 48f));
        closeButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(372f, 252f);

        Button statsTabButton = CreateButton("StatsTabButton", panel.transform, "属性", new Color(0.38f, 0.25f, 0.12f, 1f), new Vector2(140f, 48f));
        statsTabButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-90f, 176f);

        Button weaponTabButton = CreateButton("WeaponTabButton", panel.transform, "武器", new Color(0.22f, 0.18f, 0.14f, 1f), new Vector2(140f, 48f));
        weaponTabButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(90f, 176f);

        GameObject statsPage = CreateUIObject("StatsPage", panel.transform);
        SetCenteredRect(statsPage.GetComponent<RectTransform>(), new Vector2(0f, -36f), new Vector2(720f, 360f));
        PlayerStatsPanelUI statsPanel = statsPage.AddComponent<PlayerStatsPanelUI>();
        BuildStatsPage(statsPage.transform, statsPanel);

        GameObject weaponPage = CreateUIObject("WeaponPage", panel.transform);
        SetCenteredRect(weaponPage.GetComponent<RectTransform>(), new Vector2(0f, -36f), new Vector2(720f, 360f));
        WeaponSelectionPanelUI weaponPanel = weaponPage.AddComponent<WeaponSelectionPanelUI>();
        BuildWeaponPage(weaponPage.transform, weaponPanel);

        SpiritPanelUI spiritPanel = root.AddComponent<SpiritPanelUI>();
        spiritPanel.Configure(statsPage, weaponPage, statsTabButton, weaponTabButton, closeButton, title, statsPanel, weaponPanel);

        root.SetActive(false);
        return spiritPanel;
    }

    private void BuildStatsPage(Transform parent, PlayerStatsPanelUI statsPanel)
    {
        GameObject avatarFrame = CreateUIObject("AvatarFrame", parent);
        SetCenteredRect(avatarFrame.GetComponent<RectTransform>(), new Vector2(-240f, 72f), new Vector2(170f, 170f));
        Image frameImage = avatarFrame.AddComponent<Image>();
        frameImage.color = new Color(0.17f, 0.14f, 0.10f, 1f);

        GameObject avatarObject = CreateUIObject("Avatar", avatarFrame.transform);
        SetStretch(avatarObject.GetComponent<RectTransform>(), 16f, 16f, 16f, 16f);
        Image avatarImage = avatarObject.AddComponent<Image>();
        avatarImage.color = new Color(0.92f, 0.78f, 0.52f, 1f);
        avatarImage.preserveAspect = true;

        GameObject rows = CreateUIObject("StatRows", parent);
        SetCenteredRect(rows.GetComponent<RectTransform>(), new Vector2(130f, 32f), new Vector2(430f, 280f));
        VerticalLayoutGroup layout = rows.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 14f;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        TextMeshProUGUI health = CreateRowText(rows.transform, "生命：-");
        TextMeshProUGUI maxHealth = CreateRowText(rows.transform, "生命上限：-");
        TextMeshProUGUI durability = CreateRowText(rows.transform, "耐久：-");
        TextMeshProUGUI attack = CreateRowText(rows.transform, "攻击力：-");
        TextMeshProUGUI moveSpeed = CreateRowText(rows.transform, "移动速度：-");
        TextMeshProUGUI defense = CreateRowText(rows.transform, "防御力：-");

        statsPanel.Configure(avatarImage, health, maxHealth, durability, attack, moveSpeed, defense);
    }

    private void BuildWeaponPage(Transform parent, WeaponSelectionPanelUI weaponPanel)
    {
        GameObject list = CreateUIObject("WeaponOptions", parent);
        SetCenteredRect(list.GetComponent<RectTransform>(), Vector2.zero, new Vector2(680f, 330f));
        VerticalLayoutGroup layout = list.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 18f;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        CreateWeaponOption(list.transform, weaponPanel, WeaponType.Melee, "近战", "稳定直接，适合贴身输出。");
        CreateWeaponOption(list.transform, weaponPanel, WeaponType.Ranged, "远程", "保持距离，使用墨水弹攻击。");
        CreateWeaponOption(list.transform, weaponPanel, WeaponType.Special, "特殊", "预留给机关、召唤或功能型武器。");
    }

    private void CreateWeaponOption(
        Transform parent,
        WeaponSelectionPanelUI weaponPanel,
        WeaponType type,
        string title,
        string description)
    {
        Button button = CreateButton($"{type}Button", parent, string.Empty, new Color(0.18f, 0.15f, 0.12f, 0.92f), new Vector2(680f, 86f));
        LayoutElement layoutElement = button.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 86f;

        Image background = button.GetComponent<Image>();
        TextMeshProUGUI titleText = CreateText("Title", button.transform, title, 26, new Color(0.96f, 0.83f, 0.52f, 1f), TextAlignmentOptions.MidlineLeft);
        titleText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        titleText.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        titleText.rectTransform.pivot = new Vector2(0f, 0.5f);
        titleText.rectTransform.anchoredPosition = new Vector2(28f, 14f);
        titleText.rectTransform.sizeDelta = new Vector2(240f, 32f);

        TextMeshProUGUI descText = CreateText("Description", button.transform, description, 20, new Color(0.86f, 0.80f, 0.70f, 1f), TextAlignmentOptions.MidlineLeft);
        descText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        descText.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        descText.rectTransform.pivot = new Vector2(0f, 0.5f);
        descText.rectTransform.anchoredPosition = new Vector2(28f, -20f);
        descText.rectTransform.sizeDelta = new Vector2(420f, 28f);

        TextMeshProUGUI stateText = CreateText("State", button.transform, "点击装备", 22, Color.white, TextAlignmentOptions.Center);
        stateText.rectTransform.anchorMin = new Vector2(1f, 0.5f);
        stateText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        stateText.rectTransform.pivot = new Vector2(1f, 0.5f);
        stateText.rectTransform.anchoredPosition = new Vector2(-28f, 0f);
        stateText.rectTransform.sizeDelta = new Vector2(150f, 36f);

        WeaponOptionData data = new WeaponOptionData
        {
            weaponType = type,
            displayName = title,
            description = description
        };
        weaponPanel.RegisterOption(data, button, background, stateText);
    }

    private GameObject CreatePlayer(InteractPrompt prompt)
    {
        GameObject playerObject = new GameObject("Player");
        playerObject.tag = "Player";
        playerObject.transform.position = new Vector3(0f, -1.2f, 0f);

        Sprite playerVisual = playerSprite != null
            ? playerSprite
            : GetOrCreateGeneratedSprite(ref generatedPlayerSprite, new Color(0.92f, 0.75f, 0.45f, 1f));
        SpriteRenderer renderer = playerObject.AddComponent<SpriteRenderer>();
        renderer.sprite = playerVisual;
        renderer.sortingOrder = 5;
        playerObject.transform.localScale = new Vector3(0.8f, 1.1f, 1f);

        Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;

        BoxCollider2D collider = playerObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.8f, 0.9f);

        playerObject.AddComponent<DirectionTracker>();

        CharacterCore core = playerObject.AddComponent<CharacterCore>();
        core.stats = new CharacterStats
        {
            maxHp = 120f,
            attackDamage = 18f,
            moveSpeed = 4.5f,
            defense = 6f
        };
        core.currentHp = core.stats.maxHp;

        PlayerProfileData profile = playerObject.AddComponent<PlayerProfileData>();
        profile.avatar = avatarSprite ?? playerVisual;
        profile.currentDurability = 100f;
        profile.maxDurability = 100f;
        profile.currentWeaponType = WeaponType.Ranged;

        PlayerMove move = playerObject.AddComponent<PlayerMove>();
        move.rb = body;

        PlayerInteraction interaction = playerObject.AddComponent<PlayerInteraction>();
        interaction.fImage = prompt.KeyObject;
        interaction.boxPanel = prompt.Root;
        interaction.boxText = prompt.Text;

        return playerObject;
    }

    private void CreateBookInteractable(BaseHubUIController uiController)
    {
        GameObject book = CreateWorldObject(
            "BookInteractable",
            new Vector3(-2.2f, 1.2f, 0f),
            bookSprite != null
                ? bookSprite
                : GetOrCreateGeneratedSprite(ref generatedBookSprite, new Color(0.56f, 0.30f, 0.14f, 1f)),
            new Vector3(0.9f, 0.7f, 1f));

        CircleCollider2D trigger = book.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 1.05f;

        BaseHubBookInteract interact = book.AddComponent<BaseHubBookInteract>();
        interact.Configure(uiController);
    }

    private void CreateSpiritInteractable(BaseHubUIController uiController)
    {
        GameObject spirit = CreateWorldObject(
            "SpiritInteractable",
            new Vector3(2.2f, 1.2f, 0f),
            spiritSprite != null
                ? spiritSprite
                : GetOrCreateGeneratedSprite(ref generatedSpiritSprite, new Color(0.42f, 0.78f, 0.95f, 1f)),
            new Vector3(0.8f, 0.8f, 1f));

        CircleCollider2D trigger = spirit.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 1.05f;

        SpiritInteract interact = spirit.AddComponent<SpiritInteract>();
        interact.Configure(uiController);
    }

    private void CreateGameSceneInteractable()
    {
        GameObject gate = CreateWorldObject(
            "GameSceneGateInteractable",
            new Vector3(0f, 2.9f, 0f),
            GetOrCreateGeneratedSprite(ref generatedGateSprite, new Color(0.78f, 0.58f, 0.28f, 1f)),
            new Vector3(1.8f, 0.55f, 1f));

        BoxCollider2D trigger = gate.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.4f, 1.2f);

        gate.AddComponent<BaseHubGameSceneInteract>();
    }

    private GameObject CreateWorldObject(string name, Vector3 position, Sprite sprite, Vector3 scale)
    {
        GameObject obj = new GameObject(name);
        obj.transform.position = position;
        obj.transform.localScale = scale;
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 3;
        return obj;
    }

    private GameObject CreateModalRoot(string name, Transform parent)
    {
        GameObject root = CreateUIObject(name, parent);
        SetStretch(root.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

        Image overlay = root.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.58f);
        return root;
    }

    private GameObject CreateCenteredPanel(string name, Transform parent, Vector2 size)
    {
        GameObject panel = CreateUIObject(name, parent);
        SetCenteredRect(panel.GetComponent<RectTransform>(), Vector2.zero, size);
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.10f, 0.08f, 0.06f, 0.97f);
        return panel;
    }

    private Button CreateButton(string name, Transform parent, string label, Color color)
    {
        return CreateButton(name, parent, label, color, new Vector2(180f, 56f));
    }

    private Button CreateButton(string name, Transform parent, string label, Color color, Vector2 size)
    {
        GameObject buttonObject = CreateUIObject(name, parent);
        SetCenteredRect(buttonObject.GetComponent<RectTransform>(), Vector2.zero, size);

        Image image = buttonObject.AddComponent<Image>();
        image.color = color;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        if (!string.IsNullOrEmpty(label))
        {
            CreateText("Label", buttonObject.transform, label, 26, Color.white, TextAlignmentOptions.Center);
        }

        return button;
    }

    private TextMeshProUGUI CreateRowText(Transform parent, string value)
    {
        TextMeshProUGUI text = CreateText("Row", parent, value, 26, new Color(0.93f, 0.88f, 0.78f, 1f), TextAlignmentOptions.MidlineLeft);
        LayoutElement layoutElement = text.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 34f;
        return text;
    }

    private TextMeshProUGUI CreateText(
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
        text.font = TMP_Settings.defaultFontAsset;
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

    private static Sprite CreateSolidSprite(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    private static Sprite GetOrCreateGeneratedSprite(ref Sprite sprite, Color color)
    {
        if (sprite == null)
        {
            sprite = CreateSolidSprite(color);
        }

        return sprite;
    }

    private struct InteractPrompt
    {
        public readonly GameObject Root;
        public readonly GameObject KeyObject;
        public readonly TextMeshProUGUI Text;

        public InteractPrompt(GameObject root, GameObject keyObject, TextMeshProUGUI text)
        {
            Root = root;
            KeyObject = keyObject;
            Text = text;
        }
    }
}
