using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class FirstPassV2VerificationRunner
{
    private static TestRunnerApi testRunnerApi;
    private static VerificationCallbacks callbacks;

    [MenuItem("Tools/Architecture/Run Project EditMode Tests")]
    public static void RunEditModeTests()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
        callbacks = new VerificationCallbacks();
        testRunnerApi.RegisterCallbacks(callbacks);
        testRunnerApi.Execute(new ExecutionSettings(new Filter
        {
            testMode = TestMode.EditMode
        }));
    }

    private sealed class VerificationCallbacks : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun)
        {
            Debug.Log($"开始运行 EditMode 测试：{testsToRun.TestCaseCount} 项。");
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("Project EditMode Test Result");
            report.AppendLine($"Result: {result.ResultState}");
            report.AppendLine($"Pass: {result.PassCount}");
            report.AppendLine($"Fail: {result.FailCount}");
            report.AppendLine($"Skip: {result.SkipCount}");
            report.AppendLine($"Duration: {result.Duration:0.000}s");
            AppendFailures(result, report);

            string outputPath = Path.GetFullPath("../by-product/EditMode测试结果-手动运行.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, report.ToString(), Encoding.UTF8);
            Debug.Log(report.ToString());
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
        }

        private static void AppendFailures(ITestResultAdaptor result, StringBuilder report)
        {
            if (result.HasChildren)
            {
                foreach (ITestResultAdaptor child in result.Children)
                {
                    AppendFailures(child, report);
                }

                return;
            }

            if (result.FailCount <= 0)
            {
                return;
            }

            report.AppendLine();
            report.AppendLine($"FAILED: {result.FullName}");
            report.AppendLine(result.Message);
            report.AppendLine(result.StackTrace);
        }
    }
}
