using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
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
            foreach (var upgrade in runtimeCopy.upgradeAttributes)
                upgrade.ownerModule = module;
            runtimeCopy.Init();
            module.upgradeSet = runtimeCopy;
        }
    }
}
