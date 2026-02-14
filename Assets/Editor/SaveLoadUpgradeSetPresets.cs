#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SaveLoadUpgradeSetPresets
{
    const string FOLDER_NAME = "Presets/UpgradeSetPresets"; // under Assets/

    [Serializable]
    class UpgradeSetEntry
    {
        public string assetPath;
        public string json;
    }

    [Serializable]
    class PresetFile
    {
        public List<UpgradeSetEntry> sets = new();
    }

    static string FolderPath => Path.Combine(Application.dataPath, FOLDER_NAME);

    public static void Save(string presetName, UnityEngine.Object[] upgradeSets)
    {
        if (string.IsNullOrWhiteSpace(presetName)) presetName = "Preset";
        if (upgradeSets == null || upgradeSets.Length == 0) return;

        var file = new PresetFile();

        foreach (var set in upgradeSets)
        {
            if (!set) continue;
            var path = AssetDatabase.GetAssetPath(set);
            if (string.IsNullOrEmpty(path)) continue;

            file.sets.Add(new UpgradeSetEntry
            {
                assetPath = path,
                json = EditorJsonUtility.ToJson(set)
            });
        }

        if (!Directory.Exists(FolderPath))
            Directory.CreateDirectory(FolderPath);

        var fullPath = GetPresetPath(presetName);
        File.WriteAllText(fullPath, JsonUtility.ToJson(file, true));
        AssetDatabase.Refresh();

        Debug.Log($"[UpgradeSetPreset] Saved: {fullPath}");
    }

    static string GetPresetPath(string presetName)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            presetName = presetName.Replace(c, '_');

        return Path.Combine(FolderPath, presetName + ".json");
    }

    public static void LoadFromPath(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
        {
            Debug.LogWarning($"[UpgradeSetPreset] File not found: {fullPath}");
            return;
        }

        string text = File.ReadAllText(fullPath);
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("[UpgradeSetPreset] File empty.");
            return;
        }

        PresetFile file;
        try
        {
            file = JsonUtility.FromJson<PresetFile>(text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UpgradeSetPreset] JSON parse failed: {ex.Message}");
            return;
        }

        if (file == null || file.sets == null || file.sets.Count == 0)
        {
            // Sehr wahrscheinlich falsches Preset (z.B. ModulePreset)
            Debug.LogWarning(
                "[UpgradeSetPreset] No 'sets' found. Wrong preset type?\n" +
                "Expected: UpgradeSetPreset (contains 'sets' with assetPath+json)."
            );
            return;
        }

        int applied = 0, missing = 0;

        foreach (var e in file.sets)
        {
            if (string.IsNullOrEmpty(e.assetPath) || string.IsNullOrEmpty(e.json)) continue;

            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(e.assetPath);
            if (!obj) { missing++; continue; }

            Undo.RecordObject(obj, "Load UpgradeSet Preset");
            EditorJsonUtility.FromJsonOverwrite(e.json, obj);
            EditorUtility.SetDirty(obj);
            applied++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[UpgradeSetPreset] Loaded: {Path.GetFileName(fullPath)} | applied={applied} missing={missing}");
    }


}
#endif
