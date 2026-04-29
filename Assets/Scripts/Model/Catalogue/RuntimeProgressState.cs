using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BuildingRuntimeStateData
{
    public CatalogueBuildingId buildingId;
    public int progress;
    public bool[] unlockedSlots;
    public bool[] grantedSlotRewards;
    public bool grantedCompletionReward;
    public bool isBuildingUnlocked;
    public bool isRepaired;

    public void EnsureSlotCapacity(int slotCount)
    {
        if (slotCount < 0)
        {
            slotCount = 0;
        }

        if (unlockedSlots == null || unlockedSlots.Length != slotCount)
        {
            bool[] previous = unlockedSlots;
            unlockedSlots = new bool[slotCount];
            if (previous != null)
            {
                Array.Copy(previous, unlockedSlots, Mathf.Min(previous.Length, unlockedSlots.Length));
            }
        }

        if (grantedSlotRewards == null || grantedSlotRewards.Length != slotCount)
        {
            bool[] previous = grantedSlotRewards;
            grantedSlotRewards = new bool[slotCount];
            if (previous != null)
            {
                Array.Copy(previous, grantedSlotRewards, Mathf.Min(previous.Length, grantedSlotRewards.Length));
            }
        }
    }
}

public class RuntimeProgressState : MonoBehaviour
{
    private const float CommonProgressRatio = 0.7f;

    public static RuntimeProgressState Instance { get; private set; }

    public event Action OnStateChanged;

    [SerializeField] private int availableSpecialStructureInventory;

    private readonly Dictionary<CatalogueBuildingId, BuildingRuntimeStateData> buildingStates =
        new Dictionary<CatalogueBuildingId, BuildingRuntimeStateData>();

    public int AvailableSpecialStructureInventory => availableSpecialStructureInventory;

    public static RuntimeProgressState EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        RuntimeProgressState existing = FindObjectOfType<RuntimeProgressState>();
        if (existing != null)
        {
            Instance = existing;
            Instance.InitializeDefinitions();
            return Instance;
        }

        GameObject runtimeObject = new GameObject("RuntimeProgressState");
        Instance = runtimeObject.AddComponent<RuntimeProgressState>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDefinitions();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public BuildingRuntimeStateData GetBuildingState(CatalogueBuildingId buildingId)
    {
        InitializeDefinitions();
        return buildingStates[buildingId];
    }

    public int GetBuildingProgress(CatalogueBuildingId buildingId)
    {
        return GetBuildingState(buildingId).progress;
    }

    public int GetTotalProgress()
    {
        InitializeDefinitions();

        int total = 0;
        foreach (KeyValuePair<CatalogueBuildingId, BuildingRuntimeStateData> pair in buildingStates)
        {
            total += Mathf.Max(pair.Value.progress, 0);
        }

        return total;
    }

    public int GetTotalMaxProgress()
    {
        int total = 0;
        foreach (BuildingDefinition definition in BuildingDefinitionLibrary.GetAll())
        {
            total += definition.requiredProgress;
        }

        return total;
    }

    public bool AddBuildingProgress(
        CatalogueBuildingId buildingId,
        int value,
        out BuildingRewardDefinition completionReward)
    {
        completionReward = null;

        if (value <= 0)
        {
            return false;
        }

        BuildingDefinition definition = BuildingDefinitionLibrary.Get(buildingId);
        BuildingRuntimeStateData state = GetBuildingState(buildingId);
        int previousProgress = state.progress;
        int currentCommonProgress = GetCommonProgressContribution(definition, state);
        int specialProgress = GetSpecialProgressContribution(definition, state);
        int commonProgressCap = GetCommonProgressCap(definition);
        int nextCommonProgress = Mathf.Clamp(currentCommonProgress + value, 0, commonProgressCap);
        state.progress = Mathf.Clamp(nextCommonProgress + specialProgress, 0, definition.requiredProgress);

        if (previousProgress == state.progress)
        {
            return false;
        }

        NotifyStateChanged();
        return true;
    }

