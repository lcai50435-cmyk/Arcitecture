using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

[InitializeOnLoad]
public static class RuntimeBuildInfoGenerator
{
    private const string SnapshotAssetPath = "Assets/StreamingAssets/runtime-build-info.json";

    static RuntimeBuildInfoGenerator()
    {
        EditorApplication.delayCall += RefreshSnapshotOnLoad;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    public static void RefreshSnapshot()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string snapshotAbsolutePath = Path.Combine(projectRoot, "Assets/StreamingAssets/runtime-build-info.json");
        string snapshotDirectory = Path.GetDirectoryName(snapshotAbsolutePath);
        if (string.IsNullOrWhiteSpace(snapshotDirectory))
        {
            return;
        }

        Directory.CreateDirectory(snapshotDirectory);

        RuntimeBuildInfoSnapshot snapshot = BuildSnapshot(projectRoot);
        string content = JsonUtility.ToJson(snapshot, true) + Environment.NewLine;

        bool fileChanged = !File.Exists(snapshotAbsolutePath) || !string.Equals(File.ReadAllText(snapshotAbsolutePath), content, StringComparison.Ordinal);
        if (!fileChanged)
        {
            return;
        }

        File.WriteAllText(snapshotAbsolutePath, content, new UTF8Encoding(false));
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(SnapshotAssetPath, ImportAssetOptions.ForceUpdate);
    }

    private static void RefreshSnapshotOnLoad()
    {
        try
        {
            RefreshSnapshot();
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogWarning($"刷新运行时版本快照失败：{exception.Message}");
        }
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
        {
            return;
        }

        RefreshSnapshotOnLoad();
    }

    private static RuntimeBuildInfoSnapshot BuildSnapshot(string projectRoot)
    {
        RuntimeBuildInfoSnapshot snapshot = new RuntimeBuildInfoSnapshot
        {
            bundleVersion = PlayerSettings.bundleVersion,
            buildNumber = string.Empty,
            gitShortSha = string.Empty,
            gitDescribe = string.Empty,
            isDirty = false,
            generatedAtUtc = DateTime.UtcNow.ToString("O")
        };

        if (TryReadGitValue(projectRoot, "rev-list --count HEAD", out string buildNumber))
        {
            snapshot.buildNumber = buildNumber;
        }

        if (TryReadGitValue(projectRoot, "rev-parse --short HEAD", out string gitShortSha))
        {
            snapshot.gitShortSha = gitShortSha;
        }

        if (TryReadGitValue(projectRoot, "describe --tags --always --dirty", out string gitDescribe))
        {
            snapshot.gitDescribe = gitDescribe;
            snapshot.isDirty = gitDescribe.EndsWith("-dirty", StringComparison.OrdinalIgnoreCase);
        }

        return snapshot;
    }

    private static bool TryReadGitValue(string workingDirectory, string arguments, out string value)
    {
        value = string.Empty;

        string[] gitCommands =
        {
            "git",
            "/usr/bin/git"
        };

        for (int i = 0; i < gitCommands.Length; i++)
        {
            if (!TryRunProcess(gitCommands[i], arguments, workingDirectory, out value))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    private static bool TryRunProcess(string fileName, string arguments, string workingDirectory, out string value)
    {
        value = string.Empty;

        try
        {
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                if (!process.Start())
                {
                    return false;
                }

                if (!process.WaitForExit(2500))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                    }

                    return false;
                }

                string stdout = process.StandardOutput.ReadToEnd();
                if (process.ExitCode != 0)
                {
                    return false;
                }

                value = stdout.Trim();
                return true;
            }
        }
        catch
        {
            return false;
        }
    }
}

public sealed class RuntimeBuildInfoBuildProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        RuntimeBuildInfoGenerator.RefreshSnapshot();
    }
}
