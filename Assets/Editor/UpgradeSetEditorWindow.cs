#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class UpgradeSetEditorWindow : EditorWindow
{
    const string UPGRADESETS_PATH = "Assets/GameContent/Prefabs/UpgradeSets";
    const string UPGRADE_SET_TYPE = "UpgradeSet";
    const string LIST_FIELD = "upgradeAttributes"; // dein Feldname

    Vector2 scroll;

    readonly List<UnityEngine.Object> sets = new();
    readonly Dictionary<int, bool> foldout = new();
    readonly Dictionary<int, SerializedObject> soCache = new();

    string presetName = "Preset_01";
    float baseCostMultiplier = 1f;
    float costStepMultiplier = 1f;
    string droppedPath;

    [MenuItem("Tools/Aegis/UpgradeSets Editor")]
    static void Open() => GetWindow<UpgradeSetEditorWindow>("UpgradeSets Editor");

    void OnEnable()
    {
        Refresh(); // einmal initial
        // EditorApplication.projectChanged += Refresh;  // <-- RAUS
    }

    void OnDisable()
    {
        // EditorApplication.projectChanged -= Refresh;  // <-- RAUS
        soCache.Clear();
        foldout.Clear();
        sets.Clear();
    }

    void Refresh()
    {
        sets.Clear();
        soCache.Clear();
        foldout.Clear();

        var guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { UPGRADESETS_PATH });
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (!obj) continue;

            if (obj.GetType().Name == UPGRADE_SET_TYPE)
                sets.Add(obj);
        }

        Repaint();
    }

    void OnGUI()
    {
        DrawHeader();
        DrawMultiplierRow();

        if (sets.Count == 0)
        {
            EditorGUILayout.HelpBox($"Keine UpgradeSets gefunden unter:\n{UPGRADESETS_PATH}", MessageType.Info);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (var set in sets)
        {
            if (!set) continue;

            int id = set.GetInstanceID();
            if (!foldout.ContainsKey(id)) foldout[id] = false;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    foldout[id] = EditorGUILayout.Foldout(foldout[id], set.name, true);

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Ping", GUILayout.Width(55)))
                        EditorGUIUtility.PingObject(set);

                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                        Selection.activeObject = set;
                }

                if (!foldout[id]) continue;

                var so = GetSO(set);
                so.Update();

                var list = so.FindProperty(LIST_FIELD);
                if (list == null || !list.isArray)
                {
                    EditorGUILayout.HelpBox($"Feld '{LIST_FIELD}' nicht gefunden/kein Array.", MessageType.Warning);
                    DrawRawInspector(so);
                }
                else
                {
                    for (int i = 0; i < list.arraySize; i++)
                    {
                        var entry = list.GetArrayElementAtIndex(i);
                        if (entry == null) continue;

                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                            EditorGUILayout.PropertyField(entry, true);
                    }
                }

                if (so.ApplyModifiedProperties())
                    EditorUtility.SetDirty(set);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawHeader()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            if (GUILayout.Button("Expand", GUILayout.Width(70))) SetAllFoldouts(true);
            if (GUILayout.Button("Collapse", GUILayout.Width(80))) SetAllFoldouts(false);

            if (GUILayout.Button("Refresh", GUILayout.Width(70)))
                Refresh();

            GUILayout.Space(12);

            GUILayout.Label(" | ", GUILayout.Width(20));
            GUILayout.Label("Preset", GUILayout.Width(45));
            presetName = GUILayout.TextField(presetName, GUILayout.Width(160));

            using (new EditorGUI.DisabledScope(sets.Count == 0))
            {
                if (GUILayout.Button("Save", GUILayout.Width(60)))
                    SaveLoadUpgradeSetPresets.Save(presetName, sets.ToArray());

                DrawDropZone();
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label(" | ", GUILayout.Width(20));
            GUILayout.Label($"Sets: {sets.Count}", GUILayout.Width(70));
        }
    }

    void DrawDropZone()
    {
        GUILayout.Space(10);

        var r = GUILayoutUtility.GetRect(170, 20, GUILayout.Width(220));
        GUI.Box(r, "Load: Drop Preset JSON here", EditorStyles.helpBox);

        var e = Event.current;
        if (!r.Contains(e.mousePosition)) return;

        if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                // 1) per Asset (TextAsset)
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    var ta = obj as TextAsset;
                    if (!ta) continue;

                    var assetPath = AssetDatabase.GetAssetPath(ta);
                    var fullPath = System.IO.Path.Combine(
                        System.IO.Directory.GetParent(Application.dataPath).FullName,
                        assetPath
                    );

                    droppedPath = fullPath;
                    SaveLoadUpgradeSetPresets.LoadFromPath(fullPath);
                    Repaint();
                    break;
                }

                // 2) oder per FilePath (falls möglich)
                if (string.IsNullOrEmpty(droppedPath) && DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    var p = DragAndDrop.paths[0];
                    if (p.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        droppedPath = p;
                        SaveLoadUpgradeSetPresets.LoadFromPath(p);
                        Refresh();
                        Repaint();
                    }
                }
            }

            e.Use();
        }
    }


    void DrawMultiplierRow()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("BaseCost x", GUILayout.Width(75));
            baseCostMultiplier = EditorGUILayout.FloatField(baseCostMultiplier, GUILayout.Width(60));

            GUILayout.Space(10);

            GUILayout.Label("CostStep x", GUILayout.Width(75));
            costStepMultiplier = EditorGUILayout.FloatField(costStepMultiplier, GUILayout.Width(60));

            using (new EditorGUI.DisabledScope(
                sets.Count == 0 ||
                (Mathf.Approximately(baseCostMultiplier, 1f) &&
                 Mathf.Approximately(costStepMultiplier, 1f))))
            {
                if (GUILayout.Button("Apply", GUILayout.Width(70)))
                    ApplyCostMultipliers();
            }

            GUILayout.FlexibleSpace();
        }
    }

    void ApplyCostMultipliers()
    {
        if (sets.Count == 0) return;

        foreach (var set in sets)
        {
            if (!set) continue;

            var so = new SerializedObject(set);
            so.Update();

            var list = so.FindProperty(LIST_FIELD);
            if (list == null || !list.isArray) continue;

            Undo.RecordObject(set, "Apply Upgrade Cost Multipliers");

            for (int i = 0; i < list.arraySize; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                if (entry == null) continue;

                var baseCost = entry.FindPropertyRelative("baseCost");
                var costStep = entry.FindPropertyRelative("costStep");

                if (baseCost != null)
                {
                    if (baseCost.propertyType == SerializedPropertyType.Integer)
                        baseCost.intValue = Mathf.RoundToInt(baseCost.intValue * baseCostMultiplier);
                    else if (baseCost.propertyType == SerializedPropertyType.Float)
                    {
                        float v = baseCost.floatValue * baseCostMultiplier;
                        baseCost.floatValue = Mathf.Round(v * 100f) / 100f;
                    }
                }

                if (costStep != null)
                {
                    if (costStep.propertyType == SerializedPropertyType.Integer)
                        costStep.intValue = Mathf.RoundToInt(costStep.intValue * costStepMultiplier);
                    else if (costStep.propertyType == SerializedPropertyType.Float)
                    {
                        float v = costStep.floatValue * costStepMultiplier;
                        costStep.floatValue = Mathf.Round(v * 100f) / 100f;
                    }
                }

            }

            if (so.ApplyModifiedProperties())
                EditorUtility.SetDirty(set);
        }

        baseCostMultiplier = 1f;
        costStepMultiplier = 1f;

        AssetDatabase.SaveAssets();
        Repaint();
    }

    void SetAllFoldouts(bool state)
    {
        foreach (var s in sets)
            if (s) foldout[s.GetInstanceID()] = state;

        Repaint();
    }

    SerializedObject GetSO(UnityEngine.Object obj)
    {
        int id = obj.GetInstanceID();
        if (!soCache.TryGetValue(id, out var so) || so == null)
        {
            so = new SerializedObject(obj);
            soCache[id] = so;
        }
        return so;
    }

    static void DrawRawInspector(SerializedObject so)
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
}
#endif
