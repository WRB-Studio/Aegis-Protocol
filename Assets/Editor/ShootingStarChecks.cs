using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public class ShootingStarChecks : ScriptableObject, ICallbacks
{
    const string RunningKey = "Aegis.ShootingStarChecks.Running";

    [InitializeOnLoadMethod]
    static void CheckRequest()
    {
        if (SessionState.GetBool(RunningKey, false))
            CreateInstance<TestRunnerApi>().RegisterCallbacks(CreateInstance<ShootingStarChecks>());
        if (File.Exists("Temp/ShootingStarChecks.request")) EditorApplication.delayCall += Run;
    }

    [MenuItem("Tools/Aegis/Test shooting stars")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            File.Exists("Temp/UpgradeUiSetup.request") || File.Exists("Temp/UiSymbolSetup.request") || File.Exists("Temp/ShootingStarSetup.request"))
        {
            EditorApplication.delayCall += Run;
            return;
        }
        if (SessionState.GetBool(RunningKey, false)) return;
        File.Delete("Temp/ShootingStarChecks.request");
        Directory.CreateDirectory("Temp");
        File.WriteAllText("Temp/ShootingStarChecks.result", "Running PlayMode tests.");
        SessionState.SetBool(RunningKey, true);
        var api = CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(CreateInstance<ShootingStarChecks>());
        api.Execute(new ExecutionSettings(new Filter
        {
            testMode = TestMode.PlayMode,
            testNames = new[] { "GameFlowTests.ShootingStarRequiresTapAndAwardsOnlyOnce", "GameFlowTests.ShootingStarExpiryPauseAndReplayDoNotAwardMaterials", "GameFlowTests.ShootingStarPathsUseEveryEdgeAndCrossThePlayableScreen" }
        }));
    }

    public void RunStarted(ITestAdaptor testsToRun) { }
    public void TestStarted(ITestAdaptor test) { }
    public void TestFinished(ITestResultAdaptor result) { }
    public void RunFinished(ITestResultAdaptor result)
    {
        TestRunnerApi.SaveResultToFile(result, "Temp/ShootingStarChecks.xml");
        File.WriteAllText("Temp/ShootingStarChecks.result",
            "Passed: " + result.PassCount + ", Failed: " + result.FailCount + ", Skipped: " + result.SkipCount);
        SessionState.SetBool(RunningKey, false);
    }
}
