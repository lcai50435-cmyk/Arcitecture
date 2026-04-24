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
    private const string ResourceSnapshotAssetPath = "Assets/Resources/RuntimeBuildInfo/runtime-build-info.json";
    private const string BuildVersionArgument = "-arcitectureVersion";
    private const string BuildNumberArgument = "-arcitectureBuildNumber";
    private const string GitShaArgument = "-arcitectureGitSha";
    private const string GitDescribeArgument = "-arcitectureGitDescribe";

    static RuntimeBuildInfoGenerator()
    {
        EditorApplication.delayCall += RefreshSnapshotOnLoad;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    public static void RefreshSnapshot()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        RuntimeBuildInfoSnapshot snapshot = BuildSnapshot(projectRoot);
        string content = JsonUtility.ToJson(snapshot, true) + Environment.NewLine;

        WriteSnapshotFile(projectRoot, SnapshotAssetPath, content);
        WriteSnapshotFile(projectRoot, ResourceSnapshotAssetPath, content);
    }

    private static void WriteSnapshotFile(string projectRoot, string assetPath, string content)
    {
        string snapshotAbsolutePath = Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        string snapshotDirectory = Path.GetDirectoryName(snapshotAbsolutePath);
        if (string.IsNullOrWhiteSpace(snapshotDirectory))
        {
            return;
        }

        Directory.CreateDirectory(snapshotDirectory);

        bool fileChanged = !File.Exists(snapshotAbsolutePath) || !string.Equals(File.ReadAllText(snapshotAbsolutePath), content, StringComparison.Ordinal);
        if (!fileChanged)
        {
            return;
        }

        File.WriteAllText(snapshotAbsolutePath, content, new UTF8Encoding(false));
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
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
            bundleVersion = ResolveConfiguredValue("ARCITECTURE_BUILD_VERSION", BuildVersionArgument, PlayerSettings.bundleVersion),
            buildNumber = string.Empty,
            gitShortSha = string.Empty,
            gitDescribe = string.Empty,
            isDirty = false,
            generatedAtUtc = DateTime.UtcNow.ToString("O")
        };

        string configuredBuildNumber = ResolveConfiguredValue("ARCITECTURE_BUILD_NUMBER", BuildNumberArgument, string.Empty);
        if (!string.IsNullOrWhiteSpace(configuredBuildNumber))
        {
            snapshot.buildNumber = configuredBuildNumber;
        }
        else if (TryReadGitValue(projectRoot, "rev-list --count HEAD", out string buildNumber))
        {
            snapshot.buildNumber = buildNumber;
        }

        string configuredGitSha = ResolveConfiguredValue("ARCITECTURE_GIT_SHA", GitShaArgument, string.Empty);
        if (!string.IsNullOrWhiteSpace(configuredGitSha))
        {
            snapshot.gitShortSha = ShortenGitSha(configuredGitSha);
        }
        else if (TryReadGitValue(projectRoot, "rev-parse --short HEAD", out string gitShortSha))
        {
            snapshot.gitShortSha = gitShortSha;
        }

        string configuredGitDescribe = ResolveConfiguredValue("ARCITECTURE_GIT_DESCRIBE", GitDescribeArgument, string.Empty);
        if (!string.IsNullOrWhiteSpace(configuredGitDescribe))
        {
            snapshot.gitDescribe = configuredGitDescribe;
            snapshot.isDirty = configuredGitDescribe.EndsWith("-dirty", StringComparison.OrdinalIgnoreCase);
        }
        else if (TryReadGitValue(projectRoot, "describe --tags --always --dirty", out string gitDescribe))
        {
            snapshot.gitDescribe = gitDescribe;
            snapshot.isDirty = gitDescribe.EndsWith("-dirty", StringComparison.OrdinalIgnoreCase);
        }

        return snapshot;
    }

    private static string ResolveConfiguredValue(string environmentVariable, string argumentName, string fallback)
    {
        string value = Environment.GetEnvironmentVariable(environmentVariable);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        if (TryReadCommandLineArgument(argumentName, out value))
        {
            return value.Trim();
        }

        return string.IsNullOrWhiteSpace(fallback) ? string.Empty : fallback.Trim();
    }

    private static bool TryReadCommandLineArgument(string argumentName, out string value)
    {
        value = string.Empty;
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (string.Equals(arg, argumentName, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length)
                {
                    value = args[i + 1];
                    return !string.IsNullOrWhiteSpace(value);
                }

                return false;
            }

            string prefix = argumentName + "=";
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                value = arg.Substring(prefix.Length);
                return !string.IsNullOrWhiteSpace(value);
            }
        }

        return false;
    }

    private static string ShortenGitSha(string value)
    {
        string trimmed = value.Trim();
        return trimmed.Length > 7 ? trimmed.Substring(0, 7) : trimmed;
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
