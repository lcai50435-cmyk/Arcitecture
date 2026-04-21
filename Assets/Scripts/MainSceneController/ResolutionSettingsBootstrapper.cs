using UnityEngine;
using UnityEngine.SceneManagement;

public static class ResolutionSettingsBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        GameSettingsStore.ApplyDisplaySettings();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BindSceneEvents()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        GameSettingsStore.ApplyAudioSettings();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameSettingsStore.ApplyDisplaySettings();
        GameSettingsStore.ApplyAudioSettings();
    }
}
