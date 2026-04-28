using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
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
        Assert.That(slotSize, Is.EqualTo(new Vector2(80f, 80f)));
    }

    [Test]
    public void ConfigureRuntimeLayoutKeepsSceneAuthoredBackpackAboveBottomEdge()
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
        Assert.That(scenePanel.anchoredPosition.y, Is.GreaterThanOrEqualTo(20f));

        Vector3[] itemCorners = new Vector3[4];
        itemPanel.GetWorldCorners(itemCorners);
        Assert.That(itemCorners[0].y, Is.GreaterThan(0f));

        Vector3[] attackCorners = new Vector3[4];
        attackPanel.GetWorldCorners(attackCorners);
        Assert.That(attackCorners[0].y, Is.GreaterThan(0f));
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
