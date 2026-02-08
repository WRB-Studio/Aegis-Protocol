using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Upgrade/UpgradeSet")]
public class UpgradeSet : ScriptableObject
{
    public StationModule.eModuleType moduleType;
    public List<UpgradeAttribute> upgradeAttributes;

    public static List<UpgradeSet> allUpgradeSets = new List<UpgradeSet>();


    public void Init()
    {
        allUpgradeSets.Add(this);
    }

    public UpgradeSet GetInstanceOfUpgradeSet()
    {
        UpgradeSet clonedSet = CreateInstance<UpgradeSet>();
        clonedSet.name = name;

        clonedSet.upgradeAttributes = new List<UpgradeAttribute>();
        foreach (var upgrade in upgradeAttributes)
        {
            var clone = new UpgradeAttribute
            {
                upgradeName = upgrade.upgradeName,
                description = upgrade.description,

                level = upgrade.level,
                maxLevel = upgrade.maxLevel,

                baseValue = upgrade.baseValue,
                upgradeValue = upgrade.upgradeValue,
                maxValue = upgrade.maxValue,
                currentValue = upgrade.currentValue,

                baseCost = upgrade.baseCost,
                costStep = upgrade.costStep,
                cost = upgrade.cost,
            };

            clone.Init();
            clonedSet.upgradeAttributes.Add(clone);
        }

        return clonedSet;
    }



}
