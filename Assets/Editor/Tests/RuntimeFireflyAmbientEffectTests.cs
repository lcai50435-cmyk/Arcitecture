using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

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

    private static Type ResolveEffectType()
    {
        return Type.GetType("RuntimeFireflyAmbientEffect, Assembly-CSharp");
    }
}
