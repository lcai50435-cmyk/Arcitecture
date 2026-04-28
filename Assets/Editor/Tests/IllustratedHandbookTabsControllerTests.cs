using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class IllustratedHandbookTabsControllerTests
{
    private GameObject rootObject;

    [TearDown]
    public void TearDown()
    {
        if (rootObject != null)
        {
            Object.DestroyImmediate(rootObject);
        }

        if (BackpackMananger.Instance != null)
        {
            Object.DestroyImmediate(BackpackMananger.Instance.gameObject);
        }

        if (RuntimeProgressState.Instance != null)
        {
            Object.DestroyImmediate(RuntimeProgressState.Instance.gameObject);
        }

        if (EventSystem.current != null && EventSystem.current.gameObject.name == "EventSystem")
        {
            Object.DestroyImmediate(EventSystem.current.gameObject);
        }
    }

    [Test]
    public void SceneBookmarkBindingKeepsAuthoredButtonAndDisablesTransparentBlocker()
    {
        rootObject = new GameObject("ControllerRoot");
        IllustratedHandbookTabsController controller = rootObject.AddComponent<IllustratedHandbookTabsController>();
        GameObject pageRoot = CreateScenePageRoot();
        pageRoot.transform.SetParent(rootObject.transform, false);

        InvokePrivate(controller, "RegisterScenePageButtons", pageRoot);

        Transform handbook = pageRoot.transform.Find("BookMark/HandBook");
        Button authoredButton = handbook.Find("Button").GetComponent<Button>();
        Button hitAreaButton = handbook.Find("SceneBookmarkHitArea").GetComponent<Button>();
        Image blockerImage = pageRoot.transform.Find("BookMark/Panel").GetComponent<Image>();

        Assert.IsNotNull(authoredButton);
        Assert.AreEqual(Selectable.Transition.None, authoredButton.transition);
        Assert.IsTrue(authoredButton.interactable);
        Assert.IsTrue(authoredButton.targetGraphic.raycastTarget);
        Assert.IsNotNull(hitAreaButton);
        Assert.IsTrue(hitAreaButton.interactable);
        Assert.IsTrue(hitAreaButton.targetGraphic.raycastTarget);
        Assert.IsFalse(blockerImage.raycastTarget);
    }

    [Test]
    public void SceneCloseBindingDisablesTransparentBookmarkBlocker()
    {
        rootObject = new GameObject("ControllerRoot");
        IllustratedHandbookTabsController controller = rootObject.AddComponent<IllustratedHandbookTabsController>();
        GameObject pageRoot = CreateScenePageRoot();
        pageRoot.transform.SetParent(rootObject.transform, false);

        InvokePrivate(controller, "BindSceneCloseButton", pageRoot);

        Image blockerImage = pageRoot.transform.Find("BookMark/Panel").GetComponent<Image>();
        Button hitAreaButton = pageRoot.transform.Find("BookMark/Setting/SceneBookmarkHitArea").GetComponent<Button>();

        Assert.IsFalse(blockerImage.raycastTarget);
        Assert.IsNotNull(hitAreaButton);
        Assert.IsTrue(hitAreaButton.interactable);
        Assert.IsTrue(hitAreaButton.targetGraphic.raycastTarget);
    }

    [Test]
    public void SceneCloseBookmarkStaysAboveTabHitAreasAfterSelectionRefresh()
    {
        rootObject = new GameObject("ControllerRoot");
        IllustratedHandbookTabsController controller = rootObject.AddComponent<IllustratedHandbookTabsController>();
        controller.enabled = false;
        GameObject pageRoot = CreateScenePageRoot();
        pageRoot.transform.SetParent(rootObject.transform, false);

        InvokePrivate(controller, "RegisterScenePageButtons", pageRoot);
        InvokePrivate(controller, "BindSceneCloseButton", pageRoot);
        InvokePrivate(controller, "UpdateSceneBookmarkVisualState", pageRoot.transform, IllustratedHandbookPage.IllustratedHandbook);

        Transform bookmarkRoot = pageRoot.transform.Find("BookMark");
        Transform closeBookmark = bookmarkRoot.Find("Setting");

        Assert.AreEqual(bookmarkRoot.childCount - 1, closeBookmark.GetSiblingIndex());
    }

    [Test]
    public void CloseIllustratedHandbookKeepsSceneAuthoredRootHiddenWithoutRootManager()
    {
        DestroyExistingRootManager();
        rootObject = CreateSceneAuthoredRoot();
        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        manager.ConfigureForRuntime(rootObject, null, new GameObject[0], null, null);

        manager.OpenIllustratedHandbook();
        Assert.IsTrue(rootObject.activeSelf);

        manager.CloseIllustratedHandbook();

        Assert.IsFalse(rootObject.activeSelf);
    }

    [Test]
    public void CloseIllustratedHandbookClosesVisibleSceneRootWhenOpenFlagIsStale()
    {
        DestroyExistingRootManager();
        rootObject = CreateSceneAuthoredRoot();
        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        manager.ConfigureForRuntime(rootObject, null, new GameObject[0], null, null);
        rootObject.SetActive(true);

        manager.CloseIllustratedHandbook();

        Assert.IsFalse(rootObject.activeSelf);
    }

    [Test]
    public void DisabledPersonalInformationBookmarkIsHiddenAndCannotOpenPage()
    {
        rootObject = CreateSceneAuthoredRoot();
        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        IllustratedHandbookTabsController controller = IllustratedHandbookTabsController.EnsureInstalled(manager);

        controller.SetPersonalInformationPageAvailable(false);
        controller.SwitchToPage(IllustratedHandbookPage.PersonalInformation);

        GameObject handbookPage = rootObject.transform.Find("IllustratedHandbookCanvas").gameObject;
        GameObject personalPage = rootObject.transform.Find("PersonalInformationCanvas").gameObject;
        GameObject personalBookmark = rootObject.transform.Find("IllustratedHandbookCanvas/BookMark/PersonalInformation").gameObject;

        Assert.IsTrue(handbookPage.activeSelf);
        Assert.IsFalse(personalPage.activeSelf);
        Assert.IsFalse(personalBookmark.activeSelf);
    }

    [Test]
    public void SceneBookmarkSelectionKeepsAuthoredTabsUniformSize()
    {
        rootObject = new GameObject("ControllerRoot");
        IllustratedHandbookTabsController controller = rootObject.AddComponent<IllustratedHandbookTabsController>();
        controller.enabled = false;

        GameObject pageRoot = CreateScenePageRoot();
        pageRoot.transform.SetParent(rootObject.transform, false);

        RectTransform handbookBookmark = pageRoot.transform.Find("BookMark/HandBook") as RectTransform;
        RectTransform personalBookmark = pageRoot.transform.Find("BookMark/PersonalInformation") as RectTransform;
        RectTransform albumBookmark = pageRoot.transform.Find("BookMark/PhotoAlbum") as RectTransform;
        RectTransform settingBookmark = pageRoot.transform.Find("BookMark/Mission") as RectTransform;

        handbookBookmark.sizeDelta = new Vector2(240f, 110f);
        handbookBookmark.localScale = new Vector3(1.2f, 1.2f, 1f);

        InvokePrivate(controller, "RegisterScenePageButtons", pageRoot);
        InvokePrivate(controller, "UpdateSceneBookmarkVisualState", pageRoot.transform, IllustratedHandbookPage.IllustratedHandbook);

        Vector2 expectedSize = new Vector2(200f, 80f);
        Assert.AreEqual(expectedSize, handbookBookmark.sizeDelta);
        Assert.AreEqual(expectedSize, personalBookmark.sizeDelta);
        Assert.AreEqual(expectedSize, albumBookmark.sizeDelta);
        Assert.AreEqual(expectedSize, settingBookmark.sizeDelta);
        Assert.AreEqual(Vector3.one, handbookBookmark.localScale);
        Assert.AreEqual(Vector3.one, personalBookmark.localScale);
        Assert.AreEqual(Vector3.one, albumBookmark.localScale);
        Assert.AreEqual(Vector3.one, settingBookmark.localScale);
    }

    [Test]
    public void IllustratedHandbookPageShowsSpecialStructureInsideRuntimeBackpack()
    {
        RuntimeProgressState progressState = RuntimeProgressState.EnsureInstance();
        progressState.ResetProgress(false);

        BackpackMananger backpack = CreateRuntimeBackpack();
        Assert.IsTrue(backpack.PickItem(ArchitecturalCrystalFactory.CreateSpecialStructureMaterial()));

        rootObject = CreateSceneAuthoredRoot();
        GameObject handbookPage = rootObject.transform.Find("IllustratedHandbookCanvas").gameObject;
        CreateSceneAuthoredHandbookSurface(handbookPage);

        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        IllustratedHandbookTabsController controller = IllustratedHandbookTabsController.EnsureInstalled(manager);
        controller.SwitchToPage(IllustratedHandbookPage.IllustratedHandbook);

        Transform backpackTray = FindDescendant(handbookPage.transform, "HandbookBackpackTray");
        Assert.IsNotNull(backpackTray);
        Assert.AreEqual(handbookPage.transform, backpackTray.parent);

        RectTransform backpackTrayRect = backpackTray as RectTransform;
        Assert.IsNotNull(backpackTrayRect);
        Assert.AreEqual(new Vector2(0.5f, 0f), backpackTrayRect.anchorMin);
        Assert.AreEqual(new Vector2(0.5f, 0f), backpackTrayRect.anchorMax);
        Assert.AreEqual(new Vector2(0.5f, 0f), backpackTrayRect.pivot);
        Assert.AreEqual(new Vector2(0f, 12f), backpackTrayRect.anchoredPosition);

        Transform firstSlot = FindDescendant(backpackTray, "HandbookBackpackSlot_1");
        Assert.IsNotNull(firstSlot);
        RectTransform firstSlotRect = firstSlot as RectTransform;
        Assert.IsNotNull(firstSlotRect);
        Assert.AreEqual(new Vector2(80f, 80f), firstSlotRect.sizeDelta);
        Assert.IsTrue(firstSlot.GetComponents<MonoBehaviour>().Any(component => component is IBeginDragHandler));

        Image firstSlotIcon = FindDescendant(firstSlot, "ItemIcon").GetComponent<Image>();
        Assert.IsTrue(firstSlotIcon.enabled);
        Assert.IsTrue(backpack.GetItem(0).HasValue);
        Assert.IsTrue(backpack.GetItem(0).Value.IsSpecialStructure);

        Transform specialStack = FindDescendant(backpackTray, "SpecialMaterialStack");
        Assert.IsTrue(specialStack == null || !specialStack.gameObject.activeSelf);
    }

    [Test]
    public void SubmitCommonMaterialButtonConsumesBackpackMaterialsForSelectedBuilding()
    {
        RuntimeProgressState progressState = RuntimeProgressState.EnsureInstance();
        progressState.ResetProgress(false);

        BackpackMananger backpack = CreateRuntimeBackpack();
        Assert.IsTrue(backpack.PickItem(ArchitecturalCrystalFactory.CreateGenericCommonMaterial()));
        Assert.IsTrue(backpack.PickItem(ArchitecturalCrystalFactory.CreateGenericCommonMaterial()));

        rootObject = CreateSceneAuthoredRoot();
        GameObject handbookPage = rootObject.transform.Find("IllustratedHandbookCanvas").gameObject;
        CreateSceneAuthoredHandbookSurface(handbookPage);

        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        IllustratedHandbookTabsController controller = IllustratedHandbookTabsController.EnsureInstalled(manager);
        controller.SwitchToPage(IllustratedHandbookPage.IllustratedHandbook);

        Button submitButton = FindDescendant(handbookPage.transform, "SubmitCommonMaterialButton").GetComponent<Button>();
        submitButton.onClick.Invoke();

        Assert.AreEqual(0, backpack.GetCommonMaterialCount());
        Assert.Greater(progressState.GetBuildingProgress(CatalogueBuildingId.Building1), 0);

        Slider generalSlider = FindDescendant(handbookPage.transform, "GeneralProgressSlider").GetComponent<Slider>();
        Assert.AreEqual(progressState.GetBuildingProgress(CatalogueBuildingId.Building1), (int)generalSlider.value);
    }

    [Test]
    public void DroppingSpecialBackpackItemOntoProprietarySlotConsumesBackpackItem()
    {
        RuntimeProgressState progressState = RuntimeProgressState.EnsureInstance();
        progressState.ResetProgress(false);

        BackpackMananger backpack = CreateRuntimeBackpack();
        Assert.IsTrue(backpack.PickItem(ArchitecturalCrystalFactory.CreateSpecialStructureMaterial()));

        rootObject = CreateSceneAuthoredRoot();
        GameObject handbookPage = rootObject.transform.Find("IllustratedHandbookCanvas").gameObject;
        CreateSceneAuthoredHandbookSurface(handbookPage);

        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        IllustratedHandbookTabsController controller = IllustratedHandbookTabsController.EnsureInstalled(manager);
        controller.SwitchToPage(IllustratedHandbookPage.IllustratedHandbook);

        Transform backpackSlot = FindDescendant(handbookPage.transform, "HandbookBackpackSlot_1");
        Button firstProprietarySlot = FindDescendant(handbookPage.transform, "ProprietarySlot_1").GetComponent<Button>();
        IDropHandler dropHandler = firstProprietarySlot.GetComponents<MonoBehaviour>().OfType<IDropHandler>().FirstOrDefault();
        Assert.IsNotNull(dropHandler);
        Assert.IsNotNull(backpackSlot);

        EventSystem eventSystem = EventSystem.current ?? new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        PointerEventData eventData = new PointerEventData(eventSystem)
        {
            pointerDrag = backpackSlot.gameObject
        };

        dropHandler.OnDrop(eventData);

        Assert.IsTrue(progressState.IsSlotUnlocked(CatalogueBuildingId.Building1, 0));
        Assert.IsFalse(backpack.GetItem(0).HasValue);
        Assert.AreEqual(0, backpack.GetSpecialStructureMaterialCount());
    }

    private static GameObject CreateScenePageRoot()
    {
        return CreateScenePageRoot("IllustratedHandbookCanvas");
    }

    private static GameObject CreateSceneAuthoredRoot()
    {
        GameObject sceneRoot = new GameObject(IllustratedHandbookTabsController.RootObjectName, typeof(RectTransform));
        CreateScenePageRoot("IllustratedHandbookCanvas").transform.SetParent(sceneRoot.transform, false);
        CreateScenePageRoot("PersonalInformationCanvas").transform.SetParent(sceneRoot.transform, false);
        CreateScenePageRoot("PhotoAlbumCanvas").transform.SetParent(sceneRoot.transform, false);
        CreateScenePageRoot("SettingCanvas").transform.SetParent(sceneRoot.transform, false);
        return sceneRoot;
    }

    private static GameObject CreateScenePageRoot(string pageName)
    {
        GameObject pageRoot = new GameObject(pageName, typeof(RectTransform));
        GameObject bookmarkRoot = new GameObject("BookMark", typeof(RectTransform));
        bookmarkRoot.transform.SetParent(pageRoot.transform, false);

        CreateBookmark("HandBook", bookmarkRoot.transform);
        CreateBookmark("PersonalInformation", bookmarkRoot.transform);
        CreateBookmark("PhotoAlbum", bookmarkRoot.transform);
        CreateBookmark("Mission", bookmarkRoot.transform);
        CreateBookmark("Setting", bookmarkRoot.transform);

        GameObject transparentPanel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        transparentPanel.transform.SetParent(bookmarkRoot.transform, false);
        Image panelImage = transparentPanel.GetComponent<Image>();
        panelImage.color = Color.clear;
        panelImage.raycastTarget = true;

        return pageRoot;
    }

    private static void DestroyExistingRootManager()
    {
        if (UIRootManager.Instance != null)
        {
            Object.DestroyImmediate(UIRootManager.Instance.gameObject);
        }
    }

    private static BackpackMananger CreateRuntimeBackpack()
    {
        GameObject backpackObject = new GameObject("RuntimeBackpackManager");
        return backpackObject.AddComponent<BackpackMananger>();
    }

    private static void CreateSceneAuthoredHandbookSurface(GameObject handbookPage)
    {
        GameObject leftPanel = new GameObject("LeftPanel", typeof(RectTransform));
        leftPanel.transform.SetParent(handbookPage.transform, false);

        GameObject card = new GameObject("ArcitectureImage_1", typeof(RectTransform), typeof(Image), typeof(Button));
        card.transform.SetParent(leftPanel.transform, false);
        CreateTmpText("Name", card.transform, "福建土楼");
        CreateChild<Image>(card.transform, "Picture");
        CreateChild<Slider>(card.transform, "Slider");
        CreateChild<Image>(card.transform, "Lock");
        CreateChild<Image>(card.transform, "Unlock");

        GameObject rightPageBackground = new GameObject("BackGround", typeof(RectTransform));
        rightPageBackground.transform.SetParent(handbookPage.transform, false);

        GameObject rightIntroduction = new GameObject("RightIntroduction", typeof(RectTransform));
        rightIntroduction.transform.SetParent(rightPageBackground.transform, false);
        CreateTmpText("Name", rightIntroduction.transform, string.Empty);

        GameObject buildingImage = new GameObject("BuildingImage", typeof(RectTransform), typeof(Image));
        buildingImage.transform.SetParent(rightIntroduction.transform, false);
        CreateTmpText("Introduction", buildingImage.transform, string.Empty);

        GameObject proprietary = new GameObject("ProprietaryMaterial", typeof(RectTransform));
        proprietary.transform.SetParent(rightIntroduction.transform, false);
        CreateTmpText("Label", proprietary.transform, "专用进度（0/3）");
        for (int i = 0; i < 3; i++)
        {
            GameObject slot = new GameObject($"ProprietarySlot_{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
            slot.transform.SetParent(proprietary.transform, false);
        }

        GameObject general = new GameObject("MaterialForGeneralPurpose", typeof(RectTransform));
        general.transform.SetParent(rightIntroduction.transform, false);
        CreateTmpText("Label", general.transform, "通用材料");
        GameObject sliderObject = new GameObject("GeneralProgressSlider", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(general.transform, false);
        GameObject button = new GameObject("SubmitCommonMaterialButton", typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(general.transform, false);
        CreateTmpText("Label", button.transform, "提交通用材料");
    }

    private static T CreateChild<T>(Transform parent, string name)
        where T : Component
    {
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(T));
        child.transform.SetParent(parent, false);
        return child.GetComponent<T>();
    }

    private static TMP_Text CreateTmpText(string name, Transform parent, string text)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TMP_Text tmpText = textObject.GetComponent<TMP_Text>();
        tmpText.text = text;
        return tmpText;
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDescendant(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void CreateBookmark(string name, Transform parent)
    {
        GameObject bookmark = new GameObject(name, typeof(RectTransform), typeof(Image));
        bookmark.transform.SetParent(parent, false);

        GameObject authoredButton = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        authoredButton.transform.SetParent(bookmark.transform, false);

        Image buttonImage = authoredButton.GetComponent<Image>();
        buttonImage.color = Color.clear;
        buttonImage.raycastTarget = true;
        authoredButton.GetComponent<Button>().targetGraphic = buttonImage;
    }

    private static void InvokePrivate(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        method.Invoke(target, args);
    }
}
