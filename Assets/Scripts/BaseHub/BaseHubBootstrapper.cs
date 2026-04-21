using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public class BaseHubBootstrapper : MonoBehaviour
{
    private const string BaseHubMapResourcePath = "BaseHub/base_hub_map";
    private const string DefaultHandbookPrefabPath = "Assets/Scripts/View/Prefab/CatagloueUI.prefab";
    private const string RequiredRuntimeCharacters = "图鉴精灵关卡入口打开查看属性武器攻击基地允许生命上限耐久攻击力移动速度防御调试面板按住显示关闭点击装备";
    private static readonly string[] RuntimeFontNames =
    {
        "Arial Unicode MS",
        "Arial Unicode",
        "Hiragino Sans GB",
        "PingFang SC",
        "Noto Sans CJK SC",
        "Helvetica",
        "Arial"
    };

    private static readonly Vector3 DetailedPlayerSpawnPosition = new Vector3(0f, -1.75f, 0f);
    private static readonly Vector3 DetailedBookPosition = new Vector3(-4.2f, 0.4f, 0f);
    private static readonly Vector3 DetailedSpiritPosition = new Vector3(4.2f, 0.4f, 0f);
    private static readonly Vector3 DetailedGatePosition = new Vector3(0f, 2.85f, 0f);
    private static readonly Vector3 DetailedLeftDummyPosition = new Vector3(-4.1f, -3.3f, 0f);
    private static readonly Vector3 DetailedRightDummyPosition = new Vector3(4.1f, -3.3f, 0f);

    private static TMP_FontAsset runtimeFontAsset;

    [Header("运行时生成")]
    [SerializeField] private bool buildOnStart = true;

    [Header("可选美术资源")]
    [SerializeField] private Sprite playerSprite;
    [SerializeField] private RuntimeAnimatorController playerAnimatorController;
    [SerializeField] private Sprite avatarSprite;
    [SerializeField] private Sprite bookSprite;
    [SerializeField] private Sprite spiritSprite;
    [SerializeField] private Sprite hubMapSprite;

    [Header("复用现有 UI")]
    [SerializeField] private GameObject handbookUIPrefab;
    [SerializeField] private GameObject healthHudPrefab;
    [SerializeField] private GameObject weaponHudPrefab;

    private bool useDetailedHubMap;
    private Sprite generatedPlayerSprite;
    private Sprite generatedBookSprite;
    private Sprite generatedSpiritSprite;
    private Sprite generatedGateSprite;
    private Sprite generatedFloorSprite;
    private Sprite generatedHubMapSprite;

    private void Start()
    {
        RuntimeMiniMapHud.EnsureInstance();
        if (!buildOnStart) return;
        if (FindObjectOfType<BaseHubUIController>() != null) return;

        BuildBaseHub();
    }

    private void BuildBaseHub()
    {
        EnsureCamera();
        EnsureEventSystem();

        useDetailedHubMap = CreateBaseMap();
        if (!useDetailedHubMap)
        {
            CreateFloor();
            CreateBaseDecorations();
        }

        Canvas canvas = CreateCanvas();
        InteractPrompt prompt = CreateInteractPrompt(canvas.transform);
        SpiritPanelUI spiritPanel = CreateSpiritPanel(canvas.transform);

        BaseHubUIController uiController = new GameObject("BaseHubUIController").AddComponent<BaseHubUIController>();
        GameObject player = CreatePlayer(prompt);
        CharacterCore characterCore = player.GetComponent<CharacterCore>();
        PlayerProfileData profileData = player.GetComponent<PlayerProfileData>();
        CreateStatusHud(canvas.transform, characterCore, profileData);
        GameObject handbookPanel = CreateBaseHandbookUI(player, prompt.Root);

        spiritPanel.Bind(characterCore, profileData);
        uiController.Configure(player, handbookPanel, spiritPanel, prompt.Root);
        spiritPanel.SetCloseAction(uiController.CloseAll);

        CreateBookInteractable(uiController);
        CreateSpiritInteractable(uiController);
        CreateGameSceneInteractable();
        CreateTrainingDummies();
    }

    private void EnsureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
        }

        camera.orthographic = true;
        camera.orthographicSize = 5.8f;
        camera.backgroundColor = new Color(0.03f, 0.04f, 0.05f, 1f);
        camera.transform.position = new Vector3(0f, -0.1f, -10f);
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private void CreateStatusHud(Transform parent, CharacterCore characterCore, PlayerProfileData profileData)
    {
        GameObject root = CreateUIObject("BaseHubStatusHudRoot", parent);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 0f);
        rootRect.anchorMax = new Vector2(0f, 0f);
        rootRect.pivot = new Vector2(0f, 0f);
        rootRect.anchoredPosition = new Vector2(26f, 26f);
        rootRect.sizeDelta = new Vector2(420f, 108f);

        Image background = root.AddComponent<Image>();
        background.color = new Color(0.04f, 0.03f, 0.03f, 0.78f);

        StatusHudWidgets healthWidgets = CreateStatusHudRow(
            root.transform,
            "Health",
            "生命",
            new Vector2(18f, 62f),
            new Color(0.86f, 0.22f, 0.22f, 1f));
        StatusHudWidgets weaponWidgets = CreateStatusHudRow(
            root.transform,
            "Weapon",
            "武器",
            new Vector2(18f, 20f),
            new Color(0.26f, 0.72f, 0.90f, 1f));

        BaseHubStatusHud hud = root.AddComponent<BaseHubStatusHud>();
        hud.Configure(
            characterCore,
            profileData,
            healthWidgets.valueTrans,
            weaponWidgets.valueTrans,
            weaponWidgets.fillImage,
            healthWidgets.valueText,
            weaponWidgets.valueText);
    }

    private StatusHudWidgets CreateStatusHudRow(Transform parent, string name, string title, Vector2 anchoredPosition, Color fillColor)
    {
        GameObject row = CreateUIObject($"{name}Row", parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 0f);
        rowRect.anchorMax = new Vector2(0f, 0f);
        rowRect.pivot = new Vector2(0f, 0f);
        rowRect.anchoredPosition = anchoredPosition;
        rowRect.sizeDelta = new Vector2(384f, 28f);

        TextMeshProUGUI titleText = CreateText(
            $"{name}Title",
            row.transform,
            title,
            22,
            new Color(0.96f, 0.91f, 0.80f, 1f),
            TextAlignmentOptions.MidlineLeft);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0.5f);
        titleRect.anchorMax = new Vector2(0f, 0.5f);
        titleRect.pivot = new Vector2(0f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 0f);
        titleRect.sizeDelta = new Vector2(58f, 26f);

        GameObject barObject = CreateUIObject($"{name}Bar", row.transform);
        RectTransform barRect = barObject.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 0.5f);
        barRect.anchorMax = new Vector2(0f, 0.5f);
        barRect.pivot = new Vector2(0f, 0.5f);
        barRect.anchoredPosition = new Vector2(70f, 0f);
        barRect.sizeDelta = new Vector2(220f, 18f);

        Image background = barObject.AddComponent<Image>();
        background.color = new Color(0.19f, 0.16f, 0.15f, 1f);

        Slider slider = barObject.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 100f;
        slider.targetGraphic = background;

        GameObject fillArea = CreateUIObject($"{name}FillArea", barObject.transform);
        SetStretch(fillArea.GetComponent<RectTransform>(), 2f, 2f, 2f, 2f);

        GameObject fillObject = CreateUIObject($"{name}Fill", fillArea.transform);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fillObject.AddComponent<Image>();
        fillImage.color = fillColor;
        slider.fillRect = fillRect;

        ValueTrans valueTrans = barObject.AddComponent<ValueTrans>();
        valueTrans.slider = slider;

        TextMeshProUGUI valueText = CreateText(
            $"{name}Value",
            row.transform,
            string.Empty,
            20,
            Color.white,
            TextAlignmentOptions.MidlineRight);
        RectTransform valueRect = valueText.rectTransform;
        valueRect.anchorMin = new Vector2(1f, 0.5f);
        valueRect.anchorMax = new Vector2(1f, 0.5f);
        valueRect.pivot = new Vector2(1f, 0.5f);
        valueRect.anchoredPosition = new Vector2(0f, 0f);
        valueRect.sizeDelta = new Vector2(92f, 24f);

        return new StatusHudWidgets(valueTrans, fillImage, valueText);
    }

    private GameObject ResolveHudPrefab(GameObject prefab, string exactName)
    {
        if (IsHudPrefab(prefab, exactName))
        {
            return prefab;
        }

        GameObject[] candidates = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < candidates.Length; i++)
        {
            GameObject candidate = candidates[i];
            if (!IsHudPrefab(candidate, exactName)) continue;

            return candidate;
        }

        return null;
    }

    private static bool IsHudPrefab(GameObject candidate, string exactName)
    {
        try
        {
            if (candidate == null) return false;
            if (candidate.scene.IsValid()) return false;
            if (!string.Equals(candidate.name, exactName, StringComparison.Ordinal)) return false;
            if (candidate.GetComponent<Canvas>() == null) return false;
            if (candidate.GetComponentInChildren<ValueTrans>(true) == null) return false;

            return true;
        }
        catch (MissingReferenceException)
        {
            return false;
        }
    }

    private static ValueTrans ConfigureStatusHudRoot(GameObject hudRoot, string name, int sortingOrder)
    {
        if (hudRoot == null)
        {
            return null;
        }

        hudRoot.name = name;
        RectTransform rectTransform = hudRoot.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one;
        }

        Canvas canvas = hudRoot.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
        }

        GraphicRaycaster raycaster = hudRoot.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = false;
        }

        CanvasGroup canvasGroup = hudRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = hudRoot.AddComponent<CanvasGroup>();
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        return hudRoot.GetComponentInChildren<ValueTrans>(true);
    }

    private static Image FindSliderFillImage(ValueTrans valueTrans)
    {
        if (valueTrans == null || valueTrans.slider == null || valueTrans.slider.fillRect == null)
        {
            return null;
        }

        return valueTrans.slider.fillRect.GetComponent<Image>();
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

    private bool CreateBaseMap()
    {
        Sprite sprite = ResolveHubMapSprite();
        if (sprite == null)
        {
            return false;
        }

        GameObject map = new GameObject("BaseHubMap");
        SpriteRenderer renderer = map.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = -12;
        map.transform.position = new Vector3(0f, -0.18f, 0f);
        map.transform.localScale = Vector3.one;
        return true;
    }

    private void CreateBaseDecorations()
    {
        Sprite pathSprite = CreateSolidSprite(new Color(0.30f, 0.25f, 0.17f, 1f));
        Sprite mossSprite = CreateSolidSprite(new Color(0.15f, 0.30f, 0.18f, 1f));
        Sprite stoneSprite = CreateSolidSprite(new Color(0.42f, 0.43f, 0.38f, 1f));
        Sprite glowSprite = CreateSolidSprite(new Color(0.65f, 0.84f, 0.80f, 1f));

        CreateWorldObject("BaseMainPath", new Vector3(0f, 0.8f, 0f), pathSprite, new Vector3(1.7f, 5.2f, 1f), -8);
        CreateWorldObject("BaseCrossPath", new Vector3(0f, 1.2f, 0f), pathSprite, new Vector3(6.4f, 1.1f, 1f), -8);
        CreateWorldObject("BookReadingMat", new Vector3(-2.2f, 0.95f, 0f), mossSprite, new Vector3(1.8f, 1.2f, 1f), -7);
        CreateWorldObject("SpiritGarden", new Vector3(2.2f, 0.95f, 0f), glowSprite, new Vector3(1.8f, 1.2f, 1f), -7);

        CreateWorldObject("LeftPillar", new Vector3(-1.25f, 2.55f, 0f), stoneSprite, new Vector3(0.34f, 0.85f, 1f), 1);
        CreateWorldObject("RightPillar", new Vector3(1.25f, 2.55f, 0f), stoneSprite, new Vector3(0.34f, 0.85f, 1f), 1);

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

    private GameObject CreateBaseHandbookUI(GameObject playerObject, GameObject interactPrompt)
    {
        EnsureBaseHandbookRuntimeSystems(playerObject);

        GameObject handbookRoot = TryInstantiatePrefabObject(handbookUIPrefab);
        if (handbookRoot == null)
        {
            GameObject prefab = ResolveHandbookPrefab();
            handbookRoot = TryInstantiatePrefabObject(prefab);
        }

        if (handbookRoot == null)
        {
            Debug.LogError("基地图鉴预制体未绑定，且未找到可用的图鉴 UI 预制体。");
            return null;
        }

        handbookRoot.name = "BaseHandbookUI";

        GameObject illustratedHandbook = FindChildByName(handbookRoot.transform, "IllustratedHandbookCanvas");
        GameObject detailedInformation = FindChildByName(handbookRoot.transform, "DetailedInformationCanvas");
        GameObject dialogCanvas = FindChildByName(handbookRoot.transform, "DialogCanvas");
        GameObject packBagCanvas = FindChildByName(handbookRoot.transform, "PackBagCanvas");
        GameObject interactionCanvas = FindChildByName(handbookRoot.transform, "InteractionCanvas");

        if (dialogCanvas != null) dialogCanvas.SetActive(false);
        if (packBagCanvas != null) ConfigureBaseBackpackCanvas(packBagCanvas);
        if (interactionCanvas != null) interactionCanvas.SetActive(false);
        if (illustratedHandbook != null) illustratedHandbook.SetActive(false);
        if (detailedInformation != null) detailedInformation.SetActive(false);

        RuntimeProgressState.EnsureInstance();
        CatalogueUnlockSelectionManager.EnsureInstance();

        UIManager uiManager = UIManager.Instance;
        if (uiManager == null)
        {
            uiManager = handbookRoot.GetComponentInChildren<UIManager>(true);
        }

        uiManager?.ConfigureForRuntime(
            illustratedHandbook,
            detailedInformation,
            new[] { interactPrompt },
            interactPrompt,
            playerObject);

        BackpackUI backpackUI = handbookRoot.GetComponentInChildren<BackpackUI>(true);
        backpackUI?.RefreshUI();

        return illustratedHandbook;
    }

    private static void EnsureBaseHandbookRuntimeSystems(GameObject playerObject)
    {
        if (BackpackMananger.Instance == null)
        {
            GameObject manager = new GameObject("BaseBackpackManager");
            manager.AddComponent<BackpackMananger>();
        }

        if (playerObject != null && playerObject.GetComponent<PlayerGetArchitectural>() == null)
        {
            playerObject.AddComponent<PlayerGetArchitectural>();
        }
    }

    private static void ConfigureBaseBackpackCanvas(GameObject packBagCanvas)
    {
        if (packBagCanvas == null)
        {
            return;
        }

        packBagCanvas.SetActive(true);

        RectTransform rectTransform = packBagCanvas.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one;
        }

        CanvasGroup canvasGroup = packBagCanvas.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    private GameObject ResolveHandbookPrefab()
    {
        if (IsHandbookPrefab(handbookUIPrefab))
        {
            return handbookUIPrefab;
        }

        GameObject knownPrefab = LoadHandbookPrefabFromKnownPath();
        if (IsHandbookPrefab(knownPrefab))
        {
            handbookUIPrefab = knownPrefab;
            return handbookUIPrefab;
        }

        GameObject[] candidates = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < candidates.Length; i++)
        {
            GameObject candidate = candidates[i];
            if (!IsHandbookPrefab(candidate)) continue;

            handbookUIPrefab = candidate;
            return handbookUIPrefab;
        }

        return null;
    }

    private static GameObject TryInstantiatePrefabObject(GameObject prefab)
    {
        if (prefab == null)
        {
            return null;
        }

        try
        {
            UnityEngine.Object handbookInstance = Instantiate((UnityEngine.Object)prefab);
            if (handbookInstance is GameObject handbookRoot)
            {
                return handbookRoot;
            }

            if (handbookInstance is Component handbookComponent)
            {
                return handbookComponent.gameObject;
            }
        }
        catch (MissingReferenceException)
        {
            return null;
        }

        return null;
    }

    private bool IsHandbookPrefab(GameObject candidate)
    {
        try
        {
            if (candidate == null) return false;
            if (candidate.scene.IsValid()) return false;
            if (FindChildByName(candidate.transform, "IllustratedHandbookCanvas") == null) return false;
            if (FindChildByName(candidate.transform, "DetailedInformationCanvas") == null) return false;
            if (candidate.GetComponentInChildren<UIManager>(true) == null) return false;

            return true;
        }
        catch (MissingReferenceException)
        {
            return false;
        }
    }

    private static GameObject LoadHandbookPrefabFromKnownPath()
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(DefaultHandbookPrefabPath);
#else
        return null;
#endif
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

        Button weaponTabButton = CreateButton("WeaponTabButton", panel.transform, "墨水", new Color(0.22f, 0.18f, 0.14f, 1f), new Vector2(140f, 48f));
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
        SetCenteredRect(list.GetComponent<RectTransform>(), Vector2.zero, new Vector2(680f, 440f));
        VerticalLayoutGroup layout = list.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 18f;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        CreateWeaponOption(list.transform, weaponPanel, WeaponType.DirectInk, "直墨", "标准墨迹，稳定直射，适合作为通用基型。");
        CreateWeaponOption(list.transform, weaponPanel, WeaponType.BurstInk, "爆墨", "命中后爆散成片，擅长处理聚集敌人。");
        CreateWeaponOption(list.transform, weaponPanel, WeaponType.PierceInk, "贯墨", "初始可连续命中 3 次，更适合打穿一列目标。");
        CreateWeaponOption(list.transform, weaponPanel, WeaponType.FlowInk, "流墨", "命中后附带持续 3 秒的流墨侵蚀。");
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
        playerObject.transform.position = useDetailedHubMap
            ? DetailedPlayerSpawnPosition
            : new Vector3(0f, -1.2f, 0f);

        bool useConfiguredPlayerVisual = playerSprite != null;
        Sprite playerVisual = useConfiguredPlayerVisual
            ? playerSprite
            : GetOrCreateGeneratedPlayerSprite();
        SpriteRenderer renderer = playerObject.AddComponent<SpriteRenderer>();
        renderer.sprite = playerVisual;
        renderer.sortingOrder = 5;
        playerObject.transform.localScale = useConfiguredPlayerVisual
            ? new Vector3(4f, 4f, 1f)
            : new Vector3(0.8f, 1.1f, 1f);

        Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;

        BoxCollider2D collider = playerObject.AddComponent<BoxCollider2D>();
        collider.size = useConfiguredPlayerVisual
            ? new Vector2(0.22f, 0.24f)
            : new Vector2(0.8f, 0.9f);

        playerObject.AddComponent<DirectionTracker>();

        CharacterCore core = playerObject.AddComponent<CharacterCore>();
        core.stats = new CharacterStats
        {
            maxHp = 100f,
            attackDamage = 20f,
            moveSpeed = 4.5f,
            defense = 5f
        };
        core.currentHp = core.stats.maxHp;

        PlayerProfileData profile = playerObject.AddComponent<PlayerProfileData>();
        profile.avatar = avatarSprite ?? playerVisual;
        profile.currentDurability = 100f;
        profile.maxDurability = 100f;
        profile.currentInkType = PlayerLoadoutRuntime.CurrentInkType;
        profile.currentWeaponType = PlayerLoadoutRuntime.CurrentWeaponType;

        if (playerObject.GetComponent<PlayerAttributeManager>() == null)
        {
            playerObject.AddComponent<PlayerAttributeManager>();
        }

        if (playerObject.GetComponent<PlayerGetArchitectural>() == null)
        {
            playerObject.AddComponent<PlayerGetArchitectural>();
        }

        PlayerMove move = playerObject.AddComponent<PlayerMove>();
        move.rb = body;
        if (playerAnimatorController != null)
        {
            Animator animator = playerObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = playerAnimatorController;
            move.animator = animator;
        }

        PlayerInteraction interaction = playerObject.AddComponent<PlayerInteraction>();
        interaction.fImage = prompt.KeyObject;
        interaction.boxPanel = prompt.Root;
        interaction.boxText = prompt.Text;

        playerObject.AddComponent<BaseHubInkAttack>();

        return playerObject;
    }

    private void CreateBookInteractable(BaseHubUIController uiController)
    {
        GameObject book = useDetailedHubMap
            ? CreateInteractionAnchor("BookInteractable", DetailedBookPosition)
            : CreateWorldObject(
                "BookInteractable",
                new Vector3(-2.2f, 1.2f, 0f),
                bookSprite != null
                    ? bookSprite
                    : GetOrCreateGeneratedBookSprite(),
                new Vector3(1.05f, 0.82f, 1f));

        CircleCollider2D trigger = book.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = useDetailedHubMap ? 1.2f : 1.05f;

        BaseHubBookInteract interact = book.AddComponent<BaseHubBookInteract>();
        interact.Configure(uiController);
    }

    private void CreateSpiritInteractable(BaseHubUIController uiController)
    {
        GameObject spirit = useDetailedHubMap
            ? CreateInteractionAnchor("SpiritInteractable", DetailedSpiritPosition)
            : CreateWorldObject(
                "SpiritInteractable",
                new Vector3(2.2f, 1.2f, 0f),
                spiritSprite != null
                    ? spiritSprite
                    : GetOrCreateGeneratedSpiritSprite(),
                new Vector3(0.95f, 1.05f, 1f));

        CircleCollider2D trigger = spirit.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = useDetailedHubMap ? 1.2f : 1.05f;

        SpiritInteract interact = spirit.AddComponent<SpiritInteract>();
        interact.Configure(uiController);
    }

    private void CreateGameSceneInteractable()
    {
        GameObject gate = useDetailedHubMap
            ? CreateInteractionAnchor("GameSceneGateInteractable", DetailedGatePosition)
            : CreateWorldObject(
                "GameSceneGateInteractable",
                new Vector3(0f, 2.9f, 0f),
                GetOrCreateGeneratedGateSprite(),
                new Vector3(1.55f, 1.05f, 1f));

        BoxCollider2D trigger = gate.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = useDetailedHubMap
            ? new Vector2(2.2f, 1.6f)
            : new Vector2(1.4f, 1.2f);

        gate.AddComponent<BaseHubGameSceneInteract>();
    }

    private void CreateTrainingDummies()
    {
        CreateTrainingDummy(
            "TrainingDummy_Left",
            useDetailedHubMap ? DetailedLeftDummyPosition : new Vector3(-4.3f, -1.4f, 0f),
            useDetailedHubMap ? new Vector3(0.78f, 1.08f, 1f) : new Vector3(0.9f, 1.25f, 1f));
        CreateTrainingDummy(
            "TrainingDummy_Right",
            useDetailedHubMap ? DetailedRightDummyPosition : new Vector3(4.3f, -1.4f, 0f),
            useDetailedHubMap ? new Vector3(0.78f, 1.08f, 1f) : new Vector3(0.9f, 1.25f, 1f));
    }

    private void CreateTrainingDummy(string name, Vector3 position, Vector3 scale)
    {
        GameObject dummy = CreateWorldObject(
            name,
            position,
            CreateTrainingDummySprite(),
            scale,
            2);

        BoxCollider2D collider = dummy.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.9f, 1.2f);

        Rigidbody2D body = dummy.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;

        CharacterCore core = dummy.AddComponent<CharacterCore>();
        core.stats = new CharacterStats
        {
            maxHp = 80f,
            attackDamage = 0f,
            moveSpeed = 0f,
            defense = 0f
        };
        core.currentHp = core.stats.maxHp;

        dummy.AddComponent<BaseHubTrainingDummy>();
        dummy.AddComponent<EnemyCombatFeedback>();
    }

    private GameObject CreateInteractionAnchor(string name, Vector3 position)
    {
        GameObject anchor = new GameObject(name);
        anchor.transform.position = position;
        return anchor;
    }

    private GameObject CreateWorldObject(string name, Vector3 position, Sprite sprite, Vector3 scale, int sortingOrder = 3)
    {
        GameObject obj = new GameObject(name);
        obj.transform.position = position;
        obj.transform.localScale = scale;
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
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
        text.font = GetRuntimeFont();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        SetStretch(text.rectTransform, 0f, 0f, 0f, 0f);
        return text;
    }

    private Sprite ResolveHubMapSprite()
    {
        if (hubMapSprite != null)
        {
            ApplyHubMapTextureSettings(hubMapSprite);
            return hubMapSprite;
        }

        if (generatedHubMapSprite != null)
        {
            ApplyHubMapTextureSettings(generatedHubMapSprite);
            return generatedHubMapSprite;
        }

        generatedHubMapSprite = Resources.Load<Sprite>(BaseHubMapResourcePath);
        if (generatedHubMapSprite != null)
        {
            ApplyHubMapTextureSettings(generatedHubMapSprite);
            return generatedHubMapSprite;
        }

        Texture2D hubMapTexture = Resources.Load<Texture2D>(BaseHubMapResourcePath);
        if (hubMapTexture == null)
        {
            return null;
        }

        hubMapTexture.filterMode = FilterMode.Bilinear;
        generatedHubMapSprite = Sprite.Create(
            hubMapTexture,
            new Rect(0f, 0f, hubMapTexture.width, hubMapTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        return generatedHubMapSprite;
    }

    private static void ApplyHubMapTextureSettings(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
        {
            return;
        }

        sprite.texture.filterMode = FilterMode.Bilinear;
    }

    private static TMP_FontAsset GetRuntimeFont()
    {
        if (runtimeFontAsset != null)
        {
            return runtimeFontAsset;
        }

        Font[] loadedFonts = Resources.FindObjectsOfTypeAll<Font>();
        for (int i = 0; i < loadedFonts.Length; i++)
        {
            Font font = loadedFonts[i];
            if (font == null)
            {
                continue;
            }

            string fontName = font.name ?? string.Empty;
            if (!fontName.Contains("NotoSansSC") && !fontName.Contains("Noto Sans SC"))
            {
                continue;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (fontAsset == null)
            {
                continue;
            }

            fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset>();
            fontAsset.TryAddCharacters(RequiredRuntimeCharacters);
            runtimeFontAsset = fontAsset;
            return runtimeFontAsset;
        }

        TMP_FontAsset[] loadedFontAssets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < loadedFontAssets.Length; i++)
        {
            TMP_FontAsset fontAsset = loadedFontAssets[i];
            if (fontAsset == null) continue;
            if (!fontAsset.name.Contains("NotoSansSC")) continue;
            if (!fontAsset.HasCharacters(RequiredRuntimeCharacters))
            {
                continue;
            }

            runtimeFontAsset = fontAsset;
            return runtimeFontAsset;
        }

        for (int i = 0; i < RuntimeFontNames.Length; i++)
        {
            Font font;
            try
            {
                font = Font.CreateDynamicFontFromOSFont(RuntimeFontNames[i], 90);
            }
            catch (Exception)
            {
                continue;
            }

            if (font == null)
            {
                continue;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (fontAsset == null)
            {
                continue;
            }

            fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset>();
            fontAsset.TryAddCharacters(RequiredRuntimeCharacters);
            runtimeFontAsset = fontAsset;
            return runtimeFontAsset;
        }

        runtimeFontAsset = TMP_Settings.defaultFontAsset;
        return runtimeFontAsset;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static GameObject FindChildByName(Transform root, string targetName)
    {
        if (root == null) return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == targetName)
            {
                return child.gameObject;
            }

            GameObject nested = FindChildByName(child, targetName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
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

    private Sprite GetOrCreateGeneratedPlayerSprite()
    {
        if (generatedPlayerSprite == null)
        {
            generatedPlayerSprite = CreatePlayerSprite();
        }

        return generatedPlayerSprite;
    }

    private Sprite GetOrCreateGeneratedBookSprite()
    {
        if (generatedBookSprite == null)
        {
            generatedBookSprite = CreateBookSprite();
        }

        return generatedBookSprite;
    }

    private Sprite GetOrCreateGeneratedSpiritSprite()
    {
        if (generatedSpiritSprite == null)
        {
            generatedSpiritSprite = CreateSpiritSprite();
        }

        return generatedSpiritSprite;
    }

    private Sprite GetOrCreateGeneratedGateSprite()
    {
        if (generatedGateSprite == null)
        {
            generatedGateSprite = CreateGateSprite();
        }

        return generatedGateSprite;
    }

    private static Sprite CreatePlayerSprite()
    {
        Texture2D texture = CreateTransparentTexture(24, 32);
        Color skin = new Color(0.84f, 0.62f, 0.38f, 1f);
        Color coat = new Color(0.45f, 0.25f, 0.16f, 1f);
        Color scarf = new Color(0.86f, 0.66f, 0.30f, 1f);

        FillRect(texture, 8, 20, 8, 8, skin);
        FillRect(texture, 6, 10, 12, 11, coat);
        FillRect(texture, 7, 17, 10, 3, scarf);
        FillRect(texture, 7, 4, 4, 7, new Color(0.24f, 0.16f, 0.12f, 1f));
        FillRect(texture, 13, 4, 4, 7, new Color(0.24f, 0.16f, 0.12f, 1f));
        FillRect(texture, 9, 24, 2, 2, Color.black);
        FillRect(texture, 14, 24, 2, 2, Color.black);
        texture.Apply();
        return CreateSpriteFromTexture(texture, 16f);
    }

    private static Sprite CreateBookSprite()
    {
        Texture2D texture = CreateTransparentTexture(36, 24);
        Color cover = new Color(0.42f, 0.17f, 0.10f, 1f);
        Color page = new Color(0.90f, 0.78f, 0.55f, 1f);
        Color line = new Color(0.47f, 0.30f, 0.16f, 1f);

        FillRect(texture, 2, 3, 32, 18, cover);
        FillRect(texture, 5, 6, 12, 12, page);
        FillRect(texture, 19, 6, 12, 12, page);
        FillRect(texture, 17, 4, 2, 16, new Color(0.20f, 0.10f, 0.07f, 1f));
        FillRect(texture, 8, 10, 7, 1, line);
        FillRect(texture, 8, 14, 6, 1, line);
        FillRect(texture, 21, 10, 7, 1, line);
        FillRect(texture, 22, 14, 6, 1, line);
        texture.Apply();
        return CreateSpriteFromTexture(texture, 18f);
    }

    private static Sprite CreateSpiritSprite()
    {
        Texture2D texture = CreateTransparentTexture(28, 32);
        Color body = new Color(0.42f, 0.78f, 0.95f, 1f);
        Color light = new Color(0.78f, 0.94f, 1f, 1f);
        Color shadow = new Color(0.18f, 0.43f, 0.55f, 1f);

        FillRect(texture, 9, 9, 10, 14, body);
        FillRect(texture, 7, 12, 14, 8, body);
        FillRect(texture, 11, 21, 6, 5, light);
        FillRect(texture, 8, 7, 4, 4, shadow);
        FillRect(texture, 16, 7, 4, 4, shadow);
        FillRect(texture, 11, 16, 2, 2, Color.black);
        FillRect(texture, 16, 16, 2, 2, Color.black);
        FillRect(texture, 12, 3, 4, 3, new Color(0.68f, 0.90f, 0.96f, 1f));
        texture.Apply();
        return CreateSpriteFromTexture(texture, 16f);
    }

    private static Sprite CreateGateSprite()
    {
        Texture2D texture = CreateTransparentTexture(48, 32);
        Color wood = new Color(0.55f, 0.33f, 0.16f, 1f);
        Color roof = new Color(0.78f, 0.58f, 0.28f, 1f);
        Color dark = new Color(0.22f, 0.13f, 0.08f, 1f);

        FillRect(texture, 7, 4, 6, 19, wood);
        FillRect(texture, 35, 4, 6, 19, wood);
        FillRect(texture, 12, 19, 24, 5, wood);
        FillRect(texture, 8, 24, 32, 4, roof);
        FillRect(texture, 14, 12, 20, 3, dark);
        FillRect(texture, 20, 4, 8, 8, dark);
        texture.Apply();
        return CreateSpriteFromTexture(texture, 16f);
    }

    private static Sprite CreateTrainingDummySprite()
    {
        Texture2D texture = CreateTransparentTexture(24, 32);
        Color wood = new Color(0.52f, 0.34f, 0.18f, 1f);
        Color rope = new Color(0.82f, 0.66f, 0.38f, 1f);
        Color dark = new Color(0.22f, 0.14f, 0.08f, 1f);

        FillRect(texture, 10, 3, 4, 26, wood);
        FillRect(texture, 6, 18, 12, 8, wood);
        FillRect(texture, 5, 16, 14, 3, rope);
        FillRect(texture, 7, 8, 10, 4, rope);
        FillRect(texture, 8, 22, 2, 2, dark);
        FillRect(texture, 14, 22, 2, 2, dark);
        texture.Apply();
        return CreateSpriteFromTexture(texture, 16f);
    }

    private static Texture2D CreateTransparentTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }

        return texture;
    }

    private static Sprite CreateSpriteFromTexture(Texture2D texture, float pixelsPerUnit)
    {
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit);
    }

    private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        int maxX = Mathf.Min(texture.width, x + width);
        int maxY = Mathf.Min(texture.height, y + height);

        for (int py = Mathf.Max(0, y); py < maxY; py++)
        {
            for (int px = Mathf.Max(0, x); px < maxX; px++)
            {
                texture.SetPixel(px, py, color);
            }
        }
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

    private readonly struct StatusHudWidgets
    {
        public readonly ValueTrans valueTrans;
        public readonly Image fillImage;
        public readonly TextMeshProUGUI valueText;

        public StatusHudWidgets(ValueTrans trans, Image fill, TextMeshProUGUI text)
        {
            valueTrans = trans;
            fillImage = fill;
            valueText = text;
        }
    }
}
