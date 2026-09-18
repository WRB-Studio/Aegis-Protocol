using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

public class SaveGameTests
{
    [Test]
    public void SaveGame_FileRoundTrip_PreservesResourcesModulesAndDrones()
    {
        var original = new SaveGame
        {
            material = 137,
            currentWaveIndex = 4,
            currentShieldPoints = 12.5f,
            shieldRechargeCountdown = 3.25f,
            droneBuildCountdown = 8.5f
        };
        original.modules.Add(new SaveGame.ModuleState
        {
            moduleType = StationModule.eModuleType.Core.ToString(),
            isBuilt = true,
            currentHP = 7
        });
        original.modules.Add(new SaveGame.ModuleState
        {
            moduleType = StationModule.eModuleType.Drone.ToString(),
            isBuilt = true,
            wasDestroyed = false,
            currentHP = 3
        });
        original.drones.Add(new SaveGame.DroneState { currentHP = 2 });
        original.drones.Add(new SaveGame.DroneState { currentHP = 5 });
        original.upgrades.Add(new SaveGame.UpgradeState
        {
            upgradeName = UpgradeAttribute.eUpgradeName.DroneHP.ToString(),
            level = 2
        });

        string path = Path.Combine(Path.GetTempPath(), "aegis-save-test-" + Guid.NewGuid() + ".json");
        try
        {
            File.WriteAllText(path, JsonUtility.ToJson(original));
            var loaded = JsonUtility.FromJson<SaveGame>(File.ReadAllText(path));

            Assert.That(loaded.material, Is.EqualTo(137));
            Assert.That(loaded.currentWaveIndex, Is.EqualTo(4));
            Assert.That(loaded.currentShieldPoints, Is.EqualTo(12.5f));
            Assert.That(loaded.shieldRechargeCountdown, Is.EqualTo(3.25f));
            Assert.That(loaded.droneBuildCountdown, Is.EqualTo(8.5f));
            Assert.That(loaded.modules, Has.Count.EqualTo(2));
            Assert.That(loaded.modules[0].currentHP, Is.EqualTo(7));
            Assert.That(loaded.modules[1].currentHP, Is.EqualTo(3));
            Assert.That(loaded.modules[1].isBuilt, Is.True);
            Assert.That(loaded.drones, Has.Count.EqualTo(2));
            Assert.That(loaded.drones[0].currentHP, Is.EqualTo(2));
            Assert.That(loaded.drones[1].currentHP, Is.EqualTo(5));
            Assert.That(loaded.upgrades[0].level, Is.EqualTo(2));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    public void Stats_FileRoundTrip_RestoresCountersAndKills()
    {
        var previousInstance = Stats.Instance;
        var gameObject = new GameObject("SaveGameTests Stats");
        try
        {
            var stats = gameObject.AddComponent<Stats>();
            Stats.Instance = stats;
            stats.resourcesSpawned = 19;
            stats.resourcesCollectedManually = 11;
            stats.modulesCost = 42;
            stats.dronesBuilt = 2;
            stats.RegisterKill(Enemy.eEnemyType.Fast, Stats.eDeadBy.droneProjectile);
            stats.RegisterKill(Enemy.eEnemyType.Fast, Stats.eDeadBy.droneProjectile);

            var save = new SaveGame { stats = stats.GetStatsData() };
            var loaded = JsonUtility.FromJson<SaveGame>(JsonUtility.ToJson(save));

            stats.ResetStats();
            stats.ApplyStatsData(loaded.stats);

            Assert.That(stats.resourcesSpawned, Is.EqualTo(19));
            Assert.That(stats.resourcesCollectedManually, Is.EqualTo(11));
            Assert.That(stats.modulesCost, Is.EqualTo(42));
            Assert.That(stats.dronesBuilt, Is.EqualTo(2));
            Assert.That(stats.GetTotalKills(), Is.EqualTo(2));
            Assert.That(stats.killsByTypeAndCause[(Enemy.eEnemyType.Fast, Stats.eDeadBy.droneProjectile)], Is.EqualTo(2));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
            Stats.Instance = previousInstance;
        }
    }
}
