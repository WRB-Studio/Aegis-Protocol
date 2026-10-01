using System.IO;
using UnityEditor;
using UnityEngine;

public static class MatchReportMenu
{
    [MenuItem("Aegis/Match Reports/Open Reports Folder")]
    public static void OpenReportsFolder()
    {
        string directory = Path.Combine(Application.persistentDataPath, "MatchReports");
        Directory.CreateDirectory(directory);
        EditorUtility.RevealInFinder(directory);
    }

    [MenuItem("Aegis/Match Reports/Open Latest Analysis")]
    public static void OpenLatestAnalysis()
    {
        MatchReporter.Current?.Checkpoint();
        string directory = Path.Combine(Application.persistentDataPath, "MatchReports");
        string latest = null;
        if (Directory.Exists(directory))
            foreach (string file in Directory.GetFiles(directory, "analysis.md", SearchOption.AllDirectories))
                if (latest == null || File.GetLastWriteTimeUtc(file) > File.GetLastWriteTimeUtc(latest)) latest = file;
        if (latest != null) EditorUtility.OpenWithDefaultApp(latest);
        else Debug.Log("No match analysis recorded yet. Start a match first.");
    }
}
