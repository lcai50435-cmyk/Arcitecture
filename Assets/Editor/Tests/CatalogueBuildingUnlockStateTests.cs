using System.Collections.Generic;
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

    private static T CreateChild<T>(Transform parent, string name)
        where T : Component
    {
        GameObject child = new GameObject(name, typeof(RectTransform), typeof(T));
        child.transform.SetParent(parent, false);
        return child.GetComponent<T>();
    }
}
