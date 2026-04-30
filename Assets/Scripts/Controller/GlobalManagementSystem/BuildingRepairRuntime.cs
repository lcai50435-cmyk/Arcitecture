using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BaseHubRepairWorkbenchInteract : MonoBehaviour, IInteractable
{
    private readonly Dictionary<CatalogueBuildingId, GameObject> activeDrops =
        new Dictionary<CatalogueBuildingId, GameObject>();

    public string InteractionTip => "图鉴解锁即开放关卡";

    public void OnInteract()
    {
        RuntimeSubtitleFeedHud.PushMessage("现在解锁建筑图鉴后即可开放下一关，不再需要领取修复材料。");
    }

    private static bool ResolveReadyBuilding(out CatalogueBuildingId buildingId)
    {
        RuntimeProgressState runtimeState = RuntimeProgressState.Instance ?? RuntimeProgressState.EnsureInstance();
        foreach (BuildingDefinition definition in BuildingDefinitionLibrary.GetAll())
        {
            if (definition == null)
            {
                continue;
            }

            if (!runtimeState.IsBuildingRepaired(definition.buildingId) &&
                runtimeState.IsBuildingRepairReady(definition.buildingId))
            {
                buildingId = definition.buildingId;
                return true;
            }
        }

        buildingId = CatalogueBuildingId.Building1;
        return false;
    }
}

public sealed class RepairableBuildingGroup : MonoBehaviour, IInteractable
{
    [SerializeField] private CatalogueBuildingId buildingId = CatalogueBuildingId.Building1;
    [SerializeField] private Sprite brokenSprite;
    [SerializeField] private Sprite repairedSprite;

    private readonly List<RepairableBuildingVisual> visuals = new List<RepairableBuildingVisual>();
    private bool initialized;

    public CatalogueBuildingId BuildingId
    {
        get => buildingId;
        set => buildingId = value;
    }

    public string InteractionTip
    {
        get
        {
            RuntimeProgressState runtimeState = RuntimeProgressState.Instance ?? RuntimeProgressState.EnsureInstance();
            if (runtimeState.IsBuildingRepaired(buildingId))
            {
                return "建筑已修复";
            }

            BackpackMananger backpack = BackpackMananger.Instance;
            return backpack != null && backpack.HasRepairMaterial(buildingId)
                ? "修复建筑"
                : "缺少修复材料";
        }
    }

    public void Configure(CatalogueBuildingId targetBuildingId, Sprite broken, Sprite repaired)
    {
        buildingId = targetBuildingId;
        brokenSprite = broken;
        repairedSprite = repaired;
        initialized = true;
        RefreshVisuals();
    }

    public void RegisterVisual(RepairableBuildingVisual visual)
    {
        if (visual == null || visuals.Contains(visual))
        {
            return;
        }

        visuals.Add(visual);
        visual.Configure(this, brokenSprite, repairedSprite);
    }

    public void OnInteract()
    {
        RuntimeProgressState runtimeState = RuntimeProgressState.Instance ?? RuntimeProgressState.EnsureInstance();
        BuildingDefinition definition = BuildingDefinitionLibrary.Get(buildingId);

        if (runtimeState.IsBuildingRepaired(buildingId))
        {
            RuntimeSubtitleFeedHud.PushMessage($"{definition.displayName}已经恢复完整。");
            return;
        }

        BackpackMananger backpack = BackpackMananger.Instance;
        if (backpack == null || !backpack.HasRepairMaterial(buildingId))
        {
            RuntimeSubtitleFeedHud.PushMessage($"需要从基地带来{definition.displayName}修复材料。");
            return;
        }

        if (!runtimeState.IsBuildingRepairReady(buildingId))
        {
            RuntimeSubtitleFeedHud.PushMessage("图鉴进度和专用结构尚未完成，暂时无法修复。");
            return;
        }

        if (!backpack.TryConsumeRepairMaterial(buildingId))
        {
            RuntimeSubtitleFeedHud.PushMessage("修复材料消耗失败。");
            return;
        }

        if (!runtimeState.MarkBuildingRepaired(buildingId))
        {
            RuntimeSubtitleFeedHud.PushMessage("建筑修复状态未更新。");
            return;
        }

        RefreshVisuals();
        StartCoroutine(PlayRepairEffect());
        RuntimeSubtitleFeedHud.PushMessage($"{definition.displayName}已修复，新的关卡路径已经稳定。");
        GameProgressPersistence.SaveIfReady();
    }

    private void Awake()
    {
        if (!initialized)
        {
            RefreshVisuals();
        }
    }

    private void RefreshVisuals()
    {
        bool repaired = RuntimeProgressState.EnsureInstance().IsBuildingRepaired(buildingId);
        for (int i = 0; i < visuals.Count; i++)
        {
            if (visuals[i] != null)
            {
                visuals[i].ApplyState(repaired);
            }
        }
    }

    private IEnumerator PlayRepairEffect()
    {
        float elapsed = 0f;
        const float duration = 0.95f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float pulse = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
            for (int i = 0; i < visuals.Count; i++)
            {
                if (visuals[i] != null)
                {
                    visuals[i].SetPulse(pulse);
                }
            }

            yield return null;
        }

        for (int i = 0; i < visuals.Count; i++)
        {
            if (visuals[i] != null)
            {
                visuals[i].SetPulse(0f);
            }
        }
    }
}

public sealed class RepairableBuildingVisual : MonoBehaviour
{
    private RepairableBuildingGroup group;
    private Sprite brokenSprite;
    private Sprite repairedSprite;
    private SpriteRenderer spriteRenderer;
    private Color baseColor = Color.white;