    public void AddSpecialStructureInventory(int count)
    {
        if (count <= 0)
        {
            return;
        }

        availableSpecialStructureInventory += count;
        NotifyStateChanged();
    }

    public bool TryConsumeSpecialStructureInventory(int count)
    {
        if (count <= 0)
        {
            return true;
        }

        if (availableSpecialStructureInventory < count)
        {
            return false;
        }

        availableSpecialStructureInventory -= count;
        NotifyStateChanged();
        return true;
    }

    public bool TryUnlockSlot(
        CatalogueBuildingId buildingId,
        int slotIndex,
        out BuildingRewardDefinition slotReward,
        out BuildingRewardDefinition completionReward)
    {
        slotReward = null;
        completionReward = null;

        BuildingDefinition definition = BuildingDefinitionLibrary.Get(buildingId);
        BuildingRuntimeStateData state = GetBuildingState(buildingId);
        state.EnsureSlotCapacity(definition.slotDefinitions.Length);

        if (slotIndex < 0 || slotIndex >= state.unlockedSlots.Length)
        {
            return false;
        }

        if (state.unlockedSlots[slotIndex])
        {
            return false;
        }

        int commonProgress = GetCommonProgressContribution(definition, state);

        state.unlockedSlots[slotIndex] = true;
        state.grantedSlotRewards[slotIndex] = true;
        state.progress = Mathf.Clamp(
            commonProgress + GetSpecialProgressContribution(definition, state),
            0,
            definition.requiredProgress);
        slotReward = definition.slotDefinitions[slotIndex].reward;
        NotifyStateChanged();
        return true;
    }

    public bool IsSlotUnlocked(CatalogueBuildingId buildingId, int slotIndex)
    {
        BuildingRuntimeStateData state = GetBuildingState(buildingId);
        if (state.unlockedSlots == null || slotIndex < 0 || slotIndex >= state.unlockedSlots.Length)
        {
            return false;
        }

        return state.unlockedSlots[slotIndex];
    }

    public int GetUnlockedSlotCount(CatalogueBuildingId buildingId)
    {
        BuildingRuntimeStateData state = GetBuildingState(buildingId);
        if (state.unlockedSlots == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < state.unlockedSlots.Length; i++)
        {
            if (state.unlockedSlots[i])
            {
                count++;
            }
        }

        return count;
    }

    public bool IsBuildingUnlocked(CatalogueBuildingId buildingId)
    {
        return GetBuildingState(buildingId).isBuildingUnlocked;
    }

    public bool CanUnlockBuilding(CatalogueBuildingId buildingId)
    {
        BuildingDefinition definition = BuildingDefinitionLibrary.Get(buildingId);
        BuildingRuntimeStateData state = GetBuildingState(buildingId);
        return !state.isBuildingUnlocked &&
               state.progress >= definition.requiredProgress &&
               AreAllSlotsUnlocked(definition, state);
    }

    public bool TryUnlockBuilding(
        CatalogueBuildingId buildingId,
        out BuildingRewardDefinition completionReward)
    {
        completionReward = null;

        if (!CanUnlockBuilding(buildingId))
        {
            return false;
        }

        BuildingDefinition definition = BuildingDefinitionLibrary.Get(buildingId);
        BuildingRuntimeStateData state = GetBuildingState(buildingId);
        state.grantedCompletionReward = true;
        state.isBuildingUnlocked = true;
        completionReward = definition.completionReward;
        NotifyStateChanged();
        return true;
    }

    public bool IsBuildingRepairReady(CatalogueBuildingId buildingId)
    {
        BuildingDefinition definition = BuildingDefinitionLibrary.Get(buildingId);
        BuildingRuntimeStateData state = GetBuildingState(buildingId);
        state.EnsureSlotCapacity(definition.slotDefinitions != null ? definition.slotDefinitions.Length : 0);
        return state.isBuildingUnlocked &&
               state.progress >= definition.requiredProgress &&
               GetUnlockedSlotCountInternal(state) >= state.unlockedSlots.Length;
    }

