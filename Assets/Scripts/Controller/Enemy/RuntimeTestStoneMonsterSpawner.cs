using UnityEngine;

public sealed class RuntimeTestStoneMonsterSpawner : MonoBehaviour
{
    public const float TestHealth = 9999f;

    private const string SpawnedObjectName = "RuntimeTestStoneMonster";

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
