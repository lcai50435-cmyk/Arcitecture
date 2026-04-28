using System.Reflection;
using NUnit.Framework;
using UnityEngine.SceneManagement;

public sealed class RuntimePhotoCaptureManagerTests
{
    [Test]
    public void AdditiveNonGameplaySceneKeepsActiveGameplayCaptureContext()
    {
        Assert.AreEqual(
            "GameScene",
            ResolvePreparedSceneName("IllustratedUIScene", LoadSceneMode.Additive, "GameScene"));
    }

    [Test]
    public void SingleLoadedSceneBecomesCaptureContext()
    {
        Assert.AreEqual(
            "NewBase",
            ResolvePreparedSceneName("NewBase", LoadSceneMode.Single, "GameScene"));
    }

    private static string ResolvePreparedSceneName(
        string loadedSceneName,
        LoadSceneMode loadMode,
        string activeSceneName)
    {
        MethodInfo method = typeof(RuntimePhotoCaptureManager).GetMethod(
            "ResolvePreparedSceneName",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            new[] { typeof(string), typeof(LoadSceneMode), typeof(string) },
            null);
        Assert.IsNotNull(method);

        object resolved = method.Invoke(null, new object[] { loadedSceneName, loadMode, activeSceneName });
        return (string)resolved;
    }
}
