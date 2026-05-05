using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public sealed class MissingScriptBuildGuard : IPreprocessBuildWithReport
{
    public int callbackOrder => -900;

    public void OnPreprocessBuild(BuildReport report)
    {
        List<string> issues = MissingScriptDiagnostics.CollectMissingScriptIssues(false);
        if (issues.Count == 0)
        {
            return;
        }

        throw new BuildFailedException(
            $"Build blocked because {issues.Count} missing script references were found.\n" +
            string.Join("\n", issues));
    }
}
