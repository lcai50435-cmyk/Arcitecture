using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private const string GameSceneName = "GameScene";

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
    private readonly Dictionary<string, Sprite> dropSpriteCache = new Dictionary<string, Sprite>();

    [SerializeField] private DropTableConfig dropTable = new DropTableConfig();

    private GameCountDownManager countdownManager;
    private RunStageConfig currentStage;
    private float spawnTimer;
    private bool countdownFinishedHandled;

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
        if (scene.name != GameSceneName)
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
        BindCountdownManager();
        CaptureExistingEnemiesAsTemplates();
        ApplyStage(GetStageForElapsed(GetElapsedTime()));
    }

    private void Update()
    {
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
        RunStageConfig nextStage = GetStageForElapsed(GetElapsedTime());
        if (nextStage != currentStage)
        {
            ApplyStage(nextStage);
        }

        spawnTimer += Time.deltaTime;
        if (currentStage != null && spawnTimer >= currentStage.spawnInterval)
        {
            spawnTimer = 0f;

            if (GetAliveEnemyCount() < currentStage.targetAliveCount)
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
    }

    public void HandleEnemyDeath(Vector3 position)
    {
        SpawnDrop(position);
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
            enemySpeed = 1.2f,
            spawnInterval = 1f,
            targetAliveCount = 8
        });
    }

    private void BindCountdownManager()
    {
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
        countdownManager.SetInBaseState(false);
        countdownManager.OnCountdownFinished -= HandleCountdownFinished;
        countdownManager.OnCountdownFinished += HandleCountdownFinished;
    }

    private void CaptureExistingEnemiesAsTemplates()
    {
        if (spawnTemplates.Count > 0)
        {
            return;
        }

        EnemyStatsManager[] existingEnemies = FindObjectsOfType<EnemyStatsManager>();
        for (int i = 0; i < existingEnemies.Length; i++)
        {
            EnemyStatsManager enemy = existingEnemies[i];
            if (enemy == null)
            {
                continue;
            }

            GameObject templateObject = Instantiate(enemy.gameObject, enemy.transform.position, enemy.transform.rotation, transform);
            templateObject.name = $"{enemy.gameObject.name}_Template";
            templateObject.SetActive(false);

            spawnTemplates.Add(new EnemySpawnTemplate
            {
                template = templateObject,
                position = enemy.transform.position,
                rotation = enemy.transform.rotation
            });

            PrepareEnemyInstance(enemy.gameObject);
        }
    }

    private void PrepareEnemyInstance(GameObject enemyObject)
    {
        if (enemyObject == null)
        {
            return;
        }

        ApplyStageToEnemy(enemyObject, currentStage ?? GetStageForElapsed(GetElapsedTime()));

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
    }

    private void ApplyStage(RunStageConfig stage)
    {
        if (stage == null)
        {
            return;
        }

        currentStage = stage;
        spawnTimer = 0f;

        EnemyStatsManager[] aliveEnemies = FindObjectsOfType<EnemyStatsManager>();
        for (int i = 0; i < aliveEnemies.Length; i++)
        {
            ApplyStageToEnemy(aliveEnemies[i].gameObject, stage);
        }
    }

    private void ApplyStageToEnemy(GameObject enemyObject, RunStageConfig stage)
    {
        if (enemyObject == null || stage == null)
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
        core.stats.moveSpeed = stage.enemySpeed;
        core.currentHp = Mathf.Clamp(stage.enemyHp * healthRatio, 0f, stage.enemyHp);
    }

    private void SpawnOneEnemy()
    {
        if (spawnTemplates.Count == 0)
        {
            return;
        }

        EnemySpawnTemplate template = spawnTemplates[UnityEngine.Random.Range(0, spawnTemplates.Count)];
        if (template.template == null)
        {
            return;
        }

        GameObject enemyObject = Instantiate(template.template, template.position, template.rotation);
        enemyObject.name = template.template.name.Replace("_Template", string.Empty);
        enemyObject.SetActive(true);
        PrepareEnemyInstance(enemyObject);
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
        GameObject dropObject = new GameObject($"StageDrop_{crystal.DisplayName}");
        dropObject.transform.position = position;
        dropObject.transform.localScale = new Vector3(0.35f, 0.35f, 1f);

        SpriteRenderer renderer = dropObject.AddComponent<SpriteRenderer>();
        Sprite dropSprite = GetDropSprite(crystal);
        renderer.sprite = dropSprite;
        renderer.sortingOrder = 4;

        CircleCollider2D collider = dropObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;

        CrystalInteractHandler handler = dropObject.AddComponent<CrystalInteractHandler>();
        handler.type = crystal.type;
        handler.expValue = crystal.expValue;
        handler.icon = crystal.icon != null ? crystal.icon : dropSprite;
        handler.backIcon = crystal.backIcon != null ? crystal.backIcon : dropSprite;
        handler.bonusType = crystal.bonusType;
        handler.bonusValue = crystal.bonusValue;
        handler.subBonusType = crystal.subBonusType;
        handler.subBonusValue = crystal.subBonusValue;
        handler.isUnlockMaterial = crystal.isUnlockMaterial;
        handler.resourceCategory = crystal.resourceCategory;
        handler.inkRestoreValue = crystal.inkRestoreValue;
        handler.textDescription = crystal.textDescription;
    }

    private Sprite GetDropSprite(ArchitecturalCrystal crystal)
    {
        if (crystal.icon != null)
        {
            return crystal.icon;
        }

        string key = $"{crystal.Category}_{crystal.type}";
        if (dropSpriteCache.TryGetValue(key, out Sprite cached))
        {
            return cached;
        }

        Color color = crystal.IsSpecialStructure
            ? new Color(0.98f, 0.82f, 0.26f, 1f)
            : crystal.IsInkSupply
                ? new Color(0.24f, 0.74f, 0.92f, 1f)
                : new Color(0.92f, 0.92f, 0.92f, 1f);

        Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Vector2 center = new Vector2(7.5f, 7.5f);

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= 6.8f ? color : Color.clear);
            }
        }

        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
        dropSpriteCache[key] = sprite;
        return sprite;
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
        if (countdownFinishedHandled)
        {
            return;
        }

        countdownFinishedHandled = true;
        Invoke(nameof(ReturnToBaseAfterCountdown), 0.75f);
    }

    private void ReturnToBaseAfterCountdown()
    {
        GameSceneBaseReturnBootstrapper.SubmitCatalogueAndReturnToBase();
    }

    private sealed class EnemySpawnTemplate
    {
        public GameObject template;
        public Vector3 position;
        public Quaternion rotation;
    }
}

public class RunStageEnemyBinding : MonoBehaviour
{
    private RunStageDirector director;
    private CharacterCore characterCore;
    private bool handledDeath;

    public void Configure(RunStageDirector owner)
    {
        director = owner;
        characterCore = GetComponent<CharacterCore>();
        handledDeath = false;

        if (characterCore != null)
        {
            characterCore.OnDeath -= HandleDeath;
            characterCore.OnDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (characterCore != null)
        {
            characterCore.OnDeath -= HandleDeath;
        }
    }

    private void HandleDeath()
    {
        if (handledDeath)
        {
            return;
        }

        handledDeath = true;
        director?.HandleEnemyDeath(transform.position);
        Destroy(gameObject, 1f);
    }
}
