using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class RuntimeFireflyAmbientEffectTests
{
    [Test]
    public void EffectTypeExistsAndIsRuntimeBehaviour()
    {
        Type effectType = ResolveEffectType();

        Assert.IsNotNull(effectType);
        Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(effectType));
    }

    [Test]
    public void ResolveProfileOnlySupportsBaseAndGameplayScenes()
    {
        Type effectType = ResolveEffectType();
        MethodInfo resolveProfile = effectType.GetMethod("ResolveProfile", BindingFlags.Static | BindingFlags.NonPublic);

        Assert.IsNotNull(resolveProfile);
        Assert.IsNotNull(resolveProfile.Invoke(null, new object[] { "NewBase" }));
        Assert.IsNotNull(resolveProfile.Invoke(null, new object[] { "BaseScene" }));
        Assert.IsNotNull(resolveProfile.Invoke(null, new object[] { "GameScene" }));
        Assert.IsNotNull(resolveProfile.Invoke(null, new object[] { "GameScene_02" }));
        Assert.IsNotNull(resolveProfile.Invoke(null, new object[] { "GameScene_03" }));
        Assert.IsNull(resolveProfile.Invoke(null, new object[] { "MainScene" }));
        Assert.IsNull(resolveProfile.Invoke(null, new object[] { "DeadScene" }));
        Assert.IsNull(resolveProfile.Invoke(null, new object[] { "IllustratedUIScene" }));
    }

    [Test]
    public void FirstStageProfileUsesSubtleParticleScale()
    {
        Type effectType = ResolveEffectType();
        MethodInfo resolveProfile = effectType.GetMethod("ResolveProfile", BindingFlags.Static | BindingFlags.NonPublic);
        object profile = resolveProfile.Invoke(null, new object[] { "GameScene" });
        object canonicalProfile = resolveProfile.Invoke(null, new object[] { "FirstPass_1" });

        Assert.IsNotNull(profile);
        Assert.AreSame(profile, canonicalProfile);
        Assert.LessOrEqual(ReadFloat(profile, "minSize"), 0.06f);
        Assert.LessOrEqual(ReadFloat(profile, "maxSize"), 0.16f);
        Assert.LessOrEqual(ReadFloat(profile, "emissionRate"), 18f);
        Assert.LessOrEqual(ReadInt(profile, "maxParticles"), 120);
    }

    [Test]
    public void LaterStageGameplayProfileKeepsVisibleParticleScale()
    {
        Type effectType = ResolveEffectType();
        MethodInfo resolveProfile = effectType.GetMethod("ResolveProfile", BindingFlags.Static | BindingFlags.NonPublic);
        object profile = resolveProfile.Invoke(null, new object[] { "GameScene_02" });

        Assert.IsNotNull(profile);
        Assert.GreaterOrEqual(ReadFloat(profile, "minSize"), 0.12f);
        Assert.GreaterOrEqual(ReadFloat(profile, "maxSize"), 0.28f);
        Assert.GreaterOrEqual(ReadFloat(profile, "emissionRate"), 32f);
        Assert.GreaterOrEqual(ReadInt(profile, "maxParticles"), 220);
    }

    [Test]
    public void AdditiveIllustratedUiSceneKeepsActiveGameplayProfile()
    {
        Type effectType = ResolveEffectType();
        MethodInfo resolveAmbientSceneName = effectType.GetMethod("ResolveAmbientSceneName", BindingFlags.Static | BindingFlags.NonPublic);

        Assert.IsNotNull(resolveAmbientSceneName);
        Assert.AreEqual(
            "GameScene",
            resolveAmbientSceneName.Invoke(null, new object[] { "IllustratedUIScene", LoadSceneMode.Additive, "GameScene" }));
        Assert.AreEqual(
            "IllustratedUIScene",
            resolveAmbientSceneName.Invoke(null, new object[] { "IllustratedUIScene", LoadSceneMode.Single, "GameScene" }));
    }

    [Test]
    public void ConfigureParticleSystemUsesMatchingVelocityCurveModes()
    {
        Type effectType = ResolveEffectType();
        MethodInfo resolveProfile = effectType.GetMethod("ResolveProfile", BindingFlags.Static | BindingFlags.NonPublic);
        MethodInfo ensureParticleSystem = effectType.GetMethod("EnsureParticleSystem", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo configureParticleSystem = effectType.GetMethod("ConfigureParticleSystem", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo resetStatics = effectType.GetMethod("ResetStatics", BindingFlags.Static | BindingFlags.NonPublic);
        object profile = resolveProfile.Invoke(null, new object[] { "GameScene" });
        GameObject effectObject = new GameObject("RuntimeFireflyAmbientEffectTest");

        try
        {
            Component effect = effectObject.AddComponent(effectType);
            ensureParticleSystem.Invoke(effect, Array.Empty<object>());
            configureParticleSystem.Invoke(effect, new[] { profile });

            ParticleSystem.VelocityOverLifetimeModule velocity = effectObject.GetComponent<ParticleSystem>().velocityOverLifetime;
            Assert.AreEqual(velocity.x.mode, velocity.y.mode);
            Assert.AreEqual(velocity.x.mode, velocity.z.mode);

            ParticleSystem.MainModule main = effectObject.GetComponent<ParticleSystem>().main;
            Assert.IsTrue(main.useUnscaledTime);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(effectObject);
            resetStatics.Invoke(null, Array.Empty<object>());
        }
    }

    private static Type ResolveEffectType()
    {
        return Type.GetType("RuntimeFireflyAmbientEffect, Assembly-CSharp");
    }

    private static float ReadFloat(object target, string fieldName)
    {
        return (float)target.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .GetValue(target);
    }

    private static int ReadInt(object target, string fieldName)
    {
        return (int)target.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .GetValue(target);
    }
}