    public bool IsBuildingRepaired(CatalogueBuildingId buildingId)
    {
        return GetBuildingState(buildingId).isRepaired;
    }

    public bool MarkBuildingRepaired(CatalogueBuildingId buildingId)
    {
        BuildingRuntimeStateData state = GetBuildingState(buildingId);
        if (state.isRepaired || !IsBuildingRepairReady(buildingId))
        {
            return false;
        }

        state.isRepaired = true;
        NotifyStateChanged();
        return true;
    }

    public IEnumerable<BuildingRewardDefinition> GetGrantedRewards()
    {
        InitializeDefinitions();

        foreach (BuildingDefinition definition in BuildingDefinitionLibrary.GetAll())
        {
            BuildingRuntimeStateData state = GetBuildingState(definition.buildingId);
            state.EnsureSlotCapacity(definition.slotDefinitions.Length);

            for (int i = 0; i < definition.slotDefinitions.Length; i++)
            {
                if (state.grantedSlotRewards[i] && definition.slotDefinitions[i].reward != null)
                {
                    yield return definition.slotDefinitions[i].reward;
                }
            }

            if (state.grantedCompletionReward && definition.completionReward != null)
            {
                yield return definition.completionReward;
            }
        }
    }

    public BuildingRuntimeStateSaveData[] ExportSaveData()
    {
        InitializeDefinitions();

        List<BuildingRuntimeStateSaveData> exportedStates = new List<BuildingRuntimeStateSaveData>();
        foreach (BuildingDefinition definition in BuildingDefinitionLibrary.GetAll())
        {
            BuildingRuntimeStateData state = GetBuildingState(definition.buildingId);
            exportedStates.Add(new BuildingRuntimeStateSaveData
            {
                buildingId = state.buildingId,
                progress = state.progress,
                unlockedSlots = CloneBoolArray(state.unlockedSlots),
                grantedSlotRewards = CloneBoolArray(state.grantedSlotRewards),
                grantedCompletionReward = state.grantedCompletionReward,
                isRepaired = state.isRepaired
            });
        }

        return exportedStates.ToArray();
    }

    public void ResetProgress(bool notifyListeners = true)
    {
        availableSpecialStructureInventory = 0;
        buildingStates.Clear();
        InitializeDefinitions();

        if (notifyListeners)
        {
            NotifyStateChanged(false);
        }
    }

    public void ImportFromSaveData(
        BuildingRuntimeStateSaveData[] savedStates,
        int specialStructureInventory,
        bool notifyListeners = true)
    {
        availableSpecialStructureInventory = Mathf.Max(0, specialStructureInventory);
        buildingStates.Clear();
        InitializeDefinitions();

        if (savedStates != null)
        {
            for (int i = 0; i < savedStates.Length; i++)
            {
                BuildingRuntimeStateSaveData savedState = savedStates[i];
                if (savedState == null || !buildingStates.TryGetValue(savedState.buildingId, out BuildingRuntimeStateData state))
                {
                    continue;
                }

                BuildingDefinition definition = BuildingDefinitionLibrary.Get(savedState.buildingId);
                int slotCount = definition.slotDefinitions != null ? definition.slotDefinitions.Length : 0;
                state.EnsureSlotCapacity(slotCount);
                state.progress = Mathf.Clamp(savedState.progress, 0, definition.requiredProgress);
                CopyBoolArray(savedState.unlockedSlots, state.unlockedSlots);
                CopyBoolArray(savedState.grantedSlotRewards, state.grantedSlotRewards);
                state.grantedCompletionReward = savedState.grantedCompletionReward;
                state.isRepaired = savedState.isRepaired;
            }
        }

        InitializeDefinitions();

        if (notifyListeners)
        {
            NotifyStateChanged(false);
        }
    }

