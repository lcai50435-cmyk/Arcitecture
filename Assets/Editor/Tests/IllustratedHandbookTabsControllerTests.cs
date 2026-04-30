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

        PlayerLoadoutRuntime.ClearRuntimeWeaponOverride();
        PlayerLoadoutRuntime.ClearDebugWeaponOverride();
        PlayerLoadoutRuntime.CurrentWeaponType = WeaponType.DirectInk;
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
    public void PersonalInformationAttributeSlidersAreReadOnlyDisplays()
    {
        rootObject = CreateSceneAuthoredRoot();
        GameObject personalPage = rootObject.transform.Find("PersonalInformationCanvas").gameObject;
        CreateSceneAuthoredPersonalAttributeSurface(personalPage);

        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        IllustratedHandbookTabsController controller = IllustratedHandbookTabsController.EnsureInstalled(manager);

        controller.SwitchToPage(IllustratedHandbookPage.PersonalInformation);

        Slider[] sliders = personalPage.GetComponentsInChildren<Slider>(true);
        Assert.AreEqual(4, sliders.Length);
        for (int i = 0; i < sliders.Length; i++)
        {
            AssertSliderIsReadOnlyDisplay(sliders[i]);
        }
    }

    [Test]
    public void PersonalInformationAttributeValuesShowCurrentValueOnly()
    {
        rootObject = CreateSceneAuthoredRoot();
        GameObject personalPage = rootObject.transform.Find("PersonalInformationCanvas").gameObject;
        CreateSceneAuthoredPersonalAttributeSurface(personalPage);

        Slider healthSlider = FindDescendant(personalPage.transform, "生命值").GetComponent<Slider>();
        TMP_Text valueText = CreateTmpText("ValueText", healthSlider.transform, "75/100");

        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        IllustratedHandbookTabsController controller = IllustratedHandbookTabsController.EnsureInstalled(manager);

        controller.SwitchToPage(IllustratedHandbookPage.PersonalInformation);

        Assert.AreEqual("75", valueText.text);
    }

    [Test]
    public void PersonalInkSelectionUsesAuthoredIconIndexAndMovesSelectionVisual()
    {
        PlayerLoadoutRuntime.CurrentWeaponType = WeaponType.DirectInk;

        rootObject = CreateSceneAuthoredRoot();
        GameObject personalPage = rootObject.transform.Find("PersonalInformationCanvas").gameObject;
        CreateSceneAuthoredPersonalInkSurface(personalPage);

        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        IllustratedHandbookTabsController controller = IllustratedHandbookTabsController.EnsureInstalled(manager);

        controller.SwitchToPage(IllustratedHandbookPage.PersonalInformation);

        Transform firstOption = FindDescendant(personalPage.transform, "Image_1");
        Transform thirdOption = FindDescendant(personalPage.transform, "Image_3");
        AssertPersonalInkSelected(firstOption, true);
        AssertPersonalInkSelected(thirdOption, false);
        Assert.IsTrue(firstOption.GetComponent<Graphic>().raycastTarget);
        Assert.IsFalse(firstOption.Find("Circle").GetComponent<Graphic>().raycastTarget);

        IPointerEnterHandler hoverHandler = thirdOption.GetComponents<MonoBehaviour>().OfType<IPointerEnterHandler>().FirstOrDefault();
        IPointerExitHandler hoverExitHandler = thirdOption.GetComponents<MonoBehaviour>().OfType<IPointerExitHandler>().FirstOrDefault();
        Assert.IsNotNull(hoverHandler);
        Assert.IsNotNull(hoverExitHandler);
        EventSystem eventSystem = EventSystem.current ?? new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        hoverHandler.OnPointerEnter(new PointerEventData(eventSystem));
        Assert.AreEqual(1.08f, thirdOption.localScale.x, 0.001f);
        Assert.AreEqual(1.08f, thirdOption.localScale.y, 0.001f);

        IPointerClickHandler clickHandler = thirdOption.GetComponents<MonoBehaviour>().OfType<IPointerClickHandler>().FirstOrDefault();
        Assert.IsNotNull(clickHandler);
        clickHandler.OnPointerClick(new PointerEventData(eventSystem));

        PlayerLoadoutRuntime.EnsureCurrentWeaponUnlocked();
        hoverExitHandler.OnPointerExit(new PointerEventData(eventSystem));

        Assert.AreEqual(WeaponType.DirectInk, PlayerLoadoutRuntime.CurrentWeaponType);
        Assert.AreEqual(WeaponType.DirectInk, RuntimeWeaponTypeResolver.ResolveEffectiveWeaponType(null));
        AssertPersonalInkSelected(firstOption, true);
        AssertPersonalInkSelected(thirdOption, false);
    }

    [Test]
    public void PersonalBackpackSlotsRenderEmptySlotsTransparentAndItemsAsIcons()
    {
        BackpackMananger backpack = CreateRuntimeBackpack();
        Assert.IsTrue(backpack.PickItem(ArchitecturalCrystalFactory.CreateSpecialStructureMaterial()));

        rootObject = CreateSceneAuthoredRoot();
        GameObject personalPage = rootObject.transform.Find("PersonalInformationCanvas").gameObject;
        CreateSceneAuthoredPersonalBackpackSurface(personalPage);

        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        IllustratedHandbookTabsController controller = IllustratedHandbookTabsController.EnsureInstalled(manager);

        controller.SwitchToPage(IllustratedHandbookPage.PersonalInformation);

        Transform occupiedSlot = FindDescendant(personalPage.transform, "Slot_1");
        Transform emptySlot = FindDescendant(personalPage.transform, "Slot_2");
        Image occupiedSlotImage = occupiedSlot.GetComponent<Image>();
        Image emptySlotImage = emptySlot.GetComponent<Image>();
        Image occupiedIcon = FindDescendant(occupiedSlot, "ItemIcon").GetComponent<Image>();
        Image emptyIcon = FindDescendant(emptySlot, "ItemIcon").GetComponent<Image>();
        Transform emptySelection = emptySlot.Find("Image");

        Assert.AreEqual(0f, occupiedSlotImage.color.a, 0.001f);
        Assert.AreEqual(0f, emptySlotImage.color.a, 0.001f);
        Assert.IsTrue(occupiedIcon.enabled);
        Assert.IsNotNull(occupiedIcon.sprite);
        Assert.AreEqual(Color.white, occupiedIcon.color);
        Assert.IsFalse(emptyIcon.enabled);
        Assert.IsNull(emptyIcon.sprite);

        InvokePrivate(controller, "SelectPersonalBackpackSlot", 1);

        Assert.IsFalse(emptySelection.gameObject.activeSelf);
    }

    [Test]
    public void SceneAuthoredHandbookProgressSlidersAreReadOnlyDisplays()
    {
        rootObject = CreateSceneAuthoredRoot();
        GameObject handbookPage = rootObject.transform.Find("IllustratedHandbookCanvas").gameObject;
        CreateSceneAuthoredHandbookSurface(handbookPage);

        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        IllustratedHandbookTabsController controller = IllustratedHandbookTabsController.EnsureInstalled(manager);

        controller.SwitchToPage(IllustratedHandbookPage.IllustratedHandbook);

        Slider[] sliders = handbookPage.GetComponentsInChildren<Slider>(true);
        Assert.AreEqual(4, sliders.Length);
        for (int i = 0; i < sliders.Length; i++)
        {
            AssertSliderIsReadOnlyDisplay(sliders[i]);
        }
    }

    [Test]
    public void SceneAuthoredHandbookProgressSlidersClearAuthoredFillWidthBias()
    {
        RuntimeProgressState.EnsureInstance().ResetProgress(false);

        rootObject = CreateSceneAuthoredRoot();
        GameObject handbookPage = rootObject.transform.Find("IllustratedHandbookCanvas").gameObject;
        CreateSceneAuthoredHandbookSurface(handbookPage);

        Slider[] sliders = handbookPage.GetComponentsInChildren<Slider>(true);
        Assert.AreEqual(2, sliders.Length);
        for (int i = 0; i < sliders.Length; i++)
        {
            RectTransform fillRect = sliders[i].fillRect;
            RectTransform fillAreaRect = fillRect.parent as RectTransform;
            Assert.IsNotNull(fillRect, sliders[i].name);
            Assert.IsNotNull(fillAreaRect, sliders[i].name);
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5f, 0f);
            fillAreaRect.offsetMax = new Vector2(-15f, 0f);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.zero;
            fillRect.offsetMin = new Vector2(3f, 0f);
            fillRect.offsetMax = new Vector2(7f, 0f);
            fillRect.sizeDelta = new Vector2(10f, 0f);
        }

        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        IllustratedHandbookTabsController controller = IllustratedHandbookTabsController.EnsureInstalled(manager);

        controller.SwitchToPage(IllustratedHandbookPage.IllustratedHandbook);

        for (int i = 0; i < sliders.Length; i++)
        {
            RectTransform fillRect = sliders[i].fillRect;
            RectTransform fillAreaRect = fillRect.parent as RectTransform;
            Assert.AreEqual(0f, fillAreaRect.offsetMin.x, 0.001f, sliders[i].name);
            Assert.AreEqual(0f, fillAreaRect.offsetMax.x, 0.001f, sliders[i].name);
            Assert.AreEqual(0f, fillAreaRect.anchorMin.x, 0.001f, sliders[i].name);
            Assert.AreEqual(1f, fillAreaRect.anchorMax.x, 0.001f, sliders[i].name);
            Assert.AreEqual(Vector2.zero, fillRect.offsetMin, sliders[i].name);
            Assert.AreEqual(Vector2.zero, fillRect.offsetMax, sliders[i].name);
            Assert.AreEqual(0f, fillRect.sizeDelta.x, 0.001f, sliders[i].name);
        }
    }

    [Test]
    public void SceneAuthoredProprietaryIconsFollowSelectedBuilding()
    {
        RuntimeProgressState.EnsureInstance().ResetProgress(false);

        rootObject = CreateSceneAuthoredRoot();
        GameObject handbookPage = rootObject.transform.Find("IllustratedHandbookCanvas").gameObject;
        CreateSceneAuthoredHandbookSurface(handbookPage);

        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        IllustratedHandbookTabsController controller = IllustratedHandbookTabsController.EnsureInstalled(manager);
        controller.SwitchToPage(IllustratedHandbookPage.IllustratedHandbook);

        Assert.AreEqual("RammedEarthUI", GetSlotSpriteName(handbookPage.transform, "ProprietarySlot_1"));
        Assert.AreEqual("ThickWallUI", GetSlotSpriteName(handbookPage.transform, "ProprietarySlot_2"));
        Assert.AreEqual("TimberworkUI", GetSlotSpriteName(handbookPage.transform, "ProprietarySlot_3"));

        GameObject secondCard = FindDescendant(handbookPage.transform, "ArcitectureImage_2").gameObject;
        controller.SelectSceneAuthoredHandbookCard(secondCard);

        Assert.AreEqual("SingleSpan", GetSlotSpriteName(handbookPage.transform, "ProprietarySlot_1"));
        Assert.AreEqual("SmallArch", GetSlotSpriteName(handbookPage.transform, "ProprietarySlot_2"));
        Assert.AreEqual("VoussoirConstruction", GetSlotSpriteName(handbookPage.transform, "ProprietarySlot_3"));
    }

    [Test]
    public void SceneAuthoredWaterTownProprietarySlotImagesStayInsetInsideAuthoredSlots()
    {
        RuntimeProgressState.EnsureInstance().ResetProgress(false);

        rootObject = CreateSceneAuthoredRoot();
        GameObject handbookPage = rootObject.transform.Find("IllustratedHandbookCanvas").gameObject;
        CreateSceneAuthoredHandbookSurfaceWithNestedProprietarySlots(handbookPage);
        FindDescendant(handbookPage.transform, "Name").GetComponent<TMP_Text>().text = "苏浙水乡";

        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        IllustratedHandbookTabsController controller = IllustratedHandbookTabsController.EnsureInstalled(manager);

        controller.SwitchToPage(IllustratedHandbookPage.IllustratedHandbook);

        for (int i = 1; i <= 3; i++)
        {
            RectTransform slotRect = FindDescendant(handbookPage.transform, $"Material_{i}") as RectTransform;
            RectTransform iconRect = FindDescendant(slotRect, "Image") as RectTransform;

            Assert.IsNotNull(slotRect);
            Assert.IsNotNull(iconRect);
            Assert.That(iconRect.sizeDelta.x, Is.LessThanOrEqualTo(slotRect.sizeDelta.x - 8f));
            Assert.That(iconRect.sizeDelta.y, Is.LessThanOrEqualTo(slotRect.sizeDelta.y - 8f));
            Assert.AreEqual(Vector2.zero, iconRect.anchoredPosition);
        }
    }

    [Test]
    public void SceneAuthoredWaterTownProprietaryIconsUseCatalogueSprites()
    {
        RuntimeProgressState.EnsureInstance().ResetProgress(false);

        rootObject = CreateSceneAuthoredRoot();
        GameObject handbookPage = rootObject.transform.Find("IllustratedHandbookCanvas").gameObject;
        CreateSceneAuthoredHandbookSurfaceWithNestedProprietarySlots(handbookPage);
        FindDescendant(handbookPage.transform, "Name").GetComponent<TMP_Text>().text = "苏浙水乡";

        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        IllustratedHandbookTabsController controller = IllustratedHandbookTabsController.EnsureInstalled(manager);

        controller.SwitchToPage(IllustratedHandbookPage.IllustratedHandbook);

        Assert.AreEqual("ShuiXiang", GetSlotSpriteName(handbookPage.transform, "Material_1"));
        Assert.AreEqual("RoofTile", GetSlotSpriteName(handbookPage.transform, "Material_2"));
        Assert.AreEqual("AnhuiWaterTowns_1", GetSlotSpriteName(handbookPage.transform, "Material_3"));

        Image firstSlotSurface = FindDescendant(handbookPage.transform, "Material_1").GetComponent<Image>();
        Assert.AreEqual(0f, firstSlotSurface.color.a, 0.001f);
        Assert.IsTrue(firstSlotSurface.raycastTarget);
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
        Transform backpackSlotIcon = FindDescendant(backpackSlot, "ItemIcon");
        Button firstProprietarySlot = FindDescendant(handbookPage.transform, "ProprietarySlot_1").GetComponent<Button>();
        IDropHandler dropHandler = firstProprietarySlot.GetComponents<MonoBehaviour>().OfType<IDropHandler>().FirstOrDefault();
        Assert.IsNotNull(dropHandler);
        Assert.IsNotNull(backpackSlot);
        Assert.IsNotNull(backpackSlotIcon);

        EventSystem eventSystem = EventSystem.current ?? new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        PointerEventData eventData = new PointerEventData(eventSystem)
        {
            pointerDrag = backpackSlotIcon.gameObject
        };

        dropHandler.OnDrop(eventData);

        Assert.IsTrue(progressState.IsSlotUnlocked(CatalogueBuildingId.Building1, 0));
        Assert.IsFalse(backpack.GetItem(0).HasValue);
        Assert.AreEqual(0, backpack.GetSpecialStructureMaterialCount());
    }

    [Test]
    public void DroppingFinalSpecialBackpackItemCompletesAndUnlocksSelectedBuilding()
    {
        RuntimeProgressState progressState = RuntimeProgressState.EnsureInstance();
        progressState.ResetProgress(false);
        BuildingDefinition definition = BuildingDefinitionLibrary.Get(CatalogueBuildingId.Building1);

        Assert.IsTrue(progressState.AddBuildingProgress(
            CatalogueBuildingId.Building1,
            definition.requiredProgress,
            out _));
        for (int i = 1; i < definition.slotDefinitions.Length; i++)
        {
            Assert.IsTrue(progressState.TryUnlockSlot(
                CatalogueBuildingId.Building1,
                i,
                out _,
                out _));
        }

        Assert.AreEqual(90, progressState.GetBuildingProgress(CatalogueBuildingId.Building1));
        Assert.IsFalse(progressState.IsBuildingUnlocked(CatalogueBuildingId.Building1));

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
        Assert.AreEqual(definition.requiredProgress, progressState.GetBuildingProgress(CatalogueBuildingId.Building1));
        Assert.IsTrue(progressState.IsBuildingUnlocked(CatalogueBuildingId.Building1));
        Assert.IsFalse(backpack.GetItem(0).HasValue);
    }

    [Test]
    public void SceneAuthoredBuildingImageBecomesDetailEntryAfterRuntimeUnlock()
    {
        RuntimeProgressState progressState = RuntimeProgressState.EnsureInstance();
        progressState.ResetProgress(false);

        rootObject = CreateSceneAuthoredRoot();
        GameObject handbookPage = rootObject.transform.Find("IllustratedHandbookCanvas").gameObject;
        CreateSceneAuthoredHandbookSurface(handbookPage);

        GameObject firstCard = FindDescendant(handbookPage.transform, "ArcitectureImage_1").gameObject;
        BuildingDetailData detailData = firstCard.AddComponent<BuildingDetailData>();
        detailData.buildingName = "福建土楼详情";
        detailData.introduction1 = "解锁后展示完整建筑详情。";

        GameObject detailPanel = new GameObject("DetailedInformationCanvas", typeof(RectTransform));
        detailPanel.transform.SetParent(rootObject.transform, false);
        Text detailTitle = CreateChild<Text>(detailPanel.transform, "Title");
        DetailedInformationUI detailUi = detailPanel.AddComponent<DetailedInformationUI>();
        detailUi.detailedInformationPanel = detailPanel;
        detailUi.page1NameText = detailTitle;
        detailPanel.SetActive(false);

        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        IllustratedHandbookTabsController controller = IllustratedHandbookTabsController.EnsureInstalled(manager);
        controller.SwitchToPage(IllustratedHandbookPage.IllustratedHandbook);

        Button detailButton = FindDescendant(handbookPage.transform, "BuildingImage").GetComponent<Button>();
        Assert.IsNotNull(detailButton);
        Assert.IsFalse(detailButton.interactable);

        CompleteBuildingUnlock(progressState, CatalogueBuildingId.Building1);

        Assert.IsTrue(detailButton.interactable);
        detailButton.onClick.Invoke();

        Assert.IsTrue(detailPanel.activeSelf);
        Assert.AreEqual("福建土楼详情", detailTitle.text);
    }

    [Test]
    public void DroppingRuntimeBackpackSlotOntoProprietarySlotConsumesBackpackItem()
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

        GameObject runtimeSlotObject = new GameObject("RuntimeBackpackSlot_1", typeof(RectTransform), typeof(Image), typeof(BackpackSlot));
        runtimeSlotObject.transform.SetParent(rootObject.transform, false);
        runtimeSlotObject.GetComponent<BackpackSlot>().slotIndex = 0;
        Image runtimeSlotIcon = CreateChild<Image>(runtimeSlotObject.transform, "ItemIcon");

        Button firstProprietarySlot = FindDescendant(handbookPage.transform, "ProprietarySlot_1").GetComponent<Button>();
        IDropHandler dropHandler = firstProprietarySlot.GetComponents<MonoBehaviour>().OfType<IDropHandler>().FirstOrDefault();
        Assert.IsNotNull(dropHandler);

        EventSystem eventSystem = EventSystem.current ?? new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        PointerEventData eventData = new PointerEventData(eventSystem)
        {
            pointerDrag = runtimeSlotIcon.gameObject
        };

        dropHandler.OnDrop(eventData);

        Assert.IsTrue(progressState.IsSlotUnlocked(CatalogueBuildingId.Building1, 0));
        Assert.IsFalse(backpack.GetItem(0).HasValue);
        Assert.AreEqual(0, backpack.GetSpecialStructureMaterialCount());
    }

    [Test]
    public void SceneAuthoredNestedProprietarySlotDimsVisibleImageAndAcceptsDropOnFirstSlotRoot()
    {
        RuntimeProgressState progressState = RuntimeProgressState.EnsureInstance();
        progressState.ResetProgress(false);

        BackpackMananger backpack = CreateRuntimeBackpack();
        Assert.IsTrue(backpack.PickItem(ArchitecturalCrystalFactory.CreateSpecialStructureMaterial()));

        rootObject = CreateSceneAuthoredRoot();
        GameObject handbookPage = rootObject.transform.Find("IllustratedHandbookCanvas").gameObject;
        CreateSceneAuthoredHandbookSurfaceWithNestedProprietarySlots(handbookPage);

        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        IllustratedHandbookTabsController controller = IllustratedHandbookTabsController.EnsureInstalled(manager);
        controller.SwitchToPage(IllustratedHandbookPage.IllustratedHandbook);

        Transform firstSlotRoot = FindDescendant(handbookPage.transform, "Material_1");
        Assert.IsNotNull(firstSlotRoot);

        Image visibleIcon = FindDescendant(firstSlotRoot, "Image").GetComponent<Image>();
        Assert.Less(visibleIcon.color.r, 1f);
        Assert.Less(visibleIcon.color.g, 1f);
        Assert.Less(visibleIcon.color.b, 1f);

        Image transparentHitArea = FindDescendant(firstSlotRoot, "Button_1").GetComponent<Image>();
        Assert.LessOrEqual(transparentHitArea.color.a, 0.001f);
        Assert.IsTrue(transparentHitArea.raycastTarget);

        IDropHandler dropHandler = firstSlotRoot.GetComponents<MonoBehaviour>().OfType<IDropHandler>().FirstOrDefault();
        Assert.IsNotNull(dropHandler);

        Transform backpackSlot = FindDescendant(handbookPage.transform, "HandbookBackpackSlot_1");
        Assert.IsNotNull(backpackSlot);

        EventSystem eventSystem = EventSystem.current ?? new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        PointerEventData eventData = new PointerEventData(eventSystem)
        {
            pointerDrag = backpackSlot.gameObject
        };

        dropHandler.OnDrop(eventData);

        Assert.IsTrue(progressState.IsSlotUnlocked(CatalogueBuildingId.Building1, 0));
        Assert.AreEqual(0, backpack.GetSpecialStructureMaterialCount());
        Assert.AreEqual(Color.white, visibleIcon.color);
    }

    [Test]
    public void SceneAuthoredFirstProprietaryButtonAcceptsDrop()
    {
        RuntimeProgressState progressState = RuntimeProgressState.EnsureInstance();
        progressState.ResetProgress(false);

        BackpackMananger backpack = CreateRuntimeBackpack();
        Assert.IsTrue(backpack.PickItem(ArchitecturalCrystalFactory.CreateSpecialStructureMaterial()));

        rootObject = CreateSceneAuthoredRoot();
        GameObject handbookPage = rootObject.transform.Find("IllustratedHandbookCanvas").gameObject;
        CreateSceneAuthoredHandbookSurfaceWithNestedProprietarySlots(handbookPage);

        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        IllustratedHandbookTabsController controller = IllustratedHandbookTabsController.EnsureInstalled(manager);
        controller.SwitchToPage(IllustratedHandbookPage.IllustratedHandbook);

        Transform firstSlotRoot = FindDescendant(handbookPage.transform, "Material_1");
        Button firstSlotButton = FindDescendant(firstSlotRoot, "Button_1").GetComponent<Button>();
        IDropHandler dropHandler = ResolveDropHandler(firstSlotButton.transform);
        Assert.IsNotNull(dropHandler);

        Transform backpackSlotIcon = FindDescendant(
            FindDescendant(handbookPage.transform, "HandbookBackpackSlot_1"),
            "ItemIcon");
        EventSystem eventSystem = EventSystem.current ?? new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        PointerEventData eventData = new PointerEventData(eventSystem)
        {
            pointerDrag = backpackSlotIcon.gameObject
        };

        dropHandler.OnDrop(eventData);

        Assert.IsTrue(progressState.IsSlotUnlocked(CatalogueBuildingId.Building1, 0));
        Assert.IsFalse(backpack.GetItem(0).HasValue);
        Assert.AreEqual(0, backpack.GetSpecialStructureMaterialCount());
    }

    [Test]
    public void SceneAuthoredFirstProprietaryVisibleImageDoesNotBlockDrop()
    {
        RuntimeProgressState.EnsureInstance().ResetProgress(false);

        rootObject = CreateSceneAuthoredRoot();
        GameObject handbookPage = rootObject.transform.Find("IllustratedHandbookCanvas").gameObject;
        CreateSceneAuthoredHandbookSurfaceWithNestedProprietarySlots(handbookPage);

        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        IllustratedHandbookTabsController controller = IllustratedHandbookTabsController.EnsureInstalled(manager);
        controller.SwitchToPage(IllustratedHandbookPage.IllustratedHandbook);

        Transform firstSlotRoot = FindDescendant(handbookPage.transform, "Material_1");
        Transform firstSlotButton = FindDescendant(firstSlotRoot, "Button_1");
        Transform visibleImage = FindDescendant(firstSlotRoot, "Image");
        Image rootImage = firstSlotRoot.GetComponent<Image>();
        Image buttonImage = firstSlotButton.GetComponent<Image>();
        Image visibleIcon = visibleImage.GetComponent<Image>();

        Assert.IsTrue(rootImage.raycastTarget);
        Assert.IsTrue(buttonImage.raycastTarget);
        Assert.IsFalse(visibleIcon.raycastTarget);
        AssertHasDropHandler(firstSlotRoot);
        AssertHasDropHandler(firstSlotButton);
        AssertHasDropHandler(visibleImage);
    }

    [Test]
    public void ClickingFirstProprietaryButtonConsumesFirstSpecialBackpackItem()
    {
        RuntimeProgressState progressState = RuntimeProgressState.EnsureInstance();
        progressState.ResetProgress(false);

        BackpackMananger backpack = CreateRuntimeBackpack();
        Assert.IsTrue(backpack.PickItem(ArchitecturalCrystalFactory.CreateSpecialStructureMaterial()));

        rootObject = CreateSceneAuthoredRoot();
        GameObject handbookPage = rootObject.transform.Find("IllustratedHandbookCanvas").gameObject;
        CreateSceneAuthoredHandbookSurfaceWithNestedProprietarySlots(handbookPage);

        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        IllustratedHandbookTabsController controller = IllustratedHandbookTabsController.EnsureInstalled(manager);
        controller.SwitchToPage(IllustratedHandbookPage.IllustratedHandbook);

        Transform firstSlotRoot = FindDescendant(handbookPage.transform, "Material_1");
        Button firstSlotButton = FindDescendant(firstSlotRoot, "Button_1").GetComponent<Button>();
        AssertHasClickHandler(firstSlotButton.transform);

        EventSystem eventSystem = EventSystem.current ?? new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        ExecuteEvents.ExecuteHierarchy(
            firstSlotButton.gameObject,
            new PointerEventData(eventSystem) { button = PointerEventData.InputButton.Left },
            ExecuteEvents.pointerClickHandler);

        Assert.IsTrue(progressState.IsSlotUnlocked(CatalogueBuildingId.Building1, 0));
        Assert.IsFalse(backpack.GetItem(0).HasValue);
        Assert.AreEqual(0, backpack.GetSpecialStructureMaterialCount());
    }

    [Test]
    public void ClickingUnlockedProprietaryButtonDoesNotConsumeSpecialBackpackItem()
    {
        RuntimeProgressState progressState = RuntimeProgressState.EnsureInstance();
        progressState.ResetProgress(false);
        Assert.IsTrue(progressState.TryUnlockSlot(CatalogueBuildingId.Building1, 0, out _, out _));

        BackpackMananger backpack = CreateRuntimeBackpack();
        Assert.IsTrue(backpack.PickItem(ArchitecturalCrystalFactory.CreateSpecialStructureMaterial()));

        rootObject = CreateSceneAuthoredRoot();
        GameObject handbookPage = rootObject.transform.Find("IllustratedHandbookCanvas").gameObject;
        CreateSceneAuthoredHandbookSurfaceWithNestedProprietarySlots(handbookPage);

        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        IllustratedHandbookTabsController controller = IllustratedHandbookTabsController.EnsureInstalled(manager);
        controller.SwitchToPage(IllustratedHandbookPage.IllustratedHandbook);

        Transform firstSlotRoot = FindDescendant(handbookPage.transform, "Material_1");
        Button firstSlotButton = FindDescendant(firstSlotRoot, "Button_1").GetComponent<Button>();
        AssertHasClickHandler(firstSlotButton.transform);

        EventSystem eventSystem = EventSystem.current ?? new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        ExecuteEvents.ExecuteHierarchy(
            firstSlotButton.gameObject,
            new PointerEventData(eventSystem) { button = PointerEventData.InputButton.Left },
            ExecuteEvents.pointerClickHandler);

        Assert.IsTrue(progressState.IsSlotUnlocked(CatalogueBuildingId.Building1, 0));
        Assert.IsTrue(backpack.GetItem(0).HasValue);
        Assert.AreEqual(1, backpack.GetSpecialStructureMaterialCount());
    }

    [Test]
    public void DroppingRuntimeBackpackIconOntoFirstProprietaryButtonConsumesItem()
    {
        RuntimeProgressState progressState = RuntimeProgressState.EnsureInstance();
        progressState.ResetProgress(false);

        BackpackMananger backpack = CreateRuntimeBackpack();
        Assert.IsTrue(backpack.PickItem(ArchitecturalCrystalFactory.CreateSpecialStructureMaterial()));

        rootObject = CreateSceneAuthoredRoot();
        GameObject handbookPage = rootObject.transform.Find("IllustratedHandbookCanvas").gameObject;
        CreateSceneAuthoredHandbookSurfaceWithNestedProprietarySlots(handbookPage);

        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        IllustratedHandbookTabsController controller = IllustratedHandbookTabsController.EnsureInstalled(manager);
        controller.SwitchToPage(IllustratedHandbookPage.IllustratedHandbook);

        GameObject runtimeSlotObject = new GameObject("RuntimeBackpackSlot_1", typeof(RectTransform), typeof(Image), typeof(BackpackSlot));
        runtimeSlotObject.transform.SetParent(rootObject.transform, false);
        runtimeSlotObject.GetComponent<BackpackSlot>().slotIndex = 0;
        Image runtimeSlotIcon = CreateChild<Image>(runtimeSlotObject.transform, "ItemIcon");

        Transform firstSlotRoot = FindDescendant(handbookPage.transform, "Material_1");
        Button firstSlotButton = FindDescendant(firstSlotRoot, "Button_1").GetComponent<Button>();
        IDropHandler dropHandler = ResolveDropHandler(firstSlotButton.transform);
        Assert.IsNotNull(dropHandler);

        EventSystem eventSystem = EventSystem.current ?? new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        PointerEventData eventData = new PointerEventData(eventSystem)
        {
            pointerDrag = runtimeSlotIcon.gameObject
        };

        dropHandler.OnDrop(eventData);

        Assert.IsTrue(progressState.IsSlotUnlocked(CatalogueBuildingId.Building1, 0));
        Assert.IsFalse(backpack.GetItem(0).HasValue);
        Assert.AreEqual(0, backpack.GetSpecialStructureMaterialCount());
    }

    [Test]
    public void DroppingSceneHandbookDragGhostOntoFirstProprietaryButtonConsumesItem()
    {
        RuntimeProgressState progressState = RuntimeProgressState.EnsureInstance();
        progressState.ResetProgress(false);

        BackpackMananger backpack = CreateRuntimeBackpack();
        Assert.IsTrue(backpack.PickItem(ArchitecturalCrystalFactory.CreateSpecialStructureMaterial()));

        rootObject = CreateSceneAuthoredRoot();
        rootObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        GameObject handbookPage = rootObject.transform.Find("IllustratedHandbookCanvas").gameObject;
        CreateSceneAuthoredHandbookSurfaceWithNestedProprietarySlots(handbookPage);

        UIManager manager = rootObject.AddComponent<UIManager>();
        manager.illustratedHandbook = rootObject;
        IllustratedHandbookTabsController controller = IllustratedHandbookTabsController.EnsureInstalled(manager);
        controller.SwitchToPage(IllustratedHandbookPage.IllustratedHandbook);

        Transform backpackSlot = FindDescendant(handbookPage.transform, "HandbookBackpackSlot_1");
        IBeginDragHandler beginDragHandler = backpackSlot.GetComponents<MonoBehaviour>().OfType<IBeginDragHandler>().FirstOrDefault();
        Assert.IsNotNull(beginDragHandler);

        EventSystem eventSystem = EventSystem.current ?? new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        beginDragHandler.OnBeginDrag(new PointerEventData(eventSystem)
        {
            position = new Vector2(10f, 10f)
        });

        GameObject dragGhost = GameObject.Find("HandbookDragGhost");
        Assert.IsNotNull(dragGhost);

        Transform firstSlotRoot = FindDescendant(handbookPage.transform, "Material_1");
        Button firstSlotButton = FindDescendant(firstSlotRoot, "Button_1").GetComponent<Button>();
        IDropHandler dropHandler = ResolveDropHandler(firstSlotButton.transform);
        Assert.IsNotNull(dropHandler);

        PointerEventData eventData = new PointerEventData(eventSystem)
        {
            pointerDrag = dragGhost
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

    private static void CompleteBuildingUnlock(RuntimeProgressState progressState, CatalogueBuildingId buildingId)
    {
        BuildingDefinition definition = BuildingDefinitionLibrary.Get(buildingId);
        Assert.IsTrue(progressState.AddBuildingProgress(buildingId, definition.requiredProgress, out _));
        int slotCount = definition.slotDefinitions != null ? definition.slotDefinitions.Length : 0;
        for (int i = 0; i < slotCount; i++)
        {
            if (!progressState.IsSlotUnlocked(buildingId, i))
            {
                Assert.IsTrue(progressState.TryUnlockSlot(buildingId, i, out _, out _));
            }
        }

        Assert.IsTrue(progressState.TryUnlockBuilding(buildingId, out _));
    }

    private static void CreateSceneAuthoredHandbookSurface(GameObject handbookPage)
    {
        GameObject leftPanel = new GameObject("LeftPanel", typeof(RectTransform));
        leftPanel.transform.SetParent(handbookPage.transform, false);

        GameObject card = new GameObject("ArcitectureImage_1", typeof(RectTransform), typeof(Image), typeof(Button));
        card.transform.SetParent(leftPanel.transform, false);
        CreateTmpText("Name", card.transform, "福建土楼");
        CreateChild<Image>(card.transform, "Picture");
        CreateInteractiveSlider(card.transform, "Slider");
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
        CreateInteractiveSlider(general.transform, "GeneralProgressSlider");
        GameObject button = new GameObject("SubmitCommonMaterialButton", typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(general.transform, false);
        CreateTmpText("Label", button.transform, "提交通用材料");
    }

    private static void CreateSceneAuthoredHandbookSurfaceWithNestedProprietarySlots(GameObject handbookPage)
    {
        CreateSceneAuthoredHandbookSurface(handbookPage);

        Transform rightIntroduction = FindDescendant(handbookPage.transform, "RightIntroduction");
        Transform oldProprietary = FindDescendant(rightIntroduction, "ProprietaryMaterial");
        Object.DestroyImmediate(oldProprietary.gameObject);

        GameObject proprietary = new GameObject("ProprietaryMaterial", typeof(RectTransform));
        proprietary.transform.SetParent(rightIntroduction, false);
        CreateTmpText("Label", proprietary.transform, "专用进度（0/3）");

        for (int i = 0; i < 3; i++)
        {
            Transform slotRoot = CreateNestedProprietarySlot(proprietary.transform, i + 1);
            RectTransform slotRect = slotRoot as RectTransform;
            slotRect.anchoredPosition = new Vector2(i * 64f, 0f);
            slotRect.sizeDelta = new Vector2(35f, 35f);
        }
    }

    private static Transform CreateNestedProprietarySlot(Transform parent, int slotNumber)
    {
        GameObject slotObject = new GameObject($"Material_{slotNumber}", typeof(RectTransform), typeof(Image));
        slotObject.transform.SetParent(parent, false);
        Image slotImage = slotObject.GetComponent<Image>();
        slotImage.raycastTarget = slotNumber != 1;

        GameObject buttonObject = new GameObject($"Button_{slotNumber}", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(slotObject.transform, false);
        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(1f, 1f, 1f, 0f);
        buttonImage.raycastTarget = true;

        GameObject iconObject = new GameObject("Image", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(slotObject.transform, false);
        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.raycastTarget = false;
        iconImage.color = Color.white;
        (iconObject.transform as RectTransform).sizeDelta = new Vector2(35f, 35f);

        return slotObject.transform;
    }

    private static void CreateSceneAuthoredPersonalAttributeSurface(GameObject personalPage)
    {
        GameObject attributesRoot = new GameObject("人物属性", typeof(RectTransform));
        attributesRoot.transform.SetParent(personalPage.transform, false);

        CreateInteractiveSlider(attributesRoot.transform, "生命值");
        CreateInteractiveSlider(attributesRoot.transform, "攻击力");
        CreateInteractiveSlider(attributesRoot.transform, "移速");
        CreateInteractiveSlider(attributesRoot.transform, "防御");
    }

    private static void CreateSceneAuthoredPersonalBackpackSurface(GameObject personalPage)
    {
        GameObject backpackRoot = new GameObject("Backpack", typeof(RectTransform));
        backpackRoot.transform.SetParent(personalPage.transform, false);

        for (int i = 0; i < 6; i++)
        {
            GameObject slotObject = new GameObject($"Slot_{i + 1}", typeof(RectTransform), typeof(Image));
            slotObject.transform.SetParent(backpackRoot.transform, false);
            Image slotImage = slotObject.GetComponent<Image>();
            slotImage.color = new Color(1f, 1f, 1f, 0.73f);
            slotImage.raycastTarget = true;

            GameObject selectionObject = new GameObject("Image", typeof(RectTransform), typeof(Image));
            selectionObject.transform.SetParent(slotObject.transform, false);
            selectionObject.SetActive(true);
        }
    }

    private static void CreateSceneAuthoredPersonalInkSurface(GameObject personalPage)
    {
        GameObject weaponRoot = new GameObject("Weapon", typeof(RectTransform), typeof(Image));
        weaponRoot.transform.SetParent(personalPage.transform, false);

        CreatePersonalInkOption(weaponRoot.transform, "Image_3", -80f);
        CreatePersonalInkOption(weaponRoot.transform, "Image_1", -40f);
        CreatePersonalInkOption(weaponRoot.transform, "Image_2", 0f);
        CreatePersonalInkOption(weaponRoot.transform, "Image_4", 40f);

        GameObject descriptionPanel = new GameObject("Image", typeof(RectTransform), typeof(Image));
        descriptionPanel.transform.SetParent(weaponRoot.transform, false);
        CreateTmpText("Description", descriptionPanel.transform, string.Empty);
    }

    private static void CreatePersonalInkOption(Transform parent, string name, float x)
    {
        GameObject optionObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        optionObject.transform.SetParent(parent, false);
        RectTransform optionRect = optionObject.transform as RectTransform;
        optionRect.anchoredPosition = new Vector2(x, 0f);

        CreateChild<Image>(optionObject.transform, "Circle");
        CreateChild<Image>(optionObject.transform, "Designation");
        CreateChild<Image>(optionObject.transform, "Used");
        CreateChild<Button>(optionObject.transform, "Button");
    }

    private static void AssertPersonalInkSelected(Transform option, bool selected)
    {
        Assert.IsNotNull(option);
        Assert.AreEqual(selected, option.Find("Circle").gameObject.activeSelf, option.name);
        Assert.AreEqual(selected, option.Find("Designation").gameObject.activeSelf, option.name);
        Assert.AreEqual(selected, option.Find("Used").gameObject.activeSelf, option.name);
    }

    private static Slider CreateInteractiveSlider(Transform parent, string name)
    {
        GameObject sliderObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);
        Image background = sliderObject.GetComponent<Image>();
        background.raycastTarget = true;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(fillArea.transform, false);
        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.raycastTarget = true;

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.targetGraphic = background;
        slider.fillRect = fillObject.transform as RectTransform;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 80f;
        slider.interactable = true;
        slider.transition = Selectable.Transition.ColorTint;
        slider.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        return slider;
    }

    private static void AssertSliderIsReadOnlyDisplay(Slider slider)
    {
        Assert.IsFalse(slider.interactable, slider.name);
        Assert.AreEqual(Selectable.Transition.None, slider.transition, slider.name);
        Assert.AreEqual(Navigation.Mode.None, slider.navigation.mode, slider.name);
        Assert.IsFalse(slider.targetGraphic != null && slider.targetGraphic.raycastTarget, slider.name);
        Assert.IsTrue(slider.GetComponentsInChildren<Graphic>(true).All(graphic => !graphic.raycastTarget), slider.name);
    }

    private static string GetSlotSpriteName(Transform root, string slotName)
    {
        Transform slot = FindDescendant(root, slotName);
        Assert.IsNotNull(slot, slotName);

        Image image = ResolveSlotContentImage(slot);
        Assert.IsNotNull(image, slotName);
        Assert.IsNotNull(image.sprite, slotName);
        return image.sprite.name;
    }

    private static Image ResolveSlotContentImage(Transform slot)
    {
        Transform content = slot.Find("Image");
        Image contentImage = content != null ? content.GetComponent<Image>() : null;
        return contentImage != null ? contentImage : slot.GetComponent<Image>();
    }

    private static void AssertHasDropHandler(Transform target)
    {
        Assert.IsNotNull(ResolveDropHandler(target), target != null ? target.name : "null");
    }

    private static void AssertHasClickHandler(Transform target)
    {
        Assert.IsNotNull(ResolveClickHandler(target), target != null ? target.name : "null");
    }

    private static IDropHandler ResolveDropHandler(Transform target)
    {
        return target != null
            ? target.GetComponents<MonoBehaviour>().OfType<IDropHandler>().FirstOrDefault()
            : null;
    }

    private static IPointerClickHandler ResolveClickHandler(Transform target)
    {
        return target != null
            ? target.GetComponents<MonoBehaviour>().OfType<IPointerClickHandler>().FirstOrDefault(handler => !(handler is Button))
            : null;
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
