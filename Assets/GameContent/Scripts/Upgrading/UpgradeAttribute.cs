using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UpgradeAttribute : IResettable
{
    public enum eUpgradeName
    {
        None,

        FireRate,
        Damage,

        FireRange,

        RotationSpeed,
        StructuralIntegrity,

        AutoCollecting,
        CollectingEfficiency,

        DroneCount,
        DroneHP,
        DroneBuildTime,
        DroneDamage,

        ShieldCapacity,
        RechargeTime,
        DeflectionChance,

        TimeMultiplier,
    }

    [Header("Name & Description")]
    public eUpgradeName upgradeName;
    public string description;

    [Header("Level")]
    public int level;
    public int maxLevel;

    [Header("Value")]
    public float baseValue;
    public float upgradeValue;
    public float maxValue;
    public float currentValue;

    [Header("Cost")]
    [SerializeField] public float baseCost;
    [SerializeField] public float costMultiplier;
    [SerializeField] public float cost;

    public static List<UpgradeAttribute> allUpgradeAttributes = new List<UpgradeAttribute>();

    // --- INIT SNAPSHOT ---
    private int initlevel;
    private float initcurrentValue;
    private float initcost;

    // baseValue can be left at 0 in the UpgradeSets.
    // We will pull safe defaults from the current scene state.
    private bool baseValueAutoFilled;
    internal StationModule ownerModule;

    public void Init()
    {
        if (!allUpgradeAttributes.Contains(this))
            allUpgradeAttributes.Add(this);

        RecalculateFromLevel();
    }

    private void TryAutoFillBaseValue()
    {
        if (baseValueAutoFilled) return;
        if (baseValue != 0f) { baseValueAutoFilled = true; return; }

        bool filled = false;

        switch (upgradeName)
        {
            case eUpgradeName.FireRate:
                if (Tower.Instance) { baseValue = Mathf.Max(0.1f, Tower.Instance.initialFireRate); filled = true; }
                break;

            case eUpgradeName.Damage:
                if (Tower.Instance) { baseValue = Mathf.Max(1f, Tower.Instance.initialDamage); filled = true; }
                break;

            case eUpgradeName.FireRange:
                if (Tower.Instance) { baseValue = Mathf.Max(0.1f, Tower.Instance.initialFireRange); filled = true; }
                break;

            case eUpgradeName.RotationSpeed:
                if (Tower.Instance) { baseValue = Mathf.Max(0.1f, Tower.Instance.rotationSpeed); filled = true; }
                break;

            case eUpgradeName.StructuralIntegrity:
                {
                    var core = StationModule.GetModuleByType(StationModule.eModuleType.Core);
                    if (core) { baseValue = Mathf.Max(1f, core.maxHP); filled = true; }
                    break;
                }

            case eUpgradeName.ShieldCapacity:
                if (Shield.Instance) { baseValue = Mathf.Max(1f, Shield.Instance.maxShieldPoints); filled = true; }
                break;

            case eUpgradeName.RechargeTime:
                if (Shield.Instance) { baseValue = Mathf.Max(0.1f, Shield.Instance.rechargeTime); filled = true; }
                break;

            case eUpgradeName.DeflectionChance:
                if (Shield.Instance) { baseValue = Shield.Instance.deflectionChance; filled = true; }
                break;

            case eUpgradeName.DroneCount:
                if (DroneManager.Instance) { baseValue = Mathf.Max(0f, DroneManager.Instance.currentDroneSlots); filled = true; }
                break;

            case eUpgradeName.DroneHP:
                if (DroneManager.Instance) { baseValue = Mathf.Max(1f, DroneManager.Instance.droneInitialHP); filled = true; }
                break;

            case eUpgradeName.DroneBuildTime:
                if (DroneManager.Instance) { baseValue = Mathf.Max(0.1f, DroneManager.Instance.droneBuildTime); filled = true; }
                break;

            case eUpgradeName.DroneDamage:
                if (DroneManager.Instance) { baseValue = Mathf.Max(1f, DroneManager.Instance.droneInitialDamage); filled = true; }
                break;
        }

        // If dependencies were not ready yet, keep it false so we can retry later.
        baseValueAutoFilled = filled;

        // If we just filled a missing baseValue, make sure level 0 also reflects it.
        if (filled && level == 0 && currentValue == 0f)
            currentValue = baseValue;
    }

    public static void ApplyAllUpgradeEffect()
    {
        foreach (UpgradeAttribute upgradeAttribute in allUpgradeAttributes)
        {
            upgradeAttribute.ApplyUpgradeEffect();
        }
    }

    public void ApplyUpgradeEffect()
    {
        // Ensure we never apply "zero" defaults by accident.
        TryAutoFillBaseValue();
        float value = ownerModule && !ownerModule.isBuilt ? baseValue : currentValue;

        switch (upgradeName)
        {
            case eUpgradeName.None:
                break;

            // Ammo Fabricator
            case eUpgradeName.FireRate:
                Tower.Instance.fireRate = Mathf.Max(0.1f, value);
                break;

            case eUpgradeName.Damage:
                Tower.Instance.damage = Mathf.Max(1, Mathf.RoundToInt(value));
                break;

            // Radar
            case eUpgradeName.FireRange:
                Tower.Instance.fireRange = Mathf.Max(0.1f, value);
                break;

            // Command Unit
            case eUpgradeName.RotationSpeed:
                Tower.Instance.rotationSpeed = Mathf.Max(0.1f, value);
                break;

            case eUpgradeName.StructuralIntegrity:
                foreach (var module in StationModule.allModules)
                {
                    float currentMaxHP = module.maxHP;
                    float currentHP = module.currentHP;
                    float pct = currentMaxHP > 0 ? (currentHP / currentMaxHP) : 1f;

                    module.maxHP = Mathf.Max(1, Mathf.RoundToInt(value));
                    module.currentHP = Mathf.Clamp(Mathf.RoundToInt(module.maxHP * pct), 0, module.maxHP);
                }
                break;

            // Extractor
            case eUpgradeName.AutoCollecting:
                if (level == 1 && ownerModule && ownerModule.isBuilt)
                    ResourceManager.Instance.enableAutoCollecting();
                else
                    ResourceManager.Instance.disableAutoCollecting();
                break;

            case eUpgradeName.CollectingEfficiency:
                ResourceManager.Instance.collectingEffeciency = value;
                break;

            // Drone
            case eUpgradeName.DroneCount:
                DroneManager.Instance.currentDroneSlots = Mathf.Max(0, Mathf.RoundToInt(value));
                break;

            case eUpgradeName.DroneHP:
                DroneManager.Instance.droneInitialHP = Mathf.Max(1, Mathf.RoundToInt(value));
                foreach (var drone in DroneManager.Instance.allDrones)
                {
                    if (!drone) continue;
                    int damageTaken = drone.maxHP - drone.currentHP;
                    drone.maxHP = DroneManager.Instance.droneInitialHP;
                    drone.currentHP = Mathf.Clamp(drone.maxHP - damageTaken, 0, drone.maxHP);
                }
                break;

            case eUpgradeName.DroneBuildTime:
                DroneManager.Instance.droneBuildTime = Mathf.Max(0.05f, value);
                break;

            case eUpgradeName.DroneDamage:
                DroneManager.Instance.droneInitialDamage = Mathf.Max(1, Mathf.RoundToInt(value));
                foreach (var drone in DroneManager.Instance.allDrones)
                    if (drone) drone.damage = DroneManager.Instance.droneInitialDamage;
                break;

            // Shield
            case eUpgradeName.ShieldCapacity:
                {
                    float currentMax = Shield.Instance.maxShieldPoints;
                    float current = Shield.Instance.currentShieldPoints;
                    float pct = currentMax > 0 ? (current / currentMax) : 1f;

                    Shield.Instance.maxShieldPoints = Mathf.Max(1, Mathf.RoundToInt(value));
                    Shield.Instance.currentShieldPoints = Mathf.Clamp(Mathf.RoundToInt(Shield.Instance.maxShieldPoints * pct), 0, Shield.Instance.maxShieldPoints);
                    break;
                }

            case eUpgradeName.RechargeTime:
                {
                    float currentTime = Shield.Instance.rechargeTime;
                    float currentCd = Shield.Instance.rechargeCountdown;
                    float pct = currentTime > 0 ? (currentCd / currentTime) : 1f;

                    Shield.Instance.rechargeTime = Mathf.Max(0.1f, value);
                    Shield.Instance.rechargeCountdown = Shield.Instance.rechargeTime * pct;

                    Shield.Instance.refreshShieldPointSlider();
                    break;
                }

            case eUpgradeName.DeflectionChance:
                Shield.Instance.deflectionChance = value;
                break;
        }
    }

    public void Upgrade()
    {
        if (level >= maxLevel) return;

        level++;
        currentValue = CalculateValue();
        cost = CalculateCost();

        ApplyUpgradeEffect();

        SaveGameManager.Instance.Save();
    }

    public void RecalculateFromLevel()
    {
        currentValue = CalculateValue();
        cost = CalculateCost();
    }

    public float CalculateValue() => CalculateValue(level);

    public float CalculateValue(int targetLevel)
    {
        if (maxLevel <= 0) return baseValue;
        float value = baseValue + Mathf.Clamp(targetLevel, 0, maxLevel) * upgradeValue;
        return upgradeValue >= 0f ? Mathf.Min(value, maxValue) : Mathf.Max(value, maxValue);
    }

    public float CalculateCost() => CalculateCost(level);

    public float CalculateCost(int targetLevel)
    {
        double price = baseCost * System.Math.Pow(costMultiplier, Mathf.Clamp(targetLevel, 0, maxLevel));
        return (float)System.Math.Min(1000000000d, System.Math.Max(0d, price));
    }

    public static UpgradeAttribute GetUpgradeByName(eUpgradeName upgradeName)
    {
        foreach (var upgrade in allUpgradeAttributes)
            if (upgrade.upgradeName == upgradeName)
                return upgrade;
        return null;
    }

    public void StoreInit()
    {
        initlevel = level;
        initcurrentValue = currentValue;
        initcost = cost;
    }

    public void ResetScript()
    {
        level = initlevel;
        currentValue = initcurrentValue;
        cost = initcost;
    }

    public static void StoreAllInits()
    {
        for (int i = 0; i < allUpgradeAttributes.Count; i++)
            allUpgradeAttributes[i]?.StoreInit();
    }

    // IMPORTANT: only reset data here. Applying effects is done explicitly
    // after loading data (SaveGameManager) or after replay reset (GameManager.Replay).
    public static void ResetAll()
    {
        for (int i = 0; i < allUpgradeAttributes.Count; i++)
            allUpgradeAttributes[i]?.ResetScript();
    }
}
