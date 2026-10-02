using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public class UpgradeUiChecks : ScriptableObject, ICallbacks
{
    const string RunningKey = "Aegis.UpgradeUiChecks.Running";

    [InitializeOnLoadMethod]
    static void CheckRequest()
    {
        if (SessionState.GetBool(RunningKey, false))
            CreateInstance<TestRunnerApi>().RegisterCallbacks(CreateInstance<UpgradeUiChecks>());
        if (File.Exists("Temp/UpgradeUiChecks.request")) EditorApplication.delayCall += Run;
    }

    [MenuItem("Tools/Aegis/Test prepared upgrade UI")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            File.Exists("Temp/UpgradeUiSetup.request") || File.Exists("Temp/UiSymbolSetup.request"))
        {
            EditorApplication.delayCall += Run;
            return;
        }
        if (SessionState.GetBool(RunningKey, false)) return;
        File.Delete("Temp/UpgradeUiChecks.request");
        Directory.CreateDirectory("Temp");
        File.WriteAllText("Temp/UpgradeUiChecks.result", "Running PlayMode tests.");
        SessionState.SetBool(RunningKey, true);
        var api = CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(CreateInstance<UpgradeUiChecks>());
        api.Execute(new ExecutionSettings(new Filter
        {
            testMode = TestMode.PlayMode,
            testNames = new[]
            {
                "GameFlowTests.UpgradeButtons_RemainReadableWhenUnselectedAndSelected",
                "GameFlowTests.PreparedUpgradeButtonsReuseInstancesAndShowOnlySelectedModule",
                "GameFlowTests.RapidUpgradeClicksUseCurrentPricesAndCannotOverspend",
                "GameFlowTests.TargetPriorityChargesOnceAndPersistsThroughModuleLossAndLoad"
            }
        }));
    }

    public void RunStarted(ITestAdaptor testsToRun) { }
    public void TestStarted(ITestAdaptor test) { }
    public void TestFinished(ITestResultAdaptor result) { }
    public void RunFinished(ITestResultAdaptor result)
    {
        TestRunnerApi.SaveResultToFile(result, "Temp/UpgradeUiChecks.xml");
        File.WriteAllText("Temp/UpgradeUiChecks.result",
            "Passed: " + result.PassCount + ", Failed: " + result.FailCount + ", Skipped: " + result.SkipCount);
        SessionState.SetBool(RunningKey, false);
    }
}