    public void Configure(RepairableBuildingGroup owner, Sprite broken, Sprite repaired)
    {
        group = owner;
        brokenSprite = broken;
        repairedSprite = repaired;
        spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyState(RuntimeProgressState.EnsureInstance().IsBuildingRepaired(owner.BuildingId));
    }

    public void ApplyState(bool repaired)
    {
        spriteRenderer ??= GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            return;
        }

        Sprite nextSprite = repaired && repairedSprite != null ? repairedSprite : brokenSprite;
        if (nextSprite != null)
        {
            spriteRenderer.sprite = RuntimeSpriteDisplaySanitizer.GetDisplaySprite(nextSprite);
        }

        baseColor = repaired
            ? Color.white
            : new Color(0.70f, 0.66f, 0.58f, 1f);
        spriteRenderer.color = baseColor;

        if (repaired)
        {
            RuntimeWaterReflectionCaster.EnsureForRenderer(spriteRenderer);
        }
    }

    public void SetPulse(float pulse)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.color = Color.Lerp(baseColor, new Color(0.82f, 1f, 0.74f, 1f), pulse * 0.72f);
        transform.localScale = Vector3.one * (1f + pulse * 0.04f);
    }
}

public static class RepairableBuildingBootstrapper
{
    private const float BuildingScale = 0.13f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryCreate(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreate(scene);
    }

    private static void TryCreate(Scene scene)
    {
        GameplayStageDefinition stage = GameplayStageCatalog.GetStageByScene(scene.name);
        if (stage == null)
        {
            return;
        }

        if (!ShouldSpawnRepairableBuilding())
        {
            return;
        }

        RepairableBuildingGroup[] existingGroups = Object.FindObjectsOfType<RepairableBuildingGroup>(true);
        for (int i = 0; i < existingGroups.Length; i++)
        {
            if (existingGroups[i] != null && existingGroups[i].BuildingId == stage.stageBuildingId)
            {
                return;
            }
        }

        RuntimeProgressState runtimeState = RuntimeProgressState.Instance ?? RuntimeProgressState.EnsureInstance();
        BackpackMananger backpack = BackpackMananger.Instance;
        bool hasRepairMaterial = backpack != null && backpack.HasRepairMaterial(stage.stageBuildingId);
        if (!ShouldSpawnRepairableBuilding(
                runtimeState.IsBuildingRepairReady(stage.stageBuildingId),
                hasRepairMaterial,
                runtimeState.IsBuildingRepaired(stage.stageBuildingId)))
        {
            return;
        }

        Sprite broken = RuntimeProjectSpriteLoader.LoadSprite(GetBrokenSpritePath(stage.stageBuildingId), false, SpriteMeshType.FullRect);
        Sprite repaired = RuntimeProjectSpriteLoader.LoadSprite(GetRepairedSpritePath(stage.stageBuildingId), false, SpriteMeshType.FullRect);

        GameObject root = new GameObject($"RepairableBuilding_{stage.stageId}");
        SceneManager.MoveGameObjectToScene(root, scene);
        root.transform.position = ResolveSpawnPosition();

        RepairableBuildingGroup group = root.AddComponent<RepairableBuildingGroup>();
        group.BuildingId = stage.stageBuildingId;

        CircleCollider2D trigger = root.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 1.7f;

        GameObject visualObject = new GameObject("BuildingVisual");
        visualObject.transform.SetParent(root.transform, false);
        visualObject.transform.localPosition = Vector3.zero;
        visualObject.transform.localScale = Vector3.one * BuildingScale;

        SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 2;
        renderer.sprite = RuntimeSpriteDisplaySanitizer.GetDisplaySprite(broken);

        RepairableBuildingVisual visual = visualObject.AddComponent<RepairableBuildingVisual>();
        group.Configure(stage.stageBuildingId, broken, repaired);
        group.RegisterVisual(visual);
    }

    public static bool ShouldSpawnRepairableBuilding()
    {
        return false;
    }

    public static bool ShouldSpawnRepairableBuilding(
        bool isRepairReady,
        bool hasRepairMaterial,
        bool isRepaired)
    {
        return false;
    }

    private static Vector3 ResolveSpawnPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player.transform.position + new Vector3(2.4f, 1.25f, 0f);
        }

        GameObject catalogue = GameObject.FindGameObjectWithTag("Catalogue");
        if (catalogue != null)
        {
            return catalogue.transform.position + new Vector3(0f, 2.0f, 0f);
        }

        Camera camera = Camera.main;
        return camera != null
            ? camera.transform.position + new Vector3(0f, 1.6f, 10f)
            : new Vector3(0f, 1.6f, 0f);
    }

    private static string GetBrokenSpritePath(CatalogueBuildingId buildingId)
    {
        switch (buildingId)
        {
            case CatalogueBuildingId.Building2:
                return "Assets/File/TileMap/MapResources/Building/ZhaoGouQiao.jpg";
            case CatalogueBuildingId.Building3:
                return "Assets/File/TileMap/MapResources/Building/AnhuiWaterTowns_1.png";
            default:
                return "Assets/File/TileMap/MapResources/Building/FuJianTuLou.png";
        }
    }

    private static string GetRepairedSpritePath(CatalogueBuildingId buildingId)
    {
        switch (buildingId)
        {
            case CatalogueBuildingId.Building2:
                return "Assets/File/TileMap/MapResources/Building/Updata/ZhaoGouQiao.png";
            case CatalogueBuildingId.Building3:
                return "Assets/File/TileMap/MapResources/Building/Updata/ShuiXiang.png";
            default:
                return "Assets/File/TileMap/MapResources/Building/Updata/FuJianTuLou.png";
        }
    }
}
