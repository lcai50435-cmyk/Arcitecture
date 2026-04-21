using System;
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
}

[Serializable]
public class GameProgressSaveData
{
    public int version;
    public string selectedStageId;
    public WeaponType currentWeaponType;
    public int availableSpecialStructureInventory;
    public BuildingRuntimeStateSaveData[] buildingStates;
}

public static class GameProgressPersistence
{
    private const int CurrentVersion = 1;
    private const string SaveFileName = "game_progress.json";

    private static bool isReady;
    private static bool suppressSave;

    public static bool IsReady => isReady;

    public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        LoadFromDisk();
    }

    public static void SaveIfReady()
    {
        if (!isReady || suppressSave)
        {
            return;
        }

        SaveNow();
    }

    public static void SaveNow()
    {
        if (suppressSave)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(SavePath, JsonUtility.ToJson(BuildCurrentSaveData(), true));
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"保存游戏进度失败：{exception.Message}");
        }
    }

    public static void LoadFromDisk()
    {
        bool previousSuppressState = suppressSave;
        suppressSave = true;

        try
        {
            ApplyLoadedData(ReadSaveDataFromDisk());
        }
        finally
        {
            suppressSave = previousSuppressState;
            isReady = true;
        }
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

    private static GameProgressSaveData ReadSaveDataFromDisk()
    {
        try
        {
            if (!File.Exists(SavePath))
            {
                return null;
            }

            string rawJson = File.ReadAllText(SavePath);
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return null;
            }

            return JsonUtility.FromJson<GameProgressSaveData>(rawJson);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"读取游戏进度失败：{exception.Message}");
            return null;
        }
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

    private static GameProgressSaveData BuildCurrentSaveData()
    {
        RuntimeProgressState runtimeState = RuntimeProgressState.Instance ?? RuntimeProgressState.EnsureInstance();
        GameplayStageRuntime.EnsureSelectedStageUnlocked();
        PlayerLoadoutRuntime.EnsureCurrentWeaponUnlocked();

        return new GameProgressSaveData
        {
            version = CurrentVersion,
            selectedStageId = GameplayStageRuntime.SelectedStageId,
            currentWeaponType = PlayerLoadoutRuntime.CurrentWeaponType,
            availableSpecialStructureInventory = runtimeState.AvailableSpecialStructureInventory,
            buildingStates = runtimeState.ExportSaveData()
        };
    }
}
