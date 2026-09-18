using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour, IResettable
{
    public static UpgradeManager Instance;
    readonly Dictionary<StationModule, UpgradeSet> sourceSets = new();


    private void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        UpgradeAttribute.allUpgradeAttributes.Clear();
        UpgradeSet.allUpgradeSets.Clear();

        foreach (var module in StationModule.allModules)
        {
            if (module.upgradeSet == null) continue;

            if (!sourceSets.TryGetValue(module, out var sourceSet))
            {
                sourceSet = module.upgradeSet;
                sourceSets[module] = sourceSet;
            }

            var runtimeCopy = sourceSet.GetInstanceOfUpgradeSet();
            runtimeCopy.Init();
            module.upgradeSet = runtimeCopy;
        }
    }

    public void StoreInit()
    {
        // nichts zu speichern
    }

    public void ResetScript()
    {
        Init();
    }

}
