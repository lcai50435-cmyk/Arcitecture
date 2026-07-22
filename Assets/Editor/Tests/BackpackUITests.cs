using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class BackpackUITests
{
    private GameObject rootObject;
    private readonly List<Scene> createdScenes = new List<Scene>();
    private Scene originalActiveScene;

    [SetUp]
    public void SetUp()
    {
        originalActiveScene = SceneManager.GetActiveScene();
    }

    [TearDown]
    public void TearDown()
    {
        if (rootObject != null)
        {
            Object.DestroyImmediate(rootObject);
        }

        if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
        {
            SceneManager.SetActiveScene(originalActiveScene);
        }

        for (int i = createdScenes.Count - 1; i >= 0; i--)
        {
            Scene scene = createdScenes[i];
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        createdScenes.Clear();
    }

    [Test]
    public void RuntimeBackpackStartsWithAttackSlotSelected()
    {
        rootObject = new GameObject("PackBagCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        rootObject.AddComponent<BackpackUI>().ConfigureRuntimeLayout();

        BackpackUI backpackUi = rootObject.GetComponent<BackpackUI>();

        Assert.IsTrue(backpackUi.IsAttackSlotSelected);
        Assert.IsFalse(RuntimeUiInputGuard.ShouldBlockGameplayAttack(KeyCode.Space));
    }

    [Test]
    public void SelectingBackpackSlotBlocksAttackUntilAttackSlotIsSelectedAgain()
    {
        rootObject = new GameObject("PackBagCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        BackpackUI backpackUi = rootObject.AddComponent<BackpackUI>();
        backpackUi.ConfigureRuntimeLayout();

        backpackUi.SelectSlot(0);

        Assert.IsFalse(backpackUi.IsAttackSlotSelected);
        Assert.IsTrue(RuntimeUiInputGuard.ShouldBlockGameplayAttack(KeyCode.Space));

        backpackUi.SelectAttackSlot();

        Assert.IsTrue(backpackUi.IsAttackSlotSelected);
        Assert.IsFalse(RuntimeUiInputGuard.ShouldBlockGameplayAttack(KeyCode.Space));
    }

    [Test]
    public void TryGetSlotScreenPositionUsesCurrentRuntimeLayoutSlot()
    {
        rootObject = new GameObject("PackBagCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(1920f, 1080f);

        BackpackUI backpackUi = rootObject.AddComponent<BackpackUI>();

        RectTransform staleSurface = CreateRect(rootRect, "RuntimeBackpackSurface", new Vector2(-740f, -360f), new Vector2(80f, 80f));
        RectTransform staleSlot = CreateRect(staleSurface, "slot_1", Vector2.zero, new Vector2(80f, 80f));
        BackpackSlot staleSlotBehaviour = staleSlot.gameObject.AddComponent<BackpackSlot>();
        staleSlotBehaviour.slotIndex = 0;

        RectTransform itemPanel = CreateRect(rootRect, "ItemPanel", new Vector2(180f, 72f), new Vector2(600f, 120f));
        RectTransform visibleSlot = CreateRect(itemPanel, "slot_1", new Vector2(260f, 12f), new Vector2(80f, 80f));

        backpackUi.ConfigureRuntimeLayout();
        Canvas.ForceUpdateCanvases();

        Assert.IsFalse(staleSurface.gameObject.activeSelf);
        Assert.IsTrue(backpackUi.TryGetSlotScreenPosition(0, out Vector2 actualScreenPosition, out Vector2 slotSize));

        Vector2 visibleSlotScreenPosition = RectTransformUtility.WorldToScreenPoint(
            null,
            visibleSlot.TransformPoint(visibleSlot.rect.center));
        Vector2 staleSlotScreenPosition = RectTransformUtility.WorldToScreenPoint(
            null,
            staleSlot.TransformPoint(staleSlot.rect.center));

        Assert.That(Vector2.Distance(actualScreenPosition, visibleSlotScreenPosition), Is.LessThan(0.5f));
        Assert.That(Vector2.Distance(actualScreenPosition, staleSlotScreenPosition), Is.GreaterThan(1f));
        Assert.That(slotSize, Is.EqualTo(new Vector2(70f, 64f)));
    }

    [Test]
    public void ConfigureRuntimeLayoutPinsSceneAuthoredBackpackToBottomCenter()
    {
        rootObject = new GameObject("PackBagCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.zero;
        rootRect.pivot = Vector2.zero;
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(1920f, 1080f);

        BackpackUI backpackUi = rootObject.AddComponent<BackpackUI>();

        RectTransform scenePanel = CreateRect(rootRect, "Panel", new Vector2(0f, 751f), new Vector2(1920f, 1502f));
        scenePanel.anchorMin = new Vector2(0.5f, 0f);
        scenePanel.anchorMax = new Vector2(0.5f, 0f);
        scenePanel.pivot = new Vector2(0.5f, 0.5f);

        RectTransform itemPanel = CreateRect(scenePanel, "ItemPanel", new Vector2(162f, 0f), new Vector2(550f, 124f));
        itemPanel.anchorMin = new Vector2(0.5f, 0f);
        itemPanel.anchorMax = new Vector2(0.5f, 0f);
        itemPanel.pivot = new Vector2(0.5f, 0f);

        RectTransform attackPanel = CreateRect(scenePanel, "AttackPanel", new Vector2(-240f, 60.9f), new Vector2(120f, 120f));
        attackPanel.anchorMin = new Vector2(0.5f, 0f);
        attackPanel.anchorMax = new Vector2(0.5f, 0f);
        attackPanel.pivot = new Vector2(0.5f, 0.5f);

        backpackUi.ConfigureRuntimeLayout();
        Canvas.ForceUpdateCanvases();

        Assert.That(scenePanel.anchorMin, Is.EqualTo(new Vector2(0.5f, 0f)));
        Assert.That(scenePanel.anchorMax, Is.EqualTo(new Vector2(0.5f, 0f)));
        Assert.That(scenePanel.pivot, Is.EqualTo(new Vector2(0.5f, 0f)));
        Assert.That(scenePanel.anchoredPosition.x, Is.EqualTo(0f));
        Assert.That(scenePanel.anchoredPosition.y, Is.EqualTo(12f));

        Vector3[] itemCorners = new Vector3[4];
        itemPanel.GetWorldCorners(itemCorners);
        Assert.That(itemCorners[0].y, Is.GreaterThan(0f));

        Vector3[] attackCorners = new Vector3[4];
        attackPanel.GetWorldCorners(attackCorners);
        Assert.That(attackCorners[0].y, Is.GreaterThan(0f));
    }

    [Test]
    public void ConfigureRuntimeLayoutUsesMergedSeventhSlotAsAttackPanel()
    {
        rootObject = new GameObject("PackBagCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(1920f, 1080f);

        BackpackUI backpackUi = rootObject.AddComponent<BackpackUI>();
        RectTransform itemPanel = CreateRect(rootRect, "ItemPanel", Vector2.zero, new Vector2(550f, 124f));

        for (int i = 0; i < 7; i++)
        {
            CreateRect(itemPanel, $"slot_{i + 1}", new Vector2(-222f + i * 74f, 0f), new Vector2(70f, 80f));
        }

        backpackUi.ConfigureRuntimeLayout();
        backpackUi.SelectSlot(0);

        Assert.IsNull(rootRect.Find("AttackPanel"));

        Transform mergedAttackSlot = itemPanel.Find("slot_7");
        Assert.IsNotNull(mergedAttackSlot);

        Button attackButton = mergedAttackSlot.GetComponent<Button>();
        Assert.IsNotNull(attackButton);

        attackButton.onClick.Invoke();

        Assert.IsTrue(backpackUi.IsAttackSlotSelected);
    }

    [Test]
    public void ConfigureRuntimeLayoutReplacesSceneAuthoredBackpackFrameWithUnifiedHorizontalFrame()
    {
        rootObject = new GameObject("PackBagCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(1920f, 1080f);

        BackpackUI backpackUi = rootObject.AddComponent<BackpackUI>();
        RectTransform scenePanel = CreateRect(rootRect, "Panel", new Vector2(0f, 751f), new Vector2(1280f, 720f));
        scenePanel.GetComponent<Image>().color = Color.white;

        RectTransform itemPanel = CreateRect(scenePanel, "ItemPanel", Vector2.zero, new Vector2(550f, 124f));
        Sprite authoredBackpackFrame = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f));
        Image itemPanelImage = itemPanel.GetComponent<Image>();
        itemPanelImage.sprite = authoredBackpackFrame;
        itemPanelImage.color = Color.white;

        for (int i = 0; i < 7; i++)
        {
            RectTransform slot = CreateRect(itemPanel, $"slot_{i + 1}", new Vector2(-222f + i * 74f, 0f), new Vector2(70f, 80f));
            slot.GetComponent<Image>().color = Color.clear;
        }

        backpackUi.ConfigureRuntimeLayout();
        Sprite unifiedFrameSprite = Resources.Load<Sprite>("UI/BackpackSlotsHorizontal");

        Assert.That(scenePanel.GetComponent<Image>().color.a, Is.LessThan(0.01f));
        Assert.IsNotNull(unifiedFrameSprite);
        Assert.AreSame(unifiedFrameSprite, itemPanelImage.sprite);
        Assert.AreEqual(Color.white, itemPanelImage.color);
        Assert.IsFalse(itemPanelImage.raycastTarget);
        Assert.AreEqual(new Vector2(550f, 112f), itemPanel.sizeDelta);

        for (int i = 1; i <= 7; i++)
        {
            Transform slot = itemPanel.Find($"slot_{i}");
            Assert.IsNotNull(slot);

            Image slotImage = slot.GetComponent<Image>();
            Assert.IsNotNull(slotImage);
            Assert.IsNull(slotImage.sprite);
            Assert.That(slotImage.color.a, Is.LessThan(0.01f));
            Assert.IsTrue(slotImage.raycastTarget);

            RectTransform slotRect = (RectTransform)slot;
            Assert.AreEqual(new Vector2(70f, 64f), slotRect.sizeDelta);
            Assert.AreEqual(new Vector2((i - 4) * 70f, 0f), slotRect.anchoredPosition);
        }

        Object.DestroyImmediate(authoredBackpackFrame);
    }

    [Test]
    public void RuntimeSlotIconStretchesToFillBackpackSlot()
    {
        rootObject = new GameObject("PackBagCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(1920f, 1080f);

        BackpackUI backpackUi = rootObject.AddComponent<BackpackUI>();
        RectTransform itemPanel = CreateRect(rootRect, "ItemPanel", Vector2.zero, new Vector2(550f, 124f));
        RectTransform slot = CreateRect(itemPanel, "slot_1", Vector2.zero, new Vector2(70f, 80f));

        backpackUi.ConfigureRuntimeLayout();

        Transform icon = slot.Find("ItemIcon");
        Assert.IsNotNull(icon);

        RectTransform iconRect = icon.GetComponent<RectTransform>();
        Assert.IsNotNull(iconRect);
        Assert.AreEqual(Vector2.zero, iconRect.anchorMin);
        Assert.AreEqual(Vector2.one, iconRect.anchorMax);
        Assert.AreEqual(Vector2.zero, iconRect.offsetMin);
        Assert.AreEqual(Vector2.zero, iconRect.offsetMax);
    }

    [Test]
    public void GenericCommonMaterialsOccupyOnlyThreeBackpackSlotsWithoutAttackOverride()
    {
        rootObject = new GameObject("RuntimeBackpackManager");
        BackpackMananger backpack = rootObject.AddComponent<BackpackMananger>();
        ArchitecturalCrystal genericMaterial = ArchitecturalCrystalFactory.CreateCommonStructure(
            ArchitecturalType.Green,
            buildProgressPercent: 100);

        Assert.IsTrue(backpack.PickItem(genericMaterial));
        Assert.IsTrue(backpack.PickItem(genericMaterial));
        Assert.IsTrue(backpack.PickItem(genericMaterial));
        Assert.IsFalse(backpack.PickItem(genericMaterial));
        Assert.AreEqual(3, backpack.GetOccupiedCount());
        Assert.AreEqual(3, backpack.GetCommonMaterialCount());

        InkAttackRuntimeConfig config = InkModifierRuntimeConfig.BuildFromBackpack(backpack);
        Assert.AreEqual(1, config.projectileCount);
        Assert.AreEqual(1, config.maxHitCount);
        Assert.AreEqual(1f, config.projectileScale);

        Assert.IsFalse(RuntimeWeaponTypeResolver.TryGetActiveWeaponOverride(
            backpack,
            out ArchitecturalCrystal _,
            out WeaponType _,
            out int _));
    }

    [Test]
    public void SpecialStructuresOccupyBackpackSlotsAndCapAtThree()
    {
        rootObject = new GameObject("RuntimeBackpackManager");
        BackpackMananger backpack = rootObject.AddComponent<BackpackMananger>();
        ArchitecturalCrystal specialStructure = ArchitecturalCrystalFactory.CreateSpecialStructureMaterial();

        Assert.IsTrue(backpack.PickItem(specialStructure));
        Assert.IsTrue(backpack.PickItem(specialStructure));
        Assert.IsTrue(backpack.PickItem(specialStructure));
        Assert.IsFalse(backpack.PickItem(specialStructure));

        Assert.AreEqual(3, backpack.GetOccupiedCount());
        Assert.AreEqual(3, backpack.GetSpecialStructureMaterialCount());
        Assert.IsTrue(backpack.GetItem(0).HasValue && backpack.GetItem(0).Value.IsSpecialStructure);
        Assert.IsTrue(backpack.GetItem(1).HasValue && backpack.GetItem(1).Value.IsSpecialStructure);
        Assert.IsTrue(backpack.GetItem(2).HasValue && backpack.GetItem(2).Value.IsSpecialStructure);
    }

    [Test]
    public void DoubleClickingRuntimeBackpackSlotDropsLootBagWithStoredStructure()
    {
        rootObject = new GameObject("RuntimeBackpackManager");
        BackpackMananger backpack = rootObject.AddComponent<BackpackMananger>();
        ArchitecturalCrystal crystal = ArchitecturalCrystalFactory.CreateCommonStructure(ArchitecturalType.Brackets);
        Assert.IsTrue(backpack.PickItem(crystal));

        GameObject playerObject = null;
        GameObject eventSystemObject = null;
        GameObject slotObject = null;
        GameObject droppedObject = null;

        try
        {
            playerObject = new GameObject("Player");
            playerObject.tag = "Player";
            playerObject.transform.position = new Vector3(2f, -1f, 0f);
            playerObject.transform.localScale = Vector3.one;

            eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            slotObject = new GameObject("slot_1", typeof(RectTransform), typeof(Image), typeof(BackpackSlot));
            slotObject.transform.SetParent(rootObject.transform, false);
            BackpackSlot slot = slotObject.GetComponent<BackpackSlot>();
            slot.slotIndex = 0;

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                clickCount = 2
            };

            slot.OnPointerClick(eventData);

            Assert.IsFalse(backpack.GetItem(0).HasValue);
            droppedObject = GameObject.Find("Drop_Brackets");
            Assert.IsNotNull(droppedObject);
            Assert.That(droppedObject.transform.position.x, Is.EqualTo(2.2f).Within(0.001f));
            Assert.That(droppedObject.transform.position.y, Is.EqualTo(-1f).Within(0.001f));
            Assert.That(droppedObject.transform.position.z, Is.EqualTo(0f).Within(0.001f));
            Assert.That(droppedObject.transform.localScale.x, Is.EqualTo(0.0875f).Within(0.001f));
            Assert.That(droppedObject.transform.localScale.y, Is.EqualTo(0.0875f).Within(0.001f));

            CrystalInteractHandler handler = droppedObject.GetComponent<CrystalInteractHandler>();
            Assert.IsNotNull(handler);
            Assert.AreEqual(ArchitecturalType.Brackets, handler.type);
            Assert.IsTrue(handler.startClosedAsLootBag);
        }
        finally
        {
            if (droppedObject != null)
            {
                Object.DestroyImmediate(droppedObject);
            }

            if (slotObject != null)
            {
                Object.DestroyImmediate(slotObject);
            }

            if (eventSystemObject != null)
            {
                Object.DestroyImmediate(eventSystemObject);
            }

            if (playerObject != null)
            {
                Object.DestroyImmediate(playerObject);
            }
        }
    }

    [Test]
    public void DebugPanelCommonStructureUsesResolvedBackpackIcon()
    {
        rootObject = new GameObject("RuntimeDebugPage");
        GameDebugPageBootstrapper debugPage = rootObject.AddComponent<GameDebugPageBootstrapper>();
        MethodInfo createDebugCrystal = typeof(GameDebugPageBootstrapper).GetMethod(
            "CreateDebugCrystal",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(createDebugCrystal);

        ArchitecturalCrystal debugCrystal = (ArchitecturalCrystal)createDebugCrystal.Invoke(
            debugPage,
            new object[] { ArchitecturalType.Brackets });
        ArchitecturalCrystal factoryCrystal = ArchitecturalCrystalFactory.CreateCommonStructure(ArchitecturalType.Brackets);

        Assert.AreSame(factoryCrystal.backIcon, debugCrystal.backIcon);
    }

    [Test]
    public void EnsureRuntimeInstancePrefersActiveScenePackBagCanvasOverAdditiveUiScene()
    {
        Scene gameplayScene = CreateScene("GameScene");
        Scene illustratedScene = CreateScene("IllustratedUIScene");

        GameObject illustratedRoot = new GameObject("PackBagCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(illustratedRoot, illustratedScene);
        illustratedRoot.SetActive(false);

        GameObject gameplayRoot = new GameObject("PackBagCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(gameplayRoot, gameplayScene);
        Assert.IsTrue(SceneManager.SetActiveScene(gameplayScene));
        rootObject = gameplayRoot;

        BackpackUI backpackUi = BackpackUI.EnsureRuntimeInstance(false);

        Assert.IsNotNull(backpackUi);
        Assert.AreEqual(gameplayScene, backpackUi.gameObject.scene);
        Assert.IsNull(illustratedRoot.GetComponent<BackpackUI>());
    }

    [Test]
    public void EnsureRuntimeInstancePrefersUsableBaseBackpackRootOverLegacyDuplicate()
    {
        Scene baseScene = CreateScene("NewBase");

        GameObject legacyRoot = new GameObject("PackBagCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(legacyRoot, baseScene);
        legacyRoot.SetActive(false);

        GameObject usableRoot = new GameObject("PackBagCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(usableRoot, baseScene);
        Assert.IsTrue(SceneManager.SetActiveScene(baseScene));
        rootObject = usableRoot;

        RectTransform usableRootRect = usableRoot.GetComponent<RectTransform>();
        RectTransform scenePanel = CreateRect(usableRootRect, "Panel", new Vector2(0f, 751f), new Vector2(1920f, 1502f));
        RectTransform itemPanel = CreateRect(scenePanel, "ItemPanel", Vector2.zero, new Vector2(550f, 124f));
        for (int i = 0; i < 7; i++)
        {
            CreateRect(itemPanel, $"slot_{i + 1}", new Vector2(-222f + i * 74f, 0f), new Vector2(70f, 80f));
        }

        BackpackUI backpackUi = BackpackUI.EnsureRuntimeInstance();

        Assert.IsNotNull(backpackUi);
        Assert.AreSame(usableRoot, backpackUi.gameObject);
        Assert.IsNull(legacyRoot.GetComponent<BackpackUI>());
    }

    [Test]
    public void EnsureRuntimeInstanceCreatesNewBaseStyleFallbackLayoutForFirstPass()
    {
        Scene firstPassScene = CreateScene("FirstPass_1");
        Assert.IsTrue(SceneManager.SetActiveScene(firstPassScene));

        BackpackUI backpackUi = BackpackUI.EnsureRuntimeInstance();
        rootObject = backpackUi != null ? backpackUi.gameObject : null;

        Assert.IsNotNull(backpackUi);
        Assert.AreEqual(firstPassScene, backpackUi.gameObject.scene);

        Transform sharedPanel = backpackUi.transform.Find("Panel");
        Assert.IsNotNull(sharedPanel);

        Transform itemPanel = sharedPanel.Find("ItemPanel");
        Assert.IsNotNull(itemPanel);
        Assert.IsNull(backpackUi.transform.Find("AttackPanel"));

        Image itemPanelImage = itemPanel.GetComponent<Image>();
        Assert.IsNotNull(itemPanelImage);
        Assert.AreSame(Resources.Load<Sprite>("UI/BackpackSlotsHorizontal"), itemPanelImage.sprite);
        Assert.AreEqual(Color.white, itemPanelImage.color);
        Assert.IsFalse(itemPanelImage.raycastTarget);
        Assert.AreEqual(new Vector2(550f, 112f), ((RectTransform)itemPanel).sizeDelta);

        for (int i = 1; i <= 7; i++)
        {
            Transform slot = itemPanel.Find($"slot_{i}");
            Assert.IsNotNull(slot);

            Image slotImage = slot.GetComponent<Image>();
            Assert.IsNotNull(slotImage);
            Assert.IsNull(slotImage.sprite);
            Assert.That(slotImage.color.a, Is.LessThan(0.01f));

            RectTransform slotRect = (RectTransform)slot;
            Assert.AreEqual(new Vector2(70f, 64f), slotRect.sizeDelta);
            Assert.AreEqual(new Vector2((i - 4) * 70f, 0f), slotRect.anchoredPosition);
        }

        Transform mergedAttackSlot = itemPanel.Find("slot_7");
        Assert.IsNotNull(mergedAttackSlot.GetComponent<Button>());
        Assert.IsTrue(backpackUi.IsAttackSlotSelected);
    }

    [Test]
    public void BaseInteractionPromptsUseFloatingStyleToKeepBottomBackpackClear()
    {
        Assert.IsTrue(PlayerInteraction.UseFloatingPromptStyleForScene("NewBase"));
        Assert.IsTrue(PlayerInteraction.UseFloatingPromptStyleForScene("BaseScene"));
        Assert.IsTrue(PlayerInteraction.UseFloatingPromptStyleForScene("GameScene"));
        Assert.IsFalse(PlayerInteraction.UseFloatingPromptStyleForScene("MainScene"));
    }

    private static RectTransform CreateRect(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject rectObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rectTransform = rectObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Image image = rectObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = Color.clear;

        return rectTransform;
    }

    private Scene CreateScene(string sceneName)
    {
        Scene scene = SceneManager.CreateScene(sceneName);
        createdScenes.Add(scene);
        return scene;
    }
}

public sealed class LevelSelectionSceneControllerTests
{
    private readonly List<GameObject> roots = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < roots.Count; i++)
        {
            if (roots[i] != null)
            {
                Object.DestroyImmediate(roots[i]);
            }
        }

        roots.Clear();

        if (RuntimeProgressState.Instance != null &&
            RuntimeProgressState.Instance.gameObject.name == "RuntimeProgressState")
        {
            Object.DestroyImmediate(RuntimeProgressState.Instance.gameObject);
        }
    }

    [Test]
    public void BindSceneCreatesCatalogCardsWhenSceneHasOnlyPanelBackground()
    {
        GameObject canvasObject = new GameObject("LevelSelection", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        roots.Add(canvasObject);
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        GameObject backgroundObject = new GameObject("BackGround", typeof(RectTransform), typeof(Image));
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.SetParent(canvasObject.transform, false);
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject controllerObject = new GameObject("LevelSelectionSceneController");
        roots.Add(controllerObject);
        LevelSelectionSceneController controller = controllerObject.AddComponent<LevelSelectionSceneController>();

        MethodInfo bindScene = typeof(LevelSelectionSceneController).GetMethod(
            "BindScene",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(bindScene);
        bindScene.Invoke(controller, null);

        Transform backdropMask = canvasObject.transform.Find("LevelSelectionBackdropMask");
        Assert.IsNotNull(backdropMask);
        Assert.Less(backdropMask.GetSiblingIndex(), backgroundObject.transform.GetSiblingIndex());

        RawImage backdropBlur = FindChildRecursive(backdropMask, "BackdropBlur").GetComponent<RawImage>();
        Assert.IsNotNull(backdropBlur);
        Assert.IsFalse(backdropBlur.raycastTarget);

        Image backdropTint = FindChildRecursive(backdropMask, "BackdropTint").GetComponent<Image>();
        Assert.IsNotNull(backdropTint);
        Assert.That(backdropTint.color.a, Is.GreaterThan(0.25f).And.LessThan(0.8f));

        Image backdropOverlay = FindChildRecursive(backdropMask, "BackdropOverlay").GetComponent<Image>();
        Assert.IsNotNull(backdropOverlay);
        Assert.That(backdropOverlay.color.a, Is.GreaterThan(0.2f).And.LessThan(0.6f));
        Assert.That(backgroundObject.GetComponent<Image>().color.a, Is.GreaterThan(0.82f).And.LessThan(0.98f));

        Transform scrollRoot = FindChildRecursive(backgroundObject.transform, "LevelSelectionHorizontalScroll");
        Assert.IsNotNull(scrollRoot);
        Assert.Greater(((RectTransform)scrollRoot).anchoredPosition.y, -100f);

        Transform content = FindChildRecursive(scrollRoot, "Content");
        Assert.IsNotNull(content);

        Transform firstStage = content.Find("stage_01_Card");
        Transform secondStage = content.Find("stage_02_Card");
        Transform thirdStage = content.Find("stage_03_Card");
        Transform fourthStage = content.Find("stage_04_Card");
        Assert.IsNotNull(firstStage);
        Assert.IsNotNull(secondStage);
        Assert.IsNotNull(thirdStage);
        Assert.IsNotNull(fourthStage);

        RectTransform firstStageRect = (RectTransform)firstStage;
        Assert.GreaterOrEqual(firstStageRect.sizeDelta.x, 480f);
        Assert.GreaterOrEqual(firstStageRect.sizeDelta.y, 620f);

        Button firstStageButton = firstStage.GetComponent<Button>();
        Assert.IsNotNull(firstStageButton);
        Assert.IsTrue(firstStageButton.interactable);

        AssertImageSpriteName(firstStage, "Setting_4");
        AssertImageSpriteName(FindChildRecursive(firstStage, "StagePreview"), "FuJianTuLou_1");
        AssertImageSpriteName(secondStage, "Setting_4");
        AssertImageSpriteName(FindChildRecursive(secondStage, "StagePreview"), "ZhaoGouBridge");
        AssertImageSpriteName(thirdStage, "Setting_4");
        AssertImageSpriteName(FindChildRecursive(thirdStage, "StagePreview"), "ShuiXiang");
        AssertLockedPreviewUsesDimmedImageWithoutLock(secondStage);
        AssertLockedPreviewUsesDimmedImageWithoutLock(thirdStage);

        AssertRectAbove(FindChildRecursive(firstStage, "StageTitle"), FindChildRecursive(firstStage, "StagePreview"), 10f);
        AssertRectBelow(FindChildRecursive(firstStage, "StageMap"), FindChildRecursive(firstStage, "StagePreview"), 14f);
        AssertRectBelow(FindChildRecursive(firstStage, "StageStatus"), FindChildRecursive(firstStage, "StageMap"), 12f);
        AssertRectBelow(FindChildRecursive(firstStage, "StageHint"), FindChildRecursive(firstStage, "StageStatus"), 10f);
        AssertRectBelow(FindChildRecursive(firstStage, "StageAction"), FindChildRecursive(firstStage, "StageHint"), 14f);

        Transform fourthStagePreview = FindChildRecursive(fourthStage, "StagePreview");
        Assert.IsNotNull(fourthStagePreview);
        Assert.IsFalse(fourthStagePreview.gameObject.activeSelf);

        Transform fourthStageLock = FindChildRecursive(fourthStage, "StageLock");
        Assert.IsNotNull(fourthStageLock);
        Assert.IsTrue(fourthStageLock.gameObject.activeSelf);
        RectTransform fourthStageLockRect = (RectTransform)fourthStageLock;
        Assert.GreaterOrEqual(fourthStageLockRect.sizeDelta.x, 180f);
        Assert.GreaterOrEqual(fourthStageLockRect.sizeDelta.y, 180f);

        Transform closeButton = FindChildRecursive(backgroundObject.transform, "LevelSelectionCloseButton");
        Assert.IsNotNull(closeButton);
        AssertImageSpriteName(closeButton, "CloseButton");
    }

    [Test]
    public void BaseReturnPositionRestoresPlayerAfterClosingLevelSelection()
    {
        Vector3 expectedPosition = new Vector3(2.75f, -1.35f, 0f);
        GameObject playerObject = new GameObject("Player", typeof(Rigidbody2D));
        roots.Add(playerObject);
        playerObject.tag = "Player";

        LevelSelectionSceneController.CaptureBaseReturnPosition(expectedPosition);
        playerObject.transform.position = Vector3.zero;

        Assert.IsTrue(LevelSelectionSceneController.TryApplyPendingBaseReturnPosition(playerObject));
        Assert.AreEqual(expectedPosition, playerObject.transform.position);
        Assert.IsFalse(LevelSelectionSceneController.TryApplyPendingBaseReturnPosition(playerObject));
    }

    private static void AssertImageSpriteName(Transform target, string expectedSpriteName)
    {
        Assert.IsNotNull(target);

        Image image = target.GetComponent<Image>();
        Assert.IsNotNull(image);
        Assert.IsNotNull(image.sprite);
        Assert.AreEqual(expectedSpriteName, image.sprite.name);
    }

    private static void AssertLockedPreviewUsesDimmedImageWithoutLock(Transform stage)
    {
        Transform preview = FindChildRecursive(stage, "StagePreview");
        Assert.IsNotNull(preview);
        Assert.IsTrue(preview.gameObject.activeSelf);

        Image previewImage = preview.GetComponent<Image>();
        Assert.IsNotNull(previewImage);
        Assert.That(previewImage.color.r, Is.LessThan(0.9f));
        Assert.That(previewImage.color.g, Is.LessThan(0.9f));
        Assert.That(previewImage.color.b, Is.LessThan(0.9f));
        Assert.That(previewImage.color.a, Is.LessThan(0.9f));

        Transform stageLock = FindChildRecursive(stage, "StageLock");
        if (stageLock != null)
        {
            Assert.IsFalse(stageLock.gameObject.activeSelf);
        }
    }

    private static void AssertRectAbove(Transform upper, Transform lower, float minGap)
    {
        Assert.IsNotNull(upper);
        Assert.IsNotNull(lower);
        Assert.GreaterOrEqual(RectBottom((RectTransform)upper) - RectTop((RectTransform)lower), minGap);
    }

    private static void AssertRectBelow(Transform lower, Transform upper, float minGap)
    {
        Assert.IsNotNull(lower);
        Assert.IsNotNull(upper);
        Assert.GreaterOrEqual(RectBottom((RectTransform)upper) - RectTop((RectTransform)lower), minGap);
    }

    private static float RectTop(RectTransform rect)
    {
        return rect.anchoredPosition.y + rect.sizeDelta.y * (1f - rect.pivot.y);
    }

    private static float RectBottom(RectTransform rect)
    {
        return rect.anchoredPosition.y - rect.sizeDelta.y * rect.pivot.y;
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != null && child.name == name)
            {
                return child;
            }

            Transform nested = FindChildRecursive(child, name);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
}

public sealed class SubmitSelectionPanelUITests
{
    private readonly List<GameObject> roots = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < roots.Count; i++)
        {
            if (roots[i] != null)
            {
                Object.DestroyImmediate(roots[i]);
            }
        }

        roots.Clear();

        if (BackpackMananger.Instance != null &&
            BackpackMananger.Instance.gameObject.name == "RuntimeBackpackManager")
        {
            Object.DestroyImmediate(BackpackMananger.Instance.gameObject);
        }
    }

    [Test]
    public void OpenPanelShowsOnlyCurrentBuildingIndicator()
    {
        CreatePanel("Building1", out CanvasGroup firstIndicator);
        SubmitSelectionPanelUI secondPanel = CreatePanel("Building2", out CanvasGroup secondIndicator);
        CreatePanel("Building3", out CanvasGroup thirdIndicator);

        secondPanel.TogglePanelForBuilding((int)CatalogueBuildingId.Building2);

        Assert.AreEqual(0f, firstIndicator.alpha);
        Assert.AreEqual(1f, secondIndicator.alpha);
        Assert.AreEqual(0f, thirdIndicator.alpha);
        Assert.IsTrue(firstIndicator.blocksRaycasts);
        Assert.IsTrue(thirdIndicator.blocksRaycasts);
    }

    private SubmitSelectionPanelUI CreatePanel(string name, out CanvasGroup indicatorGroup)
    {
        GameObject buttonObject = new GameObject($"{name}AddButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
        roots.Add(buttonObject);

        indicatorGroup = buttonObject.GetComponent<CanvasGroup>();
        indicatorGroup.alpha = 1f;
        indicatorGroup.interactable = true;
        indicatorGroup.blocksRaycasts = true;

        GameObject panelObject = new GameObject($"{name}AddItemUI", typeof(RectTransform), typeof(CanvasGroup));
        panelObject.transform.SetParent(buttonObject.transform, false);

        SubmitSelectionPanelUI panel = panelObject.AddComponent<SubmitSelectionPanelUI>();
        panel.panelRoot = panelObject;
        return panel;
    }
}
