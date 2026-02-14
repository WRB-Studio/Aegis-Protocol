#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SaveLoadStationModulPresets
{
    const string FOLDER_NAME = "Presets/StationModulePresets"; // unter Assets/

    [Serializable]
    class ModuleEntry
    {
        public string moduleName;
        public string typeName; // optional, hilft beim Debug
        public string json;
    }

    [Serializable]
    class PresetFile
    {
        public List<ModuleEntry> modules = new();
    }

    static string FolderPath => Path.Combine(Application.dataPath, FOLDER_NAME);

    public static void Save(string presetName, Component[] modules)
    {
        if (string.IsNullOrWhiteSpace(presetName)) presetName = "Preset";
        if (modules == null || modules.Length == 0) return;

        var file = new PresetFile();

        foreach (var m in modules)
        {
            if (!m) continue;

            file.modules.Add(new ModuleEntry
            {
                moduleName = m.name,
                typeName = m.GetType().FullName,
                json = EditorJsonUtility.ToJson(m) // EditorJsonUtility kann mehr als JsonUtility
            });
        }

        if (!Directory.Exists(FolderPath))
            Directory.CreateDirectory(FolderPath);

        var path = GetPresetPath(presetName);
        File.WriteAllText(path, JsonUtility.ToJson(file, true));
        AssetDatabase.Refresh();

        Debug.Log($"[ModulePreset] Saved: {path}");
    }

    public static void LoadFromPath(string fullPath, Component[] modules)
    {
        if (modules == null || modules.Length == 0) return;

        if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
        {
            Debug.LogWarning($"[ModulePreset] File not found: {fullPath}");
            return;
        }

        string text = File.ReadAllText(fullPath);
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("[ModulePreset] File empty.");
            return;
        }

        PresetFile file;
        try
        {
            file = JsonUtility.FromJson<PresetFile>(text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ModulePreset] JSON parse failed: {ex.Message}");
            return;
        }

        if (file == null || file.modules == null || file.modules.Count == 0)
        {
            Debug.LogWarning(
                "[ModulePreset] No 'modules' found. Wrong preset type?\n" +
                "Expected: ModulePreset (contains 'modules' with moduleName+json)."
            );
            return;
        }

        Undo.RecordObjects(modules, "Load Module Preset");

        int applied = 0, missing = 0;

        foreach (var entry in file.modules)
        {
            if (string.IsNullOrEmpty(entry.moduleName) || string.IsNullOrEmpty(entry.json)) continue;

            Component target = null;
            for (int i = 0; i < modules.Length; i++)
            {
                var m = modules[i];
                if (m && m.name == entry.moduleName) { target = m; break; }
            }

            if (!target) { missing++; continue; }

            EditorJsonUtility.FromJsonOverwrite(entry.json, target);
            EditorUtility.SetDirty(target);
            applied++;
        }

        Debug.Log($"[ModulePreset] Loaded: {Path.GetFileName(fullPath)} | applied={applied} missing={missing}");
    }


    static string GetPresetPath(string presetName)
    {
        // minimal sanitize
        foreach (var c in Path.GetInvalidFileNameChars())
            presetName = presetName.Replace(c, '_');

        return Path.Combine(FolderPath, presetName + ".json");
    }
}
#endif
