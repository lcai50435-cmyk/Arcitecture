using System;
using System.Collections.Generic;

[Serializable]
public class GameplayStageDefinition
{
    public string stageId;
    public string stageLabel;
    public string mapTitle;
    public string displayName;
    public string sceneName;
    public string[] sceneAliases;
    public CatalogueBuildingId stageBuildingId;
    public CatalogueBuildingId gatingBuildingId;
    public string lockedHint;
    public bool isPlaceholder;
}

public static class GameplayStageCatalog
{
    private static readonly GameplayStageDefinition[] StageDefinitions =
    {
        new GameplayStageDefinition
        {
            stageId = "stage_01",
            stageLabel = "第一关",
            mapTitle = "福建土楼",
            displayName = "第一关 · 福建土楼",
            sceneName = "FirstPass_1",
            sceneAliases = new[] { "GameScene" },
            stageBuildingId = CatalogueBuildingId.Building1,
            gatingBuildingId = CatalogueBuildingId.Building1,
            lockedHint = "默认开放"
        },
        new GameplayStageDefinition
        {
            stageId = "stage_02",
            stageLabel = "第二关",
            mapTitle = "安徽水乡民居",
            displayName = "第二关 · 安徽水乡民居",
            sceneName = "SecondPassSence",
            sceneAliases = new[] { "GameScene_03", "SecondPass" },
            stageBuildingId = CatalogueBuildingId.Building3,
            gatingBuildingId = CatalogueBuildingId.Building1,
            lockedHint = "解锁福建土楼图鉴后开放"
        },
        new GameplayStageDefinition
        {
            stageId = "stage_03",
            stageLabel = "第三关",
            mapTitle = "赵州桥",
            displayName = "第三关 · 赵州桥",
            sceneName = "GameScene_02",
            stageBuildingId = CatalogueBuildingId.Building2,
            gatingBuildingId = CatalogueBuildingId.Building3,
            lockedHint = "解锁安徽水乡民居图鉴后开放"
        },
        new GameplayStageDefinition
        {
            stageId = "stage_04",
            stageLabel = "第四关",
            mapTitle = "未开放",
            displayName = "第四关 · 未开放",
            sceneName = string.Empty,
            stageBuildingId = CatalogueBuildingId.Building3,
            gatingBuildingId = CatalogueBuildingId.Building3,
            lockedHint = "后续版本开放",
            isPlaceholder = true
        },
        new GameplayStageDefinition
        {
            stageId = "stage_05",
            stageLabel = "第五关",
            mapTitle = "未开放",
            displayName = "第五关 · 未开放",
            sceneName = string.Empty,
            stageBuildingId = CatalogueBuildingId.Building3,
            gatingBuildingId = CatalogueBuildingId.Building3,
            lockedHint = "后续版本开放",
            isPlaceholder = true
        }
    };

    public static IReadOnlyList<GameplayStageDefinition> GetAll()
    {
        return StageDefinitions;
    }

    public static GameplayStageDefinition GetDefaultStage()
    {
        return StageDefinitions[0];
    }

    public static GameplayStageDefinition GetStageById(string stageId)
    {
        if (string.IsNullOrWhiteSpace(stageId))
        {
            return null;
        }

        for (int i = 0; i < StageDefinitions.Length; i++)
        {
            GameplayStageDefinition definition = StageDefinitions[i];
            if (definition.stageId == stageId)
            {
                return definition;
            }
        }

        return null;
    }

    public static GameplayStageDefinition GetStageByScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return null;
        }

        for (int i = 0; i < StageDefinitions.Length; i++)
        {
            GameplayStageDefinition definition = StageDefinitions[i];
            if (!definition.isPlaceholder && MatchesSceneName(definition, sceneName))
            {
                return definition;
            }
        }

        return null;
    }

    private static bool MatchesSceneName(GameplayStageDefinition definition, string sceneName)
    {
        if (definition == null)
        {
            return false;
        }

        if (string.Equals(definition.sceneName, sceneName, StringComparison.Ordinal))
        {
            return true;
        }

        string[] aliases = definition.sceneAliases;
        if (aliases == null)
        {
            return false;
        }

        for (int i = 0; i < aliases.Length; i++)
        {
            if (string.Equals(aliases[i], sceneName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsGameplayScene(string sceneName)
    {
        return GetStageByScene(sceneName) != null;
    }

    public static int GetStageIndex(string stageId)
    {
        for (int i = 0; i < StageDefinitions.Length; i++)
        {
            if (StageDefinitions[i].stageId == stageId)
            {
                return i;
            }
        }

        return -1;
    }

    public static bool IsStageUnlocked(GameplayStageDefinition definition, RuntimeProgressState runtimeState = null)
    {
        if (definition == null || definition.isPlaceholder)
        {
            return false;
        }

        int stageIndex = GetStageIndex(definition.stageId);
        if (stageIndex <= 0)
        {
            return true;
        }

        runtimeState = runtimeState ?? RuntimeProgressState.Instance ?? RuntimeProgressState.EnsureInstance();
        return runtimeState.IsBuildingUnlocked(definition.gatingBuildingId);
    }

    public static GameplayStageDefinition GetFirstUnlockedStage(RuntimeProgressState runtimeState = null)
    {
        runtimeState = runtimeState ?? RuntimeProgressState.Instance ?? RuntimeProgressState.EnsureInstance();

        for (int i = 0; i < StageDefinitions.Length; i++)
        {
            GameplayStageDefinition definition = StageDefinitions[i];
            if (IsStageUnlocked(definition, runtimeState))
            {
                return definition;
            }
        }

        return GetDefaultStage();
    }
}

public static class GameplayStageRuntime
{
    private static string selectedStageId = GameplayStageCatalog.GetDefaultStage().stageId;

    public static string SelectedStageId => ResolveSelectedStageId();

    public static GameplayStageDefinition SelectedStage
    {
        get
        {
            GameplayStageDefinition stage = GameplayStageCatalog.GetStageById(ResolveSelectedStageId());
            return stage ?? GameplayStageCatalog.GetDefaultStage();
        }
    }

    public static void SelectStage(string stageId)
    {
        GameplayStageDefinition stage = GameplayStageCatalog.GetStageById(stageId);
        selectedStageId = stage != null
            ? stage.stageId
            : GameplayStageCatalog.GetDefaultStage().stageId;
    }

    public static void ResetToDefault()
    {
        selectedStageId = GameplayStageCatalog.GetDefaultStage().stageId;
    }

    public static string GetSelectedSceneName()
    {
        return SelectedStage.sceneName;
    }

    public static void EnsureSelectedStageUnlocked()
    {
        RuntimeProgressState runtimeState = RuntimeProgressState.Instance ?? RuntimeProgressState.EnsureInstance();
        GameplayStageDefinition currentStage = GameplayStageCatalog.GetStageById(selectedStageId);
        if (currentStage != null && GameplayStageCatalog.IsStageUnlocked(currentStage, runtimeState))
        {
            return;
        }

        selectedStageId = GameplayStageCatalog.GetFirstUnlockedStage(runtimeState).stageId;
    }

    private static string ResolveSelectedStageId()
    {
        if (GameplayStageCatalog.GetStageById(selectedStageId) == null)
        {
            selectedStageId = GameplayStageCatalog.GetDefaultStage().stageId;
        }

        return selectedStageId;
    }
}
