using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum RunStagePhase
{
    Basic,
    Mid,
    HighRisk
}

[Serializable]
public class RunStageConfig
{
    public RunStagePhase phase;
    public float elapsedStartTime;
    public int enemyHp;
    public int enemyAttack;
    public float enemyDefense;
    public float enemySpeed;
    public float spawnInterval;
    public int targetAliveCount;
}

[Serializable]
public class DropTableConfig
{
    [Range(0f, 1f)] public float commonStructureProbability = 0.6f;
    [Range(0f, 1f)] public float inkSupplyProbability = 0.25f;
    [Range(0f, 1f)] public float specialStructureProbability = 0.15f;
}

public class RunStageDirector : MonoBehaviour
{
    private const string FireMonsterResourcePath = "EnemyPrefab/FireMonster";
    private const string StoneMonsterResourcePath = "EnemyPrefab/StoneMonster";
#if UNITY_EDITOR
    private const string FireMonsterPrefabPath = "Assets/File/Prefab/EnemyPrefab/FireMonster.prefab";
    private const string StoneMonsterPrefabPath = "Assets/File/Prefab/EnemyPrefab/StoneMonster.prefab";
#endif
    private const float StageRefreshInterval = 0.2f;
    private const float SpawnProbeRadius = 3.5f;
    private const int SpawnProbeAttempts = 16;
    private static readonly Vector2 SpawnProbeSize = new Vector2(0.55f, 0.55f);
    private static readonly string[] FallbackBlockedKeywords = { "Water", "Obstacle", "Building" };

    private static readonly ArchitecturalType[] CommonStructureTypes =
    {
        ArchitecturalType.Brackets,
        ArchitecturalType.MortiseAndTenonJoint,
        ArchitecturalType.Tile,
        ArchitecturalType.TampedEarth,
        ArchitecturalType.GroundMass,
        ArchitecturalType.BeamFrame
    };

    private readonly List<EnemySpawnTemplate> spawnTemplates = new List<EnemySpawnTemplate>();
    private readonly List<RunStageConfig> stageConfigs = new List<RunStageConfig>();

    [SerializeField] private DropTableConfig dropTable = new DropTableConfig();

    private GameCountDownManager countdownManager;
    private RunStageConfig currentStage;
    private ResolvedStageState currentStageState;
    private float spawnTimer;
    private float stageRefreshTimer;
    private bool countdownFinishedHandled;
    private bool runtimeSuspended;

    public static float ActiveCameraTension { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        ActiveCameraTension = 0f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
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
        if (!GameplayStageCatalog.IsGameplayScene(scene.name))
        {
            return;
        }

        if (FindObjectOfType<RunStageDirector>() != null)
        {
            return;
        }

        GameObject directorObject = new GameObject("RunStageDirector");
        directorObject.AddComponent<RunStageDirector>();
    }

    private void Awake()
    {
        EnsureStageConfigs();
    }

    private void Start()
    {
        EnsureStageConfigs();
        RuntimeMiniMapHud.EnsureInstance();
        GameplayStatusHudRuntime.EnsureHealthGauge(null);
        GameplayStatusHudRuntime.EnsureWeaponGauge(null);
        BindCountdownManager();
        CaptureExistingEnemiesAsTemplates();

        float elapsedTime = GetElapsedTime();
        ApplyStage(GetStageForElapsed(elapsedTime));
        RefreshResolvedStage(elapsedTime, true);
    }

    private void Update()
    {
        if (runtimeSuspended)
        {
            return;
        }

        if (countdownManager == null)
        {
            BindCountdownManager();
            if (countdownManager == null)
            {
                return;
            }
        }

        if (countdownManager.HasFinished)
        {
            HandleCountdownFinished();
            return;
        }

        if (countdownManager.isInBase)
        {
            return;
        }

        EnsureStageConfigs();
        float elapsedTime = GetElapsedTime();
        RunStageConfig nextStage = GetStageForElapsed(elapsedTime);
        if (nextStage != currentStage)
        {
            ApplyStage(nextStage);
            RefreshResolvedStage(elapsedTime, true);
        }
        else
        {
            stageRefreshTimer += Time.deltaTime;
            if (currentStageState == null || stageRefreshTimer >= StageRefreshInterval)
            {
                RefreshResolvedStage(elapsedTime);
            }
        }

        spawnTimer += Time.deltaTime;
        if (currentStageState != null && spawnTimer >= currentStageState.spawnInterval)
        {
            spawnTimer = 0f;

            if (GetAliveEnemyCount() < currentStageState.targetAliveCount)
            {
                SpawnOneEnemy();
            }
        }
    }

