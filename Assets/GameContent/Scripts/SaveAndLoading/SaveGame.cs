using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveGame
{
    // Optional: für spätere Änderungen am Save-Format
    public int version = 2;

    public int score = 0;

    // Resources
    public int material;

    // Progress
    public int currentWaveIndex;

    // Shield
    public float currentShieldPoints;
    public float shieldRechargeCountdown;

    // Drones
    public float droneBuildCountdown;
    public List<DroneState> drones = new();

    // Modules + Upgrades
    public List<ModuleState> modules = new();
    public List<UpgradeState> upgrades = new();

    // Stats (du hast das schon als StatsData)
    public StatsData stats;

    [Serializable]
    public class ModuleState
    {
        // als string, damit es robust bleibt (JsonUtility + Enum-Änderungen)
        public string moduleType;   // module.moduleType.ToString()
        public bool isBuilt;
        public bool wasDestroyed;
        public int currentHP;
    }

    [Serializable]
    public class DroneState
    {
        public int currentHP;
    }

    [Serializable]
    public class UpgradeState
    {
        public string upgradeName;  // upgrade.upgradeName.ToString()
        public int level;
    }


}
