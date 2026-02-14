#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class UpgradeProgressPreviewWindow : EditorWindow
{
    const string UPGRADESETS_PATH = "Assets/GameContent/Prefabs/UpgradeSets";
    const string UPGRADE_SET_TYPE = "UpgradeSet";
    const string LIST_FIELD = "upgradeAttributes";

    // Felder in UpgradeAttribute (anpassbar)
    const string F_NAME = "upgradeName";
    const string F_MAX = "maxLevel";
    const string F_BASE_COST = "baseCost";
    const string F_COST_STEP = "costStep";
    const string F_COST_MULT = "costMultiplier";
    const string F_BASE_VAL = "baseValue";
    const string F_VAL_STEP = "upgradeValue"; // bei dir so
    const string F_VAL_MULT = "valueMultiplier";

    readonly List<UnityEngine.Object> sets = new();
    readonly Dictionary<int, SerializedObject> soCache = new();
    readonly Dictionary<int, bool> foldoutSet = new();

    Vector2 scroll;
    int showLevels = 15;
    float cardWidth = 10f;

    [MenuItem("Tools/Aegis/Upgrades Progress Preview")]
    static void Open() => GetWindow<UpgradeProgressPreviewWindow>("Upgrades Preview");

    void OnEnable() => Refresh();

    void OnDisable()
    {
        sets.Clear();
        soCache.Clear();
        foldoutSet.Clear();
    }

    void Refresh()
    {
        sets.Clear();
        soCache.Clear();
        foldoutSet.Clear();

        var guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { UPGRADESETS_PATH });
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (!obj) continue;
            if (obj.GetType().Name == UPGRADE_SET_TYPE) sets.Add(obj);
        }
        Repaint();
    }

    void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            if (GUILayout.Button("Refresh", GUILayout.Width(70))) Refresh();
            showLevels = EditorGUILayout.IntSlider("Show Levels", showLevels, 5, 50);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Sets: {sets.Count}", GUILayout.Width(70));
        }

        if (sets.Count == 0)
        {
            EditorGUILayout.HelpBox($"No UpgradeSets found in:\n{UPGRADESETS_PATH}", MessageType.Info);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (var set in sets)
        {
            if (!set) continue;

            int id = set.GetInstanceID();
            if (!foldoutSet.ContainsKey(id)) foldoutSet[id] = false;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    foldoutSet[id] = EditorGUILayout.Foldout(foldoutSet[id], set.name, true);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Ping", GUILayout.Width(55))) EditorGUIUtility.PingObject(set);
                    if (GUILayout.Button("Select", GUILayout.Width(60))) Selection.activeObject = set;
                }

                if (!foldoutSet[id]) continue;

                var so = GetSO(set);
                so.Update();

                var list = so.FindProperty(LIST_FIELD);
                if (list == null || !list.isArray)
                {
                    EditorGUILayout.HelpBox($"Field '{LIST_FIELD}' not found / not array.", MessageType.Warning);
                    continue;
                }

                int cols = Mathf.Max(1, Mathf.FloorToInt((position.width - 40f) / cardWidth));
                int col = 0;

                EditorGUILayout.BeginHorizontal();

                for (int i = 0; i < list.arraySize; i++)
                {
                    var entry = list.GetArrayElementAtIndex(i);
                    if (entry == null) continue;

                    if (col >= cols)
                    {
                        col = 0;
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                    }

                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(cardWidth)))
                    {
                        string upName = TryGetName(entry, F_NAME, $"Upgrade #{i}");
                        EditorGUILayout.LabelField(upName, EditorStyles.boldLabel);

                        int maxLevel = Mathf.Max(1, TryGetInt(entry, F_MAX, showLevels));
                        int levels = Mathf.Min(showLevels, maxLevel);

                        float baseCost = TryGetFloat(entry, F_BASE_COST, 0f);
                        float costStep = TryGetFloat(entry, F_COST_STEP, 0f);
                        float costMult = TryGetFloat(entry, F_COST_MULT, 1f);

                        float baseVal = TryGetFloat(entry, F_BASE_VAL, 0f);
                        float valStep = TryGetFloat(entry, F_VAL_STEP, 0f);
                        float valMult = TryGetFloat(entry, F_VAL_MULT, 1f);

                        var costs = new float[levels];
                        var vals = new float[levels];

                        for (int l = 1; l <= levels; l++)
                        {
                            float c = baseCost + costStep * (l - 1);
                            if (!Mathf.Approximately(costMult, 1f)) c *= Mathf.Pow(costMult, (l - 1));

                            float v = baseVal + valStep * (l - 1);
                            if (!Mathf.Approximately(valMult, 1f)) v *= Mathf.Pow(valMult, (l - 1));

                            costs[l - 1] = c;
                            vals[l - 1] = v;
                        }

                        DrawTable(costs, vals);
                    }

                    col++;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawTable(float[] costs, float[] vals)
    {
        var mini = EditorStyles.miniLabel;

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Lvl", mini, GUILayout.Width(26));
            GUILayout.Label("Cost", mini, GUILayout.Width(60));
            GUILayout.Label("Val", mini, GUILayout.Width(60));
        }

        for (int i = 0; i < costs.Length; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label((i + 1).ToString(), mini, GUILayout.Width(26));
                GUILayout.Label(Mathf.RoundToInt(costs[i]).ToString(), mini, GUILayout.Width(60));
                GUILayout.Label(vals[i].ToString("0.###"), mini, GUILayout.Width(60));
            }
        }
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

    static string TryGetString(SerializedProperty entry, string name, string fallback)
    {
        var p = entry.FindPropertyRelative(name);
        return (p != null && p.propertyType == SerializedPropertyType.String && !string.IsNullOrEmpty(p.stringValue))
            ? p.stringValue : fallback;
    }

    static int TryGetInt(SerializedProperty entry, string name, int fallback)
    {
        var p = entry.FindPropertyRelative(name);
        return (p != null && p.propertyType == SerializedPropertyType.Integer) ? p.intValue : fallback;
    }

    static float TryGetFloat(SerializedProperty entry, string name, float fallback)
    {
        var p = entry.FindPropertyRelative(name);
        if (p == null) return fallback;
        if (p.propertyType == SerializedPropertyType.Float) return p.floatValue;
        if (p.propertyType == SerializedPropertyType.Integer) return p.intValue;
        return fallback;
    }

    static string TryGetName(SerializedProperty entry, string name, string fallback)
    {
        var p = entry.FindPropertyRelative(name);
        if (p == null) return fallback;

        if (p.propertyType == SerializedPropertyType.Enum)
            return p.enumDisplayNames != null && p.enumDisplayNames.Length > 0
                ? p.enumDisplayNames[p.enumValueIndex]
                : p.enumValueIndex.ToString();

        if (p.propertyType == SerializedPropertyType.String && !string.IsNullOrEmpty(p.stringValue))
            return p.stringValue;

        return fallback;
    }

}
#endif
