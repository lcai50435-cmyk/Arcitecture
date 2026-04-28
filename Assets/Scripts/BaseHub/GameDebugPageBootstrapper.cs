using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public class GameDebugPageBootstrapper : MonoBehaviour
{
    private const string BaseSceneName = "NewBase";
    private const int DebugCanvasSortingOrder = 13050;
    private const float RefreshInterval = 0.2f;
    private const float ManualScrollStep = 0.08f;
    private const string RequiredDebugCharacters = "调试面板按住显示当前场景基地允许攻击生命上限耐久攻击力移动速度防御建筑结构通用材料武器墨水属性技能关闭开关预留版本穿透效果命中图鉴进度专用福建土楼赵州桥安徽水乡民居槽位完成总召唤怪物随机火石只已满相册留念一键清空榫卯斗拱梁架石基夯土瓦片扇形波次消耗射程攻速伤害值专材已修距倒计时暂停敌人TabEsc";
    private static readonly string[] DebugFontNames =
    {
        "Arial Unicode MS",
        "Arial Unicode",
        "Hiragino Sans GB",
        "PingFang SC",
        "Noto Sans CJK SC",
        "Helvetica",
        "Arial"
    };

    private static TMP_FontAsset debugFontAsset;

    private readonly Dictionary<ArchitecturalType, Sprite> skillIcons = new Dictionary<ArchitecturalType, Sprite>();

    private GameObject panelRoot;
    private RectTransform panelRectTransform;
    private ScrollRect debugScrollRect;
    private TextMeshProUGUI statusText;
    private float refreshTimer;

    public static bool IsAnyPanelOpen
    {
        get
        {
            GameDebugPageBootstrapper[] bootstrappers = FindObjectsOfType<GameDebugPageBootstrapper>(true);
            for (int i = 0; i < bootstrappers.Length; i++)
            {
                if (bootstrappers[i] != null &&
                    bootstrappers[i].panelRoot != null &&
                    bootstrappers[i].panelRoot.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }
    }

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
        if (!CanCreateInScene(scene.name)) return;
        if (FindObjectOfType<GameDebugPageBootstrapper>(true) != null) return;

        GameObject bootstrapper = new GameObject("RuntimeDebugPage");
        bootstrapper.AddComponent<GameDebugPageBootstrapper>().Build();
    }

    private static bool CanCreateInScene(string sceneName)
    {
        return !string.IsNullOrWhiteSpace(sceneName);
    }

    private void Update()
    {
        if (panelRoot == null) return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (panelRoot.activeSelf)
            {
                SetPanelVisible(false);
            }
            else if (CanOpenPanelFromHotkey(RuntimeUiInputGuard.IsBlockingGameplayUiOpen()))
            {
                SetPanelVisible(true);
            }
        }

        if (panelRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            SetPanelVisible(false);
        }

        if (!panelRoot.activeSelf) return;

        HandleScrollInput();

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = RefreshInterval;
            RefreshStatus();
        }
    }

    private void HandleScrollInput()
    {
        float scrollDelta = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(scrollDelta, 0f) ||
            panelRectTransform == null ||
            !RectTransformUtility.RectangleContainsScreenPoint(panelRectTransform, Input.mousePosition, null))
        {
            return;
        }

        ApplyManualScrollDelta(debugScrollRect, scrollDelta);
    }

    private static void ApplyManualScrollDelta(ScrollRect scrollRect, float scrollDelta)
    {
        if (!CanScroll(scrollRect))
        {
            return;
        }

        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
            scrollRect.verticalNormalizedPosition + scrollDelta * ManualScrollStep);
    }

    private static bool CanScroll(ScrollRect scrollRect)
    {
        return scrollRect != null &&
               scrollRect.content != null &&
               scrollRect.viewport != null &&
               scrollRect.content.rect.height > scrollRect.viewport.rect.height + 1f;
    }

    private void Build()
    {
        EnsureEventSystem();
        EnsureRuntimeSceneSystems();

        GameObject canvasObject = new GameObject("RuntimeDebugCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = DebugCanvasSortingOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        panelRoot = CreateUIObject("DebugPanel", canvasObject.transform);
        panelRectTransform = panelRoot.GetComponent<RectTransform>();
        panelRectTransform.anchorMin = new Vector2(1f, 0.5f);
        panelRectTransform.anchorMax = new Vector2(1f, 0.5f);
        panelRectTransform.pivot = new Vector2(1f, 0.5f);
        panelRectTransform.anchoredPosition = new Vector2(-24f, 0f);
        panelRectTransform.sizeDelta = new Vector2(560f, 820f);

        Image panelImage = panelRoot.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.08f, 0.07f, 0.90f);

        TextMeshProUGUI title = CreateText("Title", panelRoot.transform, "调试面板（Tab 开关，Esc 关闭）", 30, new Color(0.96f, 0.86f, 0.62f, 1f), TextAlignmentOptions.MidlineLeft);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(24f, -76f);
        titleRect.offsetMax = new Vector2(-24f, -18f);

        Transform content = CreateScrollContent(panelRoot.transform);
        BuildStatusSection(content);
        BuildLoadoutSection(content);
        BuildProgressSection(content);
        BuildAlbumSection(content);

        if (IsBaseScene())
        {
            BuildBaseSection(content);
        }
        else
        {
            BuildAttributeSection(content);
            BuildSkillSection(content);
            BuildTimeSection(content);
            BuildWorldSection(content);
        }

        panelRoot.SetActive(false);
    }

    private void SetPanelVisible(bool visible)
    {
        if (panelRoot == null) return;

        panelRoot.SetActive(visible);
        refreshTimer = 0f;
        if (visible)
        {
            RefreshStatus();
        }
    }

    private static bool CanOpenPanelFromHotkey(bool blockingGameplayUiOpen)
    {
        return true;
    }

    private void BuildStatusSection(Transform parent)
    {
        GameObject section = CreateSection(parent, "实时状态");
        statusText = CreateText("Status", section.transform, string.Empty, 19, new Color(0.88f, 0.86f, 0.78f, 1f), TextAlignmentOptions.TopLeft);
        statusText.overflowMode = TextOverflowModes.Truncate;
        LayoutElement layout = statusText.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 218f;
        RefreshStatus();
    }

    private void BuildLoadoutSection(Transform parent)
    {
        GameObject section = CreateSection(parent, "墨水方案");
        CreateActionRow(section.transform, "当前墨水",
            ("直墨", () => SetWeaponType(WeaponType.DirectInk)),
            ("爆墨", () => SetWeaponType(WeaponType.BurstInk)),
            ("贯墨", () => SetWeaponType(WeaponType.PierceInk)),
            ("流墨", () => SetWeaponType(WeaponType.FlowInk)));
    }

    private void BuildBaseSection(Transform parent)
    {
        GameObject section = CreateSection(parent, "基地调试");
        CreateActionRow(section.transform, "基地攻击",
            ("禁用", () => SetBaseAttackAllowed(false)),
            ("启用", () => SetBaseAttackAllowed(true)),
            ("默认关闭", ResetBaseAttackDefault));
    }

    private void BuildAttributeSection(Transform parent)
    {
        GameObject section = CreateSection(parent, "玩家属性");
        CreateActionRow(section.transform, "当前生命", ("-10", () => ChangeCurrentHp(-10f)), ("+10", () => ChangeCurrentHp(10f)), ("回满", FillHp));
        CreateActionRow(section.transform, "生命上限", ("-20", () => ChangeMaxHp(-20f)), ("+20", () => ChangeMaxHp(20f)), ("设为300", () => SetMaxHp(300f)));
        CreateActionRow(section.transform, "攻击力", ("-10", () => ChangeAttack(-10f)), ("+10", () => ChangeAttack(10f)), ("设为120", () => SetAttack(120f)));
        CreateActionRow(section.transform, "防御力", ("-5", () => ChangeDefense(-5f)), ("+5", () => ChangeDefense(5f)), ("设为30", () => SetDefense(30f)));
        CreateActionRow(section.transform, "移动速度", ("-0.5", () => ChangeMoveSpeed(-0.5f)), ("+0.5", () => ChangeMoveSpeed(0.5f)), ("设为7", () => SetMoveSpeed(7f)));
        CreateActionRow(section.transform, "墨水值", ("+20", () => ChangeInk(20f)), ("清空", () => SetInk(0f)), ("补满", () => SetInk(100f)));
    }

    private void BuildSkillSection(Transform parent)
    {
        GameObject section = CreateSection(parent, "临时构筑");
        CreateActionRow(section.transform, "专用结构", ("加1个", () => AddSpecialStructureToBackpack(1)), ("加3个", () => AddSpecialStructureToBackpack(3)), ("清空背包", ClearBackpack));
        CreateActionRow(section.transform, "斗拱", ("加1个", () => AddSkill(ArchitecturalType.Brackets, 1)), ("加2个", () => AddSkill(ArchitecturalType.Brackets, 2)), ("清空背包", ClearBackpack));
        CreateActionRow(section.transform, "榫卯", ("加1个", () => AddSkill(ArchitecturalType.MortiseAndTenonJoint, 1)), ("加2个", () => AddSkill(ArchitecturalType.MortiseAndTenonJoint, 2)));
        CreateActionRow(section.transform, "瓦片", ("加1个", () => AddSkill(ArchitecturalType.Tile, 1)), ("加2个", () => AddSkill(ArchitecturalType.Tile, 2)));
        CreateActionRow(section.transform, "夯土", ("加1个", () => AddSkill(ArchitecturalType.TampedEarth, 1)), ("加2个", () => AddSkill(ArchitecturalType.TampedEarth, 2)));
        CreateActionRow(section.transform, "石基", ("加1个", () => AddSkill(ArchitecturalType.GroundMass, 1)), ("加2个", () => AddSkill(ArchitecturalType.GroundMass, 2)));
        CreateActionRow(section.transform, "梁架", ("加1个", () => AddSkill(ArchitecturalType.BeamFrame, 1)), ("加2个", () => AddSkill(ArchitecturalType.BeamFrame, 2)));
    }

    private void BuildProgressSection(Transform parent)
    {
        GameObject section = CreateSection(parent, "图鉴进度");
        CreateActionRow(section.transform, "福建土楼", ("+25", () => AddBuildingProgress(CatalogueBuildingId.Building1, 25)), ("+100", () => AddBuildingProgress(CatalogueBuildingId.Building1, 100)));
        CreateActionRow(section.transform, "赵州桥", ("+25", () => AddBuildingProgress(CatalogueBuildingId.Building2, 25)), ("+100", () => AddBuildingProgress(CatalogueBuildingId.Building2, 100)));
        CreateActionRow(section.transform, "安徽民居", ("+25", () => AddBuildingProgress(CatalogueBuildingId.Building3, 25)), ("+100", () => AddBuildingProgress(CatalogueBuildingId.Building3, 100)));
        CreateActionRow(section.transform, "专用结构", ("+1", () => AddSpecialStructureToBackpack(1)), ("+3", () => AddSpecialStructureToBackpack(3)));
    }

    private void BuildAlbumSection(Transform parent)
    {
        GameObject section = CreateSection(parent, "相册调试");
        CreateActionRow(section.transform, "留念相册", ("一键清空", ClearPhotoAlbum));
    }

    private void BuildTimeSection(Transform parent)
    {
        GameObject section = CreateSection(parent, "时间与倒计时");
        CreateActionRow(section.transform, "游戏速度", ("0.25倍", () => Time.timeScale = 0.25f), ("1倍", () => Time.timeScale = 1f), ("2倍", () => Time.timeScale = 2f));
        CreateActionRow(section.transform, "倒计时", ("暂停", () => SetCountdownPaused(true)), ("继续", () => SetCountdownPaused(false)), ("+30秒", () => AddRemainTime(30f)));
        CreateActionRow(section.transform, "时间预设", ("设为30秒", () => SetRemainTime(30f)), ("设为5分钟", () => SetRemainTime(300f)), ("清零", () => SetRemainTime(0f)));
    }

    private void BuildWorldSection(Transform parent)
    {
        GameObject section = CreateSection(parent, "场景与敌人");
        CreateActionRow(section.transform, "玩家位置", ("回出生点", () => TeleportPlayer(Vector3.zero)), ("去图鉴", () => TeleportPlayer(new Vector3(123.5f, 18f, 0f))));
        CreateActionRow(section.transform, "敌人", ("清空敌人", ClearEnemies), ("敌人重置", ResetEnemies));
        CreateActionRow(section.transform, "召唤怪物", ("随机1只", () => SummonEnemies(null, 1)), ("火怪1只", () => SummonEnemies("FireMonster", 1)), ("石怪1只", () => SummonEnemies("StoneMonster", 1)));
        CreateActionRow(section.transform, "场景", ("回基地", GameSceneBaseReturnBootstrapper.SubmitCatalogueAndReturnToBase), ("刷新状态", RefreshStatus));
    }

    private void RefreshStatus()
    {
        if (statusText == null) return;

        EnsureRuntimeSceneSystems();

        CharacterCore core = GetPlayerCore();
        PlayerAttack attack = GetPlayerAttack();
        string activeSceneName = SceneManager.GetActiveScene().name;
        BackpackMananger backpack = ShouldEnsureBackpackForScene(activeSceneName)
            ? EnsureBackpackManager()
            : BackpackMananger.Instance;
        GameCountDownManager countdown = ShouldEnsureCountdownForScene(activeSceneName)
            ? EnsureCountdownManager()
            : null;
        WeaponType effectiveWeaponType = RuntimeWeaponTypeResolver.ResolveEffectiveWeaponType(backpack);
        InkAttackRuntimeConfig inkConfig = BuildEffectiveInkConfig(backpack, effectiveWeaponType);
        RuntimeProgressState runtimeState = RuntimeProgressState.EnsureInstance();
        string weaponSuffix = PlayerLoadoutRuntime.HasDebugWeaponOverride ? "（调试）" : string.Empty;
        int repairedCount = 0;
        int buildingCount = 0;
        foreach (BuildingDefinition definition in BuildingDefinitionLibrary.GetAll())
        {
            buildingCount++;
            if (runtimeState.IsBuildingRepaired(definition.buildingId))
            {
                repairedCount++;
            }
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"场景：{GetSceneDisplayName(SceneManager.GetActiveScene().name)}    速度：{Time.timeScale:0.##}倍");
        builder.AppendLine($"墨水：{GetWeaponDisplayName(effectiveWeaponType)}{weaponSuffix}    基地攻击：{(PlayerLoadoutRuntime.AllowBaseAttack ? "开启" : "关闭")}");
        builder.AppendLine(core != null
            ? $"生命：{core.currentHp:0}/{core.stats.maxHp:0}    攻击：{core.stats.attackDamage:0}    防御：{core.stats.defense:0}    移速：{core.stats.moveSpeed:0.0}"
            : "玩家属性：未找到 CharacterCore");
        int specialStructureCount = backpack != null ? backpack.GetSpecialStructureMaterialCount() : 0;
        builder.AppendLine($"图鉴：{runtimeState.GetTotalProgress()}/{runtimeState.GetTotalMaxProgress()}    专构：{specialStructureCount}/{BackpackMananger.MaxSpecialStructureMaterialCount}    已修：{repairedCount}/{buildingCount}");

        if (!IsBaseScene())
        {
            builder.AppendLine(attack != null
                ? $"墨水值：{attack.ink:0}/{attack.maxInk:0}    消耗：{inkConfig.inkCost}    攻速：{inkConfig.attackInterval:0.00}秒"
                : "攻击组件：未找到 PlayerAttack");
            builder.AppendLine($"弹道：{inkConfig.projectileCount}    波次：{inkConfig.burstShotCount}    尺寸：{inkConfig.projectileScale:0.00}    伤害：x{inkConfig.damageMultiplier:0.00}");
            builder.AppendLine(backpack != null
                ? $"背包：{backpack.GetOccupiedCount()}/{backpack.backpackItems.Count}    通材：{backpack.GetCommonMaterialCount()}/{BackpackMananger.MaxCommonMaterialCount}    速/距：x{inkConfig.speedMultiplier:0.00}/x{inkConfig.lifetimeMultiplier:0.00}"
                : "背包：未找到 BackpackMananger");
            builder.AppendLine(countdown != null
                ? $"倒计时：{countdown.GetRemainTime():0.0}秒    暂停：{(countdown.isInBase ? "是" : "否")}    敌人：{FindObjectsOfType<EnemyStatsManager>().Length}"
                : "倒计时：未找到 GameCountDownManager");
        }
        else
        {
            builder.AppendLine("提示：基地默认禁止攻击，可在下方临时开启测试。");
        }

        statusText.text = builder.ToString();
    }

    private void SetWeaponType(WeaponType weaponType)
    {
        PlayerLoadoutRuntime.SetDebugWeaponOverride(weaponType);
        RefreshEffectiveWeaponState();
        RefreshStatus();
    }

    private void SetBaseAttackAllowed(bool allowed)
    {
        PlayerLoadoutRuntime.AllowBaseAttack = allowed;
        RefreshStatus();
    }

    private void ResetBaseAttackDefault()
    {
        PlayerLoadoutRuntime.AllowBaseAttack = false;
        RefreshStatus();
    }

    private void ChangeCurrentHp(float delta)
    {
        CharacterCore core = GetPlayerCore();
        if (core == null) return;

        core.currentHp = Mathf.Clamp(core.currentHp + delta, 0f, core.stats.maxHp);
        RefreshHealthUi(core);
        RefreshStatus();
    }

    private void FillHp()
    {
        CharacterCore core = GetPlayerCore();
        if (core == null) return;

        core.currentHp = core.stats.maxHp;
        RefreshHealthUi(core);
        RefreshStatus();
    }

    private void ChangeMaxHp(float delta)
    {
        CharacterCore core = GetPlayerCore();
        if (core == null) return;

        SetMaxHp(core.stats.maxHp + delta);
    }

    private void SetMaxHp(float value)
    {
        CharacterCore core = GetPlayerCore();
        if (core == null) return;

        core.stats.maxHp = Mathf.Max(1f, value);
        core.currentHp = Mathf.Clamp(core.currentHp, 0f, core.stats.maxHp);
        RefreshHealthUi(core);
        RefreshStatus();
    }

    private void ChangeAttack(float delta)
    {
        CharacterCore core = GetPlayerCore();
        if (core == null) return;

        SetAttack(core.stats.attackDamage + delta);
    }

    private void SetAttack(float value)
    {
        CharacterCore core = GetPlayerCore();
        if (core == null) return;

        core.stats.attackDamage = Mathf.Max(0f, value);
        RefreshStatus();
    }

    private void ChangeDefense(float delta)
    {
        CharacterCore core = GetPlayerCore();
        if (core == null) return;

        SetDefense(core.stats.defense + delta);
    }

    private void SetDefense(float value)
    {
        CharacterCore core = GetPlayerCore();
        if (core == null) return;

        core.stats.defense = Mathf.Max(0f, value);
        RefreshStatus();
    }

    private void ChangeMoveSpeed(float delta)
    {
        CharacterCore core = GetPlayerCore();
        if (core == null) return;

        SetMoveSpeed(core.stats.moveSpeed + delta);
    }

    private void SetMoveSpeed(float value)
    {
        CharacterCore core = GetPlayerCore();
        if (core == null) return;

        core.stats.moveSpeed = Mathf.Max(0f, value);
        RefreshStatus();
    }

    private void ChangeInk(float delta)
    {
        PlayerAttack attack = GetPlayerAttack();
        if (attack == null) return;

        SetInk(attack.ink + delta);
    }

    private void SetInk(float value)
    {
        PlayerAttack attack = GetPlayerAttack();
        if (attack == null) return;

        attack.ink = Mathf.Clamp(value, 0f, attack.maxInk);
        attack.RefreshInkUI();

        RefreshStatus();
    }

    private void AddSkill(ArchitecturalType type, int count)
    {
        BackpackMananger backpack = EnsureBackpackManager();
        if (backpack == null) return;

        for (int i = 0; i < count; i++)
        {
            if (!backpack.PickItem(CreateDebugCrystal(type)))
            {
                Debug.LogWarning("背包已满，无法继续添加调试结构");
                break;
            }
        }

        RefreshBackpackUI();
        RefreshEffectiveWeaponState();
        RefreshStatus();
    }

    private ArchitecturalCrystal CreateDebugCrystal(ArchitecturalType type)
    {
        Sprite icon = GetSkillIcon(type);
        ArchitecturalCrystal crystal = ArchitecturalCrystalFactory.CreateCommonStructure(type, icon, icon);
        crystal.textDescription = GetSkillDescription(type);
        return crystal;
    }

    private void AddBuildingProgress(CatalogueBuildingId buildingId, int value)
    {
        if (value <= 0)
        {
            return;
        }

        RuntimeProgressState.EnsureInstance().AddBuildingProgress(buildingId, value, out _);
        RefreshStatus();
    }

    private void AddSpecialStructureToBackpack(int count)
    {
        if (count <= 0)
        {
            return;
        }

        BackpackMananger backpack = EnsureBackpackManager();
        if (backpack == null) return;

        for (int i = 0; i < count; i++)
        {
            if (!backpack.PickItem(ArchitecturalCrystalFactory.CreateSpecialStructureMaterial()))
            {
                Debug.LogWarning("专用结构已满或背包已满，无法继续添加");
                break;
            }
        }

        RefreshBackpackUI();
        GameplayStatusHudRuntime.RefreshStructureProgressText();
        RefreshStatus();
    }

    private void ClearBackpack()
    {
        BackpackMananger backpack = EnsureBackpackManager();
        if (backpack == null) return;

        backpack.ClearAllItems();
        RefreshBackpackUI();
        RefreshEffectiveWeaponState();
        RefreshStatus();
    }

    private void ClearPhotoAlbum()
    {
        PhotoAlbumRepository.ClearAll();
        RefreshOpenAlbumPanels();
    }

    private static void RefreshOpenAlbumPanels()
    {
        BaseHubAlbumPanel[] albumPanels = FindObjectsOfType<BaseHubAlbumPanel>(true);
        for (int i = 0; i < albumPanels.Length; i++)
        {
            if (albumPanels[i] != null && albumPanels[i].gameObject.activeInHierarchy)
            {
                albumPanels[i].Open();
            }
        }
    }

    private void SetCountdownPaused(bool paused)
    {
        GameCountDownManager countdown = EnsureCountdownManager();
        if (countdown == null) return;

        countdown.SetInBaseState(paused);
        RefreshStatus();
    }

    private void AddRemainTime(float value)
    {
        GameCountDownManager countdown = EnsureCountdownManager();
        if (countdown == null) return;

        countdown.DebugAddRemainTime(value);
        RefreshStatus();
    }

    private void SetRemainTime(float value)
    {
        GameCountDownManager countdown = EnsureCountdownManager();
        if (countdown == null) return;

        countdown.DebugSetRemainTime(value);
        RefreshStatus();
    }

    private void TeleportPlayer(Vector3 position)
    {
        GameObject player = FindPlayerObject();
        if (player == null) return;

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.velocity = Vector2.zero;
            body.position = position;
        }

        player.transform.position = position;
        RefreshStatus();
    }

    private void ClearEnemies()
    {
        EnemyStatsManager[] enemies = FindObjectsOfType<EnemyStatsManager>();
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
            {
                Destroy(enemies[i].gameObject);
            }
        }

        RefreshStatus();
    }

    private void ResetEnemies()
    {
        EnemyStatsManager[] enemies = FindObjectsOfType<EnemyStatsManager>();
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
            {
                enemies[i].ResetState();
                enemies[i].ResolvePlayerTargetIfMissing();
            }
        }

        RefreshStatus();
    }

    private void SummonEnemies(string enemyKeyword, int count)
    {
        if (count <= 0)
        {
            return;
        }

        RunStageDirector director = FindObjectOfType<RunStageDirector>();
        if (director == null)
        {
            Debug.LogWarning("当前场景未找到 RunStageDirector，无法召唤怪物");
            return;
        }

        if (!director.DebugSpawnEnemy(enemyKeyword, count))
        {
            string enemyDisplayName = string.IsNullOrWhiteSpace(enemyKeyword) ? "随机怪物" : enemyKeyword;
            Debug.LogWarning($"未能召唤{enemyDisplayName}，请确认场景里已有对应敌人模板");
            return;
        }

        RefreshStatus();
    }

    private CharacterCore GetPlayerCore()
    {
        if (PlayerAttributeManager.Instance != null && PlayerAttributeManager.Instance.characterCore != null)
        {
            return PlayerAttributeManager.Instance.characterCore;
        }

        GameObject player = FindPlayerObject();
        return player != null ? player.GetComponent<CharacterCore>() : null;
    }

    private PlayerAttack GetPlayerAttack()
    {
        GameObject player = FindPlayerObject();
        return player != null ? player.GetComponent<PlayerAttack>() : null;
    }

    private static void RefreshHealthUi(CharacterCore core)
    {
        if (core == null || core.stats == null)
        {
            return;
        }

        PlayerTakeDamage playerTakeDamage = core.GetComponent<PlayerTakeDamage>();
        ValueTrans healthTrans = playerTakeDamage != null ? playerTakeDamage.healthTrans : null;
        healthTrans = GameplayStatusHudRuntime.EnsureHealthGauge(healthTrans);

        if (playerTakeDamage != null)
        {
            playerTakeDamage.healthTrans = healthTrans;
        }

        if (healthTrans != null)
        {
            healthTrans.SetMaxValue(core.stats.maxHp);
            healthTrans.SetValue(core.currentHp);
        }

        GameplayStatusHudRuntime.RefreshHealthText(core.currentHp, core.stats.maxHp);
    }

    private GameObject FindPlayerObject()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) return player;

        CharacterCore core = FindObjectOfType<CharacterCore>();
        return core != null ? core.gameObject : null;
    }

    private void RefreshBackpackUI()
    {
        BackpackUI backpackUI = FindObjectOfType<BackpackUI>();
        if (backpackUI != null)
        {
            backpackUI.RefreshUI();
        }
    }

    private Sprite GetSkillIcon(ArchitecturalType type)
    {
        if (skillIcons.TryGetValue(type, out Sprite sprite))
        {
            return sprite;
        }

        sprite = CreateSolidSprite(GetSkillColor(type));
        skillIcons[type] = sprite;
        return sprite;
    }

    private static string GetSkillDescription(ArchitecturalType type)
    {
        switch (type)
        {
            case ArchitecturalType.Brackets:
                return "斗拱：追加攻击波次，最多连续发出 3 波。";
            case ArchitecturalType.MortiseAndTenonJoint:
                return "榫卯：按扇形发射墨迹，最多 6 发。";
            case ArchitecturalType.Tile:
                return "瓦片：放大墨水弹体积，并降低消耗。";
            case ArchitecturalType.TampedEarth:
                return "夯土：提升墨水弹射程与速度。";
            case ArchitecturalType.GroundMass:
                return "石基：提升墨水弹体积与伤害。";
            case ArchitecturalType.BeamFrame:
                return "梁架：提高攻击速度，最低 0.4 秒。";
            default:
                return $"{type}：调试结构。";
        }
    }

    private static Color GetSkillColor(ArchitecturalType type)
    {
        switch (type)
        {
            case ArchitecturalType.Brackets:
                return new Color(0.78f, 0.34f, 0.24f, 1f);
            case ArchitecturalType.MortiseAndTenonJoint:
                return new Color(0.54f, 0.42f, 0.24f, 1f);
            case ArchitecturalType.Tile:
                return new Color(0.38f, 0.50f, 0.62f, 1f);
            case ArchitecturalType.TampedEarth:
                return new Color(0.46f, 0.36f, 0.27f, 1f);
            case ArchitecturalType.GroundMass:
                return new Color(0.36f, 0.42f, 0.39f, 1f);
            case ArchitecturalType.BeamFrame:
                return new Color(0.38f, 0.60f, 0.48f, 1f);
            default:
                return new Color(0.80f, 0.72f, 0.50f, 1f);
        }
    }

    private Transform CreateScrollContent(Transform parent)
    {
        GameObject scrollObject = CreateUIObject("ScrollView", parent);
        SetStretch(scrollObject.GetComponent<RectTransform>(), 18f, 92f, 18f, 20f);
        ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 32f;

        GameObject viewport = CreateUIObject("Viewport", scrollObject.transform);
        SetStretch(viewport.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 14f;
        layout.padding = new RectOffset(0, 12, 0, 0);
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = contentRect;
        debugScrollRect = scrollRect;

        return content.transform;
    }

    private GameObject CreateSection(Transform parent, string title)
    {
        GameObject section = CreateUIObject(title, parent);
        VerticalLayoutGroup layout = section.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = section.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI titleText = CreateText("SectionTitle", section.transform, title, 26, new Color(0.96f, 0.82f, 0.48f, 1f), TextAlignmentOptions.MidlineLeft);
        LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 34f;

        return section;
    }

    private void CreateActionRow(Transform parent, string label, params (string Label, Action Action)[] actions)
    {
        GameObject row = CreateUIObject(label, parent);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        layout.childAlignment = TextAnchor.MiddleLeft;

        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 46f;

        TextMeshProUGUI labelText = CreateText("Label", row.transform, label, 20, new Color(0.88f, 0.86f, 0.78f, 1f), TextAlignmentOptions.MidlineLeft);
        LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 96f;

        for (int i = 0; i < actions.Length; i++)
        {
            Action action = actions[i].Action;
            Button button = CreateButton(actions[i].Label, row.transform, actions[i].Label, new Color(0.24f, 0.21f, 0.17f, 0.96f), 20);
            LayoutElement buttonLayout = button.gameObject.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = 122f;
            button.onClick.AddListener(() => action?.Invoke());
        }
    }

    private static bool IsBaseScene()
    {
        return IsBaseScene(SceneManager.GetActiveScene().name);
    }

    private static bool IsBaseScene(string sceneName)
    {
        return string.Equals(sceneName, BaseSceneName, StringComparison.Ordinal);
    }

    private static bool IsGameplayScene()
    {
        return IsGameplayScene(SceneManager.GetActiveScene().name);
    }

    private static bool IsGameplayScene(string sceneName)
    {
        return GameplayStageCatalog.IsGameplayScene(sceneName);
    }

    private static bool ShouldEnsureBackpackForScene(string sceneName)
    {
        return IsBaseScene(sceneName) || IsGameplayScene(sceneName);
    }

    private static bool ShouldEnsureCountdownForScene(string sceneName)
    {
        return IsGameplayScene(sceneName);
    }

    private static void EnsureRuntimeSceneSystems()
    {
        string activeSceneName = SceneManager.GetActiveScene().name;
        if (ShouldEnsureBackpackForScene(activeSceneName))
        {
            EnsureBackpackManager();
        }

        if (ShouldEnsureCountdownForScene(activeSceneName))
        {
            EnsureCountdownManager();
        }
    }

    private static InkAttackRuntimeConfig BuildEffectiveInkConfig(BackpackMananger backpack, WeaponType weaponType)
    {
        return WeaponAttackProfile
            .FromWeaponType(weaponType)
            .ApplyToInkConfig(InkModifierRuntimeConfig.BuildFromBackpack(backpack));
    }

    private void RefreshEffectiveWeaponState()
    {
        BackpackMananger backpack = EnsureBackpackManager();
        WeaponType effectiveWeaponType = RuntimeWeaponTypeResolver.ResolveEffectiveWeaponType(backpack);

        PlayerProfileData profile = FindObjectOfType<PlayerProfileData>();
        if (profile != null)
        {
            profile.SetEffectiveWeapon(effectiveWeaponType);
        }

        WeaponSelectionPanelUI panel = FindObjectOfType<WeaponSelectionPanelUI>(true);
        if (panel != null)
        {
            panel.RefreshSelected();
        }

        if (PlayerAttributeManager.Instance != null)
        {
            PlayerAttributeManager.Instance.ApplyAllBonus();
        }

        PlayerAttack attack = GetPlayerAttack();
        if (attack != null)
        {
            attack.RefreshInkUI();
        }
    }

    private static BackpackMananger EnsureBackpackManager()
    {
        if (BackpackMananger.Instance != null)
        {
            EnsureBackpackCapacity(BackpackMananger.Instance);
            return BackpackMananger.Instance;
        }

        BackpackMananger manager = FindObjectOfType<BackpackMananger>(true);
        if (manager == null)
        {
            GameObject managerObject = new GameObject("RuntimeBackpackManager");
            manager = managerObject.AddComponent<BackpackMananger>();
        }
        else
        {
            BackpackMananger.Instance = manager;
            EnsureBackpackCapacity(manager);
        }

        return manager;
    }

    private static void EnsureBackpackCapacity(BackpackMananger manager)
    {
        if (manager == null || manager.backpackItems == null)
        {
            return;
        }

        while (manager.backpackItems.Count < 6)
        {
            manager.backpackItems.Add(null);
        }
    }

    private static GameCountDownManager EnsureCountdownManager()
    {
        if (GameCountDownManager.Instance != null)
        {
            BindCountdownTextIfMissing(GameCountDownManager.Instance);
            return GameCountDownManager.Instance;
        }

        GameCountDownManager manager = FindObjectOfType<GameCountDownManager>(true);
        if (manager == null)
        {
            GameObject managerObject = new GameObject("RuntimeGameCountDownManager");
            manager = managerObject.AddComponent<GameCountDownManager>();
            manager.isInBase = IsBaseScene();
        }
        else
        {
            GameCountDownManager.Instance = manager;
        }

        BindCountdownTextIfMissing(manager);
        return manager;
    }

    private static void BindCountdownTextIfMissing(GameCountDownManager manager)
    {
        if (manager == null || manager.timer != null)
        {
            return;
        }

        TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TextMeshProUGUI text = texts[i];
            if (text == null)
            {
                continue;
            }

            if (string.Equals(text.gameObject.name, "Timer", StringComparison.Ordinal) ||
                string.Equals(text.transform.parent != null ? text.transform.parent.gameObject.name : string.Empty, "CountDownTimer", StringComparison.Ordinal))
            {
                manager.timer = text;
                return;
            }
        }
    }

    private static string GetSceneDisplayName(string sceneName)
    {
        if (sceneName == BaseSceneName)
        {
            return "基地";
        }

        GameplayStageDefinition stage = GameplayStageCatalog.GetStageByScene(sceneName);
        return stage != null ? stage.displayName : sceneName;
    }

    private static string GetWeaponDisplayName(WeaponType weaponType)
    {
        return InkTypeCatalog.GetDisplayName(weaponType);
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static Button CreateButton(string name, Transform parent, string label, Color color, float fontSize = 22f)
    {
        GameObject buttonObject = CreateUIObject(name, parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = color;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI text = CreateText("Text", buttonObject.transform, label, fontSize, Color.white, TextAlignmentOptions.Center);
        SetStretch(text.rectTransform, 0f, 0f, 0f, 0f);
        return button;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateUIObject(name, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = GetDebugFont();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        return text;
    }

    private static TMP_FontAsset GetDebugFont()
    {
        if (debugFontAsset != null)
        {
            return debugFontAsset;
        }

        Font[] loadedFonts = Resources.FindObjectsOfTypeAll<Font>();
        for (int i = 0; i < loadedFonts.Length; i++)
        {
            Font font = loadedFonts[i];
            if (font == null)
            {
                continue;
            }

            string fontName = font.name ?? string.Empty;
            if (!fontName.Contains("NotoSansSC") && !fontName.Contains("Noto Sans SC"))
            {
                continue;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (fontAsset == null)
            {
                continue;
            }

            fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset>();
            fontAsset.TryAddCharacters(RequiredDebugCharacters);
            debugFontAsset = fontAsset;
            return debugFontAsset;
        }

        TMP_FontAsset[] loadedFontAssets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < loadedFontAssets.Length; i++)
        {
            TMP_FontAsset fontAsset = loadedFontAssets[i];
            if (fontAsset == null) continue;
            if (!fontAsset.name.Contains("NotoSansSC")) continue;
            if (!fontAsset.HasCharacters(RequiredDebugCharacters))
            {
                continue;
            }

            debugFontAsset = fontAsset;
            return debugFontAsset;
        }

        for (int i = 0; i < DebugFontNames.Length; i++)
        {
            Font font;
            try
            {
                font = Font.CreateDynamicFontFromOSFont(DebugFontNames[i], 90);
            }
            catch (Exception)
            {
                continue;
            }

            if (font == null)
            {
                continue;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (fontAsset == null)
            {
                continue;
            }

            fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset>();
            fontAsset.TryAddCharacters(RequiredDebugCharacters);
            debugFontAsset = fontAsset;
            return debugFontAsset;
        }

        debugFontAsset = TMP_Settings.defaultFontAsset;
        return debugFontAsset;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static void SetStretch(RectTransform rect, float left, float top, float right, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static Sprite CreateSolidSprite(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }
}
