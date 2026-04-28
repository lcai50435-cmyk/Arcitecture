using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class BuildingRuntimeStateSaveData
{
    public CatalogueBuildingId buildingId;
    public int progress;
    public bool[] unlockedSlots;
    public bool[] grantedSlotRewards;
    public bool grantedCompletionReward;
    public bool isRepaired;
}

[Serializable]
public class SaveSlotSummary
{
    public int slotId;
    public bool hasSave;
    public string createdAtUtc;
    public string savedAtUtc;
    public string selectedStageId;
    public WeaponType currentWeaponType;
    public float progressPercent;
}

[Serializable]
public class GameProgressSaveData
{
    public int version;
    public string createdAtUtc;
    public string savedAtUtc;
    public string selectedStageId;
    public WeaponType currentWeaponType;
    public int availableSpecialStructureInventory;
    public BuildingRuntimeStateSaveData[] buildingStates;
}

public static class GameProgressPersistence
{
    public const int SlotCount = 3;

    private const int CurrentVersion = 3;
    private const string SaveDirectoryName = "Saves";
    private const string SlotDirectoryNameFormat = "slot_{0}";
    private const string SaveFileName = "game_progress.json";

    private static bool isReady;
    private static bool suppressSave;
    private static int? activeSlotId;

    public static bool IsReady => isReady;
    public static bool HasActiveSlot => activeSlotId.HasValue;
    public static string SavePath => activeSlotId.HasValue ? GetSlotSavePath(activeSlotId.Value) : string.Empty;

