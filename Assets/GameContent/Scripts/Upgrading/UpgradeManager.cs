using UnityEngine;

public class UpgradeManager : MonoBehaviour, IResettable
{
    public static UpgradeManager Instance;


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

            var runtimeCopy = module.upgradeSet.GetInstanceOfUpgradeSet();
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
