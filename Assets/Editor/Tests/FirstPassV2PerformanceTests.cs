using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class FirstPassV2PerformanceTests
{
    private GameObject testObject;

    [TearDown]
    public void TearDown()
    {
        if (testObject != null)
        {
            Object.DestroyImmediate(testObject);
        }
    }

    [Test]
    public void FirstPassV2IsAnAliasWithoutReplacingOfficialEntry()
    {
        GameplayStageDefinition firstStage = GameplayStageCatalog.GetDefaultStage();

        Assert.AreEqual("FirstPass_1", firstStage.sceneName);
        Assert.IsTrue(GameplayStageCatalog.IsGameplayScene("FirstPass_V2"));
        Assert.AreSame(firstStage, GameplayStageCatalog.GetStageByScene("FirstPass_V2"));
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/FirstPass_V2.unity"));
    }

    [Test]
    public void PerformanceProfileKeepsFirstPassBudgets()
    {
        GameplayPerformanceProfile profile = GameplayPerformanceSettings.Profile;

        Assert.AreEqual(60, profile.TargetFrameRate);
        Assert.That(profile.EnemyDecisionInterval, Is.EqualTo(0.1f).Within(0.001f));
        Assert.That(profile.MinimumRepathInterval, Is.EqualTo(0.35f).Within(0.001f));
        Assert.AreEqual(2, profile.MaxPathRequestsPerFrame);
        Assert.AreEqual(8, profile.MaxTransientLights);
        Assert.AreEqual(20, profile.StressEnemyCount);
    }

    [Test]
    public void AttackRecoveryFallbackUnlocksMovementBeforeAttackStateEnds()
    {
        testObject = new GameObject("AttackRecoveryTest");
        testObject.SetActive(false);
        PlayerMove move = testObject.AddComponent<PlayerMove>();
        PlayerAttack attack = testObject.AddComponent<PlayerAttack>();

        SetBaseField(attack, "playerMove", move);
        SetBaseField(attack, "isAttacking", true);
        move.canMove = false;

        MethodInfo fallbackMethod = typeof(CharacterAttack).GetMethod(
            "attackRecoveryFallbackRoutine",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(fallbackMethod);

        IEnumerator fallback = (IEnumerator)fallbackMethod.Invoke(attack, null);
        Assert.IsTrue(fallback.MoveNext());
        Assert.IsInstanceOf<WaitForSeconds>(fallback.Current);

        Assert.IsTrue(fallback.MoveNext());
        Assert.IsTrue(move.canMove);
        Assert.IsTrue(attack.IsAttacking);

        Assert.IsFalse(fallback.MoveNext());
        Assert.IsFalse(attack.IsAttacking);
    }

    [Test]
    public void AttackRecoveryDoesNotChangePlayerAttackCooldown()
    {
        testObject = new GameObject("AttackCooldownTest");
        testObject.SetActive(false);
        PlayerMove move = testObject.AddComponent<PlayerMove>();
        PlayerAttack attack = testObject.AddComponent<PlayerAttack>();

        SetBaseField(attack, "playerMove", move);
        SetBaseField(attack, "isAttacking", true);
        SetPrivateField(attack, "nextAttackTime", 12.5f);
        attack.OnAttackRecovery();

        Assert.IsTrue(move.canMove);
        Assert.That(GetPrivateField<float>(attack, "nextAttackTime"), Is.EqualTo(12.5f));
        Assert.IsTrue(attack.IsAttacking);
    }

    [Test]
    public void CharacterDeathResetRestoresPhysicsAndAliveComponents()
    {
        testObject = new GameObject("DeathResetTest");
        Rigidbody2D body = testObject.AddComponent<Rigidbody2D>();
        BoxCollider2D collider = testObject.AddComponent<BoxCollider2D>();
        CharacterCore core = testObject.AddComponent<CharacterCore>();
        CharacterDeathBase death = testObject.AddComponent<CharacterDeathBase>();
        MethodInfo awakeMethod = typeof(CharacterDeathBase).GetMethod(
            "Awake",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(awakeMethod);
        awakeMethod.Invoke(death, null);

        core.ResetForReuse();
        collider.enabled = false;
        body.bodyType = RigidbodyType2D.Static;
        death.ResetForReuse();

        Assert.IsTrue(collider.enabled);
        Assert.AreEqual(RigidbodyType2D.Dynamic, body.bodyType);
    }

    [Test]
    public void CharacterCoreCanBeResetAfterDeathForPoolReuse()
    {
        testObject = new GameObject("CoreResetTest");
        CharacterCore core = testObject.AddComponent<CharacterCore>();
        core.ResetForReuse();
        core.stats.defense = 0f;
        core.TakeDamage(core.currentHp + 10f);

        Assert.IsTrue(core.IsDead);
        core.ResetForReuse();

        Assert.IsFalse(core.IsDead);
        Assert.That(core.currentHp, Is.EqualTo(core.stats.maxHp).Within(0.001f));
        Assert.That(core.LastDamageTaken, Is.EqualTo(0f));
    }

    private static void SetBaseField(object target, string fieldName, object value)
    {
        FieldInfo field = typeof(CharacterAttack).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        field.SetValue(target, value);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        return (T)field.GetValue(target);
    }
}