    private static string SaveRootPath => Path.Combine(Application.persistentDataPath, SaveDirectoryName);
    private static string LegacySavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        isReady = false;
        suppressSave = false;
        activeSlotId = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        MigrateLegacySingleSaveIfNeeded();
        ResetLoadedRuntimeState();
        isReady = true;
    }

    public static bool HasSaveData()
    {
        return HasAnySlots();
    }

    public static bool HasAnySlots()
    {
        for (int slotId = 1; slotId <= SlotCount; slotId++)
        {
            if (HasSaveInSlot(slotId))
            {
                return true;
            }
        }

        return false;
    }

    public static IReadOnlyList<SaveSlotSummary> ListSlots()
    {
        SaveSlotSummary[] summaries = new SaveSlotSummary[SlotCount];
        for (int slotIndex = 0; slotIndex < SlotCount; slotIndex++)
        {
            int slotId = slotIndex + 1;
            GameProgressSaveData saveData = ReadSaveDataFromSlot(slotId);
            summaries[slotIndex] = BuildSlotSummary(slotId, saveData);
        }

        return summaries;
    }

    public static void SaveIfReady()
    {
        if (!isReady || suppressSave || !HasActiveSlot)
        {
            return;
        }

        SaveNow();
    }

    public static void SaveNow()
    {
        if (suppressSave || !HasActiveSlot)
        {
            return;
        }

        int slotId = activeSlotId.Value;
        GameProgressSaveData existingData = ReadSaveDataFromSlot(slotId);
        string createdAtUtc = ResolveCreatedAtUtc(existingData, GetSlotSavePath(slotId));
        SaveCurrentStateToSlot(slotId, createdAtUtc);
    }

    public static void LoadSlot(int slotId)
    {
        if (!IsValidSlotId(slotId))
        {
            Debug.LogWarning($"读取存档失败：无效槽位 {slotId}");
            return;
        }

        GameProgressSaveData saveData = ReadSaveDataFromSlot(slotId);
        if (saveData == null)
        {
            Debug.LogWarning($"读取存档失败：槽位 {slotId} 没有可用数据。");
            return;
        }

        bool previousSuppressState = suppressSave;
        suppressSave = true;

        try
        {
            activeSlotId = slotId;
            ApplyLoadedData(saveData);
        }
        finally
        {
            suppressSave = previousSuppressState;
            isReady = true;
        }
    }

    public static void StartNewGame(int slotId)
    {
        if (!IsValidSlotId(slotId))
        {
            Debug.LogWarning($"开始新游戏失败：无效槽位 {slotId}");
            return;
        }

        string createdAtUtc = DateTime.UtcNow.ToString("O");
        DeleteSlotDirectoryIfExists(slotId);

        bool previousSuppressState = suppressSave;
        suppressSave = true;

        try
        {
            activeSlotId = slotId;
            ApplyLoadedData(null);
        }
        finally
        {
            suppressSave = previousSuppressState;
            isReady = true;
        }

        SaveCurrentStateToSlot(slotId, createdAtUtc);
    }

    public static void DeleteSlot(int slotId)
    {
        if (!IsValidSlotId(slotId))
        {
            return;
        }

        DeleteSlotDirectoryIfExists(slotId);

        if (activeSlotId.HasValue && activeSlotId.Value == slotId)
        {
            activeSlotId = null;
            ResetLoadedRuntimeState();
        }
    }

    public static void DeleteAllSlots()
    {
        for (int slotId = 1; slotId <= SlotCount; slotId++)
        {
            DeleteSlotDirectoryIfExists(slotId);
        }

        DeleteFileIfExists(LegacySavePath);
        activeSlotId = null;
        ResetLoadedRuntimeState();
        isReady = true;
    }

    public static void ResetSaveData()
    {
        if (activeSlotId.HasValue)
        {
            int slotId = activeSlotId.Value;
            DeleteSlotDirectoryIfExists(slotId);
            activeSlotId = null;
        }

        ResetLoadedRuntimeState();
        isReady = true;
    }

    public static void RunWithoutSaving(Action action)
    {
        bool previousSuppressState = suppressSave;
        suppressSave = true;

        try
        {
            action?.Invoke();
        }
        finally
        {
            suppressSave = previousSuppressState;
        }
    }

    private static SaveSlotSummary BuildSlotSummary(int slotId, GameProgressSaveData saveData)
    {
        if (saveData == null)
        {
            return new SaveSlotSummary
            {
                slotId = slotId,
                hasSave = false,
                createdAtUtc = string.Empty,
                savedAtUtc = string.Empty,
                selectedStageId = string.Empty,
                currentWeaponType = WeaponType.DirectInk,
                progressPercent = 0f
            };
        }

        return new SaveSlotSummary
        {
            slotId = slotId,
            hasSave = true,
            createdAtUtc = saveData.createdAtUtc ?? string.Empty,
            savedAtUtc = saveData.savedAtUtc ?? string.Empty,
            selectedStageId = saveData.selectedStageId ?? string.Empty,
            currentWeaponType = saveData.currentWeaponType,
            progressPercent = CalculateProgressPercent(saveData)
        };
    }

    private static float CalculateProgressPercent(GameProgressSaveData saveData)
    {
        if (saveData == null)
        {
            return 0f;
        }

        int totalProgress = 0;
        if (saveData.buildingStates != null)
        {
            for (int i = 0; i < saveData.buildingStates.Length; i++)
            {
                BuildingRuntimeStateSaveData state = saveData.buildingStates[i];
                if (state == null)
                {
                    continue;
                }

                totalProgress += Mathf.Max(0, state.progress);
            }
        }

        int maxProgress = 0;
        foreach (BuildingDefinition definition in BuildingDefinitionLibrary.GetAll())
        {
            if (definition != null)
            {
                maxProgress += Mathf.Max(0, definition.requiredProgress);
            }
        }

        if (maxProgress <= 0)
        {
            return 0f;
        }

        return Mathf.Clamp01(totalProgress / (float)maxProgress) * 100f;
    }

    private static void SaveCurrentStateToSlot(int slotId, string createdAtUtc)
    {
        try
        {
            string slotSavePath = GetSlotSavePath(slotId);
            Directory.CreateDirectory(Path.GetDirectoryName(slotSavePath) ?? SaveRootPath);
            File.WriteAllText(slotSavePath, JsonUtility.ToJson(BuildCurrentSaveData(createdAtUtc), true));
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"保存游戏进度失败：{exception.Message}");
        }
    }

    private static GameProgressSaveData ReadSaveDataFromSlot(int slotId)
    {
        if (!IsValidSlotId(slotId))
        {
            return null;
        }

        return ReadSaveDataFromPath(GetSlotSavePath(slotId));
    }

    private static GameProgressSaveData ReadSaveDataFromPath(string savePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(savePath) || !File.Exists(savePath))
            {
                return null;
            }

            string rawJson = File.ReadAllText(savePath);
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return null;
            }

            GameProgressSaveData saveData = JsonUtility.FromJson<GameProgressSaveData>(rawJson);
            if (saveData == null)
            {
                return null;
            }

            PopulateMissingMetadata(saveData, savePath);
            return saveData;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"读取游戏进度失败：{exception.Message}");
            return null;
        }
    }

    private static void PopulateMissingMetadata(GameProgressSaveData saveData, string savePath)
    {
        if (saveData == null)
        {
            return;
        }

        DateTime fallbackTimestamp = File.Exists(savePath)
            ? File.GetLastWriteTimeUtc(savePath)
            : DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(saveData.createdAtUtc))
        {
            saveData.createdAtUtc = fallbackTimestamp.ToString("O");
        }

        if (string.IsNullOrWhiteSpace(saveData.savedAtUtc))
        {
            saveData.savedAtUtc = fallbackTimestamp.ToString("O");
        }
    }

    private static void ResetLoadedRuntimeState()
    {
        bool previousSuppressState = suppressSave;
        suppressSave = true;

        try
        {
            ApplyLoadedData(null);
        }
        finally
        {
            suppressSave = previousSuppressState;
        }
    }

    private static void DeleteSlotDirectoryIfExists(int slotId)
    {
        try
        {
            string slotDirectoryPath = GetSlotDirectoryPath(slotId);
            if (!Directory.Exists(slotDirectoryPath))
            {
                return;
            }

            Directory.Delete(slotDirectoryPath, true);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"删除槽位 {slotId} 失败：{exception.Message}");
        }
    }

    private static void DeleteFileIfExists(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return;
            }

            File.Delete(path);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"删除文件失败：{exception.Message}");
        }
    }

    private static string ResolveCreatedAtUtc(GameProgressSaveData existingData, string savePath)
    {
        if (existingData != null && !string.IsNullOrWhiteSpace(existingData.createdAtUtc))
        {
            return existingData.createdAtUtc;
        }

        if (File.Exists(savePath))
        {
            return File.GetCreationTimeUtc(savePath).ToString("O");
        }

        return DateTime.UtcNow.ToString("O");
    }

    private static GameProgressSaveData BuildCurrentSaveData(string createdAtUtc)
    {
        RuntimeProgressState runtimeState = RuntimeProgressState.Instance ?? RuntimeProgressState.EnsureInstance();
        GameplayStageRuntime.EnsureSelectedStageUnlocked();
        PlayerLoadoutRuntime.EnsureCurrentWeaponUnlocked();

        return new GameProgressSaveData
        {
            version = CurrentVersion,
            createdAtUtc = string.IsNullOrWhiteSpace(createdAtUtc) ? DateTime.UtcNow.ToString("O") : createdAtUtc,
            savedAtUtc = DateTime.UtcNow.ToString("O"),
            selectedStageId = GameplayStageRuntime.SelectedStageId,
            currentWeaponType = PlayerLoadoutRuntime.CurrentWeaponType,
            availableSpecialStructureInventory = runtimeState.AvailableSpecialStructureInventory,
            buildingStates = runtimeState.ExportSaveData()
        };
    }

    private static void ApplyLoadedData(GameProgressSaveData saveData)
    {
        RuntimeProgressState runtimeState = RuntimeProgressState.EnsureInstance();

        if (saveData == null)
        {
            runtimeState.ResetProgress(false);
            GameplayStageRuntime.ResetToDefault();
            PlayerLoadoutRuntime.CurrentWeaponType = WeaponType.DirectInk;
            PlayerLoadoutRuntime.AllowBaseAttack = false;
            return;
        }

        runtimeState.ImportFromSaveData(
            saveData.buildingStates,
            saveData.availableSpecialStructureInventory,
            false);

        GameplayStageRuntime.SelectStage(saveData.selectedStageId);
        GameplayStageRuntime.EnsureSelectedStageUnlocked();

        PlayerLoadoutRuntime.CurrentWeaponType = saveData.currentWeaponType;
        PlayerLoadoutRuntime.EnsureCurrentWeaponUnlocked();
        PlayerLoadoutRuntime.AllowBaseAttack = false;
    }

    private static void MigrateLegacySingleSaveIfNeeded()
    {
        try
        {
            if (HasAnySlots() || !File.Exists(LegacySavePath))
            {
                return;
            }

            GameProgressSaveData legacySaveData = ReadSaveDataFromPath(LegacySavePath);
            if (legacySaveData == null)
            {
                DeleteFileIfExists(LegacySavePath);
                return;
            }

            string slotSavePath = GetSlotSavePath(1);
            Directory.CreateDirectory(Path.GetDirectoryName(slotSavePath) ?? SaveRootPath);
            File.WriteAllText(slotSavePath, JsonUtility.ToJson(legacySaveData, true));
            DeleteFileIfExists(LegacySavePath);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"迁移旧存档失败：{exception.Message}");
        }
    }

    private static string GetSlotDirectoryPath(int slotId)
    {
        return Path.Combine(SaveRootPath, string.Format(SlotDirectoryNameFormat, slotId));
    }

    private static string GetSlotSavePath(int slotId)
    {
        return Path.Combine(GetSlotDirectoryPath(slotId), SaveFileName);
    }

    private static bool HasSaveInSlot(int slotId)
    {
        try
        {
            string savePath = GetSlotSavePath(slotId);
            return File.Exists(savePath) && new FileInfo(savePath).Length > 0L;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"检查槽位 {slotId} 失败：{exception.Message}");
            return false;
        }
    }

    private static bool IsValidSlotId(int slotId)
    {
        return slotId >= 1 && slotId <= SlotCount;
    }
}

public static class GameSaveResetService
{
    public static bool HasAnySaveData()
    {
        return GameProgressPersistence.HasAnySlots() || PhotoAlbumRepository.HasEntries();
    }

    public static void ResetAllSaveData()
    {
        GameProgressPersistence.DeleteAllSlots();
        PhotoAlbumRepository.ClearAll();
        RuntimeCollectedCrystalRegistry.Instance?.Clear();
        BackpackMananger.Instance?.ClearAllItems();
    }
}