    private void OnDestroy()
    {
        if (countdownManager != null)
        {
            countdownManager.OnCountdownFinished -= HandleCountdownFinished;
        }

        ActiveCameraTension = 0f;
    }

    public void HandleEnemyDeath(Vector3 position)
    {
        if (runtimeSuspended)
        {
            return;
        }

        SpawnDrop(position);
    }

    public static bool TryTriggerPickupAmbush(Vector3 pickupPosition)
    {
        RunStageDirector director = FindObjectOfType<RunStageDirector>();
        return director != null && director.TrySpawnPickupAmbush(pickupPosition);
    }

    public bool DebugSpawnEnemy(string enemyKeyword = null, int count = 1)
    {
        if (runtimeSuspended || count <= 0)
        {
            return false;
        }

        CaptureExistingEnemiesAsTemplates();
        if (spawnTemplates.Count == 0)
        {
            return false;
        }

        RefreshResolvedStage(GetElapsedTime(), true);

        bool spawnedAny = false;
        for (int i = 0; i < count; i++)
        {
            if (TrySpawnDebugEnemy(enemyKeyword))
            {
                spawnedAny = true;
            }
        }

        return spawnedAny;
    }

    public void SuspendRuntime()
    {
        runtimeSuspended = true;
    }

    public void ResumeRuntime()
    {
        runtimeSuspended = false;
    }

    private void EnsureStageConfigs()
    {
        if (stageConfigs.Count > 0)
        {
            return;
        }

        stageConfigs.Add(new RunStageConfig
        {
            phase = RunStagePhase.Basic,
            elapsedStartTime = 0f,
            enemyHp = 100,
            enemyAttack = 10,
            enemyDefense = 0f,
            enemySpeed = 0.8f,
            spawnInterval = 2f,
            targetAliveCount = 4
        });
        stageConfigs.Add(new RunStageConfig
        {
            phase = RunStagePhase.Mid,
            elapsedStartTime = 180f,
            enemyHp = 130,
            enemyAttack = 13,
            enemyDefense = 2f,
            enemySpeed = 1f,
            spawnInterval = 1.5f,
            targetAliveCount = 6
        });
        stageConfigs.Add(new RunStageConfig
        {
            phase = RunStagePhase.HighRisk,
            elapsedStartTime = 240f,
            enemyHp = 180,
            enemyAttack = 18,
            enemyDefense = 4f,
            enemySpeed = 1.2f,
            spawnInterval = 1f,
            targetAliveCount = 8
        });
    }

    private void BindCountdownManager()
    {
        bool shouldHoldForIntro = GameplayStageIntroDirector.IsIntroActive;

        countdownManager = GameCountDownManager.Instance != null
            ? GameCountDownManager.Instance
            : FindObjectOfType<GameCountDownManager>();

        if (countdownManager == null)
        {
            GameObject managerObject = new GameObject("RuntimeGameCountDownManager");
            countdownManager = managerObject.AddComponent<GameCountDownManager>();
        }

        countdownManager.totalTime = 300f;
        countdownManager.DebugSetRemainTime(300f);
        countdownManager.SetInBaseState(shouldHoldForIntro);
        countdownManager.OnCountdownFinished -= HandleCountdownFinished;
        countdownManager.OnCountdownFinished += HandleCountdownFinished;

        runtimeSuspended = shouldHoldForIntro;
    }

