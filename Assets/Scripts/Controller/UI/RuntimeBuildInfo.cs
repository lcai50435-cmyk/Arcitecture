using System;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class RuntimeBuildInfoSnapshot
{
    public string bundleVersion;
    public string buildNumber;
    public string gitShortSha;
    public string gitDescribe;
    public bool isDirty;
    public string generatedAtUtc;
}

public sealed class RuntimeBuildInfoData
{
    public RuntimeBuildInfoData(
        RuntimeBuildInfoSnapshot snapshot,
        string environmentLabel,
        string bundleVersion,
        string buildNumber,
        string gitShortSha,
        string gitDescribe,
        bool isDirty)
    {
        Snapshot = snapshot;
        EnvironmentLabel = string.IsNullOrWhiteSpace(environmentLabel) ? "PROD" : environmentLabel.Trim().ToUpperInvariant();
        BundleVersion = string.IsNullOrWhiteSpace(bundleVersion) ? "0.0.0" : bundleVersion.Trim();
        BuildNumber = string.IsNullOrWhiteSpace(buildNumber) ? string.Empty : buildNumber.Trim();
        GitShortSha = string.IsNullOrWhiteSpace(gitShortSha) ? string.Empty : gitShortSha.Trim();
        GitDescribe = string.IsNullOrWhiteSpace(gitDescribe) ? string.Empty : gitDescribe.Trim();
        IsDirty = isDirty;
        DisplayPrimaryText = $"{EnvironmentLabel} v{BundleVersion}";
        DisplaySecondaryText = BuildSecondaryText();
    }

    public RuntimeBuildInfoSnapshot Snapshot { get; }
    public string EnvironmentLabel { get; }
    public string BundleVersion { get; }
    public string BuildNumber { get; }
    public string GitShortSha { get; }
    public string GitDescribe { get; }
    public bool IsDirty { get; }
    public string DisplayPrimaryText { get; }
    public string DisplaySecondaryText { get; }

    private string BuildSecondaryText()
    {
        if (!string.Equals(EnvironmentLabel, "DEV", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        string buildSegment = string.IsNullOrWhiteSpace(BuildNumber) ? string.Empty : $"build {BuildNumber}";
        string commitSegment = GitShortSha;

        if (string.IsNullOrWhiteSpace(buildSegment) && string.IsNullOrWhiteSpace(commitSegment))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(buildSegment))
        {
            return commitSegment;
        }

        if (string.IsNullOrWhiteSpace(commitSegment))
        {
            return buildSegment;
        }

        return $"{buildSegment} | {commitSegment}";
    }
}

public static class RuntimeBuildInfo
{
    private const string SnapshotFileName = "runtime-build-info.json";

    private static RuntimeBuildInfoData current;

    public static RuntimeBuildInfoData Current
    {
        get
        {
            if (current == null)
            {
                current = LoadCurrent();
            }

            return current;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetCache()
    {
        current = null;
    }

    private static RuntimeBuildInfoData LoadCurrent()
    {
        RuntimeBuildInfoSnapshot snapshot = LoadSnapshot();
        string bundleVersion = ResolveBundleVersion(snapshot);

        return new RuntimeBuildInfoData(
            snapshot,
            ResolveEnvironmentLabel(),
            bundleVersion,
            snapshot != null ? snapshot.buildNumber : string.Empty,
            snapshot != null ? snapshot.gitShortSha : string.Empty,
            snapshot != null ? snapshot.gitDescribe : string.Empty,
            snapshot != null && snapshot.isDirty);
    }

    private static RuntimeBuildInfoSnapshot LoadSnapshot()
    {
        string snapshotPath = Path.Combine(Application.streamingAssetsPath, SnapshotFileName);
        if (!File.Exists(snapshotPath))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(snapshotPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonUtility.FromJson<RuntimeBuildInfoSnapshot>(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"读取运行时版本快照失败：{exception.Message}");
            return null;
        }
    }

    private static string ResolveBundleVersion(RuntimeBuildInfoSnapshot snapshot)
    {
        if (snapshot != null && !string.IsNullOrWhiteSpace(snapshot.bundleVersion))
        {
            return snapshot.bundleVersion;
        }

        return string.IsNullOrWhiteSpace(Application.version) ? "0.0.0" : Application.version;
    }

    private static string ResolveEnvironmentLabel()
    {
        if (IsDevelopmentEnvironment())
        {
            return "DEV";
        }

        return "PROD";
    }

    private static bool IsDevelopmentEnvironment()
    {
#if UNITY_EDITOR
        return true;
#else
        if (Debug.isDebugBuild)
        {
            return true;
        }

#if DEVELOPMENT_BUILD
        return true;
#else
        return false;
#endif
#endif
    }
}
