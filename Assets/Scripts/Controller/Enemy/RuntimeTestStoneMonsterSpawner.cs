using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class RuntimeTestStoneMonsterSpawner : MonoBehaviour
{
    public const float TestHealth = 9999f;

    private const string SpawnedObjectName = "RuntimeTestStoneMonster";
    private const string BootstrapperObjectName = "RuntimeTestStoneMonsterSpawner";
    private const string StoneMonsterPrefabPath = "Assets/File/Prefab/EnemyPrefab/StoneMonster.prefab";
    private const float PlayerWaitSeconds = 2f;
    private static readonly Vector3 PlayerSpawnOffset = new Vector3(0.6f, 1.8f, 0f);
    private static readonly Vector3 FallbackPosition = new Vector3(0f, 1.8f, 0f);

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
        if (!GameplayStageCatalog.IsGameplayScene(scene.name) || FindExistingSpawnedMonster() != null)
        {
            return;
        }

        if (FindObjectOfType<RuntimeTestStoneMonsterSpawner>() != null)
        {
            return;
        }

        GameObject bootstrapper = new GameObject(BootstrapperObjectName);
        bootstrapper.AddComponent<RuntimeTestStoneMonsterSpawner>();
    }

    private void Start()
    {
        StartCoroutine(SpawnWhenReady());
    }

    private IEnumerator SpawnWhenReady()
    {
        float deadline = Time.realtimeSinceStartup + PlayerWaitSeconds;
        GameObject playerObject = null;

        while (Time.realtimeSinceStartup < deadline)
        {
            playerObject = ResolvePlayerObject();
            if (playerObject != null)
            {
                break;
            }

            yield return null;
        }

        if (FindExistingSpawnedMonster() == null)
        {
            GameObject template = ResolveStoneMonsterTemplate();
            if (template != null)
            {
                CreateStoneMonsterFromTemplate(template, ResolveSpawnPosition(playerObject));
            }
        }

        Destroy(gameObject);
    }

    public static GameObject CreateStoneMonsterFromTemplate(GameObject template, Vector3 position)
    {
        if (template == null)
        {
            return null;
        }

        GameObject monster = Instantiate(template, position, template.transform.rotation);
        monster.name = SpawnedObjectName;
        monster.SetActive(true);

        RuntimeTestStoneMonsterHealthOverride healthOverride = monster.GetComponent<RuntimeTestStoneMonsterHealthOverride>();
        if (healthOverride == null)
        {
            healthOverride = monster.AddComponent<RuntimeTestStoneMonsterHealthOverride>();
        }

        healthOverride.ApplyInitialHealth(TestHealth);

        RuntimeTestStoneMonsterStationary stationary = monster.GetComponent<RuntimeTestStoneMonsterStationary>();
        if (stationary == null)
        {
            stationary = monster.AddComponent<RuntimeTestStoneMonsterStationary>();
        }

        stationary.LockCurrentPosition();

        EnemyCombatFeedback combatFeedback = monster.GetComponent<EnemyCombatFeedback>();
        if (combatFeedback == null)
        {
            combatFeedback = monster.AddComponent<EnemyCombatFeedback>();
        }

        combatFeedback.SetHealthBarVisibleWhileAlive(true);
        ConfigureRunStageBinding(monster);
        NightLightingController.EnsureProjectedShadow(monster);
        NightLightingController.EnsureGameplayEnemyLight(monster);
        return monster;
    }

    public static bool TryPreserveHealthOverride(GameObject enemyObject)
    {
        RuntimeTestStoneMonsterHealthOverride healthOverride = enemyObject != null
            ? enemyObject.GetComponent<RuntimeTestStoneMonsterHealthOverride>()
            : null;

        if (healthOverride == null)
        {
            return false;
        }

        healthOverride.EnsureMaxHealth(TestHealth);
        return true;
    }

    private static GameObject ResolveStoneMonsterTemplate()
    {
        GameObject prefab = LoadStoneMonsterPrefab();
        if (prefab != null)
        {
            return prefab;
        }

        StoneMonsterDeath[] stoneDeaths = FindObjectsOfType<StoneMonsterDeath>(true);
        for (int i = 0; i < stoneDeaths.Length; i++)
        {
            if (stoneDeaths[i] != null && stoneDeaths[i].gameObject.name != SpawnedObjectName)
            {
                return stoneDeaths[i].gameObject;
            }
        }

        EnemyStatsManager[] enemies = FindObjectsOfType<EnemyStatsManager>(true);
        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemy = enemies[i] != null ? enemies[i].gameObject : null;
            if (enemy == null || enemy.name == SpawnedObjectName)
            {
                continue;
            }

            if (enemy.CompareTag("StoneEnemy") || enemy.name.IndexOf("StoneMonster", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return enemy;
            }
        }

        return null;
    }

    private static GameObject LoadStoneMonsterPrefab()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GameObject>(StoneMonsterPrefabPath);
#else
        return null;
