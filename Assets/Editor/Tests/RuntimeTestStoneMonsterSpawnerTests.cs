using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class RuntimeTestStoneMonsterSpawnerTests
{
    private GameObject templateObject;
    private GameObject spawnedObject;

    [TearDown]
    public void TearDown()
    {
        if (spawnedObject != null)
        {
            Object.DestroyImmediate(spawnedObject);
        }

        if (templateObject != null)
        {
            Object.DestroyImmediate(templateObject);
        }
    }

    [Test]
    public void SpawnerDoesNotAutoBootstrapIntoGameplayScenes()
    {
        MethodInfo[] methods = typeof(RuntimeTestStoneMonsterSpawner).GetMethods(
            BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        for (int i = 0; i < methods.Length; i++)
        {
            object[] attributes = methods[i].GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
            Assert.IsEmpty(attributes, methods[i].Name + " must not auto-create test monsters when entering gameplay scenes.");
        }
    }

    [Test]
    public void CreateStoneMonsterFromTemplateSpawnsRealEnemyWithTestHealth()
    {
        templateObject = CreateStoneMonsterTemplate();

        spawnedObject = RuntimeTestStoneMonsterSpawner.CreateStoneMonsterFromTemplate(templateObject, new Vector3(3f, 4f, 0f));

        Assert.IsNotNull(spawnedObject);
        Assert.AreEqual("RuntimeTestStoneMonster", spawnedObject.name);
        Assert.AreEqual(new Vector3(3f, 4f, 0f), spawnedObject.transform.position);
        Assert.AreEqual(Vector3.one * 4f, spawnedObject.transform.localScale);
        Assert.IsNotNull(spawnedObject.GetComponent<EnemyStatsManager>());
        Assert.IsNotNull(spawnedObject.GetComponent<RuntimeTestStoneMonsterHealthOverride>());
        Assert.IsNotNull(spawnedObject.GetComponent<RuntimeTestStoneMonsterStationary>());
        Assert.IsNotNull(spawnedObject.GetComponent<EnemyCombatFeedback>());
        Assert.IsNotNull(spawnedObject.transform.Find("EnemyHealthBar"));
        Assert.IsTrue(spawnedObject.transform.Find("EnemyHealthBar").gameObject.activeSelf);
        Assert.IsFalse(spawnedObject.GetComponent<EnemyMove>().enabled);
        Assert.IsFalse(spawnedObject.GetComponent<EnemyChase>().enabled);
        Assert.IsFalse(spawnedObject.GetComponent<EnemyPatrol>().enabled);
        Assert.IsFalse(spawnedObject.GetComponent<EnemyAvoidObstacle>().enabled);
        Assert.IsFalse(spawnedObject.GetComponent<EnemyAttack>().enabled);
        Assert.AreEqual(RigidbodyConstraints2D.FreezeAll, spawnedObject.GetComponent<Rigidbody2D>().constraints);

        CharacterCore core = spawnedObject.GetComponent<CharacterCore>();
        Assert.IsNotNull(core);
        Assert.AreEqual(9999f, core.stats.maxHp);
        Assert.AreEqual(9999f, core.baseStats.maxHp);
        Assert.AreEqual(9999f, core.currentHp);
        Assert.AreEqual(0f, core.stats.moveSpeed);
        Assert.AreEqual(0f, core.baseStats.moveSpeed);
    }

    [Test]
    public void HealthOverridePreservesRemainingDamageAndBlocksStageRefresh()
    {
        templateObject = CreateStoneMonsterTemplate();
        spawnedObject = RuntimeTestStoneMonsterSpawner.CreateStoneMonsterFromTemplate(templateObject, Vector3.zero);
        CharacterCore core = spawnedObject.GetComponent<CharacterCore>();
        core.currentHp = 9000f;
        core.stats.maxHp = 120f;

        bool preserved = RuntimeTestStoneMonsterSpawner.TryPreserveHealthOverride(spawnedObject);

        Assert.IsTrue(preserved);
        Assert.AreEqual(9999f, core.stats.maxHp);
        Assert.AreEqual(9000f, core.currentHp);
    }

    private static GameObject CreateStoneMonsterTemplate()
    {
        GameObject template = new GameObject("StoneMonster_Template");
        template.transform.localScale = Vector3.one * 4f;
        template.AddComponent<Rigidbody2D>().gravityScale = 0f;
        template.AddComponent<BoxCollider2D>();
        CharacterCore core = template.AddComponent<CharacterCore>();
        core.stats = new CharacterStats
        {
            maxHp = 200f,
            attackDamage = 20f,
            moveSpeed = 0.8f,
            defense = 0f
        };
        core.baseStats = core.stats.Clone();
        core.currentHp = 200f;
        template.AddComponent<EnemyStatsManager>();
        template.AddComponent<EnemyMove>();
        template.AddComponent<EnemyAvoidObstacle>();
        template.AddComponent<EnemyChase>();
        template.AddComponent<EnemyPatrol>();
        template.AddComponent<EnemyAttack>();
        return template;
    }
}

public sealed class RunStageDirectorFallbackTemplateTests
{
    private GameObject directorObject;

    [TearDown]
    public void TearDown()
    {
        EnemyStatsManager[] enemies = Object.FindObjectsOfType<EnemyStatsManager>(true);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
            {
                Object.DestroyImmediate(enemies[i].gameObject);
            }
        }

        if (directorObject != null)
        {
            Object.DestroyImmediate(directorObject);
        }
    }

    [Test]
    public void DebugSpawnEnemyUsesPrefabTemplateWhenSceneHasNoEnemyInstances()
    {
        directorObject = new GameObject("RunStageDirector");
        RunStageDirector director = directorObject.AddComponent<RunStageDirector>();

        bool spawned = director.DebugSpawnEnemy("StoneMonster", 1);

        Assert.IsTrue(spawned);
        EnemyStatsManager[] enemies = Object.FindObjectsOfType<EnemyStatsManager>();
        Assert.AreEqual(1, enemies.Length);
        Assert.That(enemies[0].gameObject.name, Does.Contain("StoneMonster"));
    }

    [Test]
    public void StageDropLootBagUsesCompactWorldScale()
    {
        directorObject = new GameObject("RunStageDirector");
        RunStageDirector director = directorObject.AddComponent<RunStageDirector>();
        ArchitecturalCrystal crystal = ArchitecturalCrystalFactory.CreateCommonStructure(ArchitecturalType.Brackets);
        MethodInfo createDropMethod = typeof(RunStageDirector).GetMethod(
            "CreateDropObject",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(createDropMethod);

        createDropMethod.Invoke(director, new object[] { crystal, Vector3.zero });

        Transform droppedTransform = directorObject.transform.GetChild(0);
        Assert.That(droppedTransform.name, Does.StartWith("StageDrop_"));
        Assert.That(droppedTransform.localScale.x, Is.EqualTo(0.0875f).Within(0.001f));
        Assert.That(droppedTransform.localScale.y, Is.EqualTo(0.0875f).Within(0.001f));
    }
}

public sealed class RunStageDirectorMonsterBudgetTests
{
    private GameObject directorObject;

    [TearDown]
    public void TearDown()
    {
        EnemyStatsManager[] enemies = Object.FindObjectsOfType<EnemyStatsManager>(true);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
            {
                Object.DestroyImmediate(enemies[i].gameObject);
            }
        }

        if (directorObject != null)
        {
            Object.DestroyImmediate(directorObject);
        }
    }

    [Test]
    public void CaptureExistingEnemiesTrimsActiveCountToBasicStageLimit()
    {
        RunStageDirector director = CreateDirector();
        for (int i = 0; i < 5; i++)
        {
            CreateEnemy($"SceneEnemy_{i}", new Vector3(i, 0f, 0f));
        }

        InvokePrivate(director, "CaptureExistingEnemiesAsTemplates");
        InvokePrivate(director, "RefreshResolvedStage", 0f, true);
        InvokePrivate(director, "EnforceActiveEnemyLimit");

        Assert.AreEqual(3, CountActiveEnemies());
    }

    [Test]
    public void PickupAmbushDoesNotSpawnWhenActiveEnemyBudgetIsFull()
    {
        RunStageDirector director = CreateDirector();
        for (int i = 0; i < 3; i++)
        {
            CreateEnemy($"BudgetEnemy_{i}", new Vector3(i, 0f, 0f));
        }

        InvokePrivate(director, "CaptureExistingEnemiesAsTemplates");
        InvokePrivate(director, "RefreshResolvedStage", 0f, true);

        bool spawned = (bool)InvokePrivate(director, "TrySpawnPickupAmbush", Vector3.zero);

        Assert.IsFalse(spawned);
        Assert.AreEqual(3, CountActiveEnemies());
    }

    [Test]
    public void StageRefreshOnlyUpdatesTrackedActiveEnemies()
    {
        RunStageDirector director = CreateDirector();
        GameObject tracked = CreateEnemy("TrackedEnemy", Vector3.zero);

        InvokePrivate(director, "CaptureExistingEnemiesAsTemplates");

        GameObject untracked = CreateEnemy("UntrackedEnemy", Vector3.right);
        CharacterCore untrackedCore = untracked.GetComponent<CharacterCore>();
        untrackedCore.stats.maxHp = 999f;
        untrackedCore.currentHp = 999f;

        InvokePrivate(director, "RefreshResolvedStage", 0f, true);

        Assert.AreEqual(100f, tracked.GetComponent<CharacterCore>().stats.maxHp);
        Assert.AreEqual(999f, untrackedCore.stats.maxHp);
        Assert.AreEqual(999f, untrackedCore.currentHp);
    }

    private RunStageDirector CreateDirector()
    {
        directorObject = new GameObject("RunStageDirector");
        return directorObject.AddComponent<RunStageDirector>();
    }

    private static GameObject CreateEnemy(string name, Vector3 position)
    {
        GameObject enemy = new GameObject(name, typeof(CharacterCore), typeof(EnemyStatsManager));
        enemy.transform.position = position;
        CharacterCore core = enemy.GetComponent<CharacterCore>();
        core.stats = new CharacterStats
        {
            maxHp = 50f,
            attackDamage = 5f,
            moveSpeed = 0.8f,
            defense = 0f
        };
        core.baseStats = core.stats.Clone();
        core.currentHp = 50f;
        return enemy;
    }

    private static int CountActiveEnemies()
    {
        EnemyStatsManager[] enemies = Object.FindObjectsOfType<EnemyStatsManager>();
        int count = 0;
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i].gameObject.activeInHierarchy)
            {
                count++;
            }
        }

        return count;
    }

    private static object InvokePrivate(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        return method.Invoke(target, args);
    }
}
