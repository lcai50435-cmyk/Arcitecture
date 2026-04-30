using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class FirstPassEnemyAttackTuningTests
{
    private GameObject playerObject;

    [TearDown]
    public void TearDown()
    {
        if (playerObject != null)
        {
            Object.DestroyImmediate(playerObject);
        }

        DestroyRuntimeObject("GameplayStatusHudCanvas");
        DestroyRuntimeObject("GameplayStatusHudRoot");
        DestroyRuntimeObject("RuntimeCameraController");
        DestroyRuntimeObject("SpriteCompanion");
    }

    [Test]
    public void FireballPrefabUsesCompactFirstPassScale()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/File/Prefab/WeaponPrefab/FireBall.prefab");

        Assert.IsNotNull(prefab);
        Assert.AreEqual(5, prefab.layer);
        Assert.That(prefab.transform.localScale.x, Is.EqualTo(1.2f).Within(0.001f));
        Assert.That(prefab.transform.localScale.y, Is.EqualTo(1.2f).Within(0.001f));

        FireBall fireBall = prefab.GetComponent<FireBall>();
        Assert.IsNotNull(fireBall);
        Assert.That(fireBall.autoDestroyTime, Is.LessThanOrEqualTo(4f));
        Assert.That(fireBall.hitDestroyDelay, Is.LessThanOrEqualTo(1f));
    }

    [Test]
    public void FireballAttackEffectRendersAboveWorldCharacters()
    {
        GameObject fireballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/File/Prefab/WeaponPrefab/FireBall.prefab");

        Assert.IsNotNull(fireballPrefab);

        Assert.AreEqual(5, fireballPrefab.layer);
        Assert.That(fireballPrefab.GetComponent<SpriteRenderer>().sortingOrder, Is.GreaterThanOrEqualTo(12));
    }

    [Test]
    public void StoneMonsterCrackRendersBelowStoneMonster()
    {
        GameObject stoneMonsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/File/Prefab/EnemyPrefab/StoneMonster.prefab");
        GameObject crackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/File/Prefab/WeaponPrefab/Crack.prefab");

        Assert.IsNotNull(stoneMonsterPrefab);
        Assert.IsNotNull(crackPrefab);

        SpriteRenderer stoneRenderer = stoneMonsterPrefab.GetComponent<SpriteRenderer>();
        SpriteRenderer crackRenderer = crackPrefab.GetComponent<SpriteRenderer>();

        Assert.IsNotNull(stoneRenderer);
        Assert.IsNotNull(crackRenderer);
        Assert.AreEqual(5, crackPrefab.layer);
        Assert.That(crackRenderer.sortingOrder, Is.LessThan(stoneRenderer.sortingOrder));
    }

    [Test]
    public void FireMonsterAttackRangeKeepsPlayableRadius()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/File/Prefab/EnemyPrefab/FireMonster.prefab");
        Transform attackRange = prefab != null ? prefab.transform.Find("AttackRange") : null;
        CircleCollider2D attackCollider = attackRange != null ? attackRange.GetComponent<CircleCollider2D>() : null;

        Assert.IsNotNull(prefab);
        Assert.IsNotNull(attackCollider);
        Assert.That(attackCollider.radius, Is.GreaterThanOrEqualTo(1f));
        Assert.That(attackCollider.radius, Is.LessThanOrEqualTo(1.25f));
    }

    [Test]
    public void StoneMonsterCrackUsesCompactDamageArea()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/File/Prefab/WeaponPrefab/Crack.prefab");
        CapsuleCollider2D collider = prefab != null ? prefab.GetComponent<CapsuleCollider2D>() : null;

        Assert.IsNotNull(prefab);
        Assert.IsNotNull(collider);
        Assert.AreEqual(5, prefab.layer);
        Assert.That(prefab.transform.localScale.x, Is.EqualTo(0.375f).Within(0.001f));
        Assert.That(prefab.transform.localScale.y, Is.EqualTo(0.375f).Within(0.001f));
        Assert.That(collider.size.x, Is.LessThanOrEqualTo(2f));
        Assert.That(collider.size.y, Is.LessThanOrEqualTo(1.1f));
    }

    [Test]
    public void PlayerHurtMovementLockHasTimedFallbackRecovery()
    {
        playerObject = new GameObject(
            "Player",
            typeof(Rigidbody2D),
            typeof(SpriteRenderer),
            typeof(Animator),
            typeof(CharacterCore),
            typeof(PlayerMove),
            typeof(PlayerTakeDamage));

        PlayerMove move = playerObject.GetComponent<PlayerMove>();
        PlayerTakeDamage damage = playerObject.GetComponent<PlayerTakeDamage>();
        damage.playerAnim = playerObject.GetComponent<Animator>();
        damage.playerMovement = move;
        move.canMove = true;

        InvokePrivate(damage, "PlayHurtAnimation");

        Assert.IsFalse(move.canMove);

        MethodInfo tickMethod = typeof(PlayerTakeDamage).GetMethod(
            "TickHurtRecovery",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(tickMethod);

        tickMethod.Invoke(damage, new object[] { 0.2f });

        Assert.IsTrue(move.canMove);
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        method.Invoke(target, null);
    }

    private static void DestroyRuntimeObject(string name)
    {
        GameObject target = GameObject.Find(name);
        if (target != null)
        {
            Object.DestroyImmediate(target);
        }
    }
}
