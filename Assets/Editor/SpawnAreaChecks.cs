using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public class SpawnAreaChecks : ScriptableObject, ICallbacks
{
    const string RunningKey = "Aegis.SpawnAreaChecks.Running";

    [InitializeOnLoadMethod]
    static void CheckRequest()
    {
        if (SessionState.GetBool(RunningKey, false))
            CreateInstance<TestRunnerApi>().RegisterCallbacks(CreateInstance<SpawnAreaChecks>());
        if (File.Exists("Temp/SpawnAreaChecks.request")) EditorApplication.delayCall += Run;
    }

    [MenuItem("Tools/Aegis/Test camera spawn areas")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            File.Exists("Temp/UpgradeUiSetup.request") || File.Exists("Temp/UiSymbolSetup.request"))
        {
            EditorApplication.delayCall += Run;
            return;
        }
        if (SessionState.GetBool(RunningKey, false)) return;
        File.Delete("Temp/SpawnAreaChecks.request");
        Directory.CreateDirectory("Temp");
        File.WriteAllText("Temp/SpawnAreaChecks.result", "Running PlayMode tests.");
        SessionState.SetBool(RunningKey, true);
        var api = CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(CreateInstance<SpawnAreaChecks>());
        api.Execute(new ExecutionSettings(new Filter
        {
            testMode = TestMode.PlayMode,
            testNames = new[] { "GameFlowTests.SpawnAreasKeepAllShipVariantsOutsideDifferentViewports", "GameFlowTests.ActualWaveSpawnsOutsideCameraBeforeEnemiesMove" }
        }));
    }

    public void RunStarted(ITestAdaptor testsToRun) { }
    public void TestStarted(ITestAdaptor test) { }
    public void TestFinished(ITestResultAdaptor result) { }
    public void RunFinished(ITestResultAdaptor result)
    {
        TestRunnerApi.SaveResultToFile(result, "Temp/SpawnAreaChecks.xml");
        File.WriteAllText("Temp/SpawnAreaChecks.result",
            "Passed: " + result.PassCount + ", Failed: " + result.FailCount + ", Skipped: " + result.SkipCount);
        SessionState.SetBool(RunningKey, false);
    }
}