    private void CaptureExistingEnemiesAsTemplates()
    {
        if (spawnTemplates.Count > 0)
        {
            return;
        }

        EnemyStatsManager[] existingEnemies = FindObjectsOfType<EnemyStatsManager>(true);
        for (int i = 0; i < existingEnemies.Length; i++)
        {
            EnemyStatsManager enemy = existingEnemies[i];
            if (enemy == null)
            {
                continue;
            }

            AddSpawnTemplate(enemy.gameObject, enemy.transform.position, enemy.transform.rotation);
            PrepareEnemyInstance(enemy.gameObject);
        }

        if (spawnTemplates.Count == 0)
        {
            CaptureFallbackEnemyPrefabsAsTemplates();
        }
    }

    private void CaptureFallbackEnemyPrefabsAsTemplates()
    {
        Vector3 fallbackPosition = ResolveFallbackTemplatePosition();
        AddFallbackEnemyTemplate(FireMonsterResourcePath, GetEditorFireMonsterPrefabPath(), fallbackPosition);
        AddFallbackEnemyTemplate(StoneMonsterResourcePath, GetEditorStoneMonsterPrefabPath(), fallbackPosition);
    }

    private void AddFallbackEnemyTemplate(string resourcePath, string editorAssetPath, Vector3 fallbackPosition)
    {
        GameObject prefab = LoadFallbackEnemyPrefab(resourcePath, editorAssetPath);
        if (prefab == null || prefab.GetComponent<EnemyStatsManager>() == null)
        {
            return;
        }

        AddSpawnTemplate(prefab, fallbackPosition, prefab.transform.rotation);
    }

    private void AddSpawnTemplate(GameObject source, Vector3 position, Quaternion rotation)
    {
        if (source == null)
        {
            return;
        }

        GameObject templateObject = Instantiate(source, position, rotation, transform);
        templateObject.name = $"{source.name}_Template";
        templateObject.SetActive(false);

        spawnTemplates.Add(new EnemySpawnTemplate
        {
            template = templateObject,
            position = position,
            rotation = rotation
        });
    }

    private static Vector3 ResolveFallbackTemplatePosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player.transform.position;
        }

        CharacterCore playerCore = FindObjectOfType<CharacterCore>();
        return playerCore != null ? playerCore.transform.position : Vector3.zero;
    }

    private static GameObject LoadFallbackEnemyPrefab(string resourcePath, string editorAssetPath)
    {
        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab != null)
        {
            return prefab;
        }

#if UNITY_EDITOR
        return !string.IsNullOrWhiteSpace(editorAssetPath)
            ? AssetDatabase.LoadAssetAtPath<GameObject>(editorAssetPath)
            : null;
#else
        return null;
