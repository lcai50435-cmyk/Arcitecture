using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class StoneMonsterAttackModeTests
{
    private GameObject stoneObject;
    private GameObject playerObject;
    private GameObject crackPrefab;

    [TearDown]
    public void TearDown()
    {
        DestroyIfPresent(stoneObject);
        DestroyIfPresent(playerObject);
        DestroyIfPresent(crackPrefab);

        CrackDamage[] cracks = Object.FindObjectsOfType<CrackDamage>();
        for (int i = 0; i < cracks.Length; i++)
        {
            DestroyIfPresent(cracks[i].gameObject);
        }
    }

    [Test]
    public void FootstepCrackDoesNotSpawnBeforeStoneMonsterFindsPlayer()
    {
        EnemyFootstepEffect footstep = CreateStoneMonsterFootstep();

        footstep.SpawnFootstepCrack();

        Assert.AreEqual(0, Object.FindObjectsOfType<CrackDamage>().Length);
    }

    [Test]
    public void FootstepCrackSpawnsAfterStoneMonsterStartsChasingPlayer()
    {
        EnemyFootstepEffect footstep = CreateStoneMonsterFootstep();
        EnemyStatsManager stats = stoneObject.GetComponent<EnemyStatsManager>();
        stats.NotifyPlayerEnteredRange(playerObject.GetComponent<Collider2D>());

        footstep.SpawnFootstepCrack();

        Assert.AreEqual(1, Object.FindObjectsOfType<CrackDamage>().Length);
    }

    [Test]
    public void SpawnedFootstepCrackUsesFixedGameplaySortingOrder()
    {
        EnemyFootstepEffect footstep = CreateStoneMonsterFootstep();
        SpriteRenderer stoneRenderer = stoneObject.GetComponent<SpriteRenderer>();
        stoneRenderer.sortingOrder = 5;
        crackPrefab.GetComponent<SpriteRenderer>().sortingOrder = 12;
        EnemyStatsManager stats = stoneObject.GetComponent<EnemyStatsManager>();
        stats.NotifyPlayerEnteredRange(playerObject.GetComponent<Collider2D>());

        footstep.SpawnFootstepCrack();

        SpriteRenderer crackRenderer = Object.FindObjectOfType<CrackDamage>().GetComponent<SpriteRenderer>();
        Assert.AreEqual(3, crackRenderer.sortingOrder);
    }

    [Test]
    public void SpawnedCrackStopsDamagingAfterStoneMonsterLosesPlayer()
    {
        EnemyFootstepEffect footstep = CreateStoneMonsterFootstep();
        EnemyStatsManager stats = stoneObject.GetComponent<EnemyStatsManager>();
        Collider2D playerCollider = playerObject.GetComponent<Collider2D>();
        stats.NotifyPlayerEnteredRange(playerCollider);
        footstep.SpawnFootstepCrack();
        stats.NotifyPlayerExitedRange(playerCollider);

        CharacterCore playerCore = playerObject.GetComponent<CharacterCore>();
        float healthBefore = playerCore.currentHp;
        InvokeTriggerEnter(Object.FindObjectOfType<CrackDamage>(), playerCollider);

        Assert.AreEqual(healthBefore, playerCore.currentHp);
    }

    private EnemyFootstepEffect CreateStoneMonsterFootstep()
    {
        playerObject = new GameObject("Player", typeof(BoxCollider2D), typeof(CharacterCore));
        playerObject.tag = "Player";
        CharacterCore playerCore = playerObject.GetComponent<CharacterCore>();
        playerCore.stats.maxHp = 100f;
        playerCore.baseStats = playerCore.stats.Clone();
        playerCore.currentHp = 100f;

        stoneObject = new GameObject("StoneMonster", typeof(SpriteRenderer), typeof(EnemyStatsManager), typeof(EnemyFootstepEffect));
        EnemyFootstepEffect footstep = stoneObject.GetComponent<EnemyFootstepEffect>();
        footstep.enemyTransform = stoneObject.transform;
        footstep.crackEffectPrefab = CreateCrackPrefab();
        footstep.effectDuration = 10f;
        footstep.spawnInterval = 0f;
        return footstep;
    }

    private GameObject CreateCrackPrefab()
    {
        crackPrefab = new GameObject("CrackPrefab", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(CrackDamage));
        crackPrefab.GetComponent<BoxCollider2D>().isTrigger = true;
        return crackPrefab;
    }

    private static void InvokeTriggerEnter(CrackDamage crackDamage, Collider2D other)
    {
        MethodInfo method = typeof(CrackDamage).GetMethod(
            "OnTriggerEnter2D",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        method.Invoke(crackDamage, new object[] { other });
    }

    private static void DestroyIfPresent(GameObject target)
    {
        if (target != null)
        {
            Object.DestroyImmediate(target);
        }
    }
}