#endif
    }

    private static void ConfigureRunStageBinding(GameObject monster)
    {
        RunStageDirector director = FindObjectOfType<RunStageDirector>();
        if (director == null)
        {
            return;
        }

        RunStageEnemyBinding binding = monster.GetComponent<RunStageEnemyBinding>();
        if (binding == null)
        {
            binding = monster.AddComponent<RunStageEnemyBinding>();
        }

        binding.Configure(director);
    }

    private static Vector3 ResolveSpawnPosition(GameObject playerObject)
    {
        return playerObject != null
            ? playerObject.transform.position + PlayerSpawnOffset
            : FallbackPosition;
    }

    private static GameObject ResolvePlayerObject()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player;
        }

        PlayerAttack playerAttack = FindObjectOfType<PlayerAttack>();
        return playerAttack != null ? playerAttack.gameObject : null;
    }

    private static GameObject FindExistingSpawnedMonster()
    {
        GameObject existing = GameObject.Find(SpawnedObjectName);
        if (existing != null)
        {
            return existing;
        }

        RuntimeTestStoneMonsterHealthOverride marker = FindObjectOfType<RuntimeTestStoneMonsterHealthOverride>();
        return marker != null ? marker.gameObject : null;
    }
}

public sealed class RuntimeTestStoneMonsterStationary : MonoBehaviour
{
    private Rigidbody2D body;
    private CharacterCore characterCore;
    private Vector3 lockedPosition;
    private bool hasLockedPosition;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        characterCore = GetComponent<CharacterCore>();
        LockCurrentPosition();
    }

    private void OnEnable()
    {
        DisableMovementBehaviours();
        ApplyStationaryStats();
        FreezeBody();
    }

    private void FixedUpdate()
    {
        FreezeBody();
    }

    private void LateUpdate()
    {
        ApplyStationaryStats();
        if (hasLockedPosition)
        {
            transform.position = lockedPosition;
        }
    }

    public void LockCurrentPosition()
    {
        lockedPosition = transform.position;
        hasLockedPosition = true;
        DisableMovementBehaviours();
        ApplyStationaryStats();
        FreezeBody();
    }

    private void DisableMovementBehaviours()
    {
        DisableIfPresent(GetComponents<EnemyMove>());
        DisableIfPresent(GetComponents<EnemyChase>());
        DisableIfPresent(GetComponents<EnemyPatrol>());
        DisableIfPresent(GetComponents<EnemyAvoidObstacle>());
        DisableIfPresent(GetComponents<CharacterAttack>());
        DisableIfPresent(GetComponentsInChildren<EnemyChaseTrigger2D>(true));
    }

    private void ApplyStationaryStats()
    {
        if (characterCore == null)
        {
            characterCore = GetComponent<CharacterCore>();
        }

        if (characterCore == null)
        {
            return;
        }

        if (characterCore.stats != null)
        {
            characterCore.stats.moveSpeed = 0f;
        }

        if (characterCore.baseStats != null)
        {
            characterCore.baseStats.moveSpeed = 0f;
        }
    }

    private void FreezeBody()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (body == null)
        {
            return;
        }

        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeAll;
        body.simulated = true;
    }

    private static void DisableIfPresent<T>(T[] behaviours) where T : Behaviour
    {
        if (behaviours == null)
        {
            return;
        }

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null)
            {
                behaviours[i].enabled = false;
            }
        }
    }
}

public sealed class RuntimeTestStoneMonsterHealthOverride : MonoBehaviour
{
    private CharacterCore characterCore;
    private float overrideMaxHealth = RuntimeTestStoneMonsterSpawner.TestHealth;

    private void Awake()
    {
        characterCore = GetComponent<CharacterCore>();
        EnsureMaxHealth(overrideMaxHealth);
    }

    private void LateUpdate()
    {
        EnsureMaxHealth(overrideMaxHealth);
    }

    public void ApplyInitialHealth(float maxHealth)
    {
        overrideMaxHealth = Mathf.Max(1f, maxHealth);
        if (characterCore == null)
        {
            characterCore = GetComponent<CharacterCore>();
        }

        if (characterCore == null)
        {
            return;
        }

        ConfigureStats(characterCore, overrideMaxHealth);
        characterCore.currentHp = overrideMaxHealth;
    }

    public void EnsureMaxHealth(float maxHealth)
    {
        overrideMaxHealth = Mathf.Max(1f, maxHealth);
        if (characterCore == null)
        {
            characterCore = GetComponent<CharacterCore>();
        }

        if (characterCore == null)
        {
            return;
        }

        float currentHp = characterCore.currentHp > 0f
            ? Mathf.Min(characterCore.currentHp, overrideMaxHealth)
            : overrideMaxHealth;
        ConfigureStats(characterCore, overrideMaxHealth);
        characterCore.currentHp = currentHp;
    }

    private static void ConfigureStats(CharacterCore core, float maxHealth)
    {
        if (core.stats == null)
        {
            core.stats = new CharacterStats();
        }

        core.stats.maxHp = maxHealth;
        if (core.baseStats == null)
        {
            core.baseStats = core.stats.Clone();
        }

        core.baseStats.maxHp = maxHealth;
    }
}
