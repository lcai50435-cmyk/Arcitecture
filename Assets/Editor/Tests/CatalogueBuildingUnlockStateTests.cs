using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class CatalogueBuildingUnlockStateTests
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

        if (RuntimeProgressState.Instance != null)
        {
            Object.DestroyImmediate(RuntimeProgressState.Instance.gameObject);
        }

        if (BackpackMananger.Instance != null)
        {
            Object.DestroyImmediate(BackpackMananger.Instance.gameObject);
        }

        if (EventSystem.current != null && EventSystem.current.gameObject.name == "EventSystem")
        {
            Object.DestroyImmediate(EventSystem.current.gameObject);
        }

        DestroyRuntimeDialogs();
    }

    [Test]
    public void BuildingUnlockRequiresCommonProgressCapAndAllSpecialSlots()
    {
        RuntimeProgressState state = RuntimeProgressState.EnsureInstance();
        state.ResetProgress(false);

        BuildingDefinition definition = BuildingDefinitionLibrary.Get(CatalogueBuildingId.Building1);

        Assert.IsTrue(state.AddBuildingProgress(
            CatalogueBuildingId.Building1,
            definition.requiredProgress,
            out BuildingRewardDefinition progressReward));
        Assert.IsNull(progressReward);
        Assert.AreEqual(70, state.GetBuildingProgress(CatalogueBuildingId.Building1));
        Assert.IsFalse(state.CanUnlockBuilding(CatalogueBuildingId.Building1));

        for (int i = 0; i < definition.slotDefinitions.Length; i++)
        {
            Assert.IsTrue(state.TryUnlockSlot(
                CatalogueBuildingId.Building1,
                i,
                out BuildingRewardDefinition slotReward,
                out BuildingRewardDefinition completionReward));
            Assert.IsNotNull(slotReward);
            Assert.IsNull(completionReward);
        }

        Assert.AreEqual(definition.requiredProgress, state.GetBuildingProgress(CatalogueBuildingId.Building1));
        Assert.IsTrue(state.CanUnlockBuilding(CatalogueBuildingId.Building1));
    }

    [Test]
    public void BuildingUnlockRequiresExplicitProgressFullClick()
    {
        RuntimeProgressState state = RuntimeProgressState.EnsureInstance();
        state.ResetProgress(false);

        Assert.IsFalse(state.IsBuildingUnlocked(CatalogueBuildingId.Building1));
        Assert.IsFalse(state.CanUnlockBuilding(CatalogueBuildingId.Building1));

        Assert.IsTrue(state.AddBuildingProgress(
            CatalogueBuildingId.Building1,
            BuildingDefinitionLibrary.Get(CatalogueBuildingId.Building1).requiredProgress,
            out BuildingRewardDefinition progressReward));
        Assert.IsNull(progressReward);
        Assert.IsFalse(state.IsBuildingUnlocked(CatalogueBuildingId.Building1));
        Assert.IsFalse(state.CanUnlockBuilding(CatalogueBuildingId.Building1));

        CompleteBuildingUnlockPrerequisites(state, CatalogueBuildingId.Building1);
        Assert.IsTrue(state.CanUnlockBuilding(CatalogueBuildingId.Building1));

        Assert.IsTrue(state.TryUnlockBuilding(
            CatalogueBuildingId.Building1,
            out BuildingRewardDefinition completionReward));
        Assert.IsTrue(state.IsBuildingUnlocked(CatalogueBuildingId.Building1));
        Assert.AreSame(
            BuildingDefinitionLibrary.Get(CatalogueBuildingId.Building1).completionReward,
            completionReward);
        Assert.IsFalse(state.CanUnlockBuilding(CatalogueBuildingId.Building1));
    }

    [Test]
    public void LockedAndUnlockedVisualsAreMutuallyExclusive()
    {
        RuntimeProgressState state = RuntimeProgressState.EnsureInstance();
        state.ResetProgress(false);

        GameObject cardObject = new GameObject("Card", typeof(RectTransform));
        roots.Add(cardObject);

        Slider slider = CreateChild<Slider>(cardObject.transform, "Slider");
        GameObject lockedVisual = CreateChild<Image>(cardObject.transform, "Lock").gameObject;
        GameObject unlockedVisual = CreateChild<Image>(cardObject.transform, "Unlock").gameObject;

        CatalogueBuildingUnlockState unlockState = cardObject.AddComponent<CatalogueBuildingUnlockState>();
        unlockState.buildingId = CatalogueBuildingId.Building1;
        unlockState.buildingSlider = slider;
        unlockState.lockedBuildingVisual = lockedVisual;
        unlockState.unlockedBuildingVisual = unlockedVisual;
        unlockState.RefreshState();

        Button lockedButton = lockedVisual.GetComponent<Button>();
        Assert.IsTrue(lockedVisual.activeSelf);
        Assert.IsFalse(unlockedVisual.activeSelf);
        Assert.IsNotNull(lockedButton);
        Assert.IsFalse(lockedButton.interactable);

        state.AddBuildingProgress(
            CatalogueBuildingId.Building1,
            BuildingDefinitionLibrary.Get(CatalogueBuildingId.Building1).requiredProgress,
            out _);
        unlockState.RefreshState();

        Assert.IsTrue(lockedVisual.activeSelf);
        Assert.IsFalse(unlockedVisual.activeSelf);
        Assert.IsFalse(lockedButton.interactable);

        CompleteBuildingUnlockPrerequisites(state, CatalogueBuildingId.Building1);
        unlockState.RefreshState();

        Assert.IsTrue(lockedVisual.activeSelf);
        Assert.IsFalse(unlockedVisual.activeSelf);
        Assert.IsTrue(lockedButton.interactable);

        lockedButton.onClick.Invoke();

        Assert.IsTrue(state.IsBuildingUnlocked(CatalogueBuildingId.Building1));
        Assert.IsFalse(lockedVisual.activeSelf);
        Assert.IsTrue(unlockedVisual.activeSelf);
    }

    [Test]
    public void UnlockCompletionShowsFocusedTopmostIntroductionDialogWithoutSwitchingPages()
    {
        RuntimeProgressState state = RuntimeProgressState.EnsureInstance();
        state.ResetProgress(false);

        GameObject handbookPanel = new GameObject("HandbookPanel");
        GameObject detailPanel = new GameObject("DetailPanel");
        roots.Add(handbookPanel);
        roots.Add(detailPanel);
        Text detailTitle = CreateChild<Text>(detailPanel.transform, "Title");
        DetailedInformationUI detailUi = detailPanel.AddComponent<DetailedInformationUI>();
        detailUi.detailedInformationPanel = detailPanel;
        detailUi.page1NameText = detailTitle;
        handbookPanel.SetActive(true);
        detailPanel.SetActive(false);

        GameObject cardObject = new GameObject("Card", typeof(RectTransform));
        roots.Add(cardObject);

        Slider slider = CreateChild<Slider>(cardObject.transform, "Slider");
        GameObject lockedVisual = CreateChild<Image>(cardObject.transform, "Lock").gameObject;
        GameObject unlockedVisual = CreateChild<Image>(cardObject.transform, "Unlock").gameObject;
        BuildingDetailData detailData = CreateChild<BuildingDetailData>(cardObject.transform, "DetailData");
        detailData.buildingName = "测试建筑";
        detailData.introduction1 = "这是解锁后的建筑介绍。";

        CatalogueBuildingUnlockState unlockState = cardObject.AddComponent<CatalogueBuildingUnlockState>();
        unlockState.buildingId = CatalogueBuildingId.Building1;
        unlockState.buildingSlider = slider;
        unlockState.lockedBuildingVisual = lockedVisual;
        unlockState.unlockedBuildingVisual = unlockedVisual;

        state.AddBuildingProgress(
            CatalogueBuildingId.Building1,
            BuildingDefinitionLibrary.Get(CatalogueBuildingId.Building1).requiredProgress,
            out _);
        CompleteBuildingUnlockPrerequisites(state, CatalogueBuildingId.Building1);
        unlockState.RefreshState();

        Button lockedButton = lockedVisual.GetComponent<Button>();
        Assert.IsNotNull(lockedButton);
        Assert.IsTrue(lockedButton.interactable);

        lockedButton.onClick.Invoke();

        Dialog dialog = FindRuntimeDialog();
        Assert.IsTrue(state.IsBuildingUnlocked(CatalogueBuildingId.Building1));
        Assert.IsNotNull(dialog);
        Assert.IsNotNull(dialog.dialogPanel);
        Assert.IsTrue(dialog.dialogPanel.activeSelf);
        Assert.IsTrue(dialog.clickCloseButton.gameObject.activeSelf);
        Assert.That(ReadActiveDialogContent(dialog), Does.Contain("测试建筑"));
        Assert.That(ReadActiveDialogContent(dialog), Does.Contain("这是解锁后的建筑介绍。"));
        Assert.IsTrue(handbookPanel.activeSelf);
        Assert.IsFalse(detailPanel.activeSelf);

        dialog.EnsureTopmostRuntimePanelInputSurface();
        Canvas canvas = dialog.dialogPanel.GetComponent<Canvas>();
        Assert.IsNotNull(canvas);
        Assert.Greater(canvas.sortingOrder, RuntimeModalStyle.ModalSortingOrder);
    }

    [Test]
    public void UnlockedBuildingVisualClickKeepsDetailNavigationSeparateFromIntroductionDialog()
    {
        RuntimeProgressState state = RuntimeProgressState.EnsureInstance();
        state.ResetProgress(false);

        GameObject handbookPanel = new GameObject("HandbookPanel");
        GameObject detailPanel = new GameObject("DetailPanel");
        roots.Add(handbookPanel);
        roots.Add(detailPanel);
        Text detailTitle = CreateChild<Text>(detailPanel.transform, "Title");
        DetailedInformationUI detailUi = detailPanel.AddComponent<DetailedInformationUI>();
        detailUi.detailedInformationPanel = detailPanel;
        detailUi.page1NameText = detailTitle;
        handbookPanel.SetActive(true);
        detailPanel.SetActive(false);

        GameObject cardObject = new GameObject("Card", typeof(RectTransform));
        roots.Add(cardObject);

        Slider slider = CreateChild<Slider>(cardObject.transform, "Slider");
        GameObject lockedVisual = CreateChild<Image>(cardObject.transform, "Lock").gameObject;
        GameObject unlockedVisual = CreateChild<Image>(cardObject.transform, "Unlock").gameObject;
        unlockedVisual.AddComponent<Button>();
        BuildingDetailData detailData = CreateChild<BuildingDetailData>(cardObject.transform, "DetailData");
        detailData.buildingName = "测试建筑";

        CatalogueBuildingUnlockState unlockState = cardObject.AddComponent<CatalogueBuildingUnlockState>();
        unlockState.buildingId = CatalogueBuildingId.Building1;
        unlockState.buildingSlider = slider;
        unlockState.lockedBuildingVisual = lockedVisual;
        unlockState.unlockedBuildingVisual = unlockedVisual;

        UnlockedBuildingImageButton imageButton = unlockedVisual.AddComponent<UnlockedBuildingImageButton>();
        imageButton.buildingUnlockState = unlockState;
        imageButton.illustratedHandbookPanel = handbookPanel;
        imageButton.detailedInformationPanel = detailPanel;

        state.AddBuildingProgress(
            CatalogueBuildingId.Building1,
            BuildingDefinitionLibrary.Get(CatalogueBuildingId.Building1).requiredProgress,
            out _);
        CompleteBuildingUnlockPrerequisites(state, CatalogueBuildingId.Building1);
        Assert.IsTrue(state.TryUnlockBuilding(CatalogueBuildingId.Building1, out _));
        unlockState.RefreshState();

        InvokePrivate(imageButton, "Awake");
        InvokePrivate(imageButton, "Start");

        Button unlockedButton = unlockedVisual.GetComponent<Button>();
        Assert.IsNotNull(unlockedButton);
        Assert.IsTrue(unlockedButton.interactable);

        unlockedButton.onClick.Invoke();

        Assert.IsFalse(handbookPanel.activeSelf);
        Assert.IsTrue(detailPanel.activeSelf);
        Assert.AreEqual("测试建筑", detailTitle.text);
        Assert.IsNull(FindRuntimeDialog());
    }

    [Test]
    public void DetailOpenButtonResolvesSceneBindingsAndShowsUnlockedBuildingDetail()
    {
        RuntimeProgressState state = RuntimeProgressState.EnsureInstance();
        state.ResetProgress(false);

        GameObject detailPanel = new GameObject("DetailedInformationCanvas");
        roots.Add(detailPanel);
        Text detailTitle = CreateChild<Text>(detailPanel.transform, "Title");
        DetailedInformationUI detailUi = detailPanel.AddComponent<DetailedInformationUI>();
        detailUi.detailedInformationPanel = detailPanel;
        detailUi.page1NameText = detailTitle;
        detailPanel.SetActive(false);

        GameObject cardObject = new GameObject("Card", typeof(RectTransform));
        roots.Add(cardObject);
        Slider slider = CreateChild<Slider>(cardObject.transform, "Slider");
        BuildingDetailData detailData = CreateChild<BuildingDetailData>(cardObject.transform, "DetailData");
        detailData.buildingName = "测试建筑";

        CatalogueBuildingUnlockState unlockState = cardObject.AddComponent<CatalogueBuildingUnlockState>();
        unlockState.buildingId = CatalogueBuildingId.Building1;
        unlockState.buildingSlider = slider;

        GameObject buttonObject = new GameObject("GotoButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(cardObject.transform, false);
        Button button = buttonObject.GetComponent<Button>();
        BuildingDetailOpenButton openButton = buttonObject.AddComponent<BuildingDetailOpenButton>();

        state.AddBuildingProgress(
            CatalogueBuildingId.Building1,
            BuildingDefinitionLibrary.Get(CatalogueBuildingId.Building1).requiredProgress,
            out _);
        CompleteBuildingUnlockPrerequisites(state, CatalogueBuildingId.Building1);
        Assert.IsTrue(state.TryUnlockBuilding(CatalogueBuildingId.Building1, out _));
        unlockState.RefreshState();

        InvokePrivate(openButton, "Awake");
        InvokePrivate(openButton, "Start");

        Assert.IsTrue(button.interactable);

        button.onClick.Invoke();

        Assert.IsTrue(detailPanel.activeSelf);
        Assert.AreEqual("测试建筑", detailTitle.text);
    }

    [Test]
    public void MixedDetailButtonsPreferFujianSceneAuthoredDetailCanvasOverGenericBindings()
    {
        RuntimeProgressState state = RuntimeProgressState.EnsureInstance();
        state.ResetProgress(false);

        DetailedInformationUI genericUi = CreateDetailPanel("DetailedInformationCanvas", out GameObject genericPanel, out _);
        DetailedInformationUI fujianUi = CreateDetailPanel("DetailInformationFuJianCanvas", out GameObject fujianPanel, out _);
        CreateDetailPanel("DetailInformationShuiXiangCanvas", out GameObject shuiXiangPanel, out _);
        genericPanel.SetActive(false);
        fujianPanel.SetActive(false);
        shuiXiangPanel.SetActive(true);

        GameObject cardObject = CreateUnlockedCard(CatalogueBuildingId.Building1, state, out CatalogueBuildingUnlockState unlockState);
        BuildingDetailData detailData = CreateChild<BuildingDetailData>(cardObject.transform, "DetailData");
        detailData.buildingName = "福建土楼测试";

        GameObject buttonObject = new GameObject("GotoButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(cardObject.transform, false);
        Button button = buttonObject.GetComponent<Button>();

        BuildingDetailOpenButton openButton = buttonObject.AddComponent<BuildingDetailOpenButton>();
        openButton.buildingUnlockState = unlockState;
        openButton.buildingDetailData = detailData;
        openButton.detailedInformationUI = genericUi;

        UnlockedBuildingImageButton imageButton = buttonObject.AddComponent<UnlockedBuildingImageButton>();
        imageButton.buildingUnlockState = unlockState;
        imageButton.buildingDetailData = detailData;
        imageButton.detailedInformationPanel = genericPanel;
        imageButton.detailedInformationUI = genericUi;

        InvokePrivate(openButton, "Awake");
        InvokePrivate(imageButton, "Awake");
        InvokePrivate(openButton, "Start");
        InvokePrivate(imageButton, "Start");

        Assert.IsTrue(button.interactable);
        button.onClick.Invoke();

        Assert.IsTrue(fujianPanel.activeSelf);
        Assert.IsFalse(genericPanel.activeSelf);
        Assert.IsFalse(shuiXiangPanel.activeSelf);
        Assert.AreSame(fujianUi, openButton.detailedInformationUI);
        Assert.AreSame(fujianUi, imageButton.detailedInformationUI);
    }

    [Test]
    public void DetailOpenButtonPrefersShuiXiangSceneAuthoredDetailCanvas()
    {
        RuntimeProgressState state = RuntimeProgressState.EnsureInstance();
        state.ResetProgress(false);

        DetailedInformationUI genericUi = CreateDetailPanel("DetailedInformationCanvas", out GameObject genericPanel, out _);
        CreateDetailPanel("DetailInformationFuJianCanvas", out GameObject fujianPanel, out _);
        CreateDetailPanel("DetailInformationShuiXiangCanvas", out GameObject shuiXiangPanel, out _);
        genericPanel.SetActive(false);
        fujianPanel.SetActive(true);
        shuiXiangPanel.SetActive(false);

        GameObject cardObject = CreateUnlockedCard(CatalogueBuildingId.Building3, state, out CatalogueBuildingUnlockState unlockState);
        BuildingDetailData detailData = CreateChild<BuildingDetailData>(cardObject.transform, "DetailData");
        detailData.buildingName = "苏浙水乡测试";

        GameObject buttonObject = new GameObject("GotoButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(cardObject.transform, false);
        Button button = buttonObject.GetComponent<Button>();
        BuildingDetailOpenButton openButton = buttonObject.AddComponent<BuildingDetailOpenButton>();
        openButton.buildingUnlockState = unlockState;
        openButton.buildingDetailData = detailData;
        openButton.detailedInformationUI = genericUi;

        InvokePrivate(openButton, "Awake");
        InvokePrivate(openButton, "Start");

        Assert.IsTrue(button.interactable);
        button.onClick.Invoke();

        Assert.IsTrue(shuiXiangPanel.activeSelf);
        Assert.IsFalse(genericPanel.activeSelf);
        Assert.IsFalse(fujianPanel.activeSelf);
        Assert.AreSame(shuiXiangPanel, openButton.detailedInformationUI.detailedInformationPanel);
    }

    [Test]
    public void DroppingBackpackSpecialStructureOnCatalogueSlotUnlocksSlotAndAddsProgress()
    {
        RuntimeProgressState state = RuntimeProgressState.EnsureInstance();
        state.ResetProgress(false);

        GameObject backpackObject = new GameObject("RuntimeBackpackManager");
        roots.Add(backpackObject);
        BackpackMananger backpack = backpackObject.AddComponent<BackpackMananger>();
        Assert.IsTrue(backpack.PickItem(ArchitecturalCrystalFactory.CreateSpecialStructureMaterial()));

        GameObject cardObject = new GameObject("Card", typeof(RectTransform));
        roots.Add(cardObject);
        Slider slider = CreateChild<Slider>(cardObject.transform, "Slider");

        GameObject slotObject = new GameObject("SpecialSlot", typeof(RectTransform), typeof(Image), typeof(Button));
        slotObject.transform.SetParent(cardObject.transform, false);
        Image slotImage = slotObject.GetComponent<Image>();
        CatalogueUnlockSlotButton slotButton = slotObject.AddComponent<CatalogueUnlockSlotButton>();
        slotButton.targetImage = slotImage;
        slotButton.slotIndex = 0;

        CatalogueBuildingUnlockState unlockState = cardObject.AddComponent<CatalogueBuildingUnlockState>();
        unlockState.buildingId = CatalogueBuildingId.Building1;
        unlockState.buildingSlider = slider;
        unlockState.slotButtons = new[] { slotButton };
        unlockState.RefreshState();

        Assert.Less(slotImage.color.r, 1f);

        GameObject sourceSlotObject = new GameObject("BackpackSlot", typeof(RectTransform), typeof(Image), typeof(BackpackSlot));
        roots.Add(sourceSlotObject);
        sourceSlotObject.GetComponent<BackpackSlot>().slotIndex = 0;

        IDropHandler dropHandler = slotButton as IDropHandler;
        Assert.IsNotNull(dropHandler);

        EventSystem eventSystem = EventSystem.current ?? new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        PointerEventData eventData = new PointerEventData(eventSystem)
        {
            pointerDrag = sourceSlotObject
        };

        int previousProgress = state.GetBuildingProgress(CatalogueBuildingId.Building1);
        dropHandler.OnDrop(eventData);

        Assert.IsTrue(state.IsSlotUnlocked(CatalogueBuildingId.Building1, 0));
        Assert.Greater(state.GetBuildingProgress(CatalogueBuildingId.Building1), previousProgress);
        Assert.AreEqual(0, backpack.GetSpecialStructureMaterialCount());
        Assert.AreEqual(Color.white, slotImage.color);
    }

    [Test]
    public void DroppingFinalBackpackSpecialStructureOnCatalogueSlotUnlocksBuilding()
    {
        RuntimeProgressState state = RuntimeProgressState.EnsureInstance();
        state.ResetProgress(false);
        BuildingDefinition definition = BuildingDefinitionLibrary.Get(CatalogueBuildingId.Building1);

        Assert.IsTrue(state.AddBuildingProgress(
            CatalogueBuildingId.Building1,
            definition.requiredProgress,
            out _));
        for (int i = 1; i < definition.slotDefinitions.Length; i++)
        {
            Assert.IsTrue(state.TryUnlockSlot(
                CatalogueBuildingId.Building1,
                i,
                out _,
                out _));
        }

        Assert.IsFalse(state.IsBuildingUnlocked(CatalogueBuildingId.Building1));

        GameObject backpackObject = new GameObject("RuntimeBackpackManager");
        roots.Add(backpackObject);
        BackpackMananger backpack = backpackObject.AddComponent<BackpackMananger>();
        Assert.IsTrue(backpack.PickItem(ArchitecturalCrystalFactory.CreateSpecialStructureMaterial()));

        GameObject cardObject = new GameObject("Card", typeof(RectTransform));
        roots.Add(cardObject);
        Slider slider = CreateChild<Slider>(cardObject.transform, "Slider");

        GameObject slotObject = new GameObject("SpecialSlot", typeof(RectTransform), typeof(Image), typeof(Button));
        slotObject.transform.SetParent(cardObject.transform, false);
        Image slotImage = slotObject.GetComponent<Image>();
        CatalogueUnlockSlotButton slotButton = slotObject.AddComponent<CatalogueUnlockSlotButton>();
        slotButton.targetImage = slotImage;
        slotButton.slotIndex = 0;

        CatalogueBuildingUnlockState unlockState = cardObject.AddComponent<CatalogueBuildingUnlockState>();
        unlockState.buildingId = CatalogueBuildingId.Building1;
        unlockState.buildingSlider = slider;
        unlockState.slotButtons = new[] { slotButton };
        unlockState.RefreshState();

        GameObject sourceSlotObject = new GameObject("BackpackSlot", typeof(RectTransform), typeof(Image), typeof(BackpackSlot));
        roots.Add(sourceSlotObject);
        sourceSlotObject.GetComponent<BackpackSlot>().slotIndex = 0;

        EventSystem eventSystem = EventSystem.current ?? new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        PointerEventData eventData = new PointerEventData(eventSystem)
        {
            pointerDrag = sourceSlotObject
        };

        ((IDropHandler)slotButton).OnDrop(eventData);

        Assert.IsTrue(state.IsSlotUnlocked(CatalogueBuildingId.Building1, 0));
        Assert.AreEqual(definition.requiredProgress, state.GetBuildingProgress(CatalogueBuildingId.Building1));
        Assert.IsTrue(state.IsBuildingUnlocked(CatalogueBuildingId.Building1));
        Assert.AreEqual(0, backpack.GetSpecialStructureMaterialCount());
        Assert.AreEqual(Color.white, slotImage.color);
    }

    private static T CreateChild<T>(Transform parent, string name)
        where T : Component
    {
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(T));
        child.transform.SetParent(parent, false);
        return child.GetComponent<T>();
    }

    private DetailedInformationUI CreateDetailPanel(
        string panelName,
        out GameObject panel,
        out Text title)
    {
        panel = new GameObject(panelName, typeof(RectTransform));
        roots.Add(panel);
        title = CreateChild<Text>(panel.transform, "Name");
        DetailedInformationUI detailUi = panel.AddComponent<DetailedInformationUI>();
        detailUi.detailedInformationPanel = panel;
        detailUi.page1NameText = title;
        return detailUi;
    }

    private GameObject CreateUnlockedCard(
        CatalogueBuildingId buildingId,
        RuntimeProgressState state,
        out CatalogueBuildingUnlockState unlockState)
    {
        GameObject cardObject = new GameObject("Card", typeof(RectTransform));
        roots.Add(cardObject);

        Slider slider = CreateChild<Slider>(cardObject.transform, "Slider");
        unlockState = cardObject.AddComponent<CatalogueBuildingUnlockState>();
        unlockState.buildingId = buildingId;
        unlockState.buildingSlider = slider;

        state.AddBuildingProgress(
            buildingId,
            BuildingDefinitionLibrary.Get(buildingId).requiredProgress,
            out _);
        CompleteBuildingUnlockPrerequisites(state, buildingId);
        Assert.IsTrue(state.TryUnlockBuilding(buildingId, out _));
        unlockState.RefreshState();
        return cardObject;
    }

    private static void CompleteBuildingUnlockPrerequisites(
        RuntimeProgressState state,
        CatalogueBuildingId buildingId)
    {
        BuildingDefinition definition = BuildingDefinitionLibrary.Get(buildingId);
        int slotCount = definition.slotDefinitions != null ? definition.slotDefinitions.Length : 0;
        for (int i = 0; i < slotCount; i++)
        {
            Assert.IsTrue(state.TryUnlockSlot(
                buildingId,
                i,
                out _,
                out _));
        }
    }

    private static Dialog FindRuntimeDialog()
    {
        Dialog[] dialogs = Object.FindObjectsOfType<Dialog>(true);
        for (int i = 0; i < dialogs.Length; i++)
        {
            if (dialogs[i] != null && dialogs[i].gameObject.name == "RuntimeDialogController")
            {
                return dialogs[i];
            }
        }

        return null;
    }

    private static string ReadActiveDialogContent(Dialog dialog)
    {
        FieldInfo field = typeof(Dialog).GetField(
            "activeDialogContent",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        return (string)field.GetValue(dialog);
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        method.Invoke(target, null);
    }

    private static void DestroyRuntimeDialogs()
    {
        Dialog[] dialogs = Object.FindObjectsOfType<Dialog>(true);
        for (int i = 0; i < dialogs.Length; i++)
        {
            if (dialogs[i] != null && dialogs[i].gameObject.name == "RuntimeDialogController")
            {
                Object.DestroyImmediate(dialogs[i].gameObject);
            }
        }
    }
}
