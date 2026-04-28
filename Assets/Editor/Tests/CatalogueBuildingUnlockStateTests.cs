using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
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

        DestroyRuntimeDialogs();
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
        Assert.IsTrue(lockedButton.interactable);

        lockedButton.onClick.Invoke();

        Assert.IsTrue(state.IsBuildingUnlocked(CatalogueBuildingId.Building1));
        Assert.IsFalse(lockedVisual.activeSelf);
        Assert.IsTrue(unlockedVisual.activeSelf);
    }

    [Test]
    public void UnlockedBuildingVisualOpensFocusedTopmostIntroductionDialog()
    {
        RuntimeProgressState state = RuntimeProgressState.EnsureInstance();
        state.ResetProgress(false);

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
        Assert.IsTrue(state.TryUnlockBuilding(CatalogueBuildingId.Building1, out _));

        unlockState.RefreshState();

        Button unlockedButton = unlockedVisual.GetComponent<Button>();
        Assert.IsNotNull(unlockedButton);
        Assert.IsTrue(unlockedButton.interactable);

        unlockedButton.onClick.Invoke();

        Dialog dialog = FindRuntimeDialog();
        Assert.IsNotNull(dialog);
        Assert.IsNotNull(dialog.dialogPanel);
        Assert.IsTrue(dialog.dialogPanel.activeSelf);
        Assert.IsTrue(dialog.clickCloseButton.gameObject.activeSelf);
        Assert.That(ReadActiveDialogContent(dialog), Does.Contain("测试建筑"));
        Assert.That(ReadActiveDialogContent(dialog), Does.Contain("这是解锁后的建筑介绍。"));

        Canvas canvas = dialog.dialogPanel.GetComponent<Canvas>();
        Assert.IsNotNull(canvas);
        Assert.IsTrue(canvas.overrideSorting);
        Assert.Greater(canvas.sortingOrder, RuntimeModalStyle.ModalSortingOrder);
    }

    private static T CreateChild<T>(Transform parent, string name)
        where T : Component
    {
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(T));
        child.transform.SetParent(parent, false);
        return child.GetComponent<T>();
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
