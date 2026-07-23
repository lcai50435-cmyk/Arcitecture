using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FirstPassV2SmokeRunner
{
    private const string V2ScenePath = "Assets/Scenes/FirstPass_V2.unity";
    private const string MainScenePath = "Assets/Scenes/MainScene.unity";
    private static double enteredPlayTime;
    private static bool stressWaveSpawned;
    private static int runtimeErrorCount;
    private static int activeEnemyCount;

    [MenuItem("Tools/Architecture/Run FirstPass V2 Smoke Test")]
    public static void RunSmokeTest()
    {
        SceneAsset v2Scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(V2ScenePath);
        if (v2Scene == null)
        {
            Debug.LogError($"未找到 V2 场景：{V2ScenePath}");
            return;
        }

        EditorSceneManager.playModeStartScene = v2Scene;
        EditorSceneManager.OpenScene(V2ScenePath, OpenSceneMode.Single);
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        Application.logMessageReceived -= HandleRuntimeLog;
        Application.logMessageReceived += HandleRuntimeLog;
        runtimeErrorCount = 0;
        activeEnemyCount = 0;
        stressWaveSpawned = false;
        EditorApplication.isPlaying = true;
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            enteredPlayTime = EditorApplication.timeSinceStartup;
            EditorApplication.update -= TickSmokeTest;
            EditorApplication.update += TickSmokeTest;
            return;
        }

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            RestoreMainStartScene();
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.update -= TickSmokeTest;
            Application.logMessageReceived -= HandleRuntimeLog;
        }
    }

    private static void TickSmokeTest()
    {
        double elapsed = EditorApplication.timeSinceStartup - enteredPlayTime;
        if (!stressWaveSpawned && elapsed >= 4d)
        {
            GameplayStressTestController controller = Object.FindObjectOfType<GameplayStressTestController>();
            if (controller != null)
            {
                controller.SpawnStressWave();
                stressWaveSpawned = true;
            }
        }

        if (elapsed < 10d)
        {
            return;
        }

        activeEnemyCount = Object.FindObjectsOfType<EnemyStatsManager>().Length;
        WriteResult();
        EditorApplication.update -= TickSmokeTest;
        EditorApplication.isPlaying = false;
    }

    private static void HandleRuntimeLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            runtimeErrorCount++;
        }
    }

    private static void WriteResult()
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("FirstPass V2 Smoke Test");
        report.AppendLine($"Stress wave triggered: {stressWaveSpawned}");
        report.AppendLine($"Active enemies after 10s: {activeEnemyCount}");
        report.AppendLine($"Runtime errors: {runtimeErrorCount}");

        string outputPath = Path.GetFullPath("../by-product/首轮V2冒烟测试.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        File.WriteAllText(outputPath, report.ToString(), Encoding.UTF8);
        Debug.Log(report.ToString());
    }

    private static void RestoreMainStartScene()
    {
        SceneAsset mainScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath);
        if (mainScene != null)
        {
            EditorSceneManager.playModeStartScene = mainScene;
        }
    }
}