#endif
    }

    private static string GetEditorFireMonsterPrefabPath()
    {
#if UNITY_EDITOR
        return FireMonsterPrefabPath;
#else
        return null;
#endif
    }

    private static string GetEditorStoneMonsterPrefabPath()
    {
#if UNITY_EDITOR
        return StoneMonsterPrefabPath;
#else
        return null;
#endif
    }

    private void PrepareEnemyInstance(GameObject enemyObject)
    {
        if (enemyObject == null)
        {
            return;
        }

        ResolvedStageState stageState = currentStageState ?? ResolveStageState(GetElapsedTime());
        ApplyStageToEnemy(enemyObject, stageState);

        RunStageEnemyBinding binding = enemyObject.GetComponent<RunStageEnemyBinding>();
        if (binding == null)
        {
            binding = enemyObject.AddComponent<RunStageEnemyBinding>();
        }

        binding.Configure(this);

        if (enemyObject.GetComponent<EnemyCombatFeedback>() == null)
        {
            enemyObject.AddComponent<EnemyCombatFeedback>();
        }

        NightLightingController.EnsureProjectedShadow(enemyObject);
        NightLightingController.EnsureGameplayEnemyLight(enemyObject);
    }

    private void ApplyStage(RunStageConfig stage)
    {
        if (stage == null)
        {
            return;
        }

        currentStage = stage;
        ActiveCameraTension = ResolveCameraTension(stage.phase);
        spawnTimer = 0f;
        stageRefreshTimer = 0f;
    }

    private static float ResolveCameraTension(RunStagePhase phase)
    {
        switch (phase)
        {
            case RunStagePhase.Mid:
                return 0.45f;
            case RunStagePhase.HighRisk:
                return 1f;
            default:
                return 0f;
        }
    }

    private void RefreshResolvedStage(float elapsedTime, bool force = false)
    {
        if (!force && stageRefreshTimer < StageRefreshInterval)
        {
            return;
        }

        stageRefreshTimer = 0f;
        currentStageState = ResolveStageState(elapsedTime);

        EnemyStatsManager[] aliveEnemies = FindObjectsOfType<EnemyStatsManager>();
        for (int i = 0; i < aliveEnemies.Length; i++)
        {
            ApplyStageToEnemy(aliveEnemies[i].gameObject, currentStageState);
        }
    }

    private ResolvedStageState ResolveStageState(float elapsedTime)
    {
        RunStageConfig stage = GetStageForElapsed(elapsedTime);
        RunStageConfig nextStage = GetNextStage(stage);
        float progress = GetStageProgress(stage, nextStage, elapsedTime);

        if (stage == null)
        {
            return null;
        }

        return new ResolvedStageState
        {
            phase = stage.phase,
            enemyHp = Mathf.RoundToInt(Mathf.Lerp(stage.enemyHp, nextStage != null ? nextStage.enemyHp : stage.enemyHp, progress)),
            enemyAttack = Mathf.Lerp(stage.enemyAttack, nextStage != null ? nextStage.enemyAttack : stage.enemyAttack, progress),
            enemyDefense = Mathf.Lerp(stage.enemyDefense, nextStage != null ? nextStage.enemyDefense : stage.enemyDefense, progress),
            enemySpeed = Mathf.Lerp(stage.enemySpeed, nextStage != null ? nextStage.enemySpeed : stage.enemySpeed, progress),
            spawnInterval = Mathf.Lerp(stage.spawnInterval, nextStage != null ? nextStage.spawnInterval : stage.spawnInterval, progress),
            targetAliveCount = Mathf.RoundToInt(Mathf.Lerp(stage.targetAliveCount, nextStage != null ? nextStage.targetAliveCount : stage.targetAliveCount, progress))
        };
    }

    private RunStageConfig GetNextStage(RunStageConfig stage)
    {
        if (stage == null)
        {
            return null;
        }

        int stageIndex = stageConfigs.IndexOf(stage);
        if (stageIndex < 0 || stageIndex >= stageConfigs.Count - 1)
        {
            return null;
        }

        return stageConfigs[stageIndex + 1];
    }

    private float GetStageProgress(RunStageConfig stage, RunStageConfig nextStage, float elapsedTime)
    {
        if (stage == null || nextStage == null)
        {
            return 0f;
        }

        float duration = nextStage.elapsedStartTime - stage.elapsedStartTime;
        if (duration <= Mathf.Epsilon)
        {
            return 1f;
        }

        return Mathf.Clamp01((elapsedTime - stage.elapsedStartTime) / duration);
    }

    private void ApplyStageToEnemy(GameObject enemyObject, ResolvedStageState stage)
    {
        if (enemyObject == null || stage == null)
        {
            return;
        }

        if (RuntimeTestStoneMonsterSpawner.TryPreserveHealthOverride(enemyObject))
        {
            return;
        }

        CharacterCore core = enemyObject.GetComponent<CharacterCore>();
        if (core == null)
        {
            return;
        }

        if (core.stats == null)
        {
            core.stats = new CharacterStats();
        }

        float previousMaxHp = Mathf.Max(1f, core.stats.maxHp);
        float healthRatio = Mathf.Clamp01(core.currentHp / previousMaxHp);
        if (core.currentHp <= 0f)
        {
            healthRatio = 1f;
        }

        core.stats.maxHp = stage.enemyHp;
        core.stats.attackDamage = stage.enemyAttack;
        core.stats.defense = stage.enemyDefense;
        core.stats.moveSpeed = stage.enemySpeed;
        core.currentHp = Mathf.Clamp(stage.enemyHp * healthRatio, 0f, stage.enemyHp);
    }

    private void SpawnOneEnemy()
    {
        if (spawnTemplates.Count == 0)
        {
            return;
        }

        int startIndex = UnityEngine.Random.Range(0, spawnTemplates.Count);
        for (int i = 0; i < spawnTemplates.Count; i++)
        {
            EnemySpawnTemplate template = spawnTemplates[(startIndex + i) % spawnTemplates.Count];
            if (template.template == null)
            {
                continue;
            }

            if (!TryResolveSpawnPosition(template, out Vector3 spawnPosition))
            {
                continue;
            }

            SpawnEnemyFromTemplate(template, spawnPosition);
            return;
        }
    }

    private bool TryResolveSpawnPosition(EnemySpawnTemplate template, out Vector3 spawnPosition)
    {
        EnemyAvoidObstacle avoidObstacle = template.template.GetComponent<EnemyAvoidObstacle>();
        if (TryGetValidSpawnPoint(template.position, template.position, template.position.z, avoidObstacle, out spawnPosition))
        {
            return true;
        }

        for (int attempt = 0; attempt < SpawnProbeAttempts; attempt++)
        {
            Vector2 candidate = (Vector2)template.position + UnityEngine.Random.insideUnitCircle * SpawnProbeRadius;
            if (TryGetValidSpawnPoint(candidate, template.position, template.position.z, avoidObstacle, out spawnPosition))
            {
                return true;
            }
        }

        spawnPosition = default;
        return false;
    }

    private bool TrySpawnPickupAmbush(Vector3 pickupPosition)
    {
        if (runtimeSuspended)
        {
            return false;
        }

        CaptureExistingEnemiesAsTemplates();
        if (spawnTemplates.Count == 0)
        {
            return false;
        }

        float elapsedTime = GetElapsedTime();
        RefreshResolvedStage(elapsedTime, true);

        int startIndex = UnityEngine.Random.Range(0, spawnTemplates.Count);
        for (int i = 0; i < spawnTemplates.Count; i++)
        {
            EnemySpawnTemplate template = spawnTemplates[(startIndex + i) % spawnTemplates.Count];
            if (template.template == null)
            {
                continue;
            }

            if (!TryResolvePickupAmbushPosition(template, pickupPosition, out Vector3 spawnPosition))
            {
                continue;
            }

            SpawnEnemyFromTemplate(template, spawnPosition);
            return true;
        }

        return false;
    }

    private bool TrySpawnDebugEnemy(string enemyKeyword)
    {
        int startIndex = UnityEngine.Random.Range(0, spawnTemplates.Count);
        bool hasSpawnAnchor = TryGetDebugSpawnAnchor(out Vector3 spawnAnchor);

        for (int i = 0; i < spawnTemplates.Count; i++)
        {
            EnemySpawnTemplate template = spawnTemplates[(startIndex + i) % spawnTemplates.Count];
            if (template.template == null || !TemplateMatchesKeyword(template, enemyKeyword))
            {
                continue;
            }

            bool resolved = hasSpawnAnchor
                ? TryResolvePickupAmbushPosition(template, spawnAnchor, out Vector3 spawnPosition)
                : TryResolveSpawnPosition(template, out spawnPosition);

            if (!resolved)
            {
                continue;
            }

            SpawnEnemyFromTemplate(template, spawnPosition);
            return true;
        }

        return false;
    }

    private bool TryGetDebugSpawnAnchor(out Vector3 spawnAnchor)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            CharacterCore playerCore = FindObjectOfType<CharacterCore>();
            player = playerCore != null ? playerCore.gameObject : null;
        }

        if (player == null)
        {
            spawnAnchor = default;
            return false;
        }

        spawnAnchor = player.transform.position;
        return true;
    }

    private static bool TemplateMatchesKeyword(EnemySpawnTemplate template, string enemyKeyword)
    {
        if (template == null || template.template == null || string.IsNullOrWhiteSpace(enemyKeyword))
        {
            return true;
        }

        return template.template.name.IndexOf(enemyKeyword, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void SpawnEnemyFromTemplate(EnemySpawnTemplate template, Vector3 spawnPosition)
    {
        GameObject enemyObject = Instantiate(template.template, spawnPosition, template.rotation);
        enemyObject.name = template.template.name.Replace("_Template", string.Empty);
        enemyObject.SetActive(true);
        PrepareEnemyInstance(enemyObject);
    }

    private bool TryResolvePickupAmbushPosition(EnemySpawnTemplate template, Vector3 pickupPosition, out Vector3 spawnPosition)
    {
        EnemyAvoidObstacle avoidObstacle = template.template.GetComponent<EnemyAvoidObstacle>();
        Vector2 pickupPoint = new Vector2(pickupPosition.x, pickupPosition.y);

        if (TryGetValidSpawnPoint(pickupPoint, pickupPoint, template.position.z, avoidObstacle, out spawnPosition))
        {
            return true;
        }

        const float ambushProbeRadius = 1.9f;
        for (int attempt = 0; attempt < SpawnProbeAttempts; attempt++)
        {
            Vector2 candidate = pickupPoint + UnityEngine.Random.insideUnitCircle * ambushProbeRadius;
            if (TryGetValidSpawnPoint(candidate, pickupPoint, template.position.z, avoidObstacle, out spawnPosition))
            {
                return true;
            }
        }

        spawnPosition = default;
        return false;
    }

    private bool TryGetValidSpawnPoint(
        Vector2 candidate,
        Vector2 templateOrigin,
        float zPosition,
        EnemyAvoidObstacle avoidObstacle,
        out Vector3 spawnPosition)
    {
        Vector2 resolvedPoint = avoidObstacle != null
            ? avoidObstacle.SnapWorldPositionToReachableGrid(candidate, templateOrigin)
            : candidate;

        if (IsSpawnPointBlocked(resolvedPoint, avoidObstacle))
        {
            spawnPosition = default;
            return false;
        }

        spawnPosition = new Vector3(resolvedPoint.x, resolvedPoint.y, zPosition);
        return true;
    }

    private bool IsSpawnPointBlocked(Vector2 point, EnemyAvoidObstacle avoidObstacle)
    {
        if (avoidObstacle != null)
        {
            return avoidObstacle.IsPointBlocked(point);
        }

        Collider2D[] overlaps = Physics2D.OverlapBoxAll(point, SpawnProbeSize, 0f);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D collider = overlaps[i];
            if (collider == null)
            {
                continue;
            }

            if (IsFallbackBlockedCollider(collider))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsFallbackBlockedCollider(Collider2D collider)
    {
        if (collider.CompareTag("Water") || collider.CompareTag("Obstacle") || collider.CompareTag("Building"))
        {
            return true;
        }

        string colliderName = collider.name;
        for (int i = 0; i < FallbackBlockedKeywords.Length; i++)
        {
            if (colliderName.IndexOf(FallbackBlockedKeywords[i], StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private void SpawnDrop(Vector3 position)
    {
        float roll = UnityEngine.Random.value;
        ArchitecturalCrystal crystal;

        if (roll < dropTable.commonStructureProbability)
        {
            ArchitecturalType type = CommonStructureTypes[UnityEngine.Random.Range(0, CommonStructureTypes.Length)];
            crystal = ArchitecturalCrystalFactory.CreateCommonStructure(type);
        }
        else if (roll < dropTable.commonStructureProbability + dropTable.inkSupplyProbability)
        {
            bool largeBottle = UnityEngine.Random.value >= 0.7f;
            crystal = ArchitecturalCrystalFactory.CreateInkSupply(largeBottle);
        }
        else
        {
            crystal = ArchitecturalCrystalFactory.CreateSpecialStructureMaterial();
        }

        CreateDropObject(crystal, position);
    }

    private void CreateDropObject(ArchitecturalCrystal crystal, Vector3 position)
    {
        RuntimeCrystalDropFactory.CreateInteractiveDrop(
            crystal,
            position,
            0.35f,
            4,
            transform,
            $"StageDrop_{crystal.DisplayName}",
            RuntimeDropPresentation.ClosedLootBag);
    }

    private RunStageConfig GetStageForElapsed(float elapsedTime)
    {
        EnsureStageConfigs();
        if (stageConfigs.Count == 0)
        {
            return null;
        }

        RunStageConfig selected = stageConfigs[0];
        for (int i = 0; i < stageConfigs.Count; i++)
        {
            if (elapsedTime >= stageConfigs[i].elapsedStartTime)
            {
                selected = stageConfigs[i];
            }
        }

        return selected;
    }

    private float GetElapsedTime()
    {
        if (countdownManager == null)
        {
            return 0f;
        }

        return Mathf.Max(0f, countdownManager.totalTime - countdownManager.CurrentTime);
    }

    private int GetAliveEnemyCount()
    {
        return FindObjectsOfType<EnemyStatsManager>().Length;
    }

    private void HandleCountdownFinished()
    {
        if (countdownFinishedHandled || runtimeSuspended)
        {
            return;
        }

        countdownFinishedHandled = true;
        if (!GameplayFailureController.TryTriggerFailure(
            GameplayFailureReason.TimeExpired,
            "DeadScene"))
        {
            Time.timeScale = 1f;
            SceneLoader loader = SceneLoader.EnsureInstance();
            if (loader != null)
            {
                loader.ToScene("DeadScene");
                return;
            }

            SceneManager.LoadScene("DeadScene");
        }
    }

    private sealed class EnemySpawnTemplate
    {
        public GameObject template;
        public Vector3 position;
        public Quaternion rotation;
    }

    private sealed class ResolvedStageState
    {
        public RunStagePhase phase;
        public float enemyHp;
        public float enemyAttack;
        public float enemyDefense;
        public float enemySpeed;
        public float spawnInterval;
        public int targetAliveCount;
    }
}

public class RunStageEnemyBinding : MonoBehaviour
{
    private const float FallbackDestroyDelay = 1.5f;

    private RunStageDirector director;
    private CharacterCore characterCore;
    private CharacterDeathBase deathBehaviour;
    private bool handledDeath;

    public void Configure(RunStageDirector owner)
    {
        director = owner;
        characterCore = GetComponent<CharacterCore>();
        deathBehaviour = GetComponent<CharacterDeathBase>();
        handledDeath = false;

        if (deathBehaviour != null)
        {
            deathBehaviour.OnDeathSequenceCompleted -= HandleDeathSequenceCompleted;
            deathBehaviour.OnDeathSequenceCompleted += HandleDeathSequenceCompleted;
        }
        else if (characterCore != null)
        {
            characterCore.OnDeath -= HandleImmediateDeath;
            characterCore.OnDeath += HandleImmediateDeath;
        }
    }

    private void OnDisable()
    {
        if (deathBehaviour != null)
        {
            deathBehaviour.OnDeathSequenceCompleted -= HandleDeathSequenceCompleted;
        }

        if (characterCore != null)
        {
            characterCore.OnDeath -= HandleImmediateDeath;
        }
    }

    private void HandleDeathSequenceCompleted()
    {
        if (handledDeath)
        {
            return;
        }

        handledDeath = true;
        director?.HandleEnemyDeath(transform.position);
    }

    private void HandleImmediateDeath()
    {
        HandleDeathSequenceCompleted();
        if (deathBehaviour == null)
        {
            Destroy(gameObject, FallbackDestroyDelay);
        }
    }
}

public static class RuntimeGameplayFailureBridge
{
    private const string ControllerTypeName = "GameplayFailureController";
    private const string ReasonTypeName = "GameplayFailureReason";
    private const string TryTriggerFailureMethodName = "TryTriggerFailure";

    public static bool TryTriggerFailure(string reasonName, string gameOverSceneName)
    {
        Type controllerType = ResolveType(ControllerTypeName);
        Type reasonType = ResolveType(ReasonTypeName);
        if (controllerType == null || reasonType == null)
        {
            return false;
        }

        object parsedReason;
        try
        {
            parsedReason = Enum.Parse(reasonType, reasonName);
        }
        catch
        {
            return false;
        }

        System.Reflection.MethodInfo method = controllerType.GetMethod(
            TryTriggerFailureMethodName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (method == null)
        {
            return false;
        }

        object result = method.Invoke(null, new[] { parsedReason, gameOverSceneName });
        return result is bool triggered && triggered;
    }

    private static Type ResolveType(string typeName)
    {
        Type type = Type.GetType(typeName);
        if (type != null)
        {
            return type;
        }

        System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            type = assemblies[i].GetType(typeName);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }
}
