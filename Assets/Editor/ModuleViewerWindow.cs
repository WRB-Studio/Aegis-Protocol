#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ModuleEditorWindow : EditorWindow
{
    const string MODULE_TYPE = "StationModule";

    Vector2 scroll;
    Type moduleBaseType;
    Component[] modules;

    readonly Dictionary<int, bool> foldout = new();
    readonly Dictionary<int, SerializedObject> soCache = new();

    string presetName = "Preset_01";
    float costMultiplier = 1f;
    string droppedPresetPath;

    [MenuItem("Tools/Aegis/Module Editor")]
    static void Open() => GetWindow<ModuleEditorWindow>("Module Editor");

    void OnEnable()
    {
        RefreshFull();
        EditorApplication.hierarchyChanged += RefreshModulesOnly;
    }

    void OnDisable()
    {
        EditorApplication.hierarchyChanged -= RefreshModulesOnly;
        soCache.Clear();
        foldout.Clear();
    }

    void RefreshFull()
    {
        moduleBaseType = FindTypeByName(MODULE_TYPE);
        RefreshModulesOnly();
    }

    void RefreshModulesOnly()
    {
        if (moduleBaseType == null)
        {
            modules = Array.Empty<Component>();
            Repaint();
            return;
        }

        var list = new List<Component>();

        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (!mb) continue;
            if (moduleBaseType.IsAssignableFrom(mb.GetType()))
                list.Add(mb);
        }

        modules = list.ToArray();
        CleanupCaches();
        Repaint();
    }

    void CleanupCaches()
    {
        if (modules == null) return;

        var alive = new HashSet<int>();
        foreach (var m in modules)
            if (m) alive.Add(m.GetInstanceID());

        var foldKeys = new List<int>(foldout.Keys);
        foreach (var k in foldKeys)
            if (!alive.Contains(k)) foldout.Remove(k);

        var soKeys = new List<int>(soCache.Keys);
        foreach (var k in soKeys)
            if (!alive.Contains(k)) soCache.Remove(k);
    }

    void OnGUI()
    {
        DrawHeader();

        if (moduleBaseType == null)
        {
            EditorGUILayout.HelpBox($"Type '{MODULE_TYPE}' nicht gefunden.", MessageType.Warning);
            return;
        }

        if (modules == null || modules.Length == 0)
        {
            EditorGUILayout.HelpBox("Keine Module in der Scene gefunden.", MessageType.Info);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (var m in modules)
        {
            if (!m) continue;

            int id = m.GetInstanceID();
            if (!foldout.ContainsKey(id)) foldout[id] = false;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    foldout[id] = EditorGUILayout.Foldout(foldout[id], m.name, true);

                    GUILayout.FlexibleSpace();
                    GUILayout.Label(m.GetType().Name, EditorStyles.miniLabel);

                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                        Selection.activeObject = m.gameObject;
                }

                if (!foldout[id]) continue;

                var so = GetSO(m);
                so.Update();

                DrawComponentInspector(so);

                if (so.ApplyModifiedProperties())
                    EditorUtility.SetDirty(m);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawHeader()
    {
        // -------- ZEILE 1 (Actions + Presets) --------
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            if (GUILayout.Button("Expand All", GUILayout.Width(100)))
                SetAllFoldouts(true);

            if (GUILayout.Button("Collapse All", GUILayout.Width(110)))
                SetAllFoldouts(false);

            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                soCache.Clear();
                foldout.Clear();
                RefreshFull();
            }

            GUILayout.Space(15);
            GUILayout.Label(" | ", GUILayout.Width(20));

            GUILayout.Label("Preset", GUILayout.Width(45));
            presetName = GUILayout.TextField(presetName, GUILayout.Width(140));

            using (new EditorGUI.DisabledScope(modules == null || modules.Length == 0))
            {
                if (GUILayout.Button("Save", GUILayout.Width(60)))
                    SaveLoadStationModulPresets.Save(presetName, modules);

                DrawPresetDropZone();
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label(" | ", GUILayout.Width(20));
            GUILayout.Label($"Found: {modules?.Length ?? 0}");
        }

        // -------- ZEILE 2 (Cost Multiplier) --------
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("Cost Multiplier", GUILayout.Width(100));

            costMultiplier = EditorGUILayout.FloatField(costMultiplier, GUILayout.Width(80));

            using (new EditorGUI.DisabledScope(
                modules == null ||
                modules.Length == 0 ||
                Mathf.Approximately(costMultiplier, 1f)))
            {
                if (GUILayout.Button("Apply", GUILayout.Width(150)))
                    ApplyCostMultiplier(costMultiplier);
            }

            GUILayout.FlexibleSpace();
        }
    }


    void ApplyCostMultiplier(float mult)
    {
        if (modules == null || modules.Length == 0) return;

        Undo.RecordObjects(modules, $"Apply Cost Multiplier x{mult}");

        foreach (var m in modules)
        {
            if (!m) continue;

            var so = new SerializedObject(m);
            var it = so.GetIterator();
            bool enterChildren = true;

            while (it.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (it.propertyPath == "m_Script") continue;

                string name = it.name.ToLowerInvariant();
                if (!name.Contains("cost")) continue;
                if (name == "costmultiplier") continue;

                if (it.propertyType == SerializedPropertyType.Integer)
                {
                    it.intValue = Mathf.RoundToInt(it.intValue * mult);
                }
                else if (it.propertyType == SerializedPropertyType.Float)
                {
                    it.floatValue *= mult;
                }
            }

            if (so.ApplyModifiedProperties())
                EditorUtility.SetDirty(m);
        }

        // optional: nach einmal anwenden wieder auf 1 setzen
        costMultiplier = 1f;

        RefreshModulesOnly();
    }

    void DrawPresetDropZone()
    {
        GUILayout.Space(10);

        var r = GUILayoutUtility.GetRect(180, 20, GUILayout.Width(220));
        GUI.Box(r, "Load: Drop Module Preset JSON here", EditorStyles.helpBox);

        var e = Event.current;
        if (!r.Contains(e.mousePosition)) return;

        if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return;

        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

        if (e.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();

            string fullPath = null;

            // 1) TextAsset aus Project
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is not TextAsset ta) continue;

                var assetPath = AssetDatabase.GetAssetPath(ta);
                if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    continue;

                presetName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

                // assetPath -> fullPath
                fullPath = System.IO.Path.Combine(
                    System.IO.Directory.GetParent(Application.dataPath).FullName,
                    assetPath
                );
                break;
            }

            // 2) Fallback: direkter File-Pfad
            if (fullPath == null && DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
            {
                var p = DragAndDrop.paths[0];
                if (!string.IsNullOrEmpty(p) && p.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    presetName = System.IO.Path.GetFileNameWithoutExtension(p);
                    fullPath = p;
                }
            }

            if (fullPath != null)
            {
                SaveLoadStationModulPresets.LoadFromPath(fullPath, modules);
                RefreshModulesOnly();
                Repaint();
            }
        }

        e.Use();
    }


    void SetAllFoldouts(bool state)
    {
        if (modules == null) return;

        foreach (var m in modules)
            if (m) foldout[m.GetInstanceID()] = state;

        Repaint();
    }

    SerializedObject GetSO(Component c)
    {
        int id = c.GetInstanceID();
        if (!soCache.TryGetValue(id, out var so) || so == null)
        {
            so = new SerializedObject(c);
            soCache[id] = so;
        }
        return so;
    }

    static void DrawComponentInspector(SerializedObject so)
    {
        var it = so.GetIterator();
        bool enterChildren = true;

        while (it.NextVisible(enterChildren))
        {
            if (it.propertyPath == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(it, true);
            }
            else
            {
                EditorGUILayout.PropertyField(it, true);
            }

            enterChildren = false;
        }
    }

    static Type FindTypeByName(string typeName)
    {
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = a.GetTypes(); } catch { continue; }

            foreach (var t in types)
                if (t != null && (t.Name == typeName || t.FullName == typeName))
                    return t;
        }
        return null;
    }
}
#endif