    private void InitializeDefinitions()
    {
        foreach (BuildingDefinition definition in BuildingDefinitionLibrary.GetAll())
        {
            if (!buildingStates.TryGetValue(definition.buildingId, out BuildingRuntimeStateData state))
            {
                state = new BuildingRuntimeStateData
                {
                    buildingId = definition.buildingId
                };
                buildingStates.Add(definition.buildingId, state);
            }

            state.EnsureSlotCapacity(definition.slotDefinitions != null ? definition.slotDefinitions.Length : 0);
            state.progress = NormalizeProgress(definition, state, state.progress);
            state.isBuildingUnlocked = state.grantedCompletionReward;
        }
    }

    private static int GetCommonProgressCap(BuildingDefinition definition)
    {
        if (definition == null || definition.requiredProgress <= 0)
        {
            return 0;
        }

        int slotCount = definition.slotDefinitions != null ? definition.slotDefinitions.Length : 0;
        if (slotCount <= 0)
        {
            return definition.requiredProgress;
        }

        return Mathf.Clamp(
            Mathf.RoundToInt(definition.requiredProgress * CommonProgressRatio),
            0,
            definition.requiredProgress);
    }

    private static int GetSpecialProgressMax(BuildingDefinition definition)
    {
        if (definition == null)
        {
            return 0;
        }

        return Mathf.Max(0, definition.requiredProgress - GetCommonProgressCap(definition));
    }

    private static int GetSpecialProgressContribution(
        BuildingDefinition definition,
        BuildingRuntimeStateData state)
    {
        int slotCount = definition?.slotDefinitions != null ? definition.slotDefinitions.Length : 0;
        if (slotCount <= 0 || state == null)
        {
            return 0;
        }

        int unlockedCount = GetUnlockedSlotCountInternal(state);
        int specialProgressMax = GetSpecialProgressMax(definition);
        return Mathf.Clamp(
            Mathf.RoundToInt(specialProgressMax * (unlockedCount / (float)slotCount)),
            0,
            specialProgressMax);
    }

    private static int GetCommonProgressContribution(
        BuildingDefinition definition,
        BuildingRuntimeStateData state)
    {
        if (definition == null || state == null)
        {
            return 0;
        }

        int specialProgress = GetSpecialProgressContribution(definition, state);
        return Mathf.Clamp(state.progress - specialProgress, 0, GetCommonProgressCap(definition));
    }

    private static int NormalizeProgress(
        BuildingDefinition definition,
        BuildingRuntimeStateData state,
        int totalProgress)
    {
        if (definition == null || state == null)
        {
            return 0;
        }

        int specialProgress = GetSpecialProgressContribution(definition, state);
        int commonProgress = Mathf.Clamp(
            totalProgress - specialProgress,
            0,
            GetCommonProgressCap(definition));
        return Mathf.Clamp(commonProgress + specialProgress, 0, definition.requiredProgress);
    }

    private static bool AreAllSlotsUnlocked(
        BuildingDefinition definition,
        BuildingRuntimeStateData state)
    {
        int slotCount = definition?.slotDefinitions != null ? definition.slotDefinitions.Length : 0;
        if (slotCount <= 0)
        {
            return true;
        }

        return GetUnlockedSlotCountInternal(state) >= slotCount;
    }

    private static int GetUnlockedSlotCountInternal(BuildingRuntimeStateData state)
    {
        if (state.unlockedSlots == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < state.unlockedSlots.Length; i++)
        {
            if (state.unlockedSlots[i])
            {
                count++;
            }
        }

        return count;
    }

    private static bool[] CloneBoolArray(bool[] source)
    {
        if (source == null)
        {
            return null;
        }

        bool[] clone = new bool[source.Length];
        Array.Copy(source, clone, source.Length);
        return clone;
    }

    private static void CopyBoolArray(bool[] source, bool[] target)
    {
        if (target == null)
        {
            return;
        }

        Array.Clear(target, 0, target.Length);
        if (source == null)
        {
            return;
        }

        Array.Copy(source, target, Mathf.Min(source.Length, target.Length));
    }

    private void NotifyStateChanged(bool shouldPersist = true)
    {
        OnStateChanged?.Invoke();

        if (shouldPersist)
        {
            GameProgressPersistence.SaveIfReady();
        }
    }
}
