using NUnit.Framework;

public sealed class GameSettingsStoreTests
{
    [Test]
    public void WebGlPlayerDoesNotApplyExplicitResolution()
    {
        Assert.IsFalse(GameSettingsStore.ShouldApplyExplicitResolutionForPlatform(true));
    }

    [Test]
    public void StandaloneRuntimeAppliesExplicitResolution()
    {
        Assert.IsTrue(GameSettingsStore.ShouldApplyExplicitResolutionForPlatform(false));
    }

    [Test]
    public void WebGlPlayerDoesNotWaitForExplicitDisplayMatch()
    {
        Assert.IsFalse(GameSettingsStore.ShouldWaitForExplicitDisplayMatchForPlatform(true));
    }

    [Test]
    public void StandaloneRuntimeWaitsForExplicitDisplayMatch()
    {
        Assert.IsTrue(GameSettingsStore.ShouldWaitForExplicitDisplayMatchForPlatform(false));
    }
}
