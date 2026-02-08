// Pfad: Assets/Editor/UpgradeOverviewWindow.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class UpgradeOverviewWindow : EditorWindow
{
    Vector2 scrollPos;

    [MenuItem("Tools/Upgrade Overview")]
    public static void ShowWindow()
    {
        GetWindow<UpgradeOverviewWindow>("Upgrade Overview");
    }

    void OnGUI()
    {
        GUILayout.Label("Alle UpgradeSets im Projekt", EditorStyles.boldLabel);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        List<UpgradeSet> allSets = UpgradeSet.allUpgradeSets;
        foreach (UpgradeSet set in allSets)
        {
            if (set == null) continue;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"UpgradeSet: {set.name.ToString()}", EditorStyles.boldLabel);

            foreach (var attr in set.upgradeAttributes)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Name", attr.upgradeName.ToString());
                EditorGUILayout.LabelField("Level", $"{attr.level} / {attr.maxLevel}");
                //EditorGUILayout.LabelField("Description", attr.description);

                EditorGUILayout.LabelField("BaseValue", attr.baseValue.ToString());
                EditorGUILayout.LabelField("UpgradeValue", attr.upgradeValue.ToString());
                EditorGUILayout.LabelField("MaxValue", attr.maxValue.ToString());
                EditorGUILayout.LabelField("CurrentValue", attr.currentValue.ToString());

                EditorGUILayout.LabelField("BaseCost", attr.baseCost.ToString());
                EditorGUILayout.LabelField("CostMultiplier", attr.costStep.ToString());
                EditorGUILayout.LabelField("Cost", attr.cost.ToString());

                EditorGUILayout.EndVertical();
            }
        }

        EditorGUILayout.EndScrollView();
    }
}
